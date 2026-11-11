"""Audit source preservation, exact recipe append and original composed execution."""
import build_temple_file_rewards as b
ROOT,load,dump,sha=b.ROOT,b.load,b.dump,b.sha
def main():
 s=b.build();assert s==load(b.SCHEMA);old=load('phase202-prior-package-state.json');p=load('editor-format-package.json')
 assert len(p['components'])==180 and set(p['components'])==set(old['components'])|{b.KEY}
 assert p[b.KEY]==s and p['components'][b.KEY]==b.link(b.SCHEMA)
 for k,v in old['components'].items():assert p['components'][k]==v
 for k,h in old['preserved_top_level'].items():
  if k!='precedence':assert b.canonical(p[k])==h
 assert p['precedence']==old['precedence']+[b.NOTE]
 assert old['codec']['sha256']==b.link('dk2_formats.py')['sha256']
 doc=(ROOT/'TEMPLE_FILE_REWARDS_RU.md').read_text(encoding='utf8')
 for text in ['621','622','44','5000','Ghidra','не']:assert text in doc
 dump('phase202-validation.json',dict(phase=202,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),documentation=b.link('TEMPLE_FILE_REWARDS_RU.md'),physical_preservation=s['physical_preservation'],prior_components_preserved=179,codec_unchanged=True,goal_status='active',Crypt_deferred=True))
 print('PASS phase202',s['counts'])
if __name__=='__main__':main()
