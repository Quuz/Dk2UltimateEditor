using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only: these source-callback cases open real windows if executed.
    private static void CheckCreatureJobsCallbacks(NativeMapDocument document,string language)
    {
        var workspace=new EditorWorkspace{Document=document};CreatureGlobalsWindow? parent=null;
        workspace.CreatureGlobalsPresenter=window=>{parent=window;window.Show();};
        void Open()
        {
            parent?.Close();parent=null;workspace.Measure(new Size(1200,850));workspace.Arrange(new Rect(0,0,1200,850));workspace.UpdateLayout();
            Control<Button>(workspace,"DefinitionCreatureGlobals").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(parent is not null,"Creature shared parent unavailable.");
        }
        T Job<T>(string field)where T:Control=>(T)((CreatureWorkspacePageWindow)parent!.ActiveRowEditor!).FindControl("DefinitionJobs"+field)!;
        void Choose(string field,string id){var combo=Job<ComboBox>(field);combo.SelectedItem=combo.Items.Cast<object>().Single(item=>Get<string>(item,"ID")==id);}
        void MakeBad(int index,int chance)
        {
            Expect(parent!.Select("Jobs",index),"Jobs row unavailable.");
            foreach(var group in new[]{"Happy","Unhappy","Angry"})
            {Choose("Group",group);Job<DataGrid>("Table").SelectAll();Job<Button>("Remove").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
            Choose("Group","Happy");Choose("Type",CreatureJobs.Allowed("Happy")[0].Name.Value);
            Job<TextBox>("Chance").Text=chance.ToString(CultureInfo.InvariantCulture);Job<TextBox>("Mood").Text=Job<TextBox>("Mana").Text="0";
            Job<Button>("Add").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        Dictionary<NativeFile,byte[]> Bytes()=>document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        void Exact(Dictionary<NativeFile,byte[]> expected)=>Expect(document.Files.Count==expected.Count&&expected.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Job callback wrote live map before common acceptance or lost Undo.");
        PatrolModalStep Reply(int total,bool proceed,bool remember=false)=>PatrolStep<CreatureJobsWarningWindow>(window=>
        {
            Expect(PatrolControl<TextBlock>(window,"CreatureJobsWarningMessage").Text.Contains(total.ToString(CultureInfo.InvariantCulture)),"Apply scanned changed rows instead of retained Jobs page buffer.");
            PatrolControl<CheckBox>(window,"CreatureJobsWarningRemember").IsChecked=remember;
            PatrolControl<Button>(window,proceed?"CreatureJobsWarningContinue":"CreatureJobsWarningCancel").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        });
        try
        {
            var initial=Bytes();var history=document.GetHistory().Undo.Count;var count=document.EditorChangeCount;
            Open();MakeBad(0,37);
            PatrolModal(()=>Expect(!parent!.ApplyCreatures(),"Second source request cancellation accepted common Apply."),Reply(37,true),Reply(37,false));
            Exact(initial);Expect(document.GetHistory().Undo.Count==history&&document.EditorChangeCount==count&&parent!.SelectedSection=="Jobs","Second cancellation wrote live state or changed active page.");
            PatrolModal(()=>Expect(parent!.Select("Jobs",1),"Accepted old-row navigation failed."),Reply(37,true));MakeBad(1,73);
            PatrolModal(()=>Expect(parent!.Select("Attributes",2),"Accepted Jobs deactivation failed."),Reply(73,true));
            ((TextBox)parent!.ActiveRowEditor!.FindName("CreaturePropertyHealth")).Text="222";
            PatrolModal(()=>Expect(!parent.ApplyCreatures(),"Inactive retained Jobs Cancel accepted common Apply."),Reply(73,false));
            Exact(initial);Expect(parent.SelectedSection=="Attributes"&&parent.SelectedIndex==2,"Jobs Apply rejection switched to another page.");
            PatrolModal(()=>Expect(parent.ApplyCreatures(),"Inactive retained Jobs Continue failed common Apply."),Reply(73,true));
            Expect(document.GetHistory().Undo.Count==history+1&&document.EditorChangeCount==unchecked(count+1),"Two-row Jobs and Attributes common Apply split counters/history.");
            var after=Bytes();document.Undo();Exact(initial);document.Redo();Exact(after);parent.Close();

            // A fresh parent hasn't created Jobs. An imported nonstandard sum
            // in row0 must not invent a Jobs PSN_APPLY question before visiting it.
            Open();PatrolModal(()=>Expect(parent!.ApplyCreatures(),"Unvisited Jobs blocked unchanged parent Apply."));Exact(after);
            MakeBad(0,37);
            PatrolModal(()=>Expect(parent!.ApplyCreatures(),"Remembered first request did not auto-reply to source Apply."),Reply(37,true,true));
            Expect(parent!.JobWarningsSuppressed,"First source callback did not remember Continue.");parent.ResetJobWarnings();
            var applied=Bytes();var appliedHistory=document.GetHistory().Undo.Count;var appliedCount=document.EditorChangeCount;
            // Leave Jobs once, then an equal Apply from another page still checks
            // the retained buffer. Suppression is not inferred from byte no-op.
            PatrolModal(()=>Expect(parent.Select("Light",0),"Job deactivation failed after Apply."),Reply(37,true));
            PatrolModal(()=>Expect(parent.ApplyCreatures(),"Equal Apply failed source retained-buffer check."),Reply(37,true));
            Exact(applied);Expect(document.GetHistory().Undo.Count==appliedHistory&&document.EditorChangeCount==appliedCount,"Equal common Apply changed map/history/counter.");
            Console.WriteLine($"PASS {language} {(document.IsLegacyProject?"project":"native")} source Jobs KillActive/Apply, two requests, last-row cache, inactive/unvisited/no-op, cancellation and one atomic common Apply");
        }
        finally{parent?.Close();workspace.Document=null;}
    }
}
