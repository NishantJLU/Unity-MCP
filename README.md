# 🌌 Unity-MCP Pro: The Agentic Unity Bridge

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/NishantJLU/Unity-MCP?color=00f2ff&style=for-the-badge)](https://github.com/NishantJLU/Unity-MCP/releases)
[![Status: Production Ready](https://img.shields.io/badge/Status-Production--Ready-00f2ff?style=for-the-badge)](https://github.com/NishantJLU/Unity-MCP)
[![CodeQL](https://github.com/NishantJLU/Unity-MCP/actions/workflows/codeql.yml/badge.svg)](https://github.com/NishantJLU/Unity-MCP/actions)
[![Node.js CI](https://github.com/NishantJLU/Unity-MCP/actions/workflows/node.js.yml/badge.svg)](https://github.com/NishantJLU/Unity-MCP/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=for-the-badge)](https://opensource.org/licenses/MIT)

**Unity-MCP Pro** is a high-performance bridge that transforms the Unity Editor into an agentic environment. It enables LLMs (like Claude 3.5 Sonnet and Gemini 1.5 Pro) to interact with Unity in real-time for AI-driven level design, scene auditing, and live construction.

---

## 🚀 Quick Start (3-Minute Setup)

### 1. Download & Install
Download the **[Latest Release (v1.0.0)](https://github.com/NishantJLU/Unity-MCP/releases/latest)** and extract it. Double-click **`setup.bat`** to install dependencies automatically.

### 2. Add to Unity
Drag the `UnityScripts` folder into your Unity project's `Assets` folder. Open **Window > AI > Unity MCP Pro** and click **ESTABLISH LINK**.

### 3. Connect to AI
Add this snippet to your `claude_desktop_config.json`:
```json
{
  "mcpServers": {
    "unity-mcp-pro": {
      "command": "node",
      "args": ["C:/PATH/TO/Unity-MCP/unity-mcp-server/index.js"]
    }
  }
}
```

---

## ❓ Why Unity-MCP Pro?
While basic scripts can move objects, **Unity-MCP Pro** provides a deep, production-grade integration:
*   **👁️ AI Vision:** AI can "see" through your Editor camera to reason about layout and aesthetics.
*   **⚡ Reflection-Based:** No need to write new tools. AI can access *any* public variable on *any* component via C# Reflection.
*   **🛡️ Safety First:** All commands are dispatched via the Main Thread to prevent Editor crashes.
*   **📊 Full Context:** AI receives a high-fidelity JSON map of your scene hierarchy, tags, and layers.

---

## 💎 Pro Features

### AI Vision (Visual Feedback)
Empower your AI to capture live snapshots of the Editor, allowing it to reason about material properties, lighting, and UI placement visually.

### Scene Graph Analytics
The AI receives a structured map of the entire scene hierarchy, enabling complex architectural reasoning and automated auditing.

### Reflection-Based Control
Modify deep parameters like Light intensity, Rigidbody mass, or custom script variables without needing pre-defined tools.

---

## 🏗️ Technical Architecture

- **The Brain (Node.js):** An MCP server that exposes Unity functionality as structured tools.
- **The Hand (C#):** A custom Editor window with a high-performance HTTP listener.
- **Communication:** Low-latency JSON bridge for real-time AI command execution.

---

## 💬 Community & Feedback
*   **Discussion:** Have an idea for a new tool? [Join the Discussion](https://github.com/NishantJLU/Unity-MCP/discussions).
*   **Bugs:** Report issues [here](https://github.com/NishantJLU/Unity-MCP/issues).
*   **Support:** If this helps your workflow, please consider giving us a ⭐!

---
**Developed by [NishantJLU](https://github.com/NishantJLU)**
