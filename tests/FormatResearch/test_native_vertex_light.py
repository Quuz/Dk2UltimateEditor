"""Portable audit of the published native vertex-light supplement; no game execution."""
from pathlib import Path
import hashlib,json,unittest,ast

class NativeVertexLightEvidence(unittest.TestCase):
    def test_evidence_and_coverage(self):
        root=Path(__file__).resolve().parents[2]/'research/dk2-formats/native-vertex-lighting'
        manifest=json.loads((root/'manifest.json').read_text(encoding='utf-8'))
        for row in manifest['files']:
            path=root/row['file']
            self.assertEqual(path.parent,root)
            data=path.read_bytes()
            self.assertEqual(len(data),row['bytes'])
            self.assertEqual(hashlib.sha256(data).hexdigest(),row['sha256'])
            if path.suffix=='.py':ast.parse(data.decode('utf-8'))
        cases=json.loads((root/'cases.json').read_text())
        semantics=json.loads((root/'semantics.json').read_text())
        self.assertEqual(cases['source_sha256'],semantics['profile']['sha256'])
        self.assertEqual(len(cases['cases']),80)
        keys={(r['distance'],r['intensity'],r['facing']) for r in cases['cases']}
        self.assertEqual(len(keys),80)
        self.assertEqual({r[1] for r in keys},{0,64,128,255})
        self.assertEqual({r[2] for r in keys},{-1,1})
        for row in cases['cases']:
            self.assertGreater(row['instructions'],0)
            for actual,channel in zip(row['rgb'],[64,128,255]):
                self.assertAlmostEqual(actual,channel*row['weight'],delta=.0002)
            if row['distance']>=1.5:self.assertEqual(row['rgb'],[0,0,0])

if __name__=='__main__':unittest.main()
