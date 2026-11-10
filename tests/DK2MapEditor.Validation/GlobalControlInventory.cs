using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class GlobalControlInventory
{
    // Read-only metadata export for an independent original-source comparison.
    // No UI types, map files, game invocation or model PASS counter are involved.
    public static void Write(string path)
    {
        var pages=new List<object>();
        IEnumerable<string> JobFields()=>CreatureJobs.Groups.SelectMany(group=>Enumerable.Range(0,group.Capacity).SelectMany(index=>
            group.HasParameters?new[]{$"{group.Prefix}Jobs[{index}]",$"{group.Prefix}JobsChance[{index}]",$"{group.Prefix}JobsMood[{index}]",$"{group.Prefix}JobsMana[{index}]"}:new[]{$"{group.Prefix}Jobs[{index}]"}));
        foreach(var section in GlobalDefinitionDefaults.Sections)
        {
            var fields=section.Name switch
            {
                "Terrains"=>GlobalTerrainControls.Fields,"Rooms"=>GlobalRoomControls.Fields,"Doors"=>GlobalDoorControls.Fields,
                "Traps"=>GlobalTrapControls.StoredFields,"Spells"=>GlobalSpellControls.StoredFields,
                "Attributes" or "Parameters" or "Flags" or "Light"=>CreaturePageControls.Fields(section.Name),
                "Attacks"=>CreatureAttacks.Slots.SelectMany(slot=>new[]{CreatureAttacks.TypeField(slot)}.Concat(CreatureAttacks.Parameters(slot))).ToArray(),
                "Resistances" or "Possession"=>CreatureOptions.Fields(section.Name),"Jobs"=>JobFields().ToArray(),
                "CrSpells"=>CreatureSpellControls.StoredFields,_=>throw new InvalidDataException("Uninventoried source page.")
            };
            var family=LegacyGlobalSchemas.All.Single(item=>item.Group==section.Family);
            IReadOnlyList<ValueChoice> Choices(string field,string attractionType="0")=>section.Name switch
            {
                "Terrains" when field is "MaxHealthType" or "DestroyedType" or "MaxHealthEffect" or "DestroyedEffect"=>GlobalTerrainControls.Choices(0,field),
                "Rooms" when GlobalRoomControls.Named(field)=>GlobalRoomControls.Choices(field),
                "Doors" when GlobalDoorControls.Named(field)=>GlobalDoorControls.Choices(field),
                "Traps" when GlobalTrapControls.Named(field)=>GlobalTrapControls.Choices(field),
                "Spells" when GlobalSpellControls.Named(field)=>GlobalSpellControls.Choices(field),
                "Attributes" or "Parameters" or "Flags" or "Light" when CreaturePageControls.Named(field)=>CreaturePageControls.Choices(field,attractionType),
                "Attacks" when CreatureAttacks.Slots.Any(slot=>CreatureAttacks.TypeField(slot)==field)=>CreatureAttacks.Options(CreatureAttacks.Slots.Single(slot=>CreatureAttacks.TypeField(slot)==field)).Select(option=>option.Name).ToArray(),
                "Resistances" or "Possession"=>CreatureOptions.Choices(field),
                "CrSpells" when CreatureSpellControls.Named(field)=>CreatureSpellControls.Choices(field),_=>Array.Empty<ValueChoice>()
            };
            GlobalDefinitionNumberFormat Format(string field,bool native)=>section.Name switch
            {
                "Attributes" or "Parameters" or "Flags" or "Light"=>CreaturePageControls.Format(field,native),
                "Attacks"=>CreatureAttacks.Format(field,native),"Resistances" or "Possession"=>CreatureOptions.Format(field,native),
                _=>GlobalDefinitionNumbers.Format(section.Family,field,native)
            };
            var described=fields.Select(field=>
            {
                var stored=family.Fields.Single(item=>item.Name==field);
                var catalogues=(field is "Attract1ID" or "Attract2ID"?new[]{"0","1","2","3","4"}:new[]{"0"})
                    .Select(type=>new{Context=type,Choices=Choices(field,type)}).Where(item=>item.Choices.Count>0).ToArray();
                return new{Field=field,Storage=new{stored.Offset,stored.Size,Kind=stored.Kind.ToString(),stored.BitMask},
                    Catalogues=catalogues,ProjectFormat=Format(field,false),NativeFormat=Format(field,true)};
            }).ToArray();
            pages.Add(new{Page=section.Name,Family=section.Family,FieldCount=fields.Count,Fields=described,
                CatalogueRows=GlobalDefinitionDefaults.Catalogue(section.Family).Count});
        }
        var jobs=CreatureJobs.Groups.Select(group=>new{group.ID,group.Capacity,group.HasParameters,
            Choices=CreatureJobs.Allowed(group.ID).Select(option=>option.Name).ToArray()}).ToArray();
        var scenarios=LegacyEventSchemas.All.Select(type=>new
        {
            type.Name, Signature=type.RecordSignature, ID=type.Type,
            PageID=LegacyEventSchemas.ParameterPageID(type.Name),
            Supported=ScenarioControlPages.Supports(type.Name),
            AliasPage=NativeMapDocument.HasScenarioAliasPage(type.Name),
            Fields=type.Arguments.Select(field=>new{field.Name,field.Offset,field.Size,Kind=field.Kind.ToString()}),
            Controls=ScenarioControlPages.Supports(type.Name)?ScenarioControlPages.Controls(type.Name):Array.Empty<ScenarioControl>()
        }).ToArray();
        using var output=new FileStream(Path.GetFullPath(path),FileMode.CreateNew,FileAccess.Write,FileShare.None);
        JsonSerializer.Serialize(output,new{Pages=pages,JobGroups=jobs,Scenarios=scenarios,ReadOnlyMetadataOnly=true,UIExecuted=false,WholeGoalComplete=false},new JsonSerializerOptions{WriteIndented=true});
        Console.WriteLine($"CONTROL METADATA EXPORTED: {pages.Count} pages; independent source/range/UI comparison still required.");
    }
}
