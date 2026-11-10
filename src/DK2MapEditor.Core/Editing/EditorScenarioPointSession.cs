using System.Collections.ObjectModel;

namespace DK2MapEditor.Core.Editing;

public enum ScenarioPointChoiceStatus { Available, CurrentOwner, AlreadyBound, ReservedIdentity, AmbiguousIdentity, UnsupportedFormat }
public sealed record ScenarioPointOwnerChoice(ScriptOwner Owner, ScenarioPointChoiceStatus Status)
{
    public bool CanSelect => Status is ScenarioPointChoiceStatus.Available or ScenarioPointChoiceStatus.CurrentOwner;
}

/// <summary>A private original Action Point selection. Preview never allocates
/// an identity or writes to the source document.</summary>
public sealed class EditorScenarioPointSession : IDisposable
{
    internal EditorScenarioPointSession(NativeMapDocument owner, ScriptEdge? existing, ScriptEdge? before, uint scope, long revision, byte[] fingerprint)
    { Owner=owner; Existing=existing; InsertBefore=before; Scope=scope; Revision=revision; Fingerprint=fingerprint; }
    internal NativeMapDocument Owner { get; }
    internal ScriptEdge? Existing { get; }
    internal ScriptEdge? InsertBefore { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    internal long InputRevision { get; set; }
    public bool IsModifying => Existing is not null;
    public uint Scope { get; internal set; }
    public ScriptOwner? SelectedOwner { get; internal set; }
    public bool Closed { get; private set; }
    public IReadOnlyList<ScenarioPointOwnerChoice> Choices => Owner.EditorScenarioPointChoices(this);
    public void SelectScope(uint scope) => Owner.SelectEditorScenarioPointScope(this,scope);
    public void SelectOwner(ScriptOwner owner) => Owner.SelectEditorScenarioPointOwner(this,owner);
    public void Dispose() => Closed=true;
}

public sealed class EditorScenarioPointPlan
{
    internal EditorScenarioPointPlan(EditorScenarioPointSession session,long inputRevision,ScriptOwner destination,IReadOnlyDictionary<NativeFile,byte[]> result,IReadOnlyList<HistoryOperation> children)
    { Session=session;InputRevision=inputRevision;Destination=destination;Result=result;Children=children; }
    internal EditorScenarioPointSession Session { get; }
    internal long InputRevision { get; }
    internal ScriptOwner Destination { get; }
    internal IReadOnlyDictionary<NativeFile,byte[]> Result { get; }
    internal IReadOnlyList<HistoryOperation> Children { get; }
}

public sealed partial class NativeMapDocument
{
    private static bool PointUniqueScope(uint scope) => scope is 2 or 8 or 16 or 64 or 128 or 256;
    private static bool SamePointOwner(ScriptOwner a,ScriptOwner b) => a.Scope==b.Scope && a.MemberIndex==b.MemberIndex
        && a.PlayerID==b.PlayerID && SameRecord(a.Record,b.Record);

    public EditorScenarioPointSession CreateEditorScenarioPointSession(ScriptEdge? existing=null,ScriptEdge? insertBefore=null,uint initialScope=1)
    {
        if(transactionOpen || activePenStroke is not null)throw new InvalidOperationException("Finish the active edit before opening an Action Point.");
        var graph=ScriptGraph.Build(this);RequireUnambiguousGraph(graph);
        if(existing is not null)existing=CurrentScopeRoot(graph,existing);
        if(insertBefore is not null)insertBefore=CurrentScopeRoot(graph,insertBefore);
        if(existing is not null && insertBefore is not null)throw new ArgumentException("Choose Modify or Insert, not both.");
        if(!IsLegacyProject && insertBefore is not null)throw new InvalidOperationException("Native entity roots have no stored Action Point list order.");
        var scope=existing?.Scope??initialScope;
        if(!ScriptGraph.IsKnownScope(scope))throw new ArgumentException("Select one of the original nine owner scopes.");
        var session=new EditorScenarioPointSession(this,existing,insertBefore,scope,EditRevision,UnusedScriptFingerprint());
        if(existing is not null)session.SelectedOwner=OwnerForScriptScope(existing);
        return session;
    }

    public bool IsEditorScenarioPointSessionCurrent(EditorScenarioPointSession session) => session is not null && !session.Closed
        && ReferenceEquals(session.Owner,this) && !transactionOpen && activePenStroke is null && session.Revision==EditRevision
        && UnusedScriptFingerprint().AsSpan().SequenceEqual(session.Fingerprint);

    private void RequirePointSession(EditorScenarioPointSession session)
    { if(!IsEditorScenarioPointSessionCurrent(session))throw new InvalidOperationException("The Action Point selection is closed, foreign or stale."); }

    internal void SelectEditorScenarioPointScope(EditorScenarioPointSession session,uint scope)
    {
        RequirePointSession(session);
        if(!ScriptGraph.IsKnownScope(scope) || session.IsModifying && scope!=session.Scope)throw new ArgumentException("An existing Action Point keeps its original scope.");
        if(scope==session.Scope)return;
        var revision=checked(session.InputRevision+1);session.Scope=scope;session.SelectedOwner=null;session.InputRevision=revision;
    }
    internal void SelectEditorScenarioPointOwner(EditorScenarioPointSession session,ScriptOwner requested)
    {
        RequirePointSession(session);var owner=CurrentScriptOwner(requested);
        var choice=EditorScenarioPointChoices(session).SingleOrDefault(item=>SamePointOwner(item.Owner,owner));
        if(choice is not {CanSelect:true})throw new InvalidOperationException("The selected owner cannot accept this Action Point.");
        if(session.SelectedOwner is { } selected && SamePointOwner(selected,owner))return;
        var revision=checked(session.InputRevision+1);session.SelectedOwner=owner;session.InputRevision=revision;
    }

    internal IReadOnlyList<ScenarioPointOwnerChoice> EditorScenarioPointChoices(EditorScenarioPointSession session)
    {
        RequirePointSession(session);
        // Resolve and index the graph once for the whole list. Calling
        // FindScriptScope for each displayed owner rebuilds large maps N times.
        var bindings=ScriptBindings();
        var current=session.Existing is null?null:bindings.Single(binding=>SameField(binding.Root.Owner,binding.Root.Field,session.Existing.Owner,session.Existing.Field)).Owner;
        var allOwners=ScriptOwners();var owners=allOwners.Where(owner=>owner.Scope==session.Scope);
        var nativeStates=NativeOwnerStates();
        var nativeIDs=nativeStates.Values.GroupBy(state=>state.ID).ToDictionary(group=>group.Key,group=>group.Count());
        static (uint,NativeFile,int,int,uint) Key(ScriptOwner owner)=>(owner.Scope,owner.Record.File,owner.Record.Offset,owner.MemberIndex,owner.PlayerID);
        var roots=bindings.Where(binding=>binding.Owner is not null).GroupBy(binding=>Key(binding.Owner!)).ToDictionary(group=>group.Key,group=>group.Select(binding=>binding.Root).ToArray());
        var ids=IsLegacyProject?Records().SelectMany(record=>record.Fields.Where(field=>field.Name.EndsWith("uniqueid",StringComparison.Ordinal)).Select(record.Read)).Where(id=>id!="0").GroupBy(id=>id).ToDictionary(group=>group.Key,group=>group.Count()):new Dictionary<string,int>();
        var areas=IsLegacyProject?allOwners.Where(owner=>owner.Scope==4).GroupBy(owner=>owner.Record.Read(owner.Record.Fields.Single(field=>field.Name=="id"))).ToDictionary(group=>group.Key,group=>group.Count()):new Dictionary<string,int>();
        IReadOnlyList<NativeField>? fields=null;
        if(IsLegacyProject && LegacyAddChoices().Contains("TriggerLink"))fields=LegacyProjectSchemas.Fields(LegacyStructure(projectSections.Single(section=>section.Signature==LegacyEvents),"TriggerLink"));
        var result=new List<ScenarioPointOwnerChoice>();
        foreach(var owner in owners)
        {
            var status=ScenarioPointChoiceStatus.Available;
            var bound=roots.GetValueOrDefault(Key(owner))??[];
            var own=current is not null && SamePointOwner(current,owner);
            if(IsLegacyProject)
            {
                var target=ScopeTarget(owner.Scope);var id=owner.Scope is 1 or 32?"0":owner.Record.Read(owner.Record.Fields.Single(field=>field.Name==ScopeIdentity(owner)));
                if(fields is null || target.Length!=0 && !fields.Any(field=>field.Name==target))status=ScenarioPointChoiceStatus.UnsupportedFormat;
                else if(bound.Length>1 || PointUniqueScope(owner.Scope) && id!="0" && ids.GetValueOrDefault(id)!=1 || owner.Scope==4 && areas.GetValueOrDefault(id)!=1)status=ScenarioPointChoiceStatus.AmbiguousIdentity;
                else if(PointUniqueScope(owner.Scope) && id!="0" && !own)status=bound.Length==0?ScenarioPointChoiceStatus.ReservedIdentity:ScenarioPointChoiceStatus.AlreadyBound;
                else if(bound.Length!=0 && !own)status=ScenarioPointChoiceStatus.AlreadyBound;
                else if(own)status=ScenarioPointChoiceStatus.CurrentOwner;
            }
            else
            {
                var state=PointUniqueScope(owner.Scope)?nativeStates.GetValueOrDefault((owner.Record.File,owner.Record.Offset,owner.Record.Length)):null;
                if(state is not null && (state.ID==0 || nativeIDs.GetValueOrDefault(state.ID)>1))status=ScenarioPointChoiceStatus.AmbiguousIdentity;
                else if((state?.Present==true || !PointUniqueScope(owner.Scope) && bound.Any(HasStoredScriptScope)) && !own)status=ScenarioPointChoiceStatus.AlreadyBound;
                else if(state is not null && !own)status=ScenarioPointChoiceStatus.ReservedIdentity;
                else if(own)status=ScenarioPointChoiceStatus.CurrentOwner;
            }
            // The source's Level type page disables Finish once a Level
            // point exists, including the Modify page itself.
            if(session.IsModifying && owner.Scope==32)status=ScenarioPointChoiceStatus.AlreadyBound;
            result.Add(new(owner,status));
        }
        return Array.AsReadOnly(result.ToArray());
    }

    public EditorScenarioPointPlan PrepareEditorScenarioPoint(EditorScenarioPointSession session)
    {
        RequirePointSession(session);
        if(session.SelectedOwner is not { } destination)throw new InvalidOperationException("Select an available Action Point owner.");
        if(!session.Choices.Any(choice=>choice.CanSelect && SamePointOwner(choice.Owner,destination)))throw new InvalidOperationException("The owner cannot be accepted.");
        var copy=ClonePropertiesDocument();
        ScriptOwner OwnerCopy(ScriptOwner owner) => owner with {Record=owner.Record with {File=copy.Files[owner.Record.File]},
            BandRecord=owner.BandRecord is null?null:owner.BandRecord with {File=copy.Files[owner.BandRecord.File]}};
        ScriptEdge? RootCopy(ScriptEdge? root) => root is null?null:root with {Owner=root.Owner with {File=copy.Files[root.Owner.File]}};
        copy.Document.StoreEditorScenarioPoint(OwnerCopy(destination),RootCopy(session.Existing),RootCopy(session.InsertBefore));
        var result=copy.Files.ToDictionary(pair=>pair.Key,pair=>pair.Value.Data.ToArray());
        return new(session,session.InputRevision,destination,new ReadOnlyDictionary<NativeFile,byte[]>(result),Array.AsReadOnly(copy.Document.GetHistory().Undo.ToArray()));
    }

    private void StoreEditorScenarioPoint(ScriptOwner destination,ScriptEdge? existing,ScriptEdge? insertBefore)
    {
        Edit("Save scenario point",()=>
        {
            if(!IsLegacyProject)
            {
                if(existing is not null && PointUniqueScope(existing.Scope))
                {
                    var old=CurrentScopeRoot(ScriptGraph.Build(this),existing);var target=FindScriptScope(destination)!;
                    if(SameField(old.Owner,old.Field,target.Owner,target.Field)){ReallocateNativeOwnerIdentity(old.Owner);SetEditorNativeMemberBandOperand(destination);return;}
                    // Free the old physical identity before allocating the new
                    // one. The private draft rolls back if branch validation fails.
                    SetScriptLink(old.Owner,old.Field,0);RemoveNativeOwnerScope(old,releaseIdentity:true);
                    SetScriptLink(target.Owner,target.Field,old.Target);EnsureNativeOwnerScope(target.Owner);
                }
                else if(existing is not null)_=ReassignScriptScope(existing,destination);
                else _=EnsureScriptScope(destination);
                SetEditorNativeMemberBandOperand(destination);
                return;
            }
            ScriptEdge root;
            if(existing is null)
            {
                var link=AddLegacyRecord("TriggerLink",new(0,0));
                if(insertBefore is not null)
                {
                    var bytes=link.File.Data.ToArray();var entry=bytes.AsSpan(link.Offset,link.Length).ToArray();
                    if(insertBefore.Owner.File!=link.File || insertBefore.Owner.Offset>link.Offset)throw new ArgumentException("Select a current point insertion anchor.");
                    bytes.AsSpan(insertBefore.Owner.Offset,link.Offset-insertBefore.Owner.Offset).CopyTo(bytes.AsSpan(insertBefore.Owner.Offset+link.Length));
                    entry.CopyTo(bytes,insertBefore.Owner.Offset);link.File.Bytes=bytes;
                    link=Records().Single(record=>record.File==link.File && record.Offset==insertBefore.Owner.Offset);
                }
                WriteProjectScopeOwner(link,destination);
                root=FindScriptScope(destination)!;
            }
            else
            {
                root=CurrentScopeRoot(ScriptGraph.Build(this),existing);
                var old=OwnerForScriptScope(root);
                // The isolated copy performs the source's explicit handoff.
                // The generic SetField API correctly refuses to clear a bound
                // identity or reuse an ID still stored in the old root.
                if(old is not null && PointUniqueScope(old.Scope))WritePointIdentity(old,"0");
                if(PointUniqueScope(destination.Scope))
                {
                    var id=NextEditorPointUniqueID(root.Owner);
                    WritePointIdentity(destination,id.ToString());
                }
                WriteProjectScopeOwner(root.Owner,destination);
            }
            if(destination.Scope==16)
            {
                var index=Records().Where(IsBand).ToList().FindIndex(record=>SameRecord(record,destination.Record));
                if(index<0)throw new InvalidOperationException("The selected member has no physical band.");
                SetField(root.Owner,root.Owner.Fields.Single(field=>field.Name=="band"),index.ToString());
            }
        });
    }

    private static void WritePointIdentity(ScriptOwner owner,string value)
    {
        var field=owner.Record.Fields.Single(field=>field.Name==ScopeIdentity(owner));
        field.Encode(value).CopyTo(owner.Record.File.Bytes,owner.Record.Offset+field.Offset);
    }

    private uint NextEditorPointUniqueID(NativeRecord modifiedRoot)
    {
        // The source first frees the previous owner. Its own old root must not
        // reserve that freed ID; other entity IDs and orphan roots still do.
        var used=Records().SelectMany(record=>record.Fields.Where(field=>field.Name.EndsWith("uniqueid",StringComparison.Ordinal)).Select(field=>uint.Parse(record.Read(field)))).ToHashSet();
        foreach(var link in Records().Where(record=>record.Category.StartsWith("Project.TRIGGERLINK",StringComparison.Ordinal) && !SameRecord(record,modifiedRoot)))
        {
            var scope=uint.Parse(link.Read(link.Fields.Single(field=>field.Name=="node.type")));
            if(!PointUniqueScope(scope))continue;
            if(link.Fields.FirstOrDefault(field=>field.Name==ScopeTarget(scope)) is { } target)used.Add(uint.Parse(link.Read(target)));
        }
        uint candidate=1;while(used.Contains(candidate))candidate=checked(candidate+1);return candidate;
    }

    public ScriptEdge ApplyEditorScenarioPoint(EditorScenarioPointPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);RequirePointSession(plan.Session);
        if(plan.InputRevision!=plan.Session.InputRevision)throw new InvalidOperationException("The private Action Point selection changed.");
        _=checked(changeCount+1);_=checked(EditRevision+1);
        var unchanged=plan.Result.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value));
        if(unchanged)CountAcceptedUnchangedScenarioSave();
        else Edit("Save scenario point",()=>
        {
            foreach(var pair in plan.Result)pair.Key.Bytes=(byte[])pair.Value.Clone();
            if(historyDepth!=0)operationChildren.Peek().AddRange(plan.Children);
        });
        var root=FindScriptScope(plan.Destination)??throw new InvalidDataException("The saved Action Point has no physical owner.");
        plan.Session.Dispose();return root;
    }
}
