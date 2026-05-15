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

---

### 📦 Installation

1.  **Clone & Setup:**
    ```bash
    git clone https://github.com/NishantJLU/Unity-MCP.git
    cd Unity-MCP
    ```
2.  **Unity Integration:**
    *   Copy the `unity/` folder into your Unity `Assets/Editor` directory.
    *   In Unity, go to **Window > AI > Game Master Bridge**.
    *   Click **Start GM Bridge**.

3.  **MCP Server Setup:**
    *   Navigate to `server/`.
    *   Run `npm install` and `npm run build`.
    *   Add to your MCP config:
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
