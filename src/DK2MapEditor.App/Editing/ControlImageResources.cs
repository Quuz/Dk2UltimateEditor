using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

internal static class ControlImageResources
{
    internal static int? ForScenarioName(ScenarioNameKind kind)=>kind switch{ScenarioNameKind.Variable=>54,ScenarioNameKind.Timer=>55,_=>null};
    internal static int ForVerticalMove(bool down)=>down?51:50;
    internal static int? ForBranchMove(ScriptBranchMove move)=>move switch{ScriptBranchMove.Up=>50,ScriptBranchMove.Down=>51,ScriptBranchMove.Outdent=>52,ScriptBranchMove.Indent=>53,_=>null};
}
