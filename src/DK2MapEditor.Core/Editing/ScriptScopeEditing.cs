namespace DK2MapEditor.Core.Editing;

/// <summary>A physical owner, not an event argument or an arbitrary numeric ID.
/// Project members belong to a band record; native members have their own slot record.</summary>
public sealed record ScriptOwner(uint Scope, NativeRecord Record, int MemberIndex = -1, uint PlayerID = 0, NativeRecord? BandRecord = null);
public sealed record ScriptBinding(ScriptEdge Root, ScriptOwner? Owner);

public sealed partial class NativeMapDocument
{
    public IReadOnlyList<ScriptOwner> ScriptOwners()
    {
        if (!IsLegacyProject)
        {
            var slots = Records().Where(record => record.Category == "GAMEBAND" && record.Length == 563)
                .SelectMany(band => Enumerable.Range(0, 16).Select(index => (Key: (band.File, Offset: band.Offset + 51 + index * 32), Band: band, Index: index)))
                .ToDictionary(slot => slot.Key, slot => (slot.Band, slot.Index));
            return ScriptGraph.Build(this).Roots.Where(root => !root.Owner.Category.StartsWith("Working.", StringComparison.Ordinal) && ScriptGraph.IsKnownScope(root.Scope)
                && (root.Scope != 16 || root.Owner.Read(root.Owner.Fields.Single(field => field.Name == "bGlobalCreatureID")) != "0"
                    && root.Owner.Read(root.Owner.Fields.Single(field => field.Name == "bGlobalBehavior")) != "0"))
                .Select(root => root.Scope == 16 && slots.TryGetValue((root.Owner.File, root.Owner.Offset), out var slot)
                    ? new ScriptOwner(root.Scope, root.Owner, slot.Index, BandRecord: slot.Band) : new ScriptOwner(root.Scope, root.Owner)).ToArray();
        }
        var owners = new List<ScriptOwner>(); var player = 0u;
        foreach (var record in Records().Where(record => record.Fields.Count != 0))
        {
            if (record.File.Signature == LegacyInfo) owners.Add(new(32, record));
            else if (record.File.Signature == LegacyPlayers) owners.Add(new(1, record, PlayerID: ++player));
            else if (IsBand(record))
            {
                if (record.Fields.Any(field => field.Name == "uniqueid")) owners.Add(new(8, record));
                foreach (var member in BandMembers(record).Where(member => member.Active))
                    if (record.Fields.Any(field => field.Name == $"heros[{member.Index}].uniqueid")) owners.Add(new(16, record, member.Index));
            }
            else
                foreach (var scope in ProjectScopes(record))
                    if (record.Fields.Any(field => field.Name == scope.IdentityField)) owners.Add(new(scope.Scope, record));
        }
        return owners;
    }

    private ScriptOwner CurrentScriptOwner(ScriptOwner owner) => ScriptOwners().SingleOrDefault(current => current.Scope == owner.Scope
        && current.MemberIndex == owner.MemberIndex && current.PlayerID == owner.PlayerID && current.Record.Category == owner.Record.Category && SameRecord(current.Record, owner.Record))
        ?? throw new ArgumentException("Select a current, verified scenario owner (band members must be active).");

    private static string ScopeTarget(uint scope) => scope switch
    { 1 => "player", 2 or 16 => "creature", 4 => "area", 8 => "band", 64 => "door", 128 or 256 => "object", 32 => "", _ => throw new ArgumentException("Unverified scenario scope.") };
    private static string ScopeIdentity(ScriptOwner owner) => owner.Scope switch
    { 4 => "id", 16 => $"heros[{owner.MemberIndex}].uniqueid", 1 or 32 => "", _ => "uniqueid" };

    private string ProjectOwnerID(ScriptOwner owner)
    {
        if (owner.Scope == 32) return "0";
        if (owner.Scope == 1) return owner.PlayerID.ToString();
        var field = owner.Record.Fields.Single(field => field.Name == ScopeIdentity(owner));
        var id = owner.Record.Read(field);
        if (owner.Scope == 4 || id != "0")
        {
            // Scope selects the source owner list. A door and a creature with
            // the same numeric ID are distinct; inactive band tail slots are
            // retained bytes, not owners offered by ScriptOwners.
            var matches = ScriptOwners().Count(candidate => candidate.Scope == owner.Scope
                && candidate.Record.Read(candidate.Record.Fields.Single(field => field.Name == ScopeIdentity(candidate))) == id);
            if (matches != 1) throw new InvalidOperationException("The selected owner's identity is shared within its scope; repair it before binding a scenario.");
        }
        return id;
    }

    /// <summary>Looks up one actual root. Zero unique identities never acquire orphan zero bindings.</summary>
    public ScriptEdge? FindScriptScope(ScriptOwner requested)
    {
        var owner = CurrentScriptOwner(requested); var graph = ScriptGraph.Build(this);
        if (!IsLegacyProject) return graph.Roots.Single(root => root.Scope == owner.Scope && SameRecord(root.Owner, owner.Record));
        var id = ProjectOwnerID(owner);
        if (owner.Scope is not (1 or 4 or 32) && id == "0") return null;
        var roots = graph.Roots.Where(root => root.Scope == owner.Scope && (owner.Scope == 32
            || root.Owner.Fields.FirstOrDefault(field => field.Name == ScopeTarget(owner.Scope)) is { } target && root.Owner.Read(target) == id)).ToArray();
        if (roots.Length > 1) throw new InvalidOperationException("The selected owner has multiple scenario bindings; repair duplicates before automatic editing.");
        return roots.SingleOrDefault();
    }

    /// <summary>Resolves the physical owner for display/diagnostics, without allocating IDs
    /// or choosing arbitrarily among ambiguous identities.</summary>
    public ScriptOwner? OwnerForScriptScope(ScriptEdge requested)
    {
        var root = CurrentScopeRoot(ScriptGraph.Build(this), requested);
        return ScriptBindings().Single(binding => SameField(binding.Root.Owner, binding.Root.Field, root.Owner, root.Field)).Owner;
    }

    /// <summary>Batch owner resolution for large trees; build indexes once, not once per root.</summary>
    public IReadOnlyList<ScriptBinding> ScriptBindings()
    {
        var roots = ScriptGraph.Build(this).Roots; var owners = ScriptOwners();
        if (!IsLegacyProject)
        {
            var physical = owners.ToDictionary(owner => (owner.Scope, owner.Record.File, owner.Record.Offset));
            return roots.Select(root => new ScriptBinding(root, physical.GetValueOrDefault((root.Scope, root.Owner.File, root.Owner.Offset)))).ToArray();
        }
        string ID(ScriptOwner owner) => owner.Scope == 32 ? "0" : owner.Scope == 1 ? owner.PlayerID.ToString()
            : owner.Record.Read(owner.Record.Fields.Single(field => field.Name == ScopeIdentity(owner)));
        var index = owners.Where(owner => owner.Scope is 4 or 32 || ID(owner) != "0").GroupBy(owner => (owner.Scope, Value: ID(owner)))
            .ToDictionary(group => group.Key, group => group.Count() == 1 ? group.First() : null);
        return roots.Select(root =>
        {
            var id = root.Scope == 32 ? "0" : ScriptGraph.IsKnownScope(root.Scope)
                && root.Owner.Fields.FirstOrDefault(field => field.Name == ScopeTarget(root.Scope)) is { } field ? root.Owner.Read(field) : "0";
            return new ScriptBinding(root, index.GetValueOrDefault((root.Scope, id)));
        }).ToArray();
    }

    private void WriteProjectScopeOwner(NativeRecord link, ScriptOwner owner)
    {
        var target = ScopeTarget(owner.Scope);
        if (target.Length != 0 && !link.Fields.Any(field => field.Name == target))
            throw new InvalidOperationException("Upgrade the historical scenario section before binding this owner type.");
        var id = ProjectOwnerID(owner);
        if (owner.Scope is not (1 or 4 or 32) && id == "0")
        {
            id = NextLegacyUniqueID().ToString();
            SetField(owner.Record, owner.Record.Fields.Single(field => field.Name == ScopeIdentity(owner)), id);
        }
        SetField(link, link.Fields.Single(field => field.Name == "node.type"), owner.Scope.ToString());
        foreach (var field in link.Fields.Where(field => field.Name is "player" or "area" or "creature" or "door" or "object" or "band"))
        {
            var value = field.Name == target ? id : "0";
            if (owner.Scope == 16 && field.Name == "band") value = owner.Record.Read(owner.Record.Fields.Single(field => field.Name == "id"));
            SetField(link, field, value);
        }
    }

    /// <summary>Creates a project binding or returns the existing root. Native roots already exist.
    /// Identity allocation and the new link are one undoable operation.</summary>
    public ScriptEdge EnsureScriptScope(ScriptOwner requested)
    {
        var owner = CurrentScriptOwner(requested);
        var existing = FindScriptScope(owner);
        if (existing is not null)
        {
            if (!IsLegacyProject && owner.Scope == 4) Edit("Create scenario binding", () => EnsureNativeAreaScope(owner.Record));
            else if (!IsLegacyProject && UniqueOwnerScope(owner.Scope)) Edit("Create scenario binding", () => EnsureNativeOwnerScope(owner.Record));
            return existing;
        }
        Edit("Create scenario binding", () =>
        {
            RequireUnambiguousGraph(ScriptGraph.Build(this));
            if (!LegacyAddChoices().Contains("TriggerLink")) throw new InvalidOperationException("Upgrade the historical scenario section before creating an owner binding.");
            var link = AddLegacyRecord("TriggerLink", new(0, 0));
            WriteProjectScopeOwner(link, owner);
        });
        return FindScriptScope(owner) ?? throw new InvalidDataException("Created scenario binding was not found.");
    }

    public ScriptEdge BindScriptScope(ScriptOwner owner, ushort target)
    {
        Edit("Bind owner scenario", () =>
        {
            var root = EnsureScriptScope(owner); SetScriptLink(root.Owner, root.Field, target);
        });
        return FindScriptScope(owner)!;
    }

    private static ScriptEdge CurrentScopeRoot(ScriptGraph graph, ScriptEdge requested) => graph.Roots.SingleOrDefault(root =>
        SameField(root.Owner, root.Field, requested.Owner, requested.Field)) ?? throw new ArgumentException("Select a current scenario root, not an event's outgoing link.");

    /// <summary>Changes an owner within the same scope, retaining the entire graph.
    /// An existing destination binding is never overwritten.</summary>
    public ScriptEdge ReassignScriptScope(ScriptEdge requested, ScriptOwner destination)
    {
        var owner = CurrentScriptOwner(destination);
        var root = CurrentScopeRoot(ScriptGraph.Build(this), requested);
        if (root.Scope != owner.Scope) throw new ArgumentException("A scenario binding can only move to an owner of the same scope.");
        var target = FindScriptScope(owner);
        if (target is not null && SameField(root.Owner, root.Field, target.Owner, target.Field)) return root;
        if (target is not null && HasStoredScriptScope(target)) throw new InvalidOperationException("The destination owner already has a scenario binding.");
        Edit("Change scenario owner", () =>
        {
            if (IsLegacyProject)
            {
                SetScriptLink(root.Owner, root.Field, root.Target); // Validate the retained branch before moving it.
                WriteProjectScopeOwner(root.Owner, owner);
            }
            else
            {
                SetScriptLink(target!.Owner, target.Field, root.Target);
                if (target.Owner.Category == "GAMEAREA") EnsureNativeAreaScope(target.Owner);
                else if (UniqueOwnerScope(target.Scope)) EnsureNativeOwnerScope(target.Owner);
                SetScriptLink(root.Owner, root.Field, 0);
                RemoveNativeAreaScope(root);
                RemoveNativeOwnerScope(root);
            }
        });
        return FindScriptScope(owner)!;
    }

    /// <summary>Removes only the owner binding. Events and entity identities remain available.</summary>
    public void RemoveScriptScope(ScriptEdge requested) => Edit("Remove scenario binding", () =>
    {
        var graph = ScriptGraph.Build(this); RequireUnambiguousGraph(graph);
        var root = CurrentScopeRoot(graph, requested);
        if (!IsLegacyProject) { SetScriptLink(root.Owner, root.Field, 0); RemoveNativeAreaScope(root); RemoveNativeOwnerScope(root); return; }
        DeleteLegacyRecord(root.Owner); RefreshProjectBacklinks(root.Target);
    });
}
