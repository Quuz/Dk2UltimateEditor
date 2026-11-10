"""DX AP trigger predicates; unsigned WORD counts, signed DWORD comparisons."""
import struct
import copy
OPS={1:'<',2:'<=',3:'==',4:'>',5:'>=',6:'!='}
def s32(v):return (v&0xFFFFFFFF)-(0x100000000 if v&0x80000000 else 0)
def compare(left,operator,right):
    for v in [left,operator,right]:
        if type(v) is not int or not 0<=v<=0xFFFFFFFF:raise ValueError('Expected DWORD bit patterns')
    a,b=s32(left),s32(right)
    return {1:a<b,2:a<=b,3:a==b,4:a>b,5:a>=b,6:a!=b}.get(operator,False)
def evaluate(kind,params,*,entity_count=0,owned_count=0,marked_count=0,taggable_count=0,area=0,possessed=False):
    """Parameter routing only; caller supplies measured native-helper counts.

    Bytes2/3 must be preserved even where the baseline calls them reserved.
    No map scan/navigation, no clamp or rewriting is performed here.
    """
    if type(kind) is not int or not 0<=kind<=255 or not isinstance(params,(bytes,bytearray)) or len(params)!=8:raise ValueError('Expected condition BYTE and8 parameter bytes')
    if any(type(v) is not int or not 0<=v<=0xFFFFFFFF for v in [entity_count,owned_count,marked_count,taggable_count,area]):raise ValueError('Expected DWORD count bit patterns')
    if type(possessed) is not bool:raise ValueError('Expected boolean existence result')
    op,player,target,target_type=params[:4];threshold=struct.unpack_from('<I',params,4)[0]
    if kind==0x46:return bool(possessed)
    if kind==0x27:count=entity_count
    elif kind in [0x28,0x2A]:count=owned_count
    elif kind==0x29:count=owned_count;op=3;threshold=area
    elif kind==0x3E:count=marked_count
    elif kind==0x45:count=marked_count;op=5;threshold=taggable_count&65535
    else:raise ValueError('Condition forwarded to generic world dispatcher')
    return compare(count&65535,op,threshold)

def apply_layout_overrides(layouts):
    """Refine existing source-derived fields with exact DX consumer evidence."""
    for ident in [39,40,41,42,62,69,70]:
        row=layouts['0xD5'][str(ident)]
        row['baseline_fields']=copy.deepcopy(row['fields']);row['fields']=copy.deepcopy(row['fields'])
        if ident in [40,41]:
            row['fields']=[f for f in row['fields'] if f['offset']!=2]
            row['fields'] += [dict(name='terrainId',offset=2,offset_hex='0x2',size=1,kind='u8'),dict(name='reserved_3',offset=3,offset_hex='0x3',size=1,kind='bytes')]
            row['fields'].sort(key=lambda f:f['offset'])
        row['confidence']='native_DX_AP_condition_dispatch_and_parameter_routing;baseline_names_retained'
        row['native_evidence']='action-point-condition-semantics.json;DX4B0D70'
        row['native_note']={39:'TargetType6 selects objectkind2;all other values select creaturekind0. Player/target zero disable respective filter. Count low16 then signedDWORDcomparison.',40:'Byte2 is live TerrainId filter;it was incorrectly classed as reserved. Player0/terrain0 returns low16(area),otherwise native tile filters apply.',41:'Byte2 is live TerrainId filter. Forces comparison3 equality against full32-bit inclusivearea;ignores rawcomparison/value. Count narrowed to16bits.',42:'Same native count helper/branch as type40;supports TerrainId at byte2.',62:'Count player marker-list types1or2,not arbitrary visible tiles;low16 then signedDWORDcomparison.',69:'Forces comparison5 >=;threshold is low16 number of TerrainBYTE17B&8 cells. Ignores rawcomparison/value. Empty eligible set passes with count0.',70:'Returns existence of matched possessed creaturekind0 bit3E6/80 or objectkind2 bit146/8;ignores comparison/value.'}[ident]
    return layouts
