using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class PlayerAIFieldCaptionChecks
{
    private static void Expect(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    private static NativeField Field(NativeRecord record, string sourceName) => record.Fields.Single(field =>
        field.Name == (record.Category == "GAMEPLAYER"
            ? LegacyCompilerRules.Players.Single(mapping => mapping.Source == sourceName).Target : sourceName));

    private static void Verify(NativeMapDocument document)
    {
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var state = (document.EditRevision, document.HasChanges, document.CanUndo, document.CanRedo);
        var rows = document.PlayerSettingsRows();
        Expect(rows.Count == 7, "Fixture lost the seven physical player records.");
        foreach (var row in rows)
        {
            var record = row.Record;
            foreach (var parameter in PlayerAIOptions.Parameters)
            {
                var field = Field(record, parameter.Name);
                var caption = PlayerAIFieldCaptions.ForField(record, field)
                    ?? throw new Exception("Missing contextual field: " + parameter.Name);
                Expect(caption.Value == field.Name, "Renamed contextual field: " + parameter.Name);
                if (parameter.Name == "PlayerCreaturesSlap")
                    Expect(caption.English == "Creature slap flags (0x20: do not slap)"
                        && caption.Russian == "Флаги пощёчин существам (0x20: не бить)", "Raw slap byte presented as a boolean.");
                else Expect(caption.English == parameter.English && caption.Russian == parameter.Russian,
                    "Raw and typed AI captions disagree: " + parameter.Name);
                Expect(PlayerAIFieldCaptions.ForField(record with { Length = record.Length - 1 }, field) is null,
                    "Unverified truncated record received contextual captions.");
                Expect(PlayerAIFieldCaptions.ForField(record with { Category = "GAMEKEEPER" }, field) is null,
                    "Player caption leaked into a creature record.");
            }
            var boulder = PlayerAIFieldCaptions.ForField(record, Field(record, "PlayerBoulderInAngles"));
            Expect(boulder?.English == "Place boulder traps in corridors leading to breaches"
                && boulder.Russian == "Ставить ловушки-валуны в коридорах к проломам", "Generic grid retained obsolete boulder-on-corners caption.");
            var mana = PlayerAIFieldCaptions.ForField(record, Field(record, "PlayerMana"));
            Expect(mana?.English == "Starting mana" && mana.Russian == "Начальная мана", "Starting mana lost its player context.");
            var sample = Field(record, "PlayerCreaturesSlap");
            Expect(PlayerAIFieldCaptions.ForField(record, sample with { BitMask = 0x20 }) is null,
                "Raw byte caption applied to a normalized checkbox.");
            var forged = sample with { Offset = sample.Offset + 1 };
            Expect(PlayerAIFieldCaptions.ForField(record with { Fields = [forged] }, forged) is null,
                "Unverified field offset accepted by contextual caption lookup.");
            Expect(PlayerAIFieldCaptions.ForField(record with { Fields = [] }, sample) is null
                && PlayerAIFieldCaptions.ForField(record with { Offset = -1 }, sample) is null
                && PlayerAIFieldCaptions.ForField(record with { Offset = record.File.Length }, sample) is null,
                "Foreign field or invalid record range accepted.");
            Expect(record.Fields.Where(field => PlayerAIFieldCaptions.ForField(record, field) is not null).Count() == 67,
                "Caption lookup included room-order, identity, or non-AI fields.");
        }
        foreach (var file in document.Files)
            Expect(file.Data.Span.SequenceEqual(before[file]), "Read-only caption lookup changed file bytes.");
        Expect(state == (document.EditRevision, document.HasChanges, document.CanUndo, document.CanRedo),
            "Read-only caption lookup changed document state/history.");
    }

    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2PlayerCaptions-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var project = NativeMapDocument.CreateNewMap(new("Captions", "Captions", 38, 20, Players: 5, PlaceHearts: true));
            Verify(project);
            var native = NativeMapDocument.CreateNewMap(new("Captions", "Captions", 38, 20, Players: 5, PlaceHearts: true, NativeFormat: true));
            Verify(native);
            var nativeRecord = native.PlayerSettingsRows().First().Record;
            Expect(Field(nativeRecord, "PlayerBoulderInAngles").Name == "bPlayerBoulderInAngles"
                && Field(nativeRecord, "PlayerMana").Name == "bPlayerMiscStartingMana"
                && Field(nativeRecord, "PlayerSpeed").Name == "bPlayerMiscSpeed", "Compiler field identities changed.");
            foreach (var raw in new[] { "0", "32", "165", "255" })
            {
                native.SetField(nativeRecord, Field(nativeRecord, "PlayerCreaturesSlap"), raw);
                Verify(native);
                Expect(nativeRecord.Read(Field(nativeRecord, "PlayerCreaturesSlap")) == raw, "Slap flags were normalized by a label lookup.");
            }
            var saved = SaveProjectionChecks.SaveCopy(project, Path.Combine(directory, "basis"));
            var sections = LegacySectionFile.Read(saved);
            var players = sections.Sections.Single(section => section.Signature == NativeMapDocument.LegacyPlayers);
            foreach (var version in new[] { 1, 2, 99 })
            {
                var payload = version == 1 ? new byte[7 * LegacyProjectSchemas.Size("MAPOPTIONSV1")] : players.Payload.ToArray();
                var path = Path.Combine(directory, $"version-{version}.dkm");
                File.WriteAllBytes(path, LegacySectionFile.Create(sections.Sections.Select(section =>
                    section == players ? new LegacySection(players.Signature, version, payload) : section)).ToArray());
                var old = NativeMapDocument.Load(path);
                if (version == 2) Verify(old);
                else Expect(old.Records().Where(record => record.File.Signature == NativeMapDocument.LegacyPlayers)
                    .All(record => record.Fields.All(field => PlayerAIFieldCaptions.ForField(record, field) is null)),
                    "Unsupported historical player layout received current AI captions.");
            }
        }
        finally { Directory.Delete(directory, true); }
    }
}
