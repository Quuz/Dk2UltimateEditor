using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckGlobalVariables(NativeMapDocument document, string language)
    {
        var workspace = new EditorWorkspace { Document = document };
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 9;
        GlobalVariablesWindow? active = null; workspace.GlobalVariablesPresenter = window => active = window;
        void Layout(Window window, int width = 780, int height = 760)
        { var content = (FrameworkElement)window.Content; content.Measure(new Size(width, height)); content.Arrange(new Rect(0, 0, width, height)); content.UpdateLayout(); }
        void LayoutWorkspace() { workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout(); }
        GlobalVariablesWindow Open()
        {
            active?.Close(); active = null; LayoutWorkspace(); var button = Control<Button>(workspace, "DefinitionGlobalVariables");
            Expect(button.IsEnabled && button.Visibility == Visibility.Visible, "Shared global-variable toolbar command unavailable.");
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(active is not null, "Actual shared-global command did not open."); Layout(active!); return active!;
        }
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes) => Expect(document.Files.Count == bytes.Count && document.Files.All(file => bytes[file].AsSpan().SequenceEqual(file.Data.Span)), "Shared UI changed cancel/private/undo bytes.");
        string Value(string family, int index, string field) => document.GlobalDefinitionValues(document.GlobalDefinitionRows().Single(row => row.Family == family && row.Index == index)).Single(value => value.Name == field).Value;
        string Prefix(string family) => family switch { "Terrains" => "Terrain", "Rooms" => "Room", "Doors" => "Door", "Traps" => "Trap", _ => "Spell" };
        T Field<T>(GlobalVariablesWindow window, string field) where T : Control => (T)window.ActiveRowEditor!.FindName(Prefix(window.SelectedFamily) + "Property" + field);
        void Set(GlobalVariablesWindow window, string field, string value) => Field<TextBox>(window, field).Text = value;
        string Main(string family) => family switch { "Terrains" => "Damage", "Rooms" => "TorchIntensity", "Doors" => "Cost", "Traps" => "GoldCost", _ => "ManaCost" };
        void Choose(GlobalVariablesWindow window, string field, string value)
        { var combo = Field<ComboBox>(window, field); combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "ID") == value); }
        void Render(GlobalVariablesWindow window, string state)
        {
            if (Environment.GetEnvironmentVariable("DK2_GLOBAL_VARIABLES_UI_ARTIFACTS") is not { Length: > 0 } artifacts) return; Directory.CreateDirectory(artifacts);
            foreach (var width in new[] { 560, 780 }) foreach (var bottom in new[] { false, true })
            {
                var height = width == 560 ? 480 : 760; Layout(window, width, height);
                var scroll = (ScrollViewer)window.ActiveRowEditor!.FindName(Prefix(window.SelectedFamily) + "PropertiesScroll");
                if (bottom) scroll.ScrollToEnd(); else scroll.ScrollToHome(); ((FrameworkElement)window.Content).UpdateLayout();
                Expect(scroll.ViewportHeight > 40 && scroll.ExtentWidth <= scroll.ViewportWidth + 1, "Shared page has no reachable body or overflows horizontally.");
                if(bottom)
                {
                    var field=window.SelectedFamily switch{"Terrains"=>GlobalTerrainControls.Fields.Last(),"Rooms"=>GlobalRoomControls.Fields.Last(),"Doors"=>GlobalDoorControls.Fields.Last(),"Traps"=>GlobalTrapControls.MainFields.Last(),_=>GlobalSpellControls.MainFields.Last()};
                    var last=Field<Control>(window,field);var bounds=last.TransformToAncestor(scroll).TransformBounds(new Rect(new Point(),last.RenderSize));
                    Expect(bounds.Top>=0&&bounds.Bottom<=scroll.ActualHeight+1,"Last shared global field unreachable:"+window.SelectedFamily);
                }
                var apply = (Button)window.FindName("GlobalVariablesApply"); var footer = apply.TransformToAncestor((FrameworkElement)window.Content).TransformBounds(new Rect(new Point(), apply.RenderSize));
                Expect(apply.Visibility == Visibility.Visible && apply.RenderSize.Height > 0 && footer.Top >= 0 && footer.Bottom <= height + 1, "Common footer unreachable at minimum size.");
                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render((FrameworkElement)window.Content);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts, $"globals-{state}-{language}-{(document.IsLegacyProject ? "project" : "native")}-{width}-{bottom}.png")); encoder.Save(output);
            }
        }
        var initial = Snapshot(); var history = document.GetHistory().Undo.Count; var window = Open();
        Expect(((TabControl)window.FindName("GlobalVariablesPages")).Items.Count == 5, "Shared source pages incomplete.");
        foreach (var family in GlobalVariablesSession.Families)
        {
            var count = GlobalDefinitionDefaults.Catalogue(family).Count;
            for (var index = 0; index < count; index++)
            {
                Expect(window.Select(family, index), "Source row/page default switch failed."); Layout(window);
                Expect(((ComboBox)window.FindName("GlobalVariablesRows")).Items.Count == count, "Shared source row list incomplete.");
                Expect(((Button)window.ActiveRowEditor!.FindName(Prefix(family) + "PropertiesApply")).Visibility == Visibility.Collapsed
                    && ((Button)window.ActiveRowEditor.FindName(Prefix(family) + "PropertiesCancel")).Visibility == Visibility.Collapsed, "Shared page exposed separate commit/cancel commands.");
            }
        }
        Expect(window.ApplyVariables(), "Shared default no-op failed."); Exact(initial); Expect(document.GetHistory().Undo.Count == history, "Opening/switching 98 rows materialized live history.");
        foreach (var family in GlobalVariablesSession.Families)
        { Expect(window.Select(family, 0), "Shared page switch failed."); Set(window, Main(family), "123"); }
        foreach (var family in GlobalVariablesSession.Families)
        {
            Expect(window.Select(family, 0) && Field<TextBox>(window, Main(family)).Text == "123", "Private page value was lost on return.");
            Render(window, family.ToLowerInvariant()); Exact(initial);
        }
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Expect(Field<TextBox>(window, "ManaCost").Text == "123", "Shared translation lost pending page."); LocalizationService.SetLanguage(language);
        Set(window, "ManaCost", "bad"); Expect(!window.Select("Rooms", 1) && window.SelectedFamily == "Spells" && !window.ApplyVariables(), "Invalid page switch partly committed or changed selection."); Exact(initial);
        Expect(window.Reset(false), "Current reset could not discard invalid pending controls."); Exact(initial);
        Set(window, "ManaCost", "321");
        var two = GlobalProjectiles.Options.First(option => option.Form == 0); Choose(window, "ShotType", two.ID.ToString(CultureInfo.InvariantCulture)); Choose(window, "UpgShotType", two.ID.ToString(CultureInfo.InvariantCulture));
        Window? nested = null; window.OptionsPresenter = options => { nested = options; Layout(options, 600, 370); };
        foreach (var slot in new[] { "Shot", "Upgrade" })
        {
            nested = null; Field<Button>(window, (slot == "Shot" ? "ShotType" : "UpgShotType") + "Options").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(nested is GlobalSpellOptionsWindow && !window.Select("Doors", 0) && !window.Reset(true) && !window.ApplyVariables(), "Nested shared child did not block row/reset/common Apply.");
            var options = (GlobalSpellOptionsWindow)nested!; ((TextBox)options.FindName("SpellOptionsData1")).Text = two.Signed1 ? "-9" : "456";
            ((TextBox)options.FindName("SpellOptionsData2")).Text = two.Signed2 ? "-7" : "654";
            Expect(options.AcceptOptions(), "Shared spell child OK failed."); Exact(initial);
        }
        Expect(window.Select("Doors", 1), "Child completion did not release shared selection."); Set(window, "Cost", "234");
        Expect(window.Select("Doors", 0) && Field<TextBox>(window, "Cost").Text == "123", "Per-page row state was lost.");
        Expect(window.ApplyVariables(), "Five-family/multiple-row/two-slot common Apply failed.");
        foreach (var family in GlobalVariablesSession.Families) Expect(Value(family, 0, Main(family)) == (family == "Spells" ? "321" : "123"), "Common Apply lost family values.");
        Expect(Value("Doors", 1, "Cost") == "234" && document.GetHistory().Undo.Count == history + 1, "Multiple-row shared Apply split Undo.");
        Expect(Value("Spells", 0, "ShotData1") == Value("Spells", 0, "UpgShotData1"), "Shared Apply lost one nested slot.");
        var applied = Snapshot(); Expect(window.ApplyVariables(), "Shared second Apply no-op failed."); Exact(applied); Expect(document.GetHistory().Undo.Count == history + 1, "Shared second Apply consumed Undo.");
        window.Close(); document.Undo(); Exact(initial); document.Redo(); Exact(applied); document.Undo(); Exact(initial);

        var cancel = Open(); foreach (var family in GlobalVariablesSession.Families) { Expect(cancel.Select(family, 0), "Canceled family switch failed."); Set(cancel, Main(family), "222"); }
        Expect(cancel.Reset(true) && cancel.Select("Traps", 0), "Private all-page reset/switch failed."); Set(cancel, "GoldCost", "333");
        ((Button)cancel.FindName("GlobalVariablesCancel")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(initial); Expect(!cancel.ApplyVariables(), "Canceled shared context accepted Apply.");
        var colour = Open(); colour.ColourSelector = _ => { Expect(!colour.ApplyVariables() && !colour.Reset(true) && !colour.Select("Rooms", 0), "Shared native colour lifetime was not modal."); return 0x123456; };
        Expect(((GlobalTerrainWindow)colour.ActiveRowEditor!).ChooseColour("TerrainLight"), "Shared colour composition failed."); Exact(initial); colour.Close(); Exact(initial);

        var stale = Open(); Set(stale, "Damage", "222"); document.SetGlobalDefinitionValues([new("Rooms", 0, "Cost", "333")]); var changed = Snapshot();
        Expect(!stale.Select("Spells", 0) && !stale.Reset(true) && !stale.ApplyVariables(), "Stale shared context accepted a write."); Exact(changed); stale.Close(); document.Undo(); Exact(initial);
        foreach(var guard in new[]{"refused","input","owner","source","closed"})
        {
            var owner=true;GlobalVariablesWindow guarded=null!;
            guarded=new(document,()=>owner,()=>
            {
                Expect(!guarded.ApplyVariables()&&!guarded.Select("Rooms",0)&&!guarded.Reset(true),"Common global pending commit allowed reentry.");
                if(guard=="input")Set(guarded,"Damage","222");else if(guard=="owner")owner=false;else if(guard=="source")document.SetGlobalDefinitionValues([new("Rooms",0,"Cost","777")]);else if(guard=="closed")guarded.Close();return guard!="refused";
            });Set(guarded,"Damage","111");Expect(!guarded.ApplyVariables(),"Common global Apply accepted invalid pending commit:"+guard);
            if(guard=="source"){Expect(Value("Rooms",0,"Cost")=="777","Common global Apply overwrote workspace pending edit.");document.Undo();}
            if(guard=="input")Expect(Field<TextBox>(guarded,"Damage").Text=="222","Common global Apply lost newer input.");guarded.Close();Exact(initial);
        }
        var late = Open(); Expect(late.Select("Spells", 0), "Late child page failed."); Choose(late, "ShotType", two.ID.ToString(CultureInfo.InvariantCulture));
        Window? lateChild = null; late.OptionsPresenter = options => lateChild = options;
        Field<Button>(late, "ShotTypeOptions").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(lateChild is GlobalSpellOptionsWindow, "Late shared child missing.");
        workspace.Document = null; Expect(!late.ApplyVariables() && !((GlobalSpellOptionsWindow)lateChild!).AcceptOptions(), "Owner close accepted shared parent/child."); Exact(initial);
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} shared global variables:5 pages/98 rows, private row/page switches, invalid reset, multiple rows/two slots, Cancel, one Apply/Undo, no-op, translation, modal colour/options, stale owner");
    }
}
