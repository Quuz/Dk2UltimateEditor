using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckProjectileSourceOptionFallbacks(NativeMapDocument document,string language)
    {
        foreach(var (family,slot) in new[]{("Traps","Shot"),("Spells","Shot"),("Spells","Upgrade"),("CrSpells","Shot")})
        foreach(var projectile in new[]{60u,5u})
        {
            var row=document.GlobalDefinitionRows().Single(row=>row.Family==family&&row.Index==0);
            var prefix=slot=="Upgrade"?"Upg":"";var typeField=family=="CrSpells"?"Shot":prefix+"ShotType";
            var first=projectile==60?document.IsLegacyProject?"-1":"65535":"4";
            document.SetGlobalDefinitionValues([new(family,0,typeField,projectile.ToString()),new(family,0,prefix+"ShotData1",first),new(family,0,prefix+"ShotData2",projectile==60?"99":"123")]);
            var before=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var count=document.EditorChangeCount;var history=document.GetHistory().Undo.Count;
            void Exact()=>Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&document.EditorChangeCount==count&&document.GetHistory().Undo.Count==history,"Child opening/acceptance/cancel modified map before parent Apply.");
            Window Create()=>family switch
            {
                "Traps"=>new GlobalTrapOptionsWindow(document,document.CreateGlobalTrapDraft(row),false,()=>true),
                "Spells"=>new GlobalSpellOptionsWindow(document,document.CreateGlobalSpellDraft(row),slot,()=>true),
                _=>new CreatureSpellOptionsWindow(document,document.CreateCreatureSpellDraft(row),slot,()=>true)
            };
            var child=Create();
            var name=family switch{"Traps"=>"TrapOptions","Spells"=>"SpellOptions",_=>"CreatureSpellOptions"};
            void Controls()
            {
                Expect(Get<string>(((ComboBox)child.FindName(name+"Data1")).SelectedItem,"ID")== (projectile==60?"1":"4"),"Original child first-target selection differs.");
                if(projectile==60)Expect(Get<string>(((ComboBox)child.FindName(name+"Data2")).SelectedItem,"ID")=="1","Invalid source level must display level1.");
                else Expect(!((TextBox)child.FindName(name+"Data2")).IsEnabled,"Non-gold source target must disable amount.");
            }
            Controls();Exact();LocalizationService.SetLanguage(language=="ru"?"en":"ru");Controls();Exact();LocalizationService.SetLanguage(language);Controls();
            child.Close();Exact();child=Create();Controls();
            switch(child)
            {
                case GlobalTrapOptionsWindow trap:
                    Expect(trap.AcceptOptions()&&trap.AcceptedDraft is not null,"Source trap options not accepted.");Exact();document.ApplyGlobalTrapDraft(trap.AcceptedDraft!);break;
                case GlobalSpellOptionsWindow spell:
                    Expect(spell.AcceptOptions()&&spell.AcceptedDraft is not null,"Source spell options not accepted.");Exact();document.ApplyGlobalSpellDraft(spell.AcceptedDraft!);break;
                case CreatureSpellOptionsWindow creature:
                    Expect(creature.AcceptOptions()&&creature.AcceptedDraft is not null,"Source creature options not accepted.");Exact();document.ApplyCreatureSpellDraft(creature.AcceptedDraft!);break;
            }
            var result=document.GlobalProjectile(row,slot);
            Expect(result.Data1==(projectile==60?"1":"4")&&result.Data2==(projectile==60?"1":"0")&&document.GetHistory().Undo.Count==history+1,"Source accepted fallback/non-gold clear not applied atomically by parent.");
            document.Undo();Exact();document.Undo();
        }
        Console.WriteLine($"PASS {language} four-slot source projectile child fallbacks, non-gold clear, private Cancel/OK, translation and parent Undo (prepared acceptance)");
    }
}
