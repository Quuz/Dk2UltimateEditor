using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only: actual WPF execution remains manual acceptance.
    private static void CheckScenarioPointBandDetails(bool native,string language)
    {
        var document=NativeMapDocument.CreateNewMap(new("PointBandUI","PointBandUI",20,20,NativeFormat:native));
        var band=native?document.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="GAMEBAND"),new(2,2)):document.AddLegacyRecord("Band",new(2,2));
        Expect(MemoryMarshal.TryGetArray(band.File.Data,out var raw),"Point band UI fixture unavailable.");
        for(var index=0;index<2;index++)
        {
            var offset=band.Offset+(native?51+index*32:12+index*44);
            if(native){raw.Array![offset+14]=3;raw.Array[offset+15]=index==0?(byte)3:(byte)1;raw.Array[offset+16]=47;raw.Array[offset+27]=23;raw.Array[offset+28]=17;}
            else
            {
                BinaryPrimitives.WriteInt32LittleEndian(raw.Array!.AsSpan(offset),1);BinaryPrimitives.WriteInt32LittleEndian(raw.Array.AsSpan(offset+4),3);
                foreach(var(at,value)in new[]{(12,17),(28,23),(32,index==0?3:1),(36,47)})BinaryPrimitives.WriteInt32LittleEndian(raw.Array.AsSpan(offset+at),value);
            }
        }
        var bytes=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var revision=document.EditRevision;var count=document.EditorChangeCount;
        void Exact()=>Expect(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&document.EditRevision==revision&&document.EditorChangeCount==count,"Point band presentation changed document.");
        foreach(var scope in new uint[]{8,16})
        {
            var window=new ScenarioPointWindow(document,()=>true,()=>true,owner=>"Owner "+owner.Scope,_=>{},initialScope:scope);
            try
            {
                ShowManagerCallerWindow(window);Expect(window.Next(),"Band owner page did not open.");
                var list=(ListBox)window.FindName("ScenarioPointOwners");Expect(list.Items.Count==3,"Band group/detail row count incorrect.");
                var rows=list.Items.Cast<ListBoxItem>().ToArray();
                Expect((rows[0].Tag is ScenarioPointOwnerChoice)==(scope==8),"Band header selection authority incorrect.");
                Expect(rows.Skip(1).All(row=>(row.Tag is ScenarioPointOwnerChoice)==(scope==16)),"Member detail authority incorrect.");
                Expect(rows.Where(row=>row.Tag is null).All(row=>!row.IsEnabled&&!row.Focusable),"Information row is keyboard selectable.");
                var available=rows.First(row=>row.Tag is ScenarioPointOwnerChoice {CanSelect:true});list.SelectedItem=available;
                var selected=window.SelectedOwner;Expect(selected is not null,"Owner choice failed.");
                list.SelectedItem=rows.First(row=>row.Tag is null);Expect(ReferenceEquals(list.SelectedItem,available)&&window.SelectedOwner==selected,"Programmatic information-row selection stole owner choice.");
                var labels=rows.Select(row=>Get<string>(row.Content,"Label")).ToArray();Expect(labels[1].Contains("47"),"Leader objective area missing.");
                LocalizationService.SetLanguage(language=="en"?"ru":"en");
                Expect(window.SelectedOwner==selected&&((ListBoxItem)list.SelectedItem).Tag is ScenarioPointOwnerChoice,"Language change lost physical owner choice.");
                Expect(list.Items.Cast<ListBoxItem>().All(row=>row.ContentTemplate is not null),"Rebuilt group rows lost image template.");Exact();
                LocalizationService.SetLanguage(language);
            }
            finally{window.Close();}
            Exact();
        }
    }
}
