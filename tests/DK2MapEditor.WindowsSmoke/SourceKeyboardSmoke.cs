using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;
internal static partial class Program
{
    // Prepared callers only; UI execution is not part of this stage.
    private static void CheckSourceKeyboard(bool native,string language,string directory)
    {
        var fixture=NativeMapDocument.CreateNewMap(new("KeyboardUI","Keyboard UI",18,16,NativeFormat:native));
        var path=fixture.SaveCopy(Path.Combine(directory,$"source-keyboard-{language}-{native}"));
        var shell=new MainWindow(new AppSettings(),persistSettings:false){DiscardChangesPrompt=()=>true,SaveNoticePresenter=_=>{}};
        var workspace=(EditorWorkspace)shell.FindName("Editor");var canvas=Control<MapCanvas>(workspace,"Canvas");
        ObjectBatchWindow? brush=null;var opened=0;workspace.ObjectBrushPresenter=w=>{brush=w;opened++;};EditorGuideWindow? guide=null;shell.GuidePresenter=w=>guide=w;
        try
        {
            Expect(!shell.RequestSourceKeyboardShortcut(Key.F1,ModifierKeys.Control),"Drawing shortcut enabled without a document.");
            shell.OpenMapFromPath(path);var doc=workspace.Document!;workspace.SetSelection(new(2,2,3,3));workspace.Copy();
            var selection=canvas.Selection;var clipboard=workspace.BufferedSelection;var revision=doc.EditRevision;var before=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var history=doc.GetHistory();
            void Exact()=>Expect(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditRevision==revision&&doc.EditorChangeCount==history.EditorChangeCount
                &&doc.GetHistory().Undo.Count==history.Undo.Count&&canvas.Selection==selection&&ReferenceEquals(workspace.BufferedSelection,clipboard),"Keyboard tool/brush choice changed live data/history/selection/clipboard.");
            var tools=new[]{DrawingTool.Rectangle,DrawingTool.FilledRectangle,DrawingTool.Ellipse,DrawingTool.FilledEllipse,DrawingTool.Line,DrawingTool.Brush,DrawingTool.Fill};
            for(var i=0;i<tools.Length;i++){Expect(shell.RequestSourceKeyboardShortcut(Key.F1+i,ModifierKeys.Control)&&canvas.Tool==tools[i],"Source Ctrl+Fn chose the wrong canvas tool.");Exact();}
            for(var i=0;i<5;i++)
            {
                var count=opened;Expect(shell.RequestSourceKeyboardShortcut(Key.F1+i,ModifierKeys.Control|ModifierKeys.Shift)&&canvas.Tool==DrawingTool.PlaceObject&&opened==count,"First source placement-key choice opened the template dialog instead of choosing its brush.");Exact();
                Expect(shell.RequestSourceKeyboardShortcut(Key.F1+i,ModifierKeys.Control|ModifierKeys.Shift)&&opened==count+1&&brush is not null
                    &&((ComboBox)brush.FindName("BatchFamily")).SelectedValue as string==i.ToString(),"Repeated placement-key choice did not edit the current source-kind template.");brush!.Cancel();Exact();
            }
            var active=canvas.Tool;using(var pen=doc.BeginLivePenStroke(new(4,2,0)))Expect(!shell.RequestSourceKeyboardShortcut(Key.F2,ModifierKeys.Control)&&canvas.Tool==active,"Source shortcut interrupted a live stroke.");
            Expect(!shell.RequestSourceKeyboardShortcut(Key.Y,ModifierKeys.Control)&&!shell.RequestSourceKeyboardShortcut(Key.N,ModifierKeys.Control),"Conflicting source chords bypassed the existing modern handlers.");
            Expect(!shell.RequestSourceKeyboardShortcut(Key.R,ModifierKeys.Control|ModifierKeys.Alt),"Alt chord activated a source transform.");
            Expect(shell.RequestSourceKeyboardShortcut(Key.F1,ModifierKeys.None)&&guide is not null,"F1 failed to reach the actual guide factory.");guide!.Close();guide=null;
            LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(shell.RequestSourceKeyboardShortcut(Key.F5,ModifierKeys.Control)&&canvas.Tool==DrawingTool.Line,"Language switch changed keyboard routing.");
            shell.Close();Expect(!shell.RequestSourceKeyboardShortcut(Key.F2,ModifierKeys.Control),"Closed window accepted a source shortcut.");
        }
        finally{brush?.Cancel();guide?.Close();workspace.Document=null;shell.Close();LocalizationService.SetLanguage(language);}
    }
}
