using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

public static class FieldNaming
{
    public static string CanonicalName(string name)
    {
        name = Regex.Replace(name, @"^heros\[(\d+)\]\.", "");
        name = Regex.Replace(name, @"^(?:rect\.|Player(?=[A-Z])|Main(?=[A-Z]))", "");
        name = name switch { "id" => "ID", "uniqueid" => "UniqueID", "x" => "LocationX", "y" => "LocationY", "x1" => "StartX", "x2" => "EndX", "y1" => "StartY", "y2" => "EndY", "or" => "NextOr", "and" => "ChildAnd", "next" => "NextOr", "trigger" => "Trigger", "one" => "Repeat", "repeat" => "Repeat", "node.type" => "Type", "dataAll" => "DataAll", _ => name };
        name = Regex.Replace(name, @"^(?:wstr|sz|dw|w|b)(?=[A-Z])(?:Main|Player|Global|Object|Trap|Door|Neutral|Hero|Keeper|Area|Gate|Effect|Band)?", "");
        return name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name[1..];
    }
}
