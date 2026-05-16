# Unity-MCP Pro — Agentic Unity Bridge
**Connects LLMs (Claude, GPT) to Unity in real-time via MCP Protocol**

[![CodeQL](https://github.com/NishantJLU/Unity-MCP/actions/workflows/codeql.yml/badge.svg)](https://github.com/NishantJLU/Unity-MCP/actions)
[![Node.js CI](https://github.com/NishantJLU/Unity-MCP/actions/workflows/node.js.yml/badge.svg)](https://github.com/NishantJLU/Unity-MCP/actions)
![License](https://img.shields.io/badge/License-MIT-blue.svg)

## 🏗️ How It Works
The bridge facilitates bidirectional communication between Large Language Models and the Unity Engine.

```text
LLM (Claude/GPT) ──▶ MCP Server (Node.js) ──▶ WebSocket/HTTP ──▶ Unity (C# Bridge)
      ▲                                                           │
      └───────────────────────────────────────────────────────────┘
                         World State & Feedback
```

## 🛠️ Prerequisites
- **Node.js**: 18.x or higher
- **Unity Engine**: 2021.3 LTS or higher
- **LLM API Key**: OpenAI or Anthropic key for the MCP client

## 🚀 Setup Guide

### 1. Node.js MCP Server
1. Navigate to the `server/` directory.
2. Run `npm install`.
3. Build the server: `npm run build`.
4. Configure your MCP client (e.g., Claude Desktop) to use the built `index.js`.

### 2. Unity Bridge
1. Open your Unity project.
2. Install the package via UPM: `https://github.com/NishantJLU/Unity-MCP.git?path=/unity`.
3. Open the bridge window: **Window > AI > Game Master Bridge**.
4. Click **Start Bridge**.

## 🎮 Use Cases
- **AI-Driven NPC Behavior**: Generate dynamic dialogue and actions based on world state.
- **Procedural Level Generation**: Use natural language to describe and spawn complex environments.
- **Game Testing Automation**: Instruct an AI agent to "find bugs" or "playtest level 1" via the bridge.

## 📜 License
This project is licensed under the MIT License.
