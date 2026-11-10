"""Inspect bridge tool schemas without changing the analyzed program."""
import asyncio, json, os
from pathlib import Path
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

ROOT = Path(__file__).resolve().parent

async def main():
    params = StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'), '--transport', 'stdio'],
        env={**os.environ, 'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    async with stdio_client(params) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()
            result = await session.list_tools()
            selected = [t.model_dump() for t in result.tools
                        if any(x in t.name for x in ['prototype', 'signature', 'program', 'reference'])]
            (ROOT/'ghidra/research-tool-schemas.json').write_text(json.dumps(selected,indent=2),encoding='utf8')
            print(json.dumps(selected,indent=2))

asyncio.run(main())
