using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class SelectionObjectOrderChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    public static void Run()
    {
        DirectSelectionExportChecks.Run();
        EditorSelectionCallerChecks.Run();
        EditorTransformCallerChecks.Run();
        foreach(var project in new[]{false,true})
        {
            var document=project?NativeMapDocument.CreateNewMap(new("Order","Order",12,12)):NativeMapDocument.Create(12,12,"Order");
            NativeRecord Add(string kind,int id,int x,int y)
            {
                var record=project?document.AddLegacyRecord(kind,new(x,y))
                    :document.AddRecord(RecordTemplate.All.Single(template=>template.Structure==(kind switch {"Creature"=>"GAMEKEEPER","Door"=>"GAMEDOOR","Trap"=>"GAMETRAP",_=>"GAMEOBJECT"})),new(x,y));
                var field=record.Fields.Single(field=>field.Name==(project?"id":kind switch {"Creature"=>"bGlobalCreatureID","Door"=>"bDoorID","Trap"=>"bTrapID",_=>"bObjectID"}));
                document.SetField(record,field,id.ToString());return record;
            }
            Add("Object",2,5,5);Add("Creature",17,4,3);Add("MagicalObject",117,3,3);
            Add("Door",6,3,3);Add("Creature",14,3,3);Add("Trap",4,3,3);
            Add("Creature",17,3,3);Add("Object",1,3,3);Add("Creature",18,2,3);
            Add("Creature",19,2,1); // Outside the captured region.
            foreach(var owner in document.ScriptOwners().Where(owner=>owner.Scope==2).Take(2).ToArray())
            {var root=document.EnsureScriptScope(owner);document.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED"));}
            var before=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
            var revision=document.EditRevision;var dirty=document.HasChanges;var history=document.GetHistory();
            var changes=0;EventHandler handler=(_,_)=>changes++;document.Changed+=handler;
            try
            {
                var clipboard=document.CopySelection(new(2,3,4,3));
                uint[] IDs(byte[] bytes)=>LegacySectionFile.Parse(bytes).Sections.Single(section=>section.Signature==NativeMapDocument.LegacyThings)
                    .Payload.ToArray().Chunk(64).Select(record=>BinaryPrimitives.ReadUInt32LittleEndian(record.AsSpan(20))).ToArray();
                var expected=new uint[]{18,14,17,6,4,1,117,17,2};
                var bytes=SelectionFile.Serialize(clipboard);
                Expect(IDs(bytes).SequenceEqual(expected),"Selection differs from source row-major/category/list insertion order.");
                Expect(clipboard.ObjectCount==9&&clipboard.ScriptCount==(project?4:2),"Object ordering lost an owner or its attached graph.");
                var compatible=LegacySectionFile.Parse(bytes).Sections.Single(section=>section.Signature==NativeMapDocument.LegacyThings).Payload.ToArray();
                var expectedTypes=new uint[]{0,0,0,1,2,3,4,0,3};
                for(var index=0;index<9;index++)Expect(BinaryPrimitives.ReadUInt32LittleEndian(compatible.AsSpan(index*64+8))==expectedTypes[index],"Source category priority is not reflected in compatible KTGS.");
                var reopened=SelectionFile.Parse(bytes);
                Expect(SelectionFile.Serialize(reopened).AsSpan().SequenceEqual(bytes),"Serialized list order changed on reopening.");
                // Transforming an existing buffer moves coordinates and retains
                // its source list order. Original FromCopyMemory writes that list.
                foreach(var moved in new[]{reopened.Mirror(true),reopened.RotateClockwise()})
                    Expect(IDs(SelectionFile.Serialize(moved)).SequenceEqual(expected),"Buffer Save resorted transformed objects.");
                var target=project?NativeMapDocument.CreateNewMap(new("Paste","Paste",16,16)):NativeMapDocument.Create(16,16,"Paste");
                target.PasteSelection(new(3,4),reopened);
                Expect(ScriptGraph.Build(target).Events.Count==2,"Ordered selection lost scripts during paste.");
                Expect(document.EditRevision==revision&&document.HasChanges==dirty&&changes==0
                    &&document.GetHistory().Undo.Count==history.Undo.Count&&document.GetHistory().Redo.Count==history.Redo.Count
                    &&before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Ordering changed source bytes/history/notifications.");

                // Original Copy clones every same-category list member, including
                // imported duplicate objects. Never silently discard these bytes.
                Add("Object",3,3,3);
                var duplicates=SelectionFile.Parse(SelectionFile.Serialize(document.CopySelection(new(2,3,4,3))));
                Expect(duplicates.ObjectCount==10&&IDs(SelectionFile.Serialize(duplicates)).SequenceEqual(new uint[]{18,14,17,6,4,1,3,117,17,2}),
                    "Copy dropped or reordered duplicate noncreature members.");
            }
            finally{document.Changed-=handler;}
        }
    }
}
