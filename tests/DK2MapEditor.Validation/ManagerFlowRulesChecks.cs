using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class ManagerFlowRulesChecks
{
    public static void Run()
    {
        void Check(bool value,string name){if(!value)throw new InvalidOperationException(name);}
        // Declarative source outcomes, not a second implementation of the rule.
        // Things_Manager.c pass1/2 uses original count before Used filtering.
        Check(ObjectManagerFlowRules.AskOptions(false,0,ObjectBatchFlags.AskOptions),"Add must ask before building its work list.");
        Check(!ObjectManagerFlowRules.AskOptions(true,1,ObjectBatchFlags.AskOptions),"Single opens Thing_Select, not ModifyOptions.");
        Check(ObjectManagerFlowRules.AskOptions(true,2,ObjectBatchFlags.AskOptions),"Multi remains multi after filtering to one or zero.");
        Check(!ObjectManagerFlowRules.AskOptions(true,2,ObjectBatchFlags.Preview),"Preview must not imply options.");
        var cases=new (string Name,ObjectBatchFlags Flags,ObjectReplacementPolicy Policy,ObjectBatchWarning[] Warnings,bool Expected)[]
        {
            ("empty exits before preview",ObjectBatchFlags.Preview,ObjectReplacementPolicy.Ask,[],false),
            ("normal direct",0,ObjectReplacementPolicy.Ask,[0],false),
            ("explicit preview",ObjectBatchFlags.Preview,ObjectReplacementPolicy.None,[0],true),
            ("terrain unresolved",0,ObjectReplacementPolicy.None,[ObjectBatchWarning.BadTerrain],true),
            ("terrain ignored",ObjectBatchFlags.IgnoreTerrain,ObjectReplacementPolicy.None,[ObjectBatchWarning.BadTerrain],false),
            ("fatal unresolved",0,ObjectReplacementPolicy.All,[ObjectBatchWarning.Fatal],true),
            ("fatal ignored",ObjectBatchFlags.IgnoreFatals,ObjectReplacementPolicy.All,[ObjectBatchWarning.Fatal],false),
            ("enemy unresolved",0,ObjectReplacementPolicy.None,[ObjectBatchWarning.Enemy],true),
            ("enemy ignored",ObjectBatchFlags.IgnoreEnemies,ObjectReplacementPolicy.None,[ObjectBatchWarning.Enemy],false),
            ("incompatible unresolved",0,ObjectReplacementPolicy.None,[ObjectBatchWarning.Incompatible],true),
            ("incompatible ignored",ObjectBatchFlags.IgnoreIncompatible,ObjectReplacementPolicy.None,[ObjectBatchWarning.Incompatible],false),
            ("one ignored problem is insufficient",ObjectBatchFlags.IgnoreTerrain,ObjectReplacementPolicy.None,[ObjectBatchWarning.BadTerrain|ObjectBatchWarning.Enemy],true),
            ("replacement asks",0,ObjectReplacementPolicy.Ask,[ObjectBatchWarning.Replacement|ObjectBatchWarning.SameType],true),
            ("replacement all uses default",0,ObjectReplacementPolicy.All,[ObjectBatchWarning.Replacement],false),
            ("replacement none uses default",0,ObjectReplacementPolicy.None,[ObjectBatchWarning.Replacement],false),
            ("replacement does not erase warning",0,ObjectReplacementPolicy.All,[ObjectBatchWarning.Replacement|ObjectBatchWarning.BadTerrain],true),
            ("quiet locked entry",ObjectBatchFlags.IgnoreTerrain,ObjectReplacementPolicy.All,[ObjectBatchWarning.Boundary|ObjectBatchWarning.BadTerrain],false),
            ("ask about locked entry",ObjectBatchFlags.IgnoreTerrain|ObjectBatchFlags.IgnoreAsk,ObjectReplacementPolicy.All,[ObjectBatchWarning.Boundary|ObjectBatchWarning.BadTerrain],true),
            ("quiet outside entry",ObjectBatchFlags.IgnoreTerrain,ObjectReplacementPolicy.All,[ObjectBatchWarning.OutOfMap|ObjectBatchWarning.BadTerrain],false),
            ("later entry still forces preview",0,ObjectReplacementPolicy.None,[0,ObjectBatchWarning.Enemy],true)
        };
        foreach(var c in cases)Check(ObjectManagerFlowRules.RequiresPreview(c.Flags,c.Policy,c.Warnings)==c.Expected,c.Name);
        Console.WriteLine($"MANAGER FLOW SOURCE DETAIL {cases.Length+4} declarative outcomes: original count/options, empty list, unresolved/ignored warnings, replacement and hard-limit preview; no UI or map edits");
    }
}
