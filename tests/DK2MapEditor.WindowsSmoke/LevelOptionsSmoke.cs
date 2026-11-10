using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
    private static void CheckLevelOptions(bool native, string language, string directory)
    {
        if (!native && language == "en") CheckLevelRewardPixels();
        var source = NativeMapDocument.CreateNewMap(new("LevelUI","LevelUI",24,24,PlaceHearts: true,NativeFormat:native));
        var seed = source.CreateMapLevelOptionsDraft();
        NativeField Storage(NativeMapDocument doc, MapLevelOptionsDraft draft, string key) => draft.Record!.Fields.Single(field => field.Name == MapLevelOptions.StorageField(doc.IsLegacyProject,key));
        foreach (var (key,value) in new[] { ("Options","3"),("Options2","192"),("RewardPrev1","250"),("GameTicks","0") }) source.SetField(seed.Record!,Storage(source,seed,key),value);
        var input = source.SaveCopy(Path.Combine(directory,$"level-ui-input-{language}-{native}"));
        var disk = Directory.GetFiles(Path.GetDirectoryName(input)!,"*",SearchOption.AllDirectories).ToDictionary(path => path,File.ReadAllBytes);
        var shell = new MainWindow(new AppSettings(),persistSettings:false); shell.OpenMapFromPath(input);
        var workspace = (EditorWorkspace)shell.FindName("Editor"); var doc = workspace.Document!; var before = doc.Files.ToDictionary(file => file,file => file.Data.ToArray());
        workspace.SetSelection(new(2,2,3,3)); workspace.Copy(); var buffer = workspace.BufferedSelection; var canvas = Control<MapCanvas>(workspace,"Canvas"); var selection = canvas.Selection; var history = doc.GetHistory().Undo.Count;
        void Exact() { Expect(before.All(row => row.Key.Data.Span.SequenceEqual(row.Value)),"Level UI changed unrelated/Undo bytes."); Expect(ReferenceEquals(buffer,workspace.BufferedSelection) && selection == canvas.Selection,"Level UI changed selection/copy buffer."); }
        MapLevelOptionsWindow? window = null; workspace.LevelOptionsPresenter = shown => window = shown;
        var command = Control<Button>(workspace,"LevelOptionsButton"); command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(window is not null,"Actual level-options command did not open.");
        T Named<T>(string name) => (T)window!.FindName(name);
        TextBox Number(string field) => Named<TextBox>("LevelOption"+field);
        ComboBox Reward(string field) => Named<ComboBox>("LevelOption"+field);
        Expect(Number("GameTicks").Text == "0" && Get<string>(Reward("RewardPrev1").SelectedItem,"ID") == "250"
            && Named<CheckBox>("LevelFlagLockPossession").IsChecked == true,"Opening level UI coerced imported values/inverted flag."); Exact();
        foreach (var key in MapLevelOptions.RewardFields)
        {
            var combo = Reward(key); Expect(combo.ItemTemplate is not null && combo.Items.Count == (key == "RewardPrev1" ? 50 : 49),"Source image reward list missing.");
            foreach (var choice in MapLevelOptions.Rewards)
            {
                combo.SelectedItem = combo.Items.Cast<object>().Single(row => Get<string>(row,"ID") == choice.Value);
                Expect(Get<string>(combo.SelectedItem,"ID") == choice.Value && (choice.Icon is null || Get<BitmapSource>(combo.SelectedItem,"Icon").IsFrozen),"Actual reward selection differs/lacks original image.");
            }
        }
        Exact(); Expect(doc.GetHistory().Undo.Count == history,"Draft reward selection edited live data.");
        Number("GameTicks").Text = "Pending"; LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Expect(Number("GameTicks").Text == "Pending","Language discarded number draft."); LocalizationService.SetLanguage(language);
        Named<Button>("LevelOptionsDiscard").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(Number("GameTicks").Text == "0" && Get<string>(Reward("RewardPrev1").SelectedItem,"ID") == "250","Discard coerced raw values.");
        Number("GameTicks").Text = "51"; Expect(!window!.ApplyOptions() && Named<TextBlock>("LevelOptionsStatus").Text == LocalizationService.Get("LevelOptions.Invalid"),"Invalid level input accepted/untranslated."); Exact();
        Number("GameTicks").Text = "50"; Number("SpecialLevelNumber").Text = "16"; Number("Soundtrack").Text = "3";
        Named<CheckBox>("LevelFlagSecret").IsChecked = true; Named<CheckBox>("LevelFlagLockPossession").IsChecked = false;
        var next = Reward("RewardNext3"); next.SelectedItem = next.Items.Cast<object>().Single(row => Get<string>(row,"ID") == "51");
        Named<Button>("LevelOptionsApply").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var values = doc.CreateMapLevelOptionsDraft().Values;
        Expect(values["Options"] == "39" && values["Options2"] == "192" && values["GameTicks"] == "50" && values["SpecialLevelNumber"] == "16"
            && values["RewardPrev1"] == "250" && values["RewardNext3"] == "51" && doc.GetHistory().Undo.Count == history+1,"Actual level Apply not one atomic/unknown-preserving edit.");
        Expect(window.ApplyOptions() && doc.GetHistory().Undo.Count == history+1,"Level no-op consumed history.");
        var output = doc.SaveCopy(Path.Combine(directory,$"level-ui-output-{language}-{native}")); Expect(doc.CreateMapLevelOptionsDraft().Values.OrderBy(row => row.Key).SequenceEqual(NativeMapDocument.Load(output).CreateMapLevelOptionsDraft().Values.OrderBy(row => row.Key)),"Actual level save/reopen lost values.");
        foreach (var size in new[] { new Size(850,920),new Size(760,600) })
        {
            var content = (FrameworkElement)window.Content; content.Measure(size); content.Arrange(new Rect(new Point(0,0),size)); content.UpdateLayout();
            Expect(Number("GameTicks").ActualWidth >= 300 && Reward("RewardNext3").ActualWidth >= 300 && Named<Button>("LevelOptionsApply").ActualWidth > 70,"Level fields/buttons collapsed.");
            var buttonBounds=Named<Button>("LevelOptionsApply").TransformToAncestor(content).TransformBounds(new Rect(Named<Button>("LevelOptionsApply").RenderSize));
            Expect(buttonBounds.X>=0 && buttonBounds.Right<=content.ActualWidth && buttonBounds.Y>=0 && buttonBounds.Bottom<=content.ActualHeight,"Level Apply is outside the viewport.");
            if (Environment.GetEnvironmentVariable("DK2_LEVEL_OPTIONS_ARTIFACTS") is { Length: > 0 } artifacts)
            {
                Directory.CreateDirectory(artifacts); var visual = new DrawingVisual(); using (var drawing=visual.RenderOpen())
                { drawing.DrawRectangle((Brush)Application.Current.FindResource("Brush.Background"),null,new Rect(new Point(0,0),size)); }
                // Render the arranged visual directly. VisualBrush auto-layout
                // can remeasure an offscreen Window child with infinite width.
                var bitmap=new RenderTargetBitmap((int)size.Width,(int)size.Height,96,96,PixelFormats.Pbgra32); bitmap.Render(visual); bitmap.Render(content); var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var stream=new FileStream(Path.Combine(artifacts,$"level-{language}-{native}-{size.Width}.png"),FileMode.CreateNew); encoder.Save(stream);
            }
        }
        doc.Undo(); Exact(); Expect(!window.ApplyOptions() && !Named<Button>("LevelOptionsApply").IsEnabled,"Undo reused level draft authority."); doc.Redo(); doc.Undo(); Exact();
        Named<Button>("LevelOptionsDiscard").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Number("HornyTalismanPieces").Text="4";
        // Commit an actual generic field while this modeless draft is open.
        seed=doc.CreateMapLevelOptionsDraft(); var record=seed.Record!; var field=Storage(doc,seed,"Soundtrack");
        var categories=Control<ComboBox>(workspace,"CategoryPicker"); var records=Control<ListBox>(workspace,"RecordList");
        categories.SelectedItem=categories.Items.Cast<object>().Single(row=>Get<string>(row,"ID")==record.Category); records.SelectedItem=records.Items.Cast<object>().Single(row=>Get<NativeRecord>(row,"Record").Offset==record.Offset);
        Control<TabControl>(workspace,"WorkspaceTabs").SelectedIndex=1; workspace.Measure(new Size(1300,900)); workspace.Arrange(new Rect(0,0,1300,900)); workspace.UpdateLayout();
        var properties=Control<DataGrid>(workspace,"PropertyGrid"); properties.SelectedItem=properties.Items.Cast<object>().Single(row=>Get<NativeField>(row,"Field").Name==field.Name);
        properties.ScrollIntoView(properties.SelectedItem,properties.Columns[1]); properties.UpdateLayout(); properties.CurrentCell=new DataGridCellInfo(properties.SelectedItem,properties.Columns[1]); Expect(properties.BeginEdit(),"Pending level field did not enter editing."); properties.UpdateLayout();
        var pending=properties.Columns[1].GetCellContent(properties.SelectedItem) as TextBox; Expect(pending is not null,"Pending level field has no TextBox."); pending!.Text="3";
        Expect(!window.ApplyOptions() && doc.CreateMapLevelOptionsDraft().Values["Soundtrack"]=="3" && doc.CreateMapLevelOptionsDraft().Values["HornyTalismanPieces"]=="0"
            && Number("HornyTalismanPieces").Text=="4" && doc.GetHistory().Undo.Count==history+1,"Pending commit reused whole-level authority/lost draft."); doc.Undo(); Exact();
        Named<Button>("LevelOptionsDiscard").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Number("GameTicks").Text="49";
        Expect(MemoryMarshal.TryGetArray(record.File.Data,out var backing),"Raw level fixture unavailable."); var offset=backing.Offset+record.Offset+Storage(doc,seed,"Options").Offset; var raw=backing.Array![offset]; backing.Array[offset]^=1;
        Expect(!window.ApplyOptions() && !Named<Button>("LevelOptionsApply").IsEnabled && Named<TextBlock>("LevelOptionsStatus").Text==LocalizationService.Get("LevelOptions.Stale"),"Raw changes reused level draft."); backing.Array[offset]=raw;
        Named<Button>("LevelOptionsDiscard").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(window.ApplyOptions(),"Refreshed level no-op failed."); Exact(); window.Close();
        window=workspace.CreateLevelOptionsWindow()!; workspace.Document=null; workspace.Document=doc; Expect(!window.ApplyOptions(),"Replaced/restored level owner reused authority."); window.Close();
        CheckHistoricalLevelUI(native,language,directory);
        workspace.Document=null; shell.Close(); Expect(!command.IsEnabled && workspace.CreateLevelOptionsWindow() is null,"Closed shell retained level authority.");
        Expect(disk.All(row=>File.ReadAllBytes(row.Key).AsSpan().SequenceEqual(row.Value)),"Level UI edited original disk input.");
        Console.WriteLine($"PASS WPF level options, twelve flags, original rewards/images, whole drafts, pending/stale/history and historical guards {language} {(native ? "native" : "project")}");
    }
    private static void CheckHistoricalLevelUI(bool native,string language,string directory)
    {
        var doc=NativeMapDocument.CreateNewMap(new("OldLevelUI","OldLevelUI",24,24,NativeFormat:native)); var entry=doc.SaveCopy(Path.Combine(directory,$"old-level-ui-{language}-{native}"));
        if(native)
        {
            // The loader already rejects future native versions. Also exercise
            // the panel guard when a loaded descriptor is changed in memory.
            var bytes=File.ReadAllBytes(entry); bytes[4]=9; File.WriteAllBytes(entry,bytes);
            var refused=false; try { NativeMapDocument.Load(entry); } catch(InvalidDataException) { refused=true; } Expect(refused,"Future native version loaded.");
            Expect(MemoryMarshal.TryGetArray(doc.MainFile.Data,out var backing),"Future-layout fixture backing unavailable."); backing.Array![backing.Offset+4]=9;
        }
        else
        {
            var file=LegacySectionFile.Read(entry); var info=file.Sections.Single(row=>row.Signature==NativeMapDocument.LegacyInfo);
            var replacement=new LegacySection(info.Signature,4,info.Payload[..LegacyProjectSchemas.Size("MAPPROPERTIESV4")]);
            var old=Path.Combine(directory,$"old-level-ui-{language}.dkm"); File.WriteAllBytes(old,LegacySectionFile.Create(file.Sections.Select(row=>row==info?replacement:row)).ToArray()); doc=NativeMapDocument.Load(old);
        }
        var workspace=new EditorWorkspace { Document=doc }; var window=workspace.CreateLevelOptionsWindow()!;
        Expect(((CheckBox)window.FindName("LevelFlagSecret")).IsEnabled==!native && !((CheckBox)window.FindName("LevelFlagGem")).IsEnabled
            && !((TextBox)window.FindName("LevelOptionGameTicks")).IsEnabled && ((Button)window.FindName("LevelOptionsApply")).IsEnabled==!native,"Historical/padding fields guessed as editable options.");
        if(!native) { ((CheckBox)window.FindName("LevelFlagSecret")).IsChecked=true; Expect(window.ApplyOptions() && doc.CreateMapLevelOptionsDraft().Values.Count==1,"Partial historical flags cannot apply."); doc.Undo(); }
        else Expect(!window.ApplyOptions(),"Future native layout edited.");
        window.Close(); workspace.Document=null;
    }
    private static void CheckLevelRewardPixels()
    {
        var repo=AppContext.BaseDirectory; while(!Directory.Exists(Path.Combine(repo,"Source"))) repo=Path.GetDirectoryName(repo)!;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var rc=Encoding.GetEncoding(1252).GetString(File.ReadAllBytes(Path.Combine(repo,"Source/Resources/Editor.rc")));
        var paths=Regex.Matches(rc,"(?m)^(\\d+)\\s+BITMAP\\s+\"([^\"]+)\"").ToDictionary(match=>int.Parse(match.Groups[1].Value),match=>match.Groups[2].Value.Replace("\\\\","/"));
        var source=File.ReadAllText(Path.Combine(repo,"Source/Sources/Datas/Datas_Game.c")); var body=Regex.Match(source,@"(?s)Rewards\[\]=\{(.*?)\};").Groups[1].Value;
        var entries=Regex.Matches(body,@"\{(0x[0-9A-Fa-f]+),\s*0x[0-9A-Fa-f]+,\s*(\d+),").Select(match=>(ID:Convert.ToUInt32(match.Groups[1].Value[2..],16),Resource:int.Parse(match.Groups[2].Value))).Where(row=>row.ID!=0).ToArray();
        Expect(entries.Length==48,"Independent source rewards not complete.");
        foreach(var row in entries)
        {
            Expect(MapLevelOptions.Rewards.Single(reward=>reward.Value==row.ID.ToString()).Icon==row.Resource,"Source reward image identity differs.");
            using var stream=File.OpenRead(Path.Combine(repo,"Source",paths[row.Resource])); BitmapSource original=BitmapFrame.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
            if(original.PixelWidth!=32 || original.PixelHeight!=32) original=new TransformedBitmap(original,new ScaleTransform(32d/original.PixelWidth,32d/original.PixelHeight));
            byte[] Pixels(BitmapSource input) { var converted=new FormatConvertedBitmap(input,PixelFormats.Bgra32,null,0); var bytes=new byte[32*32*4]; converted.CopyPixels(bytes,32*4,0); return bytes; }
            Expect(Pixels(original).AsSpan().SequenceEqual(Pixels(OriginalObjectImages.Get(row.Resource))),"Embedded reward pixels differ from source.");
        }
        Console.WriteLine("      original reward image comparisons=48");
    }
}
