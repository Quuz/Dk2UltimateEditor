using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class EditorPatrolGroupingChecks
{
    private static void Expect(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    private static uint[] Numbers(string text) => text.Length == 0 ? [] : text.Split(',').Select(uint.Parse).ToArray();
    private static string Sequence(IEnumerable<PatrolRoute> routes) => string.Join(";", routes.Select(route =>
        string.Join(',', route.Points.Select(point => point.ID).Concat(route.EndID == 0 ? [] : new[] { route.EndID }))));
    private static string Sequence(JsonElement groups) => string.Join(";", groups.EnumerateArray().Select(group =>
        string.Join(',', group.EnumerateArray().Select(id => id.GetUInt32()))));
    public static void Run(string repo)
    {
        var fixture = Path.Combine(repo, "tests/Fixtures/PatrolGrouping");
        var lines = File.ReadAllLines(Path.Combine(fixture, "source-groups.tsv"));
        Expect(lines.Length == 15404, "Source fixture must cover every graph and file permutation for 1–4 areas.");
        using var literal = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(fixture, "literal-cases.json")));
        foreach (var project in new[] { false, true })
        {
            var count = 0;
            for (var n = 1; n <= 4; n++)
            {
                var document = Create(project, n);
                var records = document.PatrolAreas().Select(point => point.Record).ToArray();
                foreach (var line in lines.Where(line => line.Split('|')[0].Split(',').Length == n))
                {
                    var columns = line.Split('|');
                    var order = Numbers(columns[0]); var next = Numbers(columns[1]);
                    for (var i = 0; i < n; i++)
                    { Raw(records[i], project ? "id" : "bAreaID", order[i]); Raw(records[i], project ? "nextid" : "bAreaNextID", next[order[i] - 1]); }
                    Check(document, columns[2], "exhaustive " + line);
                    count++;
                }
            }
            Expect(count == 15404, "Exhaustive case coverage lost.");
            foreach (var item in literal.RootElement.EnumerateArray())
            {
                var areas = item.GetProperty("Areas").EnumerateArray().ToArray();
                var document = Create(project, areas.Length);
                var records = document.PatrolAreas().Select(point => point.Record).ToArray();
                for (var i = 0; i < records.Length; i++)
                { Raw(records[i], project ? "id" : "bAreaID", areas[i][0].GetUInt32()); Raw(records[i], project ? "nextid" : "bAreaNextID", areas[i][1].GetUInt32()); }
                Check(document, Sequence(item.GetProperty("Groups")), "literal source branch/missing/order/loop");
            }
            // The full allowed identity range has a source-compatible bounded
            // projection; no fixed-array overflow or recursion is involved.
            var maximum = Create(project, 255);
            var maximumAreas = maximum.PatrolAreas().Select(point => point.Record).ToArray();
            for (var i = 0; i < maximumAreas.Length; i++) Raw(maximumAreas[i], project ? "nextid" : "bAreaNextID", (uint)(i == 254 ? 1 : i + 2));
            Check(maximum, string.Join(',', Enumerable.Range(1,255).Concat(new[] { 1 })), "255-point complete loop");
            // Ambiguous imports retain the generic diagnostic graph, no map
            // repair or ID guessing, and the same edit rejection guards.
            var damaged = Create(project,3); var damagedAreas = damaged.PatrolAreas().Select(point => point.Record).ToArray();
            foreach (var id in project ? new uint[] { 0, 2, 256 } : new uint[] { 0, 2 })
            {
                Raw(damagedAreas[0],project?"id":"bAreaID",id);
                Raw(damagedAreas[0],project?"nextid":"bAreaNextID",2);
                var before = damaged.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
                Expect(Sequence(damaged.EditorPatrolRoutes())==Sequence(damaged.PatrolRoutes()),"Invalid identity diagnostic fallback changed.");
                Expect(damaged.EditorPatrolRoutes().Any(route=>route.Ambiguous),"Ambiguous graph was presented as safely editable.");
                Expect(before.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Diagnostic grouping repaired the import.");
            }
        }
        // Independent packed native input, not compiler-generated evidence.
        using var level = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(fixture,"level10.json")));
        var map = Path.Combine(repo,"../Dungeon Keeper 2/Data/editor/maps/Level10.kwd");
        var things = Path.Combine(Path.GetDirectoryName(map)!,"Level10Things.kld");
        Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(things))).Equals(level.RootElement.GetProperty("ThingsSHA256").GetString(),StringComparison.OrdinalIgnoreCase),"Installed Level10 fixture changed; re-audit its source graph.");
        var real = NativeMapDocument.Load(map);
        var encodedAreas = string.Join(';',real.PatrolAreas().Select(point=>$"{point.ID},{point.NextID}"));
        var expectedAreas = string.Join(';',level.RootElement.GetProperty("Areas").EnumerateArray().Select(area=>$"{area[0].GetUInt32()},{area[1].GetUInt32()}"));
        Expect(encodedAreas==expectedAreas,"Level10 area physical order/packed identities differ from independent raw parser.");
        Check(real,Sequence(level.RootElement.GetProperty("Groups")),"installed Level10");
        Expect(!level.RootElement.GetProperty("MatchesHistoricalCommentEdges").GetBoolean(),"Installed Level10 is different from the historical comment fixture; keep these scopes separate.");
    }
    private static NativeMapDocument Create(bool project,int count)
    {
        var document=NativeMapDocument.CreateNewMap(new("Grouping","Grouping",16,16,NativeFormat:!project));
        for(var i=0;i<count;i++) document.AddMapRegion(MapRegionKind.Area,new(2,2,1,1));
        return document;
    }
    private static void Raw(NativeRecord record,string name,uint value)
    {
        Expect(MemoryMarshal.TryGetArray(record.File.Data,out var memory),"Fixture bytes unavailable.");
        var field=record.Fields.Single(field=>field.Name==name);
        field.Encode(value.ToString(CultureInfo.InvariantCulture)).CopyTo(memory.Array!,memory.Offset+record.Offset+field.Offset);
    }
    private static void Check(NativeMapDocument document,string expected,string scope)
    {
        var before=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        var count=document.EditorChangeCount;var revision=document.EditRevision;var history=document.GetHistory();
        var notifications=0;EventHandler handler=(_,_)=>notifications++;document.Changed+=handler;
        IReadOnlyList<PatrolRoute> routes;
        try{routes=document.EditorPatrolRoutes();}finally{document.Changed-=handler;}
        Expect(Sequence(routes)==expected,"Source tree order differs: "+scope+" actual="+Sequence(routes));
        foreach(var route in routes)
        {
            var raw=document.PatrolRouteFrom(route.Points[0].Record);
            Expect(!route.Ambiguous&&Sequence(new[]{route})==Sequence(new[]{raw})&&route.OccurrenceCount==raw.OccurrenceCount,"Source view cannot be passed safely to existing occurrence edits: "+scope);
        }
        Expect(before.Count==document.Files.Count&&before.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Viewing tree altered map bytes: "+scope);
        var after=document.GetHistory();
        Expect(notifications==0&&count==document.EditorChangeCount&&revision==document.EditRevision&&history.ChangeCount==after.ChangeCount
            &&history.Undo.SequenceEqual(after.Undo)&&history.Redo.SequenceEqual(after.Redo),"Viewing tree altered counters/history: "+scope);
    }
}
