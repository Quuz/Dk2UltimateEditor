using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class RegionPropertiesWindow : Window
{
    private NativeMapDocument document;
    private RegionPropertiesSession session;
    private readonly NativeMapDocument observedOwner;
    private RegionCreationSession? creation;
    private readonly Func<bool> ownerCurrent,commitFields;
    private readonly Func<TileRect?> selection;
    private readonly TextBox x1=new(),y1=new(),x2=new(),y2=new(),name=new(),information=new();
    private readonly TextBlock status=new();
    private readonly Button apply=new();
    private readonly Dictionary<string,Button> colours=[];
    private readonly List<Control> pickLocked=[];
    private readonly Dictionary<Control,bool> pickStates=[];
    private RegionPropertiesPick? pointPick;
    private bool restorePickVisibility;
    private uint colour,textColour;
    private bool closed,closing,applying,stale,updatingCoordinates;
    private Window? child;
    private readonly EditorRequestPreferences requestPreferences;
    public Func<uint,uint?>? ColourSelector {get;set;}
    public Action<RegionPropertiesWindow>? PointRequester {get;set;}
    public Action? CancelPointPick {get;set;}
    public bool IsPointPick=>pointPick?.IsPoint==true;
    public bool HasPendingPointPick=>pointPick is not null;
    public bool IsDraftCurrent=>Current;
    public bool IsCreationMode=>creation is not null;
    public (uint Colour,uint TextColour) DraftColours=>(colour,textColour);
    public event Action<NativeRecord>? RegionCreated;
    public RegionPropertiesWindow(NativeMapDocument document,NativeRecord record,Func<bool> ownerCurrent,Func<bool> commitFields,Func<TileRect?> selection,EditorRequestPreferences? requestPreferences=null)
        :this(document,document.CreateRegionPropertiesSession(record),ownerCurrent,commitFields,selection,requestPreferences,null,document){}
    public RegionPropertiesWindow(NativeMapDocument owner,RegionCreationSession creation,Func<bool> ownerCurrent,Func<bool> commitFields,Func<TileRect?> selection,EditorRequestPreferences? requestPreferences=null)
        :this(creation.WorkingDocument,creation.Properties,ownerCurrent,commitFields,selection,requestPreferences,creation,owner){}
    private RegionPropertiesWindow(NativeMapDocument document,RegionPropertiesSession session,Func<bool> ownerCurrent,Func<bool> commitFields,
        Func<TileRect?> selection,EditorRequestPreferences? requestPreferences,RegionCreationSession? creation,NativeMapDocument observedOwner)
    {
        this.document=document;this.ownerCurrent=ownerCurrent;this.commitFields=commitFields;this.selection=selection;
        this.session=session;this.creation=creation;this.observedOwner=observedOwner;
        this.requestPreferences=requestPreferences??new();
        var values=session.Values;
        NameScope.SetNameScope(this,new NameScope());SetResourceReference(TitleProperty,creation is null?"RegionProperties.Title":"RegionProperties.CreateTitle");
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=760;Height=630;MinWidth=640;MinHeight=530;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new DockPanel{Margin=new(14)};Content=panel;
        var bottom=new StackPanel();DockPanel.SetDock(bottom,Dock.Bottom);panel.Children.Add(bottom);
        bottom.Children.Add(status);status.TextWrapping=TextWrapping.Wrap;
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};bottom.Children.Add(buttons);
        apply.IsDefault=true;Key(apply,ContentControl.ContentProperty,creation is null?"Apply":"Add");apply.Click+=(_,_)=>TryApply();RegisterName("RegionApply",apply);buttons.Children.Add(apply);
        var cancel=new Button{IsCancel=true};Key(cancel,ContentControl.ContentProperty,creation is null?"Cancel":"CloseCreation");cancel.Click+=(_,_)=>Close();buttons.Children.Add(cancel);
        var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto};panel.Children.Add(scroll);
        var fields=new StackPanel();scroll.Content=fields;
        var kind=new TextBlock{Margin=new(0,0,0,10)};kind.SetResourceReference(TextBlock.TextProperty,"Editor.RegionKind."+session.Kind);fields.Children.Add(kind);
        var coords=new WrapPanel();fields.Children.Add(coords);
        foreach(var (key,input,value) in new[]{("X1",x1,values.Bounds.X),("Y1",y1,values.Bounds.Y),
            ("X2",x2,values.Bounds.X+values.Bounds.Width-1),("Y2",y2,values.Bounds.Y+values.Bounds.Height-1)})
        {
            input.Text=value.ToString(CultureInfo.InvariantCulture);RegisterName("Region"+key,input);
            var field=new DockPanel();var arrows=new StackPanel();DockPanel.SetDock(arrows,Dock.Right);field.Children.Add(arrows);field.Children.Add(input);
            foreach(var (delta,glyph,label) in new[]{(1,"▲","Increase"),(-1,"▼","Decrease")})
            {
                var step=new RepeatButton{Content=glyph,Width=22,Height=15,Padding=new(0),Focusable=false};
                Key(step,ToolTipProperty,label);Key(step,System.Windows.Automation.AutomationProperties.NameProperty,label);
                step.Click+=(_,_)=>StepCoordinate(input,delta);arrows.Children.Add(step);pickLocked.Add(step);RegisterName("Region"+key+label,step);
            }
            if(session.Kind==MapRegionKind.Gate&&key is "X2" or "Y2")field.IsEnabled=false;
            AddField(coords,key,field,130);
            input.TextChanged+=(_,_)=>CoordinateChanged(input);
            // Source RC6700 uses ES_NUMBER. Paste/programmatic text still goes
            // through the same unsigned-prefix read at acceptance.
            input.PreviewTextInput+=(_,args)=>{if(CanEditCoordinate(input)&&args.Text.Any(c=>!char.IsAsciiDigit(c)))args.Handled=true;};
            input.PreviewKeyDown+=(_,args)=>
            {if(Keyboard.Modifiers==ModifierKeys.None&&args.Key is System.Windows.Input.Key.Up or System.Windows.Input.Key.Down)
                args.Handled=StepCoordinate(input,args.Key==System.Windows.Input.Key.Up?1:-1);};
        }
        x2.IsEnabled=y2.IsEnabled=session.Kind!=MapRegionKind.Gate;
        var use=new Button{HorizontalAlignment=HorizontalAlignment.Left};Key(use,ContentControl.ContentProperty,"UseSelection");fields.Children.Add(use);
        use.Click+=(_,_)=>{if(Current&&selection() is {} bounds){x1.Text=bounds.X.ToString(CultureInfo.InvariantCulture);y1.Text=bounds.Y.ToString(CultureInfo.InvariantCulture);
            x2.Text=(session.Kind==MapRegionKind.Gate?bounds.X:bounds.X+bounds.Width-1).ToString(CultureInfo.InvariantCulture);
            y2.Text=(session.Kind==MapRegionKind.Gate?bounds.Y:bounds.Y+bounds.Height-1).ToString(CultureInfo.InvariantCulture);}};
        pickLocked.Add(use);
        var pick=new Button{HorizontalAlignment=HorizontalAlignment.Left};Key(pick,ContentControl.ContentProperty,"PickGeometry");pick.Click+=(_,_)=>BeginPointPick();fields.Children.Add(pick);
        pickLocked.Add(pick);RegisterName("RegionPickGeometry",pick);
        name.Text=values.Name;name.MaxLength=63;name.IsEnabled=session.HasEditorLabels;RegisterName("RegionName",name);AddField(fields,"Name",name);
        information.Text=values.Information;information.MaxLength=127;information.IsEnabled=session.HasEditorLabels;
        information.Height=76;information.TextWrapping=TextWrapping.Wrap;RegisterName("RegionInformation",information);AddField(fields,"Information",information);
        colour=values.Colour;textColour=values.TextColour;
        foreach(var key in new[]{"Colour","TextColour"})
        {
            var button=new Button{IsEnabled=session.HasEditorLabels,HorizontalAlignment=HorizontalAlignment.Left};Key(button,ContentControl.ContentProperty,key);
            button.Click+=(_,_)=>ChooseColour(key);colours[key]=button;pickLocked.Add(button);fields.Children.Add(button);RegisterName("Region"+key,button);
        }
        UpdateColours();
        if(!session.HasEditorLabels){var hint=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(0,8,0,8)};Key(hint,TextBlock.TextProperty,"NativeLabels");fields.Children.Add(hint);}
        if(session.Kind is MapRegionKind.Area or MapRegionKind.Effect)
        {var options=new Button{HorizontalAlignment=HorizontalAlignment.Left};Key(options,ContentControl.ContentProperty,"Options");options.Click+=(_,_)=>ShowOptions();fields.Children.Add(options);pickLocked.Add(options);RegisterName("RegionOptions",options);}
        else
        {var generate=new Button{HorizontalAlignment=HorizontalAlignment.Left};Key(generate,ContentControl.ContentProperty,"Generation");generate.Click+=(_,_)=>ShowGeneration();fields.Children.Add(generate);pickLocked.Add(generate);RegisterName("RegionGeneration",generate);}
        var note=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(0,12,0,0)};Key(note,TextBlock.TextProperty,creation is null?"Hint":"CreateHint");fields.Children.Add(note);
        pickLocked.AddRange(new Control[]{x1,y1,x2,y2,name,information});
        observedOwner.Changed+=DocumentChanged;Closed+=(_,_)=>{closed=true;this.session.Dispose();this.creation?.Dispose();child?.Close();observedOwner.Changed-=DocumentChanged;};
        Closing+=(_,_)=>{closing=true;EndPointPick();};
        SetStatus("Ready");
    }
    private bool Current=>!closed&&!closing&&!stale&&ownerCurrent()&&document.IsRegionPropertiesSessionCurrent(session)
        &&(creation is null||observedOwner.IsRegionCreationSessionCurrent(creation));
    private static void Key(FrameworkElement target,DependencyProperty property,string key)=>target.SetResourceReference(property,"RegionProperties."+key);
    private static void AddField(Panel panel,string key,FrameworkElement control,double? width=null)
    {var cell=new StackPanel{Margin=new(0,0,12,10)};if(width is {} size)cell.Width=size;
        var label=new TextBlock{TextWrapping=TextWrapping.Wrap};Key(label,TextBlock.TextProperty,key);cell.Children.Add(label);cell.Children.Add(control);panel.Children.Add(cell);}
    private void SetStatus(string key,Exception? error=null){Key(status,TextBlock.TextProperty,key);status.ToolTip=error?.Message;apply.IsEnabled=Current&&!applying&&pointPick is null;}
    private void DocumentChanged(object? sender,EventArgs args){if(applying)return;stale=true;EndPointPick();child?.Close();SetStatus("Stale");}
    private int CoordinateExtent(TextBox input)=>ReferenceEquals(input,x1)||ReferenceEquals(input,x2)?document.Width:document.Height;
    private bool CanEditCoordinate(TextBox input)=>Current&&!applying&&pointPick is null&&child is null&&input.IsEnabled;
    private void CoordinateChanged(TextBox input)
    {
        if(updatingCoordinates||!CanEditCoordinate(input))return;
        updatingCoordinates=true;
        try
        {
            var text=RegionCoordinateRules.Normalize(input.Text,CoordinateExtent(input));
            if(input.Text!=text){input.Text=text;input.CaretIndex=text.Length;}
            if(session.Kind==MapRegionKind.Gate)
            {
                var other=ReferenceEquals(input,x1)?x2:ReferenceEquals(input,y1)?y2:null;
                if(other is not null)other.Text=RegionCoordinateRules.Value(input.Text,CoordinateExtent(input)).ToString(CultureInfo.InvariantCulture);
            }
        }
        finally{updatingCoordinates=false;}
    }
    private bool StepCoordinate(TextBox input,int delta)
    {
        if(!CanEditCoordinate(input))return false;
        input.Text=RegionCoordinateRules.Step(input.Text,CoordinateExtent(input),delta);input.CaretIndex=input.Text.Length;
        return true;
    }
    private RegionPropertiesValues Inputs()
    {
        int N(TextBox box)=>RegionCoordinateRules.Value(box.Text,CoordinateExtent(box));
        var ax=N(x1);var ay=N(y1);var bx=session.Kind==MapRegionKind.Gate?ax:N(x2);var by=session.Kind==MapRegionKind.Gate?ay:N(y2);
        return session.Values with{Bounds=new(Math.Min(ax,bx),Math.Min(ay,by),checked(Math.Abs(checked(bx-ax))+1),checked(Math.Abs(checked(by-ay))+1)),
            Name=name.Text,Information=information.Text,Colour=colour,TextColour=textColour};
    }
    public bool TryApply()
    {
        if(!Current||applying||pointPick is not null||child is not null)return false;
        try
        {
            if(!commitFields()||!Current)throw new InvalidOperationException("The map changed while committing its pending fields.");
            document.UpdateRegionProperties(session,Inputs());
            if(creation is null)
            {
                var plan=document.PrepareRegionProperties(session);
                applying=true;document.ApplyEditorRegionProperties(plan);Close();return true;
            }
            var addition=observedOwner.PrepareRegionCreation(creation);var defaults=creation.ContinuationDefaults;
            applying=true;var added=observedOwner.ApplyEditorRegionCreation(addition);
            // Like the original manager, Add keeps geometry, custom labels,
            // colours and options for the next record. Generated children are
            // committed once and are not replayed by the next Add.
            try
            {
                creation=observedOwner.CreateRegionCreationSession(session.Kind,defaults.Values.Bounds,defaults);
                document=creation.WorkingDocument;session=creation.Properties;
                name.Text=session.Values.Name;SetStatus("Added");
            }
            catch(InvalidOperationException error){SetStatus("AddedFull",error);}
            RegionCreated?.Invoke(added);return true;
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException)
        {SetStatus(Current?"Failed":"Stale",error);return false;}
        finally{applying=false;if(creation is not null)apply.IsEnabled=Current&&pointPick is null&&child is null;}
    }
    public bool BeginPointPick()
    {
        if(!Current||applying||child is not null||pointPick is not null||PointRequester is null)return false;
        try
        {
            pointPick=document.BeginRegionPropertiesPick(session);restorePickVisibility=IsVisible;
            foreach(var control in pickLocked){pickStates[control]=control.IsEnabled;control.IsEnabled=false;}
            SetStatus(IsPointPick?"PickPointHint":"PickRectangleHint");
            if(IsVisible)Hide(); // Modeless: the map stays available and receives focus next.
            PointRequester(this);
            if(pointPick is not null&&Current)return true;
            EndPointPick();return false;
        }
        catch(Exception error)when(error is InvalidOperationException or ArgumentException)
        {EndPointPick();SetStatus(Current?"Failed":"Stale",error);return false;}
    }
    public bool AcceptPointPick(TileRect bounds)
    {
        if(pointPick is not {} pick)return false;
        try
        {
            document.CompleteRegionPropertiesPick(pick,bounds);
            x1.Text=bounds.X.ToString(CultureInfo.InvariantCulture);y1.Text=bounds.Y.ToString(CultureInfo.InvariantCulture);
            x2.Text=(bounds.X+bounds.Width-1).ToString(CultureInfo.InvariantCulture);y2.Text=(bounds.Y+bounds.Height-1).ToString(CultureInfo.InvariantCulture);
            EndPointPick();SetStatus("Picked");return true;
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or OverflowException)
        {EndPointPick();SetStatus(Current?"Failed":"Stale",error);return false;}
    }
    public void EndPointPick()
    {
        var pick=pointPick;if(pick is null)return;pointPick=null;
        var cancel=CancelPointPick;CancelPointPick=null;cancel?.Invoke();document.CancelRegionPropertiesPick(pick);
        foreach(var (control,enabled) in pickStates)control.IsEnabled=enabled;pickStates.Clear();
        if(restorePickVisibility&&!closed&&!closing&&ownerCurrent()){Show();Activate();}restorePickVisibility=false;
        if(!closed&&!closing)SetStatus(Current?"PickCanceled":"Stale");
    }
    private void ChooseColour(string key)
    {
        if(!Current||pointPick is not null)return;var initial=key=="Colour"?colour:textColour;
        try{var result=(ColourSelector??(value=>NativeTerrainColourDialog.Choose(this,value)))(initial);
            if(Current&&result is {} value){if(key=="Colour")colour=value;else textColour=value;UpdateColours();}}
        catch(Exception error)when(error is InvalidOperationException or ArgumentException){SetStatus("Failed",error);}
    }
    private void UpdateColours()
    {
        foreach(var (key,button) in colours)
        {var value=key=="Colour"?colour:textColour;button.BorderBrush=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)value,(byte)(value>>8),(byte)(value>>16)));
            button.BorderThickness=new(4);button.ToolTip=$"#{value&0xFFFFFF:X6}";}
    }
    private sealed record EffectChoice(int Value,string Label);
    private void ShowGeneration()
    {
        if(!Current||child is not null||pointPick is not null)return;
        try
        {
            document.UpdateRegionProperties(session,Inputs());
            using var generation=document.BeginRegionGateGeneration(session);
            var window=new RegionGateGenerationWindow(document,generation,()=>Current,requestPreferences){Owner=this};child=window;
            try{window.ShowDialog();if(Current&&window.KeptInDraft)SetStatus("Draft");}
            finally{child=null;}
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException)
        {SetStatus(Current?"Failed":"Stale",error);}
    }
    private void ShowOptions()
    {
        if(!Current||child is not null||pointPick is not null)return;
        var values=session.Values;var window=new Window{Owner=this,Width=470,Height=430,MinWidth=400,MinHeight=350,WindowStartupLocation=WindowStartupLocation.CenterOwner};child=window;
        Key(window,TitleProperty,"Options");window.SetResourceReference(BackgroundProperty,"Brush.Background");window.SetResourceReference(ForegroundProperty,"Brush.Text");
        var layout=new DockPanel{Margin=new(14)};window.Content=layout;
        var bottom=new StackPanel();DockPanel.SetDock(bottom,Dock.Bottom);layout.Children.Add(bottom);
        var errorText=new TextBlock{TextWrapping=TextWrapping.Wrap};bottom.Children.Add(errorText);
        var done=new Button{IsDefault=true,HorizontalAlignment=HorizontalAlignment.Right};Key(done,ContentControl.ContentProperty,"Done");bottom.Children.Add(done);
        var content=new StackPanel();layout.Children.Add(new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        var flags=new List<(uint Mask,CheckBox Box)>();var choices=new List<ComboBox>();var frequency=new TextBox();
        Action? relabel=null;
        if(session.Kind==MapRegionKind.Area)
        {
            foreach(var (mask,key) in new[]{(1u,"HeroLair"),(64u,"IgnoreSolid"),(16u,"Visible"),(32u,"Toolbox")})
            {var box=new CheckBox{IsChecked=(values.AreaFlags&mask)!=0,Margin=new(0,8,0,8)};Key(box,ContentControl.ContentProperty,key);content.Children.Add(box);flags.Add((mask,box));}
        }
        else
        {
            var slots=new[]{values.Effect1,values.Effect2,values.Effect3,values.Effect4};
            for(var index=0;index<4;index++){var picker=new ComboBox{DisplayMemberPath="Label",SelectedValuePath="Value"};choices.Add(picker);AddField(content,"Effect"+(index+1),picker);}
            relabel=()=>{for(var index=0;index<4;index++)
                {var selected=choices[index].SelectedValue is int value?value:slots[index];var list=EffectCatalogue.Choices.Select(choice=>new EffectChoice(int.Parse(choice.Value,CultureInfo.InvariantCulture),choice.Name(LocalizationService.CurrentLanguage))).ToList();
                    if(list.All(choice=>choice.Value!=selected))list.Add(new(selected,selected+" · "+LocalizationService.Get("Editor.EffectGenerator.Unknown")));
                    choices[index].ItemsSource=list;choices[index].SelectedValue=selected;}};
            relabel();frequency.Text=values.Frequency.ToString(CultureInfo.InvariantCulture);AddField(content,"Frequency",frequency);
            var slider=new Slider{Minimum=1,Maximum=255,TickFrequency=1,IsSnapToTickEnabled=true,SmallChange=1,LargeChange=5,Value=Math.Clamp(values.Frequency,1,255)};
            slider.ValueChanged+=(_,_)=>frequency.Text=((int)slider.Value).ToString(CultureInfo.InvariantCulture);content.Children.Add(slider);
            content.Children.Add(FrequencyScaleCaptions.Create());
        }
        EventHandler translated=(_,_)=>relabel?.Invoke();LocalizationService.LanguageChanged+=translated;
        var optionsStored=false;
        bool StoreOptions()
        {
            if(!Current)return true;
            try
            {
                var next=values;
                if(session.Kind==MapRegionKind.Area){var bits=values.AreaFlags&~0x71u;foreach(var item in flags)if(item.Box.IsChecked==true)bits|=item.Mask;next=values with{AreaFlags=bits};}
                else next=values with{Effect1=(int)choices[0].SelectedValue,Effect2=(int)choices[1].SelectedValue,Effect3=(int)choices[2].SelectedValue,Effect4=(int)choices[3].SelectedValue,
                    Frequency=int.Parse(frequency.Text,NumberStyles.Integer,CultureInfo.InvariantCulture)};
                document.UpdateRegionProperties(session,next);optionsStored=true;SetStatus("Draft");return true;
            }
            catch(Exception error)when(error is ArgumentException or InvalidOperationException or FormatException or OverflowException)
            {Key(errorText,TextBlock.TextProperty,"Failed");errorText.ToolTip=error.Message;return false;}
        }
        done.Click+=(_,_)=>{if(StoreOptions())window.Close();};
        // Original child IDOK and IDCANCEL both keep their values in the private
        // parent context. Outer Cancel still discards the complete session.
        window.Closing+=(_,args)=>{if(Current&&!optionsStored&&!StoreOptions())args.Cancel=true;};
        try{window.ShowDialog();}
        finally{LocalizationService.LanguageChanged-=translated;child=null;}
    }
}
