using System.Buffers.Binary;
using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>Compiles verified project records into an independent native game bundle.
    /// Base definitions supply graphics and other data the DKM format never contained.</summary>
    public NativeMapDocument CompileLegacyProject(string name, NativeMapDocument baseDefinitions)
        => CompileLegacyProject(name, baseDefinitions, includeDeclaredKeepers: false);

    private NativeMapDocument CompileLegacyProject(string name, NativeMapDocument baseDefinitions, bool includeDeclaredKeepers)
    {
        if (!IsLegacyProject || baseDefinitions.IsLegacyProject) throw new ArgumentException("Compilation requires a project and native base definitions.");
        if (!includeDeclaredKeepers && CompilerMapNames.IsReserved(name))
            throw new InvalidDataException("The game output name is reserved by the original compiler: " + name + ". Save the project under another filename before compiling.");
        var source = Records();
        if (source.Any(record => record.Category == "Project.Opaque" && record.File.Signature is LegacyInfo or LegacyPlayers or LegacyThings or LegacyAreas or LegacyBands or LegacyEvents or LegacyGlobals))
            throw new InvalidDataException("An unsupported project section cannot be compiled safely.");
        var info = source.SingleOrDefault(record => record.Category == "Project.MAPPROPERTIES") ?? throw new InvalidDataException("Compilation currently requires version 7 project metadata.");
        var players = source.Where(record => record.Category == "Project.MAPOPTIONS").ToArray();
        if (players.Length != 7) throw new InvalidDataException("Compilation requires version 3 player settings.");
        if (source.Any(record => record.File.Signature == LegacyThings && record.Category != "Project.MAPTHING" || record.File.Signature == LegacyBands && record.Category != "Project.MTHBAND"))
            throw new InvalidDataException("Upgrade older thing/band section versions before native compilation.");
        var errors = NativeMapValidator.Validate(this).Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length != 0) throw new InvalidDataException("Fix project validation errors before compilation: " + string.Join("; ", errors.Take(5).Select(item => item.Message)));
        var output = Create(Width, Height, name, baseDefinitions);
        output.compilingNativeGateIdentities = true;
        StampGeneratedGameDate(output.MainFile, 28);
        // The working output starts without the factory player; source player records replace it.
        foreach (var file in output.files.Where(file => file.Signature is 0xB4 or 0xBE or 0xD2 or 0xE6 && file.ExportPath.Contains("Maps" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
        {
            var headerSize = file.Signature == 0xD2 ? 60 : 56;
            file.Bytes = file.Bytes.AsSpan(0, headerSize).ToArray(); WriteUInt32(file.Bytes, 8, (uint)headerSize); WriteUInt32(file.Bytes, 20, 0);
            if (file.Signature == 0xD2) WriteUInt32(file.Bytes, 24, 0);
            WriteUInt32(file.Bytes, headerSize - 4, 0);
            StampGeneratedGameDate(file, file.Signature == 0xD2 ? 32 : 28);
        }
        TileFile.Bytes.AsSpan(0).CopyTo(output.TileFile.Bytes);
        string Value(NativeRecord record, string field, string fallback = "0") => record.Fields.FirstOrDefault(candidate => candidate.Name == field) is { } found ? record.Read(found) : fallback;
        var links = source.Where(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)).ToArray();
        ushort Root(uint scope, string target, string id)
        {
            var link = links.FirstOrDefault(record => Value(record, "node.type") == scope.ToString() && (scope == 0x20 || Value(record, target) == id));
            return link is null ? (ushort)0 : ushort.Parse(Value(link, "trigger"));
        }
        void Set(NativeRecord target, string name, string value)
        {
            var field = target.Fields.FirstOrDefault(field => field.Name == name) ?? throw new InvalidDataException("Unverified compiler field: " + name);
            EncodeCompilerValue(field, value).CopyTo(target.File.Bytes, target.Offset + field.Offset);
        }
        var metadata = output.Records().Single(record => record.Category == "Map");
        foreach (var rule in LegacyCompilerRules.Metadata)
        {
            if (rule.Source != "SpeechFile") { Set(metadata, rule.Target, Value(info, rule.Source)); continue; }
            // Original Compiler_Generate uses CopyMemory(...,32), not a string
            // rewrite: retain full-width imported names and bytes after NUL.
            var from = info.Fields.Single(field => field.Name == "SpeechFile");
            var to = metadata.Fields.Single(field => field.Name == "szMainSpeech");
            if (from.Size != 32 || to.Size != 32 || from.Kind != NativeValueKind.WesternAnsi || to.Kind != NativeValueKind.WesternAnsi)
                throw new InvalidDataException("Unverified speech-bank compiler layout.");
            info.File.Data.Span.Slice(info.Offset + from.Offset, 32).CopyTo(metadata.File.Bytes.AsSpan(metadata.Offset + to.Offset, 32));
        }
        Set(metadata, "wMainTrigger", Root(0x20, "", "0").ToString());
        foreach (var constant in LegacyCompilerRules.MetadataConstants) output.MainFile.Bytes[constant.Offset] = constant.Value;
        foreach (var record in source.Where(record => record.Category is "Project.TRIGGER" or "Project.ACTION"))
        {
            var signature = record.Category == "Project.TRIGGER" ? 0xD5u : 0xD6u;
            var type = LegacyEventSchemas.Find(signature, byte.Parse(Value(record, "ctrl"))) ?? throw new InvalidDataException("Unsupported event type during compilation.");
            var bytes = new byte[24]; WriteUInt32(bytes, 0, signature); WriteUInt32(bytes, 4, 16); type.DefaultPayload.CopyTo(bytes, 8);
            foreach (var argument in type.Arguments)
                if (argument.Name.StartsWith("Param", StringComparison.Ordinal)) EncodeCompilerValue(argument, Value(record, "param" + argument.Name[5..])).CopyTo(bytes, argument.Offset);
            if (signature == 0xD6 && type.Type == 2)
            {
                var crystal = source.FirstOrDefault(record => record.File.Signature == LegacyThings && Value(record, "node.type") == "3" && Value(record, "id") == "28") ?? throw new InvalidDataException("Show portal gem requires object ID 28.");
                BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12), ushort.Parse(Value(crystal, "x"))); BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(14), ushort.Parse(Value(crystal, "y")));
            }
            if (signature == 0xD6 && type.Type == 0x20)
            {
                var gate = source.FirstOrDefault(candidate => candidate.File.Signature == LegacyAreas && Value(candidate, "node.type") == "1" && Value(candidate, "id") == Value(record, "param5"));
                // RectEx_GetRectLocation returns zero for an absent ID. The
                // original writer adds one on each axis; its validator does
                // not reject DESTROYGATE solely for this missing target.
                BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12), gate is null ? (ushort)1 : checked((ushort)(int.Parse(Value(gate, "rect.x1")) + 1))); BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(14), gate is null ? (ushort)1 : checked((ushort)(int.Parse(Value(gate, "rect.y1")) + 1)));
            }
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(16), ushort.Parse(Value(record, "id"))); BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(18), ushort.Parse(Value(record, signature == 0xD5 ? "or" : "next")));
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(20), signature == 0xD5 ? ushort.Parse(Value(record, "and")) : (ushort)0);
            bytes[22] = type.Type; bytes[23] = byte.Parse(Value(record, signature == 0xD5 ? "repeat" : "one"));
            output.InsertRecord(output.files.Single(file => file.Signature == 0xD2), bytes);
        }
        for (var index = 0; index < 7; index++)
        {
            var camera = HeartPosition((byte)(index + 1));
            if (index >= 2 && camera == new TilePoint(0, 0))
            {
                if (!includeDeclaredKeepers || index >= int.Parse(Value(info, "Players")) + 2) continue;
                camera = new(Width / 2, Height / 2);
            }
            var player = output.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEPLAYER"), new(0, 0), (byte)(index + 1));
            foreach (var rule in LegacyCompilerRules.Players) Set(player, rule.Target, Value(players[index], rule.Source));
            var playerName = player.Fields.Single(field => field.Name == "szPlayerName");
            (playerName with { Kind = NativeValueKind.WesternAnsi }).Encode(Value(players[index], "PlayerName", "")).CopyTo(player.File.Bytes, player.Offset + playerName.Offset);
            Set(player, "bPlayerTrigger", Root(1, "player", (index + 1).ToString()).ToString());
            Set(player, "bPlayerCameraX", index < 2 ? "0" : Value(players[index], "PlayerCameraX") == "-1" ? camera.X.ToString() : Value(players[index], "PlayerCameraX"));
            Set(player, "bPlayerCameraY", index < 2 ? "0" : Value(players[index], "PlayerCameraX") == "-1" ? camera.Y.ToString() : Value(players[index], "PlayerCameraY"));
            if (int.Parse(Value(players[index], "PlayerCPUAreaX1")) < 0)
            {
                Set(player, "bPlayerBuildAreaX1", "2"); Set(player, "bPlayerBuildAreaY1", "2");
                Set(player, "bPlayerBuildAreaX2", (Width - 2).ToString()); Set(player, "bPlayerBuildAreaY2", (Height - 2).ToString());
            }
        }
        // Compiler_GenerateThingsFile calls the category writers in this
        // order. DKM storage order is different; keep each list's own order.
        foreach (var record in source.Where(record => record.File.Signature == LegacyThings).OrderBy(CompilerThingOrder))
        {
            var type = uint.Parse(Value(record, "node.type")); var owner = byte.Parse(Value(record, "owner"));
            var structure = type switch { 0 => owner == 1 ? "GAMEHERO" : owner == 2 ? "GAMENEUTRAL" : "GAMEKEEPER", 1 => "GAMEDOOR", 2 => "GAMETRAP", 3 or 4 => "GAMEOBJECT", _ => throw new InvalidDataException("Invalid project thing type.") };
            var target = output.AddRecord(RecordTemplate.All.Single(template => template.Structure == structure), new(int.Parse(Value(record, "x")), int.Parse(Value(record, "y"))), owner);
            foreach (var field in target.Fields)
            {
                var mapped = field.Name switch
                {
                    "bObjectID" or "bTrapID" or "bDoorID" or "bGlobalCreatureID" => "id", "bGlobalGoldHeld" => "goldheld", "bGlobalLevel" => "dataAll", "bGlobalBehavior" => "creaturesoptions", "bHeroTargetArea" => "area", "dwGlobalHealth" => "health", "bHeroObjective" => "objective", "bHeroOptions" => "herosoptions", "bTrapShots" or "bDoorLocked" => "dataAll", "dwObjectGoldValue" when Value(record, "id") is "1" or "2" or "3" => "dataAll", "dwObjectSpell" when Value(record, "id") == "4" => "dataAll", _ => ""
                };
                if (mapped.Length != 0 && (type != 4 || !field.Name.StartsWith("dwObject", StringComparison.Ordinal))) Set(target, field.Name, Value(record, mapped));
                if (field.Name.EndsWith("Trigger", StringComparison.Ordinal)) Set(target, field.Name, Root(type == 1 ? 0x40u : type == 4 ? 0x100u : type == 3 ? 0x80u : 2u, type == 1 ? "door" : type is 3 or 4 ? "object" : "creature", Value(record, "uniqueid")).ToString());
            }
            if (structure == "GAMEHERO") Set(target, "bGlobalPlayer", Value(record, "ennemy"));
        }
        CompileLegacyAreasAndBands(output, source, Value, Root, Set);
        CompileLegacyVariables(output, info, players, Value, includeDeclaredKeepers);
        CompileRetainedNativeVariables(output);
        CompileLegacyDefinitions(output, source);
        RestoreUneditedNativePayloads(output,baseDefinitions,includeDeclaredKeepers);
        // Incomplete synthetic/working documents remain saveable as drafts.
        // Game publication already refuses their unresolved definition links.
        if (OfficialDefinitionOrder.All(signature => output.files.Count(file => file.Signature == signature) == 1))
            output.officialDefinitionExtras = CaptureOfficialDefinitionExtras(baseDefinitions);
        var finalErrors = NativeMapValidator.Validate(output).Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
        if (finalErrors.Length != 0) throw new InvalidDataException("Compiled map failed validation: " + string.Join("; ", finalErrors.Take(5).Select(item => item.Message)));
        CopyScenarioNamesToNative(output);
        // The newly generated working document retains the compiler's target;
        // exported GAMEINFO remains format 4 and native reload resets to 17.
        output.WriteNativeGameTarget(TargetGameVersion ?? 17);
        output.WriteNativeResizeFill(ResizeFillPreset ?? DefaultResizeFillPreset);
        // Compilation writes actions before gates. Resolve the completed native
        // projection only after all collections and retained bytes exist.
        output.ReinitializeNativeGateBindings();
        output.RestoreCompiledNativeGateIdentities(source);
        output.RestoreCompiledNativeAreaScopes(source);
        output.RestoreCompiledNativeOwnerScopes(source);
        output.compilingNativeGateIdentities = false;
        return output;
    }

    private static int CompilerThingOrder(NativeRecord record)
        => record.Read(record.Fields.Single(field => field.Name == "node.type")) switch
        { "3" => 0, "4" => 1, "2" => 2, "1" => 3, _ => 4 };

    private static byte[] EncodeCompilerValue(NativeField field, string value)
    {
        if (field.Kind is NativeValueKind.Ascii or NativeValueKind.Unicode or NativeValueKind.WesternAnsi) return field.Encode(value);
        var number = long.Parse(value);
        if (field.BitMask != 0) return field.Encode(number == 0 ? "0" : "1");
        // Model the original compiler's explicit integer casts, including -1 sentinels.
        return field.Encode(field.Kind switch
        {
            NativeValueKind.Byte => unchecked((byte)number).ToString(), NativeValueKind.UInt16 => unchecked((ushort)number).ToString(), NativeValueKind.Int16 => unchecked((short)number).ToString(), NativeValueKind.UInt32 => unchecked((uint)number).ToString(), _ => unchecked((int)number).ToString()
        });
    }

    private TilePoint HeartPosition(byte owner)
    {
        // Compiler_InfoHeartPosition uses the first heart room tile and its
        // (+2,+2) centre, not a search for any later complete 5x5 square.
        // Shape validation is separate; reading a default never repairs tiles.
        for (var y = 0; y < Height; y++) for (var x = 0; x < Width; x++)
            if (GetTile(x, y) is { Terrain: 14, Variant: 1 } first && first.Owner == owner)
            {
                if (x + 2 >= Width || y + 2 >= Height) return new(0, 0);
                var center = GetTile(x + 2, y + 2);
                return center is { Terrain: 14, Variant: 1 } && center.Owner == owner ? new(x + 2, y + 2) : new(0, 0);
            }
        return new(0, 0);
    }

    private void CompileLegacyAreasAndBands(NativeMapDocument output, IReadOnlyList<NativeRecord> source, Func<NativeRecord, string, string, string> value, Func<uint, string, string, ushort> root, Action<NativeRecord, string, string> set)
    {
        string V(NativeRecord record, string name) => value(record, name, "0");
        void CompileRegion(NativeRecord record)
        {
            var type = V(record, "node.type");
            var structure = type switch { "0" => "GAMEAREA", "1" => "GAMEGATE", "2" => "GAMEEFFECT", _ => throw new InvalidDataException("Invalid project rectangle type.") };
            var x = int.Parse(V(record, "rect.x1")); var y = int.Parse(V(record, "rect.y1"));
            var endX = int.Parse(V(record, "rect.x2")); var endY = int.Parse(V(record, "rect.y2"));
            if (endX < x || endY < y) throw new InvalidDataException("Rectangle endpoints are reversed.");
            var target = output.AddRecord(RecordTemplate.All.Single(template => template.Structure == structure), new(x, y), 1);
            if (type == "0")
            {
                foreach (var (native, editor) in new[] { ("dwAreaLocationEndX", "rect.x2"), ("dwAreaLocationEndY", "rect.y2"), ("wAreaWaitDelay", "waitdelay"), ("bAreaFlags", "flags"), ("bAreaID", "id"), ("bAreaNextID", "nextid") }) set(target, native, V(record, editor));
                set(target, "wAreaTrigger", root(4, "area", V(record, "id")).ToString());
            }
            else if (type == "2")
            {
                set(target, "wEffectWidth", (endX - x).ToString()); set(target, "wEffectHeight", (endY - y).ToString()); set(target, "bEffectID", V(record, "id")); set(target, "bEffectFrequency", V(record, "freq"));
                for (var index = 1; index <= 4; index++) set(target, "wEffect" + index, V(record, "effect" + index));
            }
            else
            {
                var (gateType, orientation) = HeroGateTerrainInfo(x, y);
                set(target, "bGateOrientation", orientation.ToString()); set(target, "bGateType", gateType.ToString()); set(target, "bGateID", "1"); set(target, "wGateHealth", "100");
            }
        }
        foreach (var record in source.Where(record => record.File.Signature == LegacyAreas && V(record, "node.type") != "2")
            .OrderBy(record => V(record, "node.type") == "0" ? 0 : 1)) CompileRegion(record);
        for (byte player = 3; player <= 7; player++)
        {
            var heart = HeartPosition(player); if (heart == new TilePoint(0, 0)) continue;
            var gate = output.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEGATE"), heart, player);
            set(gate, "bGateType", "14"); set(gate, "bGateID", "0");
            var options = source.Where(record => record.Category == "Project.MAPOPTIONS").ElementAt(player - 1); set(gate, "wGateHealth", V(options, "PlayerHealth"));
        }
        // Keeper hearts belong to GenerateGates, before GenerateEffects.
        foreach (var record in source.Where(record => record.File.Signature == LegacyAreas && V(record, "node.type") == "2")) CompileRegion(record);
        foreach (var record in source.Where(record => record.Category == "Project.MTHBAND"))
        {
            var target = output.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEBAND"), new(0, 0));
            // Compiler_GenerateHeros zeroes the whole slot array and fills only
            // the active prefix. Native editing templates initialise every
            // slot's band ID; those defaults must not leak into compilation.
            target.File.Bytes.AsSpan(target.Offset + 51, 16 * 32).Clear();
            set(target, "bBandID", V(record, "id")); set(target, "wBandTrigger", root(8, "band", V(record, "uniqueid")).ToString());
            for (var index = 0; index < 16; index++)
            {
                if (V(record, $"heros[{index}].isvalid") == "0") break;
                var slot = output.Records().Single(item => item.File == target.File && item.Offset == target.Offset + 51 + index * 32);
                foreach (var (native, editor) in new[] { ("bGlobalGoldHeld", "goldheld"), ("bGlobalLevel", "level"), ("bGlobalBehavior", "options2"), ("bHeroTargetArea", "area"), ("dwGlobalHealth", "health"), ("bGlobalPlayer", "enemy"), ("bHeroObjective", "objective"), ("bGlobalCreatureID", "id"), ("bHeroOptions", "options") }) set(slot, native, V(record, $"heros[{index}].{editor}"));
                set(slot, "bHeroBandID", V(record, "id")); set(slot, "wHeroTrigger", root(0x10, "creature", V(record, $"heros[{index}].uniqueid")).ToString());
            }
        }
    }

    private void CompileLegacyVariables(NativeMapDocument output, NativeRecord info, IReadOnlyList<NativeRecord> players, Func<NativeRecord, string, string, string> value, bool includeDeclaredKeepers)
    {
        var file = output.CollectionFile(0xE6);
        var records = new List<byte[]>();
        void Add(uint key, uint first, uint second, uint third)
        { var bytes = new byte[16]; WriteUInt32(bytes, 0, key); WriteUInt32(bytes, 4, first); WriteUInt32(bytes, 8, second); WriteUInt32(bytes, 12, third); records.Add(bytes); }
        string V(NativeRecord record, string name) => value(record, name, "0");
        var creatures = LegacyCompilerRules.Elements("BadCreatures").Concat(LegacyCompilerRules.Elements("BadElites")).Concat(LegacyCompilerRules.Elements("Heros")).ToArray();
        if (creatures.Length != 45) throw new InvalidDataException("Original creature catalogue is incomplete.");
        for (uint player = 1; player <= 7; player++)
        {
            if (HeartPosition((byte)player) == new TilePoint(0, 0) && !(includeDeclaredKeepers && player >= 3 && player < int.Parse(V(info, "Players")) + 3)) continue;
            var options = players[(int)player - 1];
            for (var index = 0; index < creatures.Length; index++)
            { var count = int.Parse(V(options, $"CreaturesCount[{index}]")); if (count != -1) Add(1, creatures[index].Variable, unchecked((uint)count), player); }
            foreach (var (group, array, type) in new[] { ("VarRooms", "Rooms", 1), ("Traps", "Traps", 3), ("Doors", "Doors", 4), ("Spells", "Spells", 5) })
            {
                var elements = LegacyCompilerRules.Elements(group);
                for (var index = 0; index < elements.Count; index++)
                { var state = byte.Parse(V(options, $"{array}[{index}]")); if (state != 255) Add(2, (uint)type | (player << 16), elements[index].Variable, state); }
            }
        }
        for (var index = 0; index < creatures.Length; index++)
        {
            if (index is >= 16 and < 29) continue; // Original compiler skips elite modifier rows.
            for (var parameter = 1; parameter <= 4; parameter++)
            { var data = uint.Parse(V(info, $"CreaturesParams[{index}][{parameter - 1}]")); if (data != 0) Add(0x4D, creatures[index].ID, (uint)parameter, data); }
        }
        for (var left = 0; left < 7; left++) for (var right = left; right < 7; right++)
            if (V(info, $"Allies[{left}][{right}]") != "0") Add(0x42, (uint)left + 1, (uint)right + 1, 0);
        foreach (var variable in LegacyCompilerRules.Elements("MapVars").Where(item => item.ID < 256 && item.Icon == 0))
        { var data = unchecked((uint)int.Parse(V(info, $"Variables[{variable.ID}]"))); if (data != variable.Variable) Add(variable.ID, data, 0, 0); }
        foreach (var bytes in records) output.InsertRecord(file, bytes);
    }

    private static void CompileLegacyDefinitions(NativeMapDocument output, IReadOnlyList<NativeRecord> source)
    {
        foreach (var definition in LegacyGlobalSchemas.All)
        {
            var structure = definition.Group switch { "Terrains" => "GLOBALTERRAIN", "Rooms" => "GLOBALROOM", "Doors" => "GLOBALDOOR", "Traps" => "GLOBALTRAP", "Spells" => "GLOBALSPELL", "Creatures" => "GLOBALCREATURE", _ => "GLOBALCRSPELLS" };
            var editorRecords = source.Where(record => record.Category == "Project." + structure).ToArray(); if (editorRecords.Length == 0) continue;
            // Compiler_TaskGenerate leaves the base file in use when
            // Properties_GlobalChanged is false, even if an array exists.
            var regenerate = SourceGlobalDefinitionChanged(definition.Group, editorRecords);
            if (!regenerate) continue;
            var nativeRecords = output.Records().Where(record => record.Category == "Definition." + definition.Group).ToArray(); var order = LegacyCompilerRules.DefinitionOrder(definition.Group);
            if (nativeRecords.Length != order.Count || nativeRecords.Any(record => record.Fields.Count == 0)) throw new InvalidDataException("A matching verified native definition file is required for " + definition.Group);
            for (var index = 0; index < order.Count; index++)
            {
                if (order[index] < 0) continue;
                var editor = editorRecords[order[index]]; var native = nativeRecords[index];
                foreach (var field in definition.Fields)
                {
                    var sourceField = editor.Fields.FirstOrDefault(item => item.Name == field.Name) ?? throw new InvalidDataException("Unverified global compiler field: " + field.Name);
                    var bytes = EncodeCompilerValue(field, editor.Read(sourceField)); var offset = native.Offset + field.Offset;
                    if (field.BitMask != 0) native.File.Bytes[offset] = (byte)((native.File.Bytes[offset] & ~field.BitMask) | (bytes[0] == 0 ? 0 : field.BitMask));
                    else bytes.CopyTo(native.File.Bytes, offset);
                }
                // The original trap/keeper-spell compiler clears the second
                // argument for one-argument/no-options types. Creature spells
                // deliberately have no equivalent post-processing.
                if (definition.Group is "Traps" or "Spells")
                    foreach (var slot in GlobalProjectiles.Slots(definition.Group))
                    {
                        var names = GlobalProjectiles.Fields(definition.Group, slot);
                        var option = GlobalProjectiles.Find(editor.Read(editor.Fields.Single(field => field.Name == names.Type)));
                        if (option?.Form is 1 or 255)
                        {
                            var field = native.Fields.Single(field => field.Name == names.Data2);
                            field.Encode("0").CopyTo(native.File.Bytes, native.Offset + field.Offset);
                        }
                    }
            }
            if (regenerate)
                foreach (var file in nativeRecords.Select(record => record.File).Distinct()) StampGeneratedGameDate(file, 28);
        }
    }
}
