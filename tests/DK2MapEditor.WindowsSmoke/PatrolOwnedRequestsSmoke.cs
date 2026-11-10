using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static partial class Program
{
    private sealed record PatrolModalStep(Type WindowType,Action<Window> Execute);
    private static PatrolModalStep PatrolStep<T>(Action<T> action) where T:Window
        =>new(typeof(T),window=>action((T)window));
    private static T PatrolControl<T>(Window window,string name) where T:FrameworkElement
        =>window.FindName(name) as T??throw new InvalidOperationException("Missing patrol window control "+name);
    private static IEnumerable<DependencyObject> PatrolLogicalChildren(DependencyObject parent)
    {
        yield return parent;
        foreach(var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
            foreach(var nested in PatrolLogicalChildren(child))yield return nested;
    }

    // This is UI execution when invoked: ShowDialog, Loaded, HWND and sound paths
    // are real. It must not run without UI authorization. No product presenter
    // shortcuts are installed; callbacks operate the actual newly opened window.
    private static void PatrolModal(Action operation,params PatrolModalStep[] steps)
    {
        var app=Application.Current??throw new InvalidOperationException("Patrol modal test requires a live WPF application.");
        var dispatcher=app.Dispatcher;
        var existing=app.Windows.Cast<Window>().ToHashSet();
        var index=0;var done=false;Exception? failure=null;DispatcherOperation? scheduled=null;
        Window[] Opened()=>app.Windows.Cast<Window>().Where(window=>!existing.Contains(window)&&window.IsVisible).ToArray();
        void CloseOpened()
        {
            // Children first, so nested ShowDialog unwinds before its owner.
            foreach(var window in Opened().Reverse())window.Close();
        }
        void Pump()
        {
            if(done)return;
            // Queue the next callback before Execute: a range-error button opens
            // another nested dispatcher frame while the outer callback is active.
            scheduled=dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(Pump));
            try
            {
                if(failure is not null){CloseOpened();return;}
                var opened=Opened();
                var leaf=opened.LastOrDefault(window=>window.IsLoaded&&!opened.Any(child=>ReferenceEquals(child.Owner,window)));
                if(leaf is null)return;
                Expect(index<steps.Length,"Unexpected extra patrol modal window: "+leaf.GetType().Name);
                var step=steps[index];
                Expect(step.WindowType.IsInstanceOfType(leaf),$"Expected {step.WindowType.Name}, got {leaf.GetType().Name}.");
                index++;step.Execute(leaf);
            }
            catch(Exception error){failure=error;CloseOpened();}
        }
        var timeout=new DispatcherTimer(DispatcherPriority.Send,dispatcher){Interval=TimeSpan.FromSeconds(15)};
        timeout.Tick+=(_,_)=>{failure??=new TimeoutException("Patrol modal callback did not complete within15seconds.");CloseOpened();};
        scheduled=dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(Pump));timeout.Start();
        try{operation();}
        finally{done=true;timeout.Stop();scheduled?.Abort();CloseOpened();}
        if(failure is not null)throw new InvalidOperationException("Patrol owned-window interaction failed.",failure);
        Expect(index==steps.Length,$"Patrol command opened {index} of {steps.Length} required windows.");
    }

    private static void CheckPatrolRoutes(NativeMapDocument document,string language)
    {
        var areas=Enumerable.Range(1,4).Select(id=>document.AddMapRegion(MapRegionKind.Area,new(id*3,id*3,2,2))).ToArray();
        var owner=document.ScriptOwners().Single(item=>item.Scope==4&&item.Record.Offset==areas[0].Offset&&item.Record.File==areas[0].File);
        var root=document.EnsureScriptScope(owner);
        document.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED"));
        var workspace=new EditorWorkspace{Document=document};
        workspace.Measure(new Size(1400,950));workspace.Arrange(new Rect(0,0,1400,950));workspace.UpdateLayout();
        var tabs=Control<TabControl>(workspace,"WorkspaceTabs");tabs.SelectedIndex=3;
        var routes=Control<ListBox>(workspace,"PatrolRouteList");var points=Control<ListBox>(workspace,"PatrolPointList");
        var picker=Control<ComboBox>(workspace,"PatrolAreaPicker");var wait=Control<TextBox>(workspace,"PatrolWait");
        var panel=Control<StackPanel>(workspace,"PatrolPanel");var status=Control<TextBlock>(workspace,"PatrolStatus");
        var canvas=Control<MapCanvas>(workspace,"Canvas");
        EditorPatrolManagerRoute Selected()=>Get<EditorPatrolManagerRoute>(routes.SelectedItem,"Route");
        string Lists()=>string.Join(';',routes.Items.Cast<object>().Select(item=>
        {var route=Get<EditorPatrolManagerRoute>(item,"Route");return route.Key+":"+string.Join(',',route.Points.Select(point=>point.ID));}));
        Dictionary<NativeFile,byte[]> Snapshot()=>document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        void Exact(Dictionary<NativeFile,byte[]> expected)=>Expect(document.Files.Count==expected.Count&&expected.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Patrol UI changed unrelated/opaque bytes.");
        void Candidate(uint id)=>picker.SelectedItem=picker.Items.Cast<object>().Single(item=>Get<uint>(item,"ID")==id);
        void Command(string name){var button=Control<Button>(workspace,name);Expect(button.IsEnabled,"Disabled patrol command "+name);button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
        void Route(uint first)=>routes.SelectedItem=routes.Items.Cast<object>().Single(item=>Get<EditorPatrolManagerRoute>(item,"Route").Points.FirstOrDefault()?.ID==first);
        void IDs(params uint[] expected)=>Expect(Selected().Points.Select(point=>point.ID).SequenceEqual(expected),"Owned patrol occurrence list/order mismatch.");
        void ReadOnly(Action action,bool keepLists=true)
        {
            var bytes=Snapshot();var beforeLists=Lists();var revision=document.EditRevision;var count=document.EditorChangeCount;
            var history=document.GetHistory();var activity=document.ReadActivityStatistics()!.Encode();var notices=0;
            EventHandler changed=(_,_)=>notices++;document.Changed+=changed;
            try{action();}finally{document.Changed-=changed;}
            Exact(bytes);var after=document.GetHistory();
            Expect(document.EditRevision==revision&&document.EditorChangeCount==count&&notices==0
                &&history.Undo.SequenceEqual(after.Undo)&&history.Redo.SequenceEqual(after.Redo)
                &&activity.AsSpan().SequenceEqual(document.ReadActivityStatistics()!.Encode())
                &&(!keepLists||beforeLists==Lists()),"Read-only patrol request changed map/lists/history/counters/activity.");
        }
        void Accepted(Action action)
        {
            var revision=document.EditRevision;var count=document.EditorChangeCount;var history=document.GetHistory();
            var activity=document.ReadActivityStatistics()!.Encode();var notices=0;EventHandler changed=(_,_)=>notices++;
            document.Changed+=changed;try{action();}finally{document.Changed-=changed;}
            Expect(document.EditRevision==revision+1&&document.EditorChangeCount==unchecked(count+1)&&notices==1
                &&document.GetHistory().Undo.Count==history.Undo.Count+1
                &&activity.AsSpan().SequenceEqual(document.ReadActivityStatistics()!.Encode()),"Accepted patrol UI command did not commit exactly once.");
        }
        PatrolModalStep Selector(uint id,int pause,bool insert=false)=>PatrolStep<PatrolAreaSelectionWindow>(window=>
        {
            var list=PatrolControl<ListBox>(window,"PatrolSelectionCandidates");var number=PatrolControl<TextBox>(window,"PatrolSelectionPause");
            var slider=PatrolControl<Slider>(window,"PatrolSelectionSlider");var apply=PatrolControl<Button>(window,"PatrolSelectionAccept");
            var expected=document.PatrolAreas().Where(point=>!insert||point.NextID==0).Select(point=>point.ID).ToArray();
            Expect(list.Items.Cast<object>().Select(item=>Get<EditorPatrolAreaCandidate>(item,"Candidate").ID).SequenceEqual(expected),"Actual selector candidate order/filter mismatch.");
            Expect(list.SelectedIndex==-1&&number.Text=="0"&&slider.Value==0&&!apply.IsEnabled,"Selector invented initial authority/pause.");
            list.SelectedItem=list.Items.Cast<object>().Single(item=>Get<EditorPatrolAreaCandidate>(item,"Candidate").ID==id);
            var stored=document.PatrolAreas().Single(point=>point.ID==id).WaitDelay;
            Expect(number.Text==Math.Min(stored,250).ToString(System.Globalization.CultureInfo.InvariantCulture),"Selector did not load selected area's pause.");
            number.Text=pause.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Expect(slider.Value==pause&&apply.IsEnabled,"Selector text/slider/accept did not synchronize.");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");
            Expect(number.Text==pause.ToString(System.Globalization.CultureInfo.InvariantCulture)&&Get<EditorPatrolAreaCandidate>(list.SelectedItem,"Candidate").ID==id,"Selector translation lost draft.");
            LocalizationService.SetLanguage(language);apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(window.Result is not null,"Actual selector Apply failed.");
        });
        PatrolModalStep Removal(bool remember=false)=>PatrolStep<PatrolRemovalWindow>(window=>
        {
            var checkbox=PatrolControl<CheckBox>(window,"PatrolRemovalRemember");var yes=PatrolControl<Button>(window,"PatrolRemovalYes");var no=PatrolControl<Button>(window,"PatrolRemovalNo");
            Expect(yes.IsDefault&&yes.IsEnabled&&no.IsEnabled,"Removal default/Yes/No availability mismatch.");
            checkbox.IsChecked=remember;Expect(no.IsEnabled!=remember,"Remember did not disable No.");
            yes.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(window.CommitFailure is null,"Removal postcommit notification failed.");
        });
        PatrolModalStep Warning(bool remember,bool acknowledge)=>PatrolStep<PatrolWarningWindow>(window=>
        {
            var before=Snapshot();var count=document.EditorChangeCount;var history=document.GetHistory();
            var checkbox=PatrolControl<CheckBox>(window,"PatrolWarningSuppress");var ok=PatrolControl<Button>(window,"PatrolWarningAcknowledge");
            Expect(ok.IsDefault&&ok.IsEnabled&&PatrolLogicalChildren(window).OfType<Image>().Count(image=>image.Source is not null)==1,"Warning lacks actual icon/default OK.");
            checkbox.IsChecked=remember;if(acknowledge)ok.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));else window.Close();
            Exact(before);Expect(document.EditorChangeCount==count&&document.GetHistory().Undo.SequenceEqual(history.Undo),"Warning reply changed accepted edit/history.");
        });
        void Pause(string text)=>PatrolModal(()=>Command("PatrolPauseButton"),PatrolStep<PatrolPauseWindow>(window=>
        {
            var number=PatrolControl<TextBox>(window,"PatrolPauseText");var ok=PatrolControl<Button>(window,"PatrolPauseAccept");
            Expect(ok.IsDefault&&PatrolLogicalChildren(window).OfType<Slider>().Count()==0
                &&PatrolLogicalChildren(window).OfType<TextBox>().Where(box=>box.IsReadOnly).Select(box=>box.Text).Order().SequenceEqual(new[]{"0","250"}),"Numeric pause incorrectly uses slider/spinner or lost readonly bounds.");
            number.Text=text;LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(number.Text==text,"Numeric draft lost text on translation.");LocalizationService.SetLanguage(language);
            ok.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(window.Result is not null&&window.CommitFailure is null,"Numeric pause OK failed.");
        }));

        try
        {
            Expect(routes.Items.Count==0&&picker.Items.Count==4&&panel.IsEnabled&&tabs.Items.Count==10,"Patrol manager invented stored paths or failed to bind areas.");
            canvas.SetSelection(new(1,1,2,2));workspace.Copy();var buffer=workspace.BufferedSelection;var copy=SelectionFile.Serialize(buffer!);var selection=canvas.Selection;
            ReadOnly(()=>Command("PatrolNewButton"),keepLists:false);
            Expect(Selected().Points.Count==0&&points.SelectedIndex==-1&&!Control<Button>(workspace,"PatrolPauseButton").IsEnabled
                &&!Control<Button>(workspace,"PatrolInsertButton").IsEnabled&&!Control<Button>(workspace,"PatrolRemoveButton").IsEnabled,"Empty root granted point command authority.");
            ReadOnly(()=>PatrolModal(()=>Command("PatrolAppendButton"),PatrolStep<PatrolAreaSelectionWindow>(window=>
            {Expect(!PatrolControl<Button>(window,"PatrolSelectionAccept").IsEnabled,"No-choice selector enabled Apply.");window.Close();})));
            Accepted(()=>PatrolModal(()=>Command("PatrolClearButton"),Removal()));Expect(routes.Items.Count==0,"Clear retained empty root.");
            Candidate(1);wait.Text="15";Accepted(()=>Command("PatrolStartButton"));IDs(1);
            Expect(Selected().Points[0].Point!.WaitDelay==15,"Start lost first area's pause.");
            var before=Snapshot();Accepted(()=>PatrolModal(()=>Command("PatrolAppendButton"),Selector(2,21)));IDs(1,2);
            var after=Snapshot();workspace.Undo();Exact(before);workspace.Redo();Exact(after);
            Accepted(()=>PatrolModal(()=>Command("PatrolAppendButton"),Selector(3,31)));IDs(1,2,3);
            points.SelectedIndex=1;
            ReadOnly(()=>PatrolModal(()=>Command("PatrolPauseButton"),PatrolStep<PatrolPauseWindow>(window=>
            {Expect(PatrolControl<TextBox>(window,"PatrolPauseText").Text=="21","Pause did not start from selected physical area.");PatrolControl<TextBox>(window,"PatrolPauseText").Text="44";PatrolControl<Button>(window,"PatrolPauseCancel").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));})));
            Accepted(()=>Pause("44"));Expect(Selected().Points[1].Point!.WaitDelay==44,"Numeric command changed another point.");
            before=Snapshot();Accepted(()=>Pause("44"));Exact(before);workspace.Undo();Exact(before);workspace.Redo();Exact(before);
            foreach(var invalid in new[]{"251","-1"})
                ReadOnly(()=>PatrolModal(()=>Command("PatrolPauseButton"),
                    PatrolStep<PatrolPauseWindow>(window=>{PatrolControl<TextBox>(window,"PatrolPauseText").Text=invalid;PatrolControl<Button>(window,"PatrolPauseAccept").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}),
                    PatrolStep<Window>(child=>{Expect(child.Owner is PatrolPauseWindow&&PatrolLogicalChildren(child).OfType<Image>().Any(image=>image.Source is not null),"Range failure did not open owned warning child.");PatrolLogicalChildren(child).OfType<Button>().Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}),
                    PatrolStep<PatrolPauseWindow>(window=>{Expect(PatrolControl<TextBox>(window,"PatrolPauseText").Text==invalid,"Range warning rewrote draft.");window.Close();})));
            foreach(var parsed in new[]{("12x",12u),("",0u),("999999999999999",0u)})
            {Accepted(()=>Pause(parsed.Item1));Expect(Selected().Points[1].Point!.WaitDelay==parsed.Item2,"Numeric decimal-prefix/failed-conversion contract mismatch.");}
            Accepted(()=>PatrolModal(()=>Command("PatrolInsertButton"),Selector(4,22,true)));IDs(1,4,2,3);
            ReadOnly(()=>PatrolModal(()=>Command("PatrolRemoveButton"),PatrolStep<PatrolRemovalWindow>(window=>PatrolControl<Button>(window,"PatrolRemovalNo").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)))));
            ReadOnly(()=>PatrolModal(()=>Command("PatrolRemoveButton"),PatrolStep<PatrolRemovalWindow>(window=>
            {PatrolControl<CheckBox>(window,"PatrolRemovalRemember").IsChecked=true;Expect(!PatrolControl<Button>(window,"PatrolRemovalNo").IsEnabled,"Remember kept No enabled.");window.Close();})));
            Expect(!Control<Button>(workspace,"PatrolRemovalReset").IsEnabled,"Close remembered removal suppression.");
            Accepted(()=>PatrolModal(()=>Command("PatrolRemoveButton"),Removal(true)));IDs(1,2,3);
            Expect(document.PatrolAreas().Single(point=>point.ID==4).NextID==0&&Control<Button>(workspace,"PatrolRemovalReset").IsEnabled,"Remembered removal failed to bypass point or enable reset.");
            Accepted(()=>PatrolModal(()=>Command("PatrolAppendButton"),Selector(1,15)));IDs(1,2,3,1);
            Expect(Selected().Closed&&Get<uint>(points.Items[3],"ID")==1,"Closing occurrence was not kept separately.");
            points.SelectedIndex=3;Accepted(()=>Command("PatrolRemoveButton"));IDs(1,2,3);
            Expect(!Selected().Closed&&Selected().Points[0].Point!.NextID==2&&Selected().Points[^1].Point!.NextID==0,"Auto-Yes closing removal cleared loop-entry edge.");
            ReadOnly(()=>Command("PatrolRemovalReset"));Expect(!Control<Button>(workspace,"PatrolRemovalReset").IsEnabled,"Removal reset did not restore prompts.");

            ReadOnly(()=>Command("PatrolNewButton"),keepLists:false);
            Accepted(()=>PatrolModal(()=>Command("PatrolAppendButton"),Selector(4,22)));IDs(4);
            Accepted(()=>PatrolModal(()=>Command("PatrolAppendButton"),Selector(2,0),Warning(true,false)));IDs(4,2);
            Expect(Selected().IsChanged&&document.PatrolAreas().Single(point=>point.ID==2).NextID==3&&!Control<Button>(workspace,"PatrolWarningReset").IsEnabled,"Linked Append expanded tail or warning close remembered suppression.");
            Accepted(()=>PatrolModal(()=>Command("PatrolAppendButton"),Selector(3,31)));IDs(4,2,3);points.SelectedIndex=1;
            Accepted(()=>PatrolModal(()=>Command("PatrolRemoveButton"),Removal(),Warning(false,true)));IDs(4,3);
            Expect(document.PatrolAreas().Single(point=>point.ID==4).NextID==3&&routes.Items.Count==2,"Shared removal rebuilt private lists or failed edge rewiring.");
            ReadOnly(()=>PatrolModal(()=>Route(1),Warning(true,false)));
            Expect(Selected().IsChanged&&!Control<Button>(workspace,"PatrolWarningReset").IsEnabled,"Selected indirect warning was absent or close remembered checkbox.");
            ReadOnly(()=>PatrolModal(()=>points.SelectedIndex=1,Warning(true,true)));
            Expect(Control<Button>(workspace,"PatrolWarningReset").IsEnabled&&!Control<Button>(workspace,"PatrolRemovalReset").IsEnabled,"Warning suppression changed independent removal preference.");
            ReadOnly(()=>points.SelectedIndex=0);ReadOnly(()=>Command("PatrolWarningReset"));
            Accepted(()=>PatrolModal(()=>Command("PatrolClearButton"),Removal()));
            Expect(document.PatrolAreas().Count==4&&ScriptGraph.Build(document).Events.Count==1&&document.PatrolAreas().Single(point=>point.ID==4).NextID==3,"Clear deleted records/scripts or unrelated surviving route.");

            Route(4);points.SelectedIndex=0;before=Snapshot();Click(panel,"Editor.RegionProperties");
            Expect(tabs.SelectedIndex==1&&Get<NativeRecord>(Control<ListBox>(workspace,"RecordList").SelectedItem,"Record").Offset==areas[3].Offset,"Patrol properties selected another physical area.");Exact(before);
            tabs.SelectedIndex=3;Click(panel,"Editor.RegionShow");Expect(tabs.SelectedIndex==0&&canvas.Selection==document.GetMapRegion(areas[3])!.Bounds,"Patrol navigation lost actual bounds.");Exact(before);canvas.SetSelection(selection);
            tabs.SelectedIndex=3;Click(panel,"Editor.PatrolDrawStart");Expect(canvas.IsPickingRegion&&tabs.SelectedIndex==0,"Quick start did not enter geometry picking.");
            wait.Text="173";LocalizationService.SetLanguage(language=="en"?"ru":"en");workspace.RefreshLanguage();
            Expect(canvas.IsPickingRegion&&wait.Text=="173"&&status.Text==LocalizationService.Get("Editor.PatrolDrawHint"),"Translation canceled quick pick or lost draft/status.");LocalizationService.SetLanguage(language);workspace.RefreshLanguage();
            canvas.CompleteRegionPick(new(3,3),new(3,3));Exact(before);IDs(1);
            Click(panel,"Editor.PatrolDrawContinue");Accepted(()=>canvas.CompleteRegionPick(new(12,12),new(12,12)));IDs(1,4,3);
            Expect(routes.Items.Count==1,"Quick join retained raw-head duplicates instead of source grouping.");
            before=Snapshot();Click(panel,"Editor.PatrolDrawContinue");canvas.CompleteRegionPick(new(6,6),new(6,6));
            Expect(status.Text==LocalizationService.Get("Editor.PatrolFailed")&&status.ToolTip is string,"Linked quick endpoint did not fail with localized status.");Exact(before);
            tabs.SelectedIndex=3;
            var toggle=Control<CheckBox>(workspace,"PatrolToggle");toggle.IsChecked=false;toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(!canvas.ShowPatrolPaths,"Patrol display toggle failed.");toggle.IsChecked=true;toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Exact(before);
            canvas.Measure(new Size(240,240));canvas.Arrange(new Rect(0,0,240,240));var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(240,240,96,96,System.Windows.Media.PixelFormats.Pbgra32);bitmap.Render(canvas);Exact(before);
            Expect(ReferenceEquals(buffer,workspace.BufferedSelection)&&SelectionFile.Serialize(buffer!).AsSpan().SequenceEqual(copy)&&canvas.Selection==selection,"Patrol edits changed copy buffer or selection.");
            ReadOnly(()=>Click(panel,"Editor.PatrolReload"),keepLists:false);IDs(1,4,3);
            ReadOnly(()=>points.SelectedIndex=-1);
            Expect(!Control<Button>(workspace,"PatrolPauseButton").IsEnabled&&!Control<Button>(workspace,"PatrolInsertButton").IsEnabled&&!Control<Button>(workspace,"PatrolRemoveButton").IsEnabled,"Root selection granted point command authority.");
            Accepted(()=>PatrolModal(()=>Command("PatrolClearButton"),Removal()));Expect(document.PatrolAreas().All(point=>point.NextID==0),"Clear lost source final-edge semantics.");
            Click(panel,"Editor.PatrolDrawStart");Expect(workspace.CancelRegionEditing()&&!canvas.IsPickingRegion,"Cancel leaked quick picking.");
            Click(panel,"Editor.PatrolDrawStart");workspace.Document=null;
            Expect(!canvas.IsPickingRegion&&routes.Items.Count==0&&points.Items.Count==0&&picker.Items.Count==0&&!panel.IsEnabled&&status.Text.Length==0&&ReferenceEquals(buffer,workspace.BufferedSelection),"Closed document leaked manager/request/picker state.");
            foreach(var requestKind in new[]{"selector","pause","removal"})
            {
                workspace.Document=document;tabs.SelectedIndex=3;Candidate(1);wait.Text="15";
                Accepted(()=>Command("PatrolStartButton"));points.SelectedIndex=0;
                var step=requestKind switch
                {
                    "selector"=>PatrolStep<PatrolAreaSelectionWindow>(window=>
                    {workspace.Document=null;Expect(!window.TryAccept(),"Closed selector accepted stale authority.");}),
                    "pause"=>PatrolStep<PatrolPauseWindow>(window=>
                    {PatrolControl<TextBox>(window,"PatrolPauseText").Text="23";workspace.Document=null;Expect(!window.TryAccept(),"Closed numeric request accepted stale authority.");}),
                    _=>PatrolStep<PatrolRemovalWindow>(window=>
                    {PatrolControl<CheckBox>(window,"PatrolRemovalRemember").IsChecked=true;workspace.Document=null;Expect(!window.Accept(),"Closed removal request accepted stale authority.");})
                };
                var command=requestKind=="selector"?"PatrolAppendButton":requestKind=="pause"?"PatrolPauseButton":"PatrolRemoveButton";
                ReadOnly(()=>PatrolModal(()=>Command(command),step),keepLists:false);
                Expect(workspace.Document is null&&routes.Items.Count==0&&points.Items.Count==0&&!panel.IsEnabled,"Modal owner close leaked local route state.");
            }
            Console.WriteLine($"PASS {language} {(document.IsLegacyProject?"project":"native")} current owned patrol selector/pause/warning/removal, private lists, loop/joins and quick routes");
        }
        finally{workspace.Document=null;LocalizationService.SetLanguage(language);}
    }
}
