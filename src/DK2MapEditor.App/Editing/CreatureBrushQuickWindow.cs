using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
namespace DK2MapEditor.App.Editing;
public sealed class CreatureBrushQuickWindow:Window
{
    private readonly ObjectBatchTemplate original;
    private readonly BrushQuickPage page;
    private readonly Func<bool> current;
    private readonly Action<ObjectBatchTemplate> accept;
    private readonly Dictionary<uint,CheckBox> bits=[];
    private readonly Dictionary<uint,RadioButton> objectives=[];
    private readonly ComboBox area=new(){DisplayMemberPath="Label",SelectedValuePath="ID"},enemy=new(){DisplayMemberPath="Label",SelectedValuePath="ID"};
    private readonly uint[] areaIDs;
    private bool abort,accepted;
    public BrushQuickPage Page=>page;
    public bool Accepted=>accepted;
    private sealed record Choice(uint ID,string Label);
    public CreatureBrushQuickWindow(NativeMapDocument document,ObjectBatchTemplate original,BrushQuickPage page,Func<bool> current,Action<ObjectBatchTemplate> accept)
    {
        this.original=original;this.page=page;this.current=current;this.accept=accept;
        areaIDs=document.PatrolAreas().Select(a=>a.ID).ToArray();Width=520;SizeToContent=SizeToContent.Height;MinWidth=400;WindowStartupLocation=WindowStartupLocation.CenterOwner;ShowInTaskbar=false;
        SetResourceReference(TitleProperty,"QuickBrush."+page);SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        var panel=new StackPanel{Margin=new(16)};Content=panel;
        if(page==BrushQuickPage.Objective)
        {
            var initial=new uint[]{11,12,13,14,17,18,19,22,23,27}.Contains(original.Objective)?original.Objective:11;
            foreach(var id in new uint[]{11,12,13,14,17,18,19,22,23,27})
            {var radio=new RadioButton{GroupName="Objective",IsChecked=id==initial,Margin=new(0,3,0,3)};radio.SetResourceReference(ContentControl.ContentProperty,"Batch.Objective."+id);objectives.Add(id,radio);radio.Checked+=(_,_)=>area.IsEnabled=id==23;panel.Children.Add(radio);}
            var enemyLabel=new TextBlock();enemyLabel.SetResourceReference(TextBlock.TextProperty,"Field.Enemy");panel.Children.Add(enemyLabel);panel.Children.Add(enemy);
            var areaLabel=new TextBlock();areaLabel.SetResourceReference(TextBlock.TextProperty,"Field.Area");panel.Children.Add(areaLabel);panel.Children.Add(area);area.IsEnabled=initial==23;
            RefreshChoices();enemy.SelectedValue=original.Enemy;area.SelectedValue=areaIDs.Contains(original.Area)?original.Area:areaIDs.FirstOrDefault();
        }
        else foreach(var mask in page==BrushQuickPage.CreatureOptions?new uint[]{1,8,16,32,64,128}:[1u,4u,2u,8u])
        {var box=new CheckBox{IsChecked=((page==BrushQuickPage.CreatureOptions?original.CreatureOptions:original.HeroOptions)&mask)!=0,Margin=new(0,4,0,4)};box.SetResourceReference(ContentControl.ContentProperty,"Batch."+(page==BrushQuickPage.CreatureOptions?"CreatureBit.":"HeroBit.")+mask);bits.Add(mask,box);panel.Children.Add(box);}
        var hint=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(0,12,0,8)};hint.SetResourceReference(TextBlock.TextProperty,"QuickBrush.Hint");panel.Children.Add(hint);
        var close=new Button{IsDefault=true,IsCancel=true,HorizontalAlignment=HorizontalAlignment.Right};close.SetResourceReference(ContentControl.ContentProperty,"Report.Close");close.Click+=(_,_)=>Close();panel.Children.Add(close);
        Closing+=(_,_)=>CommitClose();LocalizationService.LanguageChanged+=LanguageChanged;Closed+=(_,_)=>LocalizationService.LanguageChanged-=LanguageChanged;
    }
    private void RefreshChoices()
    {
        var oldEnemy=enemy.SelectedValue;var oldArea=area.SelectedValue;
        enemy.ItemsSource=Enumerable.Range(0,8).Select(id=>new Choice((uint)id,id==0?LocalizationService.Get("Batch.Unspecified"):LocalizationService.Get("Owner."+id))).ToArray();
        area.ItemsSource=areaIDs.Select(id=>new Choice(id,LocalizationService.Get("Editor.RegionKind.Area")+" "+id)).ToArray();
        enemy.SelectedValue=oldEnemy;area.SelectedValue=oldArea;
    }
    private void LanguageChanged(object? sender,EventArgs args){if(page==BrushQuickPage.Objective)RefreshChoices();}
    public bool CommitClose()
    {
        if(abort||accepted||!current())return false;
        var value=page==BrushQuickPage.Objective?objectives.Single(p=>p.Value.IsChecked==true).Key:bits.Aggregate(0u,(word,p)=>p.Value.IsChecked==true?word|p.Key:word);
        var selectedEnemy=enemy.SelectedValue is uint e?e:uint.MaxValue;
        var result=EditorSpecialKeyboard.CloseQuick(original,page,value,area.SelectedValue is uint a?a:0,selectedEnemy);
        accept(result);accepted=true;return true;
    }
    public void Abort(){abort=true;Close();}
}
