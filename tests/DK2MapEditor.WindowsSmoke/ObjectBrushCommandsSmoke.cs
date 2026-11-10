using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared WPF integration callers. Compile only; do not launch the GUI.
    private static void CheckObjectBrushCommands(bool native,string language,string directory)
    {
        var fixture=NativeMapDocument.CreateNewMap(new("BrushCommands","Brush commands",18,16,NativeFormat:native));
        var path=fixture.SaveCopy(Path.Combine(directory,$"brush-commands-{language}-{native}"));
        var shell=new MainWindow(new AppSettings(),persistSettings:false){DiscardChangesPrompt=()=>true,SaveNoticePresenter=_=>{}};
        var workspace=(EditorWorkspace)shell.FindName("Editor");var canvas=Control<MapCanvas>(workspace,"Canvas");
        var menus=new[]{"CreatureBrushMenuItem","DoorBrushMenuItem","TrapBrushMenuItem","ObjectBrushMenuItem","SpecialBrushMenuItem"}.Select(n=>(MenuItem)shell.FindName(n)).ToArray();
        ObjectBatchWindow? window=null;var opened=0;workspace.ObjectBrushPresenter=w=>{window=w;opened++;};
        void Open(int index)=>menus[index].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        TextBox Field(string name)=>(TextBox)window!.FindName(name);
        try
        {
            Expect(menus.All(m=>!m.IsEnabled)&&!workspace.RequestObjectBrush(PlacedThingKind.Door),"Brush commands enabled without a document.");
            shell.OpenMapFromPath(path);var doc=workspace.Document!;Expect(menus.All(m=>m.IsEnabled),"A brush-kind menu stayed disabled after opening.");
            workspace.SetSelection(new(2,2,3,3));workspace.Copy();var selection=canvas.Selection;var clipboard=workspace.BufferedSelection;
            var bytes=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var revision=doc.EditRevision;var history=doc.GetHistory();
            void Exact()=>Expect(bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditRevision==revision&&doc.EditorChangeCount==history.EditorChangeCount
                &&doc.GetHistory().Undo.Count==history.Undo.Count&&canvas.Selection==selection&&ReferenceEquals(workspace.BufferedSelection,clipboard),"Brush template navigation/acceptance/cancel modified document, history, selection or buffer.");
            for(var index=0;index<5;index++)
            {
                Control<ComboBox>(workspace,"ToolPicker").SelectedValue=(int)DrawingTool.Line;var originalTool=canvas.Tool;
                Open(index);Expect(window is not null&&((ComboBox)window.FindName("BatchFamily")).SelectedValue as string==index.ToString()
                    &&!((ComboBox)window.FindName("BatchFamily")).IsEnabled&&((ComboBox)window.FindName("BatchFamily")).Items.Count==1,"Explicit brush menu used the Inspector kind or allowed another family.");
                Expect(!window!.BuildPreview()&&window.ApplyPreview(),"Template mode built a placement or failed to accept through Apply.");
                Expect(canvas.Tool==DrawingTool.PlaceObject,"Accepted source brush did not activate placement.");Exact();
                Open(index);Field("BatchOwner").Text="3";Field("BatchHealth").Text="61";
                Expect(window!.AcceptBrushTemplate(),"Brush could not be accepted on a protected/solid cursor tile without placement.");Exact();
                Open(index);Expect(Field("BatchOwner").Text=="3"&&Field("BatchHealth").Text=="61","Explicit kind did not retain its accepted private template.");
                Field("BatchOwner").Text="4";Field("BatchHealth").Text="99";window!.Cancel();Exact();
                Open(index);Expect(Field("BatchOwner").Text=="3"&&Field("BatchHealth").Text=="61","Cancelled draft overwrote the accepted source template.");
                Field("BatchTypeID").Text="4294967295";Expect(!window!.AcceptBrushTemplate(),"Invalid named object type activated a brush.");window.Cancel();Exact();
                Control<ComboBox>(workspace,"ToolPicker").SelectedValue=(int)DrawingTool.Line;Open(index);window!.Cancel();Expect(canvas.Tool==originalTool,"Cancel switched the active drawing tool.");
            }
            window=workspace.CreateObjectPaintingWindow()!;Field("BatchOwner").Text="7";window.Cancel();
            Open(0);Expect(Field("BatchOwner").Text=="3","Cancelled generic single-placement draft overwrote the source brush defaults.");window!.Cancel();Exact();
            var count=opened;using(var pen=doc.BeginLivePenStroke(new(4,2,0))){Open(0);Expect(opened==count,"Live pen opened a source brush template.");}
            var foreign=NativeMapDocument.CreateNewMap(new("ForeignBrush","Foreign brush",18,16,NativeFormat:native));
            Expect(!workspace.RequestObjectBrush(PlacedThingKind.Trap,foreign)&&opened==count,"Stale expected owner opened a source brush.");
            Open(1);LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(window!.Title==LocalizationService.Get("Brush.TemplateTitle")&&((ComboBox)window.FindName("BatchFamily")).Items.Count==1,"Live language changed the locked kind or retained old title.");
            workspace.Document=foreign;Expect(!window.AcceptBrushTemplate(),"Old source owner accepted a brush after map replacement.");window=null;workspace.Document=doc;
            Open(0);shell.Close();Expect(!window!.AcceptBrushTemplate(),"Closed main window accepted a retained brush template.");count=opened;Open(2);Expect(opened==count,"Closed main-window route reopened the brush editor.");
        }
        finally{window?.Cancel();workspace.Document=null;shell.Close();LocalizationService.SetLanguage(language);}
    }
}
