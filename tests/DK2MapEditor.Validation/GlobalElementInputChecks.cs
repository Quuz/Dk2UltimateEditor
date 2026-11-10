using DK2MapEditor.Core.Editing;

internal static class GlobalElementInputChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    private static void Reject(Action action){try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException){return;}throw new Exception("Invalid source element input accepted.");}
    public static void Run()
    {
        var rules=new List<(string Family,string Field,int Min,int Max,bool DecimalClamp)>();
        void Add(string family,int max,params string[] fields){foreach(var field in fields)rules.Add((family,field,0,max,false));}
        rules.Add(("Terrains","Damage",-32768,32767,false));Add("Terrains",65535,"GoldValue","StartingHealth","MaxHealth","ManaGain","MaxManaGain");
        Add("Terrains",255,"WibbleH","WibbleV");rules.Add(("Terrains","TerrainLightH",0,16000,true));
        Add("Rooms",65535,"Cost");Add("Rooms",255,"TorchIntensity");Add("Rooms",8,"TorchLightH");Add("Rooms",3,"TorchLightRad");
        Add("Doors",65535,"Cost","BuildPoints","Health","HealthGain","ManaCost");
        Add("Traps",16000,"IdleEffectDelay","ChargeTime","RechargeTime","ThreatDuration");Add("Traps",65535,"GoldCost","BuildPoints","Health","HealthGain","ManaUsage","ManaCostToFire","Threat");
        Add("Spells",999999,"ManaCost");Add("Spells",32767,"ManaDrain","RechargeTime","UpgRechargeTime");
        Expect(rules.Count==33,"Literal source numeric table incomplete.");
        foreach(var group in rules.GroupBy(r=>r.Family))Expect(group.Select(r=>r.Field).Order().SequenceEqual(GlobalElementInputs.Fields(group.Key).Order()),"Source control field set differs.");
        foreach(var (family,field,min,max,decimalClamp) in rules)
        {
            var suffix=decimalClamp?".0":"";
            Expect(GlobalElementInputs.Normalize(family,field,(max+1)+".75")==max+suffix,"Wrong upper EN_CHANGE format: "+family+field);
            Expect(GlobalElementInputs.Normalize(family,field,min<0?"-32769":"4294967295")==min+suffix,"Wrong lower signed/unsigned clamp.");
            foreach(var input in new[]{"","invalid","-",max+".75",min<0?"2147483648":"4294967296"})Expect(GlobalElementInputs.Normalize(family,field,input)==input,"Partial/in-range/overflow text normalized.");
            Expect(GlobalElementInputs.Step(family,field,"",1)=="1"&&GlobalElementInputs.Step(family,field,"2.75",1)=="3","Empty/whole-part UDN differs.");
            Expect(GlobalElementInputs.Step(family,field,"0",-1)==(min<0?"-1":"0"+suffix),"Zero step signed/unsigned roundtrip differs.");
            Expect(GlobalElementInputs.Step(family,field,max+".75",1)==max+suffix,"Upper source UDN format differs.");
        }
        Expect(GlobalElementInputs.Step("Terrains","Damage","-2147483648",-1)=="32767"&&GlobalElementInputs.Step("Terrains","Damage","-32768",-1)=="-32768","Signed terrain step bounds/wrap differ.");
        Expect(GlobalElementInputs.Normalize("Rooms","TorchLightH","9.5")=="8"&&GlobalElementInputs.Normalize("Traps","ChargeTime","16001.5")=="16000"
            &&GlobalElementInputs.Normalize("Spells","RechargeTime","32768.5")=="32767","Original integer clamp text for fixed-point fields lost.");
        Expect(GlobalDoorControls.Step("Cost","",1)=="1"&&GlobalDoorControls.Step("Cost","4294967295",1)=="0","Door public step bypasses source getter/setter.");
        Reject(()=>GlobalDoorControls.Step("ManaCost","1",1));Reject(()=>GlobalElementInputs.Step("Terrains","TerrainLightR","1",1));Reject(()=>GlobalElementInputs.Step("Rooms","Cost","1",0));
        foreach(var native in new[]{false,true})
        {
            var directory=Path.Combine(Path.GetTempPath(),"ElementInput-"+Guid.NewGuid().ToString("N"));
            try
            {
                var doc=native?GlobalDefinitionNumericChecks.Fixture(directory):NativeMapDocument.CreateNewMap(new("ElementInput","ElementInput",20,20));
                doc.SetGlobalDefinitionValues([new("Terrains",2,"TerrainLight","1"),new("Terrains",2,"AmbientLight","1")]);
                var indices=new Dictionary<string,int>{{"Terrains",2},{"Rooms",doc.GlobalDefinitionRows().First(r=>r.Family=="Rooms"&&GlobalRoomControls.Enabled(r.Index,"Cost")).Index},
                    {"Doors",0},{"Traps",doc.GlobalDefinitionRows().First(r=>r.Family=="Traps"&&GlobalElementInputs.Fields("Traps").All(f=>GlobalTrapControls.Enabled(r.Index,f))).Index},{"Spells",0}};
                var initial=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
                void Exact()=>Expect(initial.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history,"Private elements/Cancel/Undo changed live state.");
                string Input(string family,string field,string current)
                {
                    if(family=="Doors"&&field=="ManaCost")return current;
                    var rule=rules.SingleOrDefault(r=>r.Family==family&&r.Field==field);
                    return rule.Field is null?current:GlobalElementInputs.Normalize(family,field,rule.Min<0?"-32769":(rule.Max+1)+".75");
                }
                void ApplyPages(NativeMapDocument working)
                {
                    GlobalDefinitionRow Row(string family)=>working.GlobalDefinitionRows().Single(r=>r.Family==family&&r.Index==indices[family]);
                    var terrain=working.CreateGlobalTerrainDraft(Row("Terrains"));working.ApplyGlobalTerrainDraft(working.UpdateGlobalTerrainDraft(terrain,GlobalTerrainControls.Fields.Select(f=>new GlobalTerrainInput(f,Input("Terrains",f,terrain.Display(f))))));
                    var room=working.CreateGlobalRoomDraft(Row("Rooms"));working.ApplyGlobalRoomDraft(working.UpdateGlobalRoomDraft(room,GlobalRoomControls.Fields.Select(f=>new GlobalRoomInput(f,Input("Rooms",f,room.Display(f))))));
                    var door=working.CreateGlobalDoorDraft(Row("Doors"));working.ApplyGlobalDoorDraft(working.UpdateGlobalDoorDraft(door,GlobalDoorControls.Fields.Select(f=>new GlobalDoorInput(f,Input("Doors",f,door.Display(f))))));
                    var trap=working.CreateGlobalTrapDraft(Row("Traps"));working.ApplyGlobalTrapDraft(working.UpdateGlobalTrapDraft(trap,GlobalTrapControls.MainFields.Select(f=>new GlobalTrapInput(f,Input("Traps",f,trap.Display(f))))));
                    var spell=working.CreateGlobalSpellDraft(Row("Spells"));working.ApplyGlobalSpellDraft(working.UpdateGlobalSpellDraft(spell,GlobalSpellControls.MainFields.Select(f=>new GlobalSpellInput(f,Input("Spells",f,spell.Display(f))))));
                }
                using(var session=doc.CreateGlobalVariablesSession())
                {
                    ApplyPages(session.WorkingDocument);Exact();
                    foreach(var rule in rules.Where(r=>r.Family!="Doors"||r.Field!="ManaCost"))
                    {
                        var row=session.WorkingDocument.GlobalDefinitionRows().Single(r=>r.Family==rule.Family&&r.Index==indices[rule.Family]);
                        var raw=session.WorkingDocument.GlobalDefinitionValues(row).Single(v=>v.Name==rule.Field).Value;
                        var expected=rule.Min<0?-32768:rule.Family=="Terrains"&&rule.Field=="TerrainLightH"||rule.Family=="Rooms"&&rule.Field is "TorchLightH" or "TorchLightRad"||rule.Family=="Traps"&&rule.Field is "IdleEffectDelay" or "ChargeTime" or "RechargeTime" or "ThreatDuration"||rule.Family=="Spells"&&rule.Field=="RechargeTime"?rule.Max*4096:rule.Max;
                        Expect(raw==expected.ToString(System.Globalization.CultureInfo.InvariantCulture),"Wrong independent clamped wire value: "+rule.Family+rule.Field);
                    }
                    doc.ApplyGlobalVariables(doc.PrepareGlobalVariables(session));Expect(doc.EditorChangeCount==count+1&&doc.GetHistory().Undo.Count==history+1,"Five pages split common Undo.");doc.Undo();Exact();doc.Redo();doc.Undo();Exact();
                }
                using(var canceled=doc.CreateGlobalVariablesSession())ApplyPages(canceled.WorkingDocument);Exact();
                doc.SetGlobalDefinitionValues([new("Rooms",indices["Rooms"],"TorchLightH","65535"),new("Doors",0,"ManaCost","65535")]);
                var imported=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
                var importedRoom=doc.CreateGlobalRoomDraft(doc.GlobalDefinitionRows().Single(r=>r.Family=="Rooms"&&r.Index==indices["Rooms"]));
                doc.ApplyGlobalRoomDraft(doc.UpdateGlobalRoomDraft(importedRoom,GlobalRoomControls.Fields.Select(f=>new GlobalRoomInput(f,importedRoom.Display(f)))));
                var importedDoor=doc.CreateGlobalDoorDraft(doc.GlobalDefinitionRows().Single(r=>r.Family=="Doors"&&r.Index==0));
                doc.ApplyGlobalDoorDraft(doc.UpdateGlobalDoorDraft(importedDoor,GlobalDoorControls.Fields.Select(f=>new GlobalDoorInput(f,importedDoor.Display(f)))));
                Expect(imported.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"No-op repaired imported room light or disabled mana buddy.");doc.Undo();Exact();
            }
            finally
            {
                if(Directory.Exists(directory))
                {
                    var full=Path.GetFullPath(directory);Expect(full.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(full).StartsWith("ElementInput-",StringComparison.Ordinal),"Unexpected cleanup path.");Directory.Delete(full,true);
                }
            }
        }
        Console.WriteLine("GLOBAL ELEMENT INPUT DETAIL33 literal controls across5 pages, distinct integer/.0 clamp text, source signed/unsigned whole/partial/wrap steps, disabled door buddy,32 active fields native/DKM exact wire values/private common oneUndo/Redo/Cancel/imported no-op; no GUI/game");
    }
}
