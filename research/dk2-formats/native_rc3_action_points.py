"""RC3 AP panel with proven HeroGate08 checkbox;notgameplaysemantics."""
from native_placed_structures import checked
MASKS=(8,16,1,32,64)
CONTROL_IDS=(1835,1836,1840,1841,1842)
def panel_fields(flags_byte,wait_word):
    checked(flags_byte,8);checked(wait_word,16)
    return dict(wait=wait_word,checkboxes=[int(bool(flags_byte&m)) for m in MASKS])
def apply_panel(initial_flags,wait_dword,checkbox_dwords,next_choices,selected_index):
    checked(initial_flags,8);checked(wait_dword,32)
    if len(checkbox_dwords)!=5:raise ValueError('Expected HeroGate/reveal/lair/tool/ignore DWORDs')
    for n in checkbox_dwords:checked(n,32)
    for n in next_choices:checked(n,8)
    if type(selected_index) is not int or not 0<=selected_index<len(next_choices):raise ValueError('Expectedvalidselectionindex')
    flags=initial_flags&~0x79
    for n,m in zip(checkbox_dwords,MASKS):
        if n==1:flags|=m
    return dict(flags=flags,wait_word=wait_dword&65535,next_id=next_choices[selected_index])
def patch_hero_gate(raw,enabled):
    from native_action_points import record_view,patch_raw
    v=record_view(raw)
    if type(enabled) is not bool:raise ValueError('Expectedbool')
    return patch_raw(raw,18,v['flags']|8 if enabled else v['flags']&~8)
