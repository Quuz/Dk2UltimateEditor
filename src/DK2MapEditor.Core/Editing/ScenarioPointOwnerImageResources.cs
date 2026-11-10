using System.Globalization;

namespace DK2MapEditor.Core.Editing;

/// <summary>Action Point owner list images, distinct from TrImages scope-tree icons.</summary>
public static class ScenarioPointOwnerImageResources
{
    public static int? ForIdentity(uint scope, uint identity)
    {
        if (scope is 4) return 1501; // TrSpecial[2]: area.
        if (scope is 8) return 1212; // TrSpecial[3]: band.
        if (scope is 128 or 256) return 400; // Source helper's default case, not an object catalogue.
        if (scope == 1) return identity is >= 1 and <= 7 ? 930 + (int)identity : 400;
        if (scope == 64)
            return LegacyCompilerRules.Elements("Doors").FirstOrDefault(item => item.Variable == identity)?.Icon ?? 400;
        if (scope is 2 or 16)
            return new[] { "BadCreatures", "BadElites", "Heros" }.SelectMany(LegacyCompilerRules.Elements)
                .FirstOrDefault(item => item.ID == identity)?.Icon ?? 400;
        return null; // Level has no physical owner-selection page; unsupported scopes have no image.
    }

    public static int? For(ScriptOwner owner)
    {
        var record = owner.Record;
        if (record.File.Length < 4 || record.Offset < 0 || record.Length < 0 || record.Offset > record.File.Length - record.Length) return null;
        if (owner.Scope is 4 or 8 or 128 or 256) return ForIdentity(owner.Scope, 0);
        if (owner.Scope == 1 && owner.PlayerID != 0) return ForIdentity(1, owner.PlayerID);
        var name = owner.Scope switch
        {
            1 => "bGlobalPlayer",
            16 when record.File.Signature == NativeMapDocument.LegacyBands && owner.MemberIndex is >= 0 and < 16 => $"heros[{owner.MemberIndex}].id",
            2 or 16 => record.File.Signature == NativeMapDocument.LegacyThings ? "id" : "bGlobalCreatureID",
            64 => record.File.Signature == NativeMapDocument.LegacyThings ? "id" : "bDoorID",
            _ => null
        };
        var field = record.Fields.FirstOrDefault(field => field.Name == name);
        if (field is null || field.Offset < 0 || field.Size < 1 || field.Offset > record.Length - field.Size) return null;
        return uint.TryParse(record.Read(field), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? ForIdentity(owner.Scope, id) : null;
    }
}
