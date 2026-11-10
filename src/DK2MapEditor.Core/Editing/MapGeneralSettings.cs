using System.Collections.ObjectModel;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record MapGeneralInput(string Field, string Value);
public static class MapGeneralSettings
{
    public static IReadOnlyList<ValueChoice> Versions { get; } = Array.AsReadOnly(new[] {
        new ValueChoice("13", "Original", "Исходная версия"), new("16", "1.61", "1.61"), new("17", "1.70", "1.70") });
    public static IReadOnlyList<ValueChoice> Keepers { get; } = Array.AsReadOnly(Enumerable.Range(1,5)
        .Select(number => new ValueChoice(number.ToString(CultureInfo.InvariantCulture), number.ToString(CultureInfo.InvariantCulture), number.ToString(CultureInfo.InvariantCulture))).ToArray());
    public static IReadOnlyList<string> Fields { get; } = Array.AsReadOnly(new[] { "Name", "Players", "Version" });
    public static string StorageField(bool project, string field) => !Fields.Contains(field) ? throw new ArgumentException("Unknown general setting.")
        : project ? field : field switch { "Name" => "wstrMainName", "Players" => "bMainPlayers", _ => throw new ArgumentException("Native game target is session state, not a game-file field.") };
    internal static bool Verified(NativeField field, string key, bool project) => field.BitMask == 0 && (key switch {
        "Name" => field.Kind == (project ? NativeValueKind.WesternAnsi : NativeValueKind.Unicode) && field.Size == (project ? 64 : 128),
        "Players" => field.Kind == (project ? NativeValueKind.Int32 : NativeValueKind.Byte) && field.Size == (project ? 4 : 1),
        "Version" => project && field.Kind == NativeValueKind.Byte && field.Size == 1, _ => false });
}

public sealed class MapGeneralDraft
{
    internal MapGeneralDraft(NativeMapDocument owner, long revision, byte[] fingerprint, NativeRecord? record,
        IReadOnlyDictionary<string,string> baseline, IReadOnlyDictionary<string,string> values)
    { Owner=owner;Revision=revision;Fingerprint=fingerprint;Record=record;Baseline=baseline;Values=values; }
    internal NativeMapDocument Owner { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    internal IReadOnlyDictionary<string,string> Baseline { get; }
    public NativeRecord? Record { get; }
    public IReadOnlyDictionary<string,string> Values { get; }
    public bool Editable => Values.Count != 0;
    public bool Partial => Values.Count != MapGeneralSettings.Fields.Count;
    public bool Changed => Values.Any(row => row.Value != Baseline[row.Key]);
    internal MapGeneralDraft With(Dictionary<string,string> values) => new(Owner,Revision,Fingerprint,Record,Baseline,new ReadOnlyDictionary<string,string>(values));
}

public sealed partial class NativeMapDocument
{
    private NativeFile? nativeGameTarget;
    private uint originalNativeGameTarget = 17;
    // Transaction-only session state. It is never a Files entry, a linked KWD
    // field, a sidecar or an export. Original native reload defaults to 1.70.
    private NativeFile NativeGameTarget
    {
        get
        {
            if(nativeGameTarget is null) { var bytes=new byte[4];WriteUInt32(bytes,0,17);nativeGameTarget=new(MainFile.SourcePath+"#GameTarget",MainFile.ExportPath+"#GameTarget",bytes); }
            return nativeGameTarget;
        }
    }
    private bool NativeGameTargetChanged => !IsLegacyProject && NativeGameTarget.UInt32(0)!=originalNativeGameTarget;
    private void WriteNativeGameTarget(int value)
    {
        if(IsLegacyProject || value is <0 or >255)throw new ArgumentOutOfRangeException(nameof(value));
        WriteUInt32(NativeGameTarget.Bytes,0,(uint)value);
    }
    private NativeRecord? VerifiedGeneralRecord()
    {
        var records=Records().Where(record=>IsLegacyProject ? record.File.Signature==LegacyInfo : record.File==MainFile && record.Category=="Map").ToArray();
        if(records.Length!=1)return null;var record=records[0];
        return (IsLegacyProject ? record.File.UInt32(4) is >=1 and <=7 && record.Category.StartsWith("Project.MAPPROPERTIES",StringComparison.Ordinal) : record.File.UInt32(4)==4) ? record : null;
    }
    /// <summary>The game compatibility target, never the KWD/section version.
    /// Old project readers supply 17; unknown layouts have no inferred target.</summary>
    public int? TargetGameVersion
    {
        get
        {
            // Catalogue/graph labels ask for this frequently. Read only the
            // verified metadata header/field, never reparse every placed object
            // and definition for each label (quadratic on large maps).
            if(!IsLegacyProject)return MainFile.Signature==0xDC && MainFile.Length>=56 && MainFile.UInt32(4)==4
                ? checked((int)NativeGameTarget.UInt32(0)) : null;
            var info=projectSections.SingleOrDefault(item=>item.Signature==LegacyInfo);if(info is null)return null;
            var version=info.UInt32(4);
            var structure=version switch {1=>"MAPPROPERTIESV1",2 or 3=>"MAPPROPERTIESV2",4=>"MAPPROPERTIESV4",5=>"MAPPROPERTIESV5",6=>"MAPPROPERTIESV6",7=>"MAPPROPERTIES",_=>null};
            if(structure is null || info.Length!=12+LegacyProjectSchemas.Size(structure))return null;
            if(version<=5)return 17;
            var versionField=LegacyProjectSchemas.Fields(structure).SingleOrDefault(item=>item.Name=="Version");
            return versionField is not null && MapGeneralSettings.Verified(versionField,"Version",true)
                ? int.Parse(versionField.Read(info.Data.Span[12..]),CultureInfo.InvariantCulture) : null;
        }
    }
    public MapGeneralDraft CreateMapGeneralDraft()
    {
        var record=VerifiedGeneralRecord();var values=new Dictionary<string,string>();
        if(record is not null)
            foreach(var key in MapGeneralSettings.Fields)
            {
                if(key=="Version" && !IsLegacyProject){values.Add(key,TargetGameVersion!.Value.ToString(CultureInfo.InvariantCulture));continue;}
                var field=record.Fields.SingleOrDefault(field=>field.Name==MapGeneralSettings.StorageField(IsLegacyProject,key));
                if(field is not null && MapGeneralSettings.Verified(field,key,IsLegacyProject))values.Add(key,record.Read(field));
            }
        var view=new ReadOnlyDictionary<string,string>(values);return new(this,EditRevision,UnusedScriptFingerprint(),record,view,view);
    }
    private NativeRecord VerifyMapGeneralDraft(MapGeneralDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if(!ReferenceEquals(draft.Owner,this) || draft.Revision!=EditRevision || !UnusedScriptFingerprint().AsSpan().SequenceEqual(draft.Fingerprint))
            throw new InvalidOperationException("Refresh general settings after document changes.");
        var current=CreateMapGeneralDraft();
        if(!draft.Editable || !current.Editable || current.Record is null || draft.Record?.File!=current.Record.File || draft.Record.Offset!=current.Record.Offset
            || draft.Record.Length!=current.Record.Length || !draft.Baseline.OrderBy(row=>row.Key).SequenceEqual(current.Values.OrderBy(row=>row.Key)))
            throw new InvalidOperationException("A current verified general-settings layout is required.");
        return current.Record;
    }
    public MapGeneralDraft UpdateMapGeneralSettings(MapGeneralDraft draft,IEnumerable<MapGeneralInput> inputs)
    {
        var record=VerifyMapGeneralDraft(draft);var requested=inputs.ToArray();
        if(!requested.Select(row=>row.Field).Order().SequenceEqual(draft.Values.Keys.Order()))throw new ArgumentException("Provide every available general setting exactly once.");
        var values=new Dictionary<string,string>();
        foreach(var input in requested)
        {
            var value=input.Field=="Name" ? input.Value : int.Parse(input.Value,NumberStyles.Integer,CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            if(value!=draft.Baseline[input.Field])
            {
                if(input.Field=="Players" && !MapGeneralSettings.Keepers.Any(choice=>choice.Value==value)
                    || input.Field=="Version" && !MapGeneralSettings.Versions.Any(choice=>choice.Value==value))throw new ArgumentOutOfRangeException(nameof(inputs),"Choose an original keeper count or target game version.");
                if(input.Field!="Version" || IsLegacyProject)record.Fields.Single(field=>field.Name==MapGeneralSettings.StorageField(IsLegacyProject,input.Field)).Encode(value);
            }
            values.Add(input.Field,value);
        }
        return draft.With(values);
    }
    public void ApplyMapGeneralSettings(MapGeneralDraft draft)
    {
        var record=VerifyMapGeneralDraft(draft);if(!draft.Changed)return;
        Edit("Change general map settings",()=>{
            foreach(var row in draft.Values.Where(row=>row.Value!=draft.Baseline[row.Key]))
                if(row.Key=="Version" && !IsLegacyProject)Edit("Change target game version",()=>WriteNativeGameTarget(int.Parse(row.Value,CultureInfo.InvariantCulture)));
                else SetField(record,record.Fields.Single(field=>field.Name==MapGeneralSettings.StorageField(IsLegacyProject,row.Key)),row.Value);
        });
    }
}
