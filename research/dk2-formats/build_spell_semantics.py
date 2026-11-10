"""Version-specific native spell selectors, timers and research evidence."""
from pathlib import Path
import hashlib, json, re

ROOT=Path(__file__).resolve().parent

def initial_assignments(file):
    text=(ROOT/'ghidra'/file).read_text(encoding='utf8')
    assignments={}
    for m in re.finditer(r'_?DAT_([0-9a-f]+) = ([^;]+);',text):
        rhs=m[2].strip()
        pointer=re.fullmatch(r'&?(?:LAB|DAT|FUN)_([0-9a-f]+)',rhs)
        try:value=int(pointer[1],16) if pointer else int(rhs,0)
        except ValueError:continue
        assignments[int(m[1],16)]=value
    return assignments

def table(base,count,stride,enum_name,file,consumer,kind_byte=False):
    assignments=initial_assignments(file)
    enums=json.loads((ROOT/'enums.json').read_text(encoding='utf8'))[enum_name]
    rows=[]
    for i in range(count):
        address=base+i*stride
        assert assignments[address+16]==i,(file,i)
        row=dict(value=i,baseline_name=enums[str(i)],callback=hex(assignments[address]),
            runtime_entry_address=hex(address))
        if kind_byte:row['expects_entity_target']=bool(assignments[address+17])
        rows.append(row)
    return dict(runtime_address=hex(base),entry_bytes=stride,entry_count=count,enum=enum_name,
        initializer_evidence=file,consumer=consumer,out_of_range_behavior='return0; bounds checked before indexing',entries=rows)

def assembly(file,first,last):
    lines=(ROOT/'ghidra'/file).read_text(encoding='utf8').splitlines()
    selected=[x for x in lines if re.match(r'^[0-9a-fA-F]{8}:',x) and first<=int(x[:8],16)<=last]
    assert selected and int(selected[0][:8],16)==first
    return dict(file=file,first=hex(first),last=hex(last),instructions=selected)

def main():
    alternatives=table(0x6E4AC8,6,17,'CreatureSpell.AlternativeShot',
        'game-creaturespell-alternative-init.txt','0x49A7B0')
    casts=table(0x6FCDD8,6,17,'KeeperSpell.CastRule',
        'game-keeperspell-cast-init.txt','0x5176B0')
    targets=table(0x6FCE40,7,18,'KeeperSpell.TargetRule',
        'game-keeperspell-target-init.txt','0x517730',True)
    checks={
        0:dict(role='no callback; returns0'),
        1:dict(role='eligibility predicate',predicate='creature.timer+459 ==0',evidence='game-alternative-haste.txt'),
        2:dict(role='eligibility predicate',predicate='s32(currentHP)<threshold; level1:baseU16HP>>1; otherwise:(truncTowardZero(s16Coefficient*baseU16HP/100)&FFFFFFFF)>>1',
            evidence='game-alternative-heal.txt',assembly_evidence='game-alternative-heal-assembly.txt'),
        3:dict(role='eligibility predicate',predicate='effective AI-state category from0x478050 ==0x16 and WORDtimer+443 ==0',evidence='game-alternative-armour.txt'),
        4:dict(role='eligibility predicate',predicate='effective AI-state category from0x478050 ==0x16 and WORDtimer+445 ==0',evidence='game-alternative-invulnerable.txt'),
        5:dict(role='eligibility predicate',predicate='effective AI-state category from0x478050 ==0x16 and WORDtimer+44F ==0',evidence='game-alternative-invisible.txt')}
    for row in alternatives['entries']:row.update(checks[row['value']])
    alternatives['out_of_range_behavior']='With a valid readable definition, selector6..255 returns0 before indexing. Missing definition is not checked before reading selector+45.'
    if (ROOT/'spell-predicate-semantics.json').exists():
        alternatives['native_runtime_review']=dict(file='spell-predicate-semantics.json',sha256=hashlib.sha256((ROOT/'spell-predicate-semantics.json').read_bytes()).hexdigest())
    for row in casts['entries']:
        row['evidence_file']={0:None,1:'game-cast-own-land.txt',2:'game-cast-own-neutral-land.txt',
            3:'game-cast-enemy-land.txt',4:'game-cast-any-land.txt',5:'game-cast-anywhere.txt'}[row['value']]
    for row in targets['entries']:
        row['evidence_file']={0:None,1:'game-target-own-creatures.txt',2:'game-target-enemy-creatures.txt',
            3:'game-target-all-creatures.txt',4:'game-target-land.txt',5:'game-cast-anywhere.txt',6:'game-target-possession.txt'}[row['value']]
    rules=[
        dict(id='keeper_research_time_truncation',family='KeeperSpells',fields=['ResearchTime','BonusRTime'],
            description='Native loader uses 16-bit IMUL then logical SHR12. Result=(rawU16*low16(TPS) mod65536)>>12 and lies in0..15. It is not a direct research-point cost.',
            evidence=assembly('game-keeperspell-load-assembly.txt',0x4F7D78,0x4F7DA8)),
        dict(id='keeper_recharge_time_32bit',family='KeeperSpells',fields=['RechargeTime'],
            description='Result=(rawU32*TPS mod2^32)>>12; multiplication precedes logical shift.',
            evidence=assembly('game-keeperspell-load-assembly.txt',0x4F7D62,0x4F7D75)),
        dict(id='research_order_threshold',family='Variables',fields=['ResearchPointsRaw'],
            description='Type17 stores category/id/points in12-byte entries.0x507040 compares accumulated points with entry+8;0x507060 uses the same threshold for percentage. Player0x4C2DC0 calls the comparator; not KeeperSpells.ResearchTime.',
            evidence_files=['game-research-variable.txt','game-research-add.txt','game-research-current-entry.txt',
                'game-research-advance-entry.txt','game-player-research-completion.txt','game-player-research-percentage.txt']),
        dict(id='alternative_is_predicate',family='CreatureSpells',fields=['AlternativeShot'],
            description='RawEE chooses a suitability predicate on creature this/ECX, not a replacement ShotId or buff application. With a readable definition, selector6..255 returns0; the getter result is not checked for null before selector access. RawE8/E9 gameplay remains unresolved.',
            evidence_files=['game-spell-consumer-49a7b0.txt','game-alternative-dispatch-assembly.txt']),
        dict(id='keeper_dispatch_bounds',family='KeeperSpells',fields=['CastRule','TargetRule'],
            description='CastRule>=6 and TargetRule>=7 are rejected with return0, unlike the unchecked Effects generation table.',
            evidence_files=['game-spell-consumer-5176b0.txt','game-spell-consumer-517730.txt',
                'game-cast-dispatch-assembly.txt','game-target-dispatch-assembly.txt']),
        dict(id='target_rule_entity_kind',family='KeeperSpells',fields=['TargetRule'],
            description='Target-table byte+17 is returned by0x5177B0; true for1,2,3,6, false for0,4,5. These are entity-target modes.',
            evidence_files=['game-keeperspell-target-init.txt','game-spell-consumer-5177b0.txt']),
        dict(id='own_land_includes_alliance',family='KeeperSpells',fields=['CastRule'],
            description='Rule1 calls player relation helper0x4BF3D0: self is allowed, other owners use the alliance byte. It separately excludes the neutral/special player and applies terrain/room restrictions.',
            evidence_files=['game-cast-own-land.txt','game-player-relation-allow.txt']),
        dict(id='upgraded_shot_tuple',family='KeeperSpells',fields=['ShotTypeId','ShotData1','ShotData2','BonusShotTypeId','BonusShotData1','BonusShotData2'],
            description='Phase53 correction: player spell-state DWORD0D selects normal/bonus ShotId for nonzero check/definition lookup and selects Data1/Data2. Full DX4BBF90/4BC290 then reload primary ShotId(runtime42/rawF0) for actual factory4B7CD0, even when bonus is selected. See keeper-cast-semantics.json and120 full-wrapper checks. Native spell IDs4,5,14 also have explicit special cases in validation/cost; this does not establish CreatureSpells AlternativeShot behavior.',
            evidence_files=['game-spell-consumer-4bb8a0.txt','game-spell-consumer-4bbf90.txt',
                'game-spell-consumer-4bc290.txt','game-keeperspell-creature-cast-assembly.txt','game-keeperspell-object-cast-assembly.txt'])]
    paths=json.loads((ROOT/'scalar-storage-paths.json').read_text(encoding='utf8'))['families']
    result=dict(executable_sha256=hashlib.sha256((ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()).hexdigest(),
        scope='Native storage/selector evidence; original game was not run with modified files.',
        runtime_definition_bytes={'CreatureSpells':70,'KeeperSpells':122},
        dispatch_tables={'creature_alternative':alternatives,'keeper_cast':casts,'keeper_target':targets},
        storage_paths={family:paths[family] for family in ['CreatureSpells','KeeperSpells']},
        rules=rules,
        not_transferred_to_runtime=[dict(family='CreatureSpells',raw_offset=0xE8,size=1,baseline_name='AlternativeShotId',
                intermediate_offset=0x138,note='Copied by converter, absent from inspected70-byte runtime loader.'),
            dict(family='CreatureSpells',raw_offset=0xE9,size=1,baseline_name='AlternativeRoomId',
                intermediate_offset=0x139,note='Copied by converter, absent from inspected70-byte runtime loader.')],
        not_read_by_converter=[dict(family='CreatureSpells',raw_offset=0xEF,size=27,note='Preserve all27 bytes; not a command to zero them.'),
            dict(family='KeeperSpells',raw_offset=0xE0,size=2,note='Previously named Xe0Unreferenced; preserve bytes.')],
        unresolved=[dict(family='KeeperSpells',field='Xc8',raw_offset=0xC8,runtime_offset=0x3A,
            note='Storage confirmed; gameplay consumer and meaning unresolved. Do not label it research cost or mana cost.'),
            'Room-specific exceptions and call flag bits in target rules are not fully named.',
            'ResearchTime/BonusRTime native storage transform is proven; all other consumers of those runtime slots have not been exhaustively ruled out.'])
    for t in result['dispatch_tables'].values():
        for row in t['entries']:
            if row.get('evidence_file'):assert (ROOT/'ghidra'/row['evidence_file']).exists()
    (ROOT/'spell-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Spell schema:',sum(t['entry_count'] for t in result['dispatch_tables'].values()),'selector entries;',len(rules),'native rules')

if __name__=='__main__':main()
