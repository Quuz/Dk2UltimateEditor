"""Physical Creature work-rate fields and bounded per-callback projection."""
FIELDS={'research':0xF08,'manufacture':0xF0A}


def check(raw):
    if type(raw) is not bytes or len(raw) not in [5449,5537]:raise ValueError('Expected5449/5537-byte Creature record')


def view(raw):
    check(raw);return {k:int.from_bytes(raw[o:o+2],'little') for k,o in FIELDS.items()}


def patch(raw,**changes):
    check(raw);b=bytearray(raw)
    for k,v in changes.items():
        if k not in FIELDS or type(v) is not int or not 0<=v<=65535:raise ValueError('Expected known rate field andu16bits')
        o=FIELDS[k];b[o:o+2]=v.to_bytes(2,'little')
    return bytes(b)


def loaded_rate(raw_rate,tps):
    if type(raw_rate) is not int or not 0<=raw_rate<=65535 or type(tps) is not int or not 1<=tps<=0x7FFFFFFF:
        raise ValueError('Expectedu16 rate and positive signed32 TPS')
    return max(1,raw_rate//tps) if raw_rate else 0


def contribution(raw_rate,tps,level=1,coefficient_bits=100):
    if type(level) is not int or not 1<=level<=255 or type(coefficient_bits) is not int or not 0<=coefficient_bits<=65535:
        raise ValueError('Expected byte level1..255 and coefficientu16bits')
    base=loaded_rate(raw_rate,tps)
    if level==1:return base
    coefficient=coefficient_bits-65536 if coefficient_bits&0x8000 else coefficient_bits
    p=base*coefficient;n=(abs(p)//100)*(1 if p>=0 else -1)
    return min(65534,max(0,n))
