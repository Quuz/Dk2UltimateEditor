using System.Buffers.Binary;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckNativeGateMutationWindow(string language)
    {
        var doc=NativeMapDocument.CreateNewMap(new("GateMutationUI","GateMutationUI",32,30,NativeFormat:true));
        var gate=doc.AddMapRegion(MapRegionKind.Gate,new(3,5,1,1));
        var action=doc.AddScenarioEvent(0xD6,new(0,0),LegacyEventSchemas.All.Single(type=>type.Name=="ACTION_DESTROYGATE"));
        doc.ApplyScenarioEditorParameterDraft(doc.UpdateScenarioGate(doc.CreateScenarioParameterDraft(action),doc.ScenarioGateChoices().Single().Key));
        gate=doc.AddMapRegion(MapRegionKind.Gate,new(3,5,1,1));
        doc.ApplyScenarioEditorParameterDraft(doc.UpdateScenarioGate(doc.CreateScenarioParameterDraft(action),doc.ScenarioGateChoices().Last().Key));
        var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
        var window=new RegionPropertiesWindow(doc,gate,()=>true,()=>true,()=>null);
        try
        {
            ((TextBox)window.FindName("RegionX1")).Text="9";((TextBox)window.FindName("RegionY1")).Text="11";
            Expect(before.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Region window wrote a gate draft before Apply: "+language);
            Expect(window.TryApply(),"Region window rejected native gate move: "+language);
            Expect(BinaryPrimitives.ReadUInt16LittleEndian(action.File.Data.Span.Slice(action.Offset+12))==10&&BinaryPrimitives.ReadUInt16LittleEndian(action.File.Data.Span.Slice(action.Offset+14))==12,"Actual region window left a destroy action at the old gate.");
            Expect(doc.GetHistory().Undo.Count==history+1&&doc.EditorChangeCount==unchecked(count+1),"Actual gate window added derived counts/history.");
            doc.Undo();Expect(before.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Actual region window Undo lost complete gate/action buffers.");
        }
        finally{window.Close();}
    }
}
