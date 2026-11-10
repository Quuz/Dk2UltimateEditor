namespace DK2MapEditor.Core.Editing;

public enum EditorFilePhase { Inspect,Read,Map,Statistics,MapInfo,Players,Objects,Regions,Bands,Scenarios,Definitions,Validate,Prepare,Write,Flush,Done }

/// <summary>Observed work, not an estimate of total operation time. A positive
/// Total applies only to the current phase (files/sections); zero is unknown.</summary>
public sealed record EditorFileProgress(EditorFilePhase Phase,string? Path=null,long Completed=0,long Total=0);

public sealed partial class NativeMapDocument
{
    private static EditorFilePhase FilePhase(uint signature)=>signature switch
    {
        0x64 or Formats.LegacySectionFile.TileSectionSignature=>EditorFilePhase.Map,
        0x5354534B=>EditorFilePhase.Statistics,
        0xDC or LegacyInfo=>EditorFilePhase.MapInfo,
        0xB4 or LegacyPlayers=>EditorFilePhase.Players,
        0xBE or LegacyThings=>EditorFilePhase.Objects,
        LegacyAreas=>EditorFilePhase.Regions,
        LegacyBands=>EditorFilePhase.Bands,
        0xD2 or LegacyEvents=>EditorFilePhase.Scenarios,
        _=>EditorFilePhase.Definitions
    };
}
