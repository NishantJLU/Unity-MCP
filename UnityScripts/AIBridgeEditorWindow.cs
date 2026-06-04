using UnityEngine;
using UnityEditor;
using System;
using System.Net;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace UnityMCP
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MCPToolAttribute : Attribute
    {
        public string ToolName { get; }
        public MCPToolAttribute(string name) => ToolName = name;
    }

    public class AIBridgeEditorWindow : EditorWindow
    {
        private static HttpListener listener;
        private static bool isRunning = false;
        private static readonly string port = "60432";
        private string log = "SYSTEM READY...\n";
        private Vector2 scrollPos;
        private Dictionary<string, MethodInfo> registeredTools = new Dictionary<string, MethodInfo>();

        private int selectedTab = 0;
        private string[] tabNames = new string[] { "Connection Status", "Registered Tools", "Activity Log" };

        [MenuItem("Window/AI/Unity MCP Pro")]
        public static void ShowWindow()
        {
            GetWindow<AIBridgeEditorWindow>("AI Bridge Pro");
        }

        private void OnEnable()
        {
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
        }

        private void OnGUI()
        {
            DrawHeader();
            
            EditorGUILayout.Space(5);
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(24));
            EditorGUILayout.Space(10);
            
            switch (selectedTab)
            {
                case 0:
                    DrawStatusTab();
                    break;
                case 1:
                    DrawToolsTab();
                    break;
                case 2:
                    DrawLogsTab();
                    break;
            }
        }

        private GUIStyle GetButtonStyle(string hex) {
            var style = new GUIStyle(GUI.skin.button);
            ColorUtility.TryParseHtmlString(hex, out Color col);
            style.normal.textColor = Color.white;
            style.fontStyle = FontStyle.Bold;
            return style;
        }

        private void DrawHeader() {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 18;
            headerStyle.normal.textColor = new Color(0, 0.95f, 1f);
            GUILayout.Label("UNITY-MCP PRO", headerStyle);
            GUILayout.Label(isRunning ? "STATUS: LINK_ESTABLISHED" : "STATUS: DISCONNECTED", EditorStyles.miniLabel);
        }

        private void DrawStatusTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Bridge Status Control", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Listener Port", port);
            EditorGUILayout.LabelField("API URL", $"http://localhost:{port}/");
            
            EditorGUILayout.Space(8);
            
            if (GUILayout.Button(isRunning ? "OFFLINE LINK (STOP)" : "ESTABLISH LINK (START)", isRunning ? GetButtonStyle("#ff007f") : GetButtonStyle("#00f2ff"), GUILayout.Height(35)))
            {
                if (isRunning) StopServer();
                else StartServer();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawToolsTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label($"Active Reflective Tools ({registeredTools.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var tool in registeredTools.Keys.OrderBy(k => k))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("•", GUILayout.Width(15));
                GUILayout.Label(tool, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawLogsTab()
        {
            GUILayout.Label("Bridge Diagnostics Terminal Output:", EditorStyles.miniBoldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));
            var logStyle = new GUIStyle(EditorStyles.textArea);
            logStyle.normal.textColor = isRunning ? Color.cyan : Color.gray;
            logStyle.fontSize = 11;
            EditorGUILayout.TextArea(log, logStyle, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Clear Logs", GUILayout.Width(90))) {
                log = "SYSTEM LOGS CLEARED.\n";
            }
        }

        private void StartServer()
        {
            listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();
            isRunning = true;
            log = "[SYSTEM] Link online. Waiting for neural commands...\n";
            Task.Run(() => ListenLoop());
        }

        private void StopServer() { isRunning = false; listener?.Stop(); listener?.Close(); log += "[SYSTEM] Link severed.\n"; }

        private async void ListenLoop()
        {
            while (isRunning)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    using (var reader = new StreamReader(context.Request.InputStream))
                    {
                        string json = await reader.ReadToEndAsync();
                        EditorApplication.delayCall += () => ProcessCommand(json, context);
                    }
                }
                catch { }
            }
        }

        private void ProcessCommand(string json, HttpListenerContext context)
        {
            string responseData = "";
            try
            {
                var cmd = JsonUtility.FromJson<AICommand>(json);
                log += $"[EXEC] {cmd.command} | {DateTime.Now:HH:mm:ss}\n";

                if (registeredTools.TryGetValue(cmd.command, out MethodInfo method))
                {
                    responseData = (string)method.Invoke(this, new object[] { cmd });
                }
                else
                {
                    responseData = "{\"status\":\"error\", \"message\":\"Unknown command: " + cmd.command + "\"}";
                }
            }
            catch (Exception e)
            {
                string msg = e.InnerException != null ? e.InnerException.Message : e.Message;
                responseData = "{\"status\":\"error\", \"message\":\"" + msg + "\"}";
                log += $"[ERR] {msg}\n";
            }

            try
            {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseData);
                context.Response.ContentType = "application/json";
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.Close();
            }
            catch (Exception e)
            {
                log += $"[ERR Send] {e.Message}\n";
            }
            Repaint();
        }

        // --- Core Commands ---

        [MCPTool("GET_SCENE_GRAPH")]
        private string GetSceneGraph(AICommand cmd) {
            var objects = GameObject.FindObjectsOfType<GameObject>();
            var nodes = objects.Select(o => $"{{\"name\":\"{o.name}\", \"tags\":\"{o.tag}\", \"active\":{o.activeSelf.ToString().ToLower()}}}");
            return $"{{\"status\":\"success\", \"graph\":[{string.Join(",", nodes)}]}}";
        }

        [MCPTool("TAKE_SCREENSHOT")]
        private string TakeScreenshot(AICommand cmd) {
            Texture2D tex = null;
            try {
                if (SceneView.lastActiveSceneView != null) {
                    var sv = SceneView.lastActiveSceneView;
                    var cam = sv.camera;
                    if (cam != null) {
                        int width = 800;
                        int height = 600;
                        
                        var rt = new RenderTexture(width, height, 24);
                        var prevTarget = cam.targetTexture;
                        cam.targetTexture = rt;
                        cam.Render();
                        cam.targetTexture = prevTarget;
                        
                        RenderTexture.active = rt;
                        tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        tex.Apply();
                        RenderTexture.active = null;
                        
                        DestroyImmediate(rt);
                    }
                }
            } catch (Exception ex) {
                log += $"[WARN] Camera capture failed, falling back: {ex.Message}\n";
            }

            if (tex == null) {
                string path = "AI_View.png";
                ScreenCapture.CaptureScreenshot(path);
                return $"{{\"status\":\"success\", \"path\":\"{Path.GetFullPath(path)}\"}}";
            } else {
                byte[] bytes = tex.EncodeToPNG();
                DestroyImmediate(tex);
                string base64 = Convert.ToBase64String(bytes);
                return $"{{\"status\":\"success\", \"image\":\"{base64}\"}}";
            }
        }

        [MCPTool("CREATE_OBJECT")]
        private string CreateObject(AICommand cmd) {
            var go = GameObject.CreatePrimitive((PrimitiveType)Enum.Parse(typeof(PrimitiveType), cmd.type, true));
            go.name = string.IsNullOrEmpty(cmd.name) ? "AI_Object" : cmd.name;
            if (cmd.position != null && cmd.position.Length == 3) 
                go.transform.position = new Vector3(cmd.position[0], cmd.position[1], cmd.position[2]);
            Undo.RegisterCreatedObjectUndo(go, $"Create {go.name}");
            return "{\"status\":\"success\", \"name\":\"" + go.name + "\"}";
        }

        [MCPTool("SET_TRANSFORM")]
        private string SetTransform(AICommand cmd) {
            var target = GameObject.Find(cmd.name);
            if (target) {
                Undo.RecordObject(target.transform, "Set Transform");
                if (cmd.position != null && cmd.position.Length == 3) target.transform.position = new Vector3(cmd.position[0], cmd.position[1], cmd.position[2]);
                if (cmd.rotation != null && cmd.rotation.Length == 3) target.transform.eulerAngles = new Vector3(cmd.rotation[0], cmd.rotation[1], cmd.rotation[2]);
                if (cmd.scale != null && cmd.scale.Length == 3) target.transform.localScale = new Vector3(cmd.scale[0], cmd.scale[1], cmd.scale[2]);
                return "{\"status\":\"success\"}";
            } else return "{\"status\":\"error\", \"message\":\"Not found\"}";
        }

        [MCPTool("SET_MATERIAL")]
        private string SetMaterial(AICommand cmd) {
            var mObj = GameObject.Find(cmd.name);
            var rend = mObj?.GetComponent<Renderer>();
            if (rend) {
                Undo.RecordObject(rend.sharedMaterial, "Set Material");
                if (cmd.color != null && cmd.color.Length >= 3) 
                    rend.sharedMaterial.color = new Color(cmd.color[0], cmd.color[1], cmd.color[2], cmd.color.Length > 3 ? cmd.color[3] : 1f);
                if (rend.sharedMaterial.HasProperty("_Glossiness")) {
                    rend.sharedMaterial.SetFloat("_Glossiness", cmd.value);
                }
                return "{\"status\":\"success\"}";
            } else return "{\"status\":\"error\", \"message\":\"Renderer not found\"}";
        }

        [MCPTool("CREATE_UI")]
        private string CreateUI(AICommand cmd) {
            var canvas = GameObject.Find("Canvas") ?? new GameObject("Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            
            var ui = new GameObject(cmd.name ?? "AI_UI", typeof(RectTransform));
            ui.transform.SetParent(canvas.transform, false);
            
            if (cmd.type.ToLower() == "text") {
                var t = ui.AddComponent<UnityEngine.UI.Text>();
                t.text = cmd.text ?? "AI DATA";
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.color = Color.white;
            } else if (cmd.type.ToLower() == "button") {
                ui.AddComponent<UnityEngine.UI.Image>();
                ui.AddComponent<UnityEngine.UI.Button>();
                
                var textObj = new GameObject("Text", typeof(RectTransform));
                textObj.transform.SetParent(ui.transform, false);
                var t = textObj.AddComponent<UnityEngine.UI.Text>();
                t.text = cmd.text ?? "Button";
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.color = Color.black;
                t.alignment = TextAnchor.MiddleCenter;
            } else if (cmd.type.ToLower() == "image") {
                ui.AddComponent<UnityEngine.UI.Image>();
            }

            if (cmd.position != null && cmd.position.Length == 2) {
                var rt = ui.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(cmd.position[0], cmd.position[1]);
            }

            Undo.RegisterCreatedObjectUndo(ui, $"Create UI {ui.name}");
            return "{\"status\":\"success\"}";
        }

        [MCPTool("ADD_COMPONENT")]
        private string AddComponent(AICommand cmd) {
            var target = GameObject.Find(cmd.name);
            if (target == null) return "{\"status\":\"error\", \"message\":\"Object not found\"}";
            
            Type type = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                type = assembly.GetType(cmd.type) ?? assembly.GetType("UnityEngine." + cmd.type);
                if (type != null) break;
            }

            if (type == null) {
                return "{\"status\":\"error\", \"message\":\"Component type not found: " + cmd.type + "\"}";
            }

            var comp = target.GetComponent(type) ?? target.AddComponent(type);
            Undo.RegisterCreatedObjectUndo(comp, $"Add component {cmd.type}");
            return "{\"status\":\"success\", \"message\":\"Component added successfully\"}";
        }

        [MCPTool("DESTROY_OBJECT")]
        private string DestroyObject(AICommand cmd) {
            var target = GameObject.Find(cmd.name);
            if (target) {
                Undo.DestroyObjectImmediate(target);
                return "{\"status\":\"success\"}";
            }
            return "{\"status\":\"error\", \"message\":\"Object not found\"}";
        }

        [MCPTool("PARENT_OBJECT")]
        private string ParentObject(AICommand cmd) {
            var child = GameObject.Find(cmd.name);
            if (child == null) return "{\"status\":\"error\", \"message\":\"Child object not found\"}";
            
            if (string.IsNullOrEmpty(cmd.parent)) {
                Undo.SetTransformParent(child.transform, null, "Unparent Object");
                return "{\"status\":\"success\"}";
            }

            var parent = GameObject.Find(cmd.parent);
            if (parent == null) return "{\"status\":\"error\", \"message\":\"Parent object not found\"}";

            Undo.SetTransformParent(child.transform, parent.transform, "Parent Object");
            return "{\"status\":\"success\"}";
        }

        [MCPTool("SET_TEXT")]
        private string SetText(AICommand cmd) {
            var target = GameObject.Find(cmd.name);
            if (target == null) return "{\"status\":\"error\", \"message\":\"Object not found\"}";

            var textComponent = target.GetComponent<UnityEngine.UI.Text>();
            if (textComponent != null) {
                Undo.RecordObject(textComponent, "Set Text");
                textComponent.text = cmd.text;
                return "{\"status\":\"success\"}";
            }

            var textMesh = target.GetComponent<TextMesh>();
            if (textMesh != null) {
                Undo.RecordObject(textMesh, "Set TextMesh text");
                textMesh.text = cmd.text;
                return "{\"status\":\"success\"}";
            }

            foreach (var comp in target.GetComponents<Component>()) {
                if (comp != null && comp.GetType().Name.Contains("TextMeshPro")) {
                    var textProp = comp.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
                    if (textProp != null && textProp.CanWrite) {
                        Undo.RecordObject(comp, "Set TMP Text");
                        textProp.SetValue(comp, cmd.text, null);
                        return "{\"status\":\"success\"}";
                    }
                }
            }

            return "{\"status\":\"error\", \"message\":\"No UI Text, TextMesh, or TMP component found on " + cmd.name + "\"}";
        }

        [MCPTool("LIST_SCENE")]
        private string ListScene(AICommand cmd) {
            var objects = GameObject.FindObjectsOfType<GameObject>();
            var names = objects.Select(o => o.name).ToList();
            var wrapper = new StringListWrapper { list = names };
            return "{\"status\":\"success\", \"objects\":" + JsonUtility.ToJson(wrapper) + "}";
        }

        [MCPTool("SET_PROPERTY")]
        private string SetProperty(AICommand cmd) {
            var target = GameObject.Find(cmd.name);
            if (target == null) return "{\"status\":\"error\", \"message\":\"Object not found\"}";

            Component comp = null;
            if (!string.IsNullOrEmpty(cmd.component)) {
                comp = target.GetComponent(cmd.component);
                if (comp == null) {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                        var type = assembly.GetType(cmd.component) ?? assembly.GetType("UnityEngine." + cmd.component);
                        if (type != null) {
                            comp = target.GetComponent(type);
                            if (comp != null) break;
                        }
                    }
                }
            }

            if (comp == null) {
                return "{\"status\":\"error\", \"message\":\"Component " + cmd.component + " not found on object " + cmd.name + "\"}";
            }

            Undo.RecordObject(comp, $"Set Property {cmd.property}");
            var typeInfo = comp.GetType();
            
            var field = typeInfo.GetField(cmd.property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (field != null) {
                try {
                    object convertedValue = Convert.ChangeType(cmd.value, field.FieldType);
                    field.SetValue(comp, convertedValue);
                    return "{\"status\":\"success\", \"message\":\"Field " + cmd.property + " set to " + convertedValue + "\"}";
                } catch (Exception e) {
                    return "{\"status\":\"error\", \"message\":\"Failed to set field: " + e.Message + "\"}";
                }
            }

            var prop = typeInfo.GetProperty(cmd.property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null && prop.CanWrite) {
                try {
                    object convertedValue = Convert.ChangeType(cmd.value, prop.PropertyType);
                    prop.SetValue(comp, convertedValue, null);
                    return "{\"status\":\"success\", \"message\":\"Property " + cmd.property + " set to " + convertedValue + "\"}";
                } catch (Exception e) {
                    return "{\"status\":\"error\", \"message\":\"Failed to set property: " + e.Message + "\"}";
                }
            }

            return "{\"status\":\"error\", \"message\":\"Property or Field " + cmd.property + " not found on component " + cmd.component + "\"}";
        }

        // --- Deep Reflection / Inspection Commands ---

        [MCPTool("INSPECT_GAMEOBJECT")]
        private string InspectGameObject(AICommand cmd) {
            var target = GameObject.Find(cmd.name);
            if (target == null) return "{\"status\":\"error\", \"message\":\"Object not found\"}";

            var components = target.GetComponents<Component>();
            var list = components.Where(c => c != null).Select(c => c.GetType().Name).ToList();
            var wrapper = new StringListWrapper { list = list };
            return "{\"status\":\"success\", \"components\":" + JsonUtility.ToJson(wrapper) + "}";
        }

        [MCPTool("GET_COMPONENT_PROPERTIES")]
        private string GetComponentProperties(AICommand cmd) {
            var target = GameObject.Find(cmd.name);
            if (target == null) return "{\"status\":\"error\", \"message\":\"Object not found\"}";

            Component comp = target.GetComponent(cmd.component);
            if (comp == null) {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    var type = assembly.GetType(cmd.component) ?? assembly.GetType("UnityEngine." + cmd.component);
                    if (type != null) {
                        comp = target.GetComponent(type);
                        if (comp != null) break;
                    }
                }
            }

            if (comp == null) return "{\"status\":\"error\", \"message\":\"Component not found\"}";

            var propertiesList = new List<string>();
            var typeInfo = comp.GetType();

            foreach (var f in typeInfo.GetFields(BindingFlags.Public | BindingFlags.Instance)) {
                var val = f.GetValue(comp);
                var valStr = val != null ? val.ToString() : "null";
                propertiesList.Add($"{{\"name\":\"{f.Name}\",\"type\":\"{f.FieldType.Name}\",\"value\":\"{EscapeJson(valStr)}\",\"isField\":true}}");
            }

            foreach (var p in typeInfo.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                if (p.CanRead) {
                    try {
                        var val = p.GetValue(comp, null);
                        var valStr = val != null ? val.ToString() : "null";
                        propertiesList.Add($"{{\"name\":\"{p.Name}\",\"type\":\"{p.PropertyType.Name}\",\"value\":\"{EscapeJson(valStr)}\",\"isField\":false}}");
                    } catch {}
                }
            }

            return $"{{\"status\":\"success\",\"properties\":[{string.Join(",", propertiesList)}]}}";
        }

        private string EscapeJson(string s) {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        // --- Project & Scene Operations ---

        [MCPTool("INSTANTIATE_PREFAB")]
        private string InstantiatePrefab(AICommand cmd) {
            if (string.IsNullOrEmpty(cmd.prefabPath)) return "{\"status\":\"error\", \"message\":\"prefabPath is empty\"}";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(cmd.prefabPath);
            if (prefab == null) return "{\"status\":\"error\", \"message\":\"Prefab not found at path: " + cmd.prefabPath + "\"}";

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (!string.IsNullOrEmpty(cmd.name)) go.name = cmd.name;
            if (cmd.position != null && cmd.position.Length == 3) {
                go.transform.position = new Vector3(cmd.position[0], cmd.position[1], cmd.position[2]);
            }
            Undo.RegisterCreatedObjectUndo(go, $"Instantiate Prefab {go.name}");
            return "{\"status\":\"success\", \"name\":\"" + go.name + "\"}";
        }

        [MCPTool("SAVE_SCENE")]
        private string SaveScene(AICommand cmd) {
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            bool success = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
            return success ? "{\"status\":\"success\", \"message\":\"Scene saved successfully\"}" : "{\"status\":\"error\", \"message\":\"Failed to save scene\"}";
        }

        [MCPTool("LOAD_SCENE")]
        private string LoadScene(AICommand cmd) {
            if (string.IsNullOrEmpty(cmd.scenePath)) return "{\"status\":\"error\", \"message\":\"scenePath is empty\"}";
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(cmd.scenePath);
            return scene.IsValid() ? "{\"status\":\"success\", \"message\":\"Loaded scene " + cmd.scenePath + "\"}" : "{\"status\":\"error\", \"message\":\"Failed to load scene\"}";
        }

        // --- C# Code Generation & Compiler ---

        [MCPTool("COMPILE_CSHARP_SCRIPT")]
        private string CompileCSharpScript(AICommand cmd) {
            if (string.IsNullOrEmpty(cmd.className) || string.IsNullOrEmpty(cmd.code)) {
                return "{\"status\":\"error\", \"message\":\"className or code is empty\"}";
            }

            string folder = string.IsNullOrEmpty(cmd.targetFolder) ? "Assets/Scripts/AI" : cmd.targetFolder;
            if (!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }

            string filePath = Path.Combine(folder, cmd.className + ".cs");
            try {
                File.WriteAllText(filePath, cmd.code);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                return "{\"status\":\"success\", \"message\":\"Script written to " + filePath + " and compilation triggered.\"}";
            } catch (Exception e) {
                return "{\"status\":\"error\", \"message\":\"Failed to write script: " + e.Message + "\"}";
            }
        }

        // --- Game Master Commands ---

        [Serializable]
        public class GameStateResponse {
            public string status = "success";
            public string world_time;
            public PlayerState player;
        }
        [Serializable]
        public class PlayerState {
            public int health = 100;
            public string location;
        }

        [MCPTool("GET_STATE")]
        private string GetGameState(AICommand cmd)
        {
            var res = new GameStateResponse {
                world_time = DateTime.Now.ToString("HH:mm"),
                player = new PlayerState {
                    health = 100,
                    location = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot.ToString() : "Origin"
                }
            };
            return JsonUtility.ToJson(res);
        }

        [MCPTool("GET_PRESETS")]
        private string GetPresets(AICommand cmd)
        {
            return LoadPresetsRaw();
        }

        private string LoadPresetsRaw() {
            TextAsset asset = Resources.Load<TextAsset>("GMPresets");
            return asset != null ? asset.text : "{\"presets\":[]}";
        }

        [MCPTool("SPAWN_ENTITY")]
        private string SpawnEntity(AICommand cmd)
        {
            GameObject entity = null;
            Preset foundPreset = null;

            if (!string.IsNullOrEmpty(cmd.preset)) {
                var raw = LoadPresetsRaw();
                var list = JsonUtility.FromJson<PresetList>(raw);
                if (list != null && list.presets != null)
                {
                    foundPreset = list.presets.FirstOrDefault(p => p.id == cmd.preset);
                }
            }

            entity = GameObject.CreatePrimitive(cmd.type == "Monster" ? PrimitiveType.Capsule : PrimitiveType.Cube);
            entity.name = string.IsNullOrEmpty(cmd.name) ? (foundPreset != null ? foundPreset.displayName : "GM_" + cmd.type) : cmd.name;
            
            if (cmd.position != null && cmd.position.Length == 3)
                entity.transform.position = new Vector3(cmd.position[0], cmd.position[1], cmd.position[2]);
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
        private string BroadcastMessage(AICommand cmd) {
            log += $"[BROADCAST] ({cmd.style}): {cmd.message}\n";
            return "{\"status\":\"success\"}";
        }

        [MCPTool("TRIGGER_EVENT")]
        private string TriggerEvent(AICommand cmd)
        {
            log += $"[EVENT] {cmd.event_name} (Intensity: {cmd.intensity})\n";
            return "{\"status\":\"success\"}";
        }

        [MCPTool("SET_DIALOGUE")]
        private string SetDialogue(AICommand cmd)
        {
            var target = GameObject.Find(cmd.target_name);
            if (target == null) return "{\"status\":\"error\", \"message\":\"NPC not found\"}";
            
            Undo.RecordObject(target, "Set Dialogue");
            log += $"[DIALOGUE] {cmd.target_name}: \"{cmd.text}\" ({cmd.mood})\n";
            return "{\"status\":\"success\"}";
        }

        [MCPTool("QUERY_NEARBY")]
        private string QueryNearby(AICommand cmd)
        {
            Vector3 center = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            float radius = cmd.radius > 0 ? cmd.radius : 20f;
            
            var objects = GameObject.FindObjectsOfType<GameObject>();
            var nearby = objects.Where(o => Vector3.Distance(o.transform.position, center) <= radius)
                                .Take(50)
                                .Select(o => o.name)
                                .ToList();

            return "{\"status\":\"success\", \"entities\":" + JsonUtility.ToJson(new StringListWrapper { list = nearby }) + "}";
        }

        [MCPTool("SEARCH_ASSETS")]
        private string SearchAssets(AICommand cmd)
        {
            if (string.IsNullOrEmpty(cmd.query)) return "{\"status\":\"error\", \"message\":\"Query cannot be empty\"}";
            
            string[] guids = AssetDatabase.FindAssets(cmd.query);
            var paths = guids.Select(g => AssetDatabase.GUIDToAssetPath(g)).Take(20).ToList();
            
            return "{\"status\":\"success\", \"assets\":" + JsonUtility.ToJson(new StringListWrapper { list = paths }) + "}";
        }

        // --- Data Classes ---

        [Serializable]
        public class AICommand {
            public string command, type, name, text;
            public float[] position, rotation, scale, color;
            public string component;
            public string property;
            public float value;
            public string parent;
            public string prefabPath;
            public string scenePath;
            public string code;
            public string className;
            public string targetFolder;
            public string message;
            public string style;
            public string event_name;
            public float intensity;
            public string target_name;
            public string mood;
            public float radius;
            public string query;
            public string preset;
        }

        [Serializable] public class PresetList { public Preset[] presets; }
        [Serializable] public class Preset {
            public string id; public string displayName; public string type;
            public float scale; public float[] color; public string[] components;
        }

        [Serializable]
        public class StringListWrapper {
            public List<string> list;
        }
    }
}
