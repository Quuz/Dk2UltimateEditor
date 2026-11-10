using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class RegionGateGenerationWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly RegionGateGenerationSession session;
    private readonly Func<bool> ownerCurrent;
    private readonly Dictionary<HeroGateGenerationFlags,CheckBox> flags=[];
    private readonly CheckBox gateGroup=new(),areaGroup=new(),events=new();
    private readonly EditorGateGenerationPolicy requestPolicy;
    private GateGenerationRequestWindow? requestWindow;
    private bool requestPending;
    private long inputRevision;
    public Action<GateGenerationRequestWindow>? RequestPresenter {get;set;}
    private readonly ComboBox keepers=new();
    private readonly List<TextBox> gate=[],area=[];
    private readonly ListBox sites=new(){MinHeight=110,MaxHeight=220};
    private sealed record SiteView(string Label,int ImageResource)
    { public System.Windows.Media.Imaging.BitmapSource? Icon=>OriginalControlIcons.For(ImageResource); }
    private readonly TextBlock summary=new(){TextWrapping=TextWrapping.Wrap},status=new(){TextWrapping=TextWrapping.Wrap};
    private readonly Button accept=new();
    private RegionGateGenerationPlan? plan;
    private bool initializing=true,closed;
    public bool KeptInDraft {get;private set;}
    public RegionGateGenerationWindow(NativeMapDocument document,RegionGateGenerationSession session,Func<bool> ownerCurrent,EditorRequestPreferences? preferences=null)
    {
        this.document=document;this.session=session;this.ownerCurrent=ownerCurrent;
        requestPolicy=new(preferences??new());
        NameScope.SetNameScope(this,new NameScope());SetResourceReference(TitleProperty,"Editor.GeneratorTitle");
        sites.ItemTemplate=OriginalControlIcons.Template("Label");
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=760;Height=790;MinWidth=650;MinHeight=560;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var layout=new DockPanel{Margin=new(14)};Content=layout;
        var bottom=new StackPanel();DockPanel.SetDock(bottom,Dock.Bottom);layout.Children.Add(bottom);
        bottom.Children.Add(summary);bottom.Children.Add(status);
        var reset=new Button();Key(reset,ContentControl.ContentProperty,"GateGenerationRequest.Reset");bottom.Children.Add(reset);RegisterName("GenerationResetRequests",reset);
        reset.Click+=(_,_)=>{try{requestWindow?.Dispose();requestPolicy.ResetSuppression();Refresh();}catch(InvalidOperationException error){Failed(error);}};
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};bottom.Children.Add(buttons);
        var preview=new Button();Key(preview,ContentControl.ContentProperty,"Editor.GeneratorPreview");buttons.Children.Add(preview);preview.Click+=(_,_)=>Preview();RegisterName("GenerationPreview",preview);
        Key(accept,ContentControl.ContentProperty,"RegionProperties.GenerationKeep");accept.IsDefault=true;buttons.Children.Add(accept);accept.Click+=(_,_)=>Keep();RegisterName("GenerationKeep",accept);
        var cancel=new Button{IsCancel=true};Key(cancel,ContentControl.ContentProperty,"RegionProperties.Cancel");buttons.Children.Add(cancel);cancel.Click+=(_,_)=>Close();
        var content=new StackPanel();layout.Children.Add(new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        var note=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(0,0,0,10)};Key(note,TextBlock.TextProperty,"RegionProperties.GenerationHint");content.Children.Add(note);
        AddCheck(content,gateGroup,"Editor.GeneratorGateGroup");
        AddFlag(content,HeroGateGenerationFlags.GateDisplay,"Editor.GeneratorGateDisplay");
        AddFlag(content,HeroGateGenerationFlags.GateDestroy,"Editor.GeneratorGateDestroy");
        AddFlag(content,HeroGateGenerationFlags.GateRelease,"Editor.GeneratorGateRelease");
        AddCheck(content,areaGroup,"Editor.GeneratorAreaGroup");
        AddFlag(content,HeroGateGenerationFlags.AreaDestroy,"Editor.GeneratorAreaDestroy");
        AddFlag(content,HeroGateGenerationFlags.AreaRelease,"Editor.GeneratorAreaRelease");
        AddCheck(content,events,"Editor.GeneratorEvents");
        keepers.ItemsSource=Enumerable.Range(1,5);keepers.SelectedItem=session.Defaults.Keepers;keepers.SelectionChanged+=(_,_)=>Invalidate();
        Field(content,"Editor.GeneratorKeepers",keepers);
        var templates=new StackPanel{IsEnabled=session.HasEditorLabels};content.Children.Add(templates);
        AddTemplate(templates,"Editor.GeneratorGateTemplate",session.Defaults.Gate,gate);
        AddTemplate(templates,"Editor.GeneratorAreaTemplate",session.Defaults.Area,area);
        var templateNote=new TextBlock{TextWrapping=TextWrapping.Wrap};Key(templateNote,TextBlock.TextProperty,session.HasEditorLabels?"Editor.GeneratorTemplateHint":"Editor.GeneratorNativeNote");content.Children.Add(templateNote);
        content.Children.Add(sites);RegisterName("GenerationSites",sites);
        LocalizationService.LanguageChanged+=Translated;
        Closed+=(_,_)=>{closed=true;requestWindow?.Dispose();requestPolicy.Dispose();session.Dispose();LocalizationService.LanguageChanged-=Translated;};
        initializing=false;Refresh();
    }
    private bool Current=>!closed&&ownerCurrent()&&document.IsRegionGateGenerationCurrent(session);
    private static void Key(FrameworkElement target,DependencyProperty property,string key)=>target.SetResourceReference(property,key);
    private static void Field(Panel parent,string key,Control control)
    {var label=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(0,8,0,2)};Key(label,TextBlock.TextProperty,key);parent.Children.Add(label);parent.Children.Add(control);}
    private void AddCheck(Panel content,CheckBox box,string key)
    {box.IsChecked=true;box.Margin=new(0,5,0,5);Key(box,ContentControl.ContentProperty,key);content.Children.Add(box);box.Click+=(_,_)=>Invalidate();}
    private void AddFlag(Panel content,HeroGateGenerationFlags flag,string key)
    {var box=new CheckBox{Margin=new(18,3,0,3)};AddCheck(content,box,key);flags.Add(flag,box);}
    private void AddTemplate(Panel content,string key,HeroGateTemplate defaults,List<TextBox> inputs)
    {
        var label=new TextBlock{FontWeight=FontWeights.Bold,Margin=new(0,12,0,0)};Key(label,TextBlock.TextProperty,key);content.Children.Add(label);
        var values=new[]{defaults.Name,defaults.Info,defaults.Color.ToString("X8",CultureInfo.InvariantCulture),defaults.TextColor.ToString("X8",CultureInfo.InvariantCulture)};
        var keys=new[]{"Editor.GeneratorName","Editor.GeneratorInfo","Editor.GeneratorColor","Editor.GeneratorTextColor"};
        for(var i=0;i<4;i++)
        {
            var input=new TextBox{Text=values[i],MaxLength=i==0?63:i==1?127:8};inputs.Add(input);input.TextChanged+=(_,_)=>Invalidate();
            Field(content,keys[i],input);
            if(i<2)continue;
            var choose=new Button{HorizontalAlignment=HorizontalAlignment.Left};Key(choose,ContentControl.ContentProperty,i==2?"RegionProperties.Colour":"RegionProperties.TextColour");content.Children.Add(choose);
            choose.Click+=(_,_)=>{try{if(Current&&NativeTerrainColourDialog.Choose(this,Colour(input)) is {} value&&Current)input.Text=value.ToString("X8",CultureInfo.InvariantCulture);}
                catch(Exception error)when(error is ArgumentException or InvalidOperationException){Failed(error);}};
        }
    }
    private static uint Colour(TextBox input)=>input.Text.Length==8&&uint.TryParse(input.Text,NumberStyles.AllowHexSpecifier,CultureInfo.InvariantCulture,out var value)
        ?value:throw new ArgumentException("Use eight hexadecimal COLORREF digits.");
    private HeroGateGenerationOptions Options()
    {
        var bits=HeroGateGenerationFlags.None;
        foreach(var (flag,box) in flags)if(box.IsChecked==true&&(flag is HeroGateGenerationFlags.GateDisplay or HeroGateGenerationFlags.GateDestroy or HeroGateGenerationFlags.GateRelease?gateGroup:areaGroup).IsChecked==true)bits|=flag;
        if(events.IsChecked==true&&bits.HasFlag(HeroGateGenerationFlags.GateDestroy)&&bits.HasFlag(HeroGateGenerationFlags.AreaDestroy))bits|=HeroGateGenerationFlags.Events;
        HeroGateTemplate T(List<TextBox> inputs)=>new(inputs[0].Text,inputs[1].Text,Colour(inputs[2]),Colour(inputs[3]));
        return new(bits,(int)keepers.SelectedItem,T(gate),T(area));
    }
    private void Invalidate(){if(initializing)return;inputRevision++;requestWindow?.Dispose();plan=null;summary.Text="";status.Text="";status.ToolTip=null;Refresh();}
    private void Refresh()
    {
        foreach(var (flag,box) in flags)box.IsEnabled=(flag is HeroGateGenerationFlags.GateDisplay or HeroGateGenerationFlags.GateDestroy or HeroGateGenerationFlags.GateRelease?gateGroup:areaGroup).IsChecked==true;
        events.IsEnabled=gateGroup.IsChecked==true&&areaGroup.IsChecked==true&&flags[HeroGateGenerationFlags.GateDestroy].IsChecked==true&&flags[HeroGateGenerationFlags.AreaDestroy].IsChecked==true;
        accept.IsEnabled=!requestPending&&Current&&plan is {HasWork:true};
        if(plan is {} value)summary.Text=string.Format(CultureInfo.CurrentCulture,LocalizationService.Get("Editor.GeneratorSummary"),value.Gates,value.Areas,value.Conditions,value.Actions);
        sites.ItemsSource=(plan?.Items??[]).Select(item=>new SiteView($"{item.Type} · ({item.Footprint.X}, {item.Footprint.Y}) · "
            +LocalizationService.Get(item.CannotDestroy?"Editor.GeneratorProtected":"Editor.GeneratorDestroyable")
            +$" · {LocalizationService.Get("Editor.GeneratorGateID")} {item.GateID?.ToString()??"—"}{(item.AddGate?" +":"")}"
            +$" · {LocalizationService.Get("Editor.GeneratorAreaID")} {item.AreaID?.ToString()??"—"}{(item.AddArea?" +":"")}"
            +" · "+LocalizationService.Get(item.AddEvents?"Editor.GeneratorNewEvents":"Editor.GeneratorNoEvents")
            +(session.HasEditorLabels?" · "+item.GateName+" · "+item.AreaName:""),item.ImageResource)).ToArray();
    }
    private void Translated(object? sender,EventArgs args)=>Refresh();
    private void Failed(Exception error){Key(status,TextBlock.TextProperty,"Editor.GeneratorFailed");status.ToolTip=error.Message;}
    public bool Preview()
    {
        if(!Current||requestPending)return false;Invalidate();var revision=inputRevision;
        try{plan=document.PreviewRegionGateGeneration(session,Options());Key(status,TextBlock.TextProperty,plan.HasWork?"Editor.GeneratorReady":"Editor.GeneratorNoWork");Refresh();return true;}
        catch(GateGenerationCapacityException error)
        {
            Failed(error);
            try{using var request=requestPolicy.BeginCapacity(error,()=>Current&&revision==inputRevision);PresentRequest(request);}
            catch(InvalidOperationException failure){Failed(failure);}
            return false;
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException){Failed(error);return false;}
    }
    public bool Keep()
    {
        if(!Current||requestPending||plan is not {HasWork:true} selected)return false;
        var revision=inputRevision;
        try
        {
            using var request=requestPolicy.Begin(document,selected,()=>Current&&plan==selected&&revision==inputRevision);
            if(request is null||!PresentRequest(request)||!Current||plan!=selected||revision!=inputRevision)return false;
            document.AcceptRegionGateGeneration(selected,true);KeptInDraft=true;Close();return true;
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or OverflowException){Failed(error);return false;}
    }
    private bool PresentRequest(EditorGateGenerationRequest request)
    {
        requestPending=true;Refresh();
        try
        {
            if(!request.RequiresPrompt){if(request.Capacity is null)request.Confirm();else request.Acknowledge();return request.IsAccepted;}
            using var window=new GateGenerationRequestWindow(request){Owner=this};requestWindow=window;
            try{if(RequestPresenter is {} presenter)presenter(window);else window.ShowDialog();}
            finally{if(ReferenceEquals(requestWindow,window))requestWindow=null;}
            if(window.Failure is {} error)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
            return request.IsAccepted;
        }
        finally{requestPending=false;if(!closed)Refresh();}
    }
}
