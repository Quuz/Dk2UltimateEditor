using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;
using Microsoft.Win32;

namespace DK2MapEditor.App.Editing;

public sealed class MapSpeechTextWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly Func<bool> ownerCurrent, commitFields;
    private readonly Action<GameResourceSettings> configure;
    private readonly GameAudioPreview audio = new();
    private readonly DockPanel panel = new() { Margin = new(12) };
    private readonly Dictionary<string, FrameworkElement> controls = [];
    private MapSpeechTextDraft baseline;
    private Dictionary<string,string> inputs;
    public bool HasPendingInputs => !closed && inputs.Any(row => baseline.Values.GetValueOrDefault(row.Key) != row.Value);
    public long InputRevision { get; private set; }
    private GameResourceSettings settings;
    private IReadOnlyList<MapSpeechResource> banks = [];
    private GameSpeechBank? selectedBank;
    private GameTextResource? speechText;
    private int mentorMaximum = MapSpeechText.DefaultMentorMaximum, selectedTab;
    private bool updating, stale, closed, applying, briefing;
    private int? selectedTextId, selectedSoundId;
    private string? displayedText, displayedBank;
    private string? statusKey, resourceStatusKey;
    private sealed record Choice(string ID, string Label);
    private sealed record PreviewRow(int ID, string Label, string Text);
    public Func<string?>? FolderPicker { get; set; }
    public Func<string?>? PlayerPicker { get; set; }
    public Action<AudioPreviewLaunch>? PlaybackLauncher { get; set; }
    public MapSpeechTextWindow(NativeMapDocument document, Func<bool> ownerCurrent, Func<bool> commitFields, GameResourceSettings settings, Action<GameResourceSettings> configure)
    {
        this.document=document;this.ownerCurrent=ownerCurrent;this.commitFields=commitFields;this.configure=configure;
        this.settings = settings with { Root = settings.Root ?? DetectRoot(document.MainFile.SourcePath) };
        NameScope.SetNameScope(this,new NameScope()); SetResourceReference(TitleProperty,"SpeechText.Title"); SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=900;Height=780;MinWidth=780;MinHeight=600;WindowStartupLocation=WindowStartupLocation.CenterOwner;Content=panel;
        LoadResourceContext();baseline=document.CreateMapSpeechTextDraft(banks.Select(row=>row.Name),mentorMaximum);inputs=baseline.Values.ToDictionary(row=>row.Key,row=>row.Value);
        document.Changed+=DocumentChanged;LocalizationService.LanguageChanged+=LanguageChanged;
        Closed+=(_,_)=>{closed=true;document.Changed-=DocumentChanged;LocalizationService.LanguageChanged-=LanguageChanged;audio.Dispose();};
        Rebuild();
    }
    private static string Text(string key)=>LocalizationService.Get("SpeechText."+key);
    private static string FieldText(string key)
    {
        return MapMetadataCaptions.ForSemantic(key)?.Render(name=>LocalizationService.Get(name))
            ??LocalizationService.Get("SpeechText.Field."+key,LocalizationService.Get("Field."+key,key));
    }
    internal static string? DetectRoot(string source)
    {
        if (!File.Exists(source)) return null;
        for(var directory=new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(source))!);directory is not null;directory=directory.Parent)
            if(Directory.Exists(Path.Combine(directory.FullName,"Data","Text","Default")) || Directory.Exists(Path.Combine(directory.FullName,"Data","Sound","sfx")))return directory.FullName;
        return null;
    }
    private static bool ResourceError(Exception e)=>e is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or OverflowException or System.ComponentModel.Win32Exception;
    private void LoadResourceContext()
    {
        banks=[];mentorMaximum=MapSpeechText.DefaultMentorMaximum;resourceStatusKey=null;
        if(string.IsNullOrWhiteSpace(settings.Root)){resourceStatusKey="Missing";return;}
        try
        {
            banks=MapTextCatalogues.SpeechBanks(settings.Root);
            var mentor=banks.FirstOrDefault(row=>row.Name.Equals("SPEECH_MENTOR",StringComparison.OrdinalIgnoreCase));
            if(mentor is not null) mentorMaximum=Math.Min(ushort.MaxValue,GameSpeechBank.Load(mentor.FilePath).Entries.Count);
        }
        catch(Exception e) when(ResourceError(e)){resourceStatusKey="Missing";}
    }
    private T Register<T>(T control,string name) where T:FrameworkElement { control.Name=name;RegisterName(name,control);controls.Add(name,control);return control; }
    private Button Button(string name,string key,Action action)
    {var button=Register(new Button(),name);button.SetResourceReference(ContentControl.ContentProperty,"SpeechText."+key);button.Click+=(_,_)=>action();return button;}
    private void Rebuild()
    {
        updating=true;
        try
        {
            foreach(var name in controls.Keys)UnregisterName(name);controls.Clear();panel.Children.Clear();
            var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);panel.Children.Add(top);
            var configuration=new WrapPanel();top.Children.Add(configuration);
            configuration.Children.Add(Button("SpeechTextFolder","Folder",ChooseFolder));configuration.Children.Add(Button("SpeechTextPlayer","Player",ChoosePlayer));
            configuration.Children.Add(Button("SpeechTextDefaultPlayer","DefaultPlayer",()=>SetConfiguration(settings with{Player=null})));
            top.Children.Add(new TextBlock{Text=(settings.Root??Text("Missing"))+" · "+(settings.Player??Text("DefaultPlayer")),TextWrapping=TextWrapping.Wrap,Margin=new(4)});
            top.Children.Add(new TextBlock{Text=Text("Explanation"),TextWrapping=TextWrapping.Wrap,Margin=new(4)});
            var footer=new StackPanel();DockPanel.SetDock(footer,Dock.Bottom);panel.Children.Add(footer);
            footer.Children.Add(Register(new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(4)},"SpeechTextStatus"));
            var actions=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};footer.Children.Add(actions);
            actions.Children.Add(Button("SpeechTextApply","Apply",()=>ApplyOptions()));actions.Children.Add(Button("SpeechTextDiscard","Discard",ResetDraft));var close=Button("SpeechTextClose","Close",Close);close.IsCancel=true;actions.Children.Add(close);
            var tabs=Register(new TabControl{SelectedIndex=selectedTab},"SpeechTextTabs");panel.Children.Add(tabs);
            tabs.SetResourceReference(BackgroundProperty,"Brush.Panel");tabs.SetResourceReference(BorderBrushProperty,"Brush.Border");
            var textFields=new StackPanel();var speechFields=new StackPanel();
            tabs.Items.Add(new TabItem{Header=Text("Texts"),Content=new ScrollViewer{Content=textFields,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}});
            tabs.Items.Add(new TabItem{Header=Text("Speech"),Content=new ScrollViewer{Content=speechFields,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}});
            tabs.SelectedIndex=selectedTab;tabs.SelectionChanged+=(_,args)=>{if(!updating && ReferenceEquals(args.Source,tabs))selectedTab=tabs.SelectedIndex;};
            AddChoice(textFields,"TextTableID",MapTextCatalogues.Tables.Select(row=>new Choice(row.Identifier.ToString(CultureInfo.InvariantCulture),row.Name(LocalizationService.CurrentLanguage))),()=>RefreshTextPreview());
            var numeric=new WrapPanel();textFields.Children.Add(numeric);foreach(var key in MapSpeechText.TextIds)AddNumber(numeric,key,9999);
            for(var i=0;i<8;i++)
            {
                var row=new WrapPanel();textFields.Children.Add(row);AddChoice(row,"TextIntroCreatureID"+i,MapTextCatalogues.IntroCreatures.Select(choice=>new Choice(choice.Value,choice.Name(LocalizationService.CurrentLanguage))));AddNumber(row,"TextIntroTextID"+i,9999);
            }
            var source=Register(new ComboBox{ItemsSource=new[]{Text("Current"),Text("Briefing")},SelectedIndex=briefing?1:0,Margin=new(4)},"SpeechTextPreviewSource");textFields.Children.Add(source);
            source.SelectionChanged+=(_,_)=>{if(!updating){briefing=source.SelectedIndex==1;RefreshTextPreview();}};
            AddPreview(textFields,"Text");
            AddChoice(speechFields,"SpeechFile",new[]{new Choice("",Text("None"))}.Concat(banks.Select(row=>new Choice(row.Name,row.Name))),RefreshSpeechPreview);
            var speechIds=new WrapPanel();speechFields.Children.Add(speechIds);foreach(var key in MapSpeechText.SpeechIds)AddNumber(speechIds,key,mentorMaximum);
            speechFields.Children.Add(Button("SpeechTextPlay","Play",()=>PlaySelected()));AddPreview(speechFields,"Sound");
        }
        finally{updating=false;}
        RefreshTextPreview();RefreshSpeechPreview();UpdateStatus();
    }
    private void AddNumber(Panel parent,string key,int maximum)
    {
        var cell=new StackPanel{Width=340,Margin=new(4,4,12,8)};cell.Children.Add(new TextBlock{Text=FieldText(key)+$" (0–{maximum})",TextWrapping=TextWrapping.Wrap});
        var input=Register(new TextBox{Text=inputs.GetValueOrDefault(key,""),IsEnabled=inputs.ContainsKey(key),Margin=new(0,4,0,0)},"SpeechTextField"+key);
        var number=new DockPanel();cell.Children.Add(number);parent.Children.Add(cell);
        foreach(var delta in new[]{1,-1})
        {
            var step=Register(new Button{Content=delta>0?"+":"−",Width=32,Padding=new(3),IsEnabled=input.IsEnabled,ToolTip=Text(delta>0?"Increase":"Decrease")},"SpeechText"+(delta>0?"Increase":"Decrease")+key);
            DockPanel.SetDock(step,Dock.Right);number.Children.Add(step);step.Click+=(_,_)=>{if(!closed && ownerCurrent()){if(int.TryParse(input.Text,NumberStyles.Integer,CultureInfo.InvariantCulture,out var value))input.Text=Math.Clamp((long)value+delta,0,maximum).ToString(CultureInfo.InvariantCulture);else{statusKey="Invalid";UpdateStatus();}}};
        }
        number.Children.Add(input);
        input.TextChanged+=(_,_)=>{if(!updating){InputRevision++;inputs[key]=input.Text;if(!stale)statusKey=null;UpdateStatus();}};
    }
    private void AddChoice(Panel parent,string key,IEnumerable<Choice> source,Action? changed=null)
    {
        var choices=source.ToList();var current=inputs.GetValueOrDefault(key,"");if(inputs.ContainsKey(key) && choices.All(row=>row.ID!=current))choices.Add(new(current,Text("Unknown")+" · "+current));
        var cell=new StackPanel{Width=340,Margin=new(4,4,12,8),HorizontalAlignment=HorizontalAlignment.Left};cell.Children.Add(new TextBlock{Text=FieldText(key)});
        var combo=Register(new ComboBox{ItemsSource=choices,DisplayMemberPath="Label",SelectedItem=choices.FirstOrDefault(row=>row.ID==current),IsEnabled=inputs.ContainsKey(key),Margin=new(0,4,0,0)},"SpeechTextField"+key);cell.Children.Add(combo);parent.Children.Add(cell);
        combo.SelectionChanged+=(_,_)=>{if(!updating && combo.SelectedItem is Choice choice){InputRevision++;inputs[key]=choice.ID;if(!stale)statusKey=null;changed?.Invoke();UpdateStatus();}};
    }
    private void AddPreview(Panel parent,string kind)
    {
        parent.Children.Add(Register(new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(4)},"SpeechText"+kind+"ResourceStatus"));
        var list=Register(new ListBox{Height=180,DisplayMemberPath="Label",Margin=new(4)},"SpeechText"+kind+"List");parent.Children.Add(list);
        var preview=Register(new TextBox{IsReadOnly=true,TextWrapping=TextWrapping.Wrap,AcceptsReturn=true,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Height=140,Margin=new(4)},"SpeechText"+kind+"Preview");parent.Children.Add(preview);
        list.SelectionChanged+=(_,_)=>{if(list.SelectedItem is PreviewRow row){preview.Text=row.Text;if(kind=="Text")selectedTextId=row.ID;else selectedSoundId=row.ID;}else preview.Clear();};
        if(kind=="Sound")list.MouseDoubleClick+=(_,_)=>PlaySelected();
    }
    private void RefreshTextPreview()
    {
        var list=(ListBox)controls["SpeechTextTextList"];list.ItemsSource=null;((TextBox)controls["SpeechTextTextPreview"]).Clear();
        var status=(TextBlock)controls["SpeechTextTextResourceStatus"];status.Text=Text("Missing");
        try
        {
            var table=MapTextCatalogues.Tables.FirstOrDefault(row=>row.Identifier.ToString(CultureInfo.InvariantCulture)==inputs.GetValueOrDefault("TextTableID"));var file=briefing?table?.BriefingFile:table?.CurrentFile;
            if(file is null || string.IsNullOrWhiteSpace(settings.Root))return;
            var resource=GameTextResource.Load(MapTextCatalogues.TextResourcePath(settings.Root,file));
            var path=MapTextCatalogues.TextResourcePath(settings.Root,file);if(displayedText!=path)selectedTextId=null;displayedText=path;
            var rows=resource.Entries.Select(row=>new PreviewRow(row.Identifier,$"{row.Identifier} · {row.Preview[..Math.Min(80,row.Preview.Length)]}",row.Preview)).ToArray();list.ItemsSource=rows;list.SelectedItem=rows.FirstOrDefault(row=>row.ID==selectedTextId);status.Text=file+" · "+resource.Entries.Count.ToString(CultureInfo.InvariantCulture);
        }
        catch(Exception e)when(ResourceError(e)){status.Text=Text("Missing");}
    }
    private void RefreshSpeechPreview()
    {
        selectedBank=null;speechText=null;var list=(ListBox)controls["SpeechTextSoundList"];list.ItemsSource=null;((TextBox)controls["SpeechTextSoundPreview"]).Clear();
        var status=(TextBlock)controls["SpeechTextSoundResourceStatus"];status.Text=Text("Missing");
        try
        {
            var bank=banks.FirstOrDefault(row=>row.Name.Equals(inputs.GetValueOrDefault("SpeechFile"),StringComparison.OrdinalIgnoreCase));if(bank is null || string.IsNullOrWhiteSpace(settings.Root))return;
            selectedBank=GameSpeechBank.Load(bank.FilePath);
            try{speechText=GameTextResource.Load(MapTextCatalogues.TextResourcePath(settings.Root,bank.TextFile));}catch(Exception e)when(ResourceError(e)){/* Missing captions do not disable valid audio. */}
            if(displayedBank!=bank.FilePath)selectedSoundId=null;displayedBank=bank.FilePath;
            var rows=selectedBank.Entries.Select(row=>new PreviewRow(row.Identifier,$"{row.Identifier} · {row.Name} · {row.Length}",speechText is not null && row.Identifier<=speechText.Entries.Count?speechText.Entry(row.Identifier).Preview:Text("Missing"))).ToArray();list.ItemsSource=rows;list.SelectedItem=rows.FirstOrDefault(row=>row.ID==selectedSoundId);status.Text=bank.Name+" · "+selectedBank.Entries.Count.ToString(CultureInfo.InvariantCulture);
        }
        catch(Exception e)when(ResourceError(e)){selectedBank=null;status.Text=Text("Missing");}
    }
    public bool PlaySelected()
    {
        if(closed || !ownerCurrent() || selectedBank is null || controls["SpeechTextSoundList"] is not ListBox{SelectedItem:PreviewRow row})return false;
        try
        {
            var path=audio.Prepare(selectedBank.AudioPayload(row.ID));var launch=new AudioPreviewLaunch(path,settings.Player);
            (PlaybackLauncher??GameAudioPreview.Launch)(launch);if(!closed){statusKey="Played";UpdateStatus();}return true;
        }
        catch(Exception e)when(ResourceError(e) || e is InvalidOperationException){if(!closed){statusKey="PlaybackFailed";UpdateStatus();}return false;}
    }
    private void ChooseFolder()
    {
        if(closed || !ownerCurrent())return;
        string? path;
        if(FolderPicker is not null)path=FolderPicker();else{var dialog=new OpenFolderDialog{Title=Text("Folder")};path=dialog.ShowDialog(this)==true?dialog.FolderName:null;}
        if(path is not null)SetConfiguration(settings with{Root=path});
    }
    private void ChoosePlayer()
    {
        if(closed || !ownerCurrent())return;string? path;
        if(PlayerPicker is not null)path=PlayerPicker();else{var dialog=new OpenFileDialog{Title=Text("Player"),Filter=Text("PlayerFilter"),CheckFileExists=true};path=dialog.ShowDialog(this)==true?dialog.FileName:null;}
        if(path is not null)SetConfiguration(settings with{Player=path});
    }
    public bool SetConfiguration(GameResourceSettings value)
    {
        if(closed || !ownerCurrent())return false;
        try
        {
            if(!string.IsNullOrWhiteSpace(value.Root) && !Directory.Exists(value.Root) || !string.IsNullOrWhiteSpace(value.Player) && !File.Exists(value.Player))throw new IOException("Missing configured path.");
            if(!stale)try{document.UpdateMapSpeechText(baseline,baseline.Values.Select(row=>new MapSpeechTextInput(row.Key,row.Value)));}catch(InvalidOperationException){stale=true;statusKey="Stale";}
            settings=value with{Root=string.IsNullOrWhiteSpace(value.Root)?null:Path.GetFullPath(value.Root),Player=string.IsNullOrWhiteSpace(value.Player)?null:Path.GetFullPath(value.Player)};
            configure(settings);LoadResourceContext();if(!stale)baseline=document.CreateMapSpeechTextDraft(banks.Select(row=>row.Name),mentorMaximum);Rebuild();return true;
        }
        catch(Exception e)when(ResourceError(e)){statusKey="Missing";UpdateStatus();return false;}
    }
    private void ResetDraft()
    {if(closed || !ownerCurrent())return;InputRevision++;LoadResourceContext();baseline=document.CreateMapSpeechTextDraft(banks.Select(row=>row.Name),mentorMaximum);inputs=baseline.Values.ToDictionary(row=>row.Key,row=>row.Value);stale=false;statusKey=null;Rebuild();}
    private void UpdateStatus()
    {
        ((TextBlock)controls["SpeechTextStatus"]).Text=Text(statusKey??(!baseline.Editable?"ReadOnly":baseline.Partial?"Partial":resourceStatusKey??"Hint"));
        controls["SpeechTextApply"].IsEnabled=!closed && !stale && ownerCurrent() && baseline.Editable;controls["SpeechTextDiscard"].IsEnabled=!closed && ownerCurrent();controls["SpeechTextPlay"].IsEnabled=!closed && ownerCurrent() && selectedBank is not null;
    }
    public bool ApplyOptions()
    {
        if(closed || stale || !ownerCurrent() || !baseline.Editable)return false;
        try{var prepared=document.UpdateMapSpeechText(baseline,inputs.Select(row=>new MapSpeechTextInput(row.Key,row.Value)));if(!commitFields() || closed || stale || !ownerCurrent())return false;applying=true;document.ApplyMapSpeechText(prepared);applying=false;ResetDraft();return true;}
        catch(Exception e)when(e is ArgumentException or InvalidOperationException or FormatException or OverflowException or InvalidDataException){if(e is InvalidOperationException)stale=true;statusKey=stale?"Stale":"Invalid";UpdateStatus();return false;}
        finally{applying=false;}
    }
    private void DocumentChanged(object? sender,EventArgs args){if(!closed && !applying){stale=true;statusKey="Stale";UpdateStatus();}}
    private void LanguageChanged(object? sender,EventArgs args){if(!closed){InputRevision++;Rebuild();}}
}
