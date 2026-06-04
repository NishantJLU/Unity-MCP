# 🌌 Unity-MCP Pro: The Agentic Unity Bridge

![Node.js](https://img.shields.io/badge/node-%3E=18.x-brightgreen.svg)
![Build](https://img.shields.io/badge/build-passing-success)
![License](https://img.shields.io/badge/License-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-unity%20editor-white)

✨ Unity-MCP Pro is a high-performance Model Context Protocol (MCP) server for the Unity Editor that gives AI assistants and custom tools a reliable way to automate scenes, GameObjects, components, physics, and project workflows.

Created, maintained, and actively expanded by **NishantJLU**.

This repository is the primary home for the current Unity-MCP implementation, including the bridge workflow, automation surface, tooling, and ongoing product improvements.

## Table of Contents
- [Features](#-features)
  - [Scenes & Project](#-scenes--project)
  - [Hierarchy Management](#-hierarchy-management)
  - [Components & Properties](#-components--properties)
  - [AI Vision & Rendering](#-ai-vision--rendering)
  - [Productivity](#-productivity)
- [Setup Instructions](#-setup-instructions)
  - [Prerequisites](#-prerequisites)
  - [Installation](#-installation)
  - [Update MCP Config](#-update-mcp-config)
  - [Running the Server](#-running-the-server)
- [Usage Guide](#-usage-guide)
  - [Creating GameObjects](#-creating-gameobjects)
  - [Working with Components](#-working-with-components)
  - [AI Vision](#-ai-vision)
- [Available MCP Tools](#-available-mcp-tools)
- [For Developers](#-for-developers)
  - [Project Structure](#-project-structure)
  - [Building the Project](#-building-the-project)
  - [Tests](#-tests)
  - [Contributing](#-contributing)

## 📦 Features

### 🎥 Scenes & Project
- List and load scenes within the project
- Inspect scene settings and global configurations
- Open/save projects and manage asset database items

### 🧱 Hierarchy Management
- Create GameObjects, Prefabs, Cameras, and Lights
- Reorder, rename, delete, parent, and toggle active states
- Manage Tags, Layers, and static flags for complex scene organization

### 🌀 Components & Properties
- Add or remove components in real-time
- Read/write property values via **C# Reflection** (Deep Property Access)
- Inspect component metadata and state in the Inspector-equivalent view

### 🎨 AI Vision & Rendering
- Capture live screenshots from the Editor/Game view
- Adjust camera properties, FOV, and post-processing for visual reasoning
- Automated scene auditing through visual feedback

### 🧰 Productivity
- **Main-Thread Dispatcher:** Safely execute commands without crashing Unity
- **Scene Graph Analytics:** Structured JSON map of the entire object hierarchy
- Batch operations to minimize overhead and maximize execution speed

## ⚙️ Setup Instructions

### 🛠 Prerequisites
- Unity Editor (2021.3 LTS or later)
- Node.js (18+ recommended)
- npm or yarn package manager

### 📥 Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/NishantJLU/Unity-MCP.git
   cd Unity-MCP
   ```

2. **Install dependencies**
   ```bash
   npm install
   ```

3. **Build the project**
   ```bash
   npm run build
   ```

4. **Install the Unity Bridge**
   - Copy the `UnityScripts` folder into your Unity project's `Assets` directory.
   - This adds the custom Editor Window and bridge listener to your project.

### 🔧 Update MCP Config

Point your MCP client to the built server entry (`unity-mcp-server/index.js`):

**Windows:**
```json
{
  "mcpServers": {
    "UnityMCPPro": {
      "command": "node",
      "args": ["C:\\\\path\\\\to\\\\Unity-MCP\\\\unity-mcp-server\\\\index.js"]
    }
  }
}
```

## 👤 Authorship & Credits

- Primary author and maintainer of Unity-MCP Pro: **NishantJLU**
- This repository reflects original work across the MCP server, C# bridge dispatcher, documentation, and automation capabilities.

### ▶️ Running the Server

1. **Start the MCP server**
   ```bash
   npm start
   ```

2. **Open Unity**

3. **Open the Unity MCP Pro Panel**
   - In Unity, go to **Window > AI > Unity MCP Pro**
   - Click the **ESTABLISH LINK** button
   - The status should change to `LINK_ESTABLISHED`
   - The bridge communicates via a local high-performance HTTP listener.

## 🚀 Usage Guide

Once the server is running and the Unity MCP panel is active, AI assistants can send structured commands directly into your Unity Editor workflow.

### 📘 Creating GameObjects

You can create new objects with custom settings such as:
- Name
- Primitive Type (Cube, Sphere, etc.)
- Position, Rotation, Scale
- Tag and Layer
Example MCP tool usage:
```javascript
create-gameobject({
  name: "AI_Controlled_Cube",
  primitiveType: "Cube",
  position: { x: 0, y: 5, z: 0 },
  tag: "Untagged"
});
```

### ✍️ Working with Components

You can modify any component property via Reflection:

**Transform & Physics:**
- Adjust Rigidbody mass and drag
- Modify Transform properties precisely
- Toggle Gravity and Colliders

**Lights & Rendering:**
- Change Light intensity, color, and range
- Modify MeshRenderer materials and shaders

### 👁 AI Vision

You can automate visual workflows with:

**Screenshot Capture:**
- Take a high-resolution snapshot of the current view
- Allow the AI to "see" the scene layout for aesthetic adjustments
- Audit UI placement and visual bugs

## 🛠️ Available MCP Tools

Unity-MCP Pro exposes **26 consolidated tools** across Editor Automation, Game Master, Reflection Querying, and Script Generation:

### 🎥 Core & Editor Automation
- `create_unity_object`: Spawns 3D primitives (`Cube`, `Sphere`, `Capsule`, `Cylinder`, `Plane`, `Quad`).
- `update_unity_transform`: Modifies position, rotation, and scale of objects.
- `set_unity_material`: Sets material color (RGBA) and smoothness.
- `add_unity_component`: Dynamically appends any component to a GameObject.
- `set_unity_property`: Modifies fields and properties reflectively.
- `parent_unity_object`: Adjusts hierarchy parent/child relationships.
- `destroy_unity_object`: Permanent undoable destruction of target objects.
- `create_unity_ui`: Builds UI Canvas overlays containing buttons, text, and images.
- `set_unity_text`: Sets contents of UI Text, TextMesh, or TextMeshPro components.
- `get_unity_scene_graph`: Returns a JSON tree of active GameObjects, tags, and states.
- `list_unity_scene`: Fast scan listing all object names in the scene.

### 🎨 Visual Reasoning
- `take_unity_screenshot`: Captures SceneView viewports and returns an inline Base64 PNG block directly to the visual LLM interface.

### 🔍 Deep Reflection & Inspection
- `inspect_gameobject`: Lists all components attached to a target GameObject.
- `get_component_properties`: Lists all public properties, fields, types, and active values.

### 📁 Project & Assets Database
- `instantiate_prefab`: Spawns a prefab asset at a given coordinate using its project path (e.g. `Assets/Prefabs/Player.prefab`).
- `save_scene` / `load_scene`: Saves or opens `.unity` scene paths.

### ⚡ C# Code Injection
- `compile_csharp_script`: Injects C# classes inheriting from `MonoBehaviour` and compiles them instantly in the editor on the fly.

### 👾 Game Master (Gameplay Integration)
- `get_game_state`: Exposes runtime clock and player state metadata.
- `get_presets`: Queries dynamic spawning presets from Resources.
- `spawn_entity`: Spawns capsule monsters or prop presets.
- `trigger_world_event`: Emits scene-wide events.
- `broadcast_narrative`: Appends visual messages.
- `set_npc_dialogue`: Alters NPC speech lines and moods.
- `query_nearby_entities`: Lists active entities in a spherical range.
- `search_assets`: Query expression search on the asset database.


## 👨‍💻 For Developers

### 🧩 Project Structure

- `unity-mcp-server/index.js`: MCP server implementation (Node.js)
- `UnityScripts/`: C# bridge and Editor dispatcher logic
- `setup.bat`: Windows automation helper for installation

### 📦 Building the Project

```bash
npm run build
```

### ✅ Tests

```bash
npm test
```

### 🤝 Contributing

Contributions are welcome. If you want to improve Unity-MCP Pro, open an issue or submit a pull request with a focused change.

---
**Developed by [NishantJLU](https://github.com/NishantJLU)**
