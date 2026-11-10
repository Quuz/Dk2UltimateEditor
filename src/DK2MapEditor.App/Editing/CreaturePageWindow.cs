using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Complete typed source Attributes, Parameters, Flags or Light row page.</summary>
public sealed class CreaturePageWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly GlobalDefinitionRow row;
    private readonly Func<bool> ownerCurrent, commitFields;
    private readonly StackPanel fields=new();
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private readonly Button apply=new();
    private readonly Dictionary<string,string> inputs=[];
    private readonly List<string> registered=[];
    private CreaturePageDraft baseline;
    private bool rebuilding,closed,stale,applying,choosingColour;
    private string? statusKey;
    private sealed record Choice(string ID,string Label);
    public string Section => baseline.Section;
    public long InputRevision { get; private set; }
    public bool HasPendingInputs => inputs.Any(item=>item.Value!=baseline.Display(item.Key));
    public Func<uint,uint?>? ColourSelector { get; set; }
    public CreaturePageWindow(NativeMapDocument document,GlobalDefinitionRow row,string section,Func<bool> ownerCurrent,Func<bool> commitFields)
    {
        this.document=document;this.row=row;this.ownerCurrent=ownerCurrent;this.commitFields=commitFields;
        baseline=document.CreateCreaturePageDraft(row,section);
        foreach(var field in CreaturePageControls.Fields(section))inputs[field]=baseline.Display(field);
        NameScope.SetNameScope(this,new NameScope());SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=740;Height=660;MinWidth=540;MinHeight=420;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new DockPanel{Margin=new(12)};Content=panel;
        var hint=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(4,0,4,8)};hint.SetResourceReference(TextBlock.TextProperty,"CreaturePages.Hint");DockPanel.SetDock(hint,Dock.Top);panel.Children.Add(hint);
        var footer=new StackPanel();DockPanel.SetDock(footer,Dock.Bottom);panel.Children.Add(footer);footer.Children.Add(status);RegisterName("CreaturePagesStatus",status);
        var buttons=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};
        apply.SetResourceReference(ContentControl.ContentProperty,"CreaturePages.Apply");apply.Click+=(_,_)=>ApplyPage();RegisterName("CreaturePagesApply",apply);buttons.Children.Add(apply);
        var cancel=new Button{IsCancel=true};cancel.SetResourceReference(ContentControl.ContentProperty,"CreaturePages.Cancel");cancel.Click+=(_,_)=>Close();RegisterName("CreaturePagesCancel",cancel);buttons.Children.Add(cancel);footer.Children.Add(buttons);
        var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Content=fields};RegisterName("CreaturePagesScroll",scroll);panel.Children.Add(scroll);
        document.Changed+=DocumentChanged;LocalizationService.LanguageChanged+=LanguageChanged;
        Closed+=(_,_)=>{closed=true;InputRevision++;document.Changed-=DocumentChanged;LocalizationService.LanguageChanged-=LanguageChanged;};
        Rebuild();UpdateStatus();
    }
    private static string Text(string key)=>LocalizationService.Get("CreaturePages."+key);
    private bool Current=>!closed&&!stale&&ownerCurrent();
    private string AttractionType(string field)=>field=="Attract1ID"?inputs["Attract1Type"]:field is "Attract2ID" or "Attract2Data"?inputs["Attract2Type"]:"0";
    private string Label(string field)=>field switch
    {
        "DoNotTrain"=>Text("Trainable"),"Attract1Type"=>Text("FirstAttractionType"),"Attract2Type"=>Text("SecondAttractionType"),
        "Attract1ID"=>CreaturePageControls.AttractionSelection(field,inputs[field],inputs["Attract1Type"]) is null?"":Text("Room"),
        "Attract2ID"=>CreaturePageControls.AttractionSelection(field,inputs[field],inputs["Attract2Type"]) is null?"":Text(inputs["Attract2Type"]=="4"?"Creature":"Room"),
        "Attract2Data"=>inputs["Attract2Type"]=="0"?"":Text(inputs["Attract2Type"] switch{"1"=>"RoomTiles","2"=>"GoldAmount","3"=>"ManaAmount","4"=>"CreatureCount",_=>"Amount"}),
        _=>CreatureDefinitionFieldLabels.Label("Creatures",field)
    };
    private void Register(Control control,string field)
    {var name="CreatureProperty"+field;control.Name=name;RegisterName(name,control);registered.Add(name);}
    private void Rebuild()
    {
        rebuilding=true;
        try
        {
            Title=LocalizationService.Get("Editor.Definitions."+Section)+" · "+ValueCatalogues.Named(GlobalDefinitionDefaults.Catalogue("Creatures")[row.Index],row.ID.ToString(CultureInfo.InvariantCulture)).Name(LocalizationService.CurrentLanguage);
            foreach(var name in registered)UnregisterName(name);registered.Clear();fields.Children.Clear();
            foreach(var field in CreaturePageControls.Fields(Section))
            {
                var cell=new StackPanel{Margin=new(4,2,4,8)};var enabled=baseline.Editable&&CreaturePageControls.Enabled(field,AttractionType(field));
                if(CreaturePageControls.Boolean(Section,field))
                {
                    var check=new CheckBox{Content=Label(field),IsChecked=inputs[field]=="1",IsEnabled=enabled};
                    check.Checked+=(_,_)=>{if(!rebuilding)Changed(field,"1");};check.Unchecked+=(_,_)=>{if(!rebuilding)Changed(field,"0");};Register(check,field);cell.Children.Add(check);
                }
                else
                {
                    cell.Children.Add(new TextBlock{Text=Label(field),TextWrapping=TextWrapping.Wrap});
                    if(CreaturePageControls.Named(field))
                    {
                        var choices=CreaturePageControls.Choices(field,AttractionType(field)).Select(choice=>new Choice(choice.Value,choice.Name(LocalizationService.CurrentLanguage))).ToList();
                        var selected=field is "Attract1ID" or "Attract2ID"?CreaturePageControls.AttractionSelection(field,inputs[field],AttractionType(field)):inputs[field];
                        if(selected is not null&&!choices.Any(choice=>choice.ID==selected))choices.Insert(0,new(selected,Text("Imported")+" · "+selected));
                        var combo=new ComboBox{DisplayMemberPath="Label",ItemsSource=choices,SelectedItem=selected is null?null:choices.First(choice=>choice.ID==selected),IsEnabled=enabled};
                        combo.SelectionChanged+=(_,_)=>
                        {
                            if(rebuilding||combo.SelectedItem is not Choice choice)return;
                            if(field is "Attract1Type" or "Attract2Type")
                            {
                                if(!Current||!baseline.Editable||applying||choosingColour)return;
                                foreach(var input in CreaturePageControls.ChangeAttractionType(baseline,inputs.Select(item=>new CreaturePageInput(item.Key,item.Value)),field,choice.ID))inputs[input.Field]=input.Value;
                                InputRevision++;statusKey=null;Rebuild();UpdateStatus();
                            }
                            else Changed(field,choice.ID);
                        };Register(combo,field);cell.Children.Add(combo);
                    }
                    else
                    {
                        var input=new TextBox{Text=field=="Attract2Data"?CreaturePageControls.AttractionAmountText(inputs[field],inputs["Attract2Type"]):inputs[field],IsEnabled=enabled};input.TextChanged+=(_,_)=>NumericChanged(field,input);Register(input,field);cell.Children.Add(input);
                        input.PreviewKeyDown+=(_,e)=>
                        {
                            if(System.Windows.Input.Keyboard.Modifiers==System.Windows.Input.ModifierKeys.None
                                &&e.Key is System.Windows.Input.Key.Up or System.Windows.Input.Key.Down
                                &&StepNumber(field,e.Key==System.Windows.Input.Key.Up?1:-1))e.Handled=true;
                        };
                        var steps=new WrapPanel();foreach(var direction in new[]{-1,1})
                        {
                            var button=new System.Windows.Controls.Primitives.RepeatButton{Content=direction<0?"−":"+",IsEnabled=enabled,MinWidth=32,Delay=400,Interval=75,ToolTip=Text(direction<0?"Decrease":"Increase")};
                            button.Click+=(_,_)=>StepNumber(field,direction);Register(button,field+(direction<0?"Decrease":"Increase"));steps.Children.Add(button);
                        }
                        cell.Children.Add(steps);
                    }
                }
                if(Section=="Attributes")cell.Children.Add(new TextBlock{Text=string.Format(CultureInfo.InvariantCulture,Text("Default"),baseline.DefaultDisplay(field)),TextWrapping=TextWrapping.Wrap});
                fields.Children.Add(cell);
            }
            if(Section=="Flags")foreach(var field in new[]{"Emotionless","ImmuneLightning"})
            {
                var check=new CheckBox{Content=LocalizationService.Get("Field."+field),IsThreeState=true,IsChecked=null,IsEnabled=false,Margin=new(4,2,4,8)};Register(check,field);fields.Children.Add(check);
            }
            if(Section=="Light")
            {
                var button=new Button{Content=Text("ChooseColour"),HorizontalAlignment=HorizontalAlignment.Left,IsEnabled=baseline.Editable};button.Click+=(_,_)=>ChooseColour();Register(button,"LightColour");fields.Children.Add(button);
                try{var colour=CurrentColour();button.BorderBrush=new SolidColorBrush(Color.FromRgb((byte)colour,(byte)(colour>>8),(byte)(colour>>16)));button.BorderThickness=new(4);}catch(Exception error)when(error is FormatException or OverflowException){ }
            }
        }
        finally{rebuilding=false;}
    }
    private void Changed(string field,string value){inputs[field]=value;InputRevision++;if(!stale)statusKey=null;UpdateStatus();}
    private void NumericChanged(string field,TextBox input)
    {
        if(rebuilding)return;
        var text=input.Text;
        if(Current&&baseline.Editable&&!applying&&!choosingColour)
        {
            if(Section=="Light"&&CreatureLightInputs.Handles(field))text=CreatureLightInputs.Normalize(field,text);
            else if(Section=="Parameters"&&field=="Attract2Data"&&CreaturePageControls.Enabled(field,AttractionType(field)))
                text=CreatureParameterInputs.NormalizeAmount(text);
            else if(Section=="Attributes"&&CreatureAttributeInputs.Handles(field))text=CreatureAttributeInputs.Normalize(field,text);
        }
        if(text!=input.Text)
        {
            rebuilding=true;
            try{input.Text=text;input.CaretIndex=text.Length;}
            finally{rebuilding=false;}
        }
        Changed(field,text);
    }
    public bool StepNumber(string field,int direction)
    {
        if(!Current||!baseline.Editable||applying||choosingColour||!inputs.TryGetValue(field,out var value)||FindName("CreatureProperty"+field) is not TextBox input)return false;
        try
        {
            if(!CreaturePageControls.Enabled(field,AttractionType(field)))return false;
            input.Text=CreaturePageControls.Step(Section,field,value,direction,baseline.Native);input.CaretIndex=input.Text.Length;return true;
        }
        catch(Exception error)when(error is ArgumentException or InvalidDataException or FormatException or OverflowException){statusKey="Invalid";UpdateStatus();return false;}
    }
    private uint CurrentColour()
    {uint Component(string field)=>unchecked((byte)int.Parse(inputs[field],CultureInfo.InvariantCulture));return Component("LightR")|Component("LightG")<<8|Component("LightB")<<16;}
    public bool ChooseColour()
    {
        if(Section!="Light"||!Current||!baseline.Editable||applying||choosingColour)return false;
        var revision=InputRevision;choosingColour=true;UpdateStatus();
        try
        {
            var colour=(ColourSelector??(value=>NativeTerrainColourDialog.Choose(this,value)))(CurrentColour());
            if(colour is null||colour>0xFFFFFF||!Current||revision!=InputRevision)return false;
            inputs["LightR"]=(colour.Value&255).ToString(CultureInfo.InvariantCulture);inputs["LightG"]=((colour.Value>>8)&255).ToString(CultureInfo.InvariantCulture);inputs["LightB"]=((colour.Value>>16)&255).ToString(CultureInfo.InvariantCulture);
            InputRevision++;Rebuild();statusKey=null;return true;
        }
        catch(Exception error)when(error is ArgumentException or FormatException or OverflowException){statusKey="Invalid";return false;}
        finally{choosingColour=false;UpdateStatus();}
    }
    public bool ApplyPage()
    {
        if(!Current||!baseline.Editable||applying||choosingColour)return false;
        applying=true;var revision=InputRevision;
        try
        {
            var prepared=document.UpdateCreaturePageDraft(baseline,inputs.Select(item=>new CreaturePageInput(item.Key,item.Value)));
            if(!commitFields()||!Current||revision!=InputRevision)return false;
            if(!prepared.SameBaseline(document.CreateCreaturePageDraft(row,Section))){stale=true;statusKey="Stale";UpdateStatus();return false;}
            document.ApplyCreaturePageDraft(prepared);baseline=document.CreateCreaturePageDraft(row,Section);
            foreach(var field in CreaturePageControls.Fields(Section))inputs[field]=baseline.Display(field);
            InputRevision++;Rebuild();statusKey="Applied";UpdateStatus();return true;
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException){statusKey="Invalid";UpdateStatus();return false;}
        finally{applying=false;UpdateStatus();}
    }
    private void UpdateStatus()
    {status.Text=Text(statusKey??(!baseline.Editable?"ReadOnly":HasPendingInputs?"Draft":"Ready"));apply.IsEnabled=Current&&baseline.Editable&&!applying&&!choosingColour;}
    private void DocumentChanged(object? sender,EventArgs args){if(!closed&&!applying){stale=true;statusKey="Stale";UpdateStatus();}}
    private void LanguageChanged(object? sender,EventArgs args){if(!closed){InputRevision++;Rebuild();UpdateStatus();}}
}
