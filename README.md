# ?? Unity-MCP Pro: The Agentic Unity Bridge

[![Status: Production Ready](https://img.shields.io/badge/Status-Production--Ready-00f2ff?style=for-the-badge)](https://github.com/NishantJLU/Unity-MCP)
[![Platform: Unity Editor](https://img.shields.io/badge/Platform-Unity_Editor-ff007f?style=for-the-badge)](https://unity.com/)
[![Protocol: MCP](https://img.shields.io/badge/Protocol-MCP-yellow?style=for-the-badge)](https://modelcontextprotocol.io/)

**Unity-MCP Pro** is a high-performance bridge that transforms the Unity Editor into an agentic environment. It enables Large Language Models (LLMs) like Claude 3.5 Sonnet and Gemini 1.5 Pro to interact with Unity in real-time—allowing for AI-driven level design, automated scene auditing, and live UI construction.

---

## ?? Pro Features

### ??? AI Vision (Visual Feedback)
Empower your AI to "see" its work. Using the 	ake_unity_screenshot tool, the AI captures live snapshots of the Editor, allowing it to reason about layout, aesthetics, and material properties visually.

### ?? Scene Graph Analytics
Move beyond simple object lists. The AI receives a structured JSON map of the entire scene hierarchy, including tags, active states, and parent-child relationships, enabling complex architectural reasoning.

### ? Reflection-Based Control
Modify *any* public property on *any* component. By leveraging C# Reflection, the AI can tweak deep parameters like Light intensity, Rigidbody mass, or custom script variables without needing pre-defined tools.

### ??? Main-Thread Safety
A robust EditorApplication dispatcher ensures all AI commands are executed safely on Unity's main thread, preventing crashes during intensive automation tasks.

---

## ??? Detailed Setup Guide

### ?? Prerequisites
- **Node.js (v18+):** Required to run the MCP server. [Download here](https://nodejs.org/).
- **Unity Editor:** Compatible with Unity 2021.x and newer.
- **MCP Client:** An AI client that supports the Model Context Protocol (e.g., [Claude Desktop](https://claude.ai/download)).

---

### ?? Step 1: Installation & Dependencies
1.  **Clone the Repository:**
    `ash
    git clone https://github.com/NishantJLU/Unity-MCP.git
    cd Unity-MCP
    `
2.  **Automated Setup:**
    Double-click the **setup.bat** file. This script will automatically install the necessary Node.js modules and prepare the configuration for your specific machine.

---

### ?? Step 2: Unity Editor Configuration
1.  Open your target Unity project.
2.  **Import Bridge:** Drag the UnityScripts folder from this repo into your Unity Assets folder (we recommend creating an Editor subfolder, e.g., Assets/Plugins/UnityMCP/Editor).
3.  **Launch Window:** In the Unity top menu, navigate to **Window > AI > Unity MCP Pro**.
4.  **Establish Link:** In the AI Bridge window, click the **ESTABLISH LINK** button. The status should change to LINK_ESTABLISHED.

---

### ?? Step 3: Connecting the AI (Claude Desktop)
1.  After running setup.bat, a JSON snippet will be displayed in your terminal.
2.  Open your Claude Desktop configuration file (typically found at %APPDATA%\Claude\claude_desktop_config.json).
3.  Add the snippet to the mcpServers section:
    `json
    {
      "mcpServers": {
        "unity-mcp-pro": {
          "command": "node",
          "args": ["C:/PATH/TO/YOUR/REPO/unity-mcp-server/index.js"]
        }
      }
    }
    `
4.  Restart Claude Desktop. You should now see the Unity tools available in the interface.

---

## ??? Technical Architecture

- **The Brain (Node.js):** An MCP server that exposes Unity's functionality as structured tools for the AI.
- **The Hand (C#):** A custom Unity Editor window that hosts a high-performance HTTP listener and command dispatcher.
- **Communication:** Low-latency JSON packets exchanged via a local bridge, ensuring real-time response to AI commands.

---

## ?? Example Commands

- *"Analyze the scene and move all objects tagged as 'Prop' into a new folder called 'Environment'."*
- *"Take a screenshot, check if the red cube is properly centered, and adjust it if necessary."*
- *"Build a 10x10 grid of glowing blue spheres with physics enabled."*
- *"Create a main menu UI with a 'Start' button and a title label that says 'NEON STRIKE'."*

---

## ?? License
Distributed under the **MIT License**. See LICENSE for more information.

---
*Developed for the future of AI-assisted game development.*
