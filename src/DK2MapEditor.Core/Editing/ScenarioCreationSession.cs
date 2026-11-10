using System.Collections.ObjectModel;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

/// <summary>Original selection-dialog temporary values, retained across Finish
/// and Cancel. Alias names and unsubmitted controls are not live map edits.</summary>
public sealed class ScenarioCreationContext
{
    internal ScenarioCreationContext(NativeMapDocument owner) => Owner = owner;
    internal NativeMapDocument Owner { get; }
    internal Dictionary<uint, (LegacyEventSchemas.EventType Type, byte[] Payload, byte Repeat, TileRect? Rectangle)> Remembered { get; } = [];
    internal bool Active { get; set; }
    /// <summary>Read the source dialog's retained category choice without
    /// opening a draft or changing live data.</summary>
    public LegacyEventSchemas.EventType InitialType(uint signature)
    {
        if(signature is not (0xD5 or 0xD6))throw new ArgumentOutOfRangeException(nameof(signature));
        return Remembered.GetValueOrDefault(signature).Type??ScenarioTypeDefaults.InitialType(signature);
    }
}

public sealed class ScenarioCreationSession : IDisposable
{
    internal ScenarioCreationSession(ScenarioCreationContext context, NativeMapDocument working,
        Dictionary<NativeFile, NativeFile> files, Dictionary<NativeFile, byte[]> initial,
        long revision, byte[] fingerprint, ushort id, ScriptEdge? insertion, bool editingExisting = false)
    { Context = context; WorkingDocument = working; Files = files; Initial = initial;
      Revision = revision; Fingerprint = fingerprint; EventID = id; Insertion = insertion; IsEditingExisting = editingExisting; }
    internal ScenarioCreationContext Context { get; }
    internal Dictionary<NativeFile, NativeFile> Files { get; }
    internal Dictionary<NativeFile, byte[]> Initial { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    internal ScriptEdge? Insertion { get; }
    internal LegacyEventSchemas.EventType? ExistingType { get; init; }
    internal IReadOnlySet<uint> ExistingScopes { get; init; } = new HashSet<uint>();
    internal bool Closed { get; private set; }
    public TileRect? Rectangle { get; internal set; }
    internal long InputRevision { get; set; }
    public NativeMapDocument WorkingDocument { get; }
    public ushort EventID { get; }
    public bool IsEditingExisting { get; }
    public NativeRecord Record => ScriptGraph.Build(WorkingDocument).Events.TryGetValue(EventID, out var item)
        ? item.Record : throw new InvalidOperationException("The private event identity changed.");
    public LegacyEventSchemas.EventType Type => EntityReferences.EventType(Record)
        ?? throw new InvalidOperationException("The private event type is unverified.");
    public IReadOnlyList<LegacyEventSchemas.EventType> AllowedTypes =>
        Context.Owner.ScenarioCreationTypes(this);
    public void Dispose()
    {
        if (Closed) return;
        // Capture only submitted values in the working document, never pending
        // WPF inputs. The owner map, including aliases, remains untouched.
        try
        {
            var record = Record; var native = !WorkingDocument.IsLegacyProject; var type = Type;
            Context.Remembered[type.RecordSignature] = (type,
                record.File.Data.Span.Slice(record.Offset + (native ? 8 : 16), native ? 8 : 32).ToArray(),
                record.File.Data.Span[record.Offset + (native ? 23 : 55)], Rectangle);
        }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException or InvalidDataException or KeyNotFoundException)
        { /* Malformed/replaced private records never poison the retained context. */ }
        finally { Closed = true; Context.Active = false; }
    }
}

/// <summary>Frozen result tied to both the live owner and private draft.</summary>
public sealed class ScenarioCreationPlan
{
    internal ScenarioCreationPlan(ScenarioCreationSession session, long revision, byte[] fingerprint,
        IReadOnlyDictionary<NativeFile, byte[]> result, ushort id, IReadOnlyList<HistoryOperation> children, long inputRevision, IReadOnlyList<ushort> ids)
    { Session = session; Revision = revision; Fingerprint = fingerprint; Result = result; EventID = id; Children = children; InputRevision = inputRevision; EventIDs = ids; }
    internal ScenarioCreationSession Session { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    internal IReadOnlyDictionary<NativeFile, byte[]> Result { get; }
    internal IReadOnlyList<HistoryOperation> Children { get; }
    internal long InputRevision { get; }
    public ushort EventID { get; }
    public IReadOnlyList<ushort> EventIDs { get; }
}

public sealed partial class NativeMapDocument
{
    public ScenarioCreationContext CreateScenarioCreationContext() => new(this);

    /// <summary>Original Modify starts from this live event, replacing the
    /// category temporary values. Its identity and links never become new records.</summary>
    public ScenarioCreationSession CreateScenarioEditingSession(ScenarioCreationContext context, NativeRecord record)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (transactionOpen || !ReferenceEquals(context.Owner, this) || context.Active)
            throw new InvalidOperationException("A current owner and an unused wizard context are required.");
        var current = CurrentScenarioRecord(record);
        var type = EntityReferences.EventType(current)
            ?? throw new InvalidOperationException("The existing event type is unverified.");
        var graph = ScriptGraph.Build(this); RequireUnambiguousGraph(graph);
        var id = ScriptGraph.ID(current); var fingerprint = UnusedScriptFingerprint();
        var copy = ClonePropertiesDocument();
        var initial = copy.Files.Values.ToDictionary(file => file, file => file.Data.ToArray());
        context.Active = true;
        return new(context, copy.Document, copy.Files, initial, EditRevision, fingerprint, id, null, editingExisting: true)
        { ExistingType = type, ExistingScopes = graph.Roots.Where(edge => graph.Reachable(edge.Target).Contains(id)).Select(edge => edge.Scope).ToHashSet() };
    }

    public ScenarioCreationSession CreateScenarioCreationSession(ScenarioCreationContext context,
        uint signature, LegacyEventSchemas.EventType? selectedType = null, ScriptEdge? insertion = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (transactionOpen || !ReferenceEquals(context.Owner, this) || context.Active)
            throw new InvalidOperationException("A current owner and an unused creation context are required.");
        if (signature is not (0xD5 or 0xD6)) throw new ArgumentException("Select a condition or action.");
        if (selectedType is not null && (selectedType.RecordSignature != signature
            || !ReferenceEquals(LegacyEventSchemas.Find(signature, selectedType.Type), selectedType)))
            throw new ArgumentException("The selected event type is not in this category.");
        if (insertion is not null)
        {
            insertion = CurrentScriptEdge(ScriptGraph.Build(this), insertion.Owner, insertion.Field);
            if (selectedType is null) throw new ArgumentException("Select the insertion type before opening its wizard.");
            if (LinkScopes(ScriptGraph.Build(this), insertion).Any(scope => !ScriptGraph.IsKnownScope(scope)
                || (LegacyEventSchemas.AllowedScopes(selectedType) & scope) == 0))
                throw new ArgumentException("This event is not supported by the insertion scope.");
        }
        var fingerprint = UnusedScriptFingerprint(); var copy = ClonePropertiesDocument();
        var working = copy.Document;
        var remembered = context.Remembered.GetValueOrDefault(signature);
        var type = remembered.Type ?? ScenarioTypeDefaults.InitialType(signature);
        var record = working.AddScenarioEvent(signature, new(0, 0), type);
        if (remembered.Type is not null)
        {
            remembered.Payload.CopyTo(record.File.Bytes, record.Offset + (IsLegacyProject ? 16 : 8));
            record.File.Bytes[record.Offset + (IsLegacyProject ? 55 : 23)] = remembered.Repeat;
        }
        if (selectedType is not null)
            working.Edit(selectedType.Name, () => working.WriteScenarioEventType(record, selectedType, false, save: false));
        var initial = copy.Files.Values.ToDictionary(file => file, file => file.Data.ToArray());
        context.Active = true;
        return new(context, working, copy.Files, initial, EditRevision, fingerprint, ScriptGraph.ID(record), insertion)
        { Rectangle = (selectedType ?? type).Name == "ACTION_CHANGESQUARE" ? remembered.Rectangle : null };
    }

    public bool IsScenarioCreationSessionCurrent(ScenarioCreationSession session) => session is not null && !session.Closed
        && ReferenceEquals(session.Context.Owner, this) && !transactionOpen && session.Revision == EditRevision
        && UnusedScriptFingerprint().AsSpan().SequenceEqual(session.Fingerprint);

    private void VerifyScenarioCreation(ScenarioCreationSession session)
    {
        if (!IsScenarioCreationSessionCurrent(session))
            throw new InvalidOperationException("Reopen creation after the owner changes or the wizard closes.");
        var working = session.WorkingDocument;
        if (working.transactionOpen || session.Files.Count != TransactionFiles().Count()
            || !session.Files.Values.ToHashSet().SetEquals(working.TransactionFiles()))
            throw new InvalidOperationException("The private creation file topology changed.");
        var record = working.CurrentScenarioRecord(session.Record);
        var type = session.Type;
        if (type.RecordSignature != (record.Category == "Project.TRIGGER" || !IsLegacyProject && record.File.UInt32(record.Offset) == 0xD5 ? 0xD5u : 0xD6u))
            throw new InvalidOperationException("The private event changed category.");
        if (session.IsEditingExisting && (!ScenarioCreationTypes(session).Contains(type)
            || ScriptGraph.Build(working).DuplicateIDs.Count != 0))
            throw new InvalidOperationException("The existing event type or private identity is outside its verified scope.");
        var masks = session.Files.Values.ToDictionary(file => file, file => new bool[file.Length]);
        Array.Fill(masks[record.File], true, record.Offset + (IsLegacyProject ? 16 : 8), IsLegacyProject ? 32 : 8);
        Array.Fill(masks[record.File], true, record.Offset + (IsLegacyProject ? 54 : 22), 2);
        if (working.CanEditScenarioNames)
            foreach (var kind in new[] { ScenarioNameKind.Variable, ScenarioNameKind.Timer })
                for (var index = 0; index < (kind == ScenarioNameKind.Variable ? 129 : 17); index++)
                {
                    var location = working.ScenarioAliasLocation(kind, index);
                    Array.Fill(masks[location.File], true, location.Offset, 64);
                }
        foreach (var (file, before) in session.Initial)
        {
            if(!working.IsLegacyProject && ReferenceEquals(file,working.NativeGateBindings))
            {
                var initialState=DecodeNativeGateState(new(file.SourcePath,file.ExportPath,before));
                var currentState=DecodeNativeGateState(file);
                if(!initialState.Identities.SequenceEqual(currentState.Identities))throw new InvalidOperationException("A creation child changed gate identities.");
                var initial=initialState.Links;
                var current=currentState.Links;var actionFile=working.NativeBindingFile(record.File);
                bool Other(NativeGateLink link)=>link.ActionFile!=actionFile||link.ActionOffset!=record.Offset;
                if(!initial.Where(Other).SequenceEqual(current.Where(Other)))throw new InvalidOperationException("A creation child changed another event's gate binding.");
                continue;
            }
            if (file.Length != before.Length) throw new InvalidOperationException("A creation child changed collection length.");
            for (var index = 0; index < before.Length; index++)
                if (before[index] != file.Data.Span[index] && !masks[file][index])
                    throw new InvalidOperationException("A creation child changed terrain, objects, links, headers or unrelated bytes.");
        }
        if (working.CanEditScenarioNames)
            foreach (var slot in working.ReadScenarioNames().Slots)
            {
                var location = working.ScenarioAliasLocation(slot.Kind, slot.Index);
                var bytes = location.File.Data.Span.Slice(location.Offset, 64);
                if (!bytes.SequenceEqual(session.Initial[location.File].AsSpan(location.Offset, 64))
                    && !bytes.SequenceEqual(ScenarioAlias.Encode(slot.StoredName)))
                    throw new InvalidOperationException("A creation child changed reserved alias padding.");
            }
    }

    public void SelectScenarioCreationType(ScenarioCreationSession session, LegacyEventSchemas.EventType type)
    {
        VerifyScenarioCreation(session);
        if (!ScenarioCreationTypes(session).Contains(type)) throw new ArgumentException("This event is not supported by the insertion scope.");
        session.WorkingDocument.Edit(type.Name, () => session.WorkingDocument.WriteScenarioEventType(session.Record, type, false, save: false));
        if (type.Name != "ACTION_CHANGESQUARE") { session.Rectangle = null; session.InputRevision++; }
    }

    internal IReadOnlyList<LegacyEventSchemas.EventType> ScenarioCreationTypes(ScenarioCreationSession session)
    {
        var signature = session.Type.RecordSignature;
        var scopes = session.IsEditingExisting
            ? session.ExistingScopes
            : session.Insertion is { } edge ? LinkScopes(ScriptGraph.Build(this), edge) : new HashSet<uint>();
        // Keep an imported verified type without silently repairing a scope
        // mismatch; every changed type must fit ALL shared root scopes.
        var imported = session.ExistingType;
        return Array.AsReadOnly(LegacyEventSchemas.All.Where(type => type.RecordSignature == signature
            && (ReferenceEquals(type, imported) || scopes.All(scope => ScriptGraph.IsKnownScope(scope) && (LegacyEventSchemas.AllowedScopes(type) & scope) != 0))).ToArray());
    }

    public void StoreScenarioCreationRepeat(ScenarioCreationSession session, ScenarioRepeatMode mode, string count)
    {
        VerifyScenarioCreation(session);
        if (session.Type.RecordSignature != 0xD5) throw new ArgumentException("Actions have no condition repeat page.");
        var record = session.Record; var field = record.Fields.Single(field => field.Name is "Repeat" or "repeat");
        session.WorkingDocument.SetField(record, field, ScenarioControlPages.Repeat(mode, count).ToString(CultureInfo.InvariantCulture));
    }

    public ScenarioCreationPlan PrepareScenarioCreation(ScenarioCreationSession session)
    {
        VerifyScenarioCreation(session);
        var working = session.WorkingDocument; var source = session.Record; var type = session.Type;
        if (session.IsEditingExisting && session.Rectangle is not null)
            throw new InvalidOperationException("Rectangle expansion is creation-only.");
        if (session.Rectangle is { } countBounds)
        {
            var graph = ScriptGraph.Build(this); RequireUnambiguousGraph(graph);
            var used = graph.Events.Values.Count(item => !IsLegacyProject || item.ID >= 32768);
            if ((long)countBounds.Width * countBounds.Height > (IsLegacyProject ? 32768 : 65535) - used)
                throw new InvalidOperationException("The rectangle needs more free action identities than this map has.");
        }
        var copy = ClonePropertiesDocument(); var result = copy.Document;
        NativeRecord added;
        if (session.IsEditingExisting)
        {
            added = ScriptGraph.Build(result).Events[session.EventID].Record;
            added.File.Bytes[added.Offset + (IsLegacyProject ? 54 : 22)] = (byte)type.Type;
        }
        else if (session.Insertion is { } edge)
        {
            var owner = (edge.Owner.Category.StartsWith("Working.", StringComparison.Ordinal) ? ScriptGraph.Build(result).Roots.Select(root => root.Owner) : result.Records()).Single(record => record.File == copy.Files[edge.Owner.File]
                && record.Offset == edge.Owner.Offset && record.Length == edge.Owner.Length);
            added = result.InsertScriptEvent(owner, owner.Fields.Single(field => field.Name == edge.Field.Name), type);
        }
        else added = result.AddScenarioEvent(type.RecordSignature, new(0, 0), type);
        var ids = new List<ushort> { ScriptGraph.ID(added) };
        var offset = IsLegacyProject ? 16 : 8; var length = IsLegacyProject ? 32 : 8;
        source.File.Data.Span.Slice(source.Offset + offset, length).CopyTo(added.File.Bytes.AsSpan(added.Offset + offset, length));
        if(!IsLegacyProject && IsNativeDestroyAction(source))
        {
            var selected=session.WorkingDocument.NativeGateTarget(source);
            var address=selected is null?null:session.WorkingDocument.NativeGateAddressOf(selected);
            result.SetNativeGateTarget(added,address is null?null:result.NativeGateAddresses(result.NativeGateBindingRecords()).GetValueOrDefault(address));
        }
        if (ScenarioTypeDefaults.ClearsParametersOnSave(type.Name))
            Array.Clear(added.File.Bytes, added.Offset + offset, length);
        added.File.Bytes[added.Offset + (IsLegacyProject ? 55 : 23)] = source.File.Data.Span[source.Offset + (IsLegacyProject ? 55 : 23)];
        if (session.Rectangle is { } rectangle)
        {
            if (type.Name != "ACTION_CHANGESQUARE") throw new InvalidOperationException("Only new cell actions expand rectangles.");
            var payload = added.File.Data.Span.Slice(added.Offset + offset, length).ToArray();
            var first = result.CreateScenarioParameterDraft(added);
            result.ApplyScenarioParameterDraft(result.UpdateScenarioSpatialPoint(first, new(rectangle.X, rectangle.Y)));
            // Original Save visits x first, then y, inserting each action after
            // the previously selected action and preserving the existing tail.
            for (var y = rectangle.Y; y < rectangle.Y + rectangle.Height; y++)
                for (var x = rectangle.X; x < rectangle.X + rectangle.Width; x++)
                {
                    if (x == rectangle.X && y == rectangle.Y) continue;
                    var previous = ScriptGraph.Build(result).Events[ids[^1]];
                    var next = previous.Edges.Single(edge => edge.Field.Name is "NextOr" or "or" or "next");
                    var cell = result.InsertScriptEvent(previous.Record, next.Field, type);
                    payload.CopyTo(cell.File.Bytes, cell.Offset + offset);
                    cell.File.Bytes[cell.Offset + (IsLegacyProject ? 55 : 23)] = source.File.Data.Span[source.Offset + (IsLegacyProject ? 55 : 23)];
                    result.ApplyScenarioParameterDraft(result.UpdateScenarioSpatialPoint(result.CreateScenarioParameterDraft(cell), new(x, y)));
                    ids.Add(ScriptGraph.ID(cell));
                }
        }
        if (working.CanEditScenarioNames)
            foreach (var kind in new[] { ScenarioNameKind.Variable, ScenarioNameKind.Timer })
                for (var index = 0; index < (kind == ScenarioNameKind.Variable ? 129 : 17); index++)
                {
                    var from = working.ScenarioAliasLocation(kind, index); var to = result.ScenarioAliasLocation(kind, index);
                    from.File.Data.Span.Slice(from.Offset, 64).CopyTo(to.File.Bytes.AsSpan(to.Offset, 64));
                }
        var bytes = session.Files.Keys.ToDictionary(file => file, file => copy.Files[file].Data.ToArray());
        return new(session, working.EditRevision, working.UnusedScriptFingerprint(),
            new ReadOnlyDictionary<NativeFile, byte[]>(bytes), ids[^1], Array.AsReadOnly(working.GetHistory().Undo.ToArray()), session.InputRevision, ids.AsReadOnly());
    }

    public bool IsScenarioCreationPlanCurrent(ScenarioCreationPlan plan) => plan is not null
        && IsScenarioCreationSessionCurrent(plan.Session) && !plan.Session.WorkingDocument.transactionOpen
        && plan.Revision == plan.Session.WorkingDocument.EditRevision
        && plan.InputRevision == plan.Session.InputRevision
        && plan.Session.WorkingDocument.UnusedScriptFingerprint().AsSpan().SequenceEqual(plan.Fingerprint);

    public NativeRecord ApplyScenarioCreation(ScenarioCreationPlan plan) => ApplyScenarioCreationCore(plan, editorSave: false);

    /// <summary>Original accepted Finish: one source count per saved event,
    /// including equal Modify and each cell of a rectangle expansion. Trusted
    /// draft APIs retain their ordinary no-op and atomic-operation semantics.</summary>
    public NativeRecord ApplyScenarioEditorCreation(ScenarioCreationPlan plan) => ApplyScenarioCreationCore(plan, editorSave: true);

    private NativeRecord ApplyScenarioCreationCore(ScenarioCreationPlan plan, bool editorSave)
    {
        if (editorSave && (transactionOpen || activePenStroke is not null))
            throw new InvalidOperationException("Finish the current edit before accepting a scenario wizard.");
        if (!IsScenarioCreationPlanCurrent(plan)) throw new InvalidOperationException("The live map or private creation draft changed.");
        var acceptedCalls = checked((uint)plan.EventIDs.Count);
        if (editorSave && acceptedCalls == 0) throw new InvalidOperationException("The wizard has no saved event.");
        if (editorSave)
        {
            // Overflow must reject before changing bytes/history or disposing
            // the private session, so the user can still cancel or retry.
            _ = checked(changeCount + 1); _ = checked(EditRevision + 1);
        }
        var unchanged = plan.Result.All(row => row.Key.Data.Span.SequenceEqual(row.Value));
        if (editorSave && unchanged)
        {
            if (acceptedCalls != 1 || !plan.Session.IsEditingExisting)
                throw new InvalidOperationException("A new wizard must produce its saved event bytes.");
            CountAcceptedUnchangedScenarioSave();
        }
        else
        {
            EditCore(plan.Session.IsEditingExisting ? "Edit scenario event" : "Add scenario event", () =>
            {
                foreach (var row in plan.Result) row.Key.Bytes = (byte[])row.Value.Clone();
                if (historyDepth != 0) operationChildren.Peek().AddRange(plan.Children);
            }, editorSave ? () => new EditorDrawingCompletion(true, acceptedCalls) : null);
        }
        // Save-time zeroing is part of the retained original temporary values.
        var record = ScriptGraph.Build(this).Events[plan.EventID].Record;
        var temporary = plan.Session.Record;
        if (ScenarioTypeDefaults.ClearsParametersOnSave(plan.Session.Type.Name))
            Array.Clear(temporary.File.Bytes, temporary.Offset + (IsLegacyProject ? 16 : 8), IsLegacyProject ? 32 : 8);
        if (plan.Session.Rectangle is { } rectangle)
        {
            foreach (var (number, value) in new[] { (5, rectangle.X + 1), (7, rectangle.Y + 1) })
            {
                var field = temporary.Fields.Single(field => field.Name == (IsLegacyProject ? "param" : "Param") + number);
                field.Encode(value.ToString(CultureInfo.InvariantCulture)).CopyTo(temporary.File.Bytes, temporary.Offset + field.Offset);
            }
            // Original Save clears ChangeRect after expanding, but Cancel
            // retains it with the submitted temporary values.
            plan.Session.Rectangle = null;
        }
        plan.Session.Dispose();
        return record;
    }
    public void UpdateScenarioCreationRectangle(ScenarioCreationSession session, TileRect? rectangle)
    {
        VerifyScenarioCreation(session);
        if (session.IsEditingExisting || session.Type.Name != "ACTION_CHANGESQUARE") throw new ArgumentException("Select a new cell action.");
        if (rectangle is { } bounds)
        {
            if (bounds.Width < 1 || bounds.Height < 1 || bounds.X < 0 || bounds.Y < 0
                || (long)bounds.X + bounds.Width > Width || (long)bounds.Y + bounds.Height > Height
                || (long)bounds.X + bounds.Width > 1000 || (long)bounds.Y + bounds.Height > 1000)
                throw new ArgumentOutOfRangeException(nameof(rectangle), "Choose an original-range rectangle on this map.");
        }
        session.Rectangle = rectangle; session.InputRevision++;
    }
}
