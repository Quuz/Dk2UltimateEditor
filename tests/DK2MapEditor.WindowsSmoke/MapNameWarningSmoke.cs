using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared for manual execution only; building this never launches WPF.
    private static void CheckMapNameWarning(bool native,string language)
    {
        var document=NativeMapDocument.CreateNewMap(new("NameUI","Original",24,20,NativeFormat:native));var prefs=new EditorRequestPreferences();
        var before=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        void Exact()=>Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Name dialog touched live bytes before Apply/after Undo");
        MapPropertiesWindow Open(){var result=new MapPropertiesWindow(document,()=>true,()=>true,new(),prefs);ShowManagerCallerWindow(result);return result;}
        T Named<T>(MapPropertiesWindow window,string key)where T:class=>(T)window.FindName("MapProperties"+key);
        var window=Open();var prompts=0;
        Named<TextBox>(window,"Name").Text=new string('A',65);var workingBefore=window.WorkingDocument.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        window.MapNamePresenter=question=>
        {
            prompts++;Expect(question.Title==LocalizationService.Get("MapName.Title")&&((Button)question.FindName("MapNameYes")).IsDefault,"Name dialog title/default Yes differs");
            Expect(!window.ApplyProperties(),"Name question allowed nested parent Apply");
            ((CheckBox)question.FindName("MapNameRemember")).IsChecked=true;Expect(!((Button)question.FindName("MapNameNo")).IsEnabled&&!question.Keep(),"Checked No accepted");question.Close();
        };
        Expect(!window.ApplyProperties()&&prefs.Flags==0&&workingBefore.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Checked Close remembered or staged name");Exact();
        window.MapNamePresenter=question=>{prompts++;Expect(question.Keep(),"No failed");};
        Expect(window.ApplyProperties()&&prompts==2&&document.CreateMapGeneralDraft().Values["Name"]=="Original","No did not save parent with original name");document.Undo();Exact();

        window=Open();Named<TextBox>(window,"Name").Text=new string('B',64);window.MapNamePresenter=_=>throw new InvalidOperationException("Exactly64 asked an overflow question");
        Expect(window.ApplyProperties()&&document.CreateMapGeneralDraft().Values["Name"]==new string('B',63),"Exactly64 source read changed");document.Undo();Exact();

        window=Open();Named<TextBox>(window,"Name").Text=new string('C',80);
        window.MapNamePresenter=question=>
        {
            prompts++;((CheckBox)question.FindName("MapNameRemember")).IsChecked=true;Expect(question.Accept(),"Yes failed");
            Named<TextBox>(window,"Name").Text="Changed after answer";
        };
        Expect(!window.ApplyProperties()&&prefs.Flags==4,"Changed caller saved old name or lost session preference");Exact();window.Close();
        window=Open();Named<TextBox>(window,"Name").Text=new string('D',90);window.MapNamePresenter=_=>throw new InvalidOperationException("Outer Cancel lost remembered Copy");
        Expect(window.ApplyProperties()&&document.CreateMapGeneralDraft().Values["Name"]==new string('D',63),"Suppressed Copy reused old text");document.Undo();Exact();

        window=Open();Named<Button>(window,"NameReset").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(prefs.Flags==0,"Name reset failed");
        Named<TextBox>(window,"Name").Text=new string('E',65);window.MapNamePresenter=question=>
        {prompts++;LocalizationService.SetLanguage(language=="en"?"ru":"en");question.Accept();};
        Expect(!window.ApplyProperties(),"Language change revived old name approval");Exact();window.Close();LocalizationService.SetLanguage(language);

        window=Open();Named<TextBox>(window,"Name").Text=new string('F',65);window.MapNamePresenter=_=>throw new IOException("Name presenter unavailable");
        Expect(!window.ApplyProperties()&&prefs.Flags==0,"Name delivery failure invented Copy");Exact();window.Close();
        window=Open();Named<TextBox>(window,"Name").Text=new string('G',65);Named<TextBox>(window,"Width").Text="bad";
        window.MapNamePresenter=_=>throw new InvalidOperationException("Invalid dimensions asked for name reply");Expect(!window.ApplyProperties(),"Invalid dimensions saved");Exact();window.Close();
        Console.WriteLine($"PASS WPF map-name Copy/Keep/Cancel/64/remember/reset/stale/language/delivery {native} {language}; physical mouse/audio/DPI remains manual");
    }
}
