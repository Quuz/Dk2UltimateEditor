using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private HeroGateGenerationPlan? generationPlan;
    private sealed record GenerationView(HeroGateGenerationItem Item, string Label)
    { public System.Windows.Media.Imaging.BitmapSource? Icon=>OriginalControlIcons.For(GateGenerationImageResources.ForSite(Item.Site)); }
    private string? generationStatusKey;
    private void ResetGateGeneration()
    {
        generationContext++;CloseGateGenerationRequest();
        generationPlan = null; generationStatusKey = null; GeneratorStatus.Text = ""; GeneratorStatus.ToolTip = null;
        var options = HeroGateGenerationOptions.Defaults(document);
        GeneratorKeepers.ItemsSource = Enumerable.Range(1, 5).ToArray(); GeneratorKeepers.SelectedItem = options.Keepers;
        GatePatternName.Text = options.Gate.Name; GatePatternInfo.Text = options.Gate.Info;
        AreaPatternName.Text = options.Area.Name; AreaPatternInfo.Text = options.Area.Info;
        GatePatternColor.Text = options.Gate.Color.ToString("X8", CultureInfo.InvariantCulture); GatePatternTextColor.Text = options.Gate.TextColor.ToString("X8", CultureInfo.InvariantCulture);
        AreaPatternColor.Text = options.Area.Color.ToString("X8", CultureInfo.InvariantCulture); AreaPatternTextColor.Text = options.Area.TextColor.ToString("X8", CultureInfo.InvariantCulture);
        foreach (var control in new[] { GeneratorGateGroup, GeneratorAreaGroup, GeneratorGateDisplay, GeneratorGateDestroy, GeneratorGateRelease, GeneratorAreaDestroy, GeneratorAreaRelease, GeneratorEvents }) control.IsChecked = true;
        RefreshGateGeneration();
    }
    private void InvalidateGateGeneration()
    {
        generationContext++;CloseGateGenerationRequest();generationPlan = null;
        generationStatusKey = null; GeneratorStatus.Text = ""; GeneratorStatus.ToolTip = null;
        RefreshGateGeneration();
    }
    private HeroGateGenerationFlags GeneratorFlags()
    {
        var flags = HeroGateGenerationFlags.None;
        if (GeneratorGateGroup.IsChecked == true)
        {
            if (GeneratorGateDisplay.IsChecked == true) flags |= HeroGateGenerationFlags.GateDisplay;
            if (GeneratorGateDestroy.IsChecked == true) flags |= HeroGateGenerationFlags.GateDestroy;
            if (GeneratorGateRelease.IsChecked == true) flags |= HeroGateGenerationFlags.GateRelease;
        }
        if (GeneratorAreaGroup.IsChecked == true)
        {
            if (GeneratorAreaDestroy.IsChecked == true) flags |= HeroGateGenerationFlags.AreaDestroy;
            if (GeneratorAreaRelease.IsChecked == true) flags |= HeroGateGenerationFlags.AreaRelease;
        }
        if (GeneratorEvents.IsChecked == true && flags.HasFlag(HeroGateGenerationFlags.GateDestroy) && flags.HasFlag(HeroGateGenerationFlags.AreaDestroy)) flags |= HeroGateGenerationFlags.Events;
        return flags;
    }
    private void RefreshGateGeneration()
    {
        if (GeneratorPanel is null) return;
        GeneratorPanel.IsEnabled = document is not null;
        GeneratorTemplates.IsEnabled = document?.IsLegacyProject == true;
        GeneratorNativeNote.Visibility = document is { IsLegacyProject: false } ? Visibility.Visible : Visibility.Collapsed;
        foreach (var control in new[] { GeneratorGateDisplay, GeneratorGateDestroy, GeneratorGateRelease }) control.IsEnabled = GeneratorGateGroup.IsChecked == true;
        foreach (var control in new[] { GeneratorAreaDestroy, GeneratorAreaRelease }) control.IsEnabled = GeneratorAreaGroup.IsChecked == true;
        GeneratorEvents.IsEnabled = GeneratorFlags().HasFlag(HeroGateGenerationFlags.GateDestroy) && GeneratorFlags().HasFlag(HeroGateGenerationFlags.AreaDestroy);
        GeneratorApply.IsEnabled = !generationRequestPending && generationPlan is { HasWork: true };
        GeneratorSites.ItemsSource = (generationPlan?.Items ?? []).Select(item => new GenerationView(item,
            $"{item.Site.Type} · ({item.Site.Footprint.X}, {item.Site.Footprint.Y}) · "
            + LocalizationService.Get(item.Site.CannotDestroy ? "Editor.GeneratorProtected" : "Editor.GeneratorDestroyable")
            + $" · {LocalizationService.Get("Editor.GeneratorGateID")} {item.GateID?.ToString() ?? "—"} {(item.AddGate ? "+" : "")}" 
            + $" · {LocalizationService.Get("Editor.GeneratorAreaID")} {item.AreaID?.ToString() ?? "—"} {(item.AddArea ? "+" : "")}" 
            + " · " + LocalizationService.Get(item.AddEvents ? "Editor.GeneratorNewEvents" : item.Site.HasEvents ? "Editor.GeneratorHasEvents" : "Editor.GeneratorNoEvents")
            + (document?.IsLegacyProject == true ? " · " + item.GateName + " · " + item.AreaName : ""))).ToArray();
        GeneratorSummary.Text = generationPlan is { } plan ? string.Format(CultureInfo.CurrentCulture, LocalizationService.Get("Editor.GeneratorSummary"), plan.Gates, plan.Areas, plan.Conditions, plan.Actions) : "";
        if (generationStatusKey is { } key) GeneratorStatus.SetResourceReference(TextBlock.TextProperty, key);
    }
    private void GeneratorOption_Click(object sender, RoutedEventArgs e) => InvalidateGateGeneration();
    private void GeneratorInput_Changed(object sender, RoutedEventArgs e) { if (GeneratorApply is not null) InvalidateGateGeneration(); }
    private void GeneratorStatusMessage(string key) { generationStatusKey = key; GeneratorStatus.ToolTip = null; GeneratorStatus.SetResourceReference(TextBlock.TextProperty, key); }
    private HeroGateGenerationOptions GeneratorOptions()
    {
        uint Color(TextBox input) => input.Text.Length == 8 && uint.TryParse(input.Text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value) ? value : throw new ArgumentException("Use 8 hexadecimal COLORREF digits.");
        return new(GeneratorFlags(), GeneratorKeepers.SelectedItem is int keepers ? keepers : 1,
            new(GatePatternName.Text, GatePatternInfo.Text, Color(GatePatternColor), Color(GatePatternTextColor)),
            new(AreaPatternName.Text, AreaPatternInfo.Text, Color(AreaPatternColor), Color(AreaPatternTextColor)));
    }
    private void GeneratorPreview_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || !CommitFields()) return;
        InvalidateGateGeneration();
        var source=document;var context=generationContext;
        try
        {
            generationPlan = document.PlanHeroGateGeneration(GeneratorOptions());
            GeneratorStatusMessage(generationPlan.HasWork ? "Editor.GeneratorReady" : "Editor.GeneratorNoWork"); RefreshGateGeneration();
        }
        catch(GateGenerationCapacityException exception)
        {GeneratorStatusMessage("Editor.GeneratorFailed");try{ShowGenerationCapacity(exception,source,context);}catch(InvalidOperationException error){GeneratorStatus.ToolTip=error.Message;}}
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or OverflowException)
        { GeneratorStatusMessage("Editor.GeneratorFailed"); GeneratorStatus.ToolTip = exception.Message; }
    }
    private void GeneratorApply_Click(object sender, RoutedEventArgs e)
    {
        var plan = generationPlan;
        if (document is null || plan is null || generationRequestPending || !CommitFields() || generationPlan != plan) return;
        var source=document;var context=generationContext;
        try
        {
            using var request=GenerationRequestPolicy.Begin(plan,()=>ReferenceEquals(source,document)&&generationPlan==plan&&context==generationContext&&!scenarioNamesOwnerClosed);
            if(request is null||!PresentGateGenerationRequest(request)||!ReferenceEquals(source,document)||generationPlan!=plan||context!=generationContext)return;
            CancelRegionEditing();source.ApplyEditorHeroGateGeneration(plan);InvalidateGateGeneration();GeneratorStatusMessage("Editor.GeneratorApplied");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or OverflowException)
        { GeneratorStatusMessage("Editor.GeneratorFailed"); GeneratorStatus.ToolTip = exception.Message; }
    }
    private void OpenGenerator_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || !CommitFields()) return;
        CancelRegionEditing(); WorkspaceTabs.SelectedItem = HeroGateTab;
        if (document.IsLegacyProject && RegionPicker.SelectedItem is RegionView { Region.Kind: MapRegionKind.Gate } view)
        {
            GatePatternColor.Text = uint.Parse(view.Region.Record.Read(view.Region.Record.Fields.Single(field => field.Name == "rect.color")), CultureInfo.InvariantCulture).ToString("X8", CultureInfo.InvariantCulture);
            GatePatternTextColor.Text = uint.Parse(view.Region.Record.Read(view.Region.Record.Fields.Single(field => field.Name == "rect.textcolor")), CultureInfo.InvariantCulture).ToString("X8", CultureInfo.InvariantCulture);
        }
    }
}
