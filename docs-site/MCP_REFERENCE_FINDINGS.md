# MCP Reference Findings

The official MCP specification revision inspected was 2026-07-28.

## Protocol requirements relevant to AssetRipper

MCP uses JSON-RPC 2.0 between a host, client, and server. A server that exposes tools must declare the tools capability. The official tools page documents `tools/list` for deterministic tool discovery and `tools/call` for invocation. Tool definitions must include a name, description, and a valid JSON Schema `inputSchema`; servers should return tools in deterministic order. Tool results use a `content` array and may also include structured content/output schemas.

The specification emphasizes that tool execution is arbitrary code execution and that a human-in-the-loop consent/deny path should exist. For AssetRipper, the server should therefore keep file-system scope explicit, reject unsafe paths, avoid executing user-supplied programs, and report export operations clearly. The requested local integration is a stdio server; this task therefore delivers a standalone MCP executable and configuration without changing external connector settings.

## Sources

- https://modelcontextprotocol.io/specification/2026-07-28
- https://modelcontextprotocol.io/specification/2026-07-28/server/tools
