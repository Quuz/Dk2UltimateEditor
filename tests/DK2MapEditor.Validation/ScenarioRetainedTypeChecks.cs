using DK2MapEditor.Core.Editing;
internal static class ScenarioRetainedTypeChecks
{
    public static void Run()
    {
        foreach(var native in new[]{false,true})
        {
            var doc=NativeMapDocument.CreateNewMap(new("RetainedType","Retained type",18,16,NativeFormat:native));
            var context=doc.CreateScenarioCreationContext();var bytes=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var revision=doc.EditRevision;var count=doc.EditorChangeCount;var history=doc.GetHistory();
            foreach(var signature in new uint[]{0xD5,0xD6})
            {
                var initial=signature==0xD5?"TRIGGER_VARIABLE":"ACTION_SETVARIABLE";
                if(context.InitialType(signature).Name!=initial)throw new Exception("Source initial category type was not preserved.");
                var selected=LegacyEventSchemas.All.Single(t=>t.Name==(signature==0xD5?"TRIGGER_TIMER":"ACTION_MOVECAMERA"));
                using(var session=doc.CreateScenarioCreationSession(context,signature,selected))
                    if(context.InitialType(signature).Name!=initial)throw new Exception("Unclosed draft changed the retained source category.");
                if(context.InitialType(signature)!=selected)throw new Exception("Closed source wizard did not expose its retained category type.");
            }
            foreach(var signature in new uint[]{0,0xD4,0xD7,uint.MaxValue})
            {try{context.InitialType(signature);throw new Exception("Unknown scenario category accepted.");}catch(ArgumentOutOfRangeException){}}
            if(!bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))||doc.EditRevision!=revision||doc.EditorChangeCount!=count
                ||doc.GetHistory().Undo.Count!=history.Undo.Count||doc.GetHistory().ChangeCount!=history.ChangeCount)throw new Exception("Retained-type read/draft closure modified the live owner.");
        }
    }
}
