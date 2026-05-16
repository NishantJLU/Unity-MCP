# 🌌 Unity-MCP Pro: The Agentic Unity Bridge & Game Master 🎮🧙‍♂️

[![Status: Production Ready](https://img.shields.io/badge/Status-Production--Ready-00f2ff?style=for-the-badge)](https://github.com/NishantJLU/Unity-MCP)
[![Platform: Unity Editor](https://img.shields.io/badge/Platform-Unity_Editor-ff007f?style=for-the-badge)](https://unity.com/)
[![Protocol: MCP](https://img.shields.io/badge/Protocol-MCP-yellow?style=for-the-badge)](https://modelcontextprotocol.io/)

**Unity-MCP Pro** is a high-performance bridge that transforms the Unity Editor into an agentic environment and a living world managed by an AI Game Master. It enables Large Language Models (LLMs) like Claude 3.5 Sonnet and Gemini 1.5 Pro to interact with Unity in real-time.

---

## 💎 Features

### 🧙‍♂️ AI Game Master System
Transform your project into a dynamic experience. The AI can:
*   **Observe World State:** Query player health, location, and active quests.
*   **Spawn Entities:** Dynamically create NPCs, monsters, or props using a **JSON Preset System**.
*   **Narrative Broadcasting:** Send "flavor text" and atmospheric descriptions directly to the game.
*   **Dialogue Injection:** Set dialogue and moods for NPCs in the scene.

### ⚡ Extensibility & Safety (New in v1.1.0)
*   **Attribute-Based Command Dispatching:** Easily add new AI tools directly in C# using `[MCPTool("command_name")]`. No switch statements needed!
*   **Undo Support:** All AI actions automatically register with Unity's Undo system (`Ctrl+Z`).
*   **Robust Security:** The bridge explicitly binds to `127.0.0.1` (localhost) and supports an **API Key** required for authorization via `X-MCP-Token`.
*   **LOD Scene Query:** Level of Detail for scene querying. Query only the objects within a radius instead of dumping the whole scene.
*   **Asset Searching:** Allow the AI to search the `AssetDatabase` to find specific materials, prefabs, or scripts on the fly.

---

## 🛠️ Setup Guide

### 📋 Prerequisites
- **Node.js (v18+):** Required to run the MCP server.
- **Unity Editor:** Compatible with Unity 2021.x and newer.
- **MCP Client:** (e.g., [Claude Desktop](https://claude.ai/download)).

### 📦 Installation

#### 1. Unity Package Installation (UPM)
Instead of copying files manually, install it as a Unity Package.
*   In Unity, go to **Window > Package Manager**.
*   Click the **+** icon in the top-left corner.
*   Select **Add package from git URL...**
*   Enter: `https://github.com/NishantJLU/Unity-MCP.git?path=/unity`
*   Once installed, go to **Window > AI > Game Master Bridge**.
*   Click **Start GM Bridge**. Note the **API Key** generated in the window.

#### 2. MCP Server Setup
*   Clone the repo locally for the server part:
    ```bash
    git clone https://github.com/NishantJLU/Unity-MCP.git
    cd Unity-MCP/server
    npm install
    npm run build
    ```
*   Add the following to your MCP client configuration (e.g., `claude_desktop_config.json`), making sure to supply the API key via environment variable:

    ```json
    {
      "mcpServers": {
        "unity-game-master": {
          "command": "node",
          "args": ["C:/PATH/TO/REPO/server/build/index.js"],
          "env": {
            "UNITY_MCP_API_KEY": "YOUR_API_KEY_FROM_UNITY_WINDOW"
          }
        }
      }
    }
    ```

---

## 📚 Tool Schema (API Reference)

The following MCP tools are available to the LLM:

*   **`get_game_state`**: Get the current state of the game world (time, player location, health).
*   **`get_presets`**: List available entity presets defined in the Unity project.
*   **`spawn_entity`**: Spawn an NPC, monster, or prop into the scene.
    *   Parameters: `type` (NPC/Monster/Prop), `name` (string), `preset` (optional string), `position` (optional float[3]).
*   **`trigger_world_event`**: Trigger a global event like weather changes, music shifts, or boss spawns.
    *   Parameters: `event_name` (string), `intensity` (0-1).
*   **`broadcast_narrative`**: Send a narrative message or flavor text to the screen.
    *   Parameters: `message` (string), `style` (Standard/Warning/Epic/Whisper).
*   **`set_npc_dialogue`**: Set dialogue and mood for an NPC.
    *   Parameters: `target_name` (string), `text` (string), `mood` (Neutral/Angry/Happy/Mysterious).
*   **`query_nearby_entities`**: Scan the area around the active scene view for interactable objects.
    *   Parameters: `radius` (float, default 20).
*   **`search_assets`**: Search the Unity Asset Database.
    *   Parameters: `query` (string, e.g., 't:Prefab', 't:Material', or name).

---

## 🚨 Troubleshooting

*   **Port Conflicts:** The bridge runs on port `60432`. If the bridge fails to start, another application may be using this port. Change it in the C# source or close the conflicting application.
*   **Unauthorized Errors:** If the Node.js server connects but Unity rejects commands, ensure the `UNITY_MCP_API_KEY` environment variable in your MCP client config perfectly matches the API Key displayed in the Unity GM Bridge window.
*   **Main Thread Bottlenecks:** Be careful querying massive radii using `query_nearby_entities`, as it runs on the main thread and can cause editor hitches if retrieving thousands of objects.

---

## ⚙️ Advanced Configuration: Game Master Presets

The Game Master's true power comes from the **Preset System**. Located at `unity/Resources/GMPresets.json`, this file defines the "DNA" of entities the AI can spawn.

```json
{
  "presets": [
    {
      "id": "shadow_wraith",
      "displayName": "Shadow Wraith",
      "type": "Monster",
      "scale": 1.5,
      "color": [0.1, 0, 0.2, 0.8],
      "components": ["Rigidbody", "Light"]
    }
  ]
}
```

---

## 📜 License
Distributed under the **MIT License**.

---
*Bridging the gap between LLMs and Real-time Game Engines.*
