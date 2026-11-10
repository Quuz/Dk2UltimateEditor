using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Private source MNGRADDPARAMS/MNGRMODPARAMS copy. Only OK publishes
/// relevant preference fields; no record, preview or map operation is retained.</summary>
public sealed class ObjectManagerOptionsWindow : Window
{
    private readonly ObjectManagerSession session;
    private readonly ObjectBatchSession target;
    private readonly bool modifying,creatures;
    private readonly Func<bool> current;
    private readonly ComboBox mode=new(),replacement=new(),coordinates=new(),creatureBits=new(),heroBits=new();
    private readonly TextBox frequency=new(){Width=80},creatureMask=new(){Width=180},heroMask=new(){Width=180};
    private readonly Slider frequencySlider=new(){Minimum=2,Maximum=32,TickFrequency=1,IsSnapToTickEnabled=true,Width=220};
    private readonly Dictionary<ObjectBatchFlags,CheckBox> flags=[];
    private readonly Dictionary<ObjectModificationMask,CheckBox> masks=[];
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap};
    private readonly Button apply=new(){IsDefault=true};
    private bool updating,invalid;
    public bool IsFinished {get;private set;}
    public bool WasApplied {get;private set;}
    private sealed record Choice(int ID,string Label);

    public ObjectManagerOptionsWindow(ObjectManagerSession session,PlacedThingKind kind,bool modifying,Func<bool> current)
    {
        this.session=session;this.modifying=modifying;creatures=kind==PlacedThingKind.Creature;this.current=current;
        target=modifying?session.Modification.Template:session.Addition;
        NameScope.SetNameScope(this,new NameScope());Width=640;Height=650;MinWidth=480;MinHeight=400;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        SetResourceReference(TitleProperty,modifying?"ManagerOptions.Modify":"ManagerOptions.Add");
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        var outer=new DockPanel{Margin=new(14)};Content=outer;
        var bottom=new StackPanel();DockPanel.SetDock(bottom,Dock.Bottom);outer.Children.Add(bottom);
        Named(status,"ManagerOptionsStatus");bottom.Children.Add(status);
        var buttons=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};bottom.Children.Add(buttons);
        Named(apply,"ManagerOptionsOK");apply.SetResourceReference(ContentControl.ContentProperty,"Editor.Apply");apply.Click+=(_,_)=>Apply();buttons.Children.Add(apply);
        var cancel=new Button();cancel.SetResourceReference(ContentControl.ContentProperty,"Editor.Cancel");cancel.Click+=(_,_)=>Cancel();buttons.Children.Add(cancel);
        var panel=new StackPanel();outer.Children.Add(new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        var hint=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(0,0,0,10)};hint.SetResourceReference(TextBlock.TextProperty,"ManagerOptions.Hint");panel.Children.Add(hint);
        if(!modifying)
        {
            Add(panel,"Batch.Mode",mode,"ManagerOptionsMode");
            Add(panel,"Batch.Frequency",frequency,"ManagerOptionsFrequency");panel.Children.Add(frequencySlider);
            frequency.Text=target.Frequency.ToString(CultureInfo.InvariantCulture);frequencySlider.Value=Math.Clamp(target.Frequency,2,32);
            frequency.TextChanged+=(_,_)=>
            {
                if(updating)return;
                if(int.TryParse(frequency.Text,NumberStyles.Integer,CultureInfo.InvariantCulture,out var value)&&value is >=2 and <=32)
                {updating=true;frequencySlider.Value=value;updating=false;}
            };
            frequencySlider.ValueChanged+=(_,_)=>{if(!updating)frequency.Text=((int)frequencySlider.Value).ToString(CultureInfo.InvariantCulture);};
        }
        else
        {
            foreach(var mask in Enum.GetValues<ObjectModificationMask>().Where(value=>value is not (ObjectModificationMask.None or ObjectModificationMask.All)))
            {
                if(creatures?mask==ObjectModificationMask.Data:mask is ObjectModificationMask.Level or ObjectModificationMask.GoldHeld or ObjectModificationMask.Health or ObjectModificationMask.CreatureOptions or ObjectModificationMask.HeroOptions or ObjectModificationMask.Objective)continue;
                var check=Named(new CheckBox{Margin=new(2,3,2,3),IsChecked=(session.Modification.Mask&mask)!=0},"ManagerOptionsMask"+mask);
                check.SetResourceReference(ContentControl.ContentProperty,"Modify.Mask."+mask);masks.Add(mask,check);panel.Children.Add(check);
                check.Checked+=(_,_)=>UpdateEnabled();check.Unchecked+=(_,_)=>UpdateEnabled();
            }
            Add(panel,"Modify.Coordinates",coordinates,"ManagerOptionsCoordinates");
            if(creatures)
            {
                Add(panel,"Modify.CreatureBits",creatureBits,"ManagerOptionsCreatureBits");Add(panel,"ManagerOptions.CreatureMask",creatureMask,"ManagerOptionsCreatureMask");
                Add(panel,"Modify.HeroBits",heroBits,"ManagerOptionsHeroBits");Add(panel,"ManagerOptions.HeroMask",heroMask,"ManagerOptionsHeroMask");
                creatureMask.Text=session.Modification.CreatureMask;heroMask.Text=session.Modification.HeroMask;
            }
        }
        if(!modifying||!creatures)Add(panel,"Batch.Policy",replacement,"ManagerOptionsReplacement");
        foreach(var flag in new[]{ObjectBatchFlags.IgnoreTerrain,ObjectBatchFlags.IgnoreFatals,ObjectBatchFlags.IgnoreEnemies,ObjectBatchFlags.IgnoreIncompatible,ObjectBatchFlags.IgnoreAsk})
        {
            if(!modifying&&flag==ObjectBatchFlags.IgnoreIncompatible)continue;
            var check=Named(new CheckBox{Margin=new(2,3,2,3),IsChecked=(target.Flags&flag)!=0},"ManagerOptionsFlag"+flag);
            check.SetResourceReference(ContentControl.ContentProperty,"Batch.Flag."+flag);flags.Add(flag,check);panel.Children.Add(check);
        }
        mode.SelectionChanged+=(_,_)=>UpdateEnabled();creatureBits.SelectionChanged+=(_,_)=>UpdateEnabled();heroBits.SelectionChanged+=(_,_)=>UpdateEnabled();
        RefreshLanguage();LocalizationService.LanguageChanged+=LanguageChanged;
        Closed+=(_,_)=>{IsFinished=true;LocalizationService.LanguageChanged-=LanguageChanged;};
        PreviewKeyDown+=(_,args)=>{if(args.Key==Key.Escape){args.Handled=true;Cancel();}};
        Loaded+=(_,_)=>{UpdateEnabled();PlaceAtMouse();};
    }
    private T Named<T>(T control,string name)where T:FrameworkElement{control.Name=name;RegisterName(name,control);return control;}
    private void Add(Panel panel,string key,FrameworkElement control,string name)
    {var label=new TextBlock{Margin=new(0,7,0,3),TextWrapping=TextWrapping.Wrap};label.SetResourceReference(TextBlock.TextProperty,key);panel.Children.Add(label);Named(control,name);panel.Children.Add(control);}
    private static int Selected(ComboBox box,int fallback)=>(box.SelectedValue as int?)??fallback;
    private static void Fill(ComboBox box,Type type,string prefix,int fallback)
    {
        var selected=Selected(box,fallback);box.DisplayMemberPath="Label";box.SelectedValuePath="ID";
        box.ItemsSource=Enum.GetValues(type).Cast<object>().Select(value=>new Choice(Convert.ToInt32(value,CultureInfo.InvariantCulture),LocalizationService.Get(prefix+value))).ToArray();box.SelectedValue=selected;
    }
    private void LanguageChanged(object? sender,EventArgs args)=>RefreshLanguage();
    private void RefreshLanguage()
    {
        if(IsFinished)return;updating=true;
        try
        {
            Fill(mode,typeof(ObjectBatchMode),"Batch.Mode.",target.Mode);Fill(replacement,typeof(ObjectReplacementPolicy),"Batch.Policy.",target.Policy);
            Fill(coordinates,typeof(ObjectModificationCoordinates),"Modify.Coordinates.",creatures?session.Modification.Coordinates:1);
            Fill(creatureBits,typeof(ObjectModificationBits),"Modify.Bits.",session.Modification.CreatureBits);Fill(heroBits,typeof(ObjectModificationBits),"Modify.Bits.",session.Modification.HeroBits);
            if(invalid)status.SetResourceReference(TextBlock.TextProperty,"ManagerOptions.Invalid");
        }
        finally{updating=false;UpdateEnabled();}
    }
    private void UpdateEnabled()
    {
        if(updating)return;
        var alive=!IsFinished&&current();apply.IsEnabled=alive;
        var move=masks.TryGetValue(ObjectModificationMask.Coordinates,out var c)&&c.IsChecked==true;
        coordinates.IsEnabled=alive&&modifying&&creatures&&move;
        replacement.IsEnabled=alive&&!creatures&&(!modifying||move);
        frequency.IsEnabled=frequencySlider.IsEnabled=alive&&!modifying&&Selected(mode,target.Mode)==(int)ObjectBatchMode.RandomRectangle;
        var creature=masks.TryGetValue(ObjectModificationMask.CreatureOptions,out var cr)&&cr.IsChecked==true;
        var hero=masks.TryGetValue(ObjectModificationMask.HeroOptions,out var hr)&&hr.IsChecked==true;
        creatureBits.IsEnabled=alive&&creature;heroBits.IsEnabled=alive&&hero;
        creatureMask.IsEnabled=alive&&creature&&Selected(creatureBits,0)==1;heroMask.IsEnabled=alive&&hero&&Selected(heroBits,0)==1;
    }
    public bool Apply()
    {
        if(IsFinished||!current())return false;
        try
        {
            var nextFlags=target.Flags;foreach(var (flag,check) in flags)nextFlags=check.IsChecked==true?nextFlags|flag:nextFlags&~flag;
            var nextMode=Selected(mode,target.Mode);var nextPolicy=Selected(replacement,target.Policy);var nextFrequency=target.Frequency;
            var nextMask=session.Modification.Mask;foreach(var (mask,check) in masks)nextMask=check.IsChecked==true?nextMask|mask:nextMask&~mask;
            var nextCoordinates=Selected(coordinates,session.Modification.Coordinates);var nextCreatureBits=Selected(creatureBits,session.Modification.CreatureBits);var nextHeroBits=Selected(heroBits,session.Modification.HeroBits);
            var nextCreatureMask=creatureMask.Text;var nextHeroMask=heroMask.Text;
            if(!modifying&&nextMode==(int)ObjectBatchMode.RandomRectangle)
            {nextFrequency=int.Parse(frequency.Text,NumberStyles.Integer,CultureInfo.InvariantCulture);if(nextFrequency is <2 or >32)throw new FormatException("Random frequency must be2–32.");}
            if(modifying&&creatures)
            {_=uint.Parse(nextCreatureMask,NumberStyles.Integer,CultureInfo.InvariantCulture);_=uint.Parse(nextHeroMask,NumberStyles.Integer,CultureInfo.InvariantCulture);}
            if(!current())return false;
            // All validation and allocation precede publication. Hidden-family
            // masks/options, AskOptions/Preview, coordinates and templates survive.
            target.Flags=nextFlags;
            if(!modifying){target.Mode=nextMode;target.Frequency=nextFrequency;if(!creatures)target.Policy=nextPolicy;}
            else
            {
                session.Modification.Mask=nextMask;
                if(creatures)
                {session.Modification.Coordinates=nextCoordinates;session.Modification.CreatureBits=nextCreatureBits;session.Modification.HeroBits=nextHeroBits;session.Modification.CreatureMask=nextCreatureMask;session.Modification.HeroMask=nextHeroMask;}
                else target.Policy=nextPolicy;
            }
            WasApplied=true;IsFinished=true;Close();return true;
        }
        catch(Exception error)when(error is FormatException or OverflowException)
        {invalid=true;status.SetResourceReference(TextBlock.TextProperty,"ManagerOptions.Invalid");status.ToolTip=error.Message;return false;}
    }
    public void Cancel(){if(!IsFinished)Close();}
    private void PlaceAtMouse()
    {
        var handle=new WindowInteropHelper(this).Handle;
        if(handle==IntPtr.Zero||!GetCursorPos(out var point)||!GetWindowRect(handle,out var bounds))return;
        var info=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};if(!GetMonitorInfo(MonitorFromPoint(point,2),ref info))return;
        var position=EditorPatrolWarningPolicy.PlaceAtCursor(point.X,point.Y,bounds.Right-bounds.Left,bounds.Bottom-bounds.Top,info.Work.Left,info.Work.Top,info.Work.Right-info.Work.Left,info.Work.Bottom-info.Work.Top);
        SetWindowPos(handle,IntPtr.Zero,position.X,position.Y,0,0,0x0001|0x0004|0x0010);
    }
    [StructLayout(LayoutKind.Sequential)]private struct NativePoint{public int X,Y;}
    [StructLayout(LayoutKind.Sequential)]private struct NativeRect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)]private struct MonitorInfo{public int Size;public NativeRect Monitor,Work;public uint Flags;}
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GetWindowRect(IntPtr window,out NativeRect bounds);
    [DllImport("user32.dll")]private static extern IntPtr MonitorFromPoint(NativePoint point,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Auto)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
}
