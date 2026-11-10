using System.Buffers.Binary;

namespace DK2MapEditor.Core.Editing;

public sealed class NativeFile
{
    internal NativeFile(string sourcePath, string exportPath, byte[] data)
    {
        SourcePath = sourcePath;
        ExportPath = exportPath;
        Bytes = data;
    }
    public string SourcePath { get; }
    public string ExportPath { get; }
    public int Length => Bytes.Length;
    public uint Signature => BinaryPrimitives.ReadUInt32LittleEndian(Bytes);
    public ReadOnlyMemory<byte> Data => Bytes;
    internal byte[] Bytes { get; set; }
    internal uint UInt32(int offset) => BinaryPrimitives.ReadUInt32LittleEndian(Bytes.AsSpan(offset));
}

public sealed record NativeRecord(NativeFile File, int Offset, int Length, string Category, IReadOnlyList<NativeField> Fields)
{
    public string Read(NativeField field) => field.Read(File.Bytes.AsSpan(Offset, Length));
}
