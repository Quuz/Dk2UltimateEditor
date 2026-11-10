using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Original Action Point type page and eight physical owner pages.
/// All selections remain private until Finish.</summary>
public sealed class ScenarioPointWindow : Window
{
    private sealed record OwnerDisplay(string Label, System.Windows.Media.Imaging.BitmapSource? Icon);
    private static readonly uint[] Scopes=[32,1,2,64,128,256,4,8,16];
    private readonly NativeMapDocument document;
    private readonly EditorScenarioPointSession session;
    private IReadOnlyList<ScenarioPointBandPreview>? bandPreviews;
    private readonly Func<bool> ownerCurrent,commitFields;
    private readonly Func<ScriptOwner,string> label;
    private readonly Action<ScriptEdge> completed;
    private readonly ContentControl page=new();
    private readonly StackPanel types=new();
    private readonly ListBox owners=new(){Margin=new(4)};
    private readonly Button back=new(),next=new(),finish=new();
    private readonly TextBlock status=new(){Margin=new(4),TextWrapping=TextWrapping.Wrap};
    private bool refreshing,closed,finishing;
    public bool IsOwnerPage {get;private set;}
    public bool IsModifying=>session.IsModifying;
    public uint SelectedScope=>session.Scope;
    public ScriptOwner? SelectedOwner=>session.SelectedOwner;
    public ScriptEdge? SavedPoint {get;private set;}

    public ScenarioPointWindow(NativeMapDocument document,Func<bool> ownerCurrent,Func<bool> commitFields,
        Func<ScriptOwner,string> label,Action<ScriptEdge> completed,ScriptEdge? existing=null,
        ScriptEdge? insertBefore=null,uint initialScope=1,ScriptOwner? preferredOwner=null)
    {
        this.document=document;this.ownerCurrent=ownerCurrent;this.commitFields=commitFields;this.label=label;this.completed=completed;
        session=document.CreateEditorScenarioPointSession(existing,insertBefore,initialScope);
        if(preferredOwner is not null && session.Choices.Any(choice=>choice.CanSelect && Same(choice.Owner,preferredOwner)))session.SelectOwner(preferredOwner);
        NameScope.SetNameScope(this,new NameScope());
        SetResourceReference(TitleProperty,IsModifying?"ScenarioPoint.EditTitle":"ScenarioPoint.Title");
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=620;Height=510;MinWidth=480;MinHeight=390;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new DockPanel{Margin=new(12)};Content=panel;
        var explanation=new TextBlock{Margin=new(4),TextWrapping=TextWrapping.Wrap};explanation.SetResourceReference(TextBlock.TextProperty,"ScenarioPoint.Explanation");
        DockPanel.SetDock(explanation,Dock.Top);panel.Children.Add(explanation);
        var footer=new StackPanel();DockPanel.SetDock(footer,Dock.Bottom);panel.Children.Add(footer);footer.Children.Add(status);
        var buttons=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};footer.Children.Add(buttons);
        void Add(Button button,string key,string name,Action action)
        {button.Margin=new(4);button.SetResourceReference(ContentControl.ContentProperty,key);button.Click+=(_,_)=>action();buttons.Children.Add(button);RegisterName(name,button);}
        Add(back,"ScenarioWizard.Back","ScenarioPointBack",()=>Back());Add(next,"ScenarioWizard.Next","ScenarioPointNext",()=>Next());
        Add(finish,"ScenarioWizard.Finish","ScenarioPointFinish",()=>Finish());Add(new Button{IsCancel=true},"ScenarioPage.Cancel","ScenarioPointCancel",Close);
        RegisterName("ScenarioPointOwners",owners);RegisterName("ScenarioPointStatus",status);
        foreach(var scope in Scopes)
        {
            var radio=new RadioButton{Margin=new(8),Tag=scope,GroupName="PointScope",IsEnabled=!IsModifying};
            radio.SetResourceReference(ContentControl.ContentProperty,"ScriptScope."+scope);types.Children.Add(radio);RegisterName("ScenarioPointScope"+scope,radio);
            radio.Checked+=(_,_)=>{if(refreshing || !Current || finishing)return;try{session.SelectScope(scope);RefreshPage();}catch(Exception error)when(Expected(error)){Fail(error);}};
        }
        owners.SelectionChanged+=(_,_)=>
        {
            if(refreshing || !Current || finishing)return;
            if(owners.SelectedItem is not ListBoxItem{Tag:ScenarioPointOwnerChoice choice} || !choice.CanSelect)
            {
                refreshing=true;
                try{owners.SelectedItem=owners.Items.Cast<ListBoxItem>().FirstOrDefault(row=>row.Tag is ScenarioPointOwnerChoice candidate && session.SelectedOwner is { } selected && Same(selected,candidate.Owner));}
                finally{refreshing=false;}
                return;
            }
            try{session.SelectOwner(choice.Owner);status.Text="";RefreshButtons();}catch(Exception error)when(Expected(error)){Fail(error);}
        };
        panel.Children.Add(page);document.Changed+=OwnerChanged;LocalizationService.LanguageChanged+=LanguageChanged;
        Closed+=(_,_)=>{closed=true;session.Dispose();document.Changed-=OwnerChanged;LocalizationService.LanguageChanged-=LanguageChanged;};
        RefreshPage();
    }
    private static bool Same(ScriptOwner a,ScriptOwner b)=>a.Scope==b.Scope && a.MemberIndex==b.MemberIndex && a.PlayerID==b.PlayerID && a.Record.File==b.Record.File && a.Record.Offset==b.Record.Offset;
    private bool Current=>!closed && ownerCurrent() && document.IsEditorScenarioPointSessionCurrent(session);
    private static bool Expected(Exception error)=>error is ArgumentException or InvalidOperationException or InvalidDataException or OverflowException or FormatException;
    private void Fail(Exception error){status.Text=LocalizationService.Get("ScenarioPoint.Invalid");status.ToolTip=error.Message;RefreshButtons();}
    private void OwnerChanged(object? sender,EventArgs e){if(!finishing)RefreshButtons();}
    private void LanguageChanged(object? sender,EventArgs e){if(Current)RefreshPage();else RefreshButtons();}
    public void InvalidateOwner()=>RefreshButtons();
    private void RefreshPage()
    {
        if(!Current){RefreshButtons();return;}
        refreshing=true;
        foreach(RadioButton radio in types.Children)radio.IsChecked=(uint)radio.Tag==session.Scope;
        page.Content=null;page.Content=IsOwnerPage?owners:types;
        owners.Items.Clear();
        var choices=session.Choices;
        void AddOwner(ScenarioPointOwnerChoice? choice,string text,int? image,bool indented=false)
        {
            if(choice is {CanSelect:false})text+=" · "+LocalizationService.Get("ScenarioPoint."+choice.Status);
            var row=new ListBoxItem{Content=new OwnerDisplay(text,OriginalScenarioImages.For(image)),ContentTemplate=OriginalObjectImages.NamedTemplate(),Tag=choice,IsEnabled=choice?.CanSelect==true,Focusable=choice?.CanSelect==true,Padding=new(indented?20:0,0,0,0)};owners.Items.Add(row);
            if(choice is not null && session.SelectedOwner is { } selected && Same(selected,choice.Owner))owners.SelectedItem=row;
        }
        if(session.Scope is 8 or 16)
            foreach(var row in ScenarioPointBandRows.Build(session.Scope,bandPreviews??=document.ReadScenarioPointBandPreviews(session),choices))
            {
                var text=row.Member is { } member ? ScenarioBandPreviewRows.Label(member,LocalizationService.CurrentLanguage,key=>LocalizationService.Get(key))
                    : LocalizationService.Get("ScriptScope.8")+" #"+row.Band.Preview.ID+(row.Band.Name.Length==0?"":" · "+row.Band.Name)
                        +(row.Band.Preview.Complete?"":" · "+LocalizationService.Get("ScenarioPoint.UnsupportedFormat"));
                AddOwner(row.Choice,text,row.Member is { } item ? ScenarioPointOwnerImageResources.ForIdentity(16,item.CreatureID):1212,row.Member is not null);
            }
        else foreach(var choice in choices)AddOwner(choice,label(choice.Owner),ScenarioPointOwnerImageResources.For(choice.Owner));
        refreshing=false;RefreshButtons();
    }
    private void RefreshButtons()
    {
        var current=Current && !finishing;back.IsEnabled=current && IsOwnerPage;next.IsEnabled=current && !IsOwnerPage && session.Scope!=32;
        var selectable=current && session.Choices.Any(choice=>choice.CanSelect && (session.Scope==32 || session.SelectedOwner is { } selected && Same(selected,choice.Owner)));
        finish.IsEnabled=selectable && (IsOwnerPage || session.Scope==32);finish.IsDefault=finish.IsEnabled;next.IsDefault=!finish.IsEnabled && next.IsEnabled;
        if(!current && !finishing)status.Text=LocalizationService.Get("ScenarioPoint.Stale");
    }
    public bool Next()
    {
        if(!Current || finishing || IsOwnerPage || session.Scope==32)return false;
        if(!IsModifying && session.Scope==1 && session.SelectedOwner is null)
        {
            var player=session.Choices.FirstOrDefault(choice=>choice.CanSelect && (choice.Owner.PlayerID==3 || choice.Owner.Record.Fields.FirstOrDefault(field=>field.Name=="bGlobalPlayer") is { } id && choice.Owner.Record.Read(id)=="3"));
            if(player is not null)session.SelectOwner(player.Owner);
        }
        IsOwnerPage=true;RefreshPage();return true;
    }
    public bool Back(){if(!Current || finishing || !IsOwnerPage)return false;IsOwnerPage=false;RefreshPage();return true;}
    public bool Finish()
    {
        if(!Current || finishing || !(IsOwnerPage || session.Scope==32))return false;
        try
        {
            if(session.Scope==32){var level=session.Choices.FirstOrDefault(choice=>choice.CanSelect);if(level is null)return false;session.SelectOwner(level.Owner);}
            var plan=document.PrepareEditorScenarioPoint(session);
            if(!commitFields() || !Current)return false;
            finishing=true;RefreshButtons();SavedPoint=document.ApplyEditorScenarioPoint(plan);completed(SavedPoint);Close();return true;
        }
        catch(Exception error)when(Expected(error)){finishing=false;Fail(error);return false;}
    }
}
