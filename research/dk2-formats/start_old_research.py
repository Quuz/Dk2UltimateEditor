"""Start a separate old-EXE analysis server from the static research project.

Existing DX and Pro projects/processes stay live. No input PE is modified.
"""
from pathlib import Path
import json, logging, logging.handlers, os, shutil, socket, subprocess, sys

ROOT = Path(__file__).resolve().parent
SOURCE = Path('C:/Users/Quuz/Documents/Warfare 2/out/dynamic_driver_research_20261013')
PORT = 8091


def main():
    with socket.socket() as sock:
        if sock.connect_ex(('127.0.0.1', PORT)) == 0:
            raise RuntimeError('Port8091 already listens; inspect/reuse its actual process.')
    project = ROOT / 'old-headless-project'
    reuse = '--reuse-prepared' in sys.argv
    if project.exists() and not reuse:
        raise RuntimeError('Old analysis project already exists; inspect state before retrying.')
    if reuse and not project.exists():
        raise RuntimeError('Cannot reuse a missing prepared project.')
    assert project.resolve().is_relative_to(ROOT.resolve())
    if not reuse:
        project.mkdir()
        shutil.copytree(SOURCE / 'DriverResearch.rep', project / 'DriverResearch.rep')
        shutil.copy2(SOURCE / 'DriverResearch.gpr', project / 'DriverResearch.gpr')
    args = (ROOT / 'headless-java.args').read_text(encoding='utf8').splitlines()
    args[0] = '"-Xmx2g"'
    args = [line.replace('/headless-home', '/old-headless-home').replace('/headless-project/', '/old-headless-project/') for line in args]
    args[args.index('"--port"') + 1] = '"8091"'
    args[args.index('"--program"') + 1] = '"/DK2-format-research/DKII-local13.exe"'
    for folder in ['settings', 'cache']:
        (ROOT / 'old-headless-home' / folder).mkdir(parents=True, exist_ok=True)
    arg_path = ROOT / 'old-headless-java.args'
    arg_path.write_text('\n'.join(args) + '\n', encoding='utf8')
    logger = logging.getLogger('old-research-server')
    logger.setLevel(logging.INFO)
    handler = logging.handlers.RotatingFileHandler(ROOT / 'old-headless-stdout.log', maxBytes=8*1024*1024, backupCount=2, encoding='utf8')
    handler.setFormatter(logging.Formatter('%(message)s'))
    logger.addHandler(handler)
    child = subprocess.Popen(['C:/Program Files/Java/jdk-21.0.10/bin/java.exe', '@' + str(arg_path)], cwd=ROOT,
        stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
        creationflags=subprocess.CREATE_NO_WINDOW if os.name == 'nt' else 0)
    (ROOT / 'old-headless-instance.json').write_text(json.dumps(dict(processId=child.pid,
        supervisorPid=os.getpid(), port=PORT, project=str(project / 'DriverResearch.gpr'),
        program='/DK2-format-research/DKII-local13.exe', source_project=str(SOURCE / 'DriverResearch.gpr'),
        independent_static_copy=True, active_DX_and_Pro_projects_untouched=True,
        log_max_bytes=8*1024*1024, log_backups=2), indent=2), encoding='utf8')
    for line in iter(lambda: child.stdout.readline(131072), b''):
        logger.info(line.decode('utf8', errors='replace').rstrip('\r\n'))
    logger.info('Old research Java exited: %s', child.wait())
    handler.close()


if __name__ == '__main__':
    main()
