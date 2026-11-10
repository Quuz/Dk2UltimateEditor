using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only: compiling this suite does not open or render any window.
    private static void CheckProjectileChildInputs(NativeMapDocument document,string language)
    {
        var current=true;
        var slots=new[]{("Traps","Shot"),("Spells","Shot"),("Spells","Upgrade"),("CrSpells","Shot")};
        string TypeField(string family,string slot)=>slot=="Upgrade"?"UpgShotType":family=="CrSpells"?"Shot":"ShotType";
        string FirstField(string slot)=>slot=="Upgrade"?"UpgShotData1":"ShotData1";
        void SetType(string family,string slot,string type)=>document.SetGlobalDefinitionValues([new(family,0,TypeField(family,slot),type)]);
        GlobalDefinitionRow Row(string family)=>document.GlobalDefinitionRows().Single(r=>r.Family==family&&r.Index==0);
        Window Open(string family,string slot,bool trigger=false)=>family switch
        {"Traps"=>new GlobalTrapOptionsWindow(document,document.CreateGlobalTrapDraft(Row(family)),trigger,()=>current),
         "Spells"=>new GlobalSpellOptionsWindow(document,document.CreateGlobalSpellDraft(Row(family)),slot,()=>current),
         _=>new CreatureSpellOptionsWindow(document,document.CreateCreatureSpellDraft(Row(family)),slot,()=>current)};
        string Prefix(string family)=>family=="Traps"?"TrapOptions":family=="Spells"?"SpellOptions":"CreatureSpellOptions";
        bool Step(Window w,bool second,int direction)=>w switch
        {GlobalTrapOptionsWindow t=>t.StepNumber(second,direction),GlobalSpellOptionsWindow s=>s.StepNumber(second,direction),CreatureSpellOptionsWindow c=>c.StepNumber(second,direction),_=>false};
        TextBox Input(Window w,string prefix,bool second)=>(TextBox)w.FindName(prefix+(second?"Data2":"Data1"));
        void Press(RepeatButton b)=>b.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        document.SetGlobalDefinitionValues([new("Traps",0,"TriggerType","1")]);
        foreach(var (family,slot) in slots)
        {
            SetType(family,slot,"3");var initial=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
            var w=Open(family,slot);ShowManagerCallerWindow(w);var prefix=Prefix(family);
            try
            {
                var first=Input(w,prefix,false);var second=Input(w,prefix,true);first.Text="9";second.Text="4";
                var button=(RepeatButton)w.FindName(prefix+"Data2Increase");Press(button);
                Expect(first.Text=="9"&&second.Text=="5","Approved second spinner changed first field.");
                second.Text="32768.5";Expect(second.Text=="32767","Child immediate unsigned clamp missing.");
                second.Text="";Press(button);Expect(second.Text=="1"&&ReferenceEquals(second,Input(w,prefix,true))&&ReferenceEquals(button,w.FindName(button.Name)),"Child empty step failed or rebuilt held button/input.");
                LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(Input(w,prefix,true).Text=="1","Child translation discarded numeric draft.");LocalizationService.SetLanguage(language);
                current=false;Input(w,prefix,false).Text="32768";Expect(Input(w,prefix,false).Text=="32768"&&!Step(w,false,1),"Stale child gained numeric authority.");
                Expect(initial.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Private child input wrote live bytes.");
            }
            finally{w.Close();current=true;}
            SetType(family,slot,"4");document.SetGlobalDefinitionValues([new(family,0,FirstField(slot),document.IsLegacyProject?"-9":"65527")]);
            initial=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());w=Open(family,slot);ShowManagerCallerWindow(w);
            try
            {
                Expect(Input(w,prefix,false).Text=="-9","Opening normalized signed native/project alias.");
                Input(w,prefix,false).Text="-32769";Expect(Input(w,prefix,false).Text=="-32768"&&Step(w,false,-1)&&Input(w,prefix,false).Text=="-32768","Approved lower signed clamp lost.");
                Expect(!Step(w,true,1)&&initial.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Missing child second operand acquired authority.");
            }
            finally{w.Close();}
            SetType(family,slot,"5");document.SetGlobalDefinitionValues([new(family,0,FirstField(slot),"4")]);w=Open(family,slot);ShowManagerCallerWindow(w);
            try{var gold=Input(w,prefix,true);gold.Text="80001";Expect(!gold.IsEnabled&&gold.Text=="80001"&&!Step(w,true,1),"Disabled non-gold amount acquired authority.");}
            finally{w.Close();}
        }
        var triggerWindow=Open("Traps","Shot",true);ShowManagerCallerWindow(triggerWindow);
        try
        {
            var input=Input(triggerWindow,"TrapOptions",false);input.Text="32768.5";Expect(input.Text=="32767","Trigger source clamp missing.");input.Text="";
            var button=(RepeatButton)triggerWindow.FindName("TrapOptionsData1Increase");Press(button);
            Expect(input.Text=="1"&&ReferenceEquals(input,Input(triggerWindow,"TrapOptions",false))&&ReferenceEquals(button,triggerWindow.FindName(button.Name)),"Trigger step rebuilt held controls.");
        }
        finally{triggerWindow.Close();}
        var workspace=new EditorWorkspace{Document=document};Control<TabControl>(workspace,"WorkspaceTabs").SelectedIndex=9;
        void Choose(ComboBox combo,string id)=>combo.SelectedItem=combo.Items.Cast<object>().Single(item=>Get<string>(item,"ID")==id);
        try
        {
            foreach(var (family,slot) in slots)
            {
                Choose(Control<ComboBox>(workspace,"DefinitionFamily"),family);Control<ComboBox>(workspace,"DefinitionPicker").SelectedIndex=0;
                Choose(Control<ComboBox>(workspace,"DefinitionProjectileSlot"),slot);Choose(Control<ComboBox>(workspace,"DefinitionProjectileType"),"3");
                var first=Control<TextBox>(workspace,"DefinitionProjectileData1");var second=Control<TextBox>(workspace,"DefinitionProjectileData2");
                first.Text="9";second.Text="4";var button=Control<RepeatButton>(workspace,"DefinitionProjectileData2Increase");Press(button);
                Expect(first.Text=="9"&&second.Text=="5","Inline second spinner changed first.");second.Text="";Press(button);
                Expect(second.Text=="1"&&ReferenceEquals(button,workspace.FindName(button.Name)),"Inline step rebuilt held button.");
                Choose(Control<ComboBox>(workspace,"DefinitionProjectileType"),"4");first.Text="-32769";Expect(first.Text=="-32768"&&!workspace.StepProjectileNumber(true,1),"Inline signed clamp or inactive field guard differs.");
                Choose(Control<ComboBox>(workspace,"DefinitionProjectileType"),"5");Choose(Control<ComboBox>(workspace,"DefinitionProjectileTarget"),"4");Expect(!workspace.StepProjectileNumber(true,1),"Inline disabled gold acquired step authority.");
                Choose(Control<ComboBox>(workspace,"DefinitionProjectileType"),"60");Expect(!workspace.StepProjectileNumber(false,1)&&!workspace.StepProjectileNumber(true,1),"Inline named target/level acquired numeric authority.");
            }
        }
        finally{workspace.Document=null;}
    }
}
