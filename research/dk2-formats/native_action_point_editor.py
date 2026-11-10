"""Verified AP dialog fields and auto-ID command, not global file restrictions."""
from native_placed_structures import checked
MASKS=(16,1,32,64)
CONTROL_IDS=(1836,1840,1841,1842)

def panel_fields(flags_byte,wait_word):
    checked(flags_byte,8);checked(wait_word,16)
    return dict(wait=wait_word,reveal=int(bool(flags_byte&16)),hero_lair=int(bool(flags_byte&1)),tool_box=int(bool(flags_byte&32)),ignore_solid=int(bool(flags_byte&64)))

def apply_panel(initial_flags,wait_dword,checkbox_dwords,next_choices,selected_index):
    checked(initial_flags,8);checked(wait_dword,32)
    if len(checkbox_dwords)!=4:raise ValueError('Expected reveal/lair/tool/ignore DWORDs')
    for n in checkbox_dwords:checked(n,32)
    for n in next_choices:checked(n,8)
    if type(selected_index) is not int or not 0<=selected_index<len(next_choices):raise ValueError('Invalid selection index; native has no bounds check here')
    flags=initial_flags&~0x71
    for n,mask in zip(checkbox_dwords,MASKS):
        if n==1:flags|=mask
    return dict(flags=flags,wait_word=wait_dword&65535,next_id=next_choices[selected_index])

def auto_id(occupied_ids,previous_id=0):
    checked(previous_id,8)
    for n in occupied_ids:checked(n,8)
    used=set(occupied_ids)
    for candidate in range(1,81):
        if candidate not in used:return dict(return_byte=candidate,model_id=candidate,success=True)
    return dict(return_byte=0,model_id=previous_id,success=False)

def next_choices(raw_ids,current_id):
    checked(current_id,8)
    for n in raw_ids:checked(n,8)
    return [0]+[n for n in raw_ids if n!=current_id]

def selection_index(choices,target_id):
    checked(target_id,8)
    for n in choices:checked(n,8)
    return choices.index(target_id) if target_id in choices else 0
