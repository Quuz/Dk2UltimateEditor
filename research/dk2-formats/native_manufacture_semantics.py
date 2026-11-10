"""Read-only native manufacture projections. Runtime queues are not KWD records."""
from pathlib import Path
from math import gcd
import json
import dk2_formats as fmt

SCHEMA=json.loads((Path(__file__).resolve().parent/'manufacture-semantics.json').read_text(encoding='utf8'))

def integer(value,low,high,name):
    if type(value) is not int or not low<=value<=high:raise ValueError(f'{name} must be integer in {low}..{high}')

def manufacture_record_projection(family,record):
    spec=next((r for r in SCHEMA['manufacture_fields'] if r['family']==family),None)
    if spec is None:raise ValueError('Only Doors and Traps manufacture records are supported')
    if not isinstance(record,(bytes,bytearray)) or len(record)!=spec['record_bytes']:raise ValueError('Unexpected record length')
    return dict(family=family,kind=spec['kind'],catalogue_id=record[spec['id_offset']],
        crate_object_id=record[spec['crate_object_id_offset']],required_points_u16=fmt.u16(record,int(spec['storage_path']['raw_offset'],16)))

def manufacture_contribution(runtime_rate,level,level_multiplier_s16):
    integer(runtime_rate,0,65535,'runtime rate');integer(level,1,10,'ordinary creature level');integer(level_multiplier_s16,-32768,32767,'level multiplier')
    if level==1:return runtime_rate
    product=runtime_rate*level_multiplier_s16
    scaled=abs(product)//100*(1 if product>=0 else -1)
    return min(65534,max(0,scaled))

def creature_manufacture_projection(record,ticks_per_second,*,level=1,level_multiplier_s16=100):
    if not isinstance(record,(bytes,bytearray)) or len(record) not in (5449,5537):raise ValueError('Expected native Creatures record variant')
    integer(ticks_per_second,1,0x7FFFFFFF,'positive signed TPS')
    raw=fmt.u16(record,0xF0A);runtime=0 if raw==0 else max(1,raw//ticks_per_second)
    return dict(raw_points_per_second=raw,ticks_per_second=ticks_per_second,runtime_points_per_contribution=runtime,
        contribution=manufacture_contribution(runtime,level,level_multiplier_s16),level=level,
        level_multiplier_s16=level_multiplier_s16,scope='Raw load followed by one ordinary level contribution; runtime stat overrides and actual scheduling are not applied.')

def manufacture_queue_projection(queue,progress,contribution=0):
    if not isinstance(queue,(bytes,bytearray)) or len(queue)!=2056:raise ValueError('Expected2056-byte runtime queue snapshot')
    integer(progress,0,65535,'progress');integer(contribution,0,65535,'contribution')
    head=queue[0x801];tail=queue[0x802];count=queue[0x803]
    if head>127 or tail>127 or count>128:raise ValueError('Invalid native queue indices/count')
    p=(progress+contribution)&65535;first=fmt.u16(queue,0);nonempty=count!=0
    head_required=fmt.u16(queue,head*16) if nonempty else None
    return dict(progress_u16=p,addition_wrapped=progress+contribution>65535,queue_count=count,head_index=head,
        native_threshold_slot=0,native_required_u16=first,current_head_required_u16=head_required,
        threshold_differs_from_head=nonempty and first!=head_required,
        completed=nonempty and p>=first,percent=0 if not nonempty else None if first==0 else p*100//first,
        percent_divide_by_zero=nonempty and first==0,
        scope='Completion/percent and low16 progress addition only; no placement, dequeue, spawn or global worker scheduling.')

def constant_contribution_reachability(required,contribution,*,start=0):
    """Arithmetic-only scenario before any reset or intervention by other workers."""
    for value,name in [(required,'required'),(contribution,'contribution'),(start,'start')]:integer(value,0,65535,name)
    if contribution==0:
        return dict(reachable=start>=required,period=1,maximum_progress=start)
    divisor=gcd(contribution,65536);maximum=start%divisor+65536-divisor
    return dict(reachable=maximum>=required,period=65536//divisor,maximum_progress=maximum)
