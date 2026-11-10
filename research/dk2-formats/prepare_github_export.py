"""Export the format-research snapshot into an isolated UltimateEditor checkout."""
from pathlib import Path
import argparse,hashlib,json,zipfile
ROOT=Path(__file__).resolve().parent
sha=lambda data:hashlib.sha256(data).hexdigest()


def write(path,text):
    path.parent.mkdir(parents=True,exist_ok=True);path.write_text(text,encoding='utf-8',newline='\n')


def export(repo):
    repo=repo.resolve();assert (repo/'.git').exists()
    dest=repo/'research/dk2-formats';assert not dest.exists(),'Refuse to overwrite an existing snapshot'
    dest.mkdir(parents=True)
    raw=(ROOT/'editor-format-package.json').read_bytes();package=json.loads(raw)
    assert len(package['components'])==171
    assert json.loads((ROOT/'phase193-validation.json').read_text(encoding='utf-8'))['passed'] is True
    write(dest/'.gitattributes','* -text\n')
    write(dest/'.gitignore','__pycache__/\n*.pyc\nghidra/\neditor-format-package.json\n')
    files=[];excluded=[]
    for p in sorted(ROOT.iterdir(),key=lambda p:p.name.lower()):
        if not p.is_file() or p.name=='editor-format-package.json':continue
        if p.suffix not in ['.json','.md','.py','.csv','.java','.txt','.gz','.bin']:continue
        if 'headless' in p.name.lower() or p.name.startswith('cleanup-') or p.name=='cleanup_research_cache.py':
            excluded.append(dict(file=p.name,reason='Local process/disk-maintenance metadata'));continue
        data=p.read_bytes();(dest/p.name).write_bytes(data)
        files.append(dict(file=p.name,bytes=len(data),sha256=sha(data)))
    for key,ref in package['components'].items():assert sha((dest/ref['file']).read_bytes())==ref['sha256'],key
    metadata={k:v for k,v in package.items() if k not in package['components']}
    index=dict(schema_version=1,original_package_sha256=sha(raw),original_package_bytes=len(raw),key_order=list(package),
        serialization=dict(encoding='utf-8',indent=2,ensure_ascii=False,newline='\r\n' if b'\r\n' in raw else '\n'),
        package_metadata=metadata,use='Assemble with tools/assemble_format_package.py; the full original package reconstructs byte-for-byte.')
    write(dest/'format-package.index.json',json.dumps(index,ensure_ascii=False,indent=2)+'\n')
    archive=dest/'primary-evidence.zip';members=0;uncompressed=0
    sources=sorted((p for p in (ROOT/'ghidra').iterdir() if p.is_file() and p.suffix in ['.txt','.bin']),key=lambda p:p.name)
    with zipfile.ZipFile(archive,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
        for p in sources:
            data=p.read_bytes();info=zipfile.ZipInfo(p.name,date_time=(1980,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED
            z.writestr(info,data,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9);members+=1;uncompressed+=len(data)
    primary=dict(file=archive.name,sha256=sha(archive.read_bytes()),members=members,uncompressed_bytes=uncompressed,bytes=archive.stat().st_size,
        contents='Saved primary .txt and small .bin evidence; includes dated rejected diagnostics. Acceptance belongs to each schema/validation, not file existence.')
    manifest=dict(schema_version=1,export_date='2026-11-10',latest_integrated_phase=193,component_count=len(package['components']),
        documentation_count=sum(x['file'].endswith('.md') for x in files),python_source_count=sum(x['file'].endswith('.py') for x in files),
        files=files,primary_evidence=primary,original_package_sha256=sha(raw),original_package_bytes=len(raw),source_writes=0,
        in_progress=dict(phase=194,topic='Melee direction/backstab',status='Existing54native/6Ghidra scenes and3angle-helper profiles exported; not yet part of the171-component integrated package.'),
        excluded_root_metadata=excluded,excluded_directories=['Game executables and installed game/map files','Full emulator JSON caches (multiple GiB)','Ghidra project, dependencies, process logs and local settings'],
        evidence_scope='Primary snippets are included in ZIP. CPU replay summaries/hashes remain in schemas and manifests; raw emulator caches require the original workspace.',
        current_priority=['ProcessType/DamageType core consolidated with explicit limits','Global parameters: active and incomplete','Creature resistances and flags','Other unknowns'],Crypt='Deferred; ten counter meanings remain open')
    write(dest/'snapshot-manifest.json',json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
    write(dest/'README.md',f'''# DK2 format research snapshot

Снимок от10 ноября2026: **171 компонентов**, {manifest['documentation_count']} документов, {manifest['python_source_count']} Python-исходников. Последний интегрированный этап —193. Начните с [индекса форматов](../../docs/formats/README.md), INTEGRATION_RU.md, RESEARCH_PRIORITIES_RU.md и READINESS_RU.md. Датированная история содержит прежние выводы/очереди; effective-представления и semantic_corrections имеют приоритет над legacy-разметкой.

Полный editor-format-package.json не дублируется в Git. Из корня репозитория, Python3.12+, без игры:

```sh
python tools/assemble_format_package.py
python tools/validate_format_research.py
python -m unittest discover -s tests/FormatResearch -p 'test_*.py'
```

Результат сборки — artifacts/formats/editor-format-package.json, исходный SHA-256 `{sha(raw)}`. Проверяется точное восстановление всех30034144 байт;171 исходных компонентов сохранены побайтово.

primary-evidence.zip содержит {members} файлов первичных инструкций, декомпиляций, проверок памяти и небольших снимков. `python tools/unpack_format_evidence.py` восстанавливает игнорируемую ghidra/ для исторических относительных ссылок. Файл ошибки или проба не означает PASS: см. соответствующие validation и limitations.

Много-GiB сырых CPU-кэшей здесь нет. Их хеши/результаты/условия сохранены в схемах и phase-манифестах. Полное повторение части старых сцен требует исходного workspace, точного EXE, локальной Ghidra и Python-зависимостей. CI проверяет целостность снимка и правила сохранения; он не переисполняет все эксперименты и не доказывает игру/GUI.

Исторические имена, хеши, строки и пути локальных стендов сохранены как происхождение данных. Абсолютные пути старых JSON/скриптов не являются настройками UltimateEditor. Исторические helpers изучайте перед запуском и используйте копии данных. В набор не входят игровые EXE, установленные карты, зависимости, процессные настройки или логи запуска.

Глобальные параметры не завершены. Crypt отложен,10 назначений счётчиков открыты. Остатки резистов/флагов/opaque-полей, версий и World scheduler сохранены; экспорт не повышает процент готовности. Уже выполненные файлы phase194-direction-* и helpers перенесены как текущая работа, пока отдельно от171-компонентного пакета. Разбор возобновляется после публикации этого снимка.
''')
    docs=repo/'docs/formats';layouts=package['effective_catalogue_layouts'];rows=[]
    for family,layout in layouts.items():
        fields=layout.get('fields',[])
        if not fields:continue
        text=f'# {family}: физическая разметка\n\nНавигационная копия effective_catalogue_layouts этапа193; наличие имени не означает независимо доказанный игровой смысл. [Общие правила](../README.md).\n\nРазмер записи: **'+str(layout['record_size'])+' байт**. Варианты/расширения существ см. creature-version-layout-semantics.json.\n\n| Смещение в записи | Размер | Тип | Имя в актуальной схеме |\n|---|---:|---|---|\n'
        for f in fields:text+='| '+hex(f['offset'])+' | '+str(f['size'])+' | `'+str(f['kind']).replace('|','\\|')+'` | `'+str(f['name']).replace('|','\\|')+'` |\n'
        text+='\nОграничения, уверенность и исправления — в [машинном индексе](../../../research/dk2-formats/format-package.index.json) и его компонентах. Unknown/legacy-имя остаётся непроверенным до соответствующего доказательства.\n'
        write(docs/'families'/(family+'.md'),text);rows.append(f'- [{family}](families/{family}.md)')
    write(docs/'FAMILIES.md','# Каталоги и другие семейства\n\n'+'\n'.join(rows)+'\n\nAutoSave.kwd — дескриптор Level; GlobalVariables.kwd — записи Variables. См. level-layouts.json, variable-layouts.json, level-descriptor-semantics.json, variable-runtime-map.json и level_descriptor в пакете. AutoSaveMap.kld и AutoSaveThings.kld — отдельные семейства Map/Things. Документация и полный перечень файлов: [архив исследования](../../research/dk2-formats/README.md).\n')
    print(json.dumps(dict(files=len(files),documents=manifest['documentation_count'],python=manifest['python_source_count'],components=len(package['components']),source_bytes=sum(x['bytes'] for x in files),primary=primary),ensure_ascii=False))


if __name__=='__main__':
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('repository',type=Path);export(ap.parse_args().repository)
