"""Publish the exact stock/Pro boundary without inventing raw field meanings."""
import json
from scan_editor_variants import ROOT, build, sha
from prepare_editor_variant_primary import prepare
from build_possession_damage import source_executables


def load(name):
    return json.loads((ROOT/name).read_text(encoding='utf8'))


def link(name):
    return dict(file=name, sha256=sha((ROOT/name).read_bytes()))


def schema():
    evidence=load('phase117-editor-variant-evidence.json')
    assert evidence == build()
    primary=load('phase117-primary-manifest.json')
    assert primary == prepare()
    prior=ROOT/'phase117-prior-components.json'
    if not prior.exists():
        components=load('editor-format-package.json')['components']
        assert len(components)==95
        prior.write_text(json.dumps(components,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==95
    return dict(phase=117,date='2026-10-29',family='Original editor 2.0 and fan Pro profile boundary',
        profiles=evidence['profiles'],evidence=link('phase117-editor-variant-evidence.json'),
        primary=link('phase117-primary-manifest.json'),
        equivalence=dict(stock_alias='DK2 Editor.exe and DK2 Editor 2000.exe are byte-identical supplied files.',
            code_changes=14,changed_code_bytes=28,dialog_resource_switches=13,
            other_code_change='9-byte stock block at492603 replaced by a jump andfourNOPs.',
            all_other_text_bytes_equal=True,
            resource_leaf_pairs=281,identical_resource_leaf_pairs=280,
            only_changed_resource_path=[5,100,2057],dialog_pairs=115,identical_dialog_pairs=114,
            about='Stock About says Version2.0; Pro says Version2.02pro and Modified byfans,2007–2008.',
            inherited_Ghidra_range_pairs=232,identical_inherited_ranges=231,
            one_range_difference='Constructor442C40 changes only dialog-ID immediate byte442C49.'),
        dialog_selection=evidence['dialog_switches'],
        visibility='Counts describe WS_VISIBLE/WS_DISABLED in selected templates only; not a live proof of final control visibility or permission to edit.',
        serialization_consequence='Outside the enumerated dialog-ID immediates and save branch, .text is byte-identical. Previously attested Pro reader/writer/helper instructions in the231identical ranges also exist byte-identically in suppliedstock2.0. This is shared code,not an independent implementation validating it.',
        save_branch=evidence['save_branch'],
        implementation_policy=['Retain complete raw records/headers/unknown bits; changing selected dialogs does not establish new storage layouts.',
            'Do not interpret Pro as fixes to resistance mechanics, unknown fields or native writer holes: no such additional code edits exist in these exactfiles.',
            'Do not automatically reproduce native writer omissions, truncations or destructive overwrite behaviour.',
            'Pro still displays the official-name warning on this path; the jump bypasses the following stock abort arm,not all subsequent checks.',
            'Template labels and sharedcallbacks do not identify the gameplay meaning of Terrain198/224,Traps204,Doors236,Objects34C,CreatureJump/Priority,AP04/08 orElementB1/B2.'],
        source_executables_unchanged=source_executables(),
        helpers=[link(n) for n in ['scan_editor_variants.py','prepare_editor_variant_primary.py','extract_editor_dialogs.py']],
        verification=dict(complete_binary_comparisons=2,resource_leaf_pairs=281,parsed_dialog_pairs=115,
            new_Ghidra_CPU_controls=0,new_native_GUI_saves=0,new_container_rebuilds=0,
            explanation='No raw codec orfieldpatch changed. Earlier container checks are historical and are not recounted as new verification.'),
        limitations=['Exact supplied stock/Pro hashes only; RC3 is a different build and is not equated with them.',
            'Static whole-PE/resource comparison and cached Ghidra byte attestation; no new original GUI interaction or save execution.',
            'Shared code may contain shared bugs. Identical writers are not independent corroboration of intended file semantics.',
            'New branches do not establish that overwriting an official level fully succeeds or that all save checks are bypassed.',
            'No new unknown raw-field gameplay meaning assigned; substantial15-family questions remain open.'])


if __name__=='__main__':
    value=schema()
    (ROOT/'editor-variant-semantics.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print('Phase117: exact stock/Pro boundary;14 code patches,281 resource pairs,232 inherited ranges.')
