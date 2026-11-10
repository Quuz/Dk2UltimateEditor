namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private sealed record ProjectScope(uint Scope, string Target, string IdentityField);

    // Area scopes use the visible area ID. Other entity scopes use unique IDs,
    // including each nested band member's identity (not its creature type ID).
    private static IReadOnlyList<ProjectScope> ProjectScopes(NativeRecord record)
    {
        if (record.File.Signature == LegacyThings)
            return record.Read(record.Fields.Single(field => field.Name == "node.type")) switch
            {
                "0" => [new(2, "creature", "uniqueid")], "1" => [new(0x40, "door", "uniqueid")],
                "3" => [new(0x80, "object", "uniqueid")], "4" => [new(0x100, "object", "uniqueid")], _ => []
            };
        if (record.File.Signature == LegacyAreas)
            return record.Read(record.Fields.Single(field => field.Name == "node.type")) == "0" ? [new(4, "area", "id")] : [];
        if (record.File.Signature == LegacyBands)
            return [new(8, "band", "uniqueid"), .. record.Fields.Where(field => field.Name.StartsWith("heros[", StringComparison.Ordinal) && field.Name.EndsWith(".uniqueid", StringComparison.Ordinal)).Select(field => new ProjectScope(0x10, "creature", field.Name))];
        return [];
    }

    private IEnumerable<(NativeRecord Link, ProjectScope Scope)> OwnedProjectLinks(NativeRecord owner)
    {
        var scopes = ProjectScopes(owner);
        foreach (var link in Records().Where(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)))
            foreach (var scope in scopes)
            {
                var identity = owner.Fields.FirstOrDefault(field => field.Name == scope.IdentityField);
                var target = link.Fields.FirstOrDefault(field => field.Name == scope.Target);
                if (identity is not null && target is not null && owner.Read(identity) != "0" && link.Read(target) == owner.Read(identity) && link.Read(link.Fields.Single(field => field.Name == "node.type")) == scope.Scope.ToString())
                    yield return (link, scope);
            }
    }

    private HashSet<uint> UsedProjectUniqueIDs()
    {
        var used = Records().SelectMany(item => item.Fields.Where(field => field.Name.EndsWith("uniqueid", StringComparison.Ordinal)).Select(field => uint.Parse(item.Read(field)))).ToHashSet();
        // Do not accidentally acquire an orphan scope left by another editor.
        foreach (var link in Records().Where(item => item.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)))
        {
            var scope = link.Read(link.Fields.Single(field => field.Name == "node.type"));
            var name = scope switch { "2" or "16" => "creature", "64" => "door", "128" or "256" => "object", "8" => "band", _ => "" };
            if (link.Fields.FirstOrDefault(field => field.Name == name) is { } field) used.Add(uint.Parse(link.Read(field)));
        }
        return used;
    }

    private void AssignDuplicateProjectIdentities(NativeRecord record, byte[] duplicate)
    {
        var fields = record.Fields.Where(field => field.Name.EndsWith("uniqueid", StringComparison.Ordinal)).ToArray();
        var used = UsedProjectUniqueIDs();
        var oldIDs = fields.Select(record.Read).Where(id => id != "0").ToArray();
        if (oldIDs.Distinct().Count() != oldIDs.Length) throw new InvalidDataException("Cannot duplicate a project record with repeated nested unique IDs.");
        uint next = 1;
        foreach (var field in fields)
        {
            if (field.Name != "uniqueid" && record.Read(field) == "0")
            {
                var valid = record.Fields.FirstOrDefault(candidate => candidate.Name == field.Name.Replace("uniqueid", "isvalid", StringComparison.Ordinal));
                if (valid is null || record.Read(valid) == "0") continue;
            }
            while (used.Contains(next)) next = checked(next + 1);
            field.Encode(next.ToString()).CopyTo(duplicate, field.Offset); used.Add(next);
        }
    }

    private void CloneOwnedProjectLinks(NativeRecord owner, byte[] duplicate)
    {
        // Capture every link before script/link insertions shift KTRG offsets.
        var links = OwnedProjectLinks(owner).Select(pair => (pair.Link, pair.Scope,
            Data: pair.Link.File.Bytes.AsSpan(pair.Link.Offset, pair.Link.Length).ToArray())).ToArray();
        if (links.Length == 0) return;
        var roots = links.Select(item => ushort.Parse(item.Link.Fields.Single(field => field.Name == "trigger").Read(item.Data))).Where(id => id != 0);
        var scripts = CloneLegacyScripts(roots);
        foreach (var (link, scope, data) in links)
        {
            data.AsSpan(0, 8).Clear();
            var target = link.Fields.Single(field => field.Name == scope.Target);
            var identity = owner.Fields.Single(field => field.Name == scope.IdentityField);
            target.Encode(identity.Read(duplicate)).CopyTo(data, target.Offset);
            if (scope.Scope == 16 && link.Fields.FirstOrDefault(field => field.Name == "band") is { } parent)
                parent.Encode(owner.Fields.Single(field => field.Name == "id").Read(duplicate)).CopyTo(data, parent.Offset);
            var trigger = link.Fields.Single(field => field.Name == "trigger");
            var oldID = ushort.Parse(trigger.Read(data));
            if (oldID != 0) trigger.Encode(scripts[oldID].ToString()).CopyTo(data, trigger.Offset);
            InsertLegacyRecord(link.File, link.Category[8..], data);
        }
    }
}
