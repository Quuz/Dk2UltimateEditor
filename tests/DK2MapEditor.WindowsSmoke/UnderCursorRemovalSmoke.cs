using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckUnderCursorRemoval(bool native,string language)
    {
        var doc=NativeMapDocument.CreateNewMap(new("CursorUI","CursorUI",16,16,NativeFormat:native));
        for(var index=0;index<3;index++)
        {
            if(native)doc.AddRecord(RecordTemplate.All.Single(template=>template.Structure=="GAMEKEEPER"),new(4,5));else doc.AddEditorLegacyRecord("Creature",new(4,5));
        }
        doc.EnsureScriptScope(doc.ScriptOwners().First(owner=>owner.Scope==2));var workspace=new EditorWorkspace{Document=doc};var host=new Window{Content=workspace};ShowManagerCallerWindow(host);
        var canvas=Control<MapCanvas>(workspace,"Canvas");workspace.SetSelection(new(8,8,2,2));workspace.Copy();var buffer=workspace.BufferedSelection;var selected=canvas.Selection;
        var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());void Exact()=>Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Cursor UI changed unrelated game bytes");
        bool Request(ModifierKeys modifiers,Point point)=>(bool)typeof(EditorWorkspace).GetMethod("RequestUnderCursorRemovalAt",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(workspace,[Key.Delete,modifiers,point])!;
        Point Pointer()=>new(4.5*canvas.CellWidth,5.5*canvas.CellHeight);
        Expect(!Request(ModifierKeys.Shift|ModifierKeys.Control,Pointer()) && !Request(ModifierKeys.None,new(-1,0)),"Cursor shortcut accepted forbidden modifiers/outside pointer");
        canvas.GeometryOnly=true;Expect(!Request(ModifierKeys.None,Pointer()),"Geometry-only preview allowed object removal");canvas.GeometryOnly=false;
        workspace.UnderCursorRemovalPresenter=window=>{Expect(window.Request.Prompt==EditorConfirmationPrompt.UsedCursorRemoval && window.Title==LocalizationService.Get("ThingRequest.CursorTitle"),"Cursor route acquired manager/general request");window.Close();};
        var history=doc.GetHistory().Undo.Count;Expect(Request(ModifierKeys.None,Pointer()) && doc.GetHistory().Undo.Count==history,"Cursor Cancel allocated history");Exact();
        workspace.UnderCursorRemovalPresenter=window=>
        {
            LocalizationService.SetLanguage(language=="en"?"ru":"en");
            typeof(MapCanvas).GetMethod("MoveMapPointer",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(canvas,[new Point(0,0)]);
            Expect(window.Accept(),"Frozen cursor tile became stale after pointer/language change");
        };
        Expect(Request(ModifierKeys.None,Pointer()) && doc.ObjectManagerEntries().Count==0 && doc.GetHistory().Undo.Count==history+3,"Cursor UI grouped per-record history or retargeted the moving pointer");
        Expect(canvas.Selection==selected && ReferenceEquals(buffer,workspace.BufferedSelection),"Cursor command changed map selection/copy buffer");
        doc.Undo();Expect(doc.ObjectManagerEntries().Count==1,"One Undo failed to restore one source item");doc.Undo();doc.Undo();Exact();
        LocalizationService.SetLanguage(language);host.Close();
        Console.WriteLine($"PASS WPF under-cursor routing/frozen tile/per-item history {language} {native}");
    }
}
