using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Automation;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckCameraLibraryMcp(string directory,bool native,string language)
    {
        var seed=NativeMapDocument.CreateNewMap(new("CameraMcp","CameraMcp",20,20,NativeFormat:native));
        var added=native?seed.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="Action"),new(0,0)):seed.AddLegacyRecord("Action",new(0,0));
        seed.SetEventType(added,LegacyEventSchemas.All.Single(t=>t.Name=="ACTION_CAMERAPATH"));
        var action=seed.Records().Single(r=>r.File==added.File&&r.Offset==added.Offset);
        seed.SetField(action,action.Fields.Single(f=>f.Name==CameraPaths.FieldName(action,1)),native?"255":"9999");
        seed.SetField(action,action.Fields.Single(f=>f.Name==CameraPaths.FieldName(action,2)),"7");
        var input=seed.SaveCopy(Path.Combine(directory,$"camera-mcp-{language}-{native}"),temporary:true);
        var shell=SaveShell(new AppSettings {Language=language});shell.DocumentReportPresenter=_=>{};
        try
        {
            shell.OpenMapFromPath(input);SaveLayout(shell);shell.StartMcp("camera-check-"+Guid.NewGuid().ToString("N"),Path.Combine(directory,"bridge-"+Guid.NewGuid().ToString("N")));
            var engine=(EditorAutomation)typeof(MainWindow).GetField("mcpAutomation",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(shell)!;
            var workspace=(EditorWorkspace)shell.FindName("Editor");var doc=workspace.Document!;
            action=doc.Records().Single(CameraPaths.IsCameraAction);
            Control<TabControl>(workspace,"WorkspaceTabs").SelectedIndex=1;
            var categories=Control<ComboBox>(workspace,"CategoryPicker");categories.SelectedItem=categories.Items.Cast<object>().Single(x=>Get<string>(x,"ID")==action.Category);
            var list=Control<ListBox>(workspace,"RecordList");list.SelectedItem=list.Items.Cast<object>().Single(x=>Get<NativeRecord>(x,"Record").Offset==action.Offset);
            var button=Control<Button>(workspace,"CameraPathButton");
            JsonElement Json(object? value)=>JsonSerializer.SerializeToElement(value,AutomationJson.Options);
            string Token()=>Json(engine.State()).GetProperty("stateToken").GetString()!;
            AutomationReply Raw(string tool,object args)=>engine.Execute(new(tool,Json(args)));
            JsonElement Call(string command,string? query=null,int? id=null)
            {
                var args=new Dictionary<string,object>{{"command",command},{"expectedStateToken",Token()}};
                if(query is not null)args["query"]=query;if(id is not null)args["pathID"]=id;
                var reply=Raw("dk2_camera_library",args);Expect(reply.Ok,"Camera MCP "+command+": "+reply.Code+" "+reply.Message);return Json(reply.Result);
            }
            var baseline=doc.ReadEditingFingerprint();var history=JsonSerializer.Serialize(doc.GetHistory());var revision=doc.EditRevision;
            void Exact()=>Expect(doc.ReadEditingFingerprint()==baseline&&doc.EditRevision==revision&&JsonSerializer.Serialize(doc.GetHistory())==history,"Camera MCP private action changed map/history.");
            Expect(Raw("dk2_camera_library",new{expectedStateToken=Token(),command="Inspect"}).Code=="camera_library_unavailable","No-dialog command succeeded.");
            var old=Token();
            PatrolModal(()=>button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)),PatrolStep<CameraPathWindow>(child=>
            {
                try
                {
                    Expect(Raw("dk2_camera_library",new{expectedStateToken=old,command="Inspect"}).Code=="stale_state","Opening library did not invalidate token.");
                    var unknown=Call("Inspect");Expect(!unknown.GetProperty("canApply").GetBoolean()&&unknown.GetProperty("choices").GetArrayLength()==52,"Unknown ID was normalized or catalogue missing.");
                    Expect(Raw("dk2_history_undo",new{expectedStateToken=Token()}).Code=="editor_busy","Map command bypassed camera modality.");
                    Expect(Raw("dk2_camera_library",new{expectedStateToken=Token(),command="Search"}).Code=="arguments","Missing query accepted.");
                    Expect(Raw("dk2_camera_library",new{expectedStateToken=Token(),command="Inspect",query="extra"}).Code=="arguments","Unexpected query accepted.");
                    var filtered=Call("Search","050");
                    Expect(filtered.GetProperty("choices").GetArrayLength()==1&&filtered.GetProperty("choices")[0].GetProperty("id").GetInt32()==50,"Displayed padded ID 050 was not searchable.");
                    Expect(((TextBox)child.FindName("SearchBox")).Text=="050"&&((ListBox)child.FindName("PathList")).Items.Count==1,"MCP search bypassed actual WPF controls.");
                    var stale=Token();var selected=Call("Select",id:50);Expect(selected.GetProperty("selectedPathID").GetInt32()==50&&selected.GetProperty("ceilingMode").GetInt32()==0,"Filtered selection/default inversion failed.");
                    Expect(Raw("dk2_camera_library",new{expectedStateToken=stale,command="Cancel"}).Code=="stale_state","Stale selection token accepted.");Exact();
                    var noMatches=Call("Search","missing camera xyz");Expect(noMatches.GetProperty("choices").GetArrayLength()==0&&noMatches.GetProperty("selectedPathID").GetInt32()==50,"No-match query lost private selection.");
                    Expect(Raw("dk2_camera_library",new{expectedStateToken=Token(),command="Select",pathID=150}).Code=="command_failed","Hidden selection accepted.");
                    var tokenBeforeLanguage=Token();LocalizationService.SetLanguage(language=="en"?"ru":"en");
                    Expect(Raw("dk2_camera_library",new{expectedStateToken=tokenBeforeLanguage,command="Inspect"}).Code=="stale_state","Live translation did not invalidate dialog token.");
                    var translated=Call("Inspect");Expect(translated.GetProperty("query").GetString()=="missing camera xyz"&&translated.GetProperty("selectedPathID").GetInt32()==50
                        &&translated.GetProperty("description").GetString()==CameraPaths.Choices.Single(x=>x.ID==50).Description(LocalizationService.CurrentLanguage),"Language changed query/selection or stale description.");
                    LocalizationService.SetLanguage(language);var portal=Call("Search",language=="en"?"portal":"портал");Expect(portal.GetProperty("choices").GetArrayLength()==18,"Description search did not return portal IDs50..67.");
                    var retained=Token();Expect(Raw("dk2_camera_library",new{expectedStateToken=Token(),command="Search",query=new string('x',257)}).Code=="command_failed"&&Token()==retained,"Oversized search changed state.");
                    var other=new Window{Owner=shell,ShowInTaskbar=false,Left=-20000,Top=-20000,Width=200,Height=100};other.Show();
                    try{Expect(Raw("dk2_camera_library",new{expectedStateToken=Token(),command="Inspect"}).Code=="camera_library_unavailable","Another dialog was bypassed.");}finally{other.Close();}
                    Exact();Call("Cancel");
                }
                finally{child.Close();}
            }));
            Exact();
            PatrolModal(()=>button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)),PatrolStep<CameraPathWindow>(child=>
            {
                try{Call("Search","150");Call("Select",id:150);var result=Call("Apply");Expect(result.GetProperty("applied").GetBoolean()&&result.GetProperty("closed").GetBoolean(),"Apply did not use/close actual window.");}
                finally{child.Close();}
            }));
            var after=doc.ReadEditingFingerprint();Expect(after!=baseline&&doc.GetHistory().Undo.Count==1,"Apply was not one atomic Undo.");
            Expect(action.Read(action.Fields.Single(f=>f.Name==CameraPaths.FieldName(action,1)))=="150"&&action.Read(action.Fields.Single(f=>f.Name==CameraPaths.FieldName(action,2)))=="7","Apply changed area or lost path.");
            Expect(Raw("dk2_history_undo",new{expectedStateToken=Token()}).Ok&&doc.ReadEditingFingerprint()==baseline,"Undo did not fully restore unknown imported ID.");
            Expect(Raw("dk2_history_redo",new{expectedStateToken=Token()}).Ok&&doc.ReadEditingFingerprint()==after,"Redo differed.");
            Console.WriteLine($"PASS camera MCP {language} {(native?"native":"project")}: real modal search padded ID/descriptions, controls, unknown ID, languages, stale/argument/busy guards, Cancel, atomic Apply/UndoRedo");
        }
        finally{shell.DiscardChangesPrompt=()=>true;shell.Close();}
    }
}
