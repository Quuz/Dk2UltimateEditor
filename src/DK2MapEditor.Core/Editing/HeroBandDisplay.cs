using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record HeroBandDisplayMember(int Index,EditorHeroMemberValues Values);

public sealed partial class NativeMapDocument
{
    /// <summary>Physical active prefix for the source band selector. No private
    /// edit session, normalization, scenario identity assignment or map writes.</summary>
    public IReadOnlyList<HeroBandDisplayMember> ReadHeroBandDisplay(NativeRecord record)
    {
        var layout=GetBandLayout(record);
        if(layout.Project)
        {
            var version=record.File.UInt32(4);
            if(version is <1 or >5)throw new InvalidDataException("Unverified hero-band display version.");
            if(version<5)
            {
                // The source reader's defaults belong only to this display snapshot.
                // Use the physical ordinal, since imported IDs need not be unique.
                var converted=new NativeFile(record.File.SourcePath,record.File.ExportPath,UpgradeBands(record.File,(int)version));
                var fields=LegacyProjectSchemas.Fields("MTHBAND");var length=LegacyProjectSchemas.Size("MTHBAND");
                var copy=new NativeRecord(converted,12+(record.Offset-12)/record.Length*length,length,"Project.MTHBAND",fields);
                var start=fields.Single(f=>f.Name=="heros[0].isvalid").Offset;
                var stride=fields.Single(f=>f.Name=="heros[1].isvalid").Offset-start;
                layout=new(copy,start,stride,true,fields.Where(f=>f.Name.StartsWith("heros[0].",StringComparison.Ordinal)).Select(f=>f with{Name=f.Name[9..],Offset=f.Offset-start}).ToArray());
            }
        }
        var required=new (string Project,string Native)[]{("id","bGlobalCreatureID"),("level","bGlobalLevel"),("enemy","bGlobalPlayer"),
            ("health","dwGlobalHealth"),("goldheld","bGlobalGoldHeld"),("options","bHeroOptions"),("options2","bGlobalBehavior"),("objective","bHeroObjective"),("area","bHeroTargetArea")};
        if(required.Any(pair=>layout.Field(layout.Project?pair.Project:pair.Native) is null))throw new InvalidDataException("Unverified hero-band display fields.");
        uint U(int index,string project,string native)=>uint.Parse(layout.Read(index,layout.Project?project:native),CultureInfo.InvariantCulture);
        var count=ReadBandMembers(layout).TakeWhile(member=>member.Active).Count();
        return Array.AsReadOnly(Enumerable.Range(0,count).Select(index=>new HeroBandDisplayMember(index,
            new(U(index,"id","bGlobalCreatureID"),(int)U(index,"level","bGlobalLevel"),U(index,"enemy","bGlobalPlayer"),
                U(index,"health","dwGlobalHealth"),U(index,"goldheld","bGlobalGoldHeld"),U(index,"options","bHeroOptions"),
                U(index,"options2","bGlobalBehavior"),U(index,"objective","bHeroObjective"),U(index,"area","bHeroTargetArea")))).ToArray());
    }
}
