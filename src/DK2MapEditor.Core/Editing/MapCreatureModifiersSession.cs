using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

public sealed record MapCreatureModifierRow(uint ID, ValueChoice Name, bool Elite, bool Editable, IReadOnlyList<uint> Values);

/// <summary>Source Properties_MapCreatures scratch array. Selection and input
/// change this private context only; OK publishes it to the owning document.</summary>
public sealed class MapCreatureModifiersSession : IDisposable
{
    internal NativeMapDocument Owner { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    internal bool Closed { get; private set; }
    internal IReadOnlyDictionary<MapRuleKey, long?> Baseline { get; }
    private readonly Dictionary<MapRuleKey, long?> values;
    private readonly MapRuleElement[] elements;
    private readonly HashSet<uint> editable;
    private uint[] selection = [];
    internal MapCreatureModifiersSession(NativeMapDocument owner, IReadOnlyList<MapRuleCell> cells, long revision, byte[] fingerprint)
    {
        Owner=owner; Revision=revision; Fingerprint=fingerprint;
        Baseline=cells.ToDictionary(cell=>cell.Key,cell=>cell.Value); values=new(Baseline);
        elements=MapRules.Elements(MapRuleKind.CreatureModifier).ToArray();
        editable=elements.Where(element=>cells.Where(cell=>cell.Key.Element==element.ID).All(cell=>cell.Editable)).Select(element=>element.ID).ToHashSet();
    }
    private static MapRuleKey Key(uint id,uint parameter)=>new(MapRuleKind.CreatureModifier,0,id,parameter);
    public bool Changed=>values.Any(item=>item.Value!=Baseline[item.Key]);
    public IReadOnlyList<uint> Selected=>Array.AsReadOnly(selection);
    public bool CanEditSelection=>selection.Any(editable.Contains);
    public IReadOnlyList<MapCreatureModifierRow> Rows=>elements.Select(element=>new MapCreatureModifierRow(element.ID,element.Name,
        MapRules.IsElite(Key(element.ID,1)),editable.Contains(element.ID),Array.AsReadOnly(Enumerable.Range(1,4).Select(parameter=>unchecked((uint)(values[Key(element.ID,(uint)parameter)]??0))).ToArray()))).ToArray();
    public void Select(IEnumerable<uint> ids)
    {
        Owner.RequireMapCreatureModifiersCurrent(this);
        var selected=ids.Distinct().ToArray();
        if(selected.Any(id=>!elements.Any(element=>element.ID==id)))throw new ArgumentException("Select original creature catalogue IDs.");
        selection=selected;
    }
    public IReadOnlyList<uint> Averages()
    {
        Owner.RequireMapCreatureModifiersCurrent(this);
        var result=new uint[4];
        for(uint parameter=1;parameter<=4;parameter++)
        {
            // Original LONG sums wrap at 32 bits; only nonzero values count,
            // and elite rows17..29 are skipped before every parameter.
            int sum=0,count=0;
            foreach(var id in selection.Where(editable.Contains))
            {
                var value=unchecked((uint)(values[Key(id,parameter)]??0));
                if(value==0)continue;
                sum=unchecked(sum+(int)value);count++;
            }
            result[parameter-1]=count==0?0:unchecked((uint)(sum/count));
        }
        return Array.AsReadOnly(result);
    }
    public static uint ParseInput(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var match=Regex.Match(text,@"^\s*([+-]?\d+)");
        if(!match.Success)return 0;
        // GetDlgItemInt returns zero for failed DWORD conversion. Negative
        // and >65535 valid inputs are clamped by the original EN_CHANGE path.
        if(!long.TryParse(match.Groups[1].Value,NumberStyles.Integer,CultureInfo.InvariantCulture,out var number)||number>uint.MaxValue||number<int.MinValue)return 0;
        return (uint)Math.Clamp(unchecked((int)(uint)number),0,65535);
    }
    public int SetParameter(uint parameter,string text)
    {
        Owner.RequireMapCreatureModifiersCurrent(this);
        if(parameter is <1 or >4)throw new ArgumentOutOfRangeException(nameof(parameter));
        var value=ParseInput(text);var written=0;
        foreach(var id in selection.Where(editable.Contains)){values[Key(id,parameter)]=value==0?null:value;written++;}
        return written;
    }
    internal IEnumerable<MapRuleChange> Changes()=>values.Where(item=>item.Value!=Baseline[item.Key]).Select(item=>new MapRuleChange(item.Key,item.Value));
    public void Dispose()=>Closed=true;
}

public sealed partial class NativeMapDocument
{
    public MapCreatureModifiersSession CreateMapCreatureModifiersSession()
        =>new(this,MapRuleCells().Where(cell=>cell.Key.Kind==MapRuleKind.CreatureModifier).ToArray(),EditRevision,GateGenerationFingerprint());
    internal void RequireMapCreatureModifiersCurrent(MapCreatureModifiersSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if(!ReferenceEquals(session.Owner,this)||session.Closed||session.Revision!=EditRevision||!session.Fingerprint.AsSpan().SequenceEqual(GateGenerationFingerprint()))
            throw new InvalidOperationException("Reopen creature modifiers after document changes.");
    }
    public bool IsMapCreatureModifiersCurrent(MapCreatureModifiersSession session)
    {
        try{RequireMapCreatureModifiersCurrent(session);return true;}catch(InvalidOperationException){return false;}
    }
    public void ApplyMapCreatureModifiers(MapCreatureModifiersSession session)
    {
        RequireMapCreatureModifiersCurrent(session);
        if(transactionOpen||activePenStroke is not null)throw new InvalidOperationException("Finish the active edit before applying creature modifiers.");
        SetMapRules(session.Changes());
        session.Dispose();
    }
}
