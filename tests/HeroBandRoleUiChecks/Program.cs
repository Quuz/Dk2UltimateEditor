using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static class Program
{
    private static string artifacts = "", fixtures = "";
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length != 2 || Directory.Exists(args[0]) || !Directory.Exists(args[1]))
            throw new ArgumentException("Require fresh artifacts and accepted historical fixtures.");
        artifacts = args[0]; fixtures = args[1]; Directory.CreateDirectory(artifacts);
        var app = new DK2MapEditor.App.App(); app.InitializeComponent(); app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            foreach (var language in new[] { "en", "ru" })
                foreach (var version in new[] { 1, 5 }) CheckRoles(language, version);
            Console.WriteLine("HERO BAND ROLE UI CHECKS PASSED: 4"); return 0;
        }
        catch (Exception error) { Console.WriteLine(error); return 1; }
        finally { app.Shutdown(); }
    }
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    private static T At<T>(FrameworkElement owner, string name) where T : FrameworkElement =>
        owner.FindName(name) as T ?? throw new InvalidOperationException("Missing actual control " + name);
    private static void Flush()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
    private static void Show(Window window)
    {
        window.ShowInTaskbar = false; window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -10000; window.Top = -10000; window.Show(); Flush();
        Check(window.IsLoaded, "Actual role window not loaded.");
    }
    private static void Captions(HeroBandValuePanel panel, string language)
    {
        // Literal expectations from the original three radios in Strings.h and
        // THING_OPTIONSBANDS, independent of the resource keys under test.
        var expected = language == "ru"
            ? new[] { "Независимый участник", "Лидер отряда", "Помогает лидеру" }
            : new[] { "Independent member", "Band leader", "Assist the leader" };
        var roles = new uint[] { 0, 2, 4 };
        for (var i = 0; i < roles.Length; i++)
            Check(At<RadioButton>(panel, "HeroRole" + roles[i]).Content?.ToString() == expected[i],
                "Wrong actual band role caption: " + language + "/" + roles[i]);
        var death = language == "ru" ? "Мгновенная смерть" : "Die instantly";
        Check(At<CheckBox>(panel, "HeroOptionBit4").Content is TextBlock text && text.Text == death,
            "Distinct hero option4 lost its instant-death meaning.");
        Check(LocalizationService.Get("Batch.CreatureBit.4") == death,
            "Ordinary creature option4 was globally relabeled as a band role.");
    }
    private static void Render(Window window, string tag)
    {
        Flush(); var content = (FrameworkElement)window.Content;
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth), (int)Math.Ceiling(content.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        var background = new DrawingVisual(); using (var dc = background.RenderOpen()) dc.DrawRectangle(window.Background, null, new Rect(0, 0, content.ActualWidth, content.ActualHeight));
        bitmap.Render(background); bitmap.Render(content);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(artifacts, tag + ".png")); encoder.Save(file);
    }
    private static void CheckRoles(string language, int version)
    {
        LocalizationService.SetLanguage(language); var tag = language + "-v" + version + "-role-semantics";
        var path = Path.Combine(artifacts, tag + ".dkm");
        File.Copy(Path.Combine(fixtures, $"v{version}-preview/fixture/historical.dkm"), path);
        var document = NativeMapDocument.Load(path);
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var history = document.GetHistory();
        void Exact() => Check(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && document.GetHistory().ChangeCount == history.ChangeCount,
            "Caption/role preview or Cancel changed the original project.");
        var band = document.Records().Where(NativeMapDocument.IsBand).ElementAt(1);
        var parent = new HeroBandWindow(document, band, () => true, () => true);
        try
        {
            Show(parent); Captions(At<HeroBandValuePanel>(parent, "HeroBandTemplate"), language); Render(parent, tag + "-parent"); Exact();
        }
        finally { parent.Close(); Flush(); }
        using var session = document.CreateEditorHeroBandSession(band);
        var original = session.Members[1].Values;
        var child = new HeroBandMemberWindow(document, session, 1, () => true);
        try
        {
            Show(child); var panel = At<HeroBandValuePanel>(child, "HeroMemberFields"); Captions(panel, language);
            Check(panel.ReadValues() == original, "Opening captions changed source member flags.");
            foreach (var role in new uint[] { 0, 4, 2 })
            {
                At<RadioButton>(panel, "HeroRole" + role).IsChecked = true; Flush(); var values = panel.ReadValues();
                Check((values.Options2 & 6) == role && values.Options == original.Options,
                    "Band role radio changed the independent hero-options word.");
                Check((values.Options2 & 57) == (original.Options2 & 57), "Changing role changed named behavior flags.");
            }
            At<RadioButton>(panel, "HeroRole4").IsChecked = true;
            At<CheckBox>(panel, "HeroOptionBit4").IsChecked = true; Flush(); var selected = panel.ReadValues();
            Check((selected.Options2 & 6) == 4 && (selected.Options & 4) == 4,
                "Assist-leader role and instant-death hero option cannot coexist.");
            var alternate = language == "en" ? "ru" : "en";
            LocalizationService.SetLanguage(alternate); Flush(); Captions(panel, alternate);
            Check(panel.ReadValues() == selected, "Language switch changed selected roles/options.");
            LocalizationService.SetLanguage(language); Flush(); Captions(panel, language); Render(child, tag + "-member");
            Check(child.ApplyMember(), "Actual nested Apply failed.");
            Check(session.Members[1].Values == selected, "Nested Apply lost role/options words."); Exact();
        }
        finally { child.Close(); Flush(); }
        session.Dispose(); Exact(); Console.WriteLine("PASS " + tag);
    }
}
