using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static ScriptBinding MemberBinding(NativeMapDocument d)=>d.ScriptBindings().Single(binding=>binding.Root.Scope==16 && d.HasStoredScriptScope(binding.Root));
    private static string ProjectBandOperand(NativeMapDocument d)
    {
        var project=d.IsLegacyProject?d:d.PrepareProjectConversion().Project;var binding=MemberBinding(project);
        return binding.Root.Owner.Read(binding.Root.Owner.Fields.Single(field=>field.Name=="band"));
    }
    private static void MemberBandOperands()
    {
        foreach(var empty in new[]{false,true})
        {
            var project=Fixture(false);var owner=Owner(project,16,true);Set(project,owner.Record,"id","42");_=OwnerPoint(project,owner,empty);
            Check(ProjectBandOperand(project)=="1","Source point Save must use physical band ordinal");
            var basis=NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");var native=project.CompileLegacyProject("DK2Preview_6a88240eaf814f78",basis);
            Check(ProjectBandOperand(native)=="1","Compiler/native projection replaced literal member band operand with visible ID");
            var live=MemberBinding(native).Owner!;var parent=live.BandRecord!;Set(native,parent,"bBandID","77");Check(ProjectBandOperand(native)=="1","Native synchronization rewrote source-only operand after parent ID change");
            var path=Path.Combine(directory,$"member-band-{empty}.dkm");using(var save=new EditorSaveSession(native))Check(save.Save(save.PrepareSaveAs(path),acceptLosses:true),"Member band DKM save refused");
            var reopened=NativeMapDocument.Load(path);Check(ProjectBandOperand(reopened)=="1","DKM/archive reopen changed literal member band operand");
            var compiled=reopened.CompileLegacyProject("DK2Preview_6a88240eaf814f78",basis);Check(ProjectBandOperand(compiled)=="1","Second compiler changed literal member band operand");
            if(!empty)
            {
                var plain=NativeMapDocument.Load(compiled.SaveCopy(Path.Combine(directory,"member-band-plain")));
                Check(ProjectBandOperand(plain)=="77","Plain game import must derive the actual native parent ID");
            }
            var first=compiled.Records().First(NativeMapDocument.IsBand);compiled.DeleteEditorHeroBand(first);Check(ProjectBandOperand(compiled)=="1","Deleting a different band rewrote stored source operand");
            live=MemberBinding(compiled).Owner!;var before=OwnerState(compiled);var gameBytes=Snapshot(compiled);var history=compiled.GetHistory().Undo.Count;
            using(var session=compiled.CreateEditorScenarioPointSession(MemberBinding(compiled).Root)){session.SelectOwner(live);compiled.ApplyEditorScenarioPoint(compiled.PrepareEditorScenarioPoint(session));}
            Check(ProjectBandOperand(compiled)=="0" && compiled.GetHistory().Undo.Count==history+1,"Explicit source point Modify failed to refresh current physical ordinal");Exact(gameBytes);
            compiled.Undo();ExactOwnerState(compiled,before);compiled.Redo();Check(ProjectBandOperand(compiled)=="0","Band operand Redo lost working metadata");
            var member=MemberBinding(compiled).Owner!;compiled.MoveBandMember(member.BandRecord!,member.MemberIndex,0);Check(ProjectBandOperand(compiled)=="0","Member permutation changed stored band operand");
        }
        Console.WriteLine("MEMBER BAND OPERAND DETAIL empty/nonempty source ordinal versus native ID, parent renumber/removal, metadata-only Modify, full UndoRedo, DKM/archive/reopen/two compilers and original plain seven-file import policy");
    }
}
