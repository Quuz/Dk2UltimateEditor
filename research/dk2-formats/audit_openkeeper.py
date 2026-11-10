"""Reproducible lexical audit of pinned OpenKeeper sources; no Java execution."""
from pathlib import Path
import hashlib, json, re

ROOT = Path(__file__).resolve().parent
PIN = '59c1966b80b589cf397476b421fd779527b8e2c4'
SNAPSHOT = ROOT / 'sources' / ('openkeeper-' + PIN[:12])
JAVA = SNAPSHOT / 'src/main/java/toniarts/openkeeper'
MODEL_NAMES = ['Creature', 'CreatureSpell', 'KeeperSpell', 'Door', 'Trap', 'Room',
               'Terrain', 'Shot', 'Effect', 'EffectElement', 'GameObject']

def save(name, data):
    (ROOT / name).write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf8')

def evidence(path, first, last):
    p = JAVA / path
    lines = p.read_text(encoding='utf8').splitlines()
    assert 0 < first <= last <= len(lines)
    return dict(path=path, first_line=first, last_line=last,
                sha256=hashlib.sha256(p.read_bytes()).hexdigest(),
                text='\n'.join(lines[first-1:last]),
                url=f'https://github.com/tonihele/OpenKeeper/blob/{PIN}/src/main/java/toniarts/openkeeper/{path}#L{first}')

def main():
    runtime = {}
    for category in ['game', 'view']:
        for path in (JAVA / category).rglob('*.java'):
            runtime[path] = path.read_text(encoding='utf8').splitlines()
    unknowns = []
    for model in MODEL_NAMES:
        path = JAVA / f'tools/convert/map/{model}.java'
        for n, line in enumerate(path.read_text(encoding='utf8').splitlines(), 1):
            match = re.search(r'public\s+[\w<>\[\], ?]+\s+(get(?:Unk|Unknown)\w*)\s*\(', line)
            if not match:
                continue
            name = match[1]
            hits = []
            for file, lines in runtime.items():
                for line_no, value in enumerate(lines, 1):
                    if re.search(r'\.' + name + r'\s*\(', value):
                        hits.append(dict(path=file.relative_to(JAVA).as_posix(), line=line_no, text=value.strip()))
            unknowns.append(dict(model=model, getter=name, declaration_line=n, lexical_calls_in_game_and_view=hits))
    highlights = [
        dict(id='shot_process_subset', claim='createShot handles four ProcessType values; default warns not implemented.',
             evidence=evidence('game/controller/ShotsController.java', 66, 83)),
        dict(id='spell_attack_placeholder', claim='Creature spell attack calls createShot and also directly applies ShotData1 as damage, with explicit TODO.',
             evidence=evidence('game/controller/creature/CreatureController.java', 668, 674)),
        dict(id='spell_level_filter', claim='The source uses levelAvailable >= level. Treat this as implementation evidence, not a verified native eligibility rule.',
             evidence=evidence('game/controller/CreaturesController.java', 700, 706)),
        dict(id='research_order_in_this_remake', claim='setNextResearchTarget concatenates spells, doors, traps, rooms; upgrades follow discoveries. This method does not consume native Variables type17 records.',
             evidence=evidence('game/controller/player/PlayerResearchControl.java', 119, 156)),
        dict(id='research_cost_linkage', claim='Research uses per-definition ResearchTime, with BonusRTime for upgraded KeeperSpells.',
             evidence=evidence('game/controller/player/PlayerResearchControl.java', 80, 100)),
        dict(id='research_order_unknown_in_parser', claim='Native type17 ResearchOrder is still read as Variable.UNKNOWN_17, grouped with UNKNOWN_0 and UNKNOWN_77.',
             evidence=evidence('tools/convert/map/KwdFile.java', 2966, 2976)),
        dict(id='trigger_links', claim='NextId iterates siblings; ChildId recurses into condition children in this implementation.',
             evidence=evidence('game/trigger/TriggerLoader.java', 48, 90)),
        dict(id='trigger_todo_actions', claim='Several parsed actions still have TODO bodies in the generic trigger executor.',
             evidence=evidence('game/trigger/TriggerControl.java', 289, 323)),
        dict(id='effect_generation_target', claim='GENERATE_EFFECT_ELEMENTS and GENERATE_EFFECTS choose which definition table resolves generateIds.',
             evidence=evidence('view/effect/VisualEffect.java', 276, 286)),
        dict(id='effect_next_timing_todo', claim='NextEffect starts immediately, with TODO about timing; not proof of native scheduling.',
             evidence=evidence('view/effect/VisualEffect.java', 156, 159)),
        dict(id='effect_hp_lifetime', claim='Effect HP is treated as a lifetime in 20 Hz ticks.',
             evidence=evidence('view/effect/EffectControl.java', 65, 81)),
        dict(id='effect_element_hp_lifetime', claim='EffectElement HP is likewise treated as a lifetime in 20 Hz ticks.',
             evidence=evidence('view/effect/EffectElementControl.java', 85, 105)),
        dict(id='effect_origin_units', claim='Horizontal origin range uses raw*23/4096, vertical height raw*16/4096 in tile units.',
             evidence=evidence('view/effect/EffectControl.java', 119, 127)),
        dict(id='effect_spin_units', claim='Spin/orientation use 2048 units per turn, with a 20 Hz conversion for rates.',
             evidence=evidence('view/effect/EffectControl.java', 130, 162)),
        dict(id='effect_whirlpool_signed_low_byte', claim='A u16 file field is cast to signed byte for whirlpool direction in the effect simulation.',
             evidence=evidence('view/effect/EffectControl.java', 184, 199)),
        dict(id='effect_mass_friction', claim='Effect acceleration is parsedMass*25; friction multiplier is (1-16*parsedFriction)^(20*dt). These are remake formulas, not yet native proofs.',
             evidence=evidence('view/effect/EffectControl.java', 239, 245)),
    ]
    layouts = json.loads((ROOT / 'layouts.json').read_text(encoding='utf8'))
    source_semantics = []
    definitions = [
        ('Effects', ['MinHp', 'MaxHp'], 'lifetime seconds = raw/20', 'effect_hp_lifetime'),
        ('EffectElements', ['MinHp', 'MaxHp'], 'lifetime seconds = raw/20', 'effect_element_hp_lifetime'),
        ('Effects', ['OuterOriginRange', 'InnerOriginRange'], 'horizontal tile radius = raw*23/4096', 'effect_origin_units'),
        ('Effects', ['LowerHeightLimit', 'UpperHeightLimit'], 'height in tiles = raw*16/4096', 'effect_origin_units'),
        ('Effects', ['OrientationRange', 'SpriteSpinRateRange'], '2048 angular units per turn; random spread and tick conversion apply', 'effect_spin_units'),
        ('Effects', ['WhirlpoolRate'], 'effective signed low byte in this remake', 'effect_whirlpool_signed_low_byte'),
        ('Effects', ['Mass', 'AirFriction'], 'see effect_mass_friction formulas after fixed-point parsing', 'effect_mass_friction'),
    ]
    for family, fields, interpretation, reference in definitions:
        for field in fields:
            row = next(x for x in layouts[family]['fields'] if x['name'] == field)
            source_semantics.append(dict(family=family, field=field, offset=row['offset'], size=row['size'],
                storage_kind=row['kind'], interpretation=interpretation, evidence_id=reference,
                confidence='OpenKeeper implementation only; not a native-engine override'))
    files = {p.relative_to(SNAPSHOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
             for p in sorted(SNAPSHOT.rglob('*')) if p.is_file()}
    save('openkeeper-source-manifest.json', dict(commit=PIN, files=files))
    save('openkeeper-audit.json', dict(commit=PIN, checked_date='2026-10-22',
        snapshot_files=len(files), runtime_java_files_scanned=len(runtime),
        method='Regex declaration/call search plus manual inspection of cited source ranges; no Java type resolution or gameplay test.',
        unknown_getters=unknowns, highlights=highlights, source_semantics=source_semantics,
        warning='No matching getter calls does not prove a field is unused in native DK2. All raw bytes remain authoritative.'))
    print('OpenKeeper:', len(files), 'files;', len(runtime), 'runtime Java files;',
          len(unknowns), 'unknown getter declarations;', sum(len(x['lexical_calls_in_game_and_view']) for x in unknowns), 'matching calls;',
          len(source_semantics), 'source-only field interpretations')

if __name__ == '__main__':
    main()
