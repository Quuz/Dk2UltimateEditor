using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static partial class Program
{
    private static void CheckSpeechText(bool native,string language,string directory)
    {
        if(!native && language=="en")CheckOwnedAudioCleanup();
        var resources=Path.Combine(directory,$"speech-resources-{language}-{native}");var textFolder=Path.Combine(resources,"Data","Text","Default");Directory.CreateDirectory(textFolder);
        void TextFile(string name,params byte[] glyphs)
        {
            var bytes=new byte[12+glyphs.Length*9];"BFST"u8.CopyTo(bytes);BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4),0x420);BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8),(uint)glyphs.Length);
            for(var i=0;i<glyphs.Length;i++){var offset=12+glyphs.Length*4+i*5;BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12+i*4),(uint)(offset-12));bytes[offset]=1;bytes[offset+1]=1;bytes[offset+4]=glyphs[i];}File.WriteAllBytes(Path.Combine(textFolder,name),bytes);
        }
        TextFile("LEVEL1.STR",0xD4,0xD3,0xD2);TextFile("LEVEL1_BRIEFING.STR",0xE5);TextFile("Speech.STR",0xD4,0xD3,0xD2);
        var expectedAudio=new Dictionary<int,byte[]>();
        void Bank(string name,int count)
        {
            var folder=Path.Combine(resources,"Data","Sound","sfx",name);Directory.CreateDirectory(folder);var bytes=new byte[4+count*4+count*43];BinaryPrimitives.WriteUInt32LittleEndian(bytes,(uint)count);
            for(var i=0;i<count;i++){var offset=4+count*4+i*43;BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4+i*4),(uint)offset);BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset),40);BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset+4),3);Encoding.ASCII.GetBytes(i==0?"Zulu":"Alpha").CopyTo(bytes,offset+8);bytes[offset+40]=0xFF;bytes[offset+41]=(byte)i;bytes[offset+42]=0xFD;if(name=="SPEECH_LEVEL1")expectedAudio[i+1]=bytes.AsSpan(offset+40,3).ToArray();}File.WriteAllBytes(Path.Combine(folder,name+"HD.SDT"),bytes);
        }
        Bank("SPEECH_MENTOR",3);Bank("SPEECH_LEVEL1",2);Bank("SPEECH_ÉTÉ",1);Directory.CreateDirectory(Path.Combine(resources,"Data","Sound","sfx","SPEECH_MISSING"));
        var originalResources=Directory.GetFiles(resources,"*",SearchOption.AllDirectories).ToDictionary(file=>file,File.ReadAllBytes);
        var source=NativeMapDocument.CreateNewMap(new("SpeechUI","SpeechUI",24,24,PlaceHearts:true,NativeFormat:native));var seed=source.CreateMapSpeechTextDraft();
        NativeField Field(NativeMapDocument doc,MapSpeechTextDraft draft,string key)=>draft.Record!.Fields.Single(field=>field.Name==MapSpeechText.StorageField(doc.IsLegacyProject,key));
        foreach(var(key,value)in new[]{("SpeechFile","UNKNOWN_BANK"),("SpeechWinID","65000"),("TextTableID",native?"250":"65000"),("TextIntroCreatureID0","255")})source.SetField(seed.Record!,Field(source,seed,key),value);
        var input=source.SaveCopy(Path.Combine(directory,$"speech-input-{language}-{native}"));var originalInputs=Directory.GetFiles(Path.GetDirectoryName(input)!,"*",SearchOption.AllDirectories).ToDictionary(file=>file,File.ReadAllBytes);
        var settings=new AppSettings{GameResources=new(resources)};var shell=new MainWindow(settings,persistSettings:false);shell.OpenMapFromPath(input);var workspace=(EditorWorkspace)shell.FindName("Editor");var doc=workspace.Document!;
        var serialized=System.Text.Json.JsonSerializer.Serialize(settings);Expect(System.Text.Json.JsonSerializer.Deserialize<AppSettings>(serialized)!.GameResources==settings.GameResources,"Resource-root/player settings do not survive JSON round trip.");
        workspace.SetSelection(new(2,2,3,3));workspace.Copy();var buffer=workspace.BufferedSelection;var canvas=Control<MapCanvas>(workspace,"Canvas");var selection=canvas.Selection;var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var history=doc.GetHistory().Undo.Count;
        void Exact(){Expect(before.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Speech UI changed unrelated bytes/Undo.");Expect(ReferenceEquals(buffer,workspace.BufferedSelection)&&selection==canvas.Selection,"Speech UI changed map selection/copy buffer.");}
        MapSpeechTextWindow? window=null;workspace.SpeechTextPresenter=shown=>window=shown;var command=Control<Button>(workspace,"SpeechTextButton");command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(window is not null,"Actual speech/text command missing.");
        T Named<T>(string name)=>(T)window!.FindName(name);
        TextBox Number(string key)=>Named<TextBox>("SpeechTextField"+key);
        void Select(string key,string id){var combo=Named<ComboBox>("SpeechTextField"+key);combo.SelectedItem=combo.Items.Cast<object>().Single(row=>Get<string>(row,"ID")==id);}
        Expect(Number("SpeechWinID").Text=="65000" && Named<ComboBox>("SpeechTextFieldTextTableID").Items.Count==60 && Named<ComboBox>("SpeechTextFieldTextIntroCreatureID0").Items.Count==MapTextCatalogues.IntroCreatures.Count+1,"Imported IDs/source choices lost.");Exact();
        Named<Button>("SpeechTextIncreaseTextTitleID").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(Number("TextTitleID").Text=="1","Original numeric increment missing.");Named<Button>("SpeechTextDecreaseTextTitleID").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(Number("TextTitleID").Text=="0" && Named<Button>("SpeechTextClose").IsCancel,"Numeric decrement/Escape close missing.");Exact();
        Select("TextTableID","1");var texts=Named<ListBox>("SpeechTextTextList");Expect(texts.Items.Count==3,"Current-level text file not previewed.");texts.SelectedIndex=1;Expect(Named<TextBox>("SpeechTextTextPreview").Text=="B","Read-only original text preview.");Named<ComboBox>("SpeechTextPreviewSource").SelectedIndex=1;Expect(texts.Items.Count==1,"Briefing preview file wrong.");Named<ComboBox>("SpeechTextPreviewSource").SelectedIndex=0;
        Select("SpeechFile","SPEECH_LEVEL1");var sounds=Named<ListBox>("SpeechTextSoundList");Expect(sounds.Items.Count==2,"Selected custom sound bank preview.");sounds.SelectedIndex=1;
        string? previewPath=null;window!.PlaybackLauncher=request=>{previewPath=request.Mp2Path;Expect(File.ReadAllBytes(request.Mp2Path).SequenceEqual(expectedAudio[2]),"Playback selected row ID/payload wrong.");};
        Expect(window.PlaySelected() && previewPath is not null && File.Exists(previewPath),"Guarded playback did not prepare owned MP2.");Exact();
        // InvalidDataException must remain a recoverable resource error in the existing window too.
        var bankPath=Path.Combine(resources,"Data","Sound","sfx","SPEECH_LEVEL1","SPEECH_LEVEL1HD.SDT");
        File.WriteAllBytes(bankPath,[1,2,3]);Select("SpeechFile","SPEECH_MENTOR");Select("SpeechFile","SPEECH_LEVEL1");
        Expect(Named<ListBox>("SpeechTextSoundList").Items.Count==0 && !window.PlaySelected() && Named<TextBlock>("SpeechTextSoundResourceStatus").Text==LocalizationService.Get("SpeechText.Missing"),"Malformed SDT escaped the recoverable preview boundary.");Exact();
        File.WriteAllBytes(bankPath,originalResources[bankPath]);
        var captionPath=Path.Combine(textFolder,"LEVEL1.STR");File.WriteAllBytes(captionPath,[1,2,3]);
        Select("SpeechFile","SPEECH_MENTOR");Select("SpeechFile","SPEECH_LEVEL1");Named<ListBox>("SpeechTextSoundList").SelectedIndex=1;
        Expect(Named<TextBox>("SpeechTextSoundPreview").Text==LocalizationService.Get("SpeechText.Missing") && window.PlaySelected(),"Malformed caption disabled valid audio.");Exact();
        Select("TextTableID","0");Select("TextTableID","1");
        Expect(Named<ListBox>("SpeechTextTextList").Items.Count==0 && Named<TextBlock>("SpeechTextTextResourceStatus").Text==LocalizationService.Get("SpeechText.Missing"),"Malformed STR escaped the text preview boundary.");Exact();
        File.WriteAllBytes(captionPath,originalResources[captionPath]);
        Select("SpeechFile","SPEECH_MENTOR");Select("SpeechFile","SPEECH_LEVEL1");Named<ListBox>("SpeechTextSoundList").SelectedIndex=1;Select("TextTableID","0");Select("TextTableID","1");
        Number("SpeechPreLevelID").Text="Pending";Named<TabControl>("SpeechTextTabs").SelectedIndex=1;LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(Number("SpeechPreLevelID").Text=="Pending" && Named<ListBox>("SpeechTextSoundList").SelectedIndex==1 && Named<TabControl>("SpeechTextTabs").SelectedIndex==1,"Language discarded draft/preview/tab selection.");LocalizationService.SetLanguage(language);
        Number("SpeechPreLevelID").Text="4";Expect(!window.ApplyOptions() && Named<TextBlock>("SpeechTextStatus").Text==LocalizationService.Get("SpeechText.Invalid"),"Mentor count not used/untranslated invalid input.");Exact();
        Number("SpeechPreLevelID").Text="3";Number("TextTitleID").Text="9999";Select("TextIntroCreatureID7",MapTextCatalogues.IntroCreatures.First(row=>row.English.Contains("Elite",StringComparison.Ordinal)).Value);Number("TextIntroTextID7").Text="9999";
        Named<Button>("SpeechTextApply").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));var values=doc.CreateMapSpeechTextDraft().Values;
        Expect(values["SpeechPreLevelID"]=="3" && values["SpeechWinID"]=="65000" && values["TextTitleID"]=="9999" && values["TextTableID"]=="1" && values["TextIntroCreatureID0"]=="255" && values["SpeechFile"]=="SPEECH_LEVEL1" && doc.GetHistory().Undo.Count==history+1,"Actual whole metadata Apply not atomic/unknown-preserving.");
        Expect(window.ApplyOptions() && doc.GetHistory().Undo.Count==history+1,"Speech no-op consumed history.");
        var saved=doc.SaveCopy(Path.Combine(directory,$"speech-output-{language}-{native}"));Expect(doc.CreateMapSpeechTextDraft().Values.OrderBy(row=>row.Key).SequenceEqual(NativeMapDocument.Load(saved).CreateMapSpeechTextDraft().Values.OrderBy(row=>row.Key)),"Speech UI save/reopen lost values.");
        Select("TextTableID","0");Expect(window.ApplyOptions() && doc.CreateMapSpeechTextDraft().Values["TextTableID"]=="1","Original None table behavior changed.");Select("SpeechFile","");Expect(window.ApplyOptions() && doc.CreateMapSpeechTextDraft().Values["SpeechFile"]=="" && doc.CreateMapSpeechTextDraft().Values["SpeechWinID"]=="65000","None speech cleared IDs.");doc.Undo();doc.Undo();Exact();Expect(!window.ApplyOptions(),"Undo reused speech draft.");
        Named<Button>("SpeechTextDiscard").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Select("SpeechFile","SPEECH_MISSING");Expect(Named<ListBox>("SpeechTextSoundList").Items.Count==0 && !window.PlaySelected(),"Missing bank fabricated sounds.");Select("SpeechFile","SPEECH_ÉTÉ");Expect(window.ApplyOptions() && doc.CreateMapSpeechTextDraft().Values["SpeechFile"]=="SPEECH_ÉTÉ","Native/project ANSI bank choice failed.");doc.Undo();Exact();
        Named<Button>("SpeechTextDiscard").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Number("TextPlotID").Text="456";
        // Genuine pending DataGrid commits invalidate, rather than silently rebase, this modeless draft.
        seed=doc.CreateMapSpeechTextDraft();var record=seed.Record!;var field=Field(doc,seed,"TextTitleID");var categories=Control<ComboBox>(workspace,"CategoryPicker");var records=Control<ListBox>(workspace,"RecordList");categories.SelectedItem=categories.Items.Cast<object>().Single(row=>Get<string>(row,"ID")==record.Category);records.SelectedItem=records.Items.Cast<object>().Single(row=>Get<NativeRecord>(row,"Record").Offset==record.Offset);
        Control<TabControl>(workspace,"WorkspaceTabs").SelectedIndex=1;var properties=Control<DataGrid>(workspace,"PropertyGrid");workspace.Measure(new Size(1500,1000));workspace.Arrange(new Rect(0,0,1500,1000));workspace.UpdateLayout();properties.SelectedItem=properties.Items.Cast<object>().Single(row=>Get<NativeField>(row,"Field").Name==field.Name);properties.ScrollIntoView(properties.SelectedItem,properties.Columns[1]);properties.UpdateLayout();properties.CurrentCell=new DataGridCellInfo(properties.SelectedItem,properties.Columns[1]);Expect(properties.BeginEdit(),"Pending speech grid edit missing.");properties.UpdateLayout();var pending=properties.Columns[1].GetCellContent(properties.SelectedItem) as TextBox;Expect(pending is not null,"Pending speech editor absent.");pending!.Text="321";
        Expect(!window.ApplyOptions() && doc.CreateMapSpeechTextDraft().Values["TextTitleID"]=="321" && doc.CreateMapSpeechTextDraft().Values["TextPlotID"]=="0" && Number("TextPlotID").Text=="456","Pending generic commit reused speech authority.");doc.Undo();Exact();
        Named<Button>("SpeechTextDiscard").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(MemoryMarshal.TryGetArray(record.File.Data,out var backing),"Speech raw fixture absent.");var offset=backing.Offset+record.Offset+Field(doc,seed,"SpeechFile").Offset;backing.Array![offset]^=1;Expect(!window.ApplyOptions() && !Named<Button>("SpeechTextApply").IsEnabled,"Raw mutation reused speech authority.");backing.Array[offset]^=1;
        Named<Button>("SpeechTextDiscard").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Number("TextPlotID").Text="bad";window.FolderPicker=()=>resources;Named<Button>("SpeechTextFolder").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(Number("TextPlotID").Text=="bad" && workspace.GameResources.Root==Path.GetFullPath(resources),"Configuration discarded draft or was not retained.");Exact();
        var player=Path.Combine(resources,"fake player.exe");File.WriteAllBytes(player,[]);window.PlayerPicker=()=>player;Named<Button>("SpeechTextPlayer").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(workspace.GameResources.Player==player,"Player choice not retained.");Named<Button>("SpeechTextDefaultPlayer").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(workspace.GameResources.Player is null,"Default-player command missing.");
        Select("SpeechFile","SPEECH_LEVEL1");Select("TextTableID","1");Named<ListBox>("SpeechTextSoundList").SelectedIndex=1;Named<ListBox>("SpeechTextTextList").SelectedIndex=1;
        RenderSpeech(window,native,language);window.Close();Expect(previewPath is not null && !File.Exists(previewPath),"Owned MP2 preview leaked after close.");
        window=workspace.CreateSpeechTextWindow()!;workspace.Document=null;workspace.Document=doc;Expect(!window.ApplyOptions() && !window.PlaySelected(),"Replaced/restored workspace reused speech authority.");window.Close();CheckHistoricalSpeech(native,language,directory,resources);workspace.Document=null;shell.Close();Expect(!command.IsEnabled && workspace.CreateSpeechTextWindow() is null,"Closed shell retained speech authority.");
        Expect(originalInputs.All(row=>File.ReadAllBytes(row.Key).SequenceEqual(row.Value)) && originalResources.All(row=>File.ReadAllBytes(row.Key).SequenceEqual(row.Value)),"Speech/text UI edited original disk files.");
        Console.WriteLine($"PASS WPF speech/text actual commands, source choices/None, ANSI, previews/playback, pending/stale/config/history, historical and lifetime guards {language} {(native?"native":"project")}");
    }
    private static void RenderSpeech(MapSpeechTextWindow window,bool native,string language)
    {
        foreach(var tab in new[]{0,1})foreach(var size in new[]{new Size(900,780),new Size(780,600)})foreach(var bottom in tab==0?new[]{false,true}:new[]{false})
        {
            var tabs=(TabControl)window.FindName("SpeechTextTabs");tabs.SelectedIndex=tab;var content=(FrameworkElement)window.Content;content.Measure(size);content.Arrange(new Rect(new Point(0,0),size));content.UpdateLayout();var scroll=(ScrollViewer)((TabItem)tabs.Items[tab]).Content;if(bottom)scroll.ScrollToEnd();else scroll.ScrollToTop();content.UpdateLayout();
            var apply=(Button)window.FindName("SpeechTextApply");var bounds=apply.TransformToAncestor(content).TransformBounds(new Rect(apply.RenderSize));Expect(bounds.Left>=0 && bounds.Right<=size.Width && bounds.Bottom<=size.Height,"Speech Apply outside viewport.");
            if(Environment.GetEnvironmentVariable("DK2_SPEECH_TEXT_ARTIFACTS") is {Length:>0} artifacts)
            {
                Directory.CreateDirectory(artifacts);var background=new DrawingVisual();using(var drawing=background.RenderOpen())drawing.DrawRectangle((Brush)Application.Current.FindResource("Brush.Background"),null,new Rect(new Point(0,0),size));var bitmap=new RenderTargetBitmap((int)size.Width,(int)size.Height,96,96,PixelFormats.Pbgra32);bitmap.Render(background);bitmap.Render(content);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=new FileStream(Path.Combine(artifacts,$"speech-{language}-{native}-{tab}-{size.Width}-{bottom}.png"),FileMode.CreateNew);encoder.Save(stream);
            }
        }
    }
    private static void CheckOwnedAudioCleanup()
    {
        var preview=new GameAudioPreview();var file=preview.Prepare([1,2,3]);var folder=Path.GetDirectoryName(file)!;var foreign=Path.Combine(folder,"not-owned.txt");File.WriteAllText(foreign,"retain");preview.Dispose();Expect(!File.Exists(file)&&File.ReadAllText(foreign)=="retain","Audio cleanup removed a file it did not create.");File.Delete(foreign);Directory.Delete(folder,false);
    }
    private static void CheckHistoricalSpeech(bool native,string language,string directory,string resources)
    {
        if(native)
        {
            var doc=NativeMapDocument.Create(24,24,"FutureSpeech");Expect(MemoryMarshal.TryGetArray(doc.MainFile.Data,out var backing),"Future native speech backing unavailable.");backing.Array![backing.Offset+4]=9;
            var window=new MapSpeechTextWindow(doc,()=>true,()=>true,new(resources),_=>{});Expect(!((Button)window.FindName("SpeechTextApply")).IsEnabled && !window.ApplyOptions(),"Future native speech metadata edited.");window.Close();return;
        }
        var source=NativeMapDocument.CreateNewMap(new("SpeechHistory","SpeechHistory",24,24));var entry=source.SaveCopy(Path.Combine(directory,"speech-history-"+language));var project=LegacySectionFile.Read(entry);var info=project.Sections.Single(row=>row.Signature==NativeMapDocument.LegacyInfo);
        foreach(var version in new[]{1,2,3,4,8})
        {
            var structure=version==1?"MAPPROPERTIESV1":version is 2 or 3?"MAPPROPERTIESV2":version==4?"MAPPROPERTIESV4":"MAPPROPERTIES";var replacement=new LegacySection(info.Signature,version,info.Payload[..LegacyProjectSchemas.Size(structure)]);var bytes=LegacySectionFile.Create(project.Sections.Select(row=>row==info?replacement:row)).ToArray();var path=Path.Combine(directory,$"speech-history-{language}-{version}.dkm");File.WriteAllBytes(path,bytes);var doc=NativeMapDocument.Load(path);
            var window=new MapSpeechTextWindow(doc,()=>true,()=>true,new(resources),_=>{});Expect(((ComboBox)window.FindName("SpeechTextFieldSpeechFile")).IsEnabled==(version is >=2 and <=4) && !((TextBox)window.FindName("SpeechTextFieldTextTitleID")).IsEnabled,"Historical metadata enabled missing/padding fields.");
            if(version is >=2 and <=4){var banks=(ComboBox)window.FindName("SpeechTextFieldSpeechFile");banks.SelectedItem=banks.Items.Cast<object>().Single(row=>Get<string>(row,"ID")=="SPEECH_LEVEL1");Expect(window.ApplyOptions(),"Historical bank-only edit failed.");doc.Undo();Expect(File.ReadAllBytes(path).SequenceEqual(bytes),"Historical UI wrote source file.");}else Expect(!window.ApplyOptions(),"Unsupported project metadata edited.");window.Close();
        }
    }
}
