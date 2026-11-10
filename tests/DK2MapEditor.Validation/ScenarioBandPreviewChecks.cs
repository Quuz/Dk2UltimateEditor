using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Xml.Linq;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class ScenarioBandPreviewChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new Exception(message); }
    private static byte[] Bytes(NativeFile file)
    {
        Expect(MemoryMarshal.TryGetArray(file.Data, out var data) && data.Offset == 0, "No writable independent fixture.");
        return data.Array!;
    }
    private static void Reject(Action read)
    { try { read(); } catch (InvalidOperationException) { return; } throw new Exception("Stale/foreign/nested preview was accepted."); }
    internal static void Run()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "DK2MapEditor.slnx"))) root = root.Parent;
        foreach (var language in new[] { "en", "ru" })
        {
            var resources = XDocument.Load(Path.Combine(root!.FullName, $"src/DK2MapEditor.App/Localization/Strings.{language}.xaml")).Root!.Elements()
                .Where(e => e.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")) is not null)
                .ToDictionary(e => e.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml"))!.Value, e => e.Value);
            var member = new ScenarioBandMemberPreview(0, 17, -3, false, 23, 47, new("17", "Archer", "Лучник"), 1200);
            var expected = language == "en" ? "Archer · Level -3" : "Лучник · Уровень -3";
            Expect(ScenarioBandPreviewRows.Label(member, language, key => resources[key]) == expected, "Ordinary member caption acquired a leader objective.");
            foreach (var objective in new uint[] { 11, 12, 13, 14, 17, 18, 19, 22, 23, 27, 255 })
            {
                var leader = member with { IsLeader = true, Objective = objective };
                var label = ScenarioBandPreviewRows.Label(leader, language, key => resources[key]);
                var ending = objective == 255 ? resources["AliasEvent.Unknown"] + " 255" : resources["Batch.Objective." + objective];
                if (objective == 23) ending += " 47";
                Expect(label.EndsWith(" / " + ending) && label.Contains(language == "en" ? "Leader" : "Лидер"), "Leader source objective/area caption differs.");
            }
            var unknown = ScenarioBandPreviewRows.Label(member with { Creature = null, CreatureID = 250 }, language, key => resources[key]);
            Expect(unknown.StartsWith(resources["AliasEvent.Unknown"] + " 250"), "Unknown creature was silently substituted.");
        }
        using var input = JsonDocument.Parse(typeof(ScenarioBandPreviewChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.GlobalDefinitionImagesSource.json")!);
        var creatures = input.RootElement.GetProperty("Entries").EnumerateArray().Where(e => e.GetProperty("Family").GetString() == "Creatures")
            .Select(e => (ID:e.GetProperty("ID").GetUInt32(),Image:e.GetProperty("Resource").GetInt32())).ToArray();
        Expect(creatures.Length == 45, "Source creature fixture changed.");
        var types = new[] { "TRIGGER_BANDCREATED", "TRIGGER_STUNNEDINBAND", "TRIGGER_JAILEDINBAND", "TRIGGER_DEATHSINBAND", "ACTION_RELEASEBAND" };
        foreach (var native in new[] { false, true })
        {
            var doc = NativeMapDocument.CreateNewMap(new("BandPreview", "BandPreview", 20, 20, NativeFormat:native));
            var bands = new List<NativeRecord>();
            NativeRecord AddBand(string id)
            {
                var band = native ? doc.AddRecord(RecordTemplate.All.Single(t => t.Structure == "GAMEBAND"), new(0, 0)) : doc.AddLegacyRecord("Band", new(0, 0));
                doc.SetField(band, EntityReferences.Identity(band)!.Field, id); bands.Add(band); return band;
            }
            void Seed(NativeRecord band, int slot, uint id, int level, uint behavior, uint objective, uint area, bool active = true)
            {
                var data = Bytes(band.File); var offset = band.Offset + (native ? 51 + slot * 32 : 12 + slot * 44);
                if (native)
                {
                    data[offset + 14] = checked((byte)level); data[offset + 15] = active ? checked((byte)behavior) : (byte)0;
                    data[offset + 16] = checked((byte)area); data[offset + 27] = checked((byte)objective); data[offset + 28] = checked((byte)id);
                }
                else
                {
                    BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset), active ? 1 : 0);
                    BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset + 4), level);
                    foreach (var (field, value) in new[] { (12,id), (28,objective), (32,behavior), (36,area) }) BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset + field), value);
                }
            }
            for (var group = 0; group < 3; group++)
            {
                var band = AddBand((group * 7).ToString());
                if (!native) Expect(band.Fields.Single(f => f.Name == "heros[0].isvalid").Offset == 12, "Independent project slot origin differs.");
                for (var slot = 0; slot < 16 && group * 16 + slot < creatures.Length; slot++)
                    Seed(band, slot, creatures[group * 16 + slot].ID, slot % 10 + 1, slot % 2 == 0 ? 3u : 1u, 23, 47);
            }
            var gap = AddBand("21");
            Seed(gap, 0, 250, native ? 255 : -3, 3, 255, 55);
            Seed(gap, 1, 17, 1, 1, 19, 0, false);
            Seed(gap, 2, 17, 9, 3, 19, 0); // Imported tail is retained, never displayed past the gap.
            var duplicate = AddBand("28");
            var duplicateID = EntityReferences.Identity(duplicate)!.Field;
            duplicateID.Encode("0").CopyTo(Bytes(duplicate.File).AsSpan(duplicate.Offset + duplicateID.Offset));
            Seed(duplicate, 0, 17, 2, 1, 19, 0);
            var empty = AddBand("35");
            var records = types.Select(name =>
            {
                var type = LegacyEventSchemas.All.Single(t => t.Name == name); var kind = type.RecordSignature == 0xD5 ? "Trigger" : "Action";
                var record = native ? doc.AddRecord(RecordTemplate.All.Single(t => t.Structure == kind), new(0, 0)) : doc.AddLegacyRecord(kind, new(0, 0));
                doc.SetEventType(record, type); return doc.Records().Single(r => r.File == record.File && r.Offset == record.Offset);
            }).ToArray();
            var bytes = doc.Files.ToDictionary(f => f, f => f.Data.ToArray()); var revision = doc.EditRevision; var history = doc.GetHistory().Undo.Count;
            void Exact() => Expect(doc.EditRevision == revision && doc.GetHistory().Undo.Count == history && bytes.All(b => b.Key.Data.Span.SequenceEqual(b.Value)), "Preview changed bytes/history/revision.");
            foreach (var record in records)
            {
                var draft = doc.CreateScenarioParameterDraft(record); var previews = doc.ReadScenarioBandPreviews(draft);
                Expect(previews.Select(b => b.ID).SequenceEqual(new[] { "0", "7", "14", "21", "0", "35" }) && previews.All(b => b.Complete), "Physical band order/duplicate IDs lost.");
                var members = previews.Take(3).SelectMany(b => b.Members).ToArray();
                Expect(members.Length == 45, "First inactive slot/capacity differs.");
                for (var i = 0; i < 45; i++)
                {
                    var slot = i % 16; var member = members[i];
                    Expect(member.Index == slot && member.CreatureID == creatures[i].ID && member.ImageResource == creatures[i].Image && member.Level == slot % 10 + 1 && member.IsLeader == (slot % 2 == 0) && member.Objective == 23 && member.Area == 47, "Independent packed member projection differs.");
                    Expect(member.Creature is { English.Length: > 0, Russian.Length: > 0 }, "Missing bilingual member name.");
                }
                var unknown = previews[3].Members.Single();
                Expect(unknown.CreatureID == 250 && unknown.Creature is null && unknown.ImageResource is null && unknown.Objective == 255 && unknown.Level == (native ? 255 : -3), "Unknown imported member was guessed or normalized.");
                Expect(previews[4].Members.Single().Level == 2 && previews[5].Members.Count == 0, "Duplicate/empty band preview was replaced by another occurrence.");
                Exact();
                var foreign = NativeMapDocument.CreateNewMap(new("ForeignPreview", "ForeignPreview", 20, 20, NativeFormat:native));
                Reject(() => foreign.ReadScenarioBandPreviews(draft));
                doc.Edit("Read-only nested preview", () => Reject(() => doc.ReadScenarioBandPreviews(draft))); Exact();
            }
            var oldDraft = doc.CreateScenarioParameterDraft(records[0]); var snapshot = doc.ReadScenarioBandPreviews(oldDraft);
            doc.SetScenarioName(ScenarioNameKind.Variable, 0, "Changed"); Reject(() => doc.ReadScenarioBandPreviews(oldDraft));
            Seed(bands[0], 0, 17, 4, 1, 19, 0);
            Expect(snapshot[0].Members[0].CreatureID == creatures[0].ID && snapshot[0].Members[0].Level == 1, "Returned preview reread live bytes.");
            var current = doc.CreateScenarioParameterDraft(records[0]);
            Expect(doc.ReadScenarioBandPreviews(current)[0].Members[0].Level == 4, "Explicit refresh did not read current payload.");
        }
    }
}
