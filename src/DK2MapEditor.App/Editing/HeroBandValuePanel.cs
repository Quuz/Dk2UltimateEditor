using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Shared source creation controls and private nested member fields.</summary>
public sealed class HeroBandValuePanel : UserControl
{
    private sealed record Choice(uint Value,string Label,BitmapSource? Icon=null);
    private readonly NativeMapDocument document;
    private readonly ComboBox creature=new(),level=new(),enemy=new(),objective=new(),area=new();
    private readonly TextBox health=new(){Text="100"},gold=new(){Text="0"};
    private readonly Dictionary<uint,CheckBox> creatureBits=[],heroBits=[];
    private readonly Dictionary<uint,RadioButton> roles=[];
    private EditorHeroMemberValues baseline=new(0);
    private bool refreshing,flagsTouched,objectiveTouched;
    public HeroBandValuePanel(NativeMapDocument document)
    {
        this.document=document;NameScope.SetNameScope(this,new NameScope());
        var panel=new StackPanel{Margin=new(4)};Content=panel;
        void Add(string key,Control control,string name)
        {
            var label=new TextBlock{Margin=new(0,5,0,2),TextWrapping=TextWrapping.Wrap};label.SetResourceReference(TextBlock.TextProperty,key);panel.Children.Add(label);
            control.Margin=new(0,0,0,4);panel.Children.Add(control);RegisterName(name,control);
            if(control is ComboBox combo){combo.DisplayMemberPath="Label";combo.SelectedValuePath="Value";}
        }
        Add("HeroBand.Creature",creature,"HeroCreature");
        creature.DisplayMemberPath="";creature.ItemTemplate=CreatureTemplate();
        Add("Field.Level",level,"HeroLevel");
        Add("Field.Health",health,"HeroHealth");Add("Field.GoldHeld",gold,"HeroGold");
        var roleLabel=new TextBlock{Margin=new(0,6,0,4)};roleLabel.SetResourceReference(TextBlock.TextProperty,"HeroBand.Role");panel.Children.Add(roleLabel);
        var roleGroup="HeroBandRole"+Guid.NewGuid().ToString("N");
        foreach(var (bit,key) in new (uint,string)[]{(0,"HeroBand.Independent"),(2,"HeroBand.Leader"),(4,"HeroBand.AssistLeader")})
        {
            var radio=new RadioButton{GroupName=roleGroup,Margin=new(2)};radio.SetResourceReference(ContentControl.ContentProperty,key);
            roles.Add(bit,radio);RegisterName("HeroRole"+bit,radio);panel.Children.Add(radio);
            radio.Checked+=(_,_)=>{if(!refreshing)flagsTouched=true;};
        }
        void Bits(string header,uint[] masks,string prefix,Dictionary<uint,CheckBox> controls)
        {
            var title=new TextBlock{Margin=new(0,8,0,3),TextWrapping=TextWrapping.Wrap};title.SetResourceReference(TextBlock.TextProperty,header);panel.Children.Add(title);
            foreach(var bit in masks)
            {
                var box=new CheckBox{Margin=new(2),Content=new TextBlock{TextWrapping=TextWrapping.Wrap}};
                ((TextBlock)box.Content).SetResourceReference(TextBlock.TextProperty,prefix+bit);controls.Add(bit,box);panel.Children.Add(box);
                RegisterName((controls==creatureBits?"HeroCreatureBit":"HeroOptionBit")+bit,box);
                box.Checked+=(_,_)=>{if(!refreshing)flagsTouched=true;};box.Unchecked+=(_,_)=>{if(!refreshing)flagsTouched=true;};
            }
        }
        Bits("Field.Behavior",[1,8,16,32],"Batch.CreatureBit.",creatureBits);
        Bits("Batch.HeroOptions",[1,2,4,8],"Batch.HeroBit.",heroBits);
        Add("Field.Objective",objective,"HeroObjective");Add("Field.Enemy",enemy,"HeroEnemy");Add("Field.Area",area,"HeroArea");
        objective.SelectionChanged+=(_,_)=>{if(!refreshing){objectiveTouched=true;area.IsEnabled=objective.SelectedValue is 23u;}};
        enemy.SelectionChanged+=(_,_)=>{if(!refreshing)objectiveTouched=true;};area.SelectionChanged+=(_,_)=>{if(!refreshing)objectiveTouched=true;};
        SetValues(baseline);
    }
    private static uint Selected(ComboBox combo)=>combo.SelectedValue is uint value?value:throw new InvalidOperationException("Select a named hero field.");
    internal static DataTemplate CreatureTemplate()
    {
        var panel=new FrameworkElementFactory(typeof(DockPanel));
        var icon=new FrameworkElementFactory(typeof(Image));icon.SetValue(DockPanel.DockProperty,Dock.Left);
        icon.SetValue(FrameworkElement.WidthProperty,32.0);icon.SetValue(FrameworkElement.HeightProperty,32.0);icon.SetValue(FrameworkElement.MarginProperty,new Thickness(0,0,6,0));
        icon.SetBinding(Image.SourceProperty,new Binding("Icon"));panel.AppendChild(icon);
        var text=new FrameworkElementFactory(typeof(TextBlock));text.SetBinding(TextBlock.TextProperty,new Binding("Label"));text.SetValue(TextBlock.TextWrappingProperty,TextWrapping.Wrap);
        text.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);panel.AppendChild(text);
        return new DataTemplate{VisualTree=panel};
    }
    private void Fill(ComboBox combo,IEnumerable<Choice> source,uint? selected)
    {
        var values=source.ToList();
        if(selected is { } value && !values.Any(c=>c.Value==value))
        {
            var known=ReferenceEquals(combo,creature)?HeroBandDisplayRows.Creature(value):null;
            values.Insert(0,new(value,known?.Name(LocalizationService.CurrentLanguage)??string.Format(CultureInfo.CurrentCulture,LocalizationService.Get("HeroBand.Imported"),value),
                ReferenceEquals(combo,creature)?OriginalObjectImages.Get(HeroBandDisplayRows.Image(value)):null));
        }
        combo.ItemsSource=values;combo.SelectedValue=selected;
    }
    private void RefreshChoices(uint? id,uint? heroLevel,uint? targetEnemy,uint? targetObjective,uint? targetArea)
    {
        Fill(creature,ValueCatalogues.BandCreatures.Select(c=>new Choice(uint.Parse(c.Value,CultureInfo.InvariantCulture),c.Name(LocalizationService.CurrentLanguage),OriginalObjectImages.For(PlacedThingKind.Creature,uint.Parse(c.Value,CultureInfo.InvariantCulture)))),id);
        Fill(level,Enumerable.Range(1,10).Select(i=>new Choice((uint)i,LocalizationService.Get("Field.Level")+" "+i)),heroLevel);
        Fill(enemy,new[]{new Choice(0,LocalizationService.Get("Batch.Unspecified"))}.Concat(ValueCatalogues.Owners.Select(c=>new Choice(uint.Parse(c.Value,CultureInfo.InvariantCulture),c.Name(LocalizationService.CurrentLanguage)))),targetEnemy);
        Fill(objective,new uint[]{11,12,13,14,17,18,19,22,23,27}.Select(i=>new Choice(i,LocalizationService.Get("Batch.Objective."+i))),targetObjective);
        var areas=document.PatrolAreas().Select(p=>new Choice(p.ID,LocalizationService.Get("Field.Area")+" "+p.ID+(p.Region is { } region?" · "+region.Bounds.X+", "+region.Bounds.Y:""))).DistinctBy(c=>c.Value).ToList();
        if(!areas.Any(c=>c.Value==0))areas.Insert(0,new(0,LocalizationService.Get("Batch.Unspecified")));
        Fill(area,areas,targetArea);area.IsEnabled=targetObjective==23;
    }
    public void SetValues(EditorHeroMemberValues values)
    {
        refreshing=true;
        try
        {
            baseline=values;health.Text=values.Health.ToString(CultureInfo.InvariantCulture);gold.Text=values.GoldHeld.ToString(CultureInfo.InvariantCulture);
            RefreshChoices(values.CreatureID==0?null:values.CreatureID,(uint)values.Level,values.Enemy,values.Objective,values.Area);
            var role=(values.Options2&2)!=0?2u:(values.Options2&4)!=0?4u:0u;
            foreach(var pair in roles)pair.Value.IsChecked=pair.Key==role;
            foreach(var pair in creatureBits)pair.Value.IsChecked=(values.Options2&pair.Key)!=0;
            foreach(var pair in heroBits)pair.Value.IsChecked=(values.Options&pair.Key)!=0;
            flagsTouched=false;objectiveTouched=false;
        }
        finally{refreshing=false;}
    }
    public void RefreshLanguage()
    {
        var values=new[]{creature,level,enemy,objective,area}.Select(c=>c.SelectedValue as uint?).ToArray();
        refreshing=true;try{RefreshChoices(values[0],values[1],values[2],values[3],values[4]);}finally{refreshing=false;}
    }
    public EditorHeroMemberValues ReadValues()
    {
        var behavior=baseline.Options2;var options=baseline.Options;
        if(flagsTouched)
        {
            // Source options page rebuilds these words from its named controls.
            behavior=roles.Single(p=>p.Value.IsChecked==true).Key;
            foreach(var pair in creatureBits)if(pair.Value.IsChecked==true)behavior|=pair.Key;
            options=0;foreach(var pair in heroBits)if(pair.Value.IsChecked==true)options|=pair.Key;
        }
        var task=Selected(objective);var targetArea=objectiveTouched&&task!=23?0:Selected(area);
        return new(Selected(creature),checked((int)Selected(level)),Selected(enemy),uint.Parse(health.Text,CultureInfo.InvariantCulture),uint.Parse(gold.Text,CultureInfo.InvariantCulture),options,behavior,task,targetArea);
    }
}
