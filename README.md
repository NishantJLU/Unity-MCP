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

### 👁️ AI Vision & Scene Analytics
*   **Visual Feedback:** Use screenshots to allow the AI to reason about aesthetics and layouts.
*   **Scene Graph:** Structured JSON map of the entire hierarchy, tags, and states.

### ⚡ Reflection-Based Control & Safety
*   **Universal Property Tweaking:** Modify *any* public property on *any* component via C# Reflection.
*   **Main-Thread Dispatcher:** Robust execution on Unity's main thread to prevent automation crashes.

---

## 🛠️ Setup Guide

### 📋 Prerequisites
- **Node.js (v18+):** Required to run the MCP server.
- **Unity Editor:** Compatible with Unity 2021.x and newer.
- **MCP Client:** (e.g., [Claude Desktop](https://claude.ai/download)).

### 📦 Installation

1.  **Clone the Repository:**
    ```bash
    git clone https://github.com/NishantJLU/Unity-MCP.git
    cd Unity-MCP
    ```
2.  **Unity Integration:**
    *   Copy the `unity/` folder from this repo into your Unity project's `Assets/Editor` directory.
    *   In Unity, go to **Window > AI > Game Master Bridge**.
    *   Click **Start GM Bridge**.

3.  **MCP Server Setup:**
    *   Navigate to the `server/` directory in this repo.
    *   Run `npm install` and `npm run build`.
    *   Add the following to your MCP client configuration (e.g., `claude_desktop_config.json`):

    ```json
    {
      "mcpServers": {
        "unity-game-master": {
          "command": "node",
          "args": ["C:/PATH/TO/REPO/server/build/index.js"]
        }
      }
    }
    ```

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

*   **id:** The unique identifier used by the AI (via `spawn_entity`).
*   **type:** Base primitive type (`NPC`, `Monster`, `Prop`).
*   **components:** Unity components to be automatically added to the spawned object.

---

## 🏗️ Internal Architecture

1.  **Node.js Server (MCP):** Acts as the "Brain." It validates AI tool calls using Zod schemas and translates them into a JSON command protocol.
2.  **HTTP Local Bridge:** The Unity Editor hosts a lightweight `HttpListener` on port `60432`.
3.  **Command Dispatcher:** Unity receives the JSON, and the `EditorApplication.delayCall` ensures commands are executed on the **Unity Main Thread**.
4.  **C# Reflection:** The bridge uses `System.Reflection` to dynamically find and invoke methods, allowing for deep control without pre-defined API endpoints.

---

## 🎮 Example Commands

- *"Check the player's health and spawn a 'shadow_wraith' if it's above 80%."*
- *"Take a screenshot and tell me if the lighting looks too dramatic."*
- *"Broadcast a message: 'The ground begins to tremble...' and spawn 3 monsters near the player."*
- *"Set the Village Elder's dialogue to 'The stars are cold tonight' with a Mysterious mood."*

---

## 📜 License
Distributed under the **MIT License**.

---
*Bridging the gap between LLMs and Real-time Game Engines.*
