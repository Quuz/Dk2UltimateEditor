namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private void EditEditorPatrol(string description,Action operation)
    {
        if(transactionOpen||activePenStroke is not null)throw new InvalidOperationException("Finish the current edit before a patrol command.");
        // PathsAddArea, PathsKill and accepted ModifyPause each increment once,
        // independently of changed bytes. Preserve the modern manager's Undo
        // improvement, including source pause/one-point deletion with no Undo.
        EditCore(description,operation,()=>new(true,1));
    }
    public void StartEditorPatrol(NativeRecord first,int wait)=>EditEditorPatrol("Start patrol with first area",()=>
    {
        var point=CurrentPatrolPoint(first);
        if(point.NextID!=0)throw new InvalidOperationException("Start a new patrol from an unlinked area.");
        SetPatrolWait(point,wait);
    });
    public void SetEditorPatrolPause(NativeRecord record,int wait)=>EditEditorPatrol("Change patrol pause",()=>SetPatrolWait(CurrentPatrolPoint(record),wait));
    public void LinkEditorPatrolAreas(NativeRecord from,NativeRecord to)=>EditEditorPatrol("Draw patrol link",()=>LinkPatrolAreas(from,to));
    public void AppendEditorPatrolPoint(PatrolRoute route,NativeRecord record,int wait)=>EditEditorPatrol("Append patrol point",()=>AppendPatrolPoint(route,record,wait));
    public void InsertEditorPatrolPoint(PatrolRoute route,int occurrence,NativeRecord record,int wait)=>EditEditorPatrol("Insert patrol point",()=>InsertPatrolPoint(route,occurrence,record,wait));
    public void RemoveEditorPatrolOccurrence(PatrolRoute route,int occurrence)=>EditEditorPatrol("Remove patrol point link",()=>RemovePatrolOccurrence(route,occurrence));
    public void ClearEditorPatrolRoute(PatrolRoute route)=>EditEditorPatrol("Remove patrol route links",()=>ClearPatrolRoute(route));
}
