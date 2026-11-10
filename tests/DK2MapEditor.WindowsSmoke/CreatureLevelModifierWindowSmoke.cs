using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared/compiled only. Running this suite opens WPF windows and needs
    // the user's separate manual/UI authorization.
    private static void CheckCreatureLevelWindow(bool native,string language,string directory)
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root is not null&&!File.Exists(Path.Combine(root.FullName,"DK2MapEditor.slnx")))root=root.Parent;
        Expect(root is not null,"No source root for catalogue fixture.");
        var catalogue=CreatureLevelModifierCatalogue.ReadFile(Path.GetFullPath(Path.Combine(root!.FullName,"..","Dungeon Keeper 2","Data","editor","GlobalVariables.kwd")));
        var doc=NativeMapDocument.CreateNewMap(new("LevelWindowUI","LevelWindowUI",20,20,NativeFormat:native));
        var before=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;var current=true;
        void Exact()=>Expect(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history,"Private UI operation wrote live map/history.");
        TextBox Field(CreatureLevelModifierWindow w,string suffix)=>(TextBox)w.FindName("CreatureLevel"+suffix);
        var window=new CreatureLevelModifierWindow(doc,()=>current,()=>true,new(),catalogue);ShowManagerCallerWindow(window);
        try
        {
            Expect(((ComboBox)window.FindName("CreatureLevelStatistic")).Items.Count==23,"The official visible statistic catalogue differs.");
            var badBytes=catalogue.CopyBytes();badBytes[0]=0;
            window.CataloguePicker=()=>{var invalid=Path.Combine(directory,"invalid-"+native+language+".kwd");File.WriteAllBytes(invalid,badBytes);return invalid;};
            ((Button)window.FindName("CreatureLevelChoose")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(((TextBlock)window.FindName("CreatureLevelStatus")).Text==LocalizationService.Get("CreatureLevel.CatalogueError")&&window.SelectedCatalogue?.SHA256==catalogue.SHA256,"InvalidDataException escaped the source picker or replaced a valid context.");
            Field(window,"Value2").Text="-10000";((RepeatButton)window.FindName("CreatureLevelValue2Decrease")).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Expect(Field(window,"Value2").Text=="-10000","Signed spinner crossed its minimum.");
            ((RepeatButton)window.FindName("CreatureLevelValue2Increase")).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));Expect(Field(window,"Value2").Text=="-9999","Signed spinner did not step by one.");
            Field(window,"FirstPerson").Text="10000";((RepeatButton)window.FindName("CreatureLevelFirstPersonIncrease")).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));Expect(Field(window,"FirstPerson").Text=="10000","First Person spinner crossed its maximum.");
            Field(window,"Value2").Text="-17";Field(window,"FirstPerson").Text="0";var input=window.InputRevision;
            LocalizationService.SetLanguage(language=="en"?"ru":"en");
            Expect(Field(window,"Value2").Text=="-17"&&Field(window,"FirstPerson").Text=="0"&&window.InputRevision>input,"Language replaced pending percentages.");
            Expect(((Button)window.FindName("CreatureLevelReset")).Content.ToString()==LocalizationService.Get("CreatureLevel.Reset"),"Reset did not translate.");
            LocalizationService.SetLanguage(language);
            var selector=(ComboBox)window.FindName("CreatureLevelStatistic");var selected=selector.SelectedIndex;Field(window,"Value2").Text="10001";selector.SelectedIndex=1;
            Expect(selector.SelectedIndex==selected&&Field(window,"Value2").Text=="10001"&&!window.TryApply(),"An invalid previous page was discarded or published.");Exact();
            Field(window,"Value2").Text="-17";Field(window,"FirstPerson").Text="-9";selector.SelectedIndex=1;Field(window,"Value2").Text="777";selector.SelectedIndex=0;
            Expect(Field(window,"Value2").Text=="-17","Selection lost a private page.");
            var exported=0;window.ExportTargetPicker=()=>Path.Combine(directory,native+language+"-copy.kwd");
            window.CatalogueExporter=(_,bytes,valid)=>{Expect(valid()&&CreatureLevelModifiers.ReadCollection(bytes).Last(r=>r.RecordType==65&&r.Statistic==0&&r.Level==2).PercentBits==0xffffffef,"Export used stale or unstored controls.");exported++;};
            Expect(window.ExportCatalogue()&&exported==1&&window.HasPendingInputs,"Export applied/discarded the private map edit.");Exact();
            window.ExportTargetPicker=()=>catalogue.SourceName;Expect(!window.ExportCatalogue()&&exported==1,"Copy export overwrote its source catalogue.");Exact();
            window.Close();Exact();
        }
        finally{window.Close();}
        window=new(doc,()=>current,()=>true,new(),catalogue);ShowManagerCallerWindow(window);
        try
        {
            Field(window,"Value2").Text="999";((Button)window.FindName("CreatureLevelReset")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(Field(window,"Value2").Text=="100"&&!window.HasPendingInputs,"Reset did not restore the captured page.");
            window.ExportTargetPicker=()=>{Field(window,"Value2").Text="87";return Path.Combine(directory,"late.kwd");};
            var emitted=false;window.CatalogueExporter=(_,_,_)=>emitted=true;Expect(!window.ExportCatalogue()&&!emitted,"Late dialog input retained export authority.");Exact();
            current=false;Expect(!window.TryApply()&&!((Button)window.FindName("CreatureLevelApply")).IsEnabled,"Stale owner retained Apply.");Exact();
        }
        finally{window.Close();current=true;}
        window=new(doc,()=>current,()=>true,new());ShowManagerCallerWindow(window);
        try
        {Expect(window.LoadCatalogue(catalogue),"Explicit source selection failed on a blank map.");Field(window,"Value2").Text="123";Expect(!window.LoadCatalogue(catalogue),"Source replacement discarded typed pending input.");}
        finally{window.Close();}Exact();

        CreatureLevelModifierWindow? child=null;
        var parent=new MapPropertiesWindow(doc,()=>current,()=>true,new());ShowManagerCallerWindow(parent);
        parent.CreatureLevelsPresenter=w=>{child=w;Expect(w.LoadCatalogue(catalogue),"Parent child could not select its catalogue.");ShowManagerCallerWindow(w);};
        try
        {
            ((Button)parent.FindName("MapPropertiesCreatureLevels")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(child is not null,"Map properties has no level-percent child route.");Field(child!,"Value2").Text="201";
            Expect(!parent.ApplyProperties(),"Common parent Apply ignored pending child input.");Exact();
            Expect(child!.TryApply(),"Child did not commit to the private working document.");Exact();
            parent.CreatureLevelsPresenter=w=>{child=w;Expect(w.SelectedCatalogue?.SHA256==catalogue.SHA256,"Reopening the child forgot the selected catalogue.");w.Close();};
            ((Button)parent.FindName("MapPropertiesCreatureLevels")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));parent.Close();Exact();
        }
        finally{child?.Close();parent.Close();}
        parent=new(doc,()=>current,()=>true,new());ShowManagerCallerWindow(parent);
        parent.CreatureLevelsPresenter=w=>{child=w;Expect(w.LoadCatalogue(catalogue),"Second parent child lost catalogue selection.");ShowManagerCallerWindow(w);};
        try
        {
            ((Button)parent.FindName("MapPropertiesCreatureLevels")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Field(child!,"Value2").Text="202";
            Expect(child!.TryApply()&&parent.ApplyProperties(),"Common parent did not publish accepted level percentages.");
            Expect(doc.ReadExistingCreatureLevelModifiers().Last(r=>r.RecordType==65&&r.Statistic==0&&r.Level==2).PercentBits==202&&doc.GetHistory().Undo.Count==history+1,"Common Apply split Undo or lost a value.");
            doc.Undo();Exact();
        }
        finally{child?.Close();parent.Close();}
    }
}
