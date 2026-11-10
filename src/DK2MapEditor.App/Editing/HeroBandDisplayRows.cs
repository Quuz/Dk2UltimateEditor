using System.Globalization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

internal sealed record HeroBandDisplayRow(int Index,string Label,int ImageResource);

internal static class HeroBandDisplayRows
{
    private static readonly IReadOnlyList<ValueChoice> creatures=ValueCatalogues.ForTemplate("Creature");
    internal static ValueChoice? Creature(uint id)=>creatures.FirstOrDefault(c=>c.Value==id.ToString(CultureInfo.InvariantCulture));
    internal static int Image(uint id)=>new[]{"BadCreatures","BadElites","Heros"}.SelectMany(LegacyCompilerRules.Elements).FirstOrDefault(c=>c.ID==id)?.Icon??1115;
    internal static string Flags(uint options,uint behavior)
    {
        var chars="-----/----".ToCharArray();
        if((behavior&2)!=0)chars[0]='*';if((behavior&4)!=0)chars[0]='+';
        foreach(var (bit,index,symbol) in new (uint,int,char)[]{(1,1,'T'),(8,2,'A'),(16,3,'H'),(32,4,'L')})if((behavior&bit)!=0)chars[index]=symbol;
        foreach(var (bit,index,symbol) in new (uint,int,char)[]{(1,6,'S'),(2,7,'J'),(4,8,'M'),(8,9,'C')})if((options&bit)!=0)chars[index]=symbol;
        return new(chars);
    }
    internal static HeroBandDisplayRow Create(int index,EditorHeroMemberValues values,string language,Func<string,string> text)
    {
        var name=Creature(values.CreatureID)?.Name(language)??string.Format(CultureInfo.CurrentCulture,text("HeroBand.Imported"),values.CreatureID);
        var enemy=values.Enemy==0?text("Batch.Unspecified"):ValueCatalogues.Owners.FirstOrDefault(c=>c.Value==values.Enemy.ToString(CultureInfo.InvariantCulture))?.Name(language)
            ??string.Format(CultureInfo.CurrentCulture,text("HeroBand.Imported"),values.Enemy);
        var objective=values.Objective is 11 or 12 or 13 or 14 or 17 or 18 or 19 or 22 or 23 or 27?text("Batch.Objective."+values.Objective):text("AliasEvent.Unknown")+" "+values.Objective;
        if(values.Objective==23)objective+=" · "+text("Field.Area")+" "+values.Area;
        return new(index,((values.Options2&2)!=0?"★ ":"")+(index+1)+" · "+name+" · "+text("Field.Level")+" "+values.Level+" · "+enemy
            +"\n"+Flags(values.Options,values.Options2)+" · "+objective,Image(values.CreatureID));
    }
}
