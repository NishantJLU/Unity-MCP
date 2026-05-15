using UnityEngine;
using UnityEditor;
using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace UnityMCP
{
    public class AIBridgeEditorWindow : EditorWindow
    {
        private static HttpListener listener;
        private static bool isRunning = false;
        private static readonly string port = "60432";
        private string log = "Ready to connect...";

        [MenuItem("Window/AI/Unity MCP Bridge")]
        public static void ShowWindow()
        {
            GetWindow<AIBridgeEditorWindow>("AI Bridge");
        }

        private void OnGUI()
        {
            GUILayout.Label("Unity AI Bridge Status", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(isRunning ? $"Listening on http://localhost:{port}/" : "Server Offline", 
                isRunning ? MessageType.Info : MessageType.Warning);

            if (GUILayout.Button(isRunning ? "Start Bridge" : "Stop Bridge"))
            {
                if (isRunning) StopServer();
                else StartServer();
            }

            GUILayout.Space(10);
            GUILayout.Label("Log:");
            EditorGUILayout.TextArea(log, GUILayout.Height(200));
        }

        private void StartServer()
        {
            listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();
            isRunning = true;
            log = "Server started. Waiting for AI commands...\n";
            
            Task.Run(() => ListenLoop());
        }

        private void StopServer()
        {
            isRunning = false;
            try {
                listener?.Stop();
                listener?.Close();
            } catch {}
            log += "Server stopped.\n";
        }

        private async void ListenLoop()
        {
            while (isRunning)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    var request = context.Request;
                    
                    if (request.HttpMethod == "POST")
                    {
                        using (var reader = new StreamReader(request.InputStream))
                        {
                            string json = await reader.ReadToEndAsync();
                            // Dispatch to main thread for Unity API calls
                            EditorApplication.delayCall += () => ProcessCommand(json, context);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (isRunning) Debug.LogWarning($"Bridge Error: {e.Message}");
                }
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
                    case "CREATE_UI":
                        GameObject canvasGO = GameObject.Find("Canvas");
                        if (canvasGO == null) {
                            canvasGO = new GameObject("Canvas");
                            canvasGO.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                            if (GameObject.Find("EventSystem") == null) {
                                var es = new GameObject("EventSystem");
                                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                            }
                        }

                        GameObject uiGO = new GameObject(string.IsNullOrEmpty(cmd.name) ? "AI_UI_" + cmd.type : cmd.name);
                        uiGO.transform.SetParent(canvasGO.transform, false);
                        var rt = uiGO.AddComponent<RectTransform>();
                        
                        switch(cmd.type.ToUpper()) {
                            case "TEXT":
                                var txt = uiGO.AddComponent<UnityEngine.UI.Text>();
                                txt.text = cmd.text ?? "New AI Text";
                                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                                txt.alignment = TextAnchor.MiddleCenter;
                                txt.color = Color.black;
                                break;
                            case "IMAGE":
                                uiGO.AddComponent<UnityEngine.UI.Image>();
                                break;
                            case "BUTTON":
                                uiGO.AddComponent<UnityEngine.UI.Image>();
                                uiGO.AddComponent<UnityEngine.UI.Button>();
                                var btnText = new GameObject("Text");
                                btnText.transform.SetParent(uiGO.transform, false);
                                var btxt = btnText.AddComponent<UnityEngine.UI.Text>();
                                btxt.text = "Button";
                                btxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                                btxt.alignment = TextAnchor.MiddleCenter;
                                btxt.color = Color.black;
                                btxt.rectTransform.sizeDelta = new Vector2(160, 30);
                                break;
                        }
                        
                        if (cmd.position != null && cmd.position.Length >= 2)
                            rt.anchoredPosition = new Vector2(cmd.position[0], cmd.position[1]);
                        
                        responseData = "{\"status\":\"success\", \"name\":\"" + uiGO.name + "\"}";
                        break;

                    case "SET_TEXT":
                        var textObj = GameObject.Find(cmd.name);
                        if (textObj != null) {
                            var t = textObj.GetComponent<UnityEngine.UI.Text>();
                            if (t != null) {
                                t.text = cmd.text;
                                responseData = "{\"status\":\"success\"}";
                            } else responseData = "{\"status\":\"error\", \"message\":\"No Text component\"}";
                        } else responseData = "{\"status\":\"error\", \"message\":\"Object not found\"}";
                        break;

                    case "SET_MATERIAL":
                        var targetMat = GameObject.Find(cmd.name);
                        if (targetMat != null) {
                            var renderer = targetMat.GetComponent<Renderer>();
                            if (renderer != null) {
                                if (cmd.color != null && cmd.color.Length >= 3) {
                                    float r = cmd.color[0], g = cmd.color[1], b = cmd.color[2];
                                    float a = cmd.color.Length > 3 ? cmd.color[3] : 1f;
                                    renderer.sharedMaterial.color = new Color(r, g, b, a);
                                }
                                if (cmd.value != 0) renderer.sharedMaterial.SetFloat("_Glossiness", cmd.value);
                                responseData = "{\"status\":\"success\"}";
                            } else responseData = "{\"status\":\"error\", \"message\":\"No Renderer found\"}";
                        } else responseData = "{\"status\":\"error\", \"message\":\"Object not found\"}";
                        break;

                    case "ADD_COMPONENT":
                        var targetComp = GameObject.Find(cmd.name);
                        if (targetComp != null) {
                            var comp = targetComp.AddComponent(Type.GetType(cmd.type + ", UnityEngine"));
                            if (comp == null) comp = targetComp.AddComponent(Type.GetType(cmd.type + ", UnityEngine.UI"));
                            responseData = comp != null ? "{\"status\":\"success\"}" : "{\"status\":\"error\", \"message\":\"Component type not found\"}";
                        } else responseData = "{\"status\":\"error\", \"message\":\"Object not found\"}";
                        break;

                    case "SET_PROPERTY":
                        var targetProp = GameObject.Find(cmd.name);
                        if (targetProp != null) {
                            var component = targetProp.GetComponent(cmd.component);
                            if (component != null) {
                                var prop = component.GetType().GetProperty(cmd.property);
                                if (prop != null) {
                                    object val = cmd.value;
                                    if (prop.PropertyType == typeof(float)) val = Convert.ToSingle(cmd.value);
                                    if (prop.PropertyType == typeof(int)) val = Convert.ToInt32(cmd.value);
                                    prop.SetValue(component, val);
                                    responseData = "{\"status\":\"success\"}";
                                } else responseData = "{\"status\":\"error\", \"message\":\"Property not found\"}";
                            } else responseData = "{\"status\":\"error\", \"message\":\"Component not found\"}";
                        } else responseData = "{\"status\":\"error\", \"message\":\"Object not found\"}";
                        break;

                    case "PARENT_OBJECT":
                        var child = GameObject.Find(cmd.name);
                        var parent = GameObject.Find(cmd.parent);
                        if (child != null && parent != null) {
                            child.transform.SetParent(parent.transform);
                            responseData = "{\"status\":\"success\"}";
                        } else responseData = "{\"status\":\"error\", \"message\":\"Child or Parent not found\"}";
                        break;

                    case "DESTROY_OBJECT":
                        var victim = GameObject.Find(cmd.name);
                        if (victim != null) {
                            DestroyImmediate(victim);
                            responseData = "{\"status\":\"success\"}";
                        } else responseData = "{\"status\":\"error\", \"message\":\"Object not found\"}";
                        break;

                    case "LIST_SCENE":
                        var objects = GameObject.FindObjectsOfType<GameObject>();
                        var names = new List<string>();
                        foreach (var obj in objects) names.Add(obj.name);
                        responseData = "{\"status\":\"success\", \"objects\":[\"" + string.Join("\",\"", names) + "\"]}";
                        break;

                    case "SET_TRANSFORM":
                        var target = GameObject.Find(cmd.name);
                        if (target != null) {
                             if (cmd.position != null && cmd.position.Length == 3)
                                target.transform.position = new Vector3(cmd.position[0], cmd.position[1], cmd.position[2]);
                             if (cmd.rotation != null && cmd.rotation.Length == 3)
                                target.transform.eulerAngles = new Vector3(cmd.rotation[0], cmd.rotation[1], cmd.rotation[2]);
                             if (cmd.scale != null && cmd.scale.Length == 3)
                                target.transform.localScale = new Vector3(cmd.scale[0], cmd.scale[1], cmd.scale[2]);
                             responseData = "{\"status\":\"success\"}";
                        } else {
                             responseData = "{\"status\":\"error\", \"message\":\"Object not found\"}";
                        }
                        break;

                    default:
                        responseData = "{\"status\":\"error\", \"message\":\"Unknown command\"}";
                        break;
                }

                log += $"Executed: {cmd.command} at {DateTime.Now:HH:mm:ss}\n";
                Repaint();

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseData);
                context.Response.ContentLength64 = buffer.Length;
                context.Response.ContentType = "application/json";
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.Close();
            }
            catch (Exception e)
            {
                log += $"Error executing command: {e.Message}\n";
                try {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                } catch {}
            }
        }

        [Serializable]
        public class AICommand
        {
            public string command;
            public string type;
            public string name;
            public string parent;
            public string component;
            public string property;
            public string text;
            public float value;
            public float[] position;
            public float[] rotation;
            public float[] scale;
            public float[] color;
        }
    }
}
