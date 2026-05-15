const { Server } = require("@modelcontextprotocol/sdk/server/index.js");
const { StdioServerTransport } = require("@modelcontextprotocol/sdk/server/stdio.js");
const { CallToolRequestSchema, ListToolsRequestSchema } = require("@modelcontextprotocol/sdk/types.js");
const axios = require("axios");

const UNITY_URL = "http://localhost:60432/";

const server = new Server(
  {
    name: "unity-mcp-server-pro",
    version: "2.0.0",
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

const tools = [
  {
    name: "create_unity_object",
    description: "Create a primitive (Cube, Sphere, etc.) at a specific location.",
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
    description: "Generic tool to set a public property on a component using reflection.",
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
        parentName: { type: "string" }
      },
      required: ["childName", "parentName"]
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
    description: "Update the content of an existing UI Text component.",
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
    name: "list_unity_scene",
    description: "Scan the current scene for all object names.",
    inputSchema: { type: "object", properties: {} }
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
      case "list_unity_scene":
        body = { command: "LIST_SCENE" };
        break;
    }

    const res = await axios.post(UNITY_URL, body);
    return { content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }] };
  } catch (err) {
    return { content: [{ type: "text", text: `Error: ${err.message}` }], isError: true };
  }
});

const transport = new StdioServerTransport();
server.connect(transport).then(() => console.error("Unity Pro MCP Running"));
