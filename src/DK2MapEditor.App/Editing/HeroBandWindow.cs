using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Original private band parent: nested work belongs to one final Save.</summary>
public sealed class HeroBandWindow : Window
{
    private sealed record MemberView(int Index,string Label,BitmapSource? Icon=null);
    private readonly NativeMapDocument document;
    private readonly EditorHeroBandSession session;
    private readonly Func<bool> ownerCurrent,commitFields;
    private readonly HeroBandValuePanel template;
    private readonly ListBox members=new(){SelectionMode=SelectionMode.Extended,ItemTemplate=HeroBandValuePanel.CreatureTemplate()};
    private readonly TextBox name=new();
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)},summary=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private readonly Dictionary<string,Button> commands=[];
    private bool closed,saving;
    public NativeRecord? SavedRecord { get; private set; }
    public Func<HeroBandMemberWindow,bool>? MemberPresenter { get; set; }
    public HeroBandWindow(NativeMapDocument document,NativeRecord? band,Func<bool> ownerCurrent,Func<bool> commitFields)
    {
        this.document=document;this.ownerCurrent=ownerCurrent;this.commitFields=commitFields;session=document.CreateEditorHeroBandSession(band);
        template=new(document);NameScope.SetNameScope(this,new NameScope());
        SetResourceReference(TitleProperty,session.IsNew?"HeroBand.New":"HeroBand.Modify");SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=960;Height=800;MinWidth=700;MinHeight=460;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new DockPanel{Margin=new(12)};Content=panel;
        var heading=new StackPanel();DockPanel.SetDock(heading,Dock.Top);panel.Children.Add(heading);
        var hint=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(4,0,4,8)};hint.SetResourceReference(TextBlock.TextProperty,"HeroBand.Explanation");heading.Children.Add(hint);
        var nameLabel=new TextBlock();nameLabel.SetResourceReference(TextBlock.TextProperty,"HeroBand.Name");heading.Children.Add(nameLabel);
        name.Text=session.Name;name.IsEnabled=session.HasStoredName;name.MaxLength=63;heading.Children.Add(name);
        if(!session.HasStoredName){var noName=new TextBlock{TextWrapping=TextWrapping.Wrap};noName.SetResourceReference(TextBlock.TextProperty,"HeroBand.NoStoredName");heading.Children.Add(noName);}
        var footer=new StackPanel();DockPanel.SetDock(footer,Dock.Bottom);panel.Children.Add(footer);footer.Children.Add(summary);footer.Children.Add(status);
        var finish=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};footer.Children.Add(finish);
        Add(finish,"Save","ScenarioPage.Apply",()=>SaveBand());Add(finish,"Cancel","ScenarioPage.Cancel",()=>{Close();return true;});commands["Cancel"].IsCancel=true;
        var grid=new Grid();grid.ColumnDefinitions.Add(new(){Width=new(310)});grid.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});panel.Children.Add(grid);
        grid.Children.Add(new ScrollViewer{Content=template,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
        var right=new DockPanel{Margin=new(12,4,0,0)};Grid.SetColumn(right,1);grid.Children.Add(right);
        var buttons=new WrapPanel();DockPanel.SetDock(buttons,Dock.Bottom);right.Children.Add(buttons);
        Add(buttons,"Add","Editor.AddBandMember",()=>AddMember());Add(buttons,"Replace","HeroBand.Replace",()=>ReplaceMembers());
        Add(buttons,"Modify","HeroBand.ModifyMember",()=>ModifyMember());Add(buttons,"Remove","Editor.Delete",()=>RemoveMembers());
        right.Children.Add(members);members.SelectionChanged+=(_,_)=>RefreshButtons();members.MouseDoubleClick+=(_,_)=>ModifyMember();
        RegisterName("HeroBandName",name);RegisterName("HeroBandTemplate",template);RegisterName("HeroBandMembers",members);RegisterName("HeroBandStatus",status);
        document.Changed+=OwnerChanged;LocalizationService.LanguageChanged+=LanguageChanged;
        Closed+=(_,_)=>{closed=true;session.Dispose();document.Changed-=OwnerChanged;LocalizationService.LanguageChanged-=LanguageChanged;};
        RefreshMembers();
    }
    private void Add(Panel panel,string action,string key,Func<bool> execute)
    {
        var button=new Button{Margin=new(3)};button.SetResourceReference(ContentControl.ContentProperty,key);button.Click+=(_,_)=>execute();panel.Children.Add(button);
        commands.Add(action,button);RegisterName("HeroBand"+action,button);
    }
    private bool Current=>!closed && ownerCurrent() && document.IsEditorHeroBandSessionCurrent(session);
    private int[] Selected()=>members.SelectedItems.Cast<MemberView>().Select(m=>m.Index).Order().ToArray();
    private bool Operation(Action action)
    {
        if(!Current){RefreshButtons();return false;}
        try{action();status.Text="";RefreshMembers();return true;}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException)
        {status.Text=LocalizationService.Get("HeroBand.InvalidMember");status.ToolTip=error.Message;RefreshButtons();return false;}
    }
    public bool AddMember()=>Operation(()=>session.Add(template.ReadValues()));
    public bool ReplaceMembers()=>Operation(()=>session.Replace(Selected(),template.ReadValues()));
    public bool RemoveMembers()=>Operation(()=>session.Remove(Selected()));
    public bool ModifyMember()
    {
        if(!Current || Selected() is not [var index])return false;
        var child=new HeroBandMemberWindow(document,session,index,()=>Current){Owner=this};
        if(MemberPresenter is { } presenter)presenter(child);else child.ShowDialog();
        if(child.IsVisible)child.Close();RefreshMembers();return true;
    }
    private void RefreshMembers()
    {
        if(!Current){RefreshButtons();return;}
        var selected=Selected();
        var rows=session.Members;
        members.ItemsSource=rows.Select(m=>
        {
            var row=HeroBandDisplayImages.For(m.Index,m.Values,LocalizationService.CurrentLanguage,key=>LocalizationService.Get(key));
            return new MemberView(row.Index,row.Label,row.Icon);
        }).ToArray();
        foreach(MemberView item in members.Items)if(selected.Contains(item.Index))members.SelectedItems.Add(item);
        summary.Text=string.Format(CultureInfo.CurrentCulture,LocalizationService.Get("HeroBand.Summary"),session.ID,rows.Count,rows.Count(m=>m.IsLeader));
        RefreshButtons();
    }
    private void RefreshButtons()
    {
        var current=Current;var selected=Selected();
        commands["Save"].IsEnabled=current;commands["Add"].IsEnabled=current && session.Members.Count<16;
        commands["Replace"].IsEnabled=current && selected.Length>0;commands["Remove"].IsEnabled=current && selected.Length>0;commands["Modify"].IsEnabled=current && selected.Length==1;
        template.IsEnabled=current;name.IsEnabled=current && session.HasStoredName;
        if(!current && !saving)status.Text=LocalizationService.Get("HeroBand.Stale");
    }
    private void OwnerChanged(object? sender,EventArgs e){if(!saving)RefreshButtons();}
    private void LanguageChanged(object? sender,EventArgs e){template.RefreshLanguage();RefreshMembers();}
    public bool SaveBand()
    {
        if(!Current)return false;
        try
        {
            if(session.HasStoredName)session.SetName(name.Text);
            var plan=document.PrepareEditorHeroBand(session,"Bande de héros n°"+session.ID);
            if(!commitFields() || !Current)return false;
            saving=true;
            try{SavedRecord=document.ApplyEditorHeroBand(plan);}finally{saving=false;}
            Close();return true;
        }
        catch(EncoderFallbackException error)
        {status.Text=LocalizationService.Get("HeroBand.NameEncoding");status.ToolTip=error.Message;RefreshButtons();return false;}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException)
        {status.Text=LocalizationService.Get("HeroBand.InvalidBand");status.ToolTip=error.Message;RefreshButtons();return false;}
    }
}
