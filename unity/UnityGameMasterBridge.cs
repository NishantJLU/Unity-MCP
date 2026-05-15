using UnityEngine;
using UnityEditor;
using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace UnityGM
{
    public class UnityGameMasterBridge : EditorWindow
    {
        private static HttpListener listener;
        private static bool isRunning = false;
        private static readonly string port = "60432";
        private string log = "Game Master Bridge Ready...";
        private Vector2 scrollPos;

        [MenuItem("Window/AI/Game Master Bridge")]
        public static void ShowWindow()
        {
            GetWindow<UnityGameMasterBridge>("GM Bridge");
        }

        private void OnGUI()
        {
            // Header
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Unity GM Bridge (MCP)", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(isRunning ? $"Listening on port {port}" : "GM Bridge Offline", 
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
                listener.Prefixes.Add($"http://localhost:{port}/");
                listener.Start();
                isRunning = true;
                log = $"[{DateTime.Now:HH:mm:ss}] Bridge started. Waiting for GM commands...\n";
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

                switch (cmd.command)
                {
                    case "GET_STATE":
                        responseData = GetGameState();
                        break;
                    case "GET_PRESETS":
                        responseData = LoadPresetsRaw();
                        break;
                    case "SPAWN_ENTITY":
                        responseData = SpawnEntity(cmd.data);
                        break;
                    case "TRIGGER_EVENT":
                        responseData = TriggerEvent(cmd.data);
                        break;
                    case "BROADCAST":
                        responseData = BroadcastMessage(cmd.data);
                        break;
                    case "SET_DIALOGUE":
                        responseData = SetDialogue(cmd.data);
                        break;
                    case "QUERY_NEARBY":
                        responseData = QueryNearby(cmd.data);
                        break;
                    default:
                        responseData = "{\"status\":\"error\", \"message\":\"Unknown GM command\"}";
                        break;
                }
            }
            catch (Exception e)
            {
                responseData = "{\"status\":\"error\", \"message\":\"" + e.Message + "\"}";
                log += $"Error: {e.Message}\n";
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

        private string GetGameState()
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

        private string SpawnEntity(GMData data)
        {
            GameObject entity = null;
            Preset foundPreset = null;

            // Try to find preset
            if (!string.IsNullOrEmpty(data.preset)) {
                var raw = LoadPresetsRaw();
                var list = JsonUtility.FromJson<PresetList>(raw);
                foundPreset = list.presets.FirstOrDefault(p => p.id == data.preset);
            }

            entity = GameObject.CreatePrimitive(data.type == "Monster" ? PrimitiveType.Capsule : PrimitiveType.Cube);
            entity.name = string.IsNullOrEmpty(data.name) ? (foundPreset != null ? foundPreset.displayName : "GM_" + data.type) : data.name;
            
            if (data.position != null && data.position.Length == 3)
                entity.transform.position = new Vector3(data.position[0], data.position[1], data.position[2]);
            else
                entity.transform.position = SceneView.lastActiveSceneView.pivot;

            if (foundPreset != null) {
                entity.transform.localScale = Vector3.one * foundPreset.scale;
                var renderer = entity.GetComponent<Renderer>();
                if (renderer != null && foundPreset.color != null && foundPreset.color.Length >= 3) {
                    renderer.sharedMaterial.color = new Color(foundPreset.color[0], foundPreset.color[1], foundPreset.color[2], foundPreset.color.Length > 3 ? foundPreset.color[3] : 1f);
                }
                log += $"Applied preset: {foundPreset.id}\n";
            }

            return "{\"status\":\"success\", \"entity_name\":\"" + entity.name + "\"}";
        }

        private string BroadcastMessage(GMData data) {
            log += $"[BROADCAST] ({data.style}): {data.message}\n";
            return "{\"status\":\"success\"}";
        }

        private string TriggerEvent(GMData data)
        {
            log += $"[EVENT] {data.event_name} (Intensity: {data.intensity})\n";
            return "{\"status\":\"success\"}";
        }

        private string SetDialogue(GMData data)
        {
            var target = GameObject.Find(data.target_name);
            if (target == null) return "{\"status\":\"error\", \"message\":\"NPC not found\"}";
            log += $"[DIALOGUE] {data.target_name}: \"{data.text}\" ({data.mood})\n";
            return "{\"status\":\"success\"}";
        }

        private string QueryNearby(GMData data)
        {
            var objects = GameObject.FindObjectsOfType<GameObject>();
            var nearby = objects.Take(5).Select(o => o.name).ToList();
            return "{\"status\":\"success\", \"entities\":" + JsonUtility.ToJson(nearby) + "}";
        }

        private void SendResponse(HttpListenerContext context, string response)
        {
            try {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(response);
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
        }

        [Serializable] public class PresetList { public Preset[] presets; }
        [Serializable] public class Preset {
            public string id; public string displayName; public string type;
            public float scale; public float[] color; public string[] components;
        }
    }
}
