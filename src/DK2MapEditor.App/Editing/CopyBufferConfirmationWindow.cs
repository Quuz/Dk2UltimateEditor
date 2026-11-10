using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;

namespace DK2MapEditor.App.Editing;

public enum CopyBufferOperation { Copy, Cut, Import, RotatedCopy }
public sealed record CopyBufferRequest(CopyBufferOperation Operation, int OldWidth, int OldHeight,
    int OldObjects, int OldScripts, int NewWidth, int NewHeight);
public sealed record CopyBufferDecision(bool Replace, bool SuppressFutureQuestions = false);

public sealed class CopyBufferConfirmationWindow : Window
{
    private readonly CopyBufferRequest request;
    private readonly TextBlock summary = new() { Margin = new(4), TextWrapping = TextWrapping.Wrap };
    private readonly CheckBox suppress = new() { Margin = new(4), VerticalContentAlignment = VerticalAlignment.Center };
    private bool closed;
    public CopyBufferDecision Decision { get; private set; } = new(false);
    public CopyBufferConfirmationWindow(CopyBufferRequest request)
    {
        this.request = request; NameScope.SetNameScope(this, new NameScope());
        SetResourceReference(TitleProperty, "CopyBuffer.Title");
        SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        Width = 540; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new(16) }; Content = panel;
        panel.SetResourceReference(Panel.BackgroundProperty, "Brush.Background");
        panel.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "Brush.Text");
        var question = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new(4) }; question.SetResourceReference(TextBlock.TextProperty, "CopyBuffer.Question"); panel.Children.Add(question);
        panel.Children.Add(summary); RegisterName("CopyBufferSummary", summary);
        suppress.SetResourceReference(ContentControl.ContentProperty, "CopyBuffer.Suppress"); RegisterName("CopyBufferSuppress", suppress); panel.Children.Add(suppress);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        var yes = new Button { IsDefault = true }; yes.SetResourceReference(ContentControl.ContentProperty, "CopyBuffer.Replace"); yes.Click += (_, _) => Respond(true); RegisterName("CopyBufferReplace", yes);
        var no = new Button { IsCancel = true }; no.SetResourceReference(ContentControl.ContentProperty, "CopyBuffer.Keep"); no.Click += (_, _) => Respond(false); RegisterName("CopyBufferKeep", no);
        buttons.Children.Add(yes); buttons.Children.Add(no); panel.Children.Add(buttons);
        LocalizationService.LanguageChanged += LanguageChanged; LanguageChanged(null, EventArgs.Empty);
        Closed += (_, _) => { closed = true; LocalizationService.LanguageChanged -= LanguageChanged; };
    }
    private void LanguageChanged(object? sender, EventArgs args)
        => summary.Text = string.Format(CultureInfo.CurrentCulture, LocalizationService.Get("CopyBuffer.Summary"),
            request.OldWidth, request.OldHeight, request.OldObjects, request.OldScripts,
            LocalizationService.Get("CopyBuffer.Operation." + request.Operation), request.NewWidth, request.NewHeight);
    public bool Respond(bool replace)
    {
        if (closed) return false;
        Decision = new(replace, replace && suppress.IsChecked == true); Close(); return true;
    }
}
