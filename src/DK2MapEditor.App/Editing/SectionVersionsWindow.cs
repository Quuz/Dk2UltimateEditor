using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
namespace DK2MapEditor.App.Editing;
public sealed class SectionVersionsWindow:Window
{
    private readonly NativeMapDocument source;
    private readonly DataGrid table=new(){AutoGenerateColumns=false,IsReadOnly=true,CanUserAddRows=false,CanUserDeleteRows=false,CanUserSortColumns=false};
    private sealed record Row(string Section,string Input,string Working,string Current,string State);
    public SectionVersionsWindow(NativeMapDocument source)
    {
        this.source=source;Width=850;Height=460;MinWidth=650;MinHeight=360;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        SetResourceReference(TitleProperty,"Versions.Title");SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        var panel=new DockPanel{Margin=new(14)};Content=panel;
        var hint=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(0,0,0,12)};hint.SetResourceReference(TextBlock.TextProperty,source.IsLegacyProject?"Versions.Hint":"Versions.Native");
        DockPanel.SetDock(hint,Dock.Top);panel.Children.Add(hint);
        var close=new Button{IsDefault=true,IsCancel=true,HorizontalAlignment=HorizontalAlignment.Right};close.SetResourceReference(ContentControl.ContentProperty,"Report.Close");close.Click+=(_,_)=>Close();
        DockPanel.SetDock(close,Dock.Bottom);panel.Children.Add(close);
        foreach(var (key,property,width) in new[]{("Section",nameof(Row.Section),180d),("Input",nameof(Row.Input),90d),("Working",nameof(Row.Working),90d),("Expected",nameof(Row.Current),90d),("State",nameof(Row.State),220d)})
        {var header=new TextBlock();header.SetResourceReference(TextBlock.TextProperty,"Versions."+key);table.Columns.Add(new DataGridTextColumn{Header=header,Binding=new Binding(property),Width=width});}
        panel.Children.Add(table);Refresh();source.Changed+=RefreshEvent;LocalizationService.LanguageChanged+=RefreshEvent;
        Closed+=(_,_)=>{source.Changed-=RefreshEvent;LocalizationService.LanguageChanged-=RefreshEvent;table.ItemsSource=null;};
    }
    private void RefreshEvent(object? sender,EventArgs args)=>Refresh();
    private void Refresh()
    {
        static string Number(uint? value)=>value?.ToString(CultureInfo.InvariantCulture)??"—";
        table.ItemsSource=EditorSectionVersions.Capture(source).Select(row=>new Row(LocalizationService.Get("Versions."+row.Section.Key),
            Number(row.Input),Number(row.Working),Number(row.Section.Current),LocalizationService.Get("Versions."+row.State))).ToArray();
    }
}
