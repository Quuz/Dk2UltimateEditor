using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckEditorSelectionCallers(bool native,string language)
    {
        var doc=NativeMapDocument.CreateNewMap(new("SelectionCallerUI","SelectionCallerUI",12,12,NativeFormat:native));
        var workspace=new EditorWorkspace{Document=doc,ConfirmCopyBufferReplacement=false};
        var canvas=Control<MapCanvas>(workspace,"Canvas");canvas.PaintTile=new(2,2,1);workspace.SetSelection(new(2,2,2,2));
        var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var counter=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
        Expect(workspace.RequestCopy(cut:true),"Actual cut route did not accept equal source data.");
        var buffer=workspace.BufferedSelection!;
        Expect(doc.EditorChangeCount==unchecked(counter+1)&&doc.GetHistory().Undo.Count==history+1&&buffer is not null
            &&before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Equal UI cut did not retain one source acceptance/Undo/buffer.");
        LocalizationService.SetLanguage(language=="ru"?"en":"ru");LocalizationService.SetLanguage(language);
        Expect(ReferenceEquals(buffer,workspace.BufferedSelection)&&doc.EditorChangeCount==unchecked(counter+1),"Translation changed selection counter/buffer.");
        workspace.Undo();Expect(doc.EditorChangeCount==counter&&ReferenceEquals(buffer,workspace.BufferedSelection),"Cut Undo lost source counter or external buffer.");
        workspace.Paste();Expect(doc.EditorChangeCount==unchecked(counter+1)&&doc.GetHistory().Undo.Count==history+1,"Actual UI paste bypassed accepted equal source counter.");
        workspace.Undo();Expect(doc.EditorChangeCount==counter&&before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Paste Undo differs.");
        workspace.ConfirmCopyBufferReplacement=true;workspace.CopyBufferPrompt=_=>new(false,false);
        Expect(!workspace.RequestCopy(cut:true)&&doc.EditorChangeCount==counter&&ReferenceEquals(buffer,workspace.BufferedSelection),"Declined replacement changed count/map/buffer.");
        workspace.SetSelection(new(2,2,4,4));
        foreach(Action transform in new Action[]{workspace.Rotate,workspace.RotateCounterclockwise,workspace.MirrorHorizontal,workspace.MirrorVertical})
        {
            transform();
            Expect(doc.EditorChangeCount==unchecked(counter+1)&&doc.GetHistory().Undo.Count==history+1
                &&ReferenceEquals(buffer,workspace.BufferedSelection)&&before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Actual equal transform bypassed editor acceptance or changed buffer/bytes.");
            workspace.Undo();Expect(doc.EditorChangeCount==counter,"Actual equal transform Undo lost source count.");
        }
        workspace.SetSelection(new(2,2,4,3));workspace.Rotate();
        Expect(doc.EditorChangeCount==counter&&doc.GetHistory().Undo.Count==history,"Refused nonsquare UI rotation added count/Undo.");
        Expect(Control<TextBlock>(workspace,"TransformStatus").Text==LocalizationService.Get("Editor.RotationSquareRequired"),"Transform refusal message missing.");
        LocalizationService.SetLanguage(language=="ru"?"en":"ru");
        Expect(Control<TextBlock>(workspace,"TransformStatus").Text==LocalizationService.Get("Editor.RotationSquareRequired"),"Transform refusal did not translate.");
        LocalizationService.SetLanguage(language);
        for(var index=0;index<3;index++)
        {
            if(native)doc.AddRecord(RecordTemplate.All.Single(template=>template.Structure=="GAMEKEEPER"),new(0,3));
            else doc.AddLegacyRecord("Creature",new(0,3));
        }
        workspace.SetSelection(new(0,2,4,4));
        var objectCount=doc.EditorChangeCount;var objectHistory=doc.GetHistory().Undo.Count;
        var objectsBefore=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        foreach(Action transform in new Action[]{workspace.Rotate,workspace.RotateCounterclockwise,workspace.MirrorHorizontal,workspace.MirrorVertical})
        {
            transform();
            Expect(doc.EditorChangeCount==unchecked(objectCount+4)&&doc.GetHistory().Undo.Count==objectHistory+1
                &&ReferenceEquals(buffer,workspace.BufferedSelection),"Actual transform missed composed removal increments at protected edge.");
            workspace.Undo();
            Expect(doc.EditorChangeCount==objectCount&&objectsBefore.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Actual object transform Undo lost data/count.");
        }
        workspace.EndScenarioNamesLifetime();
        Console.WriteLine($"PASS {language} {(native?"native":"project")} actual editor cut/paste equal acceptance, one Undo, retained buffer, declined consent and EN/RU");
    }
}
