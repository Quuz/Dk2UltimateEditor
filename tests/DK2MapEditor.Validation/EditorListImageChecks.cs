using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class EditorListImageChecks
{
    private static void Expect(bool value,string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx"))) directory = directory.Parent;
        var root = directory?.FullName ?? throw new Exception("Canonical root missing.");
        using var fixture = JsonDocument.Parse(typeof(EditorListImageChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.EditorListImagesSource.json")!);
        foreach (var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"List image source input changed.");
        foreach (var scope in fixture.RootElement.GetProperty("Scopes").EnumerateArray())
            Expect(EditorListImageResources.ScenarioScope(scope.GetProperty("Scope").GetUInt32()) == scope.GetProperty("Resource").GetInt32(),"Wrong source scenario scope icon.");
        foreach (var scope in new uint[] {0,3,5,0x1000000,0x2000000,uint.MaxValue})
            Expect(EditorListImageResources.ScenarioScope(scope) is null,"Unknown or combined scope invented an image.");
        Expect(EditorListImageResources.PatrolArea == 1501,"PathAreas index0 not mapped to its source bitmap.");
        for (var mask = 0; mask < 512; mask++)
            foreach (var locked in new[] {false,true})
                foreach (var accepted in new[] {false,true})
                {
                    // Independent literal source masks: first warning pass0x11A,
                    // then overriding pass0x21; modify clears even locked icon.
                    int? expected = accepted ? null : locked ? 800 : (mask & 0x21) != 0 ? 801 : (mask & 0x11A) != 0 ? 802 : null;
                    Expect(EditorListImageResources.ManagerProblem(locked,(ObjectBatchWarning)mask,accepted) == expected,"PvIcons precedence differs from source.");
                }
        Expect(EditorListImageResources.ManagerProblem(false,(ObjectBatchWarning)0x8000,false) is null,"Unknown warning bit invented an image.");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        using var stream = File.OpenRead(Path.Combine(root,"src/DK2MapEditor.App/bin",configuration,"net10.0-windows/DK2MapEditor.App.dll"));
        using var pe = new PEReader(stream); var metadata = pe.GetMetadataReader();
        var resources = pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress);
        var embedded = new Dictionary<string,byte[]>();
        foreach (var handle in metadata.ManifestResources)
        {
            var resource = metadata.GetManifestResource(handle); if (!resource.Implementation.IsNil) continue;
            var reader = resources.GetReader(checked((int)resource.Offset),resources.Length-checked((int)resource.Offset));
            var length = checked((int)reader.ReadUInt32()); embedded.Add(metadata.GetString(resource.Name),reader.ReadBytes(length));
        }
        var rows = fixture.RootElement.GetProperty("Entries").EnumerateArray().ToArray();
        Expect(rows.Length == 14,"Incomplete source list image fixture.");
        foreach (var row in rows)
        {
            var bytes = File.ReadAllBytes(Path.Combine(root,row.GetProperty("SourceBitmap").GetString()!));
            Expect(Convert.ToHexString(SHA256.HashData(bytes)).Equals(row.GetProperty("SHA256").GetString(),StringComparison.OrdinalIgnoreCase),"Protected source BMP changed.");
            Expect(embedded.TryGetValue(row.GetProperty("LogicalName").GetString()!,out var compiled) && bytes.AsSpan().SequenceEqual(compiled),"Source list BMP not present as exact loader resource in App.");
        }
        foreach (var native in new[] {true,false})
        {
            var document = NativeMapDocument.CreateNewMap(new("ListImages","ListImages",20,20,NativeFormat:native));
            var events = new[] {"Trigger","Action"}.Select(kind => native ? document.AddRecord(RecordTemplate.All.Single(t => t.Structure == kind),new(0,0)) : document.AddLegacyRecord(kind,new(0,0))).ToArray();
            var bytes = document.Files.ToDictionary(file => file,file => file.Data.ToArray());
            var state = (document.EditRevision,document.HasChanges,document.CanUndo,document.CanRedo);
            for (var i=0;i<events.Length;i++)
            {
                Expect(EditorListImageResources.ScenarioEvent(events[i]) == 200+i,"Trigger/action category confused with event code or scope.");
                Expect(EditorListImageResources.ScenarioEvent(events[i] with { Length=events[i].Length-1 }) is null,"Wrong event layout got an icon.");
                Expect(EditorListImageResources.ScenarioEvent(events[i] with { Offset=-1 }) is null,"Invalid event range got an icon.");
            }
            foreach (var row in document.PlayerSettingsRows()) Expect(EditorListImageResources.ScenarioEvent(row.Record) is null,"Player record shown as an event.");
            foreach (var scope in ScriptGraph.Build(document).Roots.Select(edge => edge.Scope)) _ = EditorListImageResources.ScenarioScope(scope);
            foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]),"Image metadata lookup changed map bytes.");
            Expect(state == (document.EditRevision,document.HasChanges,document.CanUndo,document.CanRedo),"Image lookup changed history or revision.");
        }
        Console.WriteLine("EDITOR LIST IMAGE DETAIL 14 source BMPs/9 scopes/2048 PvIcons flag-state cases/native+DKM layouts and read-only state; no App/WPF execution.");
    }
}
