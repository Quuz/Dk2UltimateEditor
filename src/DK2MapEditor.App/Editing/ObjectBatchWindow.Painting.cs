using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed partial class ObjectBatchWindow
{
    private readonly bool isSinglePainting;
    private ObjectPaintingPlan? paintingPlan;
    public ObjectPaintingPlan? PaintingPreview => paintingPlan;
    public event Action<ObjectBatchTemplate, bool>? ObjectBrushRequested;
    public event Action<ObjectBatchTemplate>? ObjectPaintingTemplateAccepted;
    private PlacedThingKind? brushTemplateKind;
    internal void ConfigureBrushTemplateEditor()
    {
        if(!isSinglePainting)throw new InvalidOperationException("A brush template requires single-painting controls.");
        brushTemplateKind=currentKind;TemplateLayout();family.IsEnabled=false;
        foreach(var field in new[]{x,y})if(field.Parent is Panel panel)
        {var index=panel.Children.IndexOf(field);if(index>0&&panel.Children[index-1] is TextBlock label)label.Visibility=Visibility.Collapsed;field.Visibility=Visibility.Collapsed;}
        upgrade.Visibility=Visibility.Collapsed;
        ((TextBlock)FindName("SinglePaintingHint")).Visibility=Visibility.Collapsed;
        ((Button)FindName("SingleUseBrush")).Visibility=Visibility.Collapsed;
        apply.Visibility=Visibility.Visible;apply.IsDefault=true;apply.SetResourceReference(ContentControl.ContentProperty,"Single.UseBrush");
        flowStatus.SetResourceReference(TextBlock.TextProperty,"Brush.TemplateHint");
        SetResourceReference(TitleProperty,"Brush.TemplateTitle");RefreshLanguage();UpdateApply();
    }
    public bool AcceptBrushTemplate()
    {
        if(closed||brushTemplateKind is not {} kind||currentKind!=kind||!canApply())return false;
        try
        {
            uint U(int index)=>uint.Parse(fields[index].Text,System.Globalization.NumberStyles.Integer,System.Globalization.CultureInfo.InvariantCulture);
            var template=new ObjectBatchTemplate(kind,U(0),checked((byte)U(1)),U(2),U(3),
                int.Parse(fields[4].Text,System.Globalization.NumberStyles.Integer,System.Globalization.CultureInfo.InvariantCulture),U(5),U(6),U(7),U(8),U(9));
            if(template.Owner>7||!ValueCatalogues.ForTemplate(kind==PlacedThingKind.SpecialItem?"MagicalObject":kind.ToString(),ValueCatalogues.GameVersion(document))
                .Any(choice=>choice.Value==template.TypeID.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                throw new ArgumentException("Unknown object family type or owner.");
            if(!canApply()||closed)return false;
            SaveTemplate();ObjectBrushRequested?.Invoke(template,false);Cancel();return true;
        }
        catch(Exception error)when(Expected(error)){Failure(error);return false;}
    }

    private void InitializePaintingControls(Panel controls, Panel bottom)
    {
        SetResourceReference(TitleProperty, "Single.Title");
        confirmed.SetResourceReference(ContentControl.ContentProperty, "Single.Confirm");
        ((FrameworkElement)mode.Parent).Visibility = ((FrameworkElement)policy.Parent).Visibility = Visibility.Collapsed;
        wholeMap.Visibility = Visibility.Collapsed;
        ((FrameworkElement)width.Parent).Visibility = ((FrameworkElement)height.Parent).Visibility = Visibility.Collapsed;
        ((FrameworkElement)((FrameworkElement)frequency.Parent).Parent).Visibility = Visibility.Collapsed;
        foreach (var flag in flags.Values) flag.Visibility = Visibility.Collapsed;
        ((Button)FindName("BatchUseSelection")).Visibility = Visibility.Collapsed;
        preview.Columns[0].Width = preview.Columns[0].MinWidth = 95;
        preview.Columns[3].MinWidth = 165;
        var hint = Named(AddLabel(controls, "Single.Hint"),"SinglePaintingHint"); hint.TextWrapping = TextWrapping.Wrap;
        // The full source template includes hero-option bits even for a noncreature update.
        // Ordinary controls retain the family's usual fields; inactive payloads remain in its session draft.
        bottom.Children.Insert(0, Button("SingleUseBrush", "Single.UseBrush", () =>
        {
            if(brushTemplateKind is not null){AcceptBrushTemplate();return;}
            if (closed || !BuildPreview() || paintingPlan is not { } current) return;
            SaveTemplate(); ObjectBrushRequested?.Invoke(current.Template, current.UpgradeHistoricalThings); Cancel();
        }));
    }

    private bool BuildPaintingPreview(ObjectBatchTemplate template, TilePoint position)
    {
        paintingPlan = document.PlanObjectPainting(template, position, upgrade.IsChecked == true);
        var old = paintingPlan.Updating?.Thing ?? paintingPlan.Removals.FirstOrDefault()?.Thing;
        rows.Add(new(new(0, position, paintingPlan.EffectiveTemplate.Owner, paintingPlan.Warnings, paintingPlan.Locked, !paintingPlan.Locked, old),
            () => { if (selecting) return; confirmed.IsChecked = false; RefreshSummary(); UpdateApply(); }));
        preview.ItemsSource = rows; RefreshSummary(); UpdateApply(); return true;
    }

    private void RefreshPaintingSummary()
    {
        if (paintingPlan is not { } current) { summary.Text = LocalizationService.Get("Batch.Draft"); return; }
        summary.Text = LocalizationService.Get(current.TogglesDoor ? "Single.Toggle" : current.Creates ? "Single.Create" : "Single.Update")
            + " " + string.Format(LocalizationService.Get("Single.Summary"), current.Removals.Count, current.Removals.Count(entry => entry.Scripted), current.EffectiveTemplate.DataAll);
    }

    private bool ApplyPaintingPreview()
    {
        if (closed || paintingPlan is not { Locked: false } current || confirmed.IsChecked != true || !rows.Any(row => row.Add)) return false;
        try
        {
            if (!canApply() || paintingPlan != current) throw new InvalidOperationException("The document changed; rebuild the painting preview.");
            appliedCount = document.ApplyEditorObjectPainting(current); SaveTemplate();ObjectPaintingTemplateAccepted?.Invoke(current.Template); InvalidatePreview(); statusKey = "Single.Applied"; RefreshStatus(); return true;
        }
        catch (Exception exception) when (Expected(exception)) { InvalidatePreview(); Failure(exception); return false; }
    }
}
