using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class PlayerAIOptionsChecks
{
    private static void Expect(bool condition,string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException or OverflowException) { return; } throw new Exception("Unsafe custom AI operation accepted."); }
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file,file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document,Dictionary<NativeFile,byte[]> expected)
    { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(expected[file]),"AI operation touched unexpected bytes: " + file.ExportPath); }
    private static NativeField Field(NativeMapDocument document,NativeRecord record,string name) => record.Fields.Single(field => field.Name == (document.IsLegacyProject ? name : LegacyCompilerRules.Players.Single(mapping => mapping.Source == name).Target));
    private static void Encode(byte[] destination,int offset,NativeField field,long value)
    {
        var bytes = destination.AsSpan(offset + field.Offset,field.Size);
        switch (field.Kind)
        {
            case NativeValueKind.Byte: bytes[0] = checked((byte)value); break;
            case NativeValueKind.UInt16: BinaryPrimitives.WriteUInt16LittleEndian(bytes,checked((ushort)value)); break;
            case NativeValueKind.Int16: BinaryPrimitives.WriteInt16LittleEndian(bytes,checked((short)value)); break;
            case NativeValueKind.UInt32: BinaryPrimitives.WriteUInt32LittleEndian(bytes,checked((uint)value)); break;
            case NativeValueKind.Int32: BinaryPrimitives.WriteInt32LittleEndian(bytes,checked((int)value)); break;
            default: throw new Exception("Unexpected AI numeric layout.");
        }
    }
    private static void SourceCoverage()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName,"DK2MapEditor.slnx"))) root = root.Parent;
        var source = File.ReadAllText(Path.Combine(root!.FullName,"Source","Sources","Properties","Map","Properties_Map_Computer.c"));
        var fields = Regex.Matches(source,@"CurrentPlayerCPUSettings\.(\w+)\s*=").Select(match => match.Groups[1].Value).Distinct()
            .Where(name => name != "PlayerAI" && !name.StartsWith("PlayerCPUArea",StringComparison.Ordinal)).Order().ToArray();
        Expect(fields.Length == 67 && fields.SequenceEqual(PlayerAIOptions.Parameters.Select(parameter => parameter.Name).Order()),"Manual AI catalogue does not cover exactly all source editable scalar fields.");
        foreach (var (symbol,name) in new[] { ("Expand","PlayerExpandMode"),("Angle","PlayerCorridorStyle"),("Distance","PlayerDistance"),("SightOfEvil","PlayerSpellsSightOfEvil"),("CallToArms","PlayerSpellsCallToArms"),("Sack","PlayerCreaturesSackMethod"),("Breach","PlayerFirstAttackBreach"),("Dig","PlayerFirstAttackDig"),("Library","PlayerLibraryUsage"),("Jailed","PlayerJailedFate") })
        {
            var body = Regex.Match(source,@"\*" + symbol + @"\[\]\s*=\s*\{([^}]+)\}").Groups[1].Value;
            var count = Regex.Matches(body,@"szCfgCPU\w+").Count; var parameter = PlayerAIOptions.Parameters.Single(parameter => parameter.Name == name);
            Expect(count > 0 && parameter.Choices.Select(choice => choice.Value).SequenceEqual(Enumerable.Range(0,count).Select(value => value.ToString(CultureInfo.InvariantCulture))),"AI source combo order/count lost: " + name);
        }
        Expect(PlayerAIOptions.Parameters.All(parameter => parameter.English.Length > 0 && parameter.Russian.Length > 0 && parameter.Choices.All(choice => choice.English.Length > 0 && choice.Russian.Length > 0)),"Missing bilingual AI names.");
        Expect(PlayerAIOptions.Rooms.Select(room => long.Parse(room.Value)).SequenceEqual(new long[] { 1,2,3,4,6,7,8,9,10,11,12,13,14,15,16 }),"Build priority used terrain/variable IDs rather than source room IDs.");
        Expect(!PlayerAIOptions.Parameters.Any(parameter => parameter.Name == "PlayerUseCountInFirstAttack") && source.Contains("CheckDlgButton(hDlg,704,BST_INDETERMINATE)",StringComparison.Ordinal),"Disabled original count-condition flag invented as a manual control.");
        var maxima = new Dictionary<string,int> { ["PlayerMineGoldAmount"] = 65535,["PlayerMana"] = 64000,["PlayerSpeed"] = 4,["PlayerFirstAttackLevel"] = 10,["PlayerFlexibility"] = 100,["PlayerUseTrapsnDoors"] = 100,["PlayerFirstAttackCreatures"] = 100,["PlayerBattlePrison"] = 100,["PlayerBattleHealth"] = 100,["PlayerBattleWait"] = 32767,["PlayerBattleCallToHarmsPlace"] = 32767,["PlayerBattleCallToHarmsRemove"] = 32767,["PlayerFindSpecialsTiles"] = 32767,["PlayerFindSpecialsExplore"] = 100,["PlayerFindSpecialsChances"] = 100,["PlayerGoldValue"] = 100,["PlayerManaValue"] = 100,["PlayerOpenness"] = 100 };
        foreach (var parameter in PlayerAIOptions.Parameters.Where(parameter => parameter.Choices.Count == 0))
            Expect(parameter.Maximum == maxima.GetValueOrDefault(parameter.Name,255) && parameter.Minimum == (parameter.Name == "PlayerSpeed" ? 1 : 0),"Source manual numeric range lost: " + parameter.Name);
    }
    public static void Run()
    {
        SourceCoverage(); PlayerAIFieldCaptionChecks.Run(); PlayerAIRoomImageChecks.Run();
        var directory = Path.Combine(Path.GetTempPath(),"DK2CustomAI-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var native in new[] { false,true })
            {
                var document = NativeMapDocument.CreateNewMap(new("CustomAI","CustomAI",38,20,Players: 5,PlaceHearts: true,NativeFormat: native)); var initial = Snapshot(document);
                foreach (var player in Enumerable.Range(1,7).Select(value => (uint)value))
                {
                    var state = document.GetPlayerAIConfiguration(player); Expect(state.IsCurrent && state.Values.Count == 67 && PlayerAIOptions.ValidRoomOrder(state.RoomOrder),"Current AI configuration not projected completely."); Exact(document,initial);
                    var values = PlayerAIOptions.Parameters.ToDictionary(parameter => parameter.Name,parameter => (long)parameter.Maximum); var rooms = state.RoomOrder.Reverse().ToArray(); var expected = Snapshot(document);
                    foreach (var parameter in PlayerAIOptions.Parameters)
                    {
                        var field = Field(document,state.Record,parameter.Name); var value = values[parameter.Name];
                        if (parameter.Mask != 0) value = (long.Parse(field.Read(state.Record.File.Data.Span.Slice(state.Record.Offset,state.Record.Length))) & ~(long)parameter.Mask) | (long)parameter.Mask;
                        Encode(expected[state.Record.File],state.Record.Offset,field,value);
                    }
                    for (var index = 0; index < 15; index++) Encode(expected[state.Record.File],state.Record.Offset,Field(document,state.Record,$"PlayerRoomToBuildPri[{index}]"),rooms[index]);
                    document.SetPlayerAIConfiguration(state,values,rooms); Exact(document,expected); Expect(!state.IsCurrent,"Applied AI draft incorrectly remains current.");
                    var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(directory,$"{native}-{player}")); var reopened = NativeMapDocument.Load(saved);
                    // A project's MainFile is its unchanged original container;
                    // SaveCopy rebuilds it from separately edited section buffers.
                    foreach (var file in document.Files.Where(file => native || file != document.MainFile)) Expect(SaveProjectionChecks.FileEquals(document,reopened.Files.Single(item => item.ExportPath == file.ExportPath),file.Data.ToArray()),"Custom AI saved copy lost unrelated bytes: " + file.ExportPath);
                    var savedAgain = SaveProjectionChecks.SaveCopy(reopened, Path.Combine(directory,$"again-{native}-{player}")); Expect(native ? File.ReadAllBytes(saved).AsSpan().SequenceEqual(File.ReadAllBytes(savedAgain)) : SaveProjectionChecks.BeforeEquals(reopened,File.ReadAllBytes(saved)),"AI export/reopening/re-export changed serialized bytes.");
                    Expect(reopened.GetPlayerAIConfiguration(player).Values.SequenceEqual(document.GetPlayerAIConfiguration(player).Values) && reopened.GetPlayerAIConfiguration(player).RoomOrder.SequenceEqual(rooms),"Custom AI values/order changed on reopening.");
                    if (!native)
                    {
                        var compiled = document.CompileLegacyProject("CompiledAI",NativeMapDocument.Create(38,20,"AIBase")); var compiledState = compiled.GetPlayerAIConfiguration(player);
                        Expect(compiledState.Values.SequenceEqual(document.GetPlayerAIConfiguration(player).Values) && compiledState.RoomOrder.SequenceEqual(rooms),"Custom AI source/native compiler mappings lost parameters/order."); Exact(document,expected);
                    }
                    document.SetPlayerAIConfiguration(document.GetPlayerAIConfiguration(player),values,rooms); document.Undo(); Exact(document,initial); Expect(!document.CanUndo,"Repeated unchanged custom AI created history."); document.Redo(); Exact(document,expected); document.Undo(); Exact(document,initial);
                }
                foreach (var parameter in PlayerAIOptions.Parameters)
                {
                    foreach (var value in parameter.Choices.Count == 0 ? new long[] { parameter.Minimum,parameter.Maximum } : parameter.Choices.Select(choice => long.Parse(choice.Value,CultureInfo.InvariantCulture)).ToArray())
                    {
                        var state = document.GetPlayerAIConfiguration(3); var expected = Snapshot(document); var field = Field(document,state.Record,parameter.Name);
                        var stored = parameter.Mask == 0 ? value : (long.Parse(field.Read(state.Record.File.Data.Span.Slice(state.Record.Offset,state.Record.Length))) & ~(long)parameter.Mask) | (value == 0 ? 0L : (long)parameter.Mask);
                        Encode(expected[state.Record.File],state.Record.Offset,field,stored); document.SetPlayerAIConfiguration(state,new Dictionary<string,long> { [parameter.Name] = value }); Exact(document,expected);
                        if (document.CanUndo) document.Undo(); Exact(document,initial);
                    }
                    foreach (var value in new long[] { (long)parameter.Minimum - 1,(long)parameter.Maximum + 1 })
                    {
                        Reject(() => document.SetPlayerAIConfiguration(document.GetPlayerAIConfiguration(3),new Dictionary<string,long> { [parameter.Name] = value })); Exact(document,initial); Expect(!document.CanUndo,"Invalid AI parameter created history.");
                    }
                }
                var baseline = document.GetPlayerAIConfiguration(3);
                if (!native) CheckHistorical(document,Path.Combine(directory,"historical"));
                else
                {
                    var file = baseline.Record.File;
                    document.Edit("Future AI player header",() => { Expect(MemoryMarshal.TryGetArray(file.Data,out var buffer),"Future AI fixture buffer unavailable."); BinaryPrimitives.WriteUInt32LittleEndian(buffer.Array!.AsSpan(buffer.Offset + 4),99); });
                    var future = Snapshot(document); Reject(() => document.GetPlayerAIConfiguration(3)); Reject(() => document.SetPlayerAIConfiguration(baseline,new Dictionary<string,long> { ["PlayerDefaultTiles"] = 14 })); Expect(!baseline.IsCurrent,"Future native layout retained AI draft authority."); Exact(document,future); document.Undo(); Exact(document,initial);
                }
                Reject(() => document.SetPlayerAIConfiguration(baseline,new Dictionary<string,long> { ["PlayerDefaultTiles"] = 14,["PlayerSpeed"] = 99 })); Exact(document,initial);
                foreach (var name in new[] { "PlayerAI","PlayerUseCountInFirstAttack","PlayerGold","Unknown" }) { Reject(() => document.SetPlayerAIConfiguration(baseline,new Dictionary<string,long> { [name] = 1 })); Exact(document,initial); }
                foreach (var order in new[] { baseline.RoomOrder.Take(14).ToArray(),Enumerable.Repeat(1L,15).ToArray(),baseline.RoomOrder.Skip(1).Append(255L).ToArray() })
                { Reject(() => document.SetPlayerAIConfiguration(baseline,new Dictionary<string,long> { ["PlayerDefaultTiles"] = 14 },order)); Exact(document,initial); }
                var foreign = NativeMapDocument.CreateNewMap(new("ForeignAI","ForeignAI",38,20,NativeFormat: native)); Reject(() => foreign.SetPlayerAIConfiguration(baseline,new Dictionary<string,long>()));
                var gold = baseline.Record.Fields.Single(field => field.Name == (native ? "bPlayerGold" : "PlayerGold")); document.SetField(baseline.Record,gold,"1234"); var changed = Snapshot(document);
                Reject(() => document.SetPlayerAIConfiguration(baseline,new Dictionary<string,long> { ["PlayerDefaultTiles"] = 14 })); Exact(document,changed); document.Undo(); Exact(document,initial); Expect(baseline.IsCurrent,"Exact undo did not restore AI draft authority.");
                foreach (var (name,value) in new[] { ("PlayerExpandMode",255L),("PlayerSpeed",0L),("PlayerCreaturesSlap",161L) })
                {
                    var state = document.GetPlayerAIConfiguration(3); document.SetField(state.Record,Field(document,state.Record,name),value.ToString(CultureInfo.InvariantCulture));
                }
                var unknown = Snapshot(document); var unknownState = document.GetPlayerAIConfiguration(3); document.SetPlayerAIConfiguration(unknownState,unknownState.Values,unknownState.RoomOrder); Exact(document,unknown);
                document.SetPlayerAIConfiguration(unknownState,new Dictionary<string,long> { ["PlayerCreaturesSlap"] = 0 });
                var slap = Field(document,unknownState.Record,"PlayerCreaturesSlap"); Expect(slap.Read(unknownState.Record.File.Data.Span.Slice(unknownState.Record.Offset,unknownState.Record.Length)) == "129","Named no-slap toggle lost unknown bits."); document.Undo(); Exact(document,unknown);
                document.SetPlayerAIConfiguration(document.GetPlayerAIConfiguration(3),new Dictionary<string,long> { ["PlayerExpandMode"] = 2,["PlayerSpeed"] = 4 }); document.Undo(); Exact(document,unknown);
                var orderState = document.GetPlayerAIConfiguration(3); document.SetField(orderState.Record,Field(document,orderState.Record,"PlayerRoomToBuildPri[0]"),"255");
                var badOrder = Snapshot(document); orderState = document.GetPlayerAIConfiguration(3); Expect(!PlayerAIOptions.ValidRoomOrder(orderState.RoomOrder),"Unknown room ID hidden by projection.");
                document.SetPlayerAIConfiguration(orderState,new Dictionary<string,long> { ["PlayerDefaultTiles"] = 14 },orderState.RoomOrder); document.Undo(); Exact(document,badOrder);
                if (native)
                {
                    var player7 = document.PlayerSettingsRows().Single(row => row.ID == 7).Record; document.SetField(player7,player7.Fields.Single(field => field.Name == "bGlobalPlayer"),"3");
                    var duplicates = Snapshot(document); Reject(() => document.GetPlayerAIConfiguration(3)); Exact(document,duplicates);
                }
            }
        }
        finally { Directory.Delete(directory,true); }
    }
    private static void CheckHistorical(NativeMapDocument document,string directory)
    {
        Directory.CreateDirectory(directory); var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(directory,"basis")); var sections = LegacySectionFile.Read(saved);
        var players = sections.Sections.Single(section => section.Signature == NativeMapDocument.LegacyPlayers);
        foreach (var version in new[] { 1,2,99 })
        {
            var payload = version == 1 ? new byte[7 * LegacyProjectSchemas.Size("MAPOPTIONSV1")] : players.Payload.ToArray();
            var path = Path.Combine(directory,$"version-{version}.dkm"); File.WriteAllBytes(path,LegacySectionFile.Create(sections.Sections.Select(section => section == players ? new LegacySection(players.Signature,version,payload) : section)).ToArray());
            var old = NativeMapDocument.Load(path); var before = Snapshot(old);
            if (version == 2)
            {
                var state = old.GetPlayerAIConfiguration(3); old.SetPlayerAIConfiguration(state,new Dictionary<string,long> { ["PlayerDefaultTiles"] = 14 },state.RoomOrder.Reverse().ToArray());
                Expect(old.GetPlayerAIConfiguration(3).Values["PlayerDefaultTiles"] == 14,"Verified historical V2 custom AI did not edit."); old.Undo(); Exact(old,before);
            }
            else { Reject(() => old.GetPlayerAIConfiguration(3)); Exact(old,before); }
            if (version == 1) { old.UpgradeLegacyProject(); var state = old.GetPlayerAIConfiguration(3); Expect(state.Values.Count == 67 && state.IsCurrent,"Explicit historical upgrade did not enable complete custom AI."); }
        }
    }
}
