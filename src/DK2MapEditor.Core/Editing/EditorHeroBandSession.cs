using System.Collections.ObjectModel;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record EditorHeroMemberValues(uint CreatureID, int Level = 1, uint Enemy = 3,
    uint Health = 100, uint GoldHeld = 0, uint Options = 0, uint Options2 = 45,
    uint Objective = 19, uint Area = 0);
public sealed record EditorHeroBandMember(int Index, EditorHeroMemberValues Values, uint ScenarioIdentity, bool HasScenario)
{
    public bool IsLeader => (Values.Options2 & 2) != 0;
}

/// <summary>The original hero-band parent owns every nested member change.
/// Cancel discards this independent payload; only parent Save writes the map.</summary>
public sealed class EditorHeroBandSession : IDisposable
{
    internal EditorHeroBandSession(NativeMapDocument owner, NativeMapDocument working,
        Dictionary<NativeFile, NativeFile> files, NativeRecord band, NativeRecord? original,
        long revision, byte[] fingerprint)
    { Owner=owner; Working=working; Files=files; Band=band; Original=original; Revision=revision; Fingerprint=fingerprint; }
    internal NativeMapDocument Owner { get; }
    internal NativeMapDocument Working { get; }
    internal Dictionary<NativeFile, NativeFile> Files { get; }
    internal NativeRecord Band { get; }
    internal NativeRecord? Original { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    internal long InputRevision { get; set; }
    internal int[] NativeMemberOrigins { get; } = Enumerable.Range(0,16).ToArray();
    public bool Closed { get; private set; }
    public bool IsNew => Original is null;
    public bool HasStoredName => Working.IsLegacyProject;
    public uint ID => uint.Parse(Band.Read(Band.Fields.Single(f=>f.Name==(HasStoredName?"id":"bBandID"))),CultureInfo.InvariantCulture);
    public string Name => HasStoredName ? Band.Read(Band.Fields.Single(f=>f.Name=="name")) : "";
    public IReadOnlyList<EditorHeroBandMember> Members => Owner.ReadEditorHeroBandMembers(this);
    public void SetName(string name) => Owner.SetEditorHeroBandName(this,name);
    public int Add(EditorHeroMemberValues values) => Owner.AddEditorHeroMember(this,values);
    public void Replace(IReadOnlyList<int> indices,EditorHeroMemberValues values) => Owner.ReplaceEditorHeroMembers(this,indices,values);
    public void Remove(IReadOnlyList<int> indices) => Owner.RemoveEditorHeroMembers(this,indices);
    public EditorHeroBandMemberDraft CreateMemberDraft(int index) => Owner.CreateEditorHeroMemberDraft(this,index);
    public void ApplyMemberDraft(EditorHeroBandMemberDraft draft) => Owner.ApplyEditorHeroMemberDraft(this,draft);
    public void Dispose() => Closed=true;
}

/// <summary>A nested member editor can cancel independently of its parent.</summary>
public sealed class EditorHeroBandMemberDraft : IDisposable
{
    internal EditorHeroBandMemberDraft(EditorHeroBandSession session,int index,long revision,EditorHeroMemberValues values)
    { Session=session; Index=index; Revision=revision; Values=values; }
    internal EditorHeroBandSession Session { get; }
    internal long Revision { get; }
    public int Index { get; }
    public EditorHeroMemberValues Values { get; set; }
    public bool Closed { get; private set; }
    public void Dispose() => Closed=true;
}

public sealed class EditorHeroBandPlan
{
    internal EditorHeroBandPlan(EditorHeroBandSession session,long inputRevision,byte[] fingerprint,IReadOnlyDictionary<NativeFile,byte[]> result)
    { Session=session; InputRevision=inputRevision; Fingerprint=fingerprint; Result=result; }
    internal EditorHeroBandSession Session { get; }
    internal long InputRevision { get; }
    internal byte[] Fingerprint { get; }
    internal IReadOnlyDictionary<NativeFile,byte[]> Result { get; }
}

public sealed partial class NativeMapDocument
{
    public EditorHeroBandSession CreateEditorHeroBandSession(NativeRecord? original=null)
    {
        if(transactionOpen || activePenStroke is not null)throw new InvalidOperationException("Finish the active edit before opening a hero band.");
        // Original Heros_SelectChange refuses New at twenty-four bands.
        // Existing bands remain editable; generic collection APIs are separate.
        if(original is null && Records().Count(IsBand)>=24)throw new InvalidOperationException("A map can contain at most twenty-four hero bands in the editor.");
        if(original is not null)RequireEditorHeroBandInputLayout(original);
        var fingerprint=UnusedScriptFingerprint();var copy=ClonePropertiesDocument();var working=copy.Document;
        NativeRecord band;
        working.UpgradePrivateEditorHeroBandSection();
        if(original is not null)
        {
            var offset=IsLegacyProject ? 12+(original.Offset-12)/original.Length*LegacyProjectSchemas.Size("MTHBAND") : original.Offset;
            band=working.Records().Single(r=>r.File==copy.Files[original.File] && r.Offset==offset && IsBand(r));
        }
        else
        {
            var used=Records().Where(IsBand).Select(r=>uint.Parse(r.Read(r.Fields.Single(f=>f.Name==(IsLegacyProject?"id":"bBandID"))),CultureInfo.InvariantCulture)).ToHashSet();
            uint id=0;while(used.Contains(id))id=checked(id+1);
            if(!IsLegacyProject && id>byte.MaxValue)throw new InvalidOperationException("No free native hero-band ID remains.");
            if(IsLegacyProject)
            {
                var file=working.projectSections.Single(f=>f.Signature==LegacyBands);
                var bytes=new byte[LegacyProjectSchemas.Size("MTHBAND")];
                var field=LegacyProjectSchemas.Fields("MTHBAND").Single(f=>f.Name=="id");
                field.Encode(id.ToString(CultureInfo.InvariantCulture)).CopyTo(bytes,field.Offset);
                var offset=working.InsertLegacyRecord(file,"MTHBAND",bytes);
                band=working.Records().Single(r=>r.File==file && r.Offset==offset);
            }
            else
            {
                var template=RecordTemplate.All.Single(t=>t.Structure=="GAMEBAND");
                var file=working.CollectionFile(template.FileSignature);
                var offset=working.InsertRecord(file,template.Create(new(0,0),1,(ushort)id));
                band=working.Records().Single(r=>r.File==file && r.Offset==offset && r.Category=="GAMEBAND");
            }
        }
        working.RequireEditorHeroBandLayout(band);
        return new(this,working,copy.Files,band,original,EditRevision,fingerprint);
    }

    private BandLayout RequireEditorHeroBandLayout(NativeRecord band)
    {
        var layout=GetBandLayout(band);
        if(layout.Project && band.File.UInt32(4)!=5 || !layout.Project && band.File.UInt32(band.Offset+LegacySchemas.Offset("GAMEBAND","dwBandVersion"))!=9)
            throw new InvalidDataException("Unverified hero-band parent layout.");
        _=ContiguousBandCount(layout);return layout;
    }
    public bool IsEditorHeroBandSessionCurrent(EditorHeroBandSession session) => session is not null && !session.Closed
        && ReferenceEquals(session.Owner,this) && !transactionOpen && activePenStroke is null && session.Revision==EditRevision
        && UnusedScriptFingerprint().AsSpan().SequenceEqual(session.Fingerprint);
    private void RequireEditorHeroBandSession(EditorHeroBandSession session)
    { if(!IsEditorHeroBandSessionCurrent(session))throw new InvalidOperationException("The hero-band parent is closed, foreign or stale."); }

    internal IReadOnlyList<EditorHeroBandMember> ReadEditorHeroBandMembers(EditorHeroBandSession session)
    {
        RequireEditorHeroBandSession(session);var layout=session.Working.RequireEditorHeroBandLayout(session.Band);
        uint U(int index,string project,string native)=>uint.Parse(layout.Read(index,layout.Project?project:native),CultureInfo.InvariantCulture);
        return Enumerable.Range(0,session.Working.ContiguousBandCount(layout)).Select(index=>new EditorHeroBandMember(index,
            new(U(index,"id","bGlobalCreatureID"),(int)U(index,"level","bGlobalLevel"),U(index,"enemy","bGlobalPlayer"),
                U(index,"health","dwGlobalHealth"),U(index,"goldheld","bGlobalGoldHeld"),U(index,"options","bHeroOptions"),
                U(index,"options2","bGlobalBehavior"),U(index,"objective","bHeroObjective"),U(index,"area","bHeroTargetArea")),
            U(index,"uniqueid","wHeroTrigger"),layout.Project?session.Working.MemberScopeLinks(U(index,"uniqueid","wHeroTrigger").ToString(CultureInfo.InvariantCulture)).Any():session.Working.NativeOwnerState(session.Working.Records().Single(row=>row.File==session.Band.File && row.Offset==session.Band.Offset+51+index*32 && row.Category.StartsWith("BandHero",StringComparison.Ordinal)))?.Present==true)).ToArray();
    }
    private static void WriteEditorHeroMember(BandLayout layout,int index,EditorHeroMemberValues values)
    {
        if(values.CreatureID is 0 or >255 || values.Level is <1 or >10 || values.Enemy>7 || values.Health>100 || values.GoldHeld>100)
            throw new ArgumentOutOfRangeException(nameof(values),"Select a creature, level1–10, enemy0–7, health/gold0–100.");
        var entries=new (string Project,string Native,string Value)[]
        {
            ("id","bGlobalCreatureID",values.CreatureID.ToString(CultureInfo.InvariantCulture)),
            ("level","bGlobalLevel",values.Level.ToString(CultureInfo.InvariantCulture)),
            ("enemy","bGlobalPlayer",values.Enemy.ToString(CultureInfo.InvariantCulture)),
            ("health","dwGlobalHealth",values.Health.ToString(CultureInfo.InvariantCulture)),
            ("goldheld","bGlobalGoldHeld",values.GoldHeld.ToString(CultureInfo.InvariantCulture)),
            ("options","bHeroOptions",values.Options.ToString(CultureInfo.InvariantCulture)),
            ("options2","bGlobalBehavior",values.Options2.ToString(CultureInfo.InvariantCulture)),
            ("objective","bHeroObjective",values.Objective.ToString(CultureInfo.InvariantCulture)),
            ("area","bHeroTargetArea",values.Area.ToString(CultureInfo.InvariantCulture))
        };
        var encoded=entries.Select(e=>(Field:layout.Field(layout.Project?e.Project:e.Native)??throw new InvalidDataException("Missing hero field."),e.Value))
            .Select(e=>(e.Field,Bytes:e.Field.Encode(e.Value))).ToArray();
        foreach(var value in encoded)value.Bytes.CopyTo(layout.Data(index)[value.Field.Offset..]);
    }
    private void ChangeEditorHeroBand(EditorHeroBandSession session,Action<BandLayout> change)
    {
        RequireEditorHeroBandSession(session);var layout=session.Working.RequireEditorHeroBandLayout(session.Band);
        var next=checked(session.InputRevision+1);var before=session.Working.TransactionFiles().ToDictionary(file=>file,file=>file.Data.ToArray());var origins=session.NativeMemberOrigins.ToArray();
        try{change(layout);session.InputRevision=next;}
        catch{foreach(var pair in before)pair.Key.Bytes=pair.Value;origins.CopyTo(session.NativeMemberOrigins,0);throw;}
    }
    internal void SetEditorHeroBandName(EditorHeroBandSession session,string name)
    {
        RequireEditorHeroBandSession(session);
        if(!session.HasStoredName)throw new InvalidOperationException("The game hero-band format has no stored name.");
        var field=session.Band.Fields.Single(f=>f.Name=="name");var bytes=field.Encode(name);
        // Win32 GetDlgItemText replaces only the text and its terminator.
        // Retain the original bytes after NUL, including an equal parent Save.
        var length=Array.IndexOf(bytes,(byte)0)+1;
        ChangeEditorHeroBand(session,layout=>bytes.AsSpan(0,length).CopyTo(layout.Record.File.Bytes.AsSpan(layout.Record.Offset+field.Offset)));
    }
    internal int AddEditorHeroMember(EditorHeroBandSession session,EditorHeroMemberValues values)
    {
        var index=-1;ChangeEditorHeroBand(session,layout=>
        {
            index=session.Working.ContiguousBandCount(layout);
            if(index==16)throw new InvalidOperationException("A hero band holds at most sixteen members.");
            var identity=layout.Read(index,layout.Project?"uniqueid":"wHeroTrigger");
            if(identity!="0")throw new InvalidOperationException("The inactive member has a reserved scenario identity.");
            if(!layout.Project)
            {
                var slot=session.Working.Records().Single(row=>row.File==session.Band.File && row.Offset==session.Band.Offset+51+index*32 && row.Category.StartsWith("BandHero",StringComparison.Ordinal));
                if(session.Working.NativeOwnerState(slot)?.Present==true)throw new InvalidOperationException("The inactive member has an empty scenario point.");
                session.Working.ForgetNativeMemberIdentity(slot);session.NativeMemberOrigins[index]=-1;
            }
            WriteEditorHeroMember(layout,index,values);
            if(layout.Project)layout.Set(index,"isvalid","1");
            else layout.Set(index,"bHeroBandID",session.ID.ToString(CultureInfo.InvariantCulture));
        });return index;
    }
    private static int[] EditorHeroIndices(IReadOnlyList<int> indices,int count)
    {
        ArgumentNullException.ThrowIfNull(indices);
        var selected=indices.Distinct().Order().ToArray();
        if(selected.Length==0 || selected.Any(i=>i<0 || i>=count))throw new ArgumentOutOfRangeException(nameof(indices),"Select active members.");
        return selected;
    }
    internal void ReplaceEditorHeroMembers(EditorHeroBandSession session,IReadOnlyList<int> indices,EditorHeroMemberValues values)
        =>ChangeEditorHeroBand(session,layout=>
        {
            var selected=EditorHeroIndices(indices,session.Working.ContiguousBandCount(layout));
            foreach(var index in selected)WriteEditorHeroMember(layout,index,values);
        });
    internal void RemoveEditorHeroMembers(EditorHeroBandSession session,IReadOnlyList<int> indices)
        =>ChangeEditorHeroBand(session,layout=>
        {
            var count=session.Working.ContiguousBandCount(layout);var selected=EditorHeroIndices(indices,count).ToHashSet();var next=0;
            if(!layout.Project)
            {
                var mapping=Enumerable.Range(0,count).Where(index=>!selected.Contains(index)).Concat(Enumerable.Repeat(-1,selected.Count)).Concat(Enumerable.Range(count,16-count)).ToArray();
                session.Working.RebaseNativeMemberMapping(session.Band,mapping);
                var origins=mapping.Select(index=>index<0?-1:session.NativeMemberOrigins[index]).ToArray();origins.CopyTo(session.NativeMemberOrigins,0);
            }
            for(var index=0;index<count;index++)if(!selected.Contains(index))layout.Data(index).CopyTo(layout.Data(next++));
            for(var index=next;index<count;index++)
            {
                layout.Data(index).Clear();
                if(!layout.Project)layout.Set(index,"bHeroBandID",session.ID.ToString(CultureInfo.InvariantCulture));
            }
            // The original parent only compacts its payload. Scenario roots
            // remain intact, including roots whose removed identity is orphaned.
        });
    internal EditorHeroBandMemberDraft CreateEditorHeroMemberDraft(EditorHeroBandSession session,int index)
    {
        var member=ReadEditorHeroBandMembers(session).SingleOrDefault(m=>m.Index==index)??throw new ArgumentOutOfRangeException(nameof(index));
        return new(session,index,session.InputRevision,member.Values);
    }
    internal void ApplyEditorHeroMemberDraft(EditorHeroBandSession session,EditorHeroBandMemberDraft draft)
    {
        RequireEditorHeroBandSession(session);ArgumentNullException.ThrowIfNull(draft);
        if(draft.Closed || !ReferenceEquals(draft.Session,session) || draft.Revision!=session.InputRevision)
            throw new InvalidOperationException("The nested member editor is closed, foreign or stale.");
        ReplaceEditorHeroMembers(session,[draft.Index],draft.Values);draft.Dispose();
    }

    public EditorHeroBandPlan PrepareEditorHeroBand(EditorHeroBandSession session,string defaultName)
    {
        RequireEditorHeroBandSession(session);
        var members=ReadEditorHeroBandMembers(session);
        if(members.Count(m=>m.IsLeader)!=1)throw new InvalidOperationException("A hero band must have exactly one leader.");
        if(session.HasStoredName && session.Name.Length==0)SetEditorHeroBandName(session,defaultName);
        var sourceFile=session.Files.Single(pair=>pair.Value==session.Band.File).Key;
        var result=new Dictionary<NativeFile,byte[]>();
        // Reconstruct the only authorized parent operation on a fresh clone.
        // This rejects any accidental change outside the complete band payload.
        var expected=ClonePropertiesDocument();var expectedFile=expected.Files[sourceFile];
        expected.Document.UpgradePrivateEditorHeroBandSection();
        var payload=session.Band.File.Data.Slice(session.Band.Offset,session.Band.Length).ToArray();
        if(session.IsNew)
        {
            var offset=IsLegacyProject?expected.Document.InsertLegacyRecord(expectedFile,"MTHBAND",payload):expected.Document.InsertRecord(expectedFile,payload);
            if(offset!=session.Band.Offset)throw new InvalidOperationException("The private hero band changed position.");
        }
        else
        {
            if(!IsLegacyProject)expected.Document.RebaseNativeMemberMapping(expected.Document.Records().Single(row=>row.File==expectedFile && row.Offset==session.Band.Offset && IsBand(row)),session.NativeMemberOrigins);
            payload.CopyTo(expectedFile.Bytes,session.Band.Offset);
        }
        foreach(var pair in session.Files)
        {
            if(!pair.Value.Data.Span.SequenceEqual(expected.Files[pair.Key].Data.Span))throw new InvalidOperationException("The hero-band parent changed unrelated map bytes.");
            if(pair.Key==sourceFile || !IsLegacyProject && pair.Key==NativeOwnerScopes)result.Add(pair.Key,pair.Value.Data.ToArray());
        }
        return new(session,session.InputRevision,session.Working.UnusedScriptFingerprint(),new ReadOnlyDictionary<NativeFile,byte[]>(result));
    }
    public NativeRecord ApplyEditorHeroBand(EditorHeroBandPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);RequireEditorHeroBandSession(plan.Session);
        if(plan.InputRevision!=plan.Session.InputRevision || !plan.Session.Working.UnusedScriptFingerprint().AsSpan().SequenceEqual(plan.Fingerprint))
            throw new InvalidOperationException("The private hero-band parent changed after Save preparation.");
        _=checked(changeCount+1);_=checked(EditRevision+1);
        // Source UNDO_MODBAND is unconditional, including equal accepted Save.
        // The generic Edit no-op contract remains unchanged.
        EditCore(plan.Session.IsNew?"Add hero band":"Save hero band",()=>
        { foreach(var pair in plan.Result)pair.Key.Bytes=(byte[])pair.Value.Clone(); },()=>new EditorDrawingCompletion(true,1));
        var sourceFile=plan.Session.Files.Single(pair=>pair.Value==plan.Session.Band.File).Key;
        var result=Records().Single(r=>r.File==sourceFile && r.Offset==plan.Session.Band.Offset && r.Length==plan.Session.Band.Length);
        plan.Session.Dispose();return result;
    }
}
