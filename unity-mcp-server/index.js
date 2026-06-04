const { Server } = require("@modelcontextprotocol/sdk/server/index.js");
const { StdioServerTransport } = require("@modelcontextprotocol/sdk/server/stdio.js");
const { CallToolRequestSchema, ListToolsRequestSchema } = require("@modelcontextprotocol/sdk/types.js");
const axios = require("axios");

const UNITY_URL = "http://localhost:60432/";

const server = new Server(
  {
    name: "unity-mcp-server-pro",
    version: "2.5.0",
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

const tools = [
  // --- Core / Basic Editor Automation ---
  {
    name: "create_unity_object",
    description: "Create a primitive object (Cube, Sphere, etc.) at a specific location.",
    inputSchema: {
      type: "object",
      properties: {
        type: { type: "string", enum: ["Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad"] },
        name: { type: "string" },
        x: { type: "number" }, y: { type: "number" }, z: { type: "number" }
      },
      required: ["type"]
    }
  },
  {
    name: "update_unity_transform",
    description: "Modify position, rotation, or scale of an existing object.",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string" },
        px: { type: "number" }, py: { type: "number" }, pz: { type: "number" },
        rx: { type: "number" }, ry: { type: "number" }, rz: { type: "number" },
        sx: { type: "number" }, sy: { type: "number" }, sz: { type: "number" }
      },
      required: ["name"]
    }
  },
  {
    name: "set_unity_material",
    description: "Change color (0-1 range) and smoothness of an object's material.",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string" },
        r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" },
        smoothness: { type: "number", description: "0 to 1" }
      },
      required: ["name"]
    }
  },
  {
    name: "add_unity_component",
    description: "Inject a component (Rigidbody, Light, MeshCollider, etc.) into an object.",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string" },
        componentType: { type: "string", description: "e.g., 'Rigidbody', 'Light', 'BoxCollider'" }
      },
      required: ["name", "componentType"]
    }
  },
  {
    name: "set_unity_property",
    description: "Generic tool to set a public field or property on a component using reflection.",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string" },
        component: { type: "string", description: "e.g., 'Light'" },
        property: { type: "string", description: "e.g., 'intensity' or 'range'" },
        value: { type: "number" }
      },
      required: ["name", "component", "property", "value"]
    }
  },
  {
    name: "parent_unity_object",
    description: "Attach one object to another in the hierarchy.",
    inputSchema: {
      type: "object",
      properties: {
        childName: { type: "string" },
        parentName: { type: "string", description: "Leave empty or omit to unparent" }
      },
      required: ["childName"]
    }
  },
  {
    name: "destroy_unity_object",
    description: "Remove an object from the scene forever.",
    inputSchema: {
      type: "object",
      properties: { name: { type: "string" } },
      required: ["name"]
    }
  },
  {
    name: "create_unity_ui",
    description: "Create a UI element (Text, Button, Image). Automatically creates a Canvas if none exists.",
    inputSchema: {
      type: "object",
      properties: {
        type: { type: "string", enum: ["Text", "Button", "Image"] },
        name: { type: "string" },
        text: { type: "string", description: "Content for Text or Button" },
        x: { type: "number", description: "Anchored X position" },
        y: { type: "number", description: "Anchored Y position" }
      },
      required: ["type"]
    }
  },
  {
    name: "set_unity_text",
    description: "Update the content of an existing UI Text or TextMesh component.",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Name of the UI object" },
        text: { type: "string" }
      },
      required: ["name", "text"]
    }
  },
  {
    name: "take_unity_screenshot",
    description: "Capture a visual snapshot of the Unity SceneView / Editor. Returns inline image data directly.",
    inputSchema: { type: "object", properties: {} }
  },
  {
    name: "get_unity_scene_graph",
    description: "Get a comprehensive structural map of the scene, including object names, tags, and active states.",
    inputSchema: { type: "object", properties: {} }
  },
  {
    name: "list_unity_scene",
    description: "Scan the current scene for all object names.",
    inputSchema: { type: "object", properties: {} }
  },

  // --- Deep Reflection / Inspection ---
  {
    name: "inspect_gameobject",
    description: "Inspects a GameObject to list all of its attached component types.",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Name of the GameObject" }
      },
      required: ["name"]
    }
  },
  {
    name: "get_component_properties",
    description: "Uses C# reflection to retrieve all public fields, properties, their types, and values from a component.",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "GameObject name" },
        component: { type: "string", description: "Component type name (e.g., 'Light')" }
      },
      required: ["name", "component"]
    }
  },

  // --- Project Operations & Scene Management ---
  {
    name: "instantiate_prefab",
    description: "Instantiates a prefab from the asset database at a specific coordinate.",
    inputSchema: {
      type: "object",
      properties: {
        prefabPath: { type: "string", description: "Asset path starting with 'Assets/' (e.g., 'Assets/Prefabs/Player.prefab')" },
        name: { type: "string", description: "Optional custom name for the instantiated object" },
        x: { type: "number" }, y: { type: "number" }, z: { type: "number" }
      },
      required: ["prefabPath"]
    }
  },
  {
    name: "save_scene",
    description: "Saves the current active scene in the editor.",
    inputSchema: { type: "object", properties: {} }
  },
  {
    name: "load_scene",
    description: "Opens an existing scene asset inside the editor.",
    inputSchema: {
      type: "object",
      properties: {
        scenePath: { type: "string", description: "Asset path starting with 'Assets/' (e.g., 'Assets/Scenes/Main.unity')" }
      },
      required: ["scenePath"]
    }
  },

  // --- C# Code Injector & Compiler ---
  {
    name: "compile_csharp_script",
    description: "Injects and compiles a C# class inheriting from MonoBehaviour on the fly in Unity.",
    inputSchema: {
      type: "object",
      properties: {
        className: { type: "string", description: "Exact name of the C# class (must match file name)" },
        code: { type: "string", description: "Complete C# class implementation" },
        targetFolder: { type: "string", description: "Folder path relative to project root, defaults to 'Assets/Scripts/AI'" }
      },
      required: ["className", "code"]
    }
  },

  // --- Game Master & Runtime Controls ---
  {
    name: "get_game_state",
    description: "Get the current state of the game world (player statistics, location, and world parameters).",
    inputSchema: { type: "object", properties: {} },
  },
  {
    name: "get_presets",
    description: "List available entity presets (visual or behavioral definitions) configured in Unity.",
    inputSchema: { type: "object", properties: {} },
  },
  {
    name: "spawn_entity",
    description: "Spawns an NPC, monster, or prop in the scene, applying visual/behavioral presets.",
    inputSchema: {
      type: "object",
      properties: {
        type: { type: "string", enum: ["NPC", "Monster", "Prop"] },
        name: { type: "string" },
        preset: { type: "string", description: "Preset ID (e.g., 'dark_knight')" },
        x: { type: "number" }, y: { type: "number" }, z: { type: "number" }
      },
      required: ["type", "name"],
    },
  },
  {
    name: "trigger_world_event",
    description: "Triggers a world event inside the project scene (e.g. boss battle, storm, dynamic trigger).",
    inputSchema: {
      type: "object",
      properties: {
        event_name: { type: "string" },
        intensity: { type: "number", minimum: 0, maximum: 1, description: "Intensity factor" }
      },
      required: ["event_name"],
    },
  },
  {
    name: "broadcast_narrative",
    description: "Broadcasts a text style narrative message to the Game logs.",
    inputSchema: {
      type: "object",
      properties: {
        message: { type: "string" },
        style: { type: "string", enum: ["Standard", "Warning", "Epic", "Whisper"] },
      },
      required: ["message"],
    },
  },
  {
    name: "set_npc_dialogue",
    description: "Assign dialogue and mood settings to an NPC in the scene.",
    inputSchema: {
      type: "object",
      properties: {
        target_name: { type: "string", description: "Name of the target NPC GameObject" },
        text: { type: "string", description: "Dialogue script line" },
        mood: { type: "string", enum: ["Neutral", "Angry", "Happy", "Mysterious"] },
      },
      required: ["target_name", "text"],
    },
  },
  {
    name: "query_nearby_entities",
    description: "Scans for all active GameObjects within a radial distance.",
    inputSchema: {
      type: "object",
      properties: {
        radius: { type: "number", default: 20 }
      },
    },
  },
  {
    name: "search_assets",
    description: "Searches Unity's AssetDatabase for matching assets.",
    inputSchema: {
      type: "object",
      properties: {
        query: { type: "string", description: "Asset search expression (e.g. 't:Prefab weapon')" }
      },
      required: ["query"]
    }
  }
];

server.setRequestHandler(ListToolsRequestSchema, async () => ({ tools }));

server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;
  try {
    let body = { command: "" };

    switch (name) {
      case "create_unity_object":
        body = {
          command: "CREATE_OBJECT",
          type: args.type,
          name: args.name,
          position: [args.x || 0, args.y || 0, args.z || 0]
        };
        break;
      case "update_unity_transform":
        body = {
          command: "SET_TRANSFORM",
          name: args.name,
          position: (args.px !== undefined) ? [args.px, args.py, args.pz] : null,
          rotation: (args.rx !== undefined) ? [args.rx, args.ry, args.rz] : null,
          scale: (args.sx !== undefined) ? [args.sx, args.sy, args.sz] : null
        };
        break;
      case "set_unity_material":
        body = {
          command: "SET_MATERIAL",
          name: args.name,
          color: (args.r !== undefined) ? [args.r, args.g, args.b, args.a ?? 1] : null,
          value: args.smoothness || 0
        };
        break;
      case "add_unity_component":
        body = { command: "ADD_COMPONENT", name: args.name, type: args.componentType };
        break;
      case "set_unity_property":
        body = {
          command: "SET_PROPERTY",
          name: args.name,
          component: args.component,
          property: args.property,
          value: args.value
        };
        break;
      case "parent_unity_object":
        body = { command: "PARENT_OBJECT", name: args.childName, parent: args.parentName };
        break;
      case "destroy_unity_object":
        body = { command: "DESTROY_OBJECT", name: args.name };
        break;
      case "create_unity_ui":
        body = {
          command: "CREATE_UI",
          type: args.type,
          name: args.name,
          text: args.text,
          position: [args.x || 0, args.y || 0]
        };
        break;
      case "set_unity_text":
        body = {
          command: "SET_TEXT",
          name: args.name,
          text: args.text
        };
        break;
      case "take_unity_screenshot":
        body = { command: "TAKE_SCREENSHOT" };
        break;
      case "get_unity_scene_graph":
        body = { command: "GET_SCENE_GRAPH" };
        break;
      case "list_unity_scene":
        body = { command: "LIST_SCENE" };
        break;

      // Deep Reflection
      case "inspect_gameobject":
        body = { command: "INSPECT_GAMEOBJECT", name: args.name };
        break;
      case "get_component_properties":
        body = { command: "GET_COMPONENT_PROPERTIES", name: args.name, component: args.component };
        break;

      // Project Operations
      case "instantiate_prefab":
        body = {
          command: "INSTANTIATE_PREFAB",
          prefabPath: args.prefabPath,
          name: args.name,
          position: [args.x || 0, args.y || 0, args.z || 0]
        };
        break;
      case "save_scene":
        body = { command: "SAVE_SCENE" };
        break;
      case "load_scene":
        body = { command: "LOAD_SCENE", scenePath: args.scenePath };
        break;

      // C# Script compilation
      case "compile_csharp_script":
        body = {
          command: "COMPILE_CSHARP_SCRIPT",
          className: args.className,
          code: args.code,
          targetFolder: args.targetFolder || "Assets/Scripts/AI"
        };
        break;

      // Game Master
      case "get_game_state":
        body = { command: "GET_STATE" };
        break;
      case "get_presets":
        body = { command: "GET_PRESETS" };
        break;
      case "spawn_entity":
        body = {
          command: "SPAWN_ENTITY",
          type: args.type,
          name: args.name,
          preset: args.preset,
          position: [args.x || 0, args.y || 0, args.z || 0]
        };
        break;
      case "trigger_world_event":
        body = { command: "TRIGGER_EVENT", event_name: args.event_name, intensity: args.intensity || 0.5 };
        break;
      case "broadcast_narrative":
        body = { command: "BROADCAST", message: args.message, style: args.style || "Standard" };
        break;
      case "set_npc_dialogue":
        body = { command: "SET_DIALOGUE", target_name: args.target_name, text: args.text, mood: args.mood || "Neutral" };
        break;
      case "query_nearby_entities":
        body = { command: "QUERY_NEARBY", radius: args.radius || 20 };
        break;
      case "search_assets":
        body = { command: "SEARCH_ASSETS", query: args.query };
        break;
    }

    const res = await axios.post(UNITY_URL, body);
    
    // If it's a screenshot, return as inline visual block
    if (res.data && res.data.status === "success" && res.data.image) {
      return {
        content: [
          {
            type: "image",
            data: res.data.image,
            mimeType: "image/png"
          }
        ]
      };
    }

    return { content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }] };
  } catch (err) {
    return { content: [{ type: "text", text: `Error: ${err.message}` }], isError: true };
  }
});

const transport = new StdioServerTransport();
server.connect(transport).then(() => console.error("Unity Pro Consolidated MCP Running"));
