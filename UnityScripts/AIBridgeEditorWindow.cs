using UnityEngine;
using UnityEditor;
using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace UnityMCP
{
    public class AIBridgeEditorWindow : EditorWindow
    {
        private static HttpListener listener;
        private static bool isRunning = false;
        private static readonly string port = "60432";
        private string log = "SYSTEM READY...";
        private Vector2 scrollPos;

        [MenuItem("Window/AI/Unity MCP Pro")]
        public static void ShowWindow()
        {
            GetWindow<AIBridgeEditorWindow>("AI Bridge Pro");
        }

        private void OnGUI()
        {
            DrawHeader();
            
            EditorGUILayout.Space(5);
            if (GUILayout.Button(isRunning ? "OFFLINE LINK" : "ESTABLISH LINK", isRunning ? GetButtonStyle("#ff007f") : GetButtonStyle("#00f2ff")))
            {
                if (isRunning) StopServer();
                else StartServer();
            }

            EditorGUILayout.Space(10);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
            var logStyle = new GUIStyle(EditorStyles.textArea);
            logStyle.normal.textColor = isRunning ? Color.cyan : Color.gray;
            logStyle.fontSize = 11;
            EditorGUILayout.LabelField(log, logStyle);
            EditorGUILayout.EndScrollView();
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
            try
            {
                var cmd = JsonUtility.FromJson<AICommand>(json);
                string responseData = "";

                switch (cmd.command)
                {
                    case "GET_SCENE_GRAPH":
                        responseData = GetSceneGraphJson();
                        break;
                    
                    case "TAKE_SCREENSHOT":
                        responseData = TakeScreenshot();
                        break;

                    case "CREATE_OBJECT":
                        var go = GameObject.CreatePrimitive((PrimitiveType)Enum.Parse(typeof(PrimitiveType), cmd.type, true));
                        go.name = string.IsNullOrEmpty(cmd.name) ? "AI_Object" : cmd.name;
                        if (cmd.position != null && cmd.position.Length == 3) 
                            go.transform.position = new Vector3(cmd.position[0], cmd.position[1], cmd.position[2]);
                        responseData = "{\"status\":\"success\", \"name\":\"" + go.name + "\"}";
                        break;

                    case "SET_TRANSFORM":
                        var target = GameObject.Find(cmd.name);
                        if (target) {
                            if (cmd.position != null && cmd.position.Length == 3) target.transform.position = new Vector3(cmd.position[0], cmd.position[1], cmd.position[2]);
                            if (cmd.rotation != null && cmd.rotation.Length == 3) target.transform.eulerAngles = new Vector3(cmd.rotation[0], cmd.rotation[1], cmd.rotation[2]);
                            if (cmd.scale != null && cmd.scale.Length == 3) target.transform.localScale = new Vector3(cmd.scale[0], cmd.scale[1], cmd.scale[2]);
                            responseData = "{\"status\":\"success\"}";
                        } else responseData = "{\"status\":\"error\", \"message\":\"Not found\"}";
                        break;

                    case "SET_MATERIAL":
                        var mObj = GameObject.Find(cmd.name);
                        var rend = mObj?.GetComponent<Renderer>();
                        if (rend) {
                            if (cmd.color != null && cmd.color.Length >= 3) 
                                rend.sharedMaterial.color = new Color(cmd.color[0], cmd.color[1], cmd.color[2], cmd.color.Length > 3 ? cmd.color[3] : 1f);
                            responseData = "{\"status\":\"success\"}";
                        } else responseData = "{\"status\":\"error\"}";
                        break;

                    case "CREATE_UI":
                        responseData = CreateUI(cmd);
                        break;

                    default:
                        responseData = "{\"status\":\"unknown\"}";
                        break;
                }

                log += $"[EXEC] {cmd.command} | {DateTime.Now:HH:mm:ss}\n";
                Repaint();

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseData);
                context.Response.ContentType = "application/json";
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.Close();
            }
            catch (Exception e) { log += $"[ERR] {e.Message}\n"; context.Response.Close(); }
        }

        private string GetSceneGraphJson() {
            var objects = GameObject.FindObjectsOfType<GameObject>();
            var nodes = objects.Select(o => $"{{\"name\":\"{o.name}\", \"tags\":\"{o.tag}\", \"active\":{o.activeSelf.ToString().ToLower()}}}");
            return $"{{\"status\":\"success\", \"graph\":[{string.Join(",", nodes)}]}}";
        }

        private string TakeScreenshot() {
            string path = "AI_View.png";
            ScreenCapture.CaptureScreenshot(path);
            return $"{{\"status\":\"success\", \"path\":\"{Path.GetFullPath(path)}\"}}";
        }

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
            }
            return "{\"status\":\"success\"}";
        }

        [Serializable]
        public class AICommand {
            public string command, type, name, text;
            public float[] position, rotation, scale, color;
        }
    }
}
