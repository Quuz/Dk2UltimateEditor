using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class HeroBandMemberWindow : Window
{
    private readonly EditorHeroBandSession parent;
    private readonly EditorHeroBandMemberDraft draft;
    private readonly HeroBandValuePanel fields;
    private readonly Func<bool> ownerCurrent;
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private readonly Button save=new();
    private bool closed;
    public HeroBandMemberWindow(NativeMapDocument document,EditorHeroBandSession parent,int index,Func<bool> ownerCurrent)
    {
        this.parent=parent;this.ownerCurrent=ownerCurrent;draft=parent.CreateMemberDraft(index);
        fields=new(document);fields.SetValues(draft.Values);NameScope.SetNameScope(this,new NameScope());
        SetResourceReference(TitleProperty,"HeroBand.ModifyMember");SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=540;Height=760;MinWidth=420;MinHeight=420;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new DockPanel{Margin=new(12)};Content=panel;
        var owner=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(4,0,4,8)};owner.SetResourceReference(TextBlock.TextProperty,"HeroBand.FixedOwner");DockPanel.SetDock(owner,Dock.Top);panel.Children.Add(owner);
        var footer=new StackPanel();DockPanel.SetDock(footer,Dock.Bottom);panel.Children.Add(footer);footer.Children.Add(status);
        var buttons=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};footer.Children.Add(buttons);
        save.SetResourceReference(ContentControl.ContentProperty,"ScenarioPage.Apply");save.Click+=(_,_)=>ApplyMember();buttons.Children.Add(save);RegisterName("HeroMemberSave",save);
        var cancel=new Button{IsCancel=true};cancel.SetResourceReference(ContentControl.ContentProperty,"ScenarioPage.Cancel");cancel.Click+=(_,_)=>Close();buttons.Children.Add(cancel);
        RegisterName("HeroMemberFields",fields);RegisterName("HeroMemberStatus",status);panel.Children.Add(new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Content=fields});
        LocalizationService.LanguageChanged+=LanguageChanged;
        Closed+=(_,_)=>{closed=true;draft.Dispose();LocalizationService.LanguageChanged-=LanguageChanged;};
    }
    private void LanguageChanged(object? sender,EventArgs e)=>fields.RefreshLanguage();
    public bool ApplyMember()
    {
        if(closed || parent.Closed || !ownerCurrent()){save.IsEnabled=false;status.Text=LocalizationService.Get("HeroBand.Stale");return false;}
        try{draft.Values=fields.ReadValues();parent.ApplyMemberDraft(draft);Close();return true;}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException)
        {status.Text=LocalizationService.Get("HeroBand.InvalidMember");status.ToolTip=error.Message;return false;}
    }
}
