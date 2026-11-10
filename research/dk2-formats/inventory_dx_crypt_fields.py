"""Reuse narrow literal inventory for current DX; no pointer type inference."""
from pathlib import Path
import inspect,json
import inventory_old_crypt as old
ROOT=Path(__file__).resolve().parent

def main():
    source=inspect.getsource(old.main)
    replacements=[("file=ROOT/'source-binaries/DKII-local13.exe'","file=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'"),
        ('93e826a142f677ef5120699feddb0572799d93399167ef1d05ec4b48a8b2a342','d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'),
        ('phase189-old-crypt-inventory.json','phase189-dx-crypt-inventory.json')]
    for a,b in replacements:assert source.count(a)==1,a;source=source.replace(a,b)
    scope=dict(vars(old));exec(compile(source,str(ROOT/'inventory_dx_crypt_fields.py')+':shared-inventory','exec'),scope);scope['main']()
    file=ROOT/'phase189-dx-crypt-inventory.json';result=json.loads(file.read_text())
    result['scope']='CurrentDXdirectliteraloperands; typesnotinferred. Indexed/shiftedbasesandvirtualreturnsnotcovered. Noabsenceofuseclaim.'
    file.write_text(json.dumps(result,indent=2),encoding='utf-8')
    unknown={0x108A,0x108B,0x1092,0x1093,0x1096,0x1097,0x1098,0x1099,0x109A,0x109B}
    hits=[r for r in result['direct_counter_offset_hints'] if r['bytes']==1 and r['read'] and int(r['displacement'],16) in unknown]
    print('untypedunknownbyte-readsites',[(r['address'],r['instruction'],r['nearest_direct_CALL_target_hint']) for r in hits])

if __name__=='__main__':main()
