namespace DK2MapEditor.Core.Editing;

public sealed record BandMember(int Index, bool Active, uint CreatureID, int Level, uint Enemy);

public sealed partial class NativeMapDocument
{
    private sealed record BandLayout(NativeRecord Record, int Start, int Stride, bool Project, IReadOnlyList<NativeField> Fields)
    {
        public Span<byte> Data(int index) => Record.File.Bytes.AsSpan(Record.Offset + Start + index * Stride, Stride);
        public NativeField? Field(string name) => Fields.FirstOrDefault(field => field.Name == name);
        public string Read(int index, string name) => Field(name)?.Read(Data(index)) ?? "0";
        public void Set(int index, string name, string value)
        {
            if (Field(name) is { } field) field.Encode(value).CopyTo(Data(index)[field.Offset..]);
        }
    }

    public static bool IsBand(NativeRecord record) => record.Category == "GAMEBAND" || record.File.Signature == LegacyBands && record.Category.StartsWith("Project.MTHBAND", StringComparison.Ordinal);

    private BandLayout GetBandLayout(NativeRecord record)
    {
        if (!IsBand(record) || !files.Contains(record.File) || !Records().Any(current => current.File == record.File && current.Offset == record.Offset && current.Category == record.Category && current.Length == record.Length))
            throw new ArgumentException("Select a current hero band record.");
        if (record.Category == "GAMEBAND")
        {
            if (record.Length != 563) throw new InvalidDataException("Unverified native hero-band layout.");
            return new(record, 51, 32, false, LegacySchemas.Fields("GAMEBANDHERO"));
        }
        var start = record.Fields.Single(field => field.Name == "heros[0].isvalid").Offset;
        var stride = record.Fields.Single(field => field.Name == "heros[1].isvalid").Offset - start;
        var fields = record.Fields.Where(field => field.Name.StartsWith("heros[0].", StringComparison.Ordinal)).Select(field => field with { Name = field.Name[9..], Offset = field.Offset - start }).ToArray();
        if (start + 17 * stride > record.Length) throw new InvalidDataException("Truncated project hero slots or end marker.");
        return new(record, start, stride, true, fields);
    }

    public IReadOnlyList<BandMember> BandMembers(NativeRecord record) => ReadBandMembers(GetBandLayout(record));

    private static IReadOnlyList<BandMember> ReadBandMembers(BandLayout layout)
    {
        return Enumerable.Range(0, 16).Select(index => new BandMember(index,
            layout.Project ? layout.Read(index, "isvalid") != "0" : layout.Read(index, "bGlobalCreatureID") != "0" && layout.Read(index, "bGlobalBehavior") != "0",
            uint.Parse(layout.Read(index, layout.Project ? "id" : "bGlobalCreatureID")),
            int.Parse(layout.Read(index, layout.Project ? "level" : "bGlobalLevel")),
            uint.Parse(layout.Read(index, layout.Project ? "enemy" : "bGlobalPlayer")))).ToArray();
    }

    private int ContiguousBandCount(BandLayout layout)
    {
        var members = ReadBandMembers(layout);
        var count = members.TakeWhile(member => member.Active).Count();
        if (members.Skip(count).Any(member => member.Active))
            throw new InvalidOperationException("Hero bands must have consecutive active slots. Repair slot activation before changing their order.");
        if (!layout.Project && members.Skip(count).Any(member => member.CreatureID != 0 || layout.Read(member.Index, "bGlobalBehavior") != "0"))
            throw new InvalidOperationException("An inactive native slot contains a creature or behavior. Repair it before adding/removing members.");
        if (layout.Project && layout.Read(16, "isvalid") != "0")
            throw new InvalidDataException("The seventeenth project slot must remain an inactive end marker.");
        return count;
    }

    /// <summary>Activates the first free slot with the historical band-dialog defaults.</summary>
    public int AddBandMember(NativeRecord record, uint creatureID, int level = 1, uint enemy = 3)
        => AddBandMemberCore(record,creatureID,level,enemy,false);

    public int AddEditorBandMember(NativeRecord record,uint creatureID,int level=1,uint enemy=3)
        => AddBandMemberCore(record,creatureID,level,enemy,true);

    private int AddBandMemberCore(NativeRecord record,uint creatureID,int level,uint enemy,bool editorCreation)
    {
        var layout = GetBandLayout(record);
        if (creatureID is 0 or > 255 || level is < 1 or > 10 || enemy is < 1 or > 7) throw new ArgumentOutOfRangeException(nameof(creatureID));
        var index = ContiguousBandCount(layout);
        if (index == 16) throw new InvalidOperationException("A hero band can contain at most sixteen members.");
        var oldIdentity = layout.Read(index, "uniqueid");
        var nativeSlot = layout.Project ? null : Records().Single(row => row.File == record.File && row.Offset == record.Offset + layout.Start + index * layout.Stride && row.Category.StartsWith("BandHero", StringComparison.Ordinal));
        if ((layout.Project && oldIdentity != "0" && MemberScopeLinks(oldIdentity).Any()) || (nativeSlot is not null && (layout.Read(index, "wHeroTrigger") != "0" || NativeOwnerState(nativeSlot)?.Present == true)))
            throw new InvalidOperationException("The inactive slot has an attached scenario. Detach it explicitly before replacing this slot.");
        Edit("Add band member", () =>
        {
            if (nativeSlot is not null) ForgetNativeMemberIdentity(nativeSlot);
            // Reserved bytes stay intact; only documented fields receive defaults.
            foreach (var field in layout.Fields) layout.Set(index, field.Name, "0");
            if (layout.Project)
            {
                layout.Set(index, "isvalid", "1"); layout.Set(index, "id", creatureID.ToString());
                layout.Set(index, "level", level.ToString()); layout.Set(index, "enemy", enemy.ToString());
                layout.Set(index, "health", "100"); layout.Set(index, "options2", "45"); layout.Set(index, "behavior", "45");
                layout.Set(index, "objective", "19"); layout.Set(index, "uniqueid",editorCreation?"0":NextLegacyUniqueID().ToString());
            }
            else
            {
                layout.Set(index, "bGlobalCreatureID", creatureID.ToString()); layout.Set(index, "bGlobalLevel", level.ToString());
                layout.Set(index, "bGlobalPlayer", enemy.ToString()); layout.Set(index, "dwGlobalHealth", "100");
                layout.Set(index, "bGlobalBehavior", "45"); layout.Set(index, "bHeroObjective", "19");
                layout.Set(index, "bHeroBandID", record.Read(record.Fields.Single(field => field.Name == "bBandID")));
            }
        });
        return index;
    }

    private IEnumerable<NativeRecord> MemberScopeLinks(string identity) => Records().Where(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)
        && record.Read(record.Fields.Single(field => field.Name == "node.type")) == "16"
        && record.Fields.FirstOrDefault(field => field.Name == "creature") is { } target && record.Read(target) == identity);

    public void RemoveBandMember(NativeRecord record, int index)
    {
        var layout = GetBandLayout(record); var count = ContiguousBandCount(layout);
        if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index), "Select an active band member.");
        var identity = layout.Read(index, "uniqueid");
        if (layout.Project && identity != "0" && Records().Any(owner => owner.Fields.Any(field => field.Name.EndsWith("uniqueid", StringComparison.Ordinal) && owner.Read(field) == identity && !(owner.File == record.File && owner.Offset == record.Offset && field.Name == $"heros[{index}].uniqueid"))))
            throw new InvalidDataException("This member shares a unique ID with another entity. Repair its identity before deletion.");
        Edit("Remove band member", () =>
        {
            if (!layout.Project) RebaseNativeMemberRemoval(record, index, count);
            if (layout.Project && identity != "0")
                foreach (var link in MemberScopeLinks(identity).OrderByDescending(link => link.Offset)) DeleteLegacyRecord(link);
            for (var slot = index; slot < count - 1; slot++) layout.Data(slot + 1).CopyTo(layout.Data(slot));
            layout.Data(count - 1).Clear();
            if (!layout.Project) layout.Set(count - 1, "bHeroBandID", record.Read(record.Fields.Single(field => field.Name == "bBandID")));
        });
    }

    public void MoveBandMember(NativeRecord record, int index, int destination)
    {
        var layout = GetBandLayout(record); var count = ContiguousBandCount(layout);
        if (index < 0 || destination < 0 || index >= count || destination >= count) throw new ArgumentOutOfRangeException(nameof(destination), "Both positions must be active slots.");
        if (index == destination) return;
        Edit("Move band member", () =>
        {
            if (!layout.Project) RebaseNativeMemberMove(record, index, destination);
            var moved = layout.Data(index).ToArray();
            if (destination < index) for (var slot = index; slot > destination; slot--) layout.Data(slot - 1).CopyTo(layout.Data(slot));
            else for (var slot = index; slot < destination; slot++) layout.Data(slot + 1).CopyTo(layout.Data(slot));
            moved.CopyTo(layout.Data(destination));
        });
    }
}
