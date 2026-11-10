"""Lossless patches for four separately preserved84-byte Room resource blocks.

This storage helper does not promise a gameplay effect. The inspected loader
only reads these spans for its intermediate checksum in the accepted paths.
"""
SPANS=[0x11C,0x170,0x1C4,0x218]

def patch_resource(raw,offset,resource):
    if type(raw) is not bytes or len(raw)!=1055 or type(resource) is not bytes or len(resource)!=84:
        raise ValueError('Expected one1055-byte Room and84explicit resource bytes')
    if type(offset) is not int or offset not in SPANS:raise ValueError('Unsupported resource span')
    return raw[:offset]+resource+raw[offset+84:]
