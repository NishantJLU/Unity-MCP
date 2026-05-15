const path = require('path');
const fs = require('fs');

const serverPath = path.join(__dirname, 'unity-mcp-server', 'index.js').replace(/\\/g, '/');

const config = {
  "unity-mcp-pro": {
    "command": "node",
    "args": [serverPath]
  }
};

console.log("\n--------------------------------------------------");
console.log("MCP CONFIGURATION SNIPPET");
console.log("--------------------------------------------------");
console.log("Copy and paste the following into your Claude Desktop");
console.log("config file (usually %APPDATA%/Claude/claude_desktop_config.json):");
console.log("\n" + JSON.stringify(config, null, 2));
console.log("\n--------------------------------------------------");
