using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class SaveProjectionOracleChecks
{
    private const uint Opaque=0xEDAD2300;
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    public static void Run(Action<string,Action> run)
    {
        var directory=Path.Combine(Path.GetTempPath(),"SaveProjectionOracle",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        var source=NativeMapDocument.CreateNewMap(new("Oracle","Oracle",24,24));var basis=LegacySectionFile.Read(source.SaveCopy(Path.Combine(directory,"basis")));
        var preserved=new LegacySection(Opaque,47,new byte[]{0,255,19,55,128,1,0,201});
        var original=basis.Sections.Append(preserved).ToArray();
        (NativeMapDocument Doc,SaveProjectionChecks.CaptureState Before,string Saved) Fixture(string name,IEnumerable<LegacySection>? sections=null)
        {
            var path=Path.Combine(directory,name+".dkm");File.WriteAllBytes(path,LegacySectionFile.Create(sections??original).ToArray());
            var doc=NativeMapDocument.Load(path);var before=SaveProjectionChecks.Capture(doc);var saved=doc.SaveCopy(Path.Combine(directory,name+"-output"));
            SaveProjectionChecks.Complete(doc,before,saved);SaveProjectionChecks.RoundTrip(doc,NativeMapDocument.Load(saved),before.Files);return(doc,before,saved);
        }
        void Refuse((NativeMapDocument Doc,SaveProjectionChecks.CaptureState Before,string Saved) fixture,byte[] corrupt)
        {
            var valid=File.ReadAllBytes(fixture.Saved);File.WriteAllBytes(fixture.Saved,corrupt);var rejected=false;
            try{SaveProjectionChecks.Complete(fixture.Doc,fixture.Before,fixture.Saved);}catch(InvalidOperationException){rejected=true;}
            finally{File.WriteAllBytes(fixture.Saved,valid);}
            Require(rejected,"Strict source projection oracle accepted altered output.");
            SaveProjectionChecks.Complete(fixture.Doc,fixture.Before,fixture.Saved);
        }
        int Position(byte[] bytes,uint signature,int occurrence=0)
        {
            for(var offset=12;offset<bytes.Length;offset+=12+checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset+8))))
                if(BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset))==signature&&occurrence--==0)return offset;
            throw new InvalidOperationException("Expected fixture section absent.");
        }
        run("source save oracle complete projection, runtime fields, exact reopened envelope and caller snapshot",()=>{_ = Fixture("valid");});
        run("source save oracle refuses one altered non-statistics opaque byte",()=>
        {var f=Fixture("opaque-byte");var bytes=File.ReadAllBytes(f.Saved);bytes[Position(bytes,Opaque)+12]^=1;Refuse(f,bytes);});
        run("source save oracle refuses altered first KSTS save counter",()=>
        {var f=Fixture("statistics-byte");var bytes=File.ReadAllBytes(f.Saved);bytes[Position(bytes,ActivityStatisticsPayload.Signature)+12+44]^=1;Refuse(f,bytes);});
        run("source save oracle refuses dropped, added and reordered sections",()=>
        {
            var f=Fixture("section-order");var sections=LegacySectionFile.Read(f.Saved).Sections;
            Refuse(f,LegacySectionFile.Create(sections.Where(section=>section.Signature!=Opaque)).ToArray());
            Refuse(f,LegacySectionFile.Create(sections.Append(new(0xABCD2345,77,new byte[]{3,2,1}))).ToArray());
            var reordered=sections.ToArray();(reordered[0],reordered[1])=(reordered[1],reordered[0]);Refuse(f,LegacySectionFile.Create(reordered).ToArray());
        });
        run("source save oracle preserves later future KSTS and refuses its corruption",()=>
        {
            var f=Fixture("duplicate",original.Append(new(ActivityStatisticsPayload.Signature,99,new byte[]{4,3,2,1,0,255})));
            var bytes=File.ReadAllBytes(f.Saved);bytes[Position(bytes,ActivityStatisticsPayload.Signature,1)+12]^=1;Refuse(f,bytes);
        });
        run("source save oracle materializes missing first KSTS without losing opaque order or optional defaults",()=>
        {
            var f=Fixture("missing",original.Where(section=>section.Signature!=ActivityStatisticsPayload.Signature));
            var sections=LegacySectionFile.Read(f.Saved).Sections;Require(sections[^1].Signature==ActivityStatisticsPayload.Signature&&sections[^2].Signature==Opaque,"Missing KSTS displaced original opaque tail.");
        });
        run("source save oracle preserves unsupported first KSTS complete bytes",()=>
        {
            var f=Fixture("future",original.Select(section=>section.Signature==ActivityStatisticsPayload.Signature?new LegacySection(section.Signature,99,new byte[]{4,3,2,1,0,255}):section));
            Require(f.Before.Statistics is null&&File.ReadAllBytes(f.Saved).AsSpan().SequenceEqual(f.Before.Project),"Future first KSTS was guessed or normalized.");
        });
    }
}
