"""Independent Ghidra CPU checks of completion signals and non-sacrifice path."""
from pathlib import Path
from types import FunctionType
import asyncio, json, os
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_crypt_action_completion as native
import crosscheck_crypt_waiting as frozen

ROOT = Path(__file__).resolve().parent
CONTROLS = ['80-animation-end', '80-all-completed-no-model', '76-animation-in-progress',
            '76-animation-end', '80-explicit-stop', '80-clock-wrap', '80-clock-wrap-dispatch-only']
# The unchanged full-context comparison driver is reused with isolated
# globals. No frozen module is mutated, and no gameplay function is stubbed.
build = FunctionType(frozen.build.__code__, dict(vars(frozen), native=native), 'build')


async def main():
    params = StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
                                  args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'), '--transport', 'stdio'],
                                  env={**os.environ, 'GHIDRA_MCP_URL': 'http://127.0.0.1:8089'})
    rows = []
    async with stdio_client(params) as (rd, wr):
        async with ClientSession(rd, wr) as session:
            await session.initialize()
            for name in CONTROLS:
                args, n, size = build(name)
                tag = native.sha(json.dumps(args, sort_keys=True).encode())[:12]
                file = ROOT/'ghidra'/f'phase189-crypt-completion-Ghidra-{name}-{tag}.json'
                if file.exists():
                    saved = json.loads(file.read_text(encoding='utf-8')); assert saved['input'] == args
                else:
                    response = await session.call_tool('emulate_function', args)
                    value = json.loads('\n'.join(x.text for x in response.content if x.type == 'text'))
                    saved = dict(input=args, result=value.get('data', value), tool_is_error=response.isError)
                    file.write_text(json.dumps(saved, indent=2), encoding='utf-8')
                r = saved['result']
                assert not saved['tool_is_error'] and r['success'] and r['hit_return'], (name, str(r)[:500])
                assert all(int(r['registers'][k], 16) == v for k, v in dict(EAX=1, EBP=1, EBX=0, ECX=0).items()), (name, r['registers'])
                rows.append(dict(case=name, file=str(file.relative_to(ROOT)), sha256=native.sha(file.read_bytes()), checked_regions=n, checked_bytes=size))
                print(name, n, size, 'Ghidra passed', flush=True)
    (ROOT/'phase189-crypt-completion-crosschecks.json').write_text(json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf-8')


if __name__ == '__main__':
    asyncio.run(main())
