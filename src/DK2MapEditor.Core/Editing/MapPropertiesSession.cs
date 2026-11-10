using System.Buffers.Binary;
using System.Collections.ObjectModel;

namespace DK2MapEditor.Core.Editing;

/// <summary>All child editors work on this independent in-memory document.
/// Dispose/outer Cancel never commits its edits to the live map.</summary>
public sealed class MapPropertiesSession : IDisposable
{
    internal MapPropertiesSession(NativeMapDocument owner, NativeMapDocument working,
        Dictionary<NativeFile, NativeFile> files, long revision, byte[] fingerprint)
    { Owner = owner; WorkingDocument = working; Files = files; Revision = revision; Fingerprint = fingerprint; }
    internal NativeMapDocument Owner { get; }
    internal Dictionary<NativeFile, NativeFile> Files { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    internal bool Closed { get; private set; }
    public NativeMapDocument WorkingDocument { get; }
    public void Dispose() => Closed = true;
}

public sealed record MapPropertiesResize(int Width, int Height, int Anchor = 0, bool RemoveOutside = false, MapTile? Fill = null);
public sealed record MapPropertiesCrop(string Category, int Count);

/// <summary>Frozen preflight result. Consent is tied to both live-map and
/// child-draft lifetimes; no mutable prepared result document is exposed.</summary>
public sealed class MapPropertiesPlan
{
    internal MapPropertiesPlan(MapPropertiesSession session, long revision, byte[] fingerprint,
        IReadOnlyDictionary<NativeFile, byte[]> result, MapPropertiesResize resize, bool resized,
        IReadOnlyList<MapPropertiesCrop> crops, IReadOnlyList<HistoryOperation> children, bool editorCaller = false, uint editorIncrements = 0)
    { Session = session; Revision = revision; Fingerprint = fingerprint; Result = result; Resize = resize; Resized = resized; Crops = crops; Children = children; EditorCaller = editorCaller; EditorIncrements = editorIncrements; }
    internal MapPropertiesSession Session { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    internal IReadOnlyDictionary<NativeFile, byte[]> Result { get; }
    internal IReadOnlyList<HistoryOperation> Children { get; }
    internal bool EditorCaller { get; }
    internal uint EditorIncrements { get; }
    public MapPropertiesResize Resize { get; }
    public bool Resized { get; }
    public IReadOnlyList<MapPropertiesCrop> Crops { get; }
    public bool NeedsLargeMapConsent => Resized && (long)Resize.Width * Resize.Height > 120 * 120;
    public bool NeedsCropConsent => Crops.Count != 0;
    public bool Changed => Result.Any(row => !row.Key.Data.Span.SequenceEqual(row.Value));
}

public sealed partial class NativeMapDocument
{
    private (NativeMapDocument Document, Dictionary<NativeFile, NativeFile> Files) ClonePropertiesDocument()
    {
        var clone = new NativeMapDocument();
        var copies = new Dictionary<NativeFile, NativeFile>();
        foreach (var file in files)
        {
            var copy = new NativeFile(file.SourcePath, file.ExportPath, file.Data.ToArray());
            clone.Add(copy); copies.Add(file, copy);
        }
        clone.MainFile = copies[MainFile]; clone.TileFile = copies[TileFile];
        clone.nativeRetainedVariables=nativeRetainedVariables.ToArray();
        clone.nativePayloadSource=nativePayloadSource;
        clone.officialDefinitionExtras=officialDefinitionExtras;
        clone.nativeExpandedConditions.UnionWith(nativeExpandedConditions);
        clone.MissingReferences = Array.AsReadOnly(MissingReferences.ToArray());
        foreach (var section in projectSections) clone.projectSections.Add(copies[section]);
        foreach (var section in optionalProjectSections) clone.optionalProjectSections.Add(copies[section]);
        if(IsLegacyProject)
        {
            var delta=LevelModifierDeltaFile;
            clone.nativeLevelModifierDelta=new(delta.SourcePath,delta.ExportPath,delta.Data.ToArray());
            clone.originalLevelModifierDelta=delta.Data.ToArray();copies.Add(delta,clone.nativeLevelModifierDelta);
            var owned=LevelModifierOwnedFile;
            clone.nativeLevelModifierOwned=new(owned.SourcePath,owned.ExportPath,owned.Data.ToArray());
            clone.originalLevelModifierOwned=owned.Data.ToArray();copies.Add(owned,clone.nativeLevelModifierOwned);
        }
        if (!IsLegacyProject)
        {
            // Copy opaque alias state and session target without file I/O or
            // serializing/reloading the map (which would reset the target).
            var names = NativeScenarioNames;
            clone.nativeScenarioNames = new(names.SourcePath, names.ExportPath, names.Data.ToArray());
            clone.originalNativeScenarioNames = names.Data.ToArray();
            clone.nativeScenarioNamesLoaded = nativeScenarioNamesLoaded;
            copies.Add(names, clone.nativeScenarioNames);
            var target = NativeGameTarget;
            clone.nativeGameTarget = new(target.SourcePath, target.ExportPath, target.Data.ToArray());
            clone.originalNativeGameTarget = target.UInt32(0);
            copies.Add(target, clone.nativeGameTarget);
            var fill = NativeResizeFill;
            clone.nativeResizeFill = new(fill.SourcePath, fill.ExportPath, fill.Data.ToArray());
            clone.originalNativeResizeFill = fill.UInt32(0);
            copies.Add(fill, clone.nativeResizeFill);
            var gates = NativeGateBindings;
            clone.nativeGateBindings = new(gates.SourcePath, gates.ExportPath, gates.Data.ToArray());
            clone.originalNativeGateBindings = gates.Data.ToArray();
            copies.Add(gates, clone.nativeGateBindings);
            var areas = NativeAreaScopes;
            clone.nativeAreaScopes = new(areas.SourcePath, areas.ExportPath, areas.Data.ToArray());
            clone.originalNativeAreaScopes = areas.Data.ToArray();
            copies.Add(areas, clone.nativeAreaScopes);
            var owners = NativeOwnerScopes;
            clone.nativeOwnerScopes = new(owners.SourcePath, owners.ExportPath, owners.Data.ToArray());
            clone.originalNativeOwnerScopes = owners.Data.ToArray();
            copies.Add(owners, clone.nativeOwnerScopes);
        }
        return (clone, copies);
    }
    public MapPropertiesSession CreateMapPropertiesSession()
    {
        if (transactionOpen || !CreateMapGeneralDraft().Editable) throw new InvalidOperationException("A verified map-properties owner outside an edit is required.");
        var fingerprint = UnusedScriptFingerprint(); var copy = ClonePropertiesDocument();
        return new(this, copy.Document, copy.Files, EditRevision, fingerprint);
    }
    public bool IsMapPropertiesSessionCurrent(MapPropertiesSession session) => session is not null && !session.Closed
        && ReferenceEquals(session.Owner, this) && session.Revision == EditRevision && !transactionOpen
        && UnusedScriptFingerprint().AsSpan().SequenceEqual(session.Fingerprint);
    private void VerifyPropertiesSession(MapPropertiesSession session)
    {
        if (!IsMapPropertiesSessionCurrent(session)) throw new InvalidOperationException("Refresh map properties after owner changes or outer Cancel.");
    }
    private void VerifyPropertiesScope(MapPropertiesSession session)
    {
        var working = session.WorkingDocument;
        if (session.Files.Count != TransactionFiles().Count() || !session.Files.Values.ToHashSet().SetEquals(working.TransactionFiles()))
            throw new InvalidOperationException("The map-properties draft changed its file topology.");
        var masks = session.Files.Keys.ToDictionary(file => file, file => new bool[file.Length]);
        void Allow(NativeRecord record, IEnumerable<NativeField> fields)
        { foreach (var field in fields) Array.Fill(masks[record.File], true, record.Offset + field.Offset, field.Size); }
        var records = Records();
        foreach (var record in records)
        {
            if (IsLegacyProject && record.File.Signature is LegacyInfo or LegacyPlayers && record.Category != "Project.Opaque")
                Allow(record, record.Fields.Where(field => field.Name is not ("Width" or "Height")));
            else if (!IsLegacyProject && record.File == MainFile && record.Category == "Map" && record.File.UInt32(4) == 4)
                Allow(record, record.Fields.Where(field => field.Offset >= 56 && field.Name != "wMainTrigger"));
        }
        if (!IsLegacyProject)
        {
            foreach (var player in PlayerSettingsRows().Where(row => row.Editable))
            {
                Allow(player.Record, player.Record.Fields.Where(field => field.Name != "bGlobalPlayer" && !field.Name.EndsWith("Trigger", StringComparison.Ordinal)));
                if (PlayerHeart(player.ID) is { } heart && heart.Length == 28)
                    Allow(heart, heart.Fields.Where(field => field.Name == "wGateHealth"));
            }
            Array.Fill(masks[NativeGameTarget], true);
        }
        foreach (var (source, draft) in session.Files)
        {
            if(IsLegacyProject&&(source==LevelModifierDeltaFile||source==LevelModifierOwnedFile))
            {
                if(!working.nativeRetainedVariables.AsSpan().SequenceEqual(nativeRetainedVariables)
                    ||working.nativePayloadSource?.ContentHash!=nativePayloadSource?.ContentHash)
                    throw new InvalidOperationException("Map properties cannot replace the original native source.");
                _=working.EffectiveRetainedLevelVariables();_=working.ReadOwnedLevelModifiers();continue;
            }
            if (!IsLegacyProject && source == MapVariableFile && !source.Data.Span.SequenceEqual(draft.Data.Span))
            { VerifyPropertiesVariableCollection(source, draft, working); continue; }
            if (source.Length != draft.Length) throw new InvalidOperationException("Only the verified map-variable collection may change length inside properties.");
            for (var index = 0; index < source.Length; index++)
                if (source.Data.Span[index] != draft.Data.Span[index] && !masks[source][index])
                    throw new InvalidOperationException("A child draft changed terrain, objects, scripts, aliases, headers, opaque bytes or shared definitions outside map properties.");
        }
        if (!IsLegacyProject && !MapGeneralSettings.Versions.Any(row => row.Value == working.TargetGameVersion?.ToString(System.Globalization.CultureInfo.InvariantCulture))
            && working.TargetGameVersion != TargetGameVersion) throw new InvalidOperationException("An unknown changed target version is not allowed.");
    }
    private void VerifyPropertiesVariableCollection(NativeFile source, NativeFile draft, NativeMapDocument working)
    {
        if (source.UInt32(4) != 4 || draft.UInt32(4) != 4) throw new InvalidOperationException("A verified map-variable collection is required.");
        var before = Records().Where(record => record.File == source).ToArray();
        var after = working.Records().Where(record => record.File == draft).ToArray();
        bool Known(NativeRecord record) => record.Length == 16 && (NativeRuleKey(record) is { } rule && MapRules.Keys().Contains(rule)
            || record.Category == "GAMEGLOBALVARIABLE" && MapVariables.Definitions.Any(row => row.ID == record.File.UInt32(record.Offset)));
        // Unknown entries are not interpreted or repaired. Their order, bytes
        // and the extension after the declared records remain exact.
        VerifyPropertiesLevelOccurrences(before.Where(record=>!Known(record)).ToArray(),after.Where(record=>!Known(record)).ToArray());
        // Typed writers only change exposed fields. A deleted/recreated known
        // entry may have zero padding; surviving imported padding must not be
        // an unrestricted back door for child editors.
        string Padding(NativeRecord record)
        {
            var bytes = record.File.Data.Span.Slice(record.Offset, record.Length).ToArray();
            Array.Clear(bytes, 0, 4); // verified dispatch key
            foreach (var field in record.Fields) Array.Clear(bytes, field.Offset, field.Size);
            return Convert.ToHexString(bytes);
        }
        var retainedPadding = before.Where(Known).Select(record => (record.Category, Padding: Padding(record))).ToHashSet();
        var zeroPadding = Convert.ToHexString(new byte[16]);
        if (after.Where(Known).Any(record => Padding(record) != zeroPadding && !retainedPadding.Contains((record.Category, Padding(record)))))
            throw new InvalidOperationException("Map-properties edits changed reserved variable payload bytes.");
        var beforeEnd = 56 + before.Length * 16; var afterEnd = 56 + after.Length * 16;
        if (!source.Data.Span[beforeEnd..].SequenceEqual(draft.Data.Span[afterEnd..])) throw new InvalidOperationException("Map-properties edits must retain the variable extension.");
        for (var index = 0; index < 56; index++)
            if (index is not (>= 8 and < 12 or >= 20 and < 24 or >= 52 and < 56) && source.Data.Span[index] != draft.Data.Span[index])
                throw new InvalidOperationException("Map-properties edits changed unrelated variable header bytes.");
        if (draft.UInt32(20) != after.Length || draft.UInt32(52) != AdjustedNativeStoredSize(source.UInt32(52), (long)(after.Length - before.Length) * 16, after.Length * 16)
            || draft.UInt32(8) != AdjustedNativeStoredSize(source.UInt32(8), draft.Length - source.Length, draft.Length))
            throw new InvalidOperationException("Map-properties variable changes have inconsistent collection metadata.");
    }
    public MapPropertiesPlan PrepareMapProperties(MapPropertiesSession session, MapPropertiesResize? resize = null)
        => PrepareMapPropertiesCore(session,resize,editorCaller:false);
    public MapPropertiesPlan PrepareEditorMapProperties(MapPropertiesSession session, MapPropertiesResize? resize = null)
        => PrepareMapPropertiesCore(session,resize,editorCaller:true);
    private MapPropertiesPlan PrepareMapPropertiesCore(MapPropertiesSession session,MapPropertiesResize? resize,bool editorCaller)
    {
        VerifyPropertiesSession(session); VerifyPropertiesScope(session);
        var working = session.WorkingDocument; var requested = resize ?? new(Width, Height);
        var resized = requested.Width != Width || requested.Height != Height;
        if (requested.Anchor is < 0 or > 8 || requested.Width is < 1 or > 1024 || requested.Height is < 1 or > 1024
            || resized && (requested.Width < 7 || requested.Height < 7)) throw new ArgumentOutOfRangeException(nameof(resize));
        var copy = working.ClonePropertiesDocument();
        var categories = working.Records().GroupBy(record => record.Category).ToDictionary(group => group.Key, group => group.Count());
        if (resized)
        {
            var fill = requested.Fill ?? DefaultPropertiesResizeFill(working);
            if (fill.Owner is < 1 or > 7 || fill.Variant is not (1 or 2)) throw new ArgumentException("Choose a verified resize fill.");
            copy.Document.ResizeCore(requested.Width, requested.Height, (requested.Width - Width) * (requested.Anchor % 3) / 2,
                (requested.Height - Height) * (requested.Anchor / 3) / 2, fill, requested.RemoveOutside, editorCaller: editorCaller);
            if(editorCaller)copy.Document.RemoveEditorResizeBoundaryRecords(requested.RemoveOutside);
        }
        var remaining = copy.Document.Records().GroupBy(record => record.Category).ToDictionary(group => group.Key, group => group.Count());
        var crops = categories.Where(row => row.Value > remaining.GetValueOrDefault(row.Key)).Select(row => new MapPropertiesCrop(row.Key, row.Value - remaining.GetValueOrDefault(row.Key))).ToArray();
        var result = session.Files.ToDictionary(row => row.Key, row => copy.Files[row.Value].Data.ToArray());
        // Preserve child history descriptions, not their mutable payloads. A
        // source operation remains one real Undo/Redo regardless of child count.
        var children = working.GetHistory().Undo.Concat(copy.Document.GetHistory().Undo).ToArray();
        return new(session, working.EditRevision, working.UnusedScriptFingerprint(), new ReadOnlyDictionary<NativeFile, byte[]>(result), requested, resized,
            Array.AsReadOnly(crops), Array.AsReadOnly(children), editorCaller,
            editorCaller?CalculateEditorPropertiesIncrements(resized,(requested.Width-Width)*(requested.Anchor%3)/2!=0
                ||(requested.Height-Height)*(requested.Anchor/3)/2!=0,working.Records(),copy.Document.Records()):0);
    }
    private static MapTile DefaultPropertiesResizeFill(NativeMapDocument working)
    {
        return working.ResizeFillPreset is { } preset ? EditorTerrainRules.DecodeFillPreset(preset)
            : throw new InvalidOperationException("Choose an explicit resize fill for unverified background metadata.");
    }
    public bool IsMapPropertiesPlanCurrent(MapPropertiesPlan plan) => plan is not null && IsMapPropertiesSessionCurrent(plan.Session)
        && plan.Revision == plan.Session.WorkingDocument.EditRevision
        && plan.Session.WorkingDocument.UnusedScriptFingerprint().AsSpan().SequenceEqual(plan.Fingerprint);
    public void ApplyMapProperties(MapPropertiesPlan plan, bool largeMapAccepted = false, bool croppingAccepted = false)
    {
        if (!IsMapPropertiesPlanCurrent(plan)) throw new InvalidOperationException("Refresh map properties after the live map or child draft changes.");
        if (plan.NeedsLargeMapConsent && !largeMapAccepted || plan.NeedsCropConsent && !croppingAccepted)
            throw new InvalidOperationException("Confirm the current large/cropped map-properties preview first.");
        Edit("Change general map settings", () =>
        {
            retainRawCrystalTargets = true; // Generic properties retain the raw Resize contract.
            foreach (var row in plan.Result) row.Key.Bytes = (byte[])row.Value.Clone();
            if (historyDepth != 0) operationChildren.Peek().AddRange(plan.Children);
        });
        plan.Session.Dispose();
    }
}
