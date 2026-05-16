import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  ErrorCode,
  McpError,
} from "@modelcontextprotocol/sdk/types.js";
import axios from "axios";
import { z } from "zod";

const UNITY_URL = "http://127.0.0.1:60432/";
const API_KEY = process.env.UNITY_MCP_API_KEY || ""; 

// Validation Schemas
const CreateEntitySchema = z.object({
  type: z.enum(["NPC", "Monster", "Prop"]),
  name: z.string(),
  preset: z.string().optional().description("Visual/Behavior preset defined in Unity"),
  position: z.array(z.number()).length(3).optional(),
});

const TriggerEventSchema = z.object({
  event_name: z.string(),
  intensity: z.number().min(0).max(1).optional(),
  description: z.string().optional(),
});

const SetDialogueSchema = z.object({
  target_name: z.string(),
  text: z.string(),
  mood: z.enum(["Neutral", "Angry", "Happy", "Mysterious"]).optional(),
});

const QueryNearbySchema = z.object({
  radius: z.number().default(20),
});

const SearchAssetsSchema = z.object({
  query: z.string().describe("Search query for assets like 't:Material', 't:Prefab', or a name"),
});

class UnityGMServer {
  private server: Server;

  constructor() {
    this.server = new Server(
      {
        name: "unity-game-master",
        version: "1.0.0",
      },
      {
        capabilities: {
          tools: {},
        },
      }
    );

    this.setupHandlers();
  }

  private setupHandlers() {
    this.server.setRequestHandler(ListToolsRequestSchema, async () => ({
      tools: [
        {
          name: "get_game_state",
          description: "Get the current state of the game world (player health, gold, active quests, time of day).",
          inputSchema: { type: "object", properties: {} },
        },
        {
          name: "get_presets",
          description: "List available entity presets (classes/types) defined in the Unity project.",
          inputSchema: { type: "object", properties: {} },
        },
        {
          name: "spawn_entity",
          description: "Spawn an NPC, monster, or prop into the scene.",
          inputSchema: {
            type: "object",
            properties: {
              type: { type: "string", enum: ["NPC", "Monster", "Prop"] },
              name: { type: "string" },
              preset: { type: "string", description: "Visual/Behavior preset ID (e.g., 'shadow_wraith')" },
              position: { type: "array", items: { type: "number" }, minItems: 3, maxItems: 3 },
            },
            required: ["type", "name"],
          },
        },
        {
          name: "trigger_world_event",
          description: "Trigger a global event like weather changes, music shifts, or boss spawns.",
          inputSchema: {
            type: "object",
            properties: {
              event_name: { type: "string" },
              intensity: { type: "number", minimum: 0, maximum: 1 },
              description: { type: "string" },
            },
            required: ["event_name"],
          },
        },
        {
          name: "broadcast_narrative",
          description: "Send a narrative message or 'flavor text' that appears on the player's screen (e.g., 'A cold wind blows from the north').",
          inputSchema: {
            type: "object",
            properties: {
              message: { type: "string" },
              style: { type: "string", enum: ["Standard", "Warning", "Epic", "Whisper"] },
            },
            required: ["message"],
          },
        },
        {
          name: "set_npc_dialogue",
          description: "Set the current dialogue and mood for an NPC in the scene.",
          inputSchema: {
            type: "object",
            properties: {
              target_name: { type: "string" },
              text: { type: "string" },
              mood: { type: "string", enum: ["Neutral", "Angry", "Happy", "Mysterious"] },
            },
            required: ["target_name", "text"],
          },
        },
        {
          name: "query_nearby_entities",
          description: "Scan the area around the player for interactable objects and NPCs.",
          inputSchema: {
            type: "object",
            properties: {
              radius: { type: "number", default: 20 },
            },
          },
        },
        {
          name: "search_assets",
          description: "Search the Unity Asset Database for assets.",
          inputSchema: {
            type: "object",
            properties: {
              query: { type: "string", description: "Query like 't:Prefab', 't:Material' or specific name" },
            },
            required: ["query"],
          },
        },
      ],
    }));

    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;

      try {
        let unityCommand = { command: "", data: {} };

        switch (name) {
          case "get_game_state":
            unityCommand = { command: "GET_STATE", data: {} };
            break;

          case "get_presets":
            unityCommand = { command: "GET_PRESETS", data: {} };
            break;

          case "spawn_entity":
            const spawnArgs = CreateEntitySchema.parse(args);
            unityCommand = { command: "SPAWN_ENTITY", data: spawnArgs };
            break;

          case "trigger_world_event":
            const eventArgs = TriggerEventSchema.parse(args);
            unityCommand = { command: "TRIGGER_EVENT", data: eventArgs };
            break;

          case "broadcast_narrative":
            unityCommand = { command: "BROADCAST", data: args || {} };
            break;

          case "set_npc_dialogue":
            const dialogueArgs = SetDialogueSchema.parse(args);
            unityCommand = { command: "SET_DIALOGUE", data: dialogueArgs };
            break;

          case "query_nearby_entities":
            const queryArgs = QueryNearbySchema.parse(args || {});
            unityCommand = { command: "QUERY_NEARBY", data: queryArgs };
            break;

          case "search_assets":
            const searchArgs = SearchAssetsSchema.parse(args);
            unityCommand = { command: "SEARCH_ASSETS", data: searchArgs };
            break;

          default:
            throw new McpError(ErrorCode.MethodNotFound, `Unknown tool: ${name}`);
        }

        const response = await axios.post(UNITY_URL, unityCommand, { 
          timeout: 5000,
          headers: {
            "X-MCP-Token": API_KEY
          }
        });
        return {
          content: [{ type: "text", text: JSON.stringify(response.data, null, 2) }],
        };
      } catch (error) {
        if (error instanceof z.ZodError) {
          return {
            content: [{ type: "text", text: `Invalid arguments: ${error.errors.map(e => e.message).join(", ")}` }],
            isError: true,
          };
        }
        if (axios.isAxiosError(error)) {
          return {
            content: [{ type: "text", text: `Unity connection error: ${error.message}. Is the Unity bridge running?` }],
            isError: true,
          };
        }
        return {
          content: [{ type: "text", text: `Error: ${error instanceof Error ? error.message : String(error)}` }],
          isError: true,
        };
      }
    });
  }

  async run() {
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    console.error("Unity Game Master MCP Server running on stdio");
  }
}

const server = new UnityGMServer();
server.run().catch(console.error);
