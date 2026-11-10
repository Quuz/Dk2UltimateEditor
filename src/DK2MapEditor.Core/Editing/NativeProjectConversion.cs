using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using DK2MapEditor.Core.Formats;

namespace DK2MapEditor.Core.Editing;

public enum ProjectConversionSeverity { Information, Loss, Error }
public sealed record ProjectConversionIssue(string Code, ProjectConversionSeverity Severity, string Detail);
public sealed record ProjectConversionSourceFile(string SourcePath, string ExportPath, ImmutableArray<byte> Bytes);

/// <summary>Independent editable projection and immutable original bundle. Publishing both and adopting a Save target are separate operations.</summary>
public sealed class PreparedProjectConversion
{
    internal PreparedProjectConversion(NativeMapDocument source, byte[] fingerprint, byte[]? statistics, NativeMapDocument project,
        IReadOnlyList<ProjectConversionSourceFile> original, IReadOnlyList<ProjectConversionIssue> issues, IReadOnlyDictionary<ushort,ushort> eventIDs)
    { Source=source; Fingerprint=fingerprint; Statistics=statistics; Project=project; SourceFiles=original; Issues=issues; NativeEventIDs=eventIDs.ToImmutableDictionary(); }
    internal NativeMapDocument Source { get; }
    internal byte[] Fingerprint { get; }
    internal byte[]? Statistics { get; }
    public NativeMapDocument Project { get; }
    public IReadOnlyList<ProjectConversionSourceFile> SourceFiles { get; }
    public IReadOnlyList<ProjectConversionIssue> Issues { get; }
    public ImmutableDictionary<ushort,ushort> NativeEventIDs { get; }
    public bool RequiresOriginalBundleRetention => true;
    public bool HasBlockingIssues => Issues.Any(issue=>issue.Severity==ProjectConversionSeverity.Error);
    public bool HasReportedLosses => Issues.Any(issue=>issue.Severity==ProjectConversionSeverity.Loss);
    public bool IsCurrent => IsCurrentForSave(temporary: false);
    internal bool IsCurrentForSave(bool temporary) => !(temporary ? Source.IsTemporarySaveLocked : Source.IsSaveLocked) && Source.SaveWorkingFingerprint().AsSpan().SequenceEqual(Fingerprint)
        && ((Source.ReadActivityStatistics()?.Encode() is { } current && Statistics is not null && current.AsSpan().SequenceEqual(Statistics))
            || Source.ReadActivityStatistics() is null && Statistics is null);
}

public sealed partial class NativeMapDocument
{
    /// <summary>Prepare complete native or AREA editing data as a DKM, without changing or writing the input.</summary>
    public PreparedProjectConversion PrepareProjectConversion(string projectPath="ConvertedDungeon.dkm")
        => PrepareProjectConversionCore(projectPath, temporary: false);
    internal PreparedProjectConversion PrepareTemporaryProjectConversion(string projectPath)
        => PrepareProjectConversionCore(projectPath, temporary: true);
    private PreparedProjectConversion PrepareProjectConversionCore(string projectPath, bool temporary)
    {
        if(temporary ? IsTemporarySaveLocked : IsSaveLocked)throw new InvalidOperationException("Finish the current transaction before conversion.");
        if(IsLegacyProject && MainFile.Signature!=LegacySectionFile.AreaFileSignature)throw new InvalidOperationException("This document is already a DKM project.");
        if(!Path.GetExtension(projectPath).Equals(".dkm",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Select a DKM project path.",nameof(projectPath));
        var fingerprint=SaveWorkingFingerprint();var statistics=ReadActivityStatistics();
        var snapshots=Array.AsReadOnly(TransactionFiles().Select(file=>new ProjectConversionSourceFile(file.SourcePath,file.ExportPath,ImmutableArray.CreateRange(file.Data.ToArray()))).ToArray());
        var issues=new List<ProjectConversionIssue>();
        IReadOnlyDictionary<ushort,ushort> eventIDs=new Dictionary<ushort,ushort>();
        void Issue(string code,ProjectConversionSeverity severity,string detail)=>issues.Add(new(code,severity,detail));
        NativeMapDocument result;
        if(IsLegacyProject)
        {
            var original=LegacySectionFile.Parse(SerializeLegacyProject());
            var defaults=CreateNewProject(new(Width:Width,Height:Height,DisplayName:"Imported area",FileName:"ImportedArea"));
            var sections=original.Sections.ToList();
            foreach(var signature in new[]{LegacyInfo,LegacyPlayers})
                if(!sections.Any(section=>section.Signature==signature))
                {
                    var file=defaults.projectSections.Single(file=>file.Signature==signature);
                    sections.Add(new(signature,(int)file.UInt32(4),file.Data[12..].ToArray()));
                }
            result=FromLegacyProject(projectPath,LegacySectionFile.Create(sections));
            foreach(var opaque in result.Records().Where(record=>record.Category=="Project.Opaque" && record.File.Signature!=ActivityStatisticsPayload.Signature))Issue("AreaOpaqueSection",ProjectConversionSeverity.Error,$"Opaque AREA section 0x{opaque.File.Signature:X} retained; original editor compatibility is unproved.");
            defaults.activityStatistics?.Dispose();
            Issue("AreaToMap",ProjectConversionSeverity.Information,"AREA sections and opaque bytes retained; absent map/player metadata initialized for a full DKM.");
        }
        else
        {
            var sourceRecords=Records();
            foreach(var opaque in sourceRecords.Where(record=>record.Category==NativeOpaqueFile.Category))
                Issue("OpaqueNativeFile",ProjectConversionSeverity.Loss,$"The complete unknown native companion {opaque.File.ExportPath} remains in the source archive; DKM game recompilation may omit it.");
            foreach(var opaque in sourceRecords.Where(record=>record.Category==NativeCollectionLayout.OpaqueCategory))
                Issue("UnverifiedCollection",ProjectConversionSeverity.Error,$"Unverified native collection header/record at 0x{opaque.Offset:X}, {opaque.File.ExportPath}, remains in the complete original bundle; semantic projection is refused.");
            var records=sourceRecords.Where(record=>record.Category!=NativeCollectionLayout.OpaqueCategory).ToArray();
            foreach(var file in files.Where(file=>NativeCollectionLayout.IsCollection(file.Signature)
                && !sourceRecords.Any(record=>record.File==file && record.Category==NativeCollectionLayout.OpaqueCategory)))
            {
                var opaque=file.Length-NativeCollectionDataEnd(file);
                if(opaque!=0)Issue("OpaqueCollectionData",ProjectConversionSeverity.Loss,$"{opaque} trailing bytes in {file.ExportPath} remain in the complete source companion; game recompilation may omit them.");
            }
            foreach(var file in files.Where(file=>NativeDefinitionSignature(file.Signature)))
            {
                var opaque=OpaqueNativeDefinitionBytes(file,ScanNativeDefinitionSections(file));
                if(opaque!=0)Issue("OpaqueDefinitionData",ProjectConversionSeverity.Loss,$"{opaque} bytes in unknown native definition sections or trailing data remain in the source companion; game recompilation may omit them.");
            }
            var project=CreateNewProject(new(Width:Width,Height:Height,DisplayName:"Imported dungeon",FileName:"ImportedDungeon"));
            var info=project.Records().Single(record=>record.Category=="Project.MAPPROPERTIES");
            var players=project.Records().Where(record=>record.Category=="Project.MAPOPTIONS").ToArray();
            for(var index=0;index<7;index++)
                foreach(var (array,count) in new[]{("CreaturesCount",45),("Rooms",15),("Traps",14),("Doors",8),("Spells",12)})
                    for(var slot=0;slot<count;slot++)ConversionWrite(players[index],$"{array}[{slot}]",-1);
            var normalized=0;var boundary=0;
            for(var y=0;y<Height;y++)for(var x=0;x<Width;x++)
            {
                var raw=GetTile(x,y);var tile=ConversionTile(raw);
                if(tile!=raw)normalized++;
                if(x==0 || y==0 || x==Width-1 || y==Height-1) { if(tile!=new MapTile(30,2,1))boundary++;tile=new(30,2,1); }
                project.PutTile(x,y,tile);
            }
            if(normalized!=0)Issue("TileProjection",ProjectConversionSeverity.Loss,$"Original terrain/owner/option normalization affected {normalized} cells; raw tiles retained in source snapshot.");
            if(boundary!=0)Issue("BoundaryProjection",ProjectConversionSeverity.Loss,$"Original limit wall affected {boundary} edge cells; raw tiles retained in source snapshot.");
            var metadata=records.SingleOrDefault(record=>record.Category=="Map");
            if(metadata is not null)
                foreach(var rule in LegacyNativeImportRules.Metadata)
                {
                    var from=metadata.Fields.Single(field=>field.Name==rule.NativeField);
                    var to=info.Fields.Single(field=>field.Name==rule.ProjectField);
                    if(rule.ProjectField=="SpeechFile")metadata.File.Data.Span.Slice(metadata.Offset+from.Offset,32).CopyTo(info.File.Bytes.AsSpan(info.Offset+to.Offset,32));
                    else if(to.Kind==NativeValueKind.WesternAnsi)ConversionText(info,to,metadata.Read(from),Issue);
                    else ConversionWrite(info,to.Name,long.Parse(metadata.Read(from),CultureInfo.InvariantCulture));
                }
            else Issue("MissingMetadata",ProjectConversionSeverity.Information,"Tile-only input uses original map metadata defaults.");
            ConversionWrite(info,"Version",TargetGameVersion??17);
            ConversionWrite(info,"FillID",ResizeFillPreset??DefaultResizeFillPreset);
            var links=new List<byte[]>();
            var areaScopes=DecodeNativeAreaScopes(NativeAreaScopes);var emittedAreaScopes=new bool[areaScopes.Count];
            var areaAddresses=NativeAreaAddresses();
            var ownerScopes=DecodeNativeOwnerScopes(NativeOwnerScopes);var emittedOwnerScopes=new HashSet<(int,int,int)>();
            var ownerSites=NativeOwnerSites().ToDictionary(site=>(site.Record.File,site.Record.Offset,site.Record.Length));
            var ownerStates=ownerScopes.Where(row=>row.File>=0).ToDictionary(row=>(row.File,row.Ordinal,row.Member));
            uint Root(uint scope,uint identity,string field,ushort trigger,uint band=0,bool retainEmpty=false)
            {
                if(trigger==0 && !retainEmpty)return 0;
                var bytes=new byte[LegacyProjectSchemas.Size("TRIGGERLINK")];
                ConversionWrite(bytes,"TRIGGERLINK","node.type",scope);ConversionWrite(bytes,"TRIGGERLINK","trigger",trigger);
                if(field.Length!=0)ConversionWrite(bytes,"TRIGGERLINK",field,identity);
                if(scope==0x10)ConversionWrite(bytes,"TRIGGERLINK","band",band);
                links.Add(bytes);return identity;
            }
            uint OwnerPoint(NativeRecord record)
            {
                if(!ownerSites.TryGetValue((record.File,record.Offset,record.Length),out var site)
                    ||!ownerStates.TryGetValue((site.Address.File,site.Address.Ordinal,site.Address.Member),out var state))return 0;
                if(state.Present)
                {
                    Root(state.Scope,state.ID,ScopeTarget(state.Scope),state.Root,state.Band,retainEmpty:true);
                    emittedOwnerScopes.Add((state.File,state.Ordinal,state.Member));
                }
                return state.ID;
            }
            ushort ReadRoot(NativeRecord record,string name)=>checked((ushort)ConversionNumber(record,name));
            if(metadata is not null)Root(0x20,0,"",ReadRoot(metadata,"wMainTrigger"));
            var playerIDs=new HashSet<uint>();
            foreach(var record in records.Where(record=>record.Category=="GAMEPLAYER"))
            {
                var id=(uint)ConversionNumber(record,"bGlobalPlayer");
                if(id is <1 or >7){Issue("InvalidPlayer",ProjectConversionSeverity.Error,$"Player {id} cannot index seven slots; its original record is retained.");continue;}
                if(!playerIDs.Add(id))Issue("DuplicatePlayer",ProjectConversionSeverity.Loss,$"Player {id}: original last-record assignment order retained, all source records preserved.");
                var target=players[id-1];
                foreach(var rule in LegacyNativeImportRules.Players)ConversionWrite(target,rule.ProjectField,ConversionNumber(record,rule.NativeField));
                ConversionText(target,target.Fields.Single(field=>field.Name=="PlayerName"),record.Read(record.Fields.Single(field=>field.Name=="szPlayerName")),Issue);
                Root(1,id,"player",ReadRoot(record,"bPlayerTrigger"));
            }
            var defaultPlayerSlots=Enumerable.Range(1,7).Select(id=>(uint)id).Where(id=>!playerIDs.Contains(id)).ToArray();
            if(defaultPlayerSlots.Length!=0)
                Issue("PlayerSlotDefaults",ProjectConversionSeverity.Information,
                    $"DKM stores seven positional player slots. Slots without native records, including unused keepers, use original defaults: [{string.Join(", ",defaultPlayerSlots)}]. All original native records remain in the source companion.");
            var things=new List<byte[]>();var rectangles=new List<byte[]>();var bands=new List<byte[]>();
            var nativeGateIDs=new Dictionary<NativeGateAddress,uint>();
            foreach(var record in records.Where(record=>record.File.Signature==0xBE && !record.Category.StartsWith("BandHero",StringComparison.Ordinal)))
            {
                long N(string name)=>ConversionNumber(record,name);
                var category=record.Category;
                if(category is "GAMEOBJECT" or "GAMETRAP" or "GAMEDOOR" or "GAMENEUTRAL" or "GAMEHERO" or "GAMEKEEPER")
                {
                    var bytes=new byte[LegacyProjectSchemas.Size("MAPTHING")];
                    void Set(string name,long value)=>ConversionWrite(bytes,"MAPTHING",name,value);
                    var type=category=="GAMEDOOR"?1:category=="GAMETRAP"?2:category=="GAMEOBJECT"?(N("bObjectID") is >=0x75 and <=0x81?4:3):0;
                    Set("node.type",type);
                    var prefix=category switch{"GAMEOBJECT"=>"Object","GAMETRAP"=>"Trap","GAMEDOOR"=>"Door","GAMENEUTRAL"=>"Neutral","GAMEHERO"=>"Hero",_=>"Keeper"};
                    Set("x",N("dw"+prefix+"LocationX"));Set("y",N("dw"+prefix+"LocationY"));
                    Set("id",N(type==0?"bGlobalCreatureID":"b"+prefix+"ID"));
                    var owner=category=="GAMEHERO"?1:category=="GAMENEUTRAL"?2:N("bGlobalPlayer");
                    if(owner>7){Issue("ThingOwner",ProjectConversionSeverity.Loss,$"{category} owner {owner} becomes neutral; raw owner retained.");owner=2;}
                    Set("owner",owner);
                    if(type==0)
                    {
                        Set("dataAll",N("bGlobalLevel"));Set("goldheld",N("bGlobalGoldHeld"));Set("health",N("dwGlobalHealth"));Set("creaturesoptions",N("bGlobalBehavior"));
                        if(category=="GAMEHERO")foreach(var (target,source) in new[]{("herosoptions","bHeroOptions"),("objective","bHeroObjective"),("area","bHeroTargetArea"),("ennemy","bGlobalPlayer")})Set(target,N(source));
                    }
                    else if(type==1)Set("dataAll",N("bDoorLocked"));
                    else if(type==2)Set("dataAll",N("bTrapShots"));
                    else if(N("bObjectID") is 1 or 2 or 3)Set("dataAll",N("dwObjectGoldValue"));
                    else if(N("bObjectID")==4)Set("dataAll",N("dwObjectSpell"));
                    if(type!=2 || ownerSites.TryGetValue((record.File,record.Offset,record.Length),out var trapSite)
                        && ownerStates.ContainsKey((trapSite.Address.File,trapSite.Address.Ordinal,trapSite.Address.Member)))
                    {
                        var unique=OwnerPoint(record);Set("uniqueid",unique);
                        if(unique!=0)
                        {
                            if(type is 1 or 3 or 4)Issue("OriginalScopeCorrection",ProjectConversionSeverity.Information,$"{category} native root retained in its actual object category; original import helper scope quirk corrected.");
                        }
                    }
                    things.Add(bytes);
                }
                else if(category is "GAMEAREA" or "GAMEEFFECT" or "GAMEGATE")
                {
                    if(category=="GAMEGATE" && N("bGateType")==0x0E)
                    {
                        var owner=N("bGlobalPlayer");if(owner is >=1 and <=7)ConversionWrite(players[owner-1],"PlayerHealth",N("wGateHealth"));
                        else Issue("HeartOwner",ProjectConversionSeverity.Error,"Heart owner cannot index seven player slots.");
                        continue;
                    }
                    var bytes=new byte[LegacyProjectSchemas.Size("MAPRECTEX")];void Set(string name,long value)=>ConversionWrite(bytes,"MAPRECTEX",name,value);
                    var type=category=="GAMEAREA"?0:category=="GAMEGATE"?1:2;Set("node.type",type);
                    if(type==0)
                    {
                        Set("rect.x1",N("dwAreaLocationStartX"));Set("rect.y1",N("dwAreaLocationStartY"));Set("rect.x2",N("dwAreaLocationEndX"));Set("rect.y2",N("dwAreaLocationEndY"));
                        Set("id",N("bAreaID"));Set("nextid",N("bAreaNextID"));Set("waitdelay",N("wAreaWaitDelay"));Set("flags",N("bAreaFlags"));
                        // Keep the original native import's physical thing/root
                        // order. Stored orphan points have no physical row and
                        // are appended separately after the live records.
                        var address=areaAddresses.Single(pair=>SameRecord(pair.Value,record)).Key;
                        var scopeIndex=areaScopes.FindIndex(scope=>scope.File==address.File && scope.Ordinal==address.Ordinal);
                        if(scopeIndex>=0)
                        {
                            var scope=areaScopes[scopeIndex];Root(4,scope.ID,"area",scope.Root,retainEmpty:true);emittedAreaScopes[scopeIndex]=true;
                        }
                    }
                    else if(type==1)
                    {
                        Set("rect.x1",N("dwGateLocationX"));Set("rect.y1",N("dwGateLocationY"));Set("rect.x2",N("dwGateLocationX"));Set("rect.y2",N("dwGateLocationY"));Set("id",NativeGateID(record));
                        nativeGateIDs.Add(NativeGateAddressOf(record),(uint)NativeGateID(record));
                        if(N("bGateType") is not (0x21 or 0x25))Issue("GateType",ProjectConversionSeverity.Error,"Unknown native gate type retained in original bundle.");
                    }
                    else
                    {
                        Set("rect.x1",N("dwEffectStartX"));Set("rect.y1",N("dwEffectStartY"));Set("rect.x2",N("dwEffectStartX")+N("wEffectWidth"));Set("rect.y2",N("dwEffectStartY")+N("wEffectHeight"));Set("id",N("bEffectID"));
                        for(var index=1;index<=4;index++)Set("effect"+index,N("wEffect"+index));Set("freq",N("bEffectFrequency"));
                    }
                    Set("rect.color",type==0?0xFFFF00:type==1?0xAAAAAA:0x7F00FF);Set("rect.textcolor",type==0?0xFFFF00:type==1?0xAAAAAA:0x7F00FF);
                    var id=LegacyProjectSchemas.Fields("MAPRECTEX").Single(field=>field.Name=="id").Read(bytes);
                    var name=LegacyProjectSchemas.Fields("MAPRECTEX").Single(field=>field.Name=="name");name.Encode((type==0?"Area ":type==1?"Gate ":"Effect ")+id).CopyTo(bytes,name.Offset);
                    rectangles.Add(bytes);
                }
                else if(category=="GAMEBAND")
                {
                    var bytes=new byte[LegacyProjectSchemas.Size("MTHBAND")];void Set(string name,long value)=>ConversionWrite(bytes,"MTHBAND",name,value);
                    var id=(uint)N("bBandID");Set("id",id);Set("uniqueid",OwnerPoint(record));
                    var name=LegacyProjectSchemas.Fields("MTHBAND").Single(field=>field.Name=="name");name.Encode("Band "+id).CopyTo(bytes,name.Offset);
                    for(var index=0;index<16;index++)
                    {
                        var hero=new NativeRecord(record.File,record.Offset+51+index*32,32,"BandHero",LegacySchemas.Fields("GAMEBANDHERO"));
                        if(ConversionNumber(hero,"bGlobalCreatureID")==0 || ConversionNumber(hero,"bGlobalBehavior")==0)break;
                        Set($"heros[{index}].isvalid",1);
                        foreach(var (target,source) in new[]{("level","bGlobalLevel"),("enemy","bGlobalPlayer"),("area","bHeroTargetArea"),("objective","bHeroObjective"),("id","bGlobalCreatureID"),("health","dwGlobalHealth"),("goldheld","bGlobalGoldHeld"),("options","bHeroOptions"),("options2","bGlobalBehavior")})Set($"heros[{index}].{target}",ConversionNumber(hero,source));
                        Set($"heros[{index}].uniqueid",OwnerPoint(hero));
                    }
                    bands.Add(bytes);
                }
                else Issue("UnknownThing",ProjectConversionSeverity.Error,$"Unsupported native thing {category}, retained in original bundle.");
            }
            for(var index=0;index<areaScopes.Count;index++)if(!emittedAreaScopes[index])
            {var scope=areaScopes[index];Root(4,scope.ID,"area",scope.Root,retainEmpty:true);}
            foreach(var scope in ownerScopes.Where(scope=>scope.Present && (scope.File<0||!emittedOwnerScopes.Contains((scope.File,scope.Ordinal,scope.Member)))))
                Root(scope.Scope,scope.ID,ScopeTarget(scope.Scope),scope.Root,scope.Band,retainEmpty:true);
            project.ConversionSection(LegacyThings,6,things.OrderBy(bytes=>BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8))).SelectMany(bytes=>bytes).ToArray());
            project.ConversionSection(LegacyAreas,1,rectangles.SelectMany(bytes=>bytes).ToArray());
            project.ConversionSection(LegacyBands,5,bands.SelectMany(bytes=>bytes).ToArray());
            var retainedVariables=ConversionVariables(records,info,players,Issue);
            var events=new List<(ushort ID,uint Signature,byte[] Bytes)>();
            foreach(var record in records.Where(record=>record.File.Signature==0xD2))
            {
                var signature=record.File.UInt32(record.Offset);
                if(record.Length!=24 || signature is not (0xD5 or 0xD6)){Issue("UnknownEvent",ProjectConversionSeverity.Error,"Unsupported native event retained in original bundle.");continue;}
                var native=record.File.Data.Span.Slice(record.Offset,record.Length);var type=native[22];var structure=signature==0xD5?"TRIGGER":"ACTION";var bytes=new byte[LegacyProjectSchemas.Size(structure)];
                void Set(string field,long value)=>ConversionWrite(bytes,structure,field,value);
                Set("node.type",signature);Set("ctrl",type);Set(signature==0xD5?"repeat":"one",native[23]);
                foreach(var argument in LegacyNativeImportRules.EventArguments(signature,type))Set(argument.Name,long.Parse(argument.Read(native),CultureInfo.InvariantCulture));
                if(LegacyEventSchemas.Find(signature,type) is null)Issue("UnknownEventType",ProjectConversionSeverity.Error,$"Unsupported {signature:X}/{type:X} retained with raw payload.");
                if(signature==0xD6 && type==0x20)
                {
                    var x=BinaryPrimitives.ReadUInt16LittleEndian(native[12..])-1;var y=BinaryPrimitives.ReadUInt16LittleEndian(native[14..])-1;
                    var gate=NativeGateTarget(record);
                    Set("param5",NativeGateTargetID(record));
                    if(gate is null)Issue("MissingDestroyGate",ProjectConversionSeverity.Loss,$"Destroy-gate target {x},{y} is absent; missing ID{NativeGateTargetID(record)} is retained. DKM cannot encode these raw coordinates; compilation uses source fallback1,1 unless verified source-payload retention applies.");
                    else if(!nativeGateIDs.TryGetValue(NativeGateAddressOf(gate),out var targetGateID))Issue("UnprojectableDestroyGate",ProjectConversionSeverity.Error,$"Destroy-gate target {x},{y} exists but cannot be represented by a verified project gate.");
                    else Set("param5",targetGateID);
                }
                if(LegacyEventSchemas.Find(signature,type) is { } writer)
                {
                    var compiledPayload=writer.DefaultPayload.ToArray();
                    foreach(var argument in writer.Arguments)
                    {
                        if(argument.Name.StartsWith("Param",StringComparison.Ordinal))
                        {
                            var field=LegacyProjectSchemas.Fields(structure).Single(field=>field.Name=="param"+argument.Name[5..]);
                            ConversionEncode(argument,long.Parse(field.Read(bytes),CultureInfo.InvariantCulture)).CopyTo(compiledPayload,argument.Offset-8);
                        }
                        else native.Slice(argument.Offset,argument.Size).CopyTo(compiledPayload.AsSpan(argument.Offset-8,argument.Size));
                    }
                    if(!native.Slice(8,8).SequenceEqual(compiledPayload))Issue("EventPayloadProjection",ProjectConversionSeverity.Loss,$"{writer.Name}: original import ignores some argument/reserved bits; complete native event retained in source snapshot.");
                }
                var id=BinaryPrimitives.ReadUInt16LittleEndian(native[16..]);Set("id",id);Set(signature==0xD5?"or":"next",BinaryPrimitives.ReadUInt16LittleEndian(native[18..]));
                if(signature==0xD5)
                {
                    Set("and",BinaryPrimitives.ReadUInt16LittleEndian(native[20..]));
                    if(nativeExpandedConditions.Contains(id))BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(14),1);
                }
                events.Add((id,signature,bytes));
            }
            eventIDs=ConversionCanonicalEventIDs(events,links,Issue);
            ConversionPredecessors(events,links,Issue);
            var payload=new List<byte>();payload.AddRange(BitConverter.GetBytes((uint)links.Count));payload.AddRange(BitConverter.GetBytes((uint)events.Count(item=>item.Signature==0xD5)));payload.AddRange(BitConverter.GetBytes((uint)events.Count(item=>item.Signature==0xD6)));
            foreach(var link in links)payload.AddRange(link);
            foreach(var item in events.OrderBy(item=>item.Signature))payload.AddRange(item.Bytes);
            // Copy full-width aliases, including bytes after NUL.
            if(CanEditScenarioNames && NativeScenarioNames.Length==ScenarioNamesSize)payload.AddRange(NativeScenarioNames.Data.Span[12..].ToArray());
            else {payload.AddRange(new byte[146*64]);Issue("UnsupportedAliases",ProjectConversionSeverity.Error,"Unsupported native alias companion retained in original snapshot; project aliases cannot be guessed.");}
            project.ConversionSection(LegacyEvents,3,payload.ToArray());
            var families=GlobalDefinitionRows().Where(row=>row.Editable && row.Record is not null).Select(row=>row.Family).Distinct().ToArray();
            project.EnsureProjectDefinitionFamilies(families);
            var projected=project.GlobalDefinitionRows().ToDictionary(row=>(row.Family,row.Index));
            foreach(var row in GlobalDefinitionRows().Where(row=>row.Record is not null))
            {
                if(!row.Editable){Issue("UnverifiedDefinitions",ProjectConversionSeverity.Error,$"Unverified {row.Family} catalogue remains in original bundle.");continue;}
                var destination=projected[(row.Family,row.Index)].Record!;
                foreach(var rule in GlobalDefinitionDefaults.Rules(row.Family))
                    ConversionWrite(destination,rule.Name,long.Parse(rule.ReadField.Read(row.Record!.File.Data.Span.Slice(row.Record.Offset,row.Record.Length)),CultureInfo.InvariantCulture));
            }
            foreach(var missing in MissingReferences)Issue("MissingReference",ProjectConversionSeverity.Information,missing);
            if(statistics is not null)project.ConversionSection(ActivityStatisticsPayload.Signature,ActivityStatisticsPayload.Version,statistics.Encode());
            result=FromLegacyProject(projectPath,LegacySectionFile.Parse(project.SerializeLegacyProject()));
            result.nativeRetainedVariables=retainedVariables;
            project.activityStatistics?.Dispose();
        }
        // The independent converted document is unsaved; preparation never resets the source.
        result.originals[result.MainFile]=[];result.ResetHistory();
        if(!SaveWorkingFingerprint().AsSpan().SequenceEqual(fingerprint))throw new InvalidOperationException("Document changed during conversion.");
        return new(this,fingerprint,statistics?.Encode(),result,snapshots,Array.AsReadOnly(issues.ToArray()),eventIDs);
    }
    private void ConversionSection(uint signature,int version,byte[] payload)
    {
        var bytes=new byte[12+payload.Length];WriteUInt32(bytes,0,signature);WriteUInt32(bytes,4,(uint)version);WriteUInt32(bytes,8,(uint)payload.Length);payload.CopyTo(bytes,12);
        var section=projectSections.SingleOrDefault(file=>file.Signature==signature);
        if(section is null){section=new(MainFile.SourcePath+$"#{signature:X}",$"section-{projectSections.Count}",bytes);Add(section);projectSections.Add(section);}
        else section.Bytes=bytes;
        optionalProjectSections.Remove(section);
    }
    private static long ConversionNumber(NativeRecord record,string name)=>long.Parse(record.Read(record.Fields.Single(field=>field.Name==name)),CultureInfo.InvariantCulture);
    private static void ConversionWrite(NativeRecord record,string name,long value)
    {
        var field=record.Fields.Single(field=>field.Name==name);ConversionEncode(field,value).CopyTo(record.File.Bytes,record.Offset+field.Offset);
    }
    private static void ConversionWrite(byte[] bytes,string structure,string name,long value)
    {
        var field=LegacyProjectSchemas.Fields(structure).Single(field=>field.Name==name);ConversionEncode(field,value).CopyTo(bytes,field.Offset);
    }
    private static byte[] ConversionEncode(NativeField field,long value)=>field.Encode((field.Kind switch
    {
        NativeValueKind.Byte=>unchecked((byte)value).ToString(CultureInfo.InvariantCulture),NativeValueKind.UInt16=>unchecked((ushort)value).ToString(CultureInfo.InvariantCulture),
        NativeValueKind.Int16=>unchecked((short)value).ToString(CultureInfo.InvariantCulture),NativeValueKind.UInt32=>unchecked((uint)value).ToString(CultureInfo.InvariantCulture),
        _=>unchecked((int)value).ToString(CultureInfo.InvariantCulture)
    }));
    private static void ConversionText(NativeRecord target,NativeField field,string text,Action<string,ProjectConversionSeverity,string> issue)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);var encoding=Encoding.GetEncoding(1252,new EncoderReplacementFallback("?"),DecoderFallback.ExceptionFallback);
        var bytes=encoding.GetBytes(text);var count=Math.Min(bytes.Length,field.Size-1);
        if(encoding.GetString(bytes)!=text || bytes.Length>=field.Size)issue("TextProjection",ProjectConversionSeverity.Loss,$"{field.Name}: Windows-1252 substitution or truncation; original Unicode/raw bytes retained.");
        target.File.Bytes.AsSpan(target.Offset+field.Offset,field.Size).Clear();bytes.AsSpan(0,count).CopyTo(target.File.Bytes.AsSpan(target.Offset+field.Offset));
    }
    private static MapTile ConversionTile(MapTile raw)
    {
        var owner=raw.Owner is >=1 and <=7?raw.Owner:(byte)2;var variant=raw.Variant is 1 or 2?raw.Variant:(byte)1;
        foreach(var group in new[]{"World","Rooms","Gates","Walls"})
        {
            var match=LegacyCompilerRules.Elements(group).FirstOrDefault(item=>(byte)(item.ID>>24)==raw.Terrain && (byte)(item.ID>>8)==variant && (group!="Walls" || (byte)(item.ID>>16)==owner));
            if(match is not null)return new(raw.Terrain,group=="Rooms"?owner:(byte)(match.ID>>16),variant);
        }
        return new(2,2,1);
    }

    /// <summary>Source game-to-editor-to-game selection projection. Raw imported
    /// cells stay in the DKCP snapshot; the historical KMAP gets canonical bytes.</summary>
    internal static MapTile SelectionCompatibleTile(MapTile raw)
    {
        var tile=ConversionTile(raw);
        return tile.Terrain==254?tile with {Terrain=30}:tile;
    }
    private static byte[] ConversionVariables(IReadOnlyList<NativeRecord> records,NativeRecord info,NativeRecord[] players,Action<string,ProjectConversionSeverity,string> issue)
    {
        var retained=new List<byte>();
        void Retain(NativeRecord record,string code,string detail)
        {
            var bytes=record.File.Data.Span.Slice(record.Offset,record.Length);
            if(!IsUnrepresentedNativeVariable(bytes))throw new InvalidDataException("Invalid native variable preservation request.");
            retained.AddRange(bytes.ToArray());
            issue(code,ProjectConversionSeverity.Loss,detail+(code=="CreatureLevelModifiers"
                ?" Original bytes stay in the native-source ZIP. This editor stores level-percentage edits separately in DKM; keep the ZIP beside the project for compilation."
                :" Kept byte-for-byte in the native-source ZIP. Keep that ZIP beside the DKM for compilation in this editor; these entries are not editable in DKM."));
        }
        var creatureIDs=new[]{"BadCreatures","BadElites","Heros"}.SelectMany(LegacyCompilerRules.Elements).ToArray();
        int Creature(long id)=>Array.FindIndex(creatureIDs,item=>item.Variable==(uint)id);
        foreach(var record in records.Where(record=>record.File.Signature==0xE6 && !Path.GetFileName(record.File.ExportPath).Equals("GlobalVariables.kwd",StringComparison.OrdinalIgnoreCase)))
        {
            long N(string name)=>ConversionNumber(record,name);var kind=record.File.UInt32(record.Offset);
            var player=kind==2?N("wGlobalPlayer"):kind is 1 or 0x42?N("dwGlobalPlayer"):0;
            if(kind is 1 or 2 or 0x42 && player is <1 or >7){issue("VariablePlayer",ProjectConversionSeverity.Error,$"Variable player {player} is invalid.");continue;}
            if(kind==1)
            {
                var slot=Creature(N("dwGlobalCreatureID"));if(slot<0){issue("VariableCreature",ProjectConversionSeverity.Error,"Unknown creature count catalogue ID.");continue;}
                ConversionWrite(players[player-1],$"CreaturesCount[{slot}]",N("dwCreatureCount"));
            }
            else if(kind==2)
            {
                var group=N("wThingType") switch{1=>"VarRooms",3=>"Traps",4=>"Doors",5=>"Spells",_=>""};
                if(group.Length==0){Retain(record,"AvailabilityType","Availability category has no DKM slot.");continue;}
                var slot=LegacyCompilerRules.Elements(group).Select((item,index)=>(item,index)).Where(pair=>pair.item.Variable==(uint)N("dwThingID")).Select(pair=>pair.index).DefaultIfEmpty(-1).First();
                if(slot<0){Retain(record,"AvailabilityID","Availability catalogue ID has no DKM slot.");continue;}
                ConversionWrite(players[player-1],$"{(group=="VarRooms"?"Rooms":group)}[{slot}]",N("dwThingState"));
            }
            else if(kind==0x42)
            {
                var other=N("dwAllieWith");if(other is <1 or >7){issue("AlliancePlayer",ProjectConversionSeverity.Error,"Unknown allied player.");continue;}
                ConversionWrite(info,$"Allies[{player-1}][{other-1}]",1);ConversionWrite(info,$"Allies[{other-1}][{player-1}]",1);
            }
            else if(kind==0x4D)
            {
                var slot=Creature(N("dwGlobalCreatureID"));var parameter=N("dwCreatureParamType");
                if(slot<0 || parameter is <1 or >4){issue("CreatureParameter",ProjectConversionSeverity.Error,"Unknown creature parameter catalogue/slot.");continue;}
                ConversionWrite(info,$"CreaturesParams[{slot}][{parameter-1}]",N("dwCreatureNewValue"));
            }
            else
            {
                if(!MapVariables.Definitions.Any(definition=>definition.ID==kind))
                {
                    var visible=record.Length==16&&CreatureLevelModifierControls.HasStatistic(record.File.UInt32(record.Offset+4))
                        &&(kind==65&&record.File.UInt32(record.Offset+12) is >=2 and <=10||kind==224&&record.File.UInt32(record.Offset+12)==10);
                    Retain(record,visible?"CreatureLevelModifiers":"UnknownVariable",$"Map variable 0x{kind:X} has no original DKM field.");continue;
                }
                ConversionWrite(info,$"Variables[{kind}]",N("dwGlobalData"));
            }
        }
        return retained.ToArray();
    }
    private static IReadOnlyDictionary<ushort,ushort> ConversionCanonicalEventIDs(List<(ushort ID,uint Signature,byte[] Bytes)> events,List<byte[]> roots,Action<string,ProjectConversionSeverity,string> issue)
    {
        var mapping=new Dictionary<ushort,ushort>();var used=new HashSet<ushort>{0};
        var nativeIDs=events.Select(item=>item.ID).ToHashSet();
        // Reserve dangling IDs too: allocation must never make an absent native target appear valid.
        foreach(var item in events)
        {
            var structure=item.Signature==0xD5?"TRIGGER":"ACTION";
            foreach(var name in item.Signature==0xD5?new[]{"or","and"}:new[]{"next"})
            {
                var target=ushort.Parse(LegacyProjectSchemas.Fields(structure).Single(field=>field.Name==name).Read(item.Bytes),CultureInfo.InvariantCulture);
                if(!nativeIDs.Contains(target))used.Add(target);
            }
        }
        var rootField=LegacyProjectSchemas.Fields("TRIGGERLINK").Single(field=>field.Name=="trigger");
        foreach(var root in roots){var target=ushort.Parse(rootField.Read(root),CultureInfo.InvariantCulture);if(!nativeIDs.Contains(target))used.Add(target);}
        foreach(var item in events)
            if(item.ID!=0 && (item.Signature==0xD5)==(unchecked((short)item.ID)>0))
            {if(!mapping.TryAdd(item.ID,item.ID))issue("DuplicateEventID",ProjectConversionSeverity.Error,$"Duplicate native event ID {item.ID}.");used.Add(item.ID);}
        int nextTrigger=1,nextAction=ushort.MaxValue;
        foreach(var item in events)
        {
            if(item.ID==0){issue("ZeroEventID",ProjectConversionSeverity.Error,"Native zero event ID retained as invalid.");continue;}
            if(mapping.ContainsKey(item.ID))continue;
            int candidate;
            if(item.Signature==0xD5){while(nextTrigger<=short.MaxValue && used.Contains((ushort)nextTrigger))nextTrigger++;candidate=nextTrigger++;if(candidate>short.MaxValue)throw new InvalidDataException("DKM positive trigger IDs exhausted.");}
            else {while(nextAction>=0x8000 && used.Contains((ushort)nextAction))nextAction--;candidate=nextAction--;if(candidate<0x8000)throw new InvalidDataException("DKM negative action IDs exhausted.");}
            mapping.Add(item.ID,(ushort)candidate);used.Add((ushort)candidate);
            issue("EventIDRemapped",ProjectConversionSeverity.Information,$"Native event {item.ID} becomes signed DKM ID {(short)candidate}; all references translated.");
        }
        for(var index=0;index<events.Count;index++)
        {
            var item=events[index];var structure=item.Signature==0xD5?"TRIGGER":"ACTION";
            var id=mapping.GetValueOrDefault(item.ID,item.ID);ConversionWrite(item.Bytes,structure,"id",id);
            foreach(var name in item.Signature==0xD5?new[]{"or","and"}:new[]{"next"})
            {
                var target=ushort.Parse(LegacyProjectSchemas.Fields(structure).Single(field=>field.Name==name).Read(item.Bytes),CultureInfo.InvariantCulture);
                ConversionWrite(item.Bytes,structure,name,mapping.GetValueOrDefault(target,target));
            }
            events[index]=(id,item.Signature,item.Bytes);
        }
        foreach(var root in roots){var target=ushort.Parse(rootField.Read(root),CultureInfo.InvariantCulture);ConversionWrite(root,"TRIGGERLINK","trigger",mapping.GetValueOrDefault(target,target));}
        return mapping;
    }
    private static void ConversionPredecessors(List<(ushort ID,uint Signature,byte[] Bytes)> events,List<byte[]> roots,Action<string,ProjectConversionSeverity,string> issue)
    {
        var nodes=new Dictionary<ushort,(uint Signature,byte[] Bytes)>();
        foreach(var item in events)
        {
            if(item.ID==0 || (item.Signature==0xD5)!=(unchecked((short)item.ID)>0))issue("EventIDSign",ProjectConversionSeverity.Error,$"Invalid signed event ID {item.ID}.");
            if(!nodes.TryAdd(item.ID,(item.Signature,item.Bytes)))issue("DuplicateEventID",ProjectConversionSeverity.Error,$"Duplicate event ID {item.ID}.");
        }
        // Diagnose every stored graph, including unused event chains. The
        // original root import only restores predecessors for reachable nodes;
        // unused malformed records still need an explicit preservation report.
        var checkedColors=new Dictionary<ushort,byte>();
        foreach(var seed in nodes.Keys)
        {
            if(checkedColors.GetValueOrDefault(seed)==2)continue;
            var check=new Stack<(ushort ID,bool Exit)>();check.Push((seed,false));
            while(check.TryPop(out var item))
            {
                if(item.ID==0)continue;
                if(item.Exit){checkedColors[item.ID]=2;continue;}
                if(!nodes.TryGetValue(item.ID,out var node)){issue("MissingEventLink",ProjectConversionSeverity.Error,$"Absent event {item.ID}; all stored graph links retained.");continue;}
                if(checkedColors.GetValueOrDefault(item.ID)==1){issue("CyclicEventGraph",ProjectConversionSeverity.Error,$"Cycle at event {item.ID}; stored graph retained.");continue;}
                if(checkedColors.GetValueOrDefault(item.ID)==2)continue;
                checkedColors[item.ID]=1;check.Push((item.ID,true));
                var fields=LegacyProjectSchemas.Fields(node.Signature==0xD5?"TRIGGER":"ACTION");
                foreach(var name in node.Signature==0xD5?new[]{"and","or"}:new[]{"next"})
                    check.Push((ushort.Parse(fields.Single(field=>field.Name==name).Read(node.Bytes),CultureInfo.InvariantCulture),false));
            }
        }
        var colors=new Dictionary<ushort,byte>();var parents=new Dictionary<ushort,ushort>();
        var rootField=LegacyProjectSchemas.Fields("TRIGGERLINK").Single(field=>field.Name=="trigger");
        foreach(var root in roots)
        {
            var stack=new Stack<(ushort ID,ushort Parent,bool Exit)>();stack.Push((ushort.Parse(rootField.Read(root),CultureInfo.InvariantCulture),0,false));
            while(stack.TryPop(out var current))
            {
                if(current.ID==0)continue;
                if(current.Exit){colors[current.ID]=2;continue;}
                if(!nodes.TryGetValue(current.ID,out var node)){issue("MissingEventLink",ProjectConversionSeverity.Error,$"Absent event {current.ID}; link bytes retained.");continue;}
                if(colors.GetValueOrDefault(current.ID)==1){issue("CyclicEventGraph",ProjectConversionSeverity.Error,$"Cycle at event {current.ID}; source graph retained without recursion.");continue;}
                if(colors.GetValueOrDefault(current.ID)==2)
                {if(parents[current.ID]!=current.Parent)issue("SharedPredecessor",ProjectConversionSeverity.Information,$"Shared event {current.ID} has multiple parents; first predecessor selected and all outgoing links retained.");continue;}
                colors[current.ID]=1;parents[current.ID]=current.Parent;BinaryPrimitives.WriteUInt16LittleEndian(node.Bytes.AsSpan(LegacyProjectSchemas.Size("NODE")),current.Parent);
                stack.Push((current.ID,current.Parent,true));var structure=node.Signature==0xD5?"TRIGGER":"ACTION";
                var fields=LegacyProjectSchemas.Fields(structure);
                foreach(var name in node.Signature==0xD5?new[]{"and","or"}:new[]{"next"})
                    stack.Push((ushort.Parse(fields.Single(field=>field.Name==name).Read(node.Bytes),CultureInfo.InvariantCulture),current.ID,false));
            }
        }
    }
}
