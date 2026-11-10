using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class SelectionTerrainProjectionChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    public static void Run()
    {
        // Source GameMapToEditorMap normalizes owner/variant and searches
        // World, Rooms, Gates, Walls in that order. EditorMapToGameMap emits
        // reserved1 and maps locked254 to30. Literal vectors are independent.
        var cases=new (MapTile Raw,MapTile Expected)[] {
            (new(254,2,1,77),new(30,2,1)),(new(4,7,1,99),new(4,2,1)),
            (new(35,7,1,99),new(35,1,1)),(new(31,7,1,99),new(31,2,1)),
            (new(11,7,1,99),new(11,7,1)),(new(11,0,1,99),new(11,2,1)),
            (new(17,3,2,99),new(17,3,2)),(new(17,3,9,99),new(17,3,1)),
            (new(2,2,2,99),new(2,2,1)),(new(250,3,1,99),new(2,2,1))
        };
        foreach(var project in new[]{false,true})
        {
            var document=project?NativeMapDocument.CreateNewMap(new("Projection","Projection",16,16))
                :NativeMapDocument.Create(16,16,"Projection");
            for(var index=0;index<cases.Length;index++)document.Paint([new(index+1,1)],cases[index].Raw);
            var before=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
            var revision=document.EditRevision;var history=document.GetHistory();
            var clipboard=document.CopySelection(new(1,1,cases.Length,1));
            var data=SelectionFile.Serialize(clipboard);var container=LegacySectionFile.Parse(data);
            var baseTiles=container.ReadTiles();
            for(var index=0;index<cases.Length;index++)Expect(baseTiles.Tiles[index]==cases[index].Expected,
                $"Compatible KMAP differs from source at{index}: {baseTiles.Tiles[index]} expected {cases[index].Expected}.");
            var reopened=SelectionFile.Parse(data);
            Expect(reopened.Tiles.Tiles.SequenceEqual(cases.Select(item=>item.Raw)),"Raw cells lost while creating source-compatible KMAP.");
            Expect(SelectionFile.Serialize(reopened).AsSpan().SequenceEqual(data),"Terrain projection is not byte stable.");
            var kmap=container.Sections.Single(section=>section.Signature==LegacySectionFile.TileSectionSignature);
            byte[] Envelope(IEnumerable<LegacySection> sections)
            {var result=LegacySectionFile.Create(sections).ToArray();BinaryPrimitives.WriteUInt32LittleEndian(result,LegacySectionFile.AreaFileSignature);return result;}
            var changed=kmap.Payload.ToArray();changed[8]^=1;
            try{SelectionFile.Parse(Envelope(container.Sections.Select(section=>section==kmap?new(section.Signature,section.Version,changed):section)));throw new InvalidOperationException("Inconsistent KMAP projection accepted.");}
            catch(InvalidDataException){}
            var extension=container.Sections.Single(section=>section.Signature==SelectionFile.ClipboardSection);
            var oldKmap=kmap.Payload.ToArray();
            for(var index=0;index<cases.Length;index++)BinaryPrimitives.WriteUInt32LittleEndian(oldKmap.AsSpan(8+index*4),cases[index].Raw.Packed);
            foreach(var version in new[]{1,2,3,4})
            {
                var length=extension.Payload.Length-cases.Length*4;
                if(version<=2)length-=cases.Length; // No protection mask in DKCP1/2; no entities in this fixture.
                var old=Envelope(container.Sections.Select(section=>section==extension?new(section.Signature,version,section.Payload[..length])
                    :section==kmap?new(section.Signature,section.Version,oldKmap):section));
                Expect(SelectionFile.Serialize(SelectionFile.Parse(old)).AsSpan().SequenceEqual(data),$"DKCP{version} raw cells failed exact upgrade to new projection.");
            }
            var tail=extension.Payload.ToArray();tail[^1]^=1;
            // Reserved byte may vary in a valid raw snapshot without changing its
            // projection. Compare every byte on reopen, not a made-up checksum.
            var edited=SelectionFile.Parse(Envelope(container.Sections.Select(section=>section==extension?new(section.Signature,section.Version,tail):section)));
            Expect(edited.Tiles.Tiles[^1].Reserved==(byte)(cases[^1].Raw.Reserved^1),"Raw reserved byte was silently normalized.");
            try{SelectionFile.Parse(Envelope(container.Sections.Select(section=>section==extension?new(section.Signature,section.Version,extension.Payload[..^1]):section)));throw new InvalidOperationException("Truncated raw tile vector accepted.");}
            catch(InvalidDataException){}
            Expect(document.EditRevision==revision&&document.GetHistory().Undo.Count==history.Undo.Count
                &&document.GetHistory().Redo.Count==history.Redo.Count&&before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Terrain export changed source/history.");
        }
    }
}
