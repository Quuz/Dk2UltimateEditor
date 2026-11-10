namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private static bool SameRecord(NativeRecord left, NativeRecord right) => left.File == right.File && left.Offset == right.Offset && left.Length == right.Length;
    private static bool SameField(NativeRecord owner, NativeField field, NativeRecord other, NativeField candidate) => SameRecord(owner, other) && field.Offset == candidate.Offset && field.Name == candidate.Name;

    private void RequireKnownEntityReferences(IReadOnlyList<NativeRecord> records)
    {
        if (records.Any(record => record.Category == "Project.Opaque" && record.File.Signature is LegacyThings or LegacyAreas or LegacyBands or LegacyEvents
            || record.File.Signature == 0xD2 && EntityReferences.EventType(record) is null
            || record.Category is "Project.TRIGGER" or "Project.ACTION" && EntityReferences.EventType(record) is null
            || record.File.Signature == 0xBE && (record.Category.StartsWith("Record ", StringComparison.Ordinal) || record.Category == "GAMEBAND" && record.Length != 563)))
            throw new InvalidOperationException("An unverified entity/script layout is present; its references cannot safely be remapped.");
    }

    private static bool LiveReference(EntityReference reference)
    {
        if (reference.Field.Name == "bHeroBandID" && reference.Record.Category.StartsWith("BandHero", StringComparison.Ordinal))
            return reference.Record.Read(reference.Record.Fields.Single(field => field.Name == "bGlobalCreatureID")) != "0"
                && reference.Record.Read(reference.Record.Fields.Single(field => field.Name == "bGlobalBehavior")) != "0";
        return !EntityReferences.IsNone(reference, reference.Record.Read(reference.Field));
    }

    private void RemapEntityIdentity(NativeRecord owner, NativeField field, byte[] encoded)
    {
        var identity = EntityReferences.Identity(owner);
        if (identity is null || identity.Field != field) return;
        var oldID = owner.Read(field); var newID = (field with { Offset = 0 }).Read(encoded);
        if (oldID == newID) return;
        var number = uint.Parse(newID);
        if (number > 255 || number == 0 && identity.Kind != EntityKind.Band)
            throw new ArgumentException("Entity IDs must be 1–255 (band IDs may also be zero).");
        var records = Records(); RequireKnownEntityReferences(records);
        var identities = records.Select(EntityReferences.Identity).OfType<EntityIdentity>().Where(item => item.Kind == identity.Kind).ToArray();
        if (identities.Any(item => !SameRecord(owner, item.Record) && item.Record.Read(item.Field) == newID))
            throw new ArgumentException("This entity ID is already in use in the same category.");
        var incoming = records.SelectMany(EntityReferences.From).Where(reference => reference.Kind == identity.Kind && LiveReference(reference) && reference.Record.Read(reference.Field) == oldID).ToArray();
        if (identities.Any(item => !SameRecord(owner, item.Record) && item.Record.Read(item.Field) == oldID) && incoming.Length != 0)
            throw new InvalidOperationException("The old entity ID is ambiguous. Detach its references before repairing duplicate identities.");
        foreach (var reference in incoming) reference.Field.Encode(newID).CopyTo(reference.Record.File.Bytes, reference.Record.Offset + reference.Field.Offset);
        // Inactive native slots still carry their physical parent band ID in
        // factory maps. Only this owner's slots are updated, not other empty slots.
        if (owner.Category == "GAMEBAND" && owner.Length == 563)
            foreach (var member in records.Where(record => record.File == owner.File && record.Category.StartsWith("BandHero", StringComparison.Ordinal) && record.Offset >= owner.Offset + 51 && record.Offset < owner.Offset + 563))
                if (member.Fields.FirstOrDefault(field => field.Name == "bHeroBandID") is { } parent) parent.Encode(newID).CopyTo(member.File.Bytes, member.Offset + parent.Offset);
    }

    private void RemapProjectUniqueIdentity(NativeRecord owner, NativeField field, byte[] encoded)
    {
        if (!IsLegacyProject || !field.Name.EndsWith("uniqueid", StringComparison.Ordinal)) return;
        var oldID = owner.Read(field); var newID = (field with { Offset = 0 }).Read(encoded);
        if (oldID == newID) return;
        var number = uint.Parse(newID); var records = Records(); RequireKnownEntityReferences(records);
        var links = OwnedProjectLinks(owner).Where(pair => pair.Scope.IdentityField == field.Name).ToArray();
        if (number == 0 && links.Length != 0) throw new ArgumentException("Detach this entity's scenario links before clearing its unique identity.");
        if (number != 0 && UsedProjectUniqueIDs().Contains(number)) throw new ArgumentException("This unique identity is already used by an entity or a scenario scope.");
        if (oldID != "0" && links.Length != 0 && records.Any(record => record.Fields.Any(candidate => candidate.Name.EndsWith("uniqueid", StringComparison.Ordinal) && !SameField(owner, field, record, candidate) && record.Read(candidate) == oldID)))
            throw new InvalidOperationException("The old unique identity is shared. Detach ambiguous scenario scopes before repairing it.");
        foreach (var (link, scope) in links)
        {
            var target = link.Fields.Single(field => field.Name == scope.Target);
            target.Encode(newID).CopyTo(link.File.Bytes, link.Offset + target.Offset);
        }
    }

    private void RequireNoIncomingEntityReferences(NativeRecord owner, bool editorAreaRemoval = false)
    {
        if (EntityReferences.Identity(owner) is not { } identity) return;
        var records = Records(); RequireKnownEntityReferences(records);
        if (editorAreaRemoval && (IsVerifiedRegionRemovalShape(owner)
            || EditorResizeThing(owner) && ObjectManagerEntries().Any(entry => SameRecord(entry.Record, owner) && entry.CanRemove))) return;
        // RectEx_RemoveRects / Triggers_IsUsed(SCOPE_GATE) retain ACTION.param5
        // when deleting a gate. The freed ID may later be reused by new gates.
        // Keep the guards for other entity kinds and unverified layouts.
        if (IsLegacyProject && owner.Category == "Project.MAPRECTEX" && identity.Kind == EntityKind.Gate) return;
        var id = owner.Read(identity.Field);
        var ownedLinks = IsLegacyProject ? OwnedProjectLinks(owner).Select(pair => (pair.Link.File, pair.Link.Offset)).ToHashSet() : [];
        var incoming = records.SelectMany(EntityReferences.From).FirstOrDefault(reference => reference.Kind == identity.Kind && LiveReference(reference) && reference.Record.Read(reference.Field) == id
            && !(reference.Record.File == owner.File && reference.Record.Offset >= owner.Offset && reference.Record.Offset < owner.Offset + owner.Length)
            && !ownedLinks.Contains((reference.Record.File, reference.Record.Offset)));
        if (incoming is not null) throw new InvalidOperationException($"This {identity.Kind} is still referenced by {incoming.Record.Category}.{incoming.Field.Name}. Redirect its references before deleting it.");
    }

}
