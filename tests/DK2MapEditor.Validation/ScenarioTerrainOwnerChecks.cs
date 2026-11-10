using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioParameterDraftChecks
{
    public static void RunTerrainOwners()
    {
        using var fixture=JsonDocument.Parse(typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioTerrainOwnersSource.json")!);
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root is not null&&!File.Exists(Path.Combine(root.FullName,"DK2MapEditor.slnx")))root=root.Parent;
        foreach(var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Terrain-owner source input changed.");
        Expect(fixture.RootElement.GetProperty("SelectableCases").GetInt32()==56,"Incomplete terrain-owner source capture.");
        foreach(var native in new[]{false,true})
        {
            var checkedCases=0;
            foreach(var source in fixture.RootElement.GetProperty("Pages").EnumerateArray())
            {
                var type=source.GetProperty("Type").GetString()!;var terrain=source.GetProperty("TerrainParameter").GetInt32();
                var doc=NativeMapDocument.CreateNewMap(new("TerrainOwners","TerrainOwners",20,20,NativeFormat:native));doc.SetHistoryDepth(65535);
                var page=pages.Single(item=>item.Type==type);var record=Add(doc,native,page);
                string Field(int number)=>(native?"Param":"param")+number;
                var terrainField=Fields(native,page).Single(field=>field.Name==Field(terrain));
                var ownerField=Fields(native,page).Single(field=>field.Name==Field(2));
                var cases=source.GetProperty("Cases").EnumerateArray().ToArray();
                Expect(ScenarioNamedCatalogues.Choices(type,terrain).Select(item=>int.Parse(item.Value,CultureInfo.InvariantCulture)).SequenceEqual(cases.Select(item=>item.GetProperty("Terrain").GetInt32())),"Source terrain callback catalogue/order differs.");
                void Encode(byte[] bytes,Operand operand,int value)
                {
                    if(operand.Size==1)bytes[record.Offset+operand.Offset]=checked((byte)value);
                    else BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(record.Offset+operand.Offset),value);
                }
                foreach(var owner in new[]{1,2,3,4,5,6,7,255})
                {
                    Encode(Mutable(record.File),ownerField,owner);Encode(Mutable(record.File),terrainField,99);
                    var before=Snapshot(doc);var history=doc.GetHistory().Undo.Count;var count=doc.EditorChangeCount;
                    var baseline=doc.CreateScenarioParameterDraft(record);
                    foreach(var item in cases)
                    {
                        var id=item.GetProperty("Terrain").GetInt32();var text=id.ToString(CultureInfo.InvariantCulture);
                        var forced=item.GetProperty("ForcedOwner");var expectedOwner=forced.ValueKind==JsonValueKind.Null?owner:forced.GetInt32();
                        var state=ScenarioTerrainOwners.State(type,text)!;
                        Expect(state.KnownTerrain&&state.TerrainParameter==terrain&&state.OwnerParameter==2&&state.OwnerEnabled==item.GetProperty("OwnerEnabled").GetBoolean()
                            &&state.ForcedOwner==(forced.ValueKind==JsonValueKind.Null?null:expectedOwner.ToString(CultureInfo.InvariantCulture)),"Terrain owner state differs from source notification.");
                        // Capture fresh authority after each Undo; the old draft is intentionally stale.
                        baseline=doc.CreateScenarioParameterDraft(record);
                        var selected=doc.UpdateScenarioTerrainSelection(baseline,text);Exact(before);
                        Expect(baseline.Values[Field(terrain)]=="99"&&baseline.Values[Field(2)]==owner.ToString(CultureInfo.InvariantCulture),"Selection mutated immutable baseline.");
                        Expect(selected.Values[Field(terrain)]==text&&selected.Values[Field(2)]==expectedOwner.ToString(CultureInfo.InvariantCulture),"Selection lost forced/pending imported owner.");
                        var expected=before.ToDictionary(item=>item.Key,item=>item.Value.ToArray());Encode(expected[record.File],terrainField,id);Encode(expected[record.File],ownerField,expectedOwner);
                        var notices=0;void Notice(object? sender,EventArgs args)=>notices++;doc.Changed+=Notice;
                        doc.ApplyScenarioEditorParameterDraft(selected);doc.Changed-=Notice;Exact(expected);
                        Expect(notices==1&&doc.GetHistory().Undo.Count==history+1&&doc.EditorChangeCount==unchecked(count+1),"Terrain/owner acceptance split counter, notification or Undo.");
                        doc.Undo();Exact(before);Expect(doc.EditorChangeCount==count,"Terrain owner Undo lost source count.");doc.Redo();Exact(expected);doc.Undo();Exact(before);
                        Reject(()=>doc.ApplyScenarioEditorParameterDraft(selected));Exact(before);checkedCases++;
                    }
                    baseline=doc.CreateScenarioParameterDraft(record);
                    foreach(var invalid in new[]{"0","-1","99","9999999999","04","x"})
                    {Reject(()=>doc.UpdateScenarioTerrainSelection(baseline,invalid));Exact(before);}
                    var unknown=ScenarioTerrainOwners.State(type,"99")!;Expect(!unknown.KnownTerrain&&!unknown.OwnerEnabled&&unknown.ForcedOwner is null,"Unknown terrain manufactured source authority.");
                    var pending=doc.UpdateScenarioControlPage(baseline,[new(Field(2),"7")]);
                    var unlocked=doc.UpdateScenarioTerrainSelection(pending,"8");
                    Expect(unlocked.Values[Field(2)]=="7"&&ScenarioTerrainOwners.State(type,"8")!.OwnerEnabled,"Ordinary terrain discarded pending owner.");Exact(before);
                    var neutral=doc.UpdateScenarioTerrainSelection(pending,"4");var ordinary=doc.UpdateScenarioTerrainSelection(neutral,"8");
                    Expect(ordinary.Values[Field(2)]=="2","Unlocking terrain invented a previous owner restore.");Exact(before);
                    var hero=doc.UpdateScenarioTerrainSelection(neutral,"35");Expect(hero.Values[Field(2)]=="1","Hero lair did not switch owner from Neutral.");Exact(before);
                    var other=NativeMapDocument.CreateNewMap(new("Other","Other",20,20,NativeFormat:native));Reject(()=>other.UpdateScenarioTerrainSelection(baseline,"4"));Exact(before);
                    var raw=Mutable(record.File);raw[record.Offset+terrainField.Offset]^=1;var changed=Snapshot(doc);Reject(()=>doc.UpdateScenarioTerrainSelection(baseline,"4"));Exact(changed);raw[record.Offset+terrainField.Offset]^=1;Exact(before);
                    if(native)
                    {var header=Mutable(record.File)[4];Mutable(record.File)[4]=5;changed=Snapshot(doc);Reject(()=>doc.UpdateScenarioTerrainSelection(baseline,"4"));Exact(changed);Mutable(record.File)[4]=header;Exact(before);}
                    doc.Edit("Read-only nested terrain callback",()=>Reject(()=>doc.UpdateScenarioTerrainSelection(baseline,"4")));Exact(before);
                    Exact(before);Expect(doc.GetHistory().Undo.Count==history&&doc.EditorChangeCount==count,"Cancelled/invalid terrain callbacks changed live history or counter.");
                }
            }
            Expect(checkedCases==448,"Incomplete native/project terrain-owner combinations.");
            Console.WriteLine($"TERRAIN OWNER DETAIL native={native} source cases=56 owner/import combinations=448 private callback exact bytes one Undo/count notification unknown/stale/raw/foreign/version guards");
        }
        Expect(ScenarioTerrainOwners.State("ACTION_CHANGEOWNER","4") is null,"Owner-only action acquired a terrain dependency.");
    }
}
