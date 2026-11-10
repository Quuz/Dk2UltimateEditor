using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class MapRuleFillImageChecks
{
    private static void Expect(bool value,string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx"))) directory = directory.Parent;
        var root = directory?.FullName ?? throw new Exception("Canonical root missing.");
        using var fixture = JsonDocument.Parse(typeof(MapRuleFillImageChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.MapRuleFillImagesSource.json")!);
        foreach (var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Map-property image source input changed.");
        var rows = fixture.RootElement.GetProperty("Rows").EnumerateArray().ToArray();
        Expect(rows.Length == 139,"Rule image source inventory incomplete.");
        foreach (var group in rows.GroupBy(row => Enum.Parse<MapRuleKind>(row.GetProperty("Kind").GetString()!)))
        {
            var elements = MapRules.Elements(group.Key); Expect(elements.Count == group.Count(),"Rule image support changed source catalogue count.");
            foreach (var row in group)
            {
                var index = row.GetProperty("Index").GetInt32();
                Expect(elements[index].Index == index && elements[index].ID == row.GetProperty("ID").GetUInt32(),"Image catalogue confused rule variable ID, game ID, or source position.");
                Expect(MapRuleImageResources.ForRule(group.Key,index) == row.GetProperty("Resource").GetInt32(),"Wrong source rule row bitmap.");
            }
            foreach (var index in new[] {-1,elements.Count,int.MaxValue}) Expect(MapRuleImageResources.ForRule(group.Key,index) is null,"Unknown rule row got a fallback bitmap.");
        }
        Expect(MapRuleImageResources.ForRule(MapRuleKind.Alliance,0) is null && MapRuleImageResources.ForRule((MapRuleKind)999,0) is null,"Alliance/unsupported kind invented a source image.");
        var fills = fixture.RootElement.GetProperty("Fills").EnumerateArray().ToArray();
        Expect(fills.Length == 23 && LegacyCompilerRules.Elements("NewMap").Select(row => row.ID).SequenceEqual(fills.Select(row => row.GetProperty("ID").GetUInt32())),"Packed fill source order/IDs changed.");
        foreach (var fill in fills) Expect(MapRuleImageResources.ForFill(fill.GetProperty("ID").GetUInt32()) == fill.GetProperty("Resource").GetInt32(),"Fill image discarded packed owner/type bytes.");
        foreach (var id in new uint[] {0,4,8,9,0x04030101,uint.MaxValue}) Expect(MapRuleImageResources.ForFill(id) is null,"Unknown packed fill got a terrain-only fallback.");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        using var stream = File.OpenRead(Path.Combine(root,"src/DK2MapEditor.App/bin",configuration,"net10.0-windows/DK2MapEditor.App.dll"));
        using var pe = new PEReader(stream); var metadata = pe.GetMetadataReader();
        var resources = pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress); var embedded = new Dictionary<string,byte[]>();
        foreach (var handle in metadata.ManifestResources)
        {
            var resource = metadata.GetManifestResource(handle); if (!resource.Implementation.IsNil) continue;
            var reader = resources.GetReader(checked((int)resource.Offset),resources.Length-checked((int)resource.Offset));
            var length = checked((int)reader.ReadUInt32()); embedded.Add(metadata.GetString(resource.Name),reader.ReadBytes(length));
        }
        var images = fixture.RootElement.GetProperty("Images").EnumerateArray().ToArray(); Expect(images.Length == 104,"Source image identity inventory incomplete.");
        foreach (var row in images)
        {
            var original = File.ReadAllBytes(Path.Combine(root,row.GetProperty("SourceBitmap").GetString()!));
            Expect(Convert.ToHexString(SHA256.HashData(original)).Equals(row.GetProperty("SHA256").GetString(),StringComparison.OrdinalIgnoreCase),"Protected source map-property bitmap changed.");
            var id = row.GetProperty("EmbeddedResource").GetInt32();
            var bytes = new[] {"ScenarioImage","ObjectImage","TerrainImage","DefinitionImage","OverviewIcon"}.Select(prefix => embedded.GetValueOrDefault($"Legacy.{prefix}.{id}")).FirstOrDefault(value => value is not null);
            Expect(bytes is not null && original.AsSpan().SequenceEqual(bytes),"Map-property bitmap missing/different in loader resource:"+id);
        }
        foreach (var native in new[] {true,false})
        {
            var document = NativeMapDocument.CreateNewMap(new("RuleImages","RuleImages",20,20,NativeFormat:native));
            var bytes = document.Files.ToDictionary(file => file,file => file.Data.ToArray());
            var state = (document.EditRevision,document.HasChanges,document.CanUndo,document.CanRedo);
            var before = document.MapRuleCells().ToArray();
            foreach (var kind in Enum.GetValues<MapRuleKind>()) foreach (var element in MapRules.Elements(kind)) _ = MapRuleImageResources.ForRule(kind,element.Index);
            foreach (var fill in fills) _ = MapRuleImageResources.ForFill(fill.GetProperty("ID").GetUInt32());
            Expect(before.SequenceEqual(document.MapRuleCells()),"Image lookup changed rule cells or their order.");
            foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]),"Rule/fill image lookup changed map bytes.");
            Expect(state == (document.EditRevision,document.HasChanges,document.CanUndo,document.CanRedo),"Rule/fill image lookup changed history or revision.");
        }
        Console.WriteLine("MAP RULE/FILL IMAGE DETAIL139 source rule rows/23 packed fill IDs/104 source BMP identities and compiled PE bytes/native+DKM read-only state; no App/WPF execution.");
    }
}
