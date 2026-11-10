using System.Globalization;
using System.Text.RegularExpressions;
namespace DK2MapEditor.Core.Editing;

/// <summary>Import assignments audited against Files_Game.c, independently of compiler output recipes.</summary>
public static class LegacyNativeImportRules
{
    public sealed record Assignment(string ProjectField, string NativeField);
    public static IReadOnlyList<Assignment> Metadata { get; } = Array.AsReadOnly(new Assignment[] { new("GameTicks","bMainGameTicks"),
        new("Options","bMainOptions"),
        new("Options2","bMainOptions2"),
        new("HornyTalismanPieces","bMainHornyTalismanPieces"),
        new("RewardPrev1","bMainRewardPrev1"),
        new("RewardPrev2","bMainRewardPrev2"),
        new("RewardPrev3","bMainRewardPrev3"),
        new("RewardNext1","bMainRewardNext1"),
        new("RewardNext2","bMainRewardNext2"),
        new("RewardNext3","bMainRewardNext3"),
        new("Soundtrack","bMainSoundtrack"),
        new("TextTableID","bMainTextTableID"),
        new("SpecialLevelNumber","bMainSpecialLevelNumber"),
        new("TextIntroCreatureID0","bMainIntroCreatureID0"),
        new("TextIntroCreatureID1","bMainIntroCreatureID1"),
        new("TextIntroCreatureID2","bMainIntroCreatureID2"),
        new("TextIntroCreatureID3","bMainIntroCreatureID3"),
        new("TextIntroCreatureID4","bMainIntroCreatureID4"),
        new("TextIntroCreatureID5","bMainIntroCreatureID5"),
        new("TextIntroCreatureID6","bMainIntroCreatureID6"),
        new("TextIntroCreatureID7","bMainIntroCreatureID7"),
        new("LevelNumber","bMainLevelNumber"),
        new("Players","bMainPlayers"),
        new("TextTitleID","bMainTitleTextID"),
        new("TextPlotID","bMainTitlePlotTextID"),
        new("TextDebriefingID","bMainDebriefingTextID"),
        new("TextObjectiveID","bMainObjectiveTextID"),
        new("TextSubObjective3ID","bMainSubObjective3TextID"),
        new("TextSubObjective1ID","bMainSubObjective1TextID"),
        new("TextSubObjective2ID","bMainSubObjective2TextID"),
        new("TextIntroTextID0","bMainIntroTextID0"),
        new("TextIntroTextID1","bMainIntroTextID1"),
        new("TextIntroTextID2","bMainIntroTextID2"),
        new("TextIntroTextID3","bMainIntroTextID3"),
        new("TextIntroTextID4","bMainIntroTextID4"),
        new("TextIntroTextID5","bMainIntroTextID5"),
        new("TextIntroTextID6","bMainIntroTextID6"),
        new("TextIntroTextID7","bMainIntroTextID7"),
        new("SpeechPreLevelID","bMainPreLevelSpeechID"),
        new("SpeechWinID","bMainWinSpeechID"),
        new("SpeechLooseID","bMainLooseSpeechID"),
        new("SpeechNewsID","bMainNewsSpeechID"),
        new("SpeechGenericPreLevelID","bMainGenPreLevelSpeechID"),
        new("Name","wstrMainName"),
        new("Description","wstrMainDescription"),
        new("Author","wstrMainAuthor"),
        new("Email","wstrMainEmail"),
        new("Information","wstrMainInformation"),
        new("SpeechFile","szMainSpeech") });
    public static IReadOnlyList<Assignment> Players { get; } = Array.AsReadOnly(new Assignment[] { new("PlayerGold","bPlayerGold"),
        new("PlayerComputer","bPlayerCPU"),
        new("PlayerAI","bPlayerAI"),
        new("PlayerSpeed","bPlayerMiscSpeed"),
        new("PlayerOpenness","bPlayerMiscOpenness"),
        new("PlayerGoldValue","bPlayerMiscGold"),
        new("PlayerManaValue","bPlayerMiscMana"),
        new("PlayerMineGoldAmount","bPlayerMiscMineGoldUntil"),
        new("PlayerMana","bPlayerMiscStartingMana"),
        new("PlayerLibraryUsage","bPlayerMiscLibrary"),
        new("PlayerImpsToTilesRatio","bPlayerMiscImpsToTilesRatio"),
        new("PlayerFindSpecialsExplore","bPlayerMiscChanceToFindSpecials"),
        new("PlayerFindSpecialsChances","bPlayerMiscChanceOfFindingSpecials"),
        new("PlayerFindSpecialsTiles","bPlayerMiscTilesToFindSpecials"),
        new("PlayerJailedFate","bPlayerMiscJailedFate"),
        new("PlayerResearchedRoom","bPlayerResearchedRoom"),
        new("PlayerRebuildRoom","bPlayerRebuildRoom"),
        new("PlayerDefaultTiles","bPlayerDefaultTiles"),
        new("PlayerTilesBetweenRooms","bPlayerTilesLeftBetweenRooms"),
        new("PlayerDistance","bPlayerDistance"),
        new("PlayerCorridorStyle","bPlayerCorridorStyle"),
        new("PlayerExpandMode","bPlayerExpandMode"),
        new("PlayerTilesFromHeart","bPlayerTilesFromHeart"),
        new("PlayerTilesFromClaimed","bPlayerTilesFromClaimedArea"),
        new("PlayerFlexibility","bPlayerFlexibility"),
        new("PlayerRoomToBuildPri[0]","bPlayerRoomToBuildPri1"),
        new("PlayerRoomToBuildPri[1]","bPlayerRoomToBuildPri2"),
        new("PlayerRoomToBuildPri[2]","bPlayerRoomToBuildPri3"),
        new("PlayerRoomToBuildPri[3]","bPlayerRoomToBuildPri4"),
        new("PlayerRoomToBuildPri[4]","bPlayerRoomToBuildPri5"),
        new("PlayerRoomToBuildPri[5]","bPlayerRoomToBuildPri6"),
        new("PlayerRoomToBuildPri[6]","bPlayerRoomToBuildPri7"),
        new("PlayerRoomToBuildPri[7]","bPlayerRoomToBuildPri8"),
        new("PlayerRoomToBuildPri[8]","bPlayerRoomToBuildPri9"),
        new("PlayerRoomToBuildPri[9]","bPlayerRoomToBuildPri10"),
        new("PlayerRoomToBuildPri[10]","bPlayerRoomToBuildPri11"),
        new("PlayerRoomToBuildPri[11]","bPlayerRoomToBuildPri12"),
        new("PlayerRoomToBuildPri[12]","bPlayerRoomToBuildPri13"),
        new("PlayerRoomToBuildPri[13]","bPlayerRoomToBuildPri14"),
        new("PlayerRoomToBuildPri[14]","bPlayerRoomToBuildPri15"),
        new("PlayerSellCapturedRoom","bPlayerSellCapturedRoomIfLowOnGold"),
        new("PlayerCreateEmptyAreas","bPlayerCreateEmptyAreasWhenIdle"),
        new("PlayerBuildBiggerLair","bPlayerBuildBiggerLair"),
        new("PlayerTrapUseStyle","bPlayerTrapUseStyle"),
        new("PlayerPreference","bPlayerPreference"),
        new("PlayerDoorUsage","bPlayerDoorUsage"),
        new("PlayerUseTrapsnDoors","bPlayerChanceOfLookingTrapsnDoors"),
        new("PlayerSpellsSightOfEvil","bPlayerSpellsSightOfEvil"),
        new("PlayerSpellsInBattle","bPlayerSpellsInBattle"),
        new("PlayerSpellsPower","bPlayerSpellsPower"),
        new("PlayerSpellsCallToArms","bPlayerSpellsCallToArms"),
        new("PlayerBoulderInCorridors","bPlayerBoulderInCorridors"),
        new("PlayerBoulderInAngles","bPlayerBoulderInAngles"),
        new("PlayerSpellsLightning","bPlayerSpellsLightning"),
        new("PlayerCreaturesSackMethod","bPlayerCreaturesSackMethod"),
        new("PlayerCreaturesImps","bPlayerCreaturesImps"),
        new("PlayerCreaturesSlap","bPlayerCreaturesSlap"),
        new("PlayerCreaturesHappyUnhappy","bPlayerCreaturesHappyUnhappy"),
        new("PlayerCreaturesHappyUngry","bPlayerCreaturesHappyUngry"),
        new("PlayerCreaturesDisposeUngry","bPlayerCreaturesDisposeUngry"),
        new("PlayerCreaturesDisposeRubish","bPlayerCreaturesDisposeRubish"),
        new("PlayerUseLevelInFirstAttack","bPlayerUseLevelInFirstAttack"),
        new("PlayerUseThreatInFirstAttack","bPlayerUseThreatInFirstAttack"),
        new("PlayerFirstAttackAllRooms","bPlayerFirstAttackAllRooms"),
        new("PlayerFirstAttackAllSpells","bPlayerFirstAttackAllSpells"),
        new("PlayerFirstAttackOnlyAttackers","bPlayerFirstAttackOnlyAttackers"),
        new("PlayerFirstAttackDontAttack","bPlayerFirstAttackDontAttack"),
        new("PlayerFirstAttackCount","bPlayerFirstAttackCount"),
        new("PlayerFirstAttackLevel","bPlayerFirstAttackLevel"),
        new("PlayerFirstAttackThreat","bPlayerFirstAttackThreat"),
        new("PlayerFirstAttackBreach","bPlayerFirstAttackBreach"),
        new("PlayerFirstAttackDig","bPlayerFirstAttackDig"),
        new("PlayerFirstAttackPoints","bPlayerFirstAttackPoints"),
        new("PlayerFirstAttackCreatures","bPlayerFirstAttackCreatures"),
        new("PlayerBattleDropped","bPlayerBattleDropped"),
        new("PlayerBattleSpellType","bPlayerBattleSpellType"),
        new("PlayerBattlePrison","bPlayerBattlePrison"),
        new("PlayerBattleHealth","bPlayerBattleHealth"),
        new("PlayerBattleRemove","bPlayerBattleRemove"),
        new("PlayerBattleWait","bPlayerBattleWait"),
        new("PlayerBattleCallToHarmsRemoveNoCreature","bPlayerBattleCallToHarmsRemoveNoCreature"),
        new("PlayerBattleCallToHarmsRemove","bPlayerBattleCallToHarmsRemove"),
        new("PlayerBattleCallToHarmsPlace","bPlayerBattleCallToHarmsPlace"),
        new("PlayerBattleCallToHarmsRemoveNoDanger1","bPlayerBattleCallToHarmsRemoveNoDanger1"),
        new("PlayerBattleCallToHarmsRemoveNoDanger2","bPlayerBattleCallToHarmsRemoveNoDanger2"),
        new("PlayerCPUAreaX1","bPlayerBuildAreaX1"),
        new("PlayerCPUAreaY1","bPlayerBuildAreaY1"),
        new("PlayerCPUAreaX2","bPlayerBuildAreaX2"),
        new("PlayerCPUAreaY2","bPlayerBuildAreaY2"),
        new("PlayerCameraX","bPlayerCameraX"),
        new("PlayerCameraY","bPlayerCameraY") });
    private static readonly IReadOnlyDictionary<(uint, byte), IReadOnlyList<NativeField>> eventReads = ReadEvents();
    public static IReadOnlyList<NativeField> EventArguments(uint signature, byte type) => eventReads.TryGetValue((signature,type),out var found) ? found
        : Array.AsReadOnly(new[] {new NativeField("param1",8,4,NativeValueKind.Int32),new NativeField("param5",12,4,NativeValueKind.Int32)});
    private static string Read(string name)
    {
        using var stream=typeof(LegacyNativeImportRules).Assembly.GetManifestResourceStream("Legacy."+name) ?? throw new InvalidDataException("Missing original import source.");
        using var reader=new StreamReader(stream);
        return Regex.Replace(reader.ReadToEnd(), @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
    }
    private static IReadOnlyDictionary<(uint,byte),IReadOnlyList<NativeField>> ReadEvents()
    {
        var constants=Regex.Matches(Read("Triggers.h"),@"#define\s+((?:TRIGGER|ACTION)_\w+)\s+0x([0-9A-Fa-f]+)")
            .Where(m=>uint.Parse(m.Groups[2].Value,NumberStyles.HexNumber,CultureInfo.InvariantCulture)<=255)
            .ToDictionary(m=>m.Groups[1].Value,m=>byte.Parse(m.Groups[2].Value,NumberStyles.HexNumber,CultureInfo.InvariantCulture));
        var text=Read("Files_Game.c");text=text[text.IndexOf("int Game_ExtractTriggers(",StringComparison.Ordinal)..];
        var result=new Dictionary<(uint,byte),IReadOnlyList<NativeField>>();
        foreach(var (prefix,variable,signature) in new[]{("TRIGGER","Trigger",0xD5u),("ACTION","Action",0xD6u)})
        {
            var start=text.IndexOf("switch(*(TriggersPtr+14))",StringComparison.Ordinal);
            if(prefix=="ACTION")start=text.IndexOf("switch(*(TriggersPtr+14))",start+1,StringComparison.Ordinal);
            var end=text.IndexOf(variable+"->id",start,StringComparison.Ordinal);var body=text[start..end];
            foreach(Match branch in Regex.Matches(body,@"(?<cases>(?:case\s+"+prefix+@"_\w+\s*:\s*)+)(?<body>[\s\S]*?)\bbreak\s*;"))
            {
                int offset=8;var fields=new List<NativeField>();
                var tokens=@"(?:(?:Trigger|Action)->param(?<number>\d+)\s*=\s*)?\*\(\((?<type>BYTE|WORD|LONG)\s*\*\)TriggersPtr\)\+\+|TriggersPtr\s*\+=\s*(?<skip>\d+)|TriggersPtr\+\+";
                foreach(Match read in Regex.Matches(branch.Groups["body"].Value,tokens))
                {
                    if(read.Groups["type"].Success)
                    {
                        var type=read.Groups["type"].Value;var size=type=="BYTE"?1:type=="WORD"?2:4;
                        if(read.Groups["number"].Success)fields.Add(new("param"+read.Groups["number"].Value,offset,size,type=="BYTE"?NativeValueKind.Byte:type=="WORD"?NativeValueKind.UInt16:NativeValueKind.Int32));
                        offset+=size;
                    }
                    else offset+=read.Groups["skip"].Success?int.Parse(read.Groups["skip"].Value,CultureInfo.InvariantCulture):1;
                }
                if(offset!=16)throw new InvalidDataException("Unverified original import event recipe: "+branch.Groups["cases"].Value+" bytes="+offset);
                foreach(Match name in Regex.Matches(branch.Groups["cases"].Value,prefix+@"_\w+"))result.Add((signature,constants[name.Value]),Array.AsReadOnly(fields.ToArray()));
            }
        }
        return result;
    }
}
