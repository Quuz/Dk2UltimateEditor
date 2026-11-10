using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;

internal static class CreatureJobChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document, Dictionary<NativeFile, byte[]> snapshot)
    { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(snapshot[file]), "Job edit changed unrelated/undo bytes."); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException or OverflowException) { return; } throw new Exception("Invalid job input accepted."); }
    private static CreatureJobsDraft Draft(NativeMapDocument document, int index = 0) => document.CreateCreatureJobsDraft(document.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.Index == index));
    public static void Run()
    {
        var expected = new Dictionary<string, uint[]> {
            ["Happy"] = [1,2,3,4,5,6,7,8,9,24,25,28], ["Unhappy"] = [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,17],
            ["Angry"] = [10,11,12,13,14,15,16,17,18,19], ["Hate"] = [1,2,3,4,5,6,7,8,9,24,25,28] };
        Expect(CreatureJobs.Options.Count == 22 && CreatureJobs.Options.All(item => !item.Name.English.Contains("sz") && item.Name.Russian.Length > 0), "Incomplete named source catalogue.");
        foreach (var group in CreatureJobs.Groups) Expect(CreatureJobs.Allowed(group.ID).Select(item => item.ID).SequenceEqual(expected[group.ID]), "Wrong original group filter.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2-Jobs-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var native = GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory, "basis")); var basis = Snapshot(native);
            var project = NativeMapDocument.CreateNewMap(new("Jobs", "Jobs", 20, 20));
            foreach (var document in new[] { native, project })
            {
                var initial = Snapshot(document); var initialDraft = Draft(document); var events = 0; EventHandler handler = (_, _) => events++; document.Changed += handler;
                try
                {
                    document.ApplyCreatureJobs(initialDraft); Exact(document, initial); Expect(events == 0, "Unchanged draft created history/override.");
                    foreach (var row in document.GlobalDefinitionRows().Where(row => row.Family == "Creatures"))
                    { var read = document.CreateCreatureJobsDraft(row); Expect(!read.Changed && read.Editable, "Source creature job projection is not a no-op."); document.ApplyCreatureJobs(read); }
                    Exact(document, initial); Expect(events == 0,"Browsing/reapplying the 45 creature job definitions changed bytes.");
                    var foreign = NativeMapDocument.CreateNewMap(new("Foreign","Foreign",20,20)); Reject(() => foreign.ApplyCreatureJobs(initialDraft));
                    // Start each group empty, preserving all other definitions.
                    var empty = initialDraft;
                    foreach (var group in CreatureJobs.Groups)
                    {
                        Expect(empty.Slots(group.ID).Count == group.Capacity, "Wrong slot capacity.");
                        var indices = empty.Slots(group.ID).Select((slot, index) => (slot, index)).Where(item => item.slot.Job != "0").Select(item => item.index);
                        empty = document.RemoveCreatureJobs(empty, group.ID, indices);
                    }
                    foreach (var group in CreatureJobs.Groups)
                    {
                        foreach (var option in CreatureJobs.Allowed(group.ID))
                        {
                            var draft = document.AddCreatureJob(empty, group.ID, option.ID, "100", "-9", "32767"); Exact(document, initial);
                            Expect(draft.Slots(group.ID)[0].Job == option.ID.ToString(), "Add did not use first empty slot.");
                            document.ApplyCreatureJobs(draft); var edited = Snapshot(document); var read = Draft(document);
                            var slot = read.Slots(group.ID)[0];
                            Expect(slot.Job == option.ID.ToString() && (!group.HasParameters || slot.Chance == "100" && slot.Mood == (document.IsLegacyProject ? "-9" : "65527") && slot.Mana == "32767"), "Wrong job tuple encoding.");
                            // Independent native offset check, not only projection round-trips.
                            if (!document.IsLegacyProject)
                            {
                                var record = document.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.Index == 0).Record!;
                                var offset = group.ID switch { "Happy" => 0xE14, "Unhappy" => 0xE38, "Angry" => 0xE50, _ => 0xE74 };
                                var bytes = record.File.Data.Span.Slice(record.Offset + offset);
                                Expect(BinaryPrimitives.ReadInt32LittleEndian(bytes) == option.ID && (!group.HasParameters || BinaryPrimitives.ReadUInt16LittleEndian(bytes[4..]) == 65527 && BinaryPrimitives.ReadUInt16LittleEndian(bytes[6..]) == 32767 && bytes[8] == 100), "Independent packed job offsets differ.");
                            }
                            var priorEvents = events; document.ApplyCreatureJobs(read); Exact(document, edited); Expect(events == priorEvents, "No-op job apply created history.");
                            document.Undo(); Exact(document, initial); document.Redo(); Exact(document, edited); document.Undo(); Exact(document, initial);
                        }
                        var full = empty;
                        for (var index = 0; index < group.Capacity; index++) full = document.AddCreatureJob(full, group.ID, expected[group.ID][index], index == 0 ? "100" : "0", "-32767", "32767");
                        var beforeEvents = events; document.ApplyCreatureJobs(full); Expect(events == beforeEvents + 1,"Full job group not one transaction.");
                        Expect(Draft(document).Slots(group.ID).SequenceEqual(full.Slots(group.ID)),"One of the later job slots was mapped incorrectly.");
                        if (!document.IsLegacyProject)
                        {
                            var record = document.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.Index == 0).Record!;
                            var start = group.ID switch { "Happy" => 0xE14, "Unhappy" => 0xE38, "Angry" => 0xE50, _ => 0xE74 };
                            for (var index = 0; index < group.Capacity; index++)
                            {
                                var bytes = record.File.Data.Span.Slice(record.Offset + start + index * (group.HasParameters ? 12 : 4));
                                Expect(BinaryPrimitives.ReadInt32LittleEndian(bytes) == expected[group.ID][index] && (!group.HasParameters || BinaryPrimitives.ReadUInt16LittleEndian(bytes[4..]) == 32769 && BinaryPrimitives.ReadUInt16LittleEndian(bytes[6..]) == 32767 && bytes[8] == (index == 0 ? 100 : 0)),"Independent later-slot native layout mismatch.");
                            }
                        }
                        document.Undo(); Exact(document,initial);
                        Reject(() => document.AddCreatureJob(full, group.ID, expected[group.ID][group.Capacity], "0", "0", "0"));
                        Reject(() => document.ChangeCreatureJob(full, group.ID, 1, expected[group.ID][0].ToString(), "0", "0", "0"));
                        var removed = document.RemoveCreatureJobs(full, group.ID, [0, group.Capacity - 1]);
                        Expect(removed.Slots(group.ID).Take(group.Capacity - 2).SequenceEqual(full.Slots(group.ID).Skip(1).Take(group.Capacity - 2)) && removed.Slots(group.ID).Skip(group.Capacity - 2).All(slot => slot.Job == "0" && slot.Mood == "0"), "Multiple removal did not shift complete tuples.");
                        foreach (var invalid in new[] { "-32768", "32768", "1.5", "0x10" }) if (group.HasParameters) Reject(() => document.ChangeCreatureJob(full, group.ID, 0, expected[group.ID][0].ToString(), "100", invalid, "0"));
                        if (group.HasParameters) foreach (var chance in new[] { "-1", "101" }) Reject(() => document.ChangeCreatureJob(full, group.ID, 0, expected[group.ID][0].ToString(), chance, "0", "0"));
                        Reject(() => document.AddCreatureJob(empty, group.ID, 999, "100", "0", "0"));
                        var disallowed = CreatureJobs.Options.First(item => !expected[group.ID].Contains(item.ID)); Reject(() => document.AddCreatureJob(empty, group.ID, disallowed.ID, "100", "0", "0"));
                        Exact(document, initial);
                    }
                    var odd = document.AddCreatureJob(empty, "Happy", 1, "33", "-9", "-32767");
                    Reject(() => document.ApplyCreatureJobs(odd)); Exact(document, initial);
                    document.ApplyCreatureJobs(odd, true); Expect(Draft(document).Totals.Single(total => total.Group == "Happy").Total == 33, "Source warning override not accepted.");
                    var oddBytes = Snapshot(document); document.ApplyCreatureJobs(Draft(document)); Exact(document, oddBytes);
                    Reject(() => document.ApplyCreatureJobs(odd, true)); Exact(document, oddBytes); document.Undo(); Exact(document, initial);
                    // Imported unknown IDs, wider values, signed aliases and holes
                    // are moved losslessly rather than replaced or clamped.
                    document.SetGlobalDefinitionValues([new("Creatures",0,"HJobs[0]","999"),new("Creatures",0,"HJobsChance[0]","200"),new("Creatures",0,"HJobsMood[0]","32768"),new("Creatures",0,"HJobsMana[0]","65527"),new("Creatures",0,"HJobs[1]","0"),new("Creatures",0,"HJobsMood[1]","123"),new("Creatures",0,"HJobs[2]","1"),new("Creatures",0,"HJobsChance[2]","100")]);
                    var imported = Snapshot(document); var known = Draft(document); var original = known.Slots("Happy")[0];
                    var unchanged = document.ChangeCreatureJob(known,"Happy",0,"999","200","-32768","-9"); document.ApplyCreatureJobs(unchanged); Exact(document, imported);
                    var shift = document.RemoveCreatureJobs(known,"Happy",[2]); Expect(shift.Slots("Happy")[0] == original && shift.Slots("Happy")[1].Mood == "123", "Removal normalized unknown/inactive slot.");
                    document.ApplyCreatureJobs(shift,true); document.Undo(); Exact(document,imported); document.Undo(); Exact(document,initial);
                    var unknownID = document.IsLegacyProject ? "4294967295" : "-1";
                    document.SetGlobalDefinitionValues([new("Creatures",0,"HJobs[0]","1"),new("Creatures",0,"HJobs[1]",unknownID),new("Creatures",0,"HJobsChance[1]","200"),new("Creatures",0,"HJobsMood[1]","65527"),new("Creatures",0,"HJobsMana[1]","32768"),new("Creatures",0,"HJobs[2]","1"),new("Creatures",0,"HJobsChance[2]","100")]);
                    var duplicates = Snapshot(document); var duplicateDraft = Draft(document);
                    var sameUnknown = document.ChangeCreatureJob(duplicateDraft,"Happy",1,unknownID,"200","-9","-32768"); document.ApplyCreatureJobs(sameUnknown); Exact(document,duplicates);
                    var importedCopy = NativeMapDocument.Load(document.SaveCopy(Path.Combine(directory,document.IsLegacyProject ? "unknown-project" : "unknown-native")));
                    Expect(Draft(importedCopy).Slots("Happy").SequenceEqual(duplicateDraft.Slots("Happy")),"Unknown job IDs/aliases changed after save/reopen.");
                    if (document.IsLegacyProject)
                    {
                        var importedCompiled = document.CompileLegacyProject("UnknownJobsCompiled",native);
                        Expect(Draft(importedCompiled).Slots("Happy")[1] == new CreatureJobSlot("-1","200","65527","32768"),"Unknown UINT job ID was lost instead of original LONG cast."); Exact(document,duplicates);
                    }
                    var moved = document.RemoveCreatureJobs(duplicateDraft,"Happy",[0]); Expect(moved.Slots("Happy")[0] == duplicateDraft.Slots("Happy")[1] && moved.Slots("Happy")[1] == duplicateDraft.Slots("Happy")[2],"Moving unknown/signed tuples normalized their raw data.");
                    document.ApplyCreatureJobs(moved,true); document.Undo(); Exact(document,duplicates); document.Undo(); Exact(document,initial);
                    // Applying a different group still checks all three totals.
                    document.SetGlobalDefinitionValues([new("Creatures",0,"HJobs[0]","1"),new("Creatures",0,"HJobsChance[0]","33")]); var badOther = Snapshot(document); var otherDraft = Draft(document);
                    otherDraft = document.RemoveCreatureJobs(otherDraft,"Hate",otherDraft.Slots("Hate").Select((slot,index) => (slot,index)).Where(item => item.slot.Job != "0").Select(item => item.index));
                    otherDraft = document.AddCreatureJob(otherDraft,"Hate",28,"ignored","ignored","ignored"); Reject(() => document.ApplyCreatureJobs(otherDraft)); Exact(document,badOther);
                    document.ApplyCreatureJobs(otherDraft,true); document.Undo(); Exact(document,badOther); document.Undo(); Exact(document,initial);
                    var savedDraft = document.AddCreatureJob(empty,"Happy",1,"100","-32767","32767"); savedDraft = document.AddCreatureJob(savedDraft,"Hate",28,"ignored","ignored","ignored"); document.ApplyCreatureJobs(savedDraft);
                    var written = Snapshot(document); var saved = document.SaveCopy(Path.Combine(directory,document.IsLegacyProject ? "project" : "native")); var reopened = NativeMapDocument.Load(saved);
                    foreach (var group in CreatureJobs.Groups) Expect(Draft(document).Slots(group.ID).SequenceEqual(Draft(reopened).Slots(group.ID)), "Jobs changed after save/reopen.");
                    if (document.IsLegacyProject)
                    {
                        var compiled = document.CompileLegacyProject("JobsCompiled",native); var packed = Draft(compiled).Slots("Happy")[0];
                        Expect(packed == new CreatureJobSlot("1","100","32769","32767") && Draft(compiled).Slots("Hate")[0].Job == "28", "Job compiler casts differ from original."); Exact(document,written);
                    }
                    document.Undo(); Exact(document,initial);
                }
                finally { document.Changed -= handler; }
            }
            Exact(native,basis); var missing = NativeMapDocument.Create(20,20,"Missing"); var readOnly = Draft(missing);
            Expect(!readOnly.Editable && readOnly.Slots("Happy").All(slot => slot.Job.Length == 0), "Missing native jobs inferred from defaults."); Reject(() => missing.ApplyCreatureJobs(readOnly));
        }
        finally { Directory.Delete(directory,true); }
    }
}
