"""Independent editor8090 Ghidra MCP queries; original shared writer semantics."""
import asyncio
import json
import os
import sys
from pathlib import Path
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

ROOT = Path(__file__).resolve().parent

async def main():
    params = StdioServerParameters(
        command=str(ROOT.parent / 'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent / 'ghidra-mcp/bridge_mcp_ghidra.py'), '--transport', 'stdio'],
        env={**os.environ, 'GHIDRA_MCP_URL': 'http://127.0.0.1:8090'},
    )
    tasks = json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'))
    out = ROOT / 'ghidra'
    out.mkdir(exist_ok=True)
    async with stdio_client(params) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()
            for task in tasks:
                result = await session.call_tool(task['tool'], task.get('args', {}))
                value = '\n'.join(c.text for c in result.content if c.type == 'text')
                try:
                    response = json.loads(value)
                except (ValueError, TypeError):
                    response = None
                # Some bridge endpoints encode errors in successful MCP responses.
                endpoint_error = isinstance(response, dict) and 'error' in response
                already_exists = (task['tool'] == 'create_function' and endpoint_error
                                  and 'Function already exists' in str(response['error']))
                text_error = value.startswith(('Error:', 'Failed to ', 'Unknown tool:',
                                               'Automatic approval review failed:',
                                               'The automatic permission approval review'))
                if result.isError or text_error or (endpoint_error and not already_exists):
                    (out / (task['id'] + '-error.json')).write_text(result.model_dump_json(indent=2), encoding='utf-8')
                    (out / (task['id'] + '-error.txt')).write_text(value, encoding='utf-8')
                    raise RuntimeError(f"{task['id']}: MCP request failed; previous evidence retained: {value[:300]}")
                (out / (task['id'] + '.json')).write_text(result.model_dump_json(indent=2), encoding='utf-8')
                (out / (task['id'] + '.txt')).write_text(value, encoding='utf-8')
                print(task['id'], value[:task.get('print_limit', 1600)], flush=True)

asyncio.run(main())
