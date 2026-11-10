"""Bounded projection of native AP ID lookup over an explicit tag table.

Native methods have no cycle guard. Diagnostic cycle rejection here is an API
safety boundary,not a claim that the game detects cycles. Tag0 may map to a node.
"""
def lookup_tag(nodes,head,key):
    if type(head) is not int or not 0<=head<=65535 or type(key) is not int or not 0<=key<=0xFFFFFFFF:raise ValueError('Expected WORD head/DWORD selector')
    for tag,pair in nodes.items():
        if type(tag) is not int or not 0<=tag<=65535 or len(pair)!=2:raise ValueError('Invalid tag row')
        ident,nxt=pair
        if type(ident) is not int or not 0<=ident<=255 or type(nxt) is not int or not 0<=nxt<=65535:raise ValueError('Invalid BYTE ID/WORD next')
    tag=head;seen=set()
    while tag in nodes:
        if tag in seen:raise ValueError('Cycle: native traversal would not finish')
        seen.add(tag);ident,nxt=nodes[tag]
        if ident==(key&255):return tag
        tag=nxt
    return None
