using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private static bool EditorResizeThing(NativeRecord record)
        => record.File.Signature==LegacyThings&&record.Category!="Project.Opaque"
            || record.Category is "GAMEOBJECT" or "GAMETRAP" or "GAMEDOOR" or "GAMENEUTRAL" or "GAMEHERO" or "GAMEKEEPER";
    private static bool EditorResizeRect(NativeRecord record)
        => record.File.Signature==LegacyAreas&&record.Category!="Project.Opaque"
            || record.Category is "GAMEAREA" or "GAMEEFFECT"
            || record.Category=="GAMEGATE"&&record.Read(record.Fields.Single(field=>field.Name=="bGateType"))!="14";
    private void DeleteEditorResizeRegion(NativeRecord record)
    {
        if (!IsVerifiedRegionRemovalShape(record)) throw new InvalidOperationException("An unverified region layout cannot safely be cropped.");
        RequireKnownEntityReferences(Records());
        // Properties_RemoveRectEx unlinks the region only. Unlike the direct
        // Delete command it neither asks Triggers_IsUsed for a reply nor
        // rewires a predecessor. Stored points and all operands survive.
        DeleteRecordCore(record, editorAreaRemoval: true);
    }
    private void DeleteEditorResizeThing(NativeRecord record)
    {
        if (!EditorResizeThing(record) || !ObjectManagerEntries().Any(entry => SameRecord(entry.Record, record) && entry.CanRemove))
            throw new InvalidOperationException("An unverified thing layout cannot safely be cropped.");
        RequireKnownEntityReferences(Records());
        // Things_RemoveAtMapPosEx unlinks the placed owner, retaining points
        // and operands for the scenario editor. It has no second prompt.
        DeleteRecordCore(record, editorAreaRemoval: true);
    }
    private void RemoveEditorResizeBoundaryRecords(bool removalAccepted)
    {
        var removed=Records().Where(record=>EditorResizeThing(record)||EditorResizeRect(record)).Where(record=>
        {
            var coordinates=record.Fields.Where(field=>field.Name is "x" or "y" or "rect.x1" or "rect.x2" or "rect.y1" or "rect.y2"
                || field.Name.EndsWith("LocationX",StringComparison.Ordinal)||field.Name.EndsWith("LocationY",StringComparison.Ordinal)
                ||field.Name.EndsWith("StartX",StringComparison.Ordinal)||field.Name.EndsWith("StartY",StringComparison.Ordinal)
                ||field.Name.EndsWith("EndX",StringComparison.Ordinal)||field.Name.EndsWith("EndY",StringComparison.Ordinal)).ToArray();
            bool Outside(long value,bool horizontal)=>value<=0||value>=(horizontal?Width:Height)-1;
            if(coordinates.Any(field=>Outside(long.Parse(record.Read(field),CultureInfo.InvariantCulture),field.Name.Contains('x')||field.Name.EndsWith('X'))))return true;
            if(record.Category=="GAMEEFFECT")
            {
                long Value(string name)=>long.Parse(record.Read(record.Fields.Single(field=>field.Name==name)),CultureInfo.InvariantCulture);
                return Outside(Value("dwEffectStartX")+Value("wEffectWidth"),true)||Outside(Value("dwEffectStartY")+Value("wEffectHeight"),false);
            }
            return false;
        }).ToArray();
        if(removed.Length==0)return;
        if(!removalAccepted)throw new InvalidOperationException("Resizing would crop placed records on the original fixed map boundary.");
        Edit("Resize map",()=>
        {
            foreach(var group in removed.GroupBy(record=>record.File))
                foreach(var record in group.OrderByDescending(record=>record.Offset))
                    if(EditorResizeRect(record))DeleteEditorResizeRegion(record);else DeleteEditorResizeThing(record);
        });
    }
    /// <summary>The actual original properties caller allocates Undo and one
    /// MapChanges increment even for equal accepted options. Resize adds one,
    /// plus each shifted thing/rectangle and each removed thing/rectangle.</summary>
    public uint EditorMapPropertiesChangeIncrements(MapPropertiesPlan plan)
    {
        if(!IsMapPropertiesPlanCurrent(plan))throw new InvalidOperationException("Refresh map properties after owner/draft changes.");
        if(!plan.EditorCaller)throw new InvalidOperationException("Prepare the original editor properties caller first.");
        return plan.EditorIncrements;
    }
    private static uint CalculateEditorPropertiesIncrements(bool resized,bool moved,IReadOnlyList<NativeRecord> before,IReadOnlyList<NativeRecord> after)
    {
        if(!resized)return 1;
        var placed=before.Count(record=>EditorResizeThing(record)||EditorResizeRect(record));
        var remaining=after.Count(record=>EditorResizeThing(record)||EditorResizeRect(record));
        if(remaining>placed)throw new InvalidOperationException("Properties resize cannot create placed records.");
        return checked((uint)(2+(moved?placed:0)+placed-remaining));
    }
    public void ApplyEditorMapProperties(MapPropertiesPlan plan,bool largeMapAccepted=false,bool croppingAccepted=false)
    {
        var increments=EditorMapPropertiesChangeIncrements(plan);
        if(plan.NeedsLargeMapConsent&&!largeMapAccepted||plan.NeedsCropConsent&&!croppingAccepted)
            throw new InvalidOperationException("Confirm the current large/cropped map-properties preview first.");
        EditCore("Change general map settings",()=>
        {
            foreach(var row in plan.Result)row.Key.Bytes=(byte[])row.Value.Clone();
            if(historyDepth!=0)operationChildren.Peek().AddRange(plan.Children);
        },()=>new(true,increments));
        plan.Session.Dispose();
    }
}
