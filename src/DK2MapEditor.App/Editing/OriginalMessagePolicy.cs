namespace DK2MapEditor.App.Editing;

internal static class OriginalMessagePolicy
{
    internal static IReadOnlyList<string> Buttons(string kind)=>Array.AsReadOnly(kind switch
    {"OK"=>new[]{"OK"},"OKCancel"=>new[]{"OK","Cancel"},"YesNo"=>new[]{"Yes","No"},"YesNoCancel"=>new[]{"Yes","No","Cancel"},_=>throw new ArgumentOutOfRangeException(nameof(kind))});
    internal static string Default(string kind,string requested)=>Buttons(kind).Contains(requested)?requested:Buttons(kind)[0];
    internal static string Close(string kind)=>Buttons(kind).Contains("Cancel")?"Cancel":Buttons(kind).Contains("No")?"No":"OK";
    internal static int? Icon(string kind)=>kind switch{"None"=>null,"Information" or "Asterisk"=>80,"Warning" or "Exclamation"=>81,"Error" or "Hand" or "Stop"=>82,"Question"=>83,_=>throw new ArgumentOutOfRangeException(nameof(kind))};
    internal static string Label(string result)=>result switch{"OK"=>"Message.OK","Cancel"=>"Message.Cancel","Yes"=>"Message.Yes","No"=>"Message.No",_=>throw new ArgumentOutOfRangeException(nameof(result))};
}
