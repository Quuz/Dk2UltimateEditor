using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckWorkspaceLanguageHeaders(bool native)
    {
        LocalizationService.SetLanguage("en");
        var doc=NativeMapDocument.CreateNewMap(new("HeaderLanguage","HeaderLanguage",12,12,NativeFormat:native));
        var band=native?doc.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="GAMEBAND"),new(2,2)):doc.AddLegacyRecord("Band",new(2,2));
        doc.AddBandMember(band,14);
        var fingerprint=doc.ReadEditingFingerprint();
        var history=JsonSerializer.Serialize(doc.GetHistory());
        var revision=doc.EditRevision;
        var window=new MainWindow(new AppSettings(),persistSettings:false)
        {ShowInTaskbar=false,Left=-20000,Top=-20000,Width=1600,Height=1000,WindowStartupLocation=WindowStartupLocation.Manual};
        var documentField=typeof(MainWindow).GetField("document",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var workspace=(EditorWorkspace)window.FindName("Editor");
        try
        {
            documentField.SetValue(window,doc);workspace.Document=doc;workspace.Visibility=Visibility.Visible;
            Control<TabControl>(workspace,"WorkspaceTabs").SelectedIndex=1;
            var categories=Control<ComboBox>(workspace,"CategoryPicker");
            categories.SelectedItem=categories.Items.Cast<object>().Single(item=>Get<string>(item,"ID")==band.Category);
            var list=Control<ListBox>(workspace,"RecordList");
            list.SelectedItem=list.Items.Cast<object>().Single(item=>Get<NativeRecord>(item,"Record").File==band.File&&Get<NativeRecord>(item,"Record").Offset==band.Offset);
            var grid=Control<DataGrid>(workspace,"PropertyGrid");
            window.Show();window.UpdateLayout();
            Expect(grid.Items.Count>0,"Header test did not populate the actual property table.");
            foreach(var language in new[]{"en","ru","en"})
            {
                // The real shell's LanguageChanged subscriber refreshes its existing workspace.
                LocalizationService.SetLanguage(language);
                window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
                window.UpdateLayout();
                var expected=language=="ru"?new[]{"Поле","Значение","Значение","Смещение"}:new[]{"Field","Value","Meaning","Offset"};
                for(var i=0;i<expected.Length;i++)
                {
                    var column=grid.Columns[i];
                    Expect(Equals(column.Header,expected[i]),$"{(native?"native":"project")} {language} column {i}: expected '{expected[i]}', actual '{column.Header}'.");
                    grid.ScrollIntoView(grid.Items[0],column);grid.UpdateLayout();
                    var header=LanguageHeaderVisuals(grid).OfType<DataGridColumnHeader>().Single(h=>h.Column==column);
                    Expect(Equals(header.Content,expected[i]),$"Rendered {language} header {i} retained old text '{header.Content}'.");
                }
                Expect(list.SelectedItem is not null&&Get<NativeRecord>(list.SelectedItem,"Record").Offset==band.Offset&&grid.Items.Count>0,"Language switch lost the selected record/property rows.");
                Expect(doc.ReadEditingFingerprint()==fingerprint&&doc.EditRevision==revision&&JsonSerializer.Serialize(doc.GetHistory())==history,"Language switch changed document buffers/metadata/revision/history.");
                Console.WriteLine($"PASS {(native?"native":"project")} {language}: four rendered property headers; selected record, all transaction buffers and history unchanged");
            }
        }
        finally{documentField.SetValue(window,null);workspace.Document=null;window.Close();}
    }

    private static IEnumerable<DependencyObject> LanguageHeaderVisuals(DependencyObject root)
    {
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
        {
            var child=VisualTreeHelper.GetChild(root,i);yield return child;
            foreach(var nested in LanguageHeaderVisuals(child))yield return nested;
        }
    }
}
