using DK2MapEditor.Core.Editing;

internal static class CreatureSpellUsageChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    private static void Reject(Action action){try{action();}catch(ArgumentException){return;}throw new Exception("Invalid usage-list row accepted.");}
    public static void Run()
    {
        var directory=Path.Combine(Path.GetTempPath(),"CreatureSpellUsage-"+Guid.NewGuid().ToString("N"));
        try
        {
            var native=GlobalDefinitionNumericChecks.Fixture(directory);var project=NativeMapDocument.CreateNewMap(new("SpellUsage","SpellUsage",20,20));
            var nativeRows=native.GlobalDefinitionRows();var spells=nativeRows.Where(r=>r.Family=="CrSpells").ToArray();Expect(spells.Length==41,"Source spell catalogue incomplete.");
            var expected=spells.ToDictionary(s=>s.ID,s=>nativeRows.Where(r=>r.Family=="Creatures"&&r.Record is { } record
                &&new[]{0xDDA,0xDF2,0xE0A}.Any(offset=>record.File.Data.Span[record.Offset+offset]==s.ID)).Select(r=>r.Index).ToArray());
            foreach(var doc in new[]{native,project})
            {
                var initial=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var revision=doc.EditRevision;var count=doc.EditorChangeCount;var undo=doc.GetHistory().Undo.Count;
                void Exact()=>Expect(initial.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditRevision==revision&&doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==undo,"Read-only usage list changed bytes/revision/history.");
                foreach(var spell in doc.GlobalDefinitionRows().Where(r=>r.Family=="CrSpells"))
                {
                    var result=doc.ReadCreatureSpellUsage(spell);
                    Expect(result.Complete&&result.Users.Select(r=>r.Index).SequenceEqual(expected[spell.ID]),"Source OR across three packed attack IDs/order differs: "+spell.ID);Exact();
                }
                Reject(()=>doc.ReadCreatureSpellUsage(doc.GlobalDefinitionRows().First(r=>r.Family=="Creatures")));Exact();
                doc.SetGlobalDefinitionValues(doc.GlobalDefinitionRows().Where(r=>r.Family=="Creatures").SelectMany(r=>new[]{"Shot1ID","Shot2ID","Shot3ID"}.Select(f=>new GlobalDefinitionChange("Creatures",r.Index,f,"0"))));
                var cleared=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
                var selected=doc.GlobalDefinitionRows().Single(r=>r.Family=="CrSpells"&&r.Index==3);var spellID=selected.ID.ToString();
                var unused=doc.ReadCreatureSpellUsage(selected);Expect(unused.Complete&&unused.Users.Count==0,"Empty source usage list inferred users from defaults.");
                using(var session=doc.CreateCreatureGlobalsSession())
                {
                    var working=session.WorkingDocument;
                    working.SetGlobalDefinitionValues([new("Creatures",0,"Shot1ID",spellID),new("Creatures",0,"Shot2ID",spellID),new("Creatures",0,"Shot3ID",spellID),
                        new("Creatures",20,"Shot2ID",spellID),new("Creatures",44,"Shot3ID",spellID)]);
                    var usage=working.ReadCreatureSpellUsage(selected);
                    Expect(usage.Complete&&usage.Users.Select(r=>r.Index).SequenceEqual(new[]{0,20,44})&&usage.Users.Select(r=>r.ID).Distinct().Count()==3,"Usage list duplicates multi-slot user, confuses IDs/indexes or reorders45 source rows.");
                    Expect(doc.ReadCreatureSpellUsage(selected).Users.Count==0&&cleared.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Private usage leaked into live document.");
                    working.SetGlobalDefinitionValues([new("Creatures",20,"Shot2ID","0")]);
                    Expect(usage.Users.Select(r=>r.Index).SequenceEqual(new[]{0,20,44})&&working.ReadCreatureSpellUsage(selected).Users.Select(r=>r.Index).SequenceEqual(new[]{0,44}),"Usage snapshot changed after owner edit or fresh lookup remained stale.");
                }
                Expect(cleared.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Cancel of private attack list changed live state.");
            }
            var missing=NativeMapDocument.Create(20,20,"MissingUsage");var row=missing.GlobalDefinitionRows().First(r=>r.Family=="CrSpells");
            var absent=missing.ReadCreatureSpellUsage(row);Expect(!absent.Complete&&absent.Users.Count==0,"Missing layouts invented default spell users.");
        }
        finally
        {
            if(Directory.Exists(directory))
            {
                var full=Path.GetFullPath(directory);Expect(full.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(full).StartsWith("CreatureSpellUsage-",StringComparison.Ordinal),"Unexpected cleanup target.");Directory.Delete(full,true);
            }
        }
        Console.WriteLine("CREATURE SPELL USAGE DETAIL41 spells x45 source creature rows in native/default-DKM; independent packed0xDDA/0xDF2/0xE0A OR, source order/one row per user, empty/private/snapshot/missing guards; reads preserve bytes/revision/history; no UI/game");
    }
}
