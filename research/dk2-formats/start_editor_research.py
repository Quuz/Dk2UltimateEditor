"""Start an independent, bounded-log Pro-editor analysis server from a static copy.

Does not stop or reuse the live DX project. Writes only under the research root.
"""
from pathlib import Path
import hashlib, json, logging, logging.handlers, os, shutil, socket, subprocess

ROOT = Path(__file__).resolve().parent
SOURCE = Path('C:/Users/Quuz/Documents/Warfare 2/out/dynamic_driver_research_20261013')
PORT = 8090

def main():
    state_path = ROOT / 'editor-headless-instance.json'
    with socket.socket() as sock:
        if sock.connect_ex(('127.0.0.1', PORT)) == 0:
            raise RuntimeError('Port8090 already listens; inspect and reuse its actual process instead.')
    project_root = ROOT / 'editor-headless-project'
    if project_root.exists():
        raise RuntimeError('Independent project already exists; inspect its state before retrying startup.')
    project_root.mkdir()
    shutil.copytree(SOURCE / 'DriverResearch.rep', project_root / 'DriverResearch.rep')
    shutil.copy2(SOURCE / 'DriverResearch.gpr', project_root / 'DriverResearch.gpr')
    args = (ROOT / 'headless-java.args').read_text(encoding='utf8').splitlines()
    args[0] = '"-Xmx2g"'
    for i, value in enumerate(args):
        args[i] = value.replace('/headless-home', '/editor-headless-home').replace('/headless-project/', '/editor-headless-project/')
    args[args.index('"--port"') + 1] = '"8090"'
    args[args.index('"--program"') + 1] = '"/DK2-format-research/DK2 Editor pro.exe"'
    for name in ['settings', 'cache']:
        (ROOT / 'editor-headless-home' / name).mkdir(parents=True, exist_ok=True)
    arg_path = ROOT / 'editor-headless-java.args'
    arg_path.write_text('\n'.join(args) + '\n', encoding='utf8')
    logger = logging.getLogger('editor-research-server')
    logger.setLevel(logging.INFO)
    handler = logging.handlers.RotatingFileHandler(ROOT / 'editor-headless-stdout.log', maxBytes=8*1024*1024, backupCount=2, encoding='utf8')
    handler.setFormatter(logging.Formatter('%(message)s'))
    logger.addHandler(handler)
    child = subprocess.Popen(['C:/Program Files/Java/jdk-21.0.10/bin/java.exe', '@' + str(arg_path)], cwd=ROOT,
        stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
        creationflags=subprocess.CREATE_NO_WINDOW if os.name == 'nt' else 0)
    state = dict(processId=child.pid, supervisorPid=os.getpid(), port=PORT,
        project=str(project_root / 'DriverResearch.gpr'), program='/DK2-format-research/DK2 Editor pro.exe',
        source_project=str(SOURCE / 'DriverResearch.gpr'), independent_static_copy=True,
        active_DX_project_untouched=True, log_max_bytes=8*1024*1024, log_backups=2)
    state_path.write_text(json.dumps(state, indent=2), encoding='utf8')
    for line in iter(lambda: child.stdout.readline(131072), b''):
        logger.info(line.decode('utf8', errors='replace').rstrip('\r\n'))
    logger.info('Editor research Java exited: %s', child.wait())
    handler.close()

if __name__ == '__main__':
    main()
