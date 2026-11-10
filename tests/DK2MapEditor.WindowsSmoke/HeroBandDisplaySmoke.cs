using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Compiled only. WPF execution and physical keyboard/DPI acceptance stay manual.
    private static void CheckHeroBandDisplay(bool native,string language)
    {
        var d=NativeMapDocument.CreateNewMap(new("BandDisplayUI","BandDisplayUI",20,20,NativeFormat:native));
        var elite=LegacyCompilerRules.Elements("BadElites").First().ID;
        NativeRecord Band()=>native?d.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="GAMEBAND"),new(0,0)):d.AddLegacyRecord("Band",new(0,0));
        var band=Band();d.AddBandMember(band,elite,7,3);d.AddBandMember(band,17,4,3);_ = Band();
        var bytes=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var state=(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo);
        var workspace=new EditorWorkspace{Document=d};var window=new HeroBandWindow(d,band,()=>true,()=>true);
        try
        {
            var categories=Control<ComboBox>(workspace,"CategoryPicker");categories.SelectedItem=categories.Items.Cast<object>().Single(row=>Get<string>(row,"ID")=="Manager.Bands");
            var list=Control<ListBox>(workspace,"RecordList");Expect(list.Items.Count==2&&list.ItemTemplate is not null&&string.IsNullOrEmpty(list.DisplayMemberPath),"Band manager lost physical parent rows/template.");
            var previews=(System.Collections.IEnumerable)list.Items[0].GetType().GetProperty("BandPreview")!.GetValue(list.Items[0])!;var rows=previews.Cast<object>().ToArray();
            Expect(rows.Length==2&&Get<string>(rows[0],"Label").Contains("7")&&Get<BitmapSource>(rows[0],"Icon") is{PixelWidth:32,PixelHeight:32,IsFrozen:true},"Band preview missing elite/level/source32 icon.");
            var members=(ListBox)window.FindName("HeroBandMembers");Expect(members.Items.Count==2,"Private parent lost member rows.");members.SelectedIndex=0;
            var label=Get<string>(members.Items[0],"Label");Expect(label.Contains(language=="ru"?"элита":"Elite")&&label.Contains("/"),"Imported elite still unknown or compact flags absent.");
            window.MemberPresenter=child=>
            {
                var fields=(HeroBandValuePanel)child.FindName("HeroMemberFields");var choices=(ComboBox)fields.FindName("HeroCreature");
                Expect(choices.Items.Count==33&&Convert.ToUInt32(choices.SelectedValue)==elite,"Imported elite replaced or added to standard creation catalogue.");
                Expect(Get<string>(choices.SelectedItem,"Label").Contains(language=="ru"?"элита":"Elite")&&Get<BitmapSource>(choices.SelectedItem,"Icon") is{PixelWidth:32,PixelHeight:32},"Nested imported elite presentation incomplete.");child.Close();return false;
            };
            Expect(window.ModifyMember(),"Private member dialog did not open.");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");workspace.RefreshLanguage();Expect(members.SelectedIndex==0,"Language refresh lost physical member selection.");
            LocalizationService.SetLanguage(language);workspace.RefreshLanguage();
        }
        finally{window.Close();workspace.Document=null;}
        Expect(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&state==(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo),"Band image/preview browsing changed map/history.");
    }
}
