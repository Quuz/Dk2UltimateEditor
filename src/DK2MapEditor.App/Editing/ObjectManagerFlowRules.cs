using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Source Things_Manager Add pass1/5/6 and Modify pass2/7/8.
/// Does not grant permission to apply a plan or bypass its layout/current guards.</summary>
public static class ObjectManagerFlowRules
{
    public static bool AskOptions(bool modifying,int originalSelectionCount,ObjectBatchFlags flags)
        =>flags.HasFlag(ObjectBatchFlags.AskOptions)&&(!modifying||originalSelectionCount>1);
    public static bool RequiresPreview(ObjectBatchFlags flags,ObjectReplacementPolicy replacement,IEnumerable<ObjectBatchWarning> warnings)
    {
        using var entries=warnings.GetEnumerator();
        if(!entries.MoveNext())return false; // Source returns before preview on an empty work list.
        if(flags.HasFlag(ObjectBatchFlags.Preview))return true;
        do
        {
            var warning=entries.Current;
            var problems=warning&(ObjectBatchWarning.BadTerrain|ObjectBatchWarning.Fatal|ObjectBatchWarning.Enemy|ObjectBatchWarning.Incompatible);
            if(flags.HasFlag(ObjectBatchFlags.IgnoreTerrain))problems&=~ObjectBatchWarning.BadTerrain;
            if(flags.HasFlag(ObjectBatchFlags.IgnoreFatals))problems&=~ObjectBatchWarning.Fatal;
            if(flags.HasFlag(ObjectBatchFlags.IgnoreEnemies))problems&=~ObjectBatchWarning.Enemy;
            if(flags.HasFlag(ObjectBatchFlags.IgnoreIncompatible))problems&=~ObjectBatchWarning.Incompatible;
            if(problems!=0||warning.HasFlag(ObjectBatchWarning.Replacement)&&replacement==ObjectReplacementPolicy.Ask
                ||(warning&(ObjectBatchWarning.OutOfMap|ObjectBatchWarning.Boundary))!=0&&flags.HasFlag(ObjectBatchFlags.IgnoreAsk))return true;
        }while(entries.MoveNext());
        return false;
    }
}
