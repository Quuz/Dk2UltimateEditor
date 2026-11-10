using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;

// Independent native wire oracle. A packed Globals.kld holds multiple complete
// files; a new map's standalone reference must contain the selected section,
// not the entire packed container. Standalone opaque tails remain exact.
internal static class NativeDefinitionReuseChecks
{
    private static readonly uint[] required=[0x6E,0x78,0xAA,0x8C,0x82,0xA0,0x96,0xF0,0x104];
    private static readonly uint[] known=[..required,0x10E,0xFA];
    private static uint U(ReadOnlySpan<byte> bytes,int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
    internal static void Verify(NativeMapDocument source,NativeMapDocument constructed)
    {
        var expected=new Dictionary<uint,byte[]>();
        foreach(var file in source.Files.Reverse())
        {
            var bytes=file.Data.Span;
            if(bytes.Length<56 || !known.Contains(U(bytes,0)))continue;
            var firstLength=U(bytes,8);
            var packed=firstLength>=56 && firstLength<=bytes.Length-56 && known.Contains(U(bytes,(int)firstLength)) && U(bytes,(int)firstLength+4)==4;
            if(!packed)
            {
                expected.TryAdd(U(bytes,0),bytes.ToArray());continue;
            }
            for(var offset=0;offset<bytes.Length;)
            {
                if(offset>bytes.Length-56 || !known.Contains(U(bytes,offset)) || U(bytes,offset+4)!=4)throw new InvalidDataException("Unverified packed source definition header.");
                var length=U(bytes,offset+8);
                if(length<56 || length>bytes.Length-offset)throw new InvalidDataException("Invalid packed source definition length.");
                expected.TryAdd(U(bytes,offset),bytes.Slice(offset,(int)length).ToArray());
                offset+=checked((int)length);
            }
        }
        foreach(var signature in required.Where(expected.ContainsKey))
        {
            var output=constructed.Files.Where(file=>file.Signature==signature).ToArray();
            if(output.Length!=1 || !output[0].Data.Span.SequenceEqual(expected[signature]))
                throw new InvalidDataException($"New-map definition 0x{signature:X} differs from its complete source section (including opaque bytes).");
        }
    }
}
