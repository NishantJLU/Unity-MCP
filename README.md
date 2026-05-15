# Unity Game Master MCP 🎮🧙‍♂️

Transform your Unity project into a living world managed by an AI Game Master. This project provides a **Model Context Protocol (MCP)** server that allows LLMs (like Claude, GPT-4, or Gemini) to observe and manipulate a Unity game in real-time.

## 🚀 Features

-   **World Observation:** The LLM can query player health, inventory, location, and active quests.
-   **Entity Spawning:** Dynamically spawn NPCs, monsters, or props with custom names and positions.
-   **Dialogue Injection:** The GM can set dialogue and moods for any NPC in the scene.
-   **Global Events:** Trigger environmental changes, weather, or boss encounters via natural language.
-   **Scene Awareness:** Tools for the LLM to "scan" nearby entities and understand the spatial context.

## 🛠️ Tech Stack

-   **Server:** Node.js, TypeScript, MCP SDK, Zod (Validation), Axios.
-   **Unity:** C#, Unity Editor Scripting, HttpListener (for the local bridge).

## 📦 Installation

### 1. Unity Side
1.  Copy the `unity/UnityGameMasterBridge.cs` file into your Unity project's `Assets/Editor` folder.
2.  In Unity, go to `Window > AI > Game Master Bridge`.
3.  Click **Start GM Bridge**.

### 2. MCP Server Side
1.  Navigate to the `server/` directory.
2.  Run `npm install` to install dependencies.
3.  Run `npm run build` to compile the TypeScript code.
4.  Configure your MCP-compatible LLM client (e.g., Claude Desktop) to use the server:

```json
{
  "mcpServers": {
    "unity-game-master": {
      "command": "node",
      "args": ["C:/path/to/unity-game-master/server/build/index.js"]
    }
  }
}
```

## 🧪 Example Commands for the LLM

-   "Check the player's current health and location."
-   "Spawn a Mysterious Stranger near the player and set his dialogue to 'Wait! You shouldn't have come here...'"
-   "Trigger a thunder storm with high intensity."
-   "Find all monsters within 20 meters and describe them."

## 🗺️ Roadmap

-   [ ] **Quest System Integration:** Allow the LLM to create and track complex quest chains.
-   [ ] **Procedural Generation:** GM-directed placement of dungeon rooms or loot.
-   [ ] **Multi-Agent GM:** Multiple LLMs acting as different factions in the world.
-   [ ] **Visual Debugger:** An in-game console showing the GM's logic and commands.

## 📄 License

MIT
