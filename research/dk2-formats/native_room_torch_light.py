"""Read native Room intensity bits and its verified renderer normalization."""
def normalized_intensity(value):
    if type(value) is not int or not 0<=value<=255:
        raise ValueError('Expected native unsigned8 intensity')
    return value/128

def view(record):
    if type(record) is not bytes or len(record)!=1055:
        raise ValueError('Expected immutable1055-byte Room record')
    return dict(raw_u8=record[0x36E],opaque_neighbor_u8=record[0x36F],
        renderer_parameter=normalized_intensity(record[0x36E]),
        scope='CLight render-record parameter at30h/34h,not measured finalpixel brightness')
