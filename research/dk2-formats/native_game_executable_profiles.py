"""Resolve only the two byte-verified static definition/code profiles.

Does not equate Windows DLL loading, runtime memory patches or whole gameplay.
Preserves actual input hash separately from the inherited projection profile.
"""
DX='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
NORMAL='e99818fbe9ad337ab4a24fa76901d5fe5a8c0a0dd306172d24b9547762c534cc'

def static_definition_profile(source_sha256):
    if type(source_sha256) is not str or source_sha256 not in [DX,NORMAL]:
        raise ValueError('Select exact inspected normal/DX source SHA;other binaries not certified')
    return dict(source_sha256=source_sha256,projection_profile_sha256=DX,
        import_library='PATCH.dll' if source_sha256==DX else 'DDRAW.dll',
        scope='Static identical native definition/format code;notwholeWindowsruntime behavior')
