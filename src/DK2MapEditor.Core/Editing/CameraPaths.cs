using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

public sealed record CameraPathChoice(int ID, bool ShowCell, string English, string Russian)
{
    public int CeilingMode => ShowCell ? 0 : 1;
    public int AreaMode => CeilingMode; // Retained API alias for earlier callers.
    public string Description(string language) => language == "ru" ? Russian : English;
}

public static class CameraPaths
{
    public static IReadOnlyList<CameraPathChoice> Choices { get; } = Load();
    private static IReadOnlyList<CameraPathChoice> Load()
    {
        using var source = typeof(CameraPaths).Assembly.GetManifestResourceStream("Legacy.Datas_CameraPaths.c")!;
        using var reader = new StreamReader(source);
        using var names = typeof(CameraPaths).Assembly.GetManifestResourceStream("Editor.CameraPathDescriptions")!;
        using var nameReader = new StreamReader(names);
        var descriptions = nameReader.ReadToEnd().Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('\t')).ToDictionary(parts => int.Parse(parts[0], CultureInfo.InvariantCulture));
        var choices = Regex.Matches(reader.ReadToEnd(), @"\{\s*(\d+),\s*(\d+),\s*szCameraPath\d+,\s*(TRUE|FALSE)\}")
            .Select(match =>
            {
                var id = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                if (int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) != id + 1000) throw new InvalidDataException("Unexpected camera bitmap identity.");
                var text = descriptions[id];
                if (text.Length != 3 || string.IsNullOrWhiteSpace(text[1]) || string.IsNullOrWhiteSpace(text[2])) throw new InvalidDataException("Incomplete camera-path translation.");
                return new CameraPathChoice(id, match.Groups[3].Value == "TRUE", text[1], text[2]);
            }).ToArray();
        if (choices.Length != 52 || choices.Select(choice => choice.ID).Distinct().Count() != 52 || descriptions.Count != 52)
            throw new InvalidDataException("Incomplete original camera-path catalogue.");
        return Array.AsReadOnly(choices);
    }
    public static bool IsCameraAction(NativeRecord record) => EntityReferences.EventType(record)?.Name == "ACTION_CAMERAPATH";
    public static string FieldName(NativeRecord record, int parameter) => record.Category == "Project.ACTION" ? "param" + parameter : "Param" + parameter;
    public static bool Supported(NativeRecord record)
    {
        if (!IsCameraAction(record)) return false;
        var project = record.Category == "Project.ACTION";
        if (project ? record.Length != LegacyProjectSchemas.Size("ACTION") : record.File.Signature != 0xD2 || record.Length != 24) return false;
        foreach (var parameter in new[] { 1, 2, 3 })
        {
            var expected = project ? LegacyProjectSchemas.Fields("ACTION").Single(field => field.Name == "param" + parameter)
                : LegacyEventSchemas.Find(0xD6, 0x1F)!.Arguments.Single(field => field.Name == "Param" + parameter);
            if (!record.Fields.Contains(expected)) return false;
        }
        return true;
    }
}

public sealed class CameraPathDraft
{
    internal NativeMapDocument Owner { get; }
    public NativeRecord Record { get; }
    private readonly byte[] payload;
    internal CameraPathDraft(NativeMapDocument owner, NativeRecord record)
    { Owner = owner; Record = record; payload = record.File.Data.Slice(record.Offset, record.Length).ToArray(); }
    public int PathID => int.Parse(Record.Fields.Single(candidate => candidate.Name == CameraPaths.FieldName(Record, 1)).Read(payload), CultureInfo.InvariantCulture);
    internal bool Matches(NativeRecord record) => Record.File == record.File && Record.Offset == record.Offset && Record.Length == record.Length
        && Record.Category == record.Category && record.File.Data.Slice(record.Offset, record.Length).Span.SequenceEqual(payload);
}

public sealed partial class NativeMapDocument
{
    public CameraPathDraft CreateCameraPathDraft(NativeRecord record)
    {
        var current = Records().SingleOrDefault(item => item.File == record.File && item.Offset == record.Offset && item.Length == record.Length && item.Category == record.Category);
        if (current is null || !CameraPaths.Supported(current)) throw new ArgumentException("Select a verified current camera-path action.");
        return new(this, current);
    }
    public void ApplyCameraPath(CameraPathDraft draft, int pathID)
    {
        if (!ReferenceEquals(this, draft.Owner)) throw new ArgumentException("Camera draft belongs to a different document.");
        var current = CreateCameraPathDraft(draft.Record).Record;
        if (!draft.Matches(current)) throw new InvalidOperationException("The camera action changed after the chooser was opened.");
        var choice = CameraPaths.Choices.SingleOrDefault(choice => choice.ID == pathID) ?? throw new ArgumentException("Choose an original camera path.");
        Edit("Choose camera path", () =>
        {
            SetField(current, current.Fields.Single(field => field.Name == CameraPaths.FieldName(current, 1)), choice.ID.ToString(CultureInfo.InvariantCulture));
            SetField(current, current.Fields.Single(field => field.Name == CameraPaths.FieldName(current, 3)), choice.AreaMode.ToString(CultureInfo.InvariantCulture));
        });
    }
}
