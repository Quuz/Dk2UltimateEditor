namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    // Official maps concatenate the definition families in one Globals.kld.
    // Keep the editable working families separate; only publication packs them.
    private IReadOnlyList<byte[]>? officialDefinitionExtras;
    private static readonly uint[] OfficialDefinitionOrder = [0x6E, 0x78, 0xAA, 0x8C, 0x82, 0xF0, 0xA0, 0x96, 0x104];

    private IReadOnlyList<byte[]> CaptureOfficialDefinitionExtras(NativeMapDocument basis)
    {
        var source = nativePayloadSource is { } archive ? ArchivedNativeDocument(archive) : basis;
        return new uint[] { 0x10E, 0xFA }
            .Select(signature => CopyNativeDefinition(source, signature))
            .Where(section => section is not null).Select(section => section!.Value.Bytes).ToArray();
    }

    private IReadOnlyList<(string path, byte[] data)> OfficialExportFiles()
    {
        var stem = Path.GetFileNameWithoutExtension(MainFile.ExportPath);
        var definitions = OfficialDefinitionOrder.Select(signature =>
            files.SingleOrDefault(file => file.Signature == signature)
            ?? throw new InvalidDataException("Official map export requires definition family 0x" + signature.ToString("X"))).ToArray();
        var globalsPath = Path.Combine("Data", "Editor", "Maps", stem + "Globals.kld");
        var globals = definitions.Select(file => file.Data.ToArray()).Concat(officialDefinitionExtras!);
        using var packed = new MemoryStream();
        foreach (var section in globals)
        {
            if (section.Length < 56 || System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(section.AsSpan(8)) != section.Length)
                throw new InvalidDataException("A definition has opaque bytes outside its native section; official packing cannot safely relocate them.");
            packed.Write(section);
        }
        var output = new List<(string path, byte[] data)> { (globalsPath, packed.ToArray()) };
        var links = new List<(uint type, string path)> { (0, globalsPath) };
        foreach (var signature in new uint[] { 0xB4, 0x64, 0xD2 })
        {
            var file = files.Single(file => file.Signature == signature);
            output.Add((file.ExportPath, file.Data.ToArray())); links.Add((signature, file.ExportPath));
        }
        // The official seven-file map uses the installation's shared global
        // variable catalogue. SaveCopy includes it outside Maps for reopening;
        // game publication validates it and never copies it into the map set.
        foreach (var file in files.Where(file => file.Signature == 0xE6))
        { output.Add((file.ExportPath, file.Data.ToArray())); links.Add((0xE6, file.ExportPath)); }
        var things = files.Single(file => file.Signature == 0xBE);
        output.Add((things.ExportPath, things.Data.ToArray())); links.Add((0xBE, things.ExportPath));
        var start = checked(56 + (int)MainFile.UInt32(52) + 8);
        var oldCount = (int)(MainFile.UInt32(20) & 0xFFFF);
        var end = checked(start + oldCount * 72);
        if (end > MainFile.Length) throw new InvalidDataException("Truncated compiled reference table.");
        var descriptor = new byte[checked(MainFile.Length + (links.Count - oldCount) * 72)];
        MainFile.Data.Span[..start].CopyTo(descriptor);
        MainFile.Data.Span[end..].CopyTo(descriptor.AsSpan(start + links.Count * 72));
        WriteUInt32(descriptor, 8, (uint)descriptor.Length);
        WriteUInt32(descriptor, 20, (MainFile.UInt32(20) & 0xFFFF0000) | (uint)links.Count);
        WriteUInt32(descriptor, start - 4, checked(MainFile.UInt32(start - 4) + (uint)(links.Count * 72) - (uint)(oldCount * 72)));
        for (var index = 0; index < links.Count; index++)
        {
            WriteUInt32(descriptor, start + index * 72, links[index].type);
            NativeReference.Encode(links[index].path.Replace(Path.DirectorySeparatorChar, '\\'))
                .CopyTo(descriptor, start + index * 72 + 8);
        }
        output.Add((MainFile.ExportPath, descriptor));
        if (ScenarioNamesExport() is { } names)
            output.Add((Path.Combine("Data", "Editor", "EditorMetadata", Path.GetFileName(MainFile.ExportPath) + ScenarioNamesExtension), names.data));
        return output;
    }
}
