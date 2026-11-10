using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class CameraPathWindow : Window
{
    private sealed record PathView(CameraPathChoice Choice, string Label);
    private Action<CameraPathChoice>? apply;
    private CameraPathChoice? selected;
    private readonly long initialID;
    private bool updating;
    private bool closed;
    private readonly string automationIdentity=Guid.NewGuid().ToString("N");
    private long automationRevision;
    internal string AutomationToken => automationIdentity+":"+automationRevision;
    internal object ReadAutomationState() => new {
        title=Title,visible=IsVisible,closed,applied=Applied,query=SearchBox.Text,
        choices=PathList.Items.Cast<PathView>().Select(view=>new {id=view.Choice.ID,label=view.Label}).ToArray(),
        selectedPathID=selected?.ID,ceilingMode=selected?.CeilingMode,canApply=ApplyButton.IsEnabled,
        pathTitle=PathTitle.Text,description=DescriptionText.Text,mode=ModeText.Text,status=StatusText.Text
    };
    internal object AutomationCommand(string command,string? query,int? pathID)
    {
        if(closed || !IsVisible || !IsEnabled)throw new InvalidOperationException("The camera library is not available.");
        switch(command)
        {
            case "Inspect":break;
            case "Search":
                if(query is null || query.Length>256)throw new ArgumentException("Camera search is limited to 256 characters.");
                SearchBox.Text=query;break;
            case "Select":
                var view=PathList.Items.Cast<PathView>().SingleOrDefault(item=>item.Choice.ID==pathID)
                    ?? throw new ArgumentException("Choose a pathID from the current filtered choices.");
                PathList.SelectedItem=view;PathList.ScrollIntoView(view);break;
            case "Apply":Apply_Click(ApplyButton,new RoutedEventArgs());break;
            case "Cancel":CancelSelection();break;
            default:throw new ArgumentException("Unknown camera-library command.");
        }
        return ReadAutomationState();
    }
    private string? statusKey;
    public bool Applied { get; private set; }
    public CameraPathChoice? Selection => closed ? null : selected;
    public CameraPathWindow(int pathID, Action<CameraPathChoice> applySelection) : this((long)pathID, applySelection) { }
    internal CameraPathWindow(long pathID, Action<CameraPathChoice> applySelection, bool privateDraft = false)
    {
        initialID = pathID; apply = applySelection;
        InitializeComponent();
        if (privateDraft) HintText.SetResourceReference(TextBlock.TextProperty, "CameraPath.ParentHint");
        selected = CameraPaths.Choices.FirstOrDefault(choice => choice.ID == pathID);
        RefreshChoices(); RefreshDetails();
        LocalizationService.LanguageChanged += LanguageChanged;
        Closed += (_, _) =>
        {
            closed = true; automationRevision++; apply = null; selected = null; Diagram.Source = null; PathList.ItemsSource = null;
            ApplyButton.IsEnabled = false; LocalizationService.LanguageChanged -= LanguageChanged;
        };
    }
    public static BitmapSource LoadDiagram(int pathID)
    {
        if (!CameraPaths.Choices.Any(choice => choice.ID == pathID)) throw new ArgumentOutOfRangeException(nameof(pathID));
        using var stream = typeof(CameraPathWindow).Assembly.GetManifestResourceStream("Legacy.CameraPath.Chemin n°" + pathID.ToString("D3", CultureInfo.InvariantCulture))
            ?? throw new InvalidDataException("The preserved camera diagram is missing.");
        var frame = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad); frame.Freeze(); return frame;
    }
    private void RefreshChoices()
    {
        if (closed) return;
        updating = true;
        try
        {
            var search = SearchBox.Text.Trim();
            var choices = CameraPaths.Choices.Where(choice => search.Length == 0 || choice.ID.ToString("D3", CultureInfo.InvariantCulture).Contains(search, StringComparison.OrdinalIgnoreCase)
                || choice.Description(LocalizationService.CurrentLanguage).Contains(search, StringComparison.OrdinalIgnoreCase))
                .Select(choice => new PathView(choice, choice.ID.ToString("D3", CultureInfo.InvariantCulture))).ToArray();
            PathList.ItemsSource = choices; PathList.SelectedItem = choices.FirstOrDefault(view => view.Choice.ID == selected?.ID);
            if (PathList.SelectedItem is { } current) PathList.ScrollIntoView(current);
        }
        finally { updating = false; }
    }
    private void RefreshDetails()
    {
        if (closed) return;
        PathTitle.Text = string.Format(CultureInfo.CurrentCulture, LocalizationService.Get("CameraPath.Number"), selected?.ID ?? initialID);
        Diagram.Source = selected is null ? null : LoadDiagram(selected.ID);
        ModeText.Text = selected is null ? "" : LocalizationService.Get(selected.ShowCell ? "CameraPath.Cell" : "CameraPath.Area");
        DescriptionText.Text = selected?.Description(LocalizationService.CurrentLanguage) ?? LocalizationService.Get("CameraPath.Unknown");
        ApplyButton.IsEnabled = selected is not null && apply is not null;
        StatusText.Text = statusKey is null ? "" : LocalizationService.Get(statusKey);
    }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (PathList is not null && !updating) { automationRevision++; RefreshChoices(); } }
    private void Path_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updating || closed || PathList.SelectedItem is not PathView view) return;
        automationRevision++; selected = view.Choice; statusKey = null; StatusText.ToolTip = null; RefreshDetails();
    }
    private void LanguageChanged(object? sender, EventArgs e) { automationRevision++; RefreshChoices(); RefreshDetails(); }
    public bool ApplySelection()
    {
        if (closed || selected is null || apply is null) return false;
        try { apply(selected); Applied = true; return true; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException or FormatException or OverflowException)
        { automationRevision++; statusKey = "CameraPath.Failed"; StatusText.ToolTip = exception.Message; RefreshDetails(); return false; }
    }
    private void Apply_Click(object sender, RoutedEventArgs e) { if (ApplySelection()) Close(); }
    public void CancelSelection() => Close();
    private void Cancel_Click(object sender, RoutedEventArgs e) => CancelSelection();
}
