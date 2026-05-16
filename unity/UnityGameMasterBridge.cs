using UnityEngine;
using UnityEditor;
using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityGM
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MCPToolAttribute : Attribute
    {
        public string ToolName { get; }
        public MCPToolAttribute(string name) => ToolName = name;
    }

    public class UnityGameMasterBridge : EditorWindow
    {
        private static HttpListener listener;
        private static bool isRunning = false;
        private static readonly string port = "60432";
        private string log = "Game Master Bridge Ready...\n";
        private Vector2 scrollPos;
        private string apiKey = "";

        private Dictionary<string, MethodInfo> registeredTools = new Dictionary<string, MethodInfo>();

        [MenuItem("Window/AI/Game Master Bridge")]
        public static void ShowWindow()
        {
            GetWindow<UnityGameMasterBridge>("GM Bridge");
        }

        private void OnEnable()
        {
            apiKey = EditorPrefs.GetString("UnityMCP_APIKey", Guid.NewGuid().ToString());
            EditorPrefs.SetString("UnityMCP_APIKey", apiKey);
            RegisterTools();
        }

        private void RegisterTools()
        {
            registeredTools.Clear();
            var methods = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<MCPToolAttribute>();
                if (attr != null)
                {
                    registeredTools[attr.ToolName] = method;
                }
            }
            log += $"Registered {registeredTools.Count} tools via Attributes.\n";
        }

        private void OnGUI()
        {
            // Header
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Unity GM Bridge (MCP)", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            apiKey = EditorGUILayout.TextField("API Key", apiKey);
            if (EditorGUI.EndChangeCheck()) {
                EditorPrefs.SetString("UnityMCP_APIKey", apiKey);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(isRunning ? $"Listening on 127.0.0.1:{port}" : "GM Bridge Offline", 
                isRunning ? MessageType.Info : MessageType.Warning);
            
            if (GUILayout.Button(isRunning ? "Stop Bridge" : "Start Bridge", GUILayout.Height(38), GUILayout.Width(100)))
            {
                if (isRunning) StopServer();
                else StartServer();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // Log Area
            GUILayout.Label("Activity Log:", EditorStyles.miniBoldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(400));
            GUIStyle logStyle = new GUIStyle(EditorStyles.textArea);
            logStyle.wordWrap = true;
            EditorGUILayout.TextArea(log, logStyle, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Clear Log", GUILayout.Width(70))) {
                log = "Log Cleared.\n";
            }

            // Presets Preview
            if (isRunning) {
                GUILayout.Space(10);
                if (GUILayout.Button("Reload Presets.json")) {
                    LoadPresets();
                }
            }
        }

        private void StartServer()
        {
            try {
                listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                listener.Start();
                isRunning = true;
                log += $"[{DateTime.Now:HH:mm:ss}] Bridge started. Waiting for GM commands...\n";
                Task.Run(() => ListenLoop());
            } catch (Exception e) {
                Debug.LogError($"Failed to start GM Bridge: {e.Message}");
            }
        }

        private void StopServer()
        {
            isRunning = false;
            listener?.Stop();
            listener?.Close();
            log += $"[{DateTime.Now:HH:mm:ss}] Bridge stopped.\n";
        }

        private async void ListenLoop()
        {
            while (isRunning)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    if (context.Request.HttpMethod == "POST")
                    {
                        string token = context.Request.Headers["X-MCP-Token"];
                        if (token != apiKey && !string.IsNullOrEmpty(apiKey)) {
                            SendResponse(context, "{\"status\":\"error\", \"message\":\"Unauthorized: Invalid API Key\"}", 401);
                            continue;
                        }

                        using (var reader = new StreamReader(context.Request.InputStream))
                        {
                            string json = await reader.ReadToEndAsync();
                            EditorApplication.delayCall += () => ProcessCommand(json, context);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (isRunning) Debug.LogWarning($"GM Bridge Error: {e.Message}");
                }
            }
        }

        private void ProcessCommand(string json, HttpListenerContext context)
        {
            string responseData = "";
            try
            {
                var cmd = JsonUtility.FromJson<GMRequest>(json);
                log += $"[{DateTime.Now:HH:mm:ss}] CMD: {cmd.command}\n";

                if (registeredTools.TryGetValue(cmd.command, out MethodInfo method))
                {
                    responseData = (string)method.Invoke(this, new object[] { cmd.data });
                }
                else
                {
                    responseData = "{\"status\":\"error\", \"message\":\"Unknown GM command\"}";
                }
            }
            catch (Exception e)
            {
                string msg = e.InnerException != null ? e.InnerException.Message : e.Message;
                responseData = "{\"status\":\"error\", \"message\":\"" + msg + "\"}";
                log += $"Error: {msg}\n";
            }

            SendResponse(context, responseData);
            scrollPos.y = float.MaxValue; // Auto-scroll
            Repaint();
        }

        private string LoadPresetsRaw() {
            TextAsset asset = Resources.Load<TextAsset>("GMPresets");
            return asset != null ? asset.text : "{\"presets\":[]}";
        }

        private void LoadPresets() {
            var raw = LoadPresetsRaw();
            log += $"[{DateTime.Now:HH:mm:ss}] Loaded {raw.Length} bytes of preset data.\n";
        }

        [MCPTool("GET_STATE")]
        private string GetGameState(GMData data)
        {
            return JsonUtility.ToJson(new {
                status = "success",
                world_time = DateTime.Now.ToString("HH:mm"),
                player = new {
                    health = 100,
                    location = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot.ToString() : "Origin"
                }
            });
        }

        [MCPTool("GET_PRESETS")]
        private string GetPresets(GMData data)
        {
            return LoadPresetsRaw();
        }

        [MCPTool("SPAWN_ENTITY")]
        private string SpawnEntity(GMData data)
        {
            GameObject entity = null;
            Preset foundPreset = null;

            if (!string.IsNullOrEmpty(data.preset)) {
                var raw = LoadPresetsRaw();
                var list = JsonUtility.FromJson<PresetList>(raw);
                if (list != null && list.presets != null)
                {
                    foundPreset = list.presets.FirstOrDefault(p => p.id == data.preset);
                }
            }

            entity = GameObject.CreatePrimitive(data.type == "Monster" ? PrimitiveType.Capsule : PrimitiveType.Cube);
            entity.name = string.IsNullOrEmpty(data.name) ? (foundPreset != null ? foundPreset.displayName : "GM_" + data.type) : data.name;
            
            if (data.position != null && data.position.Length == 3)
                entity.transform.position = new Vector3(data.position[0], data.position[1], data.position[2]);
            else
                entity.transform.position = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;

            if (foundPreset != null) {
                entity.transform.localScale = Vector3.one * foundPreset.scale;
                var renderer = entity.GetComponent<Renderer>();
                if (renderer != null && foundPreset.color != null && foundPreset.color.Length >= 3) {
                    renderer.sharedMaterial.color = new Color(foundPreset.color[0], foundPreset.color[1], foundPreset.color[2], foundPreset.color.Length > 3 ? foundPreset.color[3] : 1f);
                }
                log += $"Applied preset: {foundPreset.id}\n";
            }

            Undo.RegisterCreatedObjectUndo(entity, $"Spawn {entity.name}");

            return "{\"status\":\"success\", \"entity_name\":\"" + entity.name + "\"}";
        }

        [MCPTool("BROADCAST")]
        private string BroadcastMessage(GMData data) {
            log += $"[BROADCAST] ({data.style}): {data.message}\n";
            return "{\"status\":\"success\"}";
        }

        [MCPTool("TRIGGER_EVENT")]
        private string TriggerEvent(GMData data)
        {
            log += $"[EVENT] {data.event_name} (Intensity: {data.intensity})\n";
            return "{\"status\":\"success\"}";
        }

        [MCPTool("SET_DIALOGUE")]
        private string SetDialogue(GMData data)
        {
            var target = GameObject.Find(data.target_name);
            if (target == null) return "{\"status\":\"error\", \"message\":\"NPC not found\"}";
            
            Undo.RecordObject(target, "Set Dialogue");
            log += $"[DIALOGUE] {data.target_name}: \"{data.text}\" ({data.mood})\n";
            return "{\"status\":\"success\"}";
        }

        [MCPTool("QUERY_NEARBY")]
        private string QueryNearby(GMData data)
        {
            Vector3 center = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            float radius = data.radius > 0 ? data.radius : 20f;
            
            var objects = GameObject.FindObjectsOfType<GameObject>();
            var nearby = objects.Where(o => Vector3.Distance(o.transform.position, center) <= radius)
                                .Take(50)
                                .Select(o => o.name)
                                .ToList();

            return "{\"status\":\"success\", \"entities\":" + JsonUtility.ToJson(new StringListWrapper { list = nearby }) + "}";
        }

        [MCPTool("SEARCH_ASSETS")]
        private string SearchAssets(GMData data)
        {
            if (string.IsNullOrEmpty(data.query)) return "{\"status\":\"error\", \"message\":\"Query cannot be empty\"}";
            
            string[] guids = AssetDatabase.FindAssets(data.query);
            var paths = guids.Select(g => AssetDatabase.GUIDToAssetPath(g)).Take(20).ToList();
            
            return "{\"status\":\"success\", \"assets\":" + JsonUtility.ToJson(new StringListWrapper { list = paths }) + "}";
        }

        private void SendResponse(HttpListenerContext context, string response, int statusCode = 200)
        {
            try {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(response);
                context.Response.StatusCode = statusCode;
                context.Response.ContentLength64 = buffer.Length;
                context.Response.ContentType = "application/json";
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.Close();
            } catch {}
        }

        [Serializable] public class GMRequest { public string command; public GMData data; }
        [Serializable] public class GMData {
            public string type; public string name; public string preset;
            public float[] position; public string event_name; public float intensity;
            public string target_name; public string text; public string mood;
            public string message; public string style;
            public float radius; public string query;
        }

        [Serializable] public class PresetList { public Preset[] presets; }
        [Serializable] public class Preset {
            public string id; public string displayName; public string type;
            public float scale; public float[] color; public string[] components;
        }

        [Serializable] public class StringListWrapper { public List<string> list; }
    }
}
