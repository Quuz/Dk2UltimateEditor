using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckScenarioEmbeddedOwners(bool native,string language)
    {
        foreach(var name in new[]{"ACTION_CAMERAPATH","ACTION_ADDCREATURE"})
        {
            var document=NativeMapDocument.CreateNewMap(new("EmbeddedOwner","EmbeddedOwner",20,20,NativeFormat:native));
            document.AddMapRegion(MapRegionKind.Area,new(2,2,3,3));
            var added=native?document.AddRecord(RecordTemplate.All.Single(item=>item.Structure=="Action"),new(0,0)):document.AddLegacyRecord("Action",new(0,0));
            var type=LegacyEventSchemas.All.Single(item=>item.Name==name);document.SetEventType(added,type);
            var record=document.Records().Single(item=>item.File==added.File&&item.Offset==added.Offset);
            var bytes=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var history=document.GetHistory();
            var revision=document.EditRevision;var count=document.EditorChangeCount;
            void Exact()=>Expect(bytes.All(row=>row.Key.Data.Span.SequenceEqual(row.Value))&&document.EditRevision==revision&&document.EditorChangeCount==count
                &&document.GetHistory().Undo.SequenceEqual(history.Undo)&&document.GetHistory().Redo.SequenceEqual(history.Redo),"Nested scenario dialog/outer Cancel changed live map or history.");
            foreach(var mode in new[]{"create","edit","standalone"})
            {
                ScenarioCreationWindow? wizard=null;ScenarioParameterWindow? page=null;Window? parent=null;
                try
                {
                    if(mode=="standalone")parent=page=new(document,record,()=>true,()=>true);
                    else
                    {
                        wizard=new(document,document.CreateScenarioCreationContext(),0xD6,()=>true,()=>true,_=>{},
                            mode=="create"?type:null,existingRecord:mode=="edit"?record:null);parent=wizard;
                    }
                    parent.Show();
                    if(wizard is not null){Expect(wizard.Next(),"Scenario owner check could not open parameters.");page=wizard.ParameterEditor;}
                    parent.UpdateLayout();var button=(Button)page!.FindName(name=="ACTION_CAMERAPATH"?"ScenarioCameraLibrary":"ScenarioSpawnFlags");
                    Expect(ReferenceEquals(Window.GetWindow(button),parent),"Embedded parameter button is not hosted by the visible parent.");
                    Console.WriteLine($"Nested owner {language} {(native?"native":"project")} {name} {mode}");
                    if(name=="ACTION_CAMERAPATH")
                    {
                        PatrolModal(()=>button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)),PatrolStep<CameraPathWindow>(child=>
                        {
                            Expect(ReferenceEquals(child.Owner,parent)&&child.Owner.IsVisible,"Camera library belongs to a hidden parameter window.");
                            var list=(ListBox)child.FindName("PathList");list.SelectedItem=list.Items.Cast<object>().Single(item=>Get<CameraPathChoice>(item,"Choice").ID==50);
                            Expect(child.ApplySelection(),"Nested camera selection failed.");child.Close();
                        }));
                        Expect(((TextBox)page.FindName("ScenarioParam1")).Text=="50","Child did not return its private camera choice.");
                    }
                    else
                    {
                        PatrolModal(()=>button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)),PatrolStep<ScenarioSpawnFlagsWindow>(child=>
                        {
                            Expect(ReferenceEquals(child.Owner,parent)&&child.Owner.IsVisible,"Spawn options belong to a hidden parameter window.");
                            Expect(child.ApplyDraft(),"Nested spawn options failed.");child.Close();
                        }));
                    }
                    Exact();parent.Close();Exact();
                }
                finally{parent?.Close();if(wizard is null)page?.Close();}
            }
        }
        Console.WriteLine($"PASS {language} {(native?"native":"project")} camera/spawn real modal ownership in create/edit/standalone; child Apply then outer Cancel preserves all bytes/history");
    }
}
