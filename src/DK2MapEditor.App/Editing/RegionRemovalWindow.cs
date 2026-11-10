using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class RegionRemovalWindow : Window, IDisposable
{
    private readonly IEditorConfirmationRequest request;
    private readonly Func<bool> ownerCurrent;
    private readonly CheckBox remember = new() { Margin = new(4, 14, 4, 10) };
    private readonly Button yes = new() { IsDefault = true }, no = new(), cancel = new();
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new(4) };
    private bool closed, closing;
    private bool advanced;
    public Exception? Failure { get; private set; }
    public IEditorConfirmationRequest Request => request;
    public RegionRemovalWindow(IEditorConfirmationRequest request, Func<bool> ownerCurrent)
    {
        this.request = request; this.ownerCurrent = ownerCurrent;
        NameScope.SetNameScope(this, new NameScope()); SetResourceReference(TitleProperty, IsScenarioPrompt?"ScenarioDelete.Title":request.Prompt==EditorConfirmationPrompt.UsedCursorRemoval?"ThingRequest.CursorTitle":request is EditorThingRequest ? "ThingRequest.Title" : "RegionRemoval.Title");
        SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        Width = 570; MinWidth = 440; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new(16) }; Content = panel;
        var message = new DockPanel(); panel.Children.Add(message);
        using (var stream = typeof(RegionRemovalWindow).Assembly.GetManifestResourceStream("Legacy.Request.Question")
            ?? throw new InvalidDataException("Original request question icon is missing."))
        {
            var icon = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad); icon.Freeze();
            message.Children.Add(new Image { Source = icon, Width = 32, Height = 32, Margin = new(4, 4, 14, 4), VerticalAlignment = VerticalAlignment.Top });
        }
        var textPanel = new StackPanel(); message.Children.Add(textPanel);
        var text = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new(4) };
        var messageKey=request.Prompt switch
        {
            EditorConfirmationPrompt.RegionArea=>"RegionRemoval.ConfirmArea",EditorConfirmationPrompt.RegionGate=>"RegionRemoval.ConfirmGate",EditorConfirmationPrompt.RegionEffect=>"RegionRemoval.ConfirmEffect",
            EditorConfirmationPrompt.UsedAreas=>"RegionRemoval.UsedAreas",EditorConfirmationPrompt.ObjectRemoval=>"ThingRequest.Confirm",
            EditorConfirmationPrompt.UsedThingRemoval=>"ThingRequest.UsedRemove",EditorConfirmationPrompt.UsedCursorRemoval=>"ThingRequest.CursorUsedRemove",
            EditorConfirmationPrompt.ScenarioPointRemoval=>request is EditorScenarioDeletionRequest {EventCount:0}?"ScenarioDelete.EmptyPoint":"ScenarioPoint.DeleteConfirm",
            EditorConfirmationPrompt.ScenarioConditionRemoval=>"ScenarioDelete.Condition",EditorConfirmationPrompt.ScenarioActionRemoval=>"ScenarioDelete.Action",_=>"ThingRequest.UsedModify"
        };
        text.SetResourceReference(TextBlock.TextProperty, messageKey); textPanel.Children.Add(text); RegisterName("RegionRemovalMessage", text);
        var count = new TextBlock { Text = "(" + request.Count.ToString(System.Globalization.CultureInfo.CurrentCulture) + ")", Margin = new(4) };
        textPanel.Children.Add(count); RegisterName("RegionRemovalCount", count);
        if(IsScenarioPrompt)count.Visibility=Visibility.Collapsed;
        var rememberText = new TextBlock { TextWrapping = TextWrapping.Wrap }; rememberText.SetResourceReference(TextBlock.TextProperty, IsScenarioPrompt?"ScenarioDelete.Remember":IsUsedPrompt ? "RegionRemoval.RememberUsed" : request.Prompt==EditorConfirmationPrompt.ObjectRemoval ? "ThingRequest.Remember" : "RegionRemoval.Remember"); remember.Content = rememberText; panel.Children.Add(remember);
        remember.Checked += (_, _) => { Refresh(); yes.Focus(); }; remember.Unchecked += (_, _) => Refresh();
        panel.Children.Add(status);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right }; panel.Children.Add(buttons);
        yes.SetResourceReference(ContentControl.ContentProperty, "Editor.PatrolRemovalYes"); yes.Click += (_, _) => Accept(); buttons.Children.Add(yes);
        no.SetResourceReference(ContentControl.ContentProperty, "Editor.PatrolRemovalNo"); no.Click += (_, _) =>
        {
            if (!Current || remember.IsChecked == true) return;
            if (IsUsedPrompt) Respond(keepUsed: true);
            else if (request.Decline()) Close();
        }; buttons.Children.Add(no);
        cancel.SetResourceReference(ContentControl.ContentProperty, "Editor.Cancel"); cancel.Click += (_, _) => Close();
        cancel.Visibility = IsUsedPrompt ? Visibility.Visible : Visibility.Collapsed;
        buttons.Children.Add(cancel); RegisterName("RegionRemovalCancel", cancel);
        RegisterName("RegionRemovalRemember", remember); RegisterName("RegionRemovalYes", yes); RegisterName("RegionRemovalNo", no);
        PreviewKeyDown += (_, args) => { if (args.Key == Key.Escape) { args.Handled = true; Close(); } };
        request.Document.Changed += DocumentChanged;
        Loaded += (_, _) => { if (!IsUsedPrompt && !IsScenarioPrompt) PlaceAtMouse(); Refresh(); yes.Focus(); };
        Closing += (_, _) => closing = true;
        Closed += (_, _) => { closed = true; if (!advanced) request.Dispose(); request.Document.Changed -= DocumentChanged; };
        Refresh();
    }
    private bool IsUsedPrompt => request.Prompt is EditorConfirmationPrompt.UsedAreas or EditorConfirmationPrompt.UsedThingRemoval or EditorConfirmationPrompt.UsedThingModification or EditorConfirmationPrompt.UsedCursorRemoval;
    private bool IsScenarioPrompt => request is EditorScenarioDeletionRequest;
    private bool Current => !closed && !closing && ownerCurrent() && request.IsCurrent;
    private void Refresh()
    {
        var current = Current; yes.IsEnabled = remember.IsEnabled = current; no.IsEnabled = cancel.IsEnabled = current && request.CanDecline(remember.IsChecked == true);
        if (!current && !closed) status.SetResourceReference(TextBlock.TextProperty, IsScenarioPrompt?"ScenarioDelete.Stale":"RegionRemoval.Stale");
    }
    private void DocumentChanged(object? sender, EventArgs args) { if (!closed) Refresh(); }
    public bool Accept() => Respond(keepUsed: false);
    private bool Respond(bool keepUsed)
    {
        if (!Current) { Refresh(); return false; }
        try
        {
            var prompt = request.Prompt;
            if (keepUsed) request.Decline(); else request.Accept(remember.IsChecked == true);
            advanced = prompt != request.Prompt && !request.IsClosed;
            if (!closed) Close(); return true;
        }
        catch (Exception error) when (error is EditorHistoryMemoryException or ArgumentException or InvalidOperationException or InvalidDataException or OverflowException or IOException or UnauthorizedAccessException or FormatException)
        { Failure = error; if (!closed) Close(); return request.IsAccepted; }
    }
    public void Dispose()
    {
        request.Document.Changed -= DocumentChanged;
        if (!advanced) request.Dispose();
        if (!closed) Close();
    }
    private void PlaceAtMouse()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero || !GetCursorPos(out var point) || !GetWindowRect(handle, out var bounds)) return;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromPoint(point, 2), ref info)) return;
        var position = EditorPatrolWarningPolicy.PlaceAtCursor(point.X, point.Y, bounds.Right - bounds.Left, bounds.Bottom - bounds.Top,
            info.Work.Left, info.Work.Top, info.Work.Right - info.Work.Left, info.Work.Bottom - info.Work.Top);
        SetWindowPos(handle, IntPtr.Zero, position.X, position.Y, 0, 0, 0x0001 | 0x0004 | 0x0010);
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(IntPtr window, out NativeRect bounds);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
}
