using System.Reflection;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only: the private-document event injects the exact boundary
    // between a successful row Save and the parent navigation/Apply continuation.
    private static void CheckGlobalCallbackTransitions(NativeMapDocument document,string language)
    {
        foreach(var action in new[]{"select","apply"})
        foreach(var guard in new[]{"owner","source","closed"})
        {
            var original=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=document.EditorChangeCount;var history=document.GetHistory().Undo.Count;
            var owner=true;var commits=0;var window=new GlobalVariablesWindow(document,()=>owner,()=>{commits++;return true;});ShowManagerCallerWindow(window);
            try
            {
                Expect(window.Select("Doors",0),"Guard setup row unavailable.");
                var row=window.ActiveRowEditor;var input=(TextBox)row!.FindName("DoorPropertyHealth");input.Text=input.Text=="111"?"112":"111";
                var session=(GlobalVariablesSession)typeof(GlobalVariablesWindow).GetField("session",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(window)!;
                var triggered=false;
                session.WorkingDocument.Changed+=(_,_)=>
                {
                    if(triggered)return;triggered=true;
                    Expect(!window.Select("Spells",0)&&!window.Reset(true)&&!window.ApplyVariables(),"Private Save allowed parent reentry.");
                    if(guard=="owner")owner=false;
                    else if(guard=="closed")window.Close();
                    else
                    {
                        var room=document.GlobalDefinitionRows().Single(r=>r.Family=="Rooms"&&r.Index==0);
                        var cost=document.GlobalDefinitionValues(room).Single(v=>v.Name=="Cost").Value;
                        document.SetGlobalDefinitionValues([new("Rooms",0,"Cost",cost=="777"?"778":"777")]);
                    }
                };
                var accepted=action=="select"?window.Select("Spells",0):window.ApplyVariables();
                Expect(triggered&&!accepted&&commits==0&&window.SelectedFamily=="Doors","Parent continued after private Save invalidated its owner.");
                if(guard=="closed")Expect(window.ActiveRowEditor is null,"Closed parent recreated a row after Save.");
                else Expect(ReferenceEquals(row,window.ActiveRowEditor),"Invalidated parent replaced active editor.");
                if(guard=="source")document.Undo();
                Expect(original.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&document.EditorChangeCount==count&&document.GetHistory().Undo.Count==history,"Row continuation wrote live data or lost source Undo.");
            }
            finally{window.Close();}
        }
    }
}
