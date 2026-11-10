using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
using Microsoft.Win32;

namespace DK2MapEditor.App.Editing;

public partial class ThumbnailWindow : Window
{
    private NativeMapDocument? document;
    private readonly Action<MapThumbnailOptions>? optionsChanged;
    private readonly EditorFileDialogSession fileDialogs;
    private bool updating;
    private bool closed;
    private ThumbnailRaster? filtered;
    private string? statusKey;
    private string? statusDetail;
    public MapThumbnailOptions Options { get; private set; }
    public NativeMapDocument? Document => document;
    public ThumbnailRaster? Preview { get; private set; }
    public IReadOnlyList<CheckBox> VisibilityControls { get; private set; } = [];
    public ThumbnailWindow(NativeMapDocument source,MapThumbnailOptions options,Action<MapThumbnailOptions>? changed = null,EditorFileDialogSession? fileDialogs=null)
    {
        this.fileDialogs=fileDialogs??new();
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(options); options.Validate();
        document = source; Options = options; optionsChanged = changed;
        InitializeComponent();
        var checks = new List<CheckBox>();
        CheckBox Check(ThumbnailVisibility choice)
        {
            var check = new CheckBox { Tag = choice,Margin = new Thickness(4,3,12,3),IsChecked = ((choice.PlayerScoped ? options.PlayersFlags : options.WorldFlags) & choice.Mask) != 0 };
            check.SetResourceReference(ContentControl.ContentProperty,"Thumbnail.Filter." + choice.Key);
            check.Click += Visibility_Click; checks.Add(check); return check;
        }
        foreach (var choice in MapThumbnail.WorldChoices) WorldFlagsPanel.Children.Add(Check(choice));
        for (var column = 0; column < 5; column++) PlayerFlagsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(column == 0 ? 120 : 1,column == 0 ? GridUnitType.Pixel : GridUnitType.Star) });
        for (var row = 0; row < 8; row++) PlayerFlagsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        void Add(UIElement item,int row,int column) { Grid.SetRow(item,row); Grid.SetColumn(item,column); PlayerFlagsGrid.Children.Add(item); }
        for (var column = 0; column < 4; column++)
        {
            var label = new TextBlock { Margin = new Thickness(4),TextWrapping = TextWrapping.Wrap };
            label.SetResourceReference(TextBlock.TextProperty,"Thumbnail.Filter." + MapThumbnail.PlayerCategories[column]); Add(label,0,column + 1);
        }
        for (var owner = 1; owner <= 7; owner++)
        {
            var label = new TextBlock { Margin = new Thickness(4),VerticalAlignment = VerticalAlignment.Center };
            label.SetResourceReference(TextBlock.TextProperty,"Owner." + owner); Add(label,owner,0);
            var choices = MapThumbnail.PlayerChoices(owner);
            for (var column = 0; column < 4; column++)
            {
                var check = Check(choices[column]); check.ClearValue(ContentControl.ContentProperty); check.Content = null;
                check.SetResourceReference(ToolTipProperty,"Thumbnail.Filter." + choices[column].Key); Add(check,owner,column + 1);
            }
        }
        VisibilityControls = checks.AsReadOnly();
        updating = true;
        SizePicker.SelectedIndex = options.PreviewSize == 144 ? 0 : 1;
        KeepProportions.IsChecked = options.KeepProportions; UseColourKey.IsChecked = options.UseColourKey;
        HiddenColourInput.Text = Rgb(options.HiddenColour); updating = false;
        RenderOptions.SetBitmapScalingMode(PreviewImage,BitmapScalingMode.NearestNeighbor);
        document.Changed += DocumentChanged;
        LocalizationService.LanguageChanged += LanguageChanged;
        Closed += (_,_) =>
        {
            closed=true;
            if (document is not null) document.Changed -= DocumentChanged;
            LocalizationService.LanguageChanged -= LanguageChanged; document = null; filtered = null; Preview = null; PreviewImage.Source = null;
            SaveButton.IsEnabled = RefreshButton.IsEnabled = ApplyColourButton.IsEnabled = false;
            foreach (var check in VisibilityControls) check.IsEnabled = false;
            SizePicker.IsEnabled = KeepProportions.IsEnabled = UseColourKey.IsEnabled = HiddenColourInput.IsEnabled = false;
        };
        TryRefreshPreview();
    }
    public void BindDocument(NativeMapDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if(closed)throw new InvalidOperationException("Thumbnail window is closed.");
        if(ReferenceEquals(document,source))return;
        if(document is not null)document.Changed-=DocumentChanged;
        document=source;document.Changed+=DocumentChanged;filtered=null;
        var invalidColour=statusKey=="Thumbnail.InvalidColour";
        if(TryRefreshPreview()&&!invalidColour)ClearStatus();
    }
    private static string Rgb(uint colour) => $"#{(byte)colour:X2}{(byte)(colour >> 8):X2}{(byte)(colour >> 16):X2}";
    private bool CommitColour()
    {
        var text = HiddenColourInput.Text.Trim(); if (text.StartsWith('#')) text = text[1..];
        if (text.Length != 6 || !uint.TryParse(text,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out var rgb))
        { SetStatus("Thumbnail.InvalidColour"); return false; }
        var colour = (rgb >> 16) | (rgb & 0xFF00) | ((rgb & 255) << 16);
        if (colour != Options.HiddenColour) ChangeOptions(Options with { HiddenColour = colour },refilter: true);
        HiddenColourInput.Text = Rgb(colour); ClearStatus(); return true;
    }
    private void ChangeOptions(MapThumbnailOptions options,bool refilter)
    {
        if (document is null) throw new InvalidOperationException("Thumbnail source is closed.");
        options.Validate(); Options = options; optionsChanged?.Invoke(options);
        if (refilter) filtered = null;if(TryRefreshPreview())ClearStatus();
    }
    private void Visibility_Click(object sender,RoutedEventArgs e)
    {
        if (document is null || updating || sender is not CheckBox { Tag: ThumbnailVisibility choice } check) return;
        var flags = choice.PlayerScoped ? Options.PlayersFlags : Options.WorldFlags;
        flags = check.IsChecked == true ? flags | choice.Mask : flags & ~choice.Mask;
        ChangeOptions(choice.PlayerScoped ? Options with { PlayersFlags = flags } : Options with { WorldFlags = flags },true);
    }
    private void Size_Changed(object sender,SelectionChangedEventArgs e)
    {
        if (document is null || updating || SizePicker.SelectedItem is not ComboBoxItem { Tag: string value }) return;
        ChangeOptions(Options with { PreviewSize = int.Parse(value,CultureInfo.InvariantCulture) },false);
    }
    private void Composition_Click(object sender,RoutedEventArgs e)
    {
        if (document is null || updating) return;
        ChangeOptions(Options with { KeepProportions = KeepProportions.IsChecked == true,UseColourKey = UseColourKey.IsChecked == true },false);
    }
    private void ApplyColour_Click(object sender,RoutedEventArgs e) { if (document is not null) CommitColour(); }
    private void Refresh_Click(object sender,RoutedEventArgs e) { if (document is not null && CommitColour()) { filtered = null; TryRefreshPreview(); } }
    private void DocumentChanged(object? sender,EventArgs e) { filtered = null; TryRefreshPreview(); }
    private bool TryRefreshPreview()
    {
        try{RefreshPreview();SaveButton.IsEnabled=true;if(statusKey=="Thumbnail.RenderFailed")ClearStatus();return true;}
        catch(Exception error)when(error is IOException or InvalidDataException or ArgumentException or InvalidOperationException or FormatException or OverflowException)
        {
            filtered=null;Preview=null;PreviewImage.Source=null;SaveButton.IsEnabled=false;
            SetStatus("Thumbnail.RenderFailed",error.Message);return false;
        }
    }
    public void SetKeepProportions(bool value)
    {
        if (document is null || Options.KeepProportions == value) return;
        updating = true; KeepProportions.IsChecked = value; updating = false;
        ChangeOptions(Options with { KeepProportions = value },false);
    }
    public void RefreshPreview()
    {
        if (document is null) throw new InvalidOperationException("Thumbnail source is closed.");
        filtered ??= MapThumbnail.Filter(document,Options);
        Preview = MapThumbnail.Compose(filtered,Options);
        var bitmap = BitmapSource.Create(Preview.Width,Preview.Height,96,96,PixelFormats.Bgra32,null,Preview.Bgra.ToArray(),Preview.Width * 4);
        bitmap.Freeze(); PreviewImage.Source = bitmap;
    }
    /// <summary>The same validated, non-overwriting export used after file selection.</summary>
    public bool SaveTo(string path)
    {
        if (document is null) throw new InvalidOperationException("Thumbnail source is closed.");
        if (!CommitColour()) return false;
        MapThumbnail.SaveNewBmp(path,document,Options); SetStatus("Thumbnail.Saved",Path.GetFileName(path)); return true;
    }
    public EditorPreviewFileDialog CreateSaveDialog()
    {
        var location=fileDialogs.Location(EditorFileDialogKind.Thumbnail,"Thumbnail.bmp");
        return new(EditorFileDialogKind.Thumbnail,save:true){Title=LocalizationService.Get("Thumbnail.Save"),Filter=LocalizationService.Get("Thumbnail.FileFilter"),DefaultExt=".bmp",AddExtension=true,
            OverwritePrompt=true,FileName=location.FileName,InitialDirectory=location.Directory};
    }
    private void Save_Click(object sender,RoutedEventArgs e)
    {
        if (document is null || !CommitColour()) return;
        var source=document;var revision=source.EditRevision;var options=Options;
        try
        {
            var bytes=MapThumbnail.Bmp(source,options);var dialog=CreateSaveDialog();
            if(dialog.ShowDialog(this)!=true)return;
            fileDialogs.AcceptThumbnailPath(dialog.FileName);
            if(!ReferenceEquals(document,source)||source.EditRevision!=revision||Options!=options)
            {SetStatus("Thumbnail.SaveFailed",LocalizationService.Get("Thumbnail.Stale"));return;}
            ThumbnailFileOutput.SaveConfirmed(dialog.FileName,bytes);SetStatus("Thumbnail.Saved",Path.GetFileName(dialog.FileName));
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        { SetStatus("Thumbnail.SaveFailed",error.Message); }
    }
    private void LanguageChanged(object? sender,EventArgs e) => UpdateStatus();
    private void SetStatus(string key,string? detail = null) { statusKey = key; statusDetail = detail; UpdateStatus(); }
    private void ClearStatus() { statusKey = null; statusDetail = null; UpdateStatus(); }
    private void UpdateStatus() => Status.Text = statusKey is null ? "" : LocalizationService.Get(statusKey) + (statusDetail is null ? "" : "\n" + statusDetail);
}
