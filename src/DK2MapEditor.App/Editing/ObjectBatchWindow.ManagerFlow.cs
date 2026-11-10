using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed partial class ObjectBatchWindow
{
    private bool managerFlow,managerTemplateOnly,managerSingleEditor,managerPreview,managerDirectApply,managerPlacementBounds;
    private bool ManagerConfirmation=>managerPreview||managerDirectApply||confirmed.IsChecked==true;
    public bool ManagerCommandApplied {get;private set;}
    public bool ManagerCommandFailed {get;private set;}
    internal int ManagerAppliedCount=>appliedCount;
    internal string? ManagerFailure=>status.ToolTip as string;
    internal bool ManagerMultiple=>sourceModificationRequest is {Count:>1}||sourceModificationRequest is null&&modificationSelection is {Count:>1};
    internal bool RequiresManagerPreview=>IsModification
        ? modificationPlan is null||ObjectManagerFlowRules.RequiresPreview(modificationPlan.Options.Flags,modificationPlan.Options.Replacement,modificationPlan.Candidates.Select(c=>c.Warnings))
            ||modificationPlan.Candidates.Any(c=>c.DetachesScript&&c.Source.Scripted)
        :plan is null||ObjectManagerFlowRules.RequiresPreview(plan.Placement.Options.Flags,plan.Placement.Options.Replacement,plan.Placement.Candidates.Select(c=>c.Warnings));
    private void TemplateLayout()
    {
        var body=(Grid)FindName("BatchBody");body.ColumnDefinitions[0].Width=new(1,GridUnitType.Star);body.ColumnDefinitions[1].Width=new(0);
        ((FrameworkElement)FindName("BatchPreviewSide")).Visibility=Visibility.Collapsed;
        Width=600;MinWidth=480;MinHeight=420;confirmed.Visibility=Visibility.Collapsed;flowStatus.Visibility=Visibility.Visible;
        PreviewKeyDown+=(_,args)=>{if(args.Key==Key.Escape){args.Handled=true;Cancel();}};
    }
    internal void ConfigureManagerTemplateEditor()
    {
        managerTemplateOnly=true;TemplateLayout();apply.Visibility=Visibility.Collapsed;
        SetResourceReference(TitleProperty,"Manager.TemplateTitle");
        ((Button)FindName("BatchCancel")).SetResourceReference(ContentControl.ContentProperty,"Manager.Return");
        flowStatus.SetResourceReference(TextBlock.TextProperty,"Manager.TemplateHint");UpdateApply();
    }
    internal void ConfigureManagerSingleEditor(ObjectBatchFlags sourceFlags,int replacementPolicy)
    {
        managerFlow=true;managerSingleEditor=true;updating=true;
        try
        {
            session.Flags=sourceFlags;session.Policy=replacementPolicy;foreach(var pair in flags)pair.Value.IsChecked=sourceFlags.HasFlag(pair.Key);policy.SelectedValue=replacementPolicy.ToString();
            modificationMasks[ObjectModificationMask.Coordinates].IsChecked=true;modificationCoordinates.SelectedValue="0";
        }
        finally{updating=false;}
        TemplateLayout();
        var left=(Panel)((ScrollViewer)FindName("BatchTemplatePanel")).Content;
        foreach(var field in new[]{y,x})if(field.Parent is FrameworkElement group&&group.Parent is Panel parent){parent.Children.Remove(group);left.Children.Insert(0,group);}
        apply.IsDefault=true;UpdateApply();
    }
    internal void UseManagerPlacement(bool whole,TileRect? bounds)
    {
        managerFlow=true;managerPlacementBounds=true;
        if(session.Mode!=0&&!whole&&bounds is null)throw new InvalidOperationException("Select a rectangle before adding objects in selection-only mode.");
        updating=true;
        try
        {
            session.WholeMap=whole;wholeMap.IsChecked=whole;
            if(session.Mode!=0&&!whole&&bounds is {} r)
            {x.Text=r.X.ToString();y.Text=r.Y.ToString();width.Text=r.Width.ToString();height.Text=r.Height.ToString();}
        }
        finally{updating=false;}
        RefreshGeometry();InvalidatePreview();
    }
    internal void ConfigureManagerPreview()
    {
        managerFlow=true;managerPreview=true;managerSingleEditor=false;managerTemplateOnly=false;
        var body=(Grid)FindName("BatchBody");body.ColumnDefinitions[0].Width=new(0);body.ColumnDefinitions[1].Width=new(1,GridUnitType.Star);
        ((FrameworkElement)FindName("BatchTemplatePanel")).Visibility=Visibility.Collapsed;
        ((FrameworkElement)FindName("BatchPreviewSide")).Visibility=Visibility.Visible;
        var controls=(Panel)FindName("BatchControls");var commands=(UIElement)FindName("BatchSelectionCommands");
        var needsDetach=IsModification&&modificationPlan!.Candidates.Any(c=>c.DetachesScript&&c.Source.Scripted);
        if(needsDetach&&detachScripts.Parent is Panel parent){parent.Children.Remove(detachScripts);controls.Children.Insert(0,detachScripts);detachScripts.Visibility=Visibility.Visible;}
        foreach(UIElement element in controls.Children)element.Visibility=ReferenceEquals(element,commands)||ReferenceEquals(element,summary)||needsDetach&&ReferenceEquals(element,detachScripts)?Visibility.Visible:Visibility.Collapsed;
        ((Button)FindName("BatchPreview")).Visibility=Visibility.Collapsed;
        confirmed.Visibility=Visibility.Collapsed;apply.IsDefault=true;SetResourceReference(TitleProperty,"Batch.Preview");UpdateApply();
    }
    internal bool ApplyManagerDefaults()
    {
        if(RequiresManagerPreview)throw new InvalidOperationException("This command requires source preview decisions.");
        managerFlow=true;managerDirectApply=true;
        try{return ApplyPreview();}finally{managerDirectApply=false;}
    }
}
