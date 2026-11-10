"""Read-only raw/native-editor views of the identified creature byte."""

def check(record):
    if not isinstance(record,(bytes,bytearray)) or len(record) not in [5449,5537]:
        raise ValueError('Expected5449- or5537-byte Creatures record')

def priority_view(record):
    check(record)
    return dict(raw=record[0xF55],native_label='Pick-Up Priority',native_editor_display=float(record[0xF55]),
        game_runtime_byte=record[0xF55],spell_swipe_raw=record[0xF45],gui_validation_range=None,
        gameplay_ordering_confirmed=False)

def commit_converted_integer(value):
    """Project only narrowing AFTER native __ftol. No float validation is inferred."""
    if type(value) is not int or not -(1<<63)<=value<(1<<63):raise ValueError('Pass a signed64 converted integer')
    return value&255
