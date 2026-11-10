using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class MapCreatureModifiersWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly Func<bool> ownerCurrent,commitFields;
    private readonly MapCreatureModifiersSession session;
    private readonly ListView creatures=new(){SelectionMode=SelectionMode.Extended};
    private readonly TextBox[] inputs=new TextBox[4];
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private readonly Button accept=new();
    private bool updating,closed,applying;
    private readonly HashSet<int> rejectedInputs=[];
    private sealed record Row(uint ID,string Name,string P1,string P2,string P3,string P4);
    public bool HasPendingInputs=>!closed&&(session.Changed||rejectedInputs.Count!=0);
    public long InputRevision {get;private set;}
    public MapCreatureModifiersWindow(NativeMapDocument document,Func<bool> ownerCurrent,Func<bool> commitFields,IEnumerable<uint>? selected=null)
    {
        this.document=document;this.ownerCurrent=ownerCurrent;this.commitFields=commitFields;session=document.CreateMapCreatureModifiersSession();
        if(selected is not null)session.Select(selected);
        NameScope.SetNameScope(this,new NameScope());
        SetResourceReference(TitleProperty,"CreatureModifiers.Title");SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=820;Height=640;MinWidth=620;MinHeight=420;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new DockPanel{Margin=new(12)};Content=panel;
        var hint=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(4)};hint.SetResourceReference(TextBlock.TextProperty,"CreatureModifiers.Hint");DockPanel.SetDock(hint,Dock.Top);panel.Children.Add(hint);
        var footer=new StackPanel();DockPanel.SetDock(footer,Dock.Bottom);panel.Children.Add(footer);
        var fields=new WrapPanel();footer.Children.Add(fields);
        for(var index=0;index<4;index++)
        {
            var parameter=index;var cell=new StackPanel{Margin=new(4),Width=165};var label=new TextBlock{TextWrapping=TextWrapping.Wrap};label.SetResourceReference(TextBlock.TextProperty,"Editor.RuleParameter."+(index+1));cell.Children.Add(label);
            var text=new TextBox{Margin=new(0,4,0,4),MaxLength=5};inputs[index]=text;RegisterName("CreatureModifierParameter"+(index+1),text);cell.Children.Add(text);
            text.TextChanged+=(_,_)=>SetParameter(parameter);
            var buttons=new WrapPanel();cell.Children.Add(buttons);
            foreach(var step in new[]{-1,1})
            {var button=new Button{Content=step<0?"−":"+",MinWidth=30};button.Click+=(_,_)=>{if(Current()&&session.CanEditSelection)text.Text=Math.Clamp((long)MapCreatureModifiersSession.ParseInput(text.Text)+step,0,65535).ToString(CultureInfo.InvariantCulture);};buttons.Children.Add(button);}
            fields.Children.Add(cell);
        }
        footer.Children.Add(status);RegisterName("CreatureModifiersStatus",status);
        var actions=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};footer.Children.Add(actions);
        accept.SetResourceReference(ContentControl.ContentProperty,"CreatureModifiers.Accept");accept.Click+=(_,_)=>ApplyModifiers();RegisterName("CreatureModifiersAccept",accept);actions.Children.Add(accept);
        var cancel=new Button{IsCancel=true};cancel.SetResourceReference(ContentControl.ContentProperty,"MapProperties.Cancel");cancel.Click+=(_,_)=>Close();RegisterName("CreatureModifiersCancel",cancel);actions.Children.Add(cancel);
        RegisterName("CreatureModifiersList",creatures);creatures.SelectionChanged+=(_,_)=>SelectionChanged();panel.Children.Add(creatures);
        document.Changed+=DocumentChanged;LocalizationService.LanguageChanged+=LanguageChanged;
        Closed+=(_,_)=>{closed=true;InputRevision++;session.Dispose();document.Changed-=DocumentChanged;LocalizationService.LanguageChanged-=LanguageChanged;};
        RefreshRows(true);UpdateStatus();
    }
    private bool Current()=>!closed&&!applying&&ownerCurrent()&&document.IsMapCreatureModifiersCurrent(session);
    private void RefreshRows(bool means)
    {
        updating=true;
        try
        {
            var columns=new GridView();columns.Columns.Add(new GridViewColumn{Header=LocalizationService.Get("Editor.RuleElement"),DisplayMemberBinding=new System.Windows.Data.Binding("Name"),Width=280});
            for(var i=1;i<=4;i++)columns.Columns.Add(new GridViewColumn{Header=LocalizationService.Get("Editor.RuleParameter."+i),DisplayMemberBinding=new System.Windows.Data.Binding("P"+i),Width=105});
            creatures.View=columns;
            creatures.ItemsSource=session.Rows.Select(row=>new Row(row.ID,row.Name.Name(LocalizationService.CurrentLanguage)+(row.Elite?" · "+LocalizationService.Get("CreatureModifiers.Elite"):""),row.Values[0].ToString(),row.Values[1].ToString(),row.Values[2].ToString(),row.Values[3].ToString())).ToArray();
            foreach(var row in creatures.Items.Cast<Row>().Where(row=>session.Selected.Contains(row.ID)))creatures.SelectedItems.Add(row);
            if(means){rejectedInputs.Clear();var averages=session.Averages();for(var i=0;i<4;i++)inputs[i].Text=averages[i].ToString(CultureInfo.InvariantCulture);}
            foreach(var input in inputs)input.IsEnabled=Current()&&session.CanEditSelection;
        }
        finally{updating=false;}
    }
    private void SelectionChanged()
    {
        if(updating||closed)return;
        InputRevision++;
        if(!Current()){UpdateStatus();return;}
        session.Select(creatures.SelectedItems.Cast<Row>().Select(row=>row.ID));rejectedInputs.Clear();
        updating=true;
        try{var averages=session.Averages();for(var i=0;i<4;i++){inputs[i].Text=averages[i].ToString(CultureInfo.InvariantCulture);inputs[i].IsEnabled=session.CanEditSelection;}}
        finally{updating=false;}
        UpdateStatus();
    }
    private void SetParameter(int index)
    {
        if(updating||closed)return;
        InputRevision++;
        if(!Current()){rejectedInputs.Add(index);UpdateStatus();return;}
        if(!session.CanEditSelection)return;
        session.SetParameter((uint)index+1,inputs[index].Text);rejectedInputs.Remove(index);
        var canonical=MapCreatureModifiersSession.ParseInput(inputs[index].Text).ToString(CultureInfo.InvariantCulture);
        if(long.TryParse(inputs[index].Text,NumberStyles.Integer,CultureInfo.InvariantCulture,out var value)&&value is <0 or >65535)
        {updating=true;try{inputs[index].Text=canonical;}finally{updating=false;}}
        RefreshRows(false);UpdateStatus();
    }
    private void UpdateStatus()
    {status.SetResourceReference(TextBlock.TextProperty,!Current()?"CreatureModifiers.Stale":rejectedInputs.Count!=0?"CreatureModifiers.Invalid":"CreatureModifiers.SelectionHint");accept.IsEnabled=Current()&&rejectedInputs.Count==0;}
    public bool ApplyModifiers()
    {
        if(!Current()||rejectedInputs.Count!=0||!commitFields()||!Current()||rejectedInputs.Count!=0){UpdateStatus();return false;}
        try{applying=true;document.ApplyMapCreatureModifiers(session);Close();return true;}
        catch(Exception error) when(error is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or OverflowException)
        {status.SetResourceReference(TextBlock.TextProperty,"CreatureModifiers.Invalid");status.ToolTip=error.Message;return false;}
        finally{applying=false;if(!closed)UpdateStatus();}
    }
    private void DocumentChanged(object? sender,EventArgs args){if(!closed&&!applying)UpdateStatus();}
    private void LanguageChanged(object? sender,EventArgs args){if(!closed&&Current()){RefreshRows(false);UpdateStatus();}}
}
