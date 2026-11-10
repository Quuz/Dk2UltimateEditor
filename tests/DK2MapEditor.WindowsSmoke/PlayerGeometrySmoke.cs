using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static partial class Program
{
    private static void CheckPlayerGeometry(NativeMapDocument document, string language, string directory)
    {
        var workspace = new EditorWorkspace { Document = document }; var tabs = Control<TabControl>(workspace, "WorkspaceTabs"); tabs.SelectedIndex = 7;
        void Layout() { workspace.Measure(new Size(1600, 1050)); workspace.Arrange(new Rect(0, 0, 1600, 1050)); workspace.UpdateLayout(); }
        var picker = Control<ComboBox>(workspace, "PlayerPicker"); var camera = Control<StackPanel>(workspace, "PlayerCameraControls"); var build = Control<StackPanel>(workspace, "PlayerBuildControls");
        var x = Control<TextBox>(workspace, "PlayerCameraX"); var y = Control<TextBox>(workspace, "PlayerCameraY");
        var x1 = Control<TextBox>(workspace, "PlayerBuildX1"); var y1 = Control<TextBox>(workspace, "PlayerBuildY1"); var x2 = Control<TextBox>(workspace, "PlayerBuildX2"); var y2 = Control<TextBox>(workspace, "PlayerBuildY2");
        var status = Control<TextBlock>(workspace, "PlayerStatus"); var stored = Control<TextBlock>(workspace, "PlayerGeometryStored"); var canvas = Control<MapCanvas>(workspace, "Canvas");
        void Select(uint id) { picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<PlayerSettingsRow>(item, "Player").ID == id); Layout(); }
        PlayerGeometry Row() => document.PlayerGeometries().Single(row => row.ID == 3);
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> expected) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(expected[file]), "Player coordinate UI changed unexpected bytes."); }
        Select(3); var before = Snapshot(); Expect(camera.IsEnabled && build.IsEnabled && x1.Text == "2" && y1.Text == "2" && x2.Text == "36" && y2.Text == "18", "Player coordinate/default drafts missing."); Exact(before);
        workspace.SetSelection(new(2, 3, 4, 5)); workspace.LoadSelectionIntoBuffer(document.CopySelection(new(2, 3, 4, 5))); var buffer = workspace.BufferedSelection;
        x.Text = "37"; y.Text = "19"; Click(camera, "Editor.PlayerCoordinatesApply");
        Expect(Row().Camera == new TilePoint(37, 19) && status.Text == LocalizationService.Get("Editor.PlayerApplied"), "Actual camera apply button failed.");
        var after = Snapshot(); workspace.Undo(); Exact(before); workspace.Redo(); Exact(after);
        foreach (var (control, value) in new[] { (x, "38"), (y, "20"), (x, "-1"), (y, "invalid") })
        {
            var old = control.Text; control.Text = value; Click(camera, "Editor.PlayerCoordinatesApply"); Exact(after);
            Expect(status.Text == LocalizationService.Get("Editor.PlayerFailed"), "Bad camera coordinates did not show inline validation."); control.Text = old;
        }
        x1.Text = "7"; y1.Text = "8"; x2.Text = "10"; y2.Text = "12"; Click(build, "Editor.PlayerCoordinatesApply");
        Expect(Row().BuildStart == new TilePoint(7, 8) && Row().BuildEnd == new TilePoint(10, 12), "Inclusive numeric build endpoints failed."); after = Snapshot();
        foreach (var (control, value) in new[] { (x1, "11"), (y2, "7"), (x2, "38"), (x1, "2147483647"), (y1, "invalid") })
        {
            var old = control.Text; control.Text = value; Click(build, "Editor.PlayerCoordinatesApply"); Exact(after);
            Expect(status.Text == LocalizationService.Get("Editor.PlayerFailed"), "Bad build endpoints did not show inline validation."); control.Text = old;
        }
        var area = document.AddMapRegion(MapRegionKind.Area, new(4, 6, 5, 7));
        var areas = Control<ComboBox>(workspace, "PlayerBuildAreaPicker");
        areas.SelectedItem = areas.Items.Cast<object>().Single(item => Get<MapRegion>(item, "Region").Record.File == area.File && Get<MapRegion>(item, "Region").Record.Offset == area.Offset);
        Click(build, "Editor.PlayerBuildFromArea"); Expect(Row().BuildStart == new TilePoint(4, 6) && Row().BuildEnd == new TilePoint(8, 12), "Existing area was not used as original build bounds.");
        Click(build, "Editor.PlayerBuildFromSelection"); Expect(Row().BuildStart == new TilePoint(2, 3) && Row().BuildEnd == new TilePoint(5, 7), "Map selection was not used for build bounds.");
        before = Snapshot(); workspace.ClearSelection(); Click(build, "Editor.PlayerBuildFromSelection"); Exact(before); Expect(status.Text == LocalizationService.Get("Editor.PlayerFailed"), "Missing selection not rejected."); workspace.SetSelection(new(2, 3, 4, 5));
        var selection = canvas.Selection;
        Select(1); Expect(!camera.IsEnabled, "NPC camera editing enabled despite the source NPC guard."); Select(3);
        Click(camera, "Editor.PlayerCameraPick"); Expect(canvas.IsPickingRegion && tabs.SelectedIndex == 0 && Control<TextBlock>(workspace, "RegionStatus").Text == LocalizationService.Get("Editor.PlayerCameraPickHint"), "Camera picker did not reach actual canvas."); Exact(before);
        // The public completion method is exactly the mouse-release path used by the canvas.
        canvas.CompleteRegionPick(new(14, 15), new(19, 18)); Layout();
        Expect(!canvas.IsPickingRegion && tabs.SelectedIndex == 7 && Row().Camera == new TilePoint(14, 15) && x.Text == "14" && y.Text == "15", "Camera release used endpoint instead of selected point or lost owner.");
        Expect(canvas.Selection == selection && workspace.BufferedSelection == buffer, "Camera picking changed copy selection/buffer.");
        after = Snapshot(); workspace.Undo(); Exact(before); workspace.Redo(); Exact(after);
        Click(build, "Editor.PlayerBuildPick"); canvas.CompleteRegionPick(new(11, 10), new(8, 6)); Layout();
        Expect(Row().BuildStart == new TilePoint(8, 6) && Row().BuildEnd == new TilePoint(11, 10) && x1.Text == "8" && y2.Text == "10" && canvas.Selection == selection && workspace.BufferedSelection == buffer, "Reversed rectangle pick was not normalized or changed map tools.");
        before = Snapshot(); Click(camera, "Editor.PlayerCameraPick"); canvas.CancelRegionPick(); Exact(before); Expect(status.Text == LocalizationService.Get("Editor.RegionCanceled"), "Canceled geometry pick wrote data/kept authority."); tabs.SelectedIndex = 7;
        Click(build, "Editor.PlayerBuildPick"); Click(Control<StackPanel>(workspace, "RegionPanel"), "Editor.Cancel"); Exact(before); Expect(!canvas.IsPickingRegion, "Map Cancel button did not cancel player pick."); tabs.SelectedIndex = 7;
        Click(camera, "Editor.PlayerCameraPick"); Select(4); Exact(before); Expect(!canvas.IsPickingRegion, "Changing player retained previous picking owner."); Select(3); tabs.SelectedIndex = 7;
        Click(camera, "Editor.PlayerCameraPick"); x.Text = "2"; Exact(before); Expect(!canvas.IsPickingRegion, "Changed coordinate draft retained prior map-pick authority."); tabs.SelectedIndex = 7;
        Click(build, "Editor.PlayerBuildPick"); document.Paint([new(30, 15)], new(4, 2, 1)); Expect(!canvas.IsPickingRegion, "External edit retained stale player pick."); document.Undo(); Exact(before); tabs.SelectedIndex = 7;
        x.Text = "9"; y.Text = "10"; x1.Text = "11"; y1.Text = "12"; x2.Text = "13"; y2.Text = "14";
        var label = stored.Text; LocalizationService.SetLanguage(language == "ru" ? "en" : "ru"); workspace.RefreshLanguage(); Layout();
        Expect(x.Text == "9" && y.Text == "10" && x1.Text == "11" && y2.Text == "14" && stored.Text != label && Get<PlayerSettingsRow>(picker.SelectedItem, "Player").ID == 3, "Translation lost coordinate drafts or owner."); Exact(before);
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Layout();
        Click(camera, "Editor.PlayerCameraPick"); LocalizationService.SetLanguage(language == "ru" ? "en" : "ru"); workspace.RefreshLanguage();
        Expect(canvas.IsPickingRegion && Control<TextBlock>(workspace, "RegionStatus").Text == LocalizationService.Get("Editor.PlayerCameraPickHint"), "Live translation canceled camera pick or left old hint."); canvas.CancelRegionPick();
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); tabs.SelectedIndex = 7;
        Click(camera, "Editor.PlayerCameraDefault"); Click(build, "Editor.PlayerBuildDefault");
        var row = Row(); Expect(row.DefaultCamera == document.IsLegacyProject && row.DefaultBuildArea == document.IsLegacyProject && row.EffectiveBuildStart == new TilePoint(2, 2) && row.EffectiveBuildEnd == new TilePoint(36, 18)
            && x.Text == row.EffectiveCamera.X.ToString() && y.Text == row.EffectiveCamera.Y.ToString() && x1.Text == "2", "Default buttons/drafts did not resolve native versus project coordinates.");
        if (Environment.GetEnvironmentVariable("DK2_PLAYER_UI_ARTIFACTS") is { Length: > 0 } artifactDirectory)
        {
            Layout(); Directory.CreateDirectory(artifactDirectory); var bitmap = new RenderTargetBitmap(1600, 1050, 96, 96, PixelFormats.Pbgra32); bitmap.Render(workspace);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var stream = File.Create(Path.Combine(artifactDirectory, $"geometry-{language}-{(document.IsLegacyProject ? "project" : "native")}.png")); encoder.Save(stream);
        }
        if (document.IsLegacyProject)
        {
            var path = document.SaveCopy(Path.Combine(directory, "geometry-ui-" + language)); var sections = LegacySectionFile.Read(path); var original = sections.Sections.Single(section => section.Signature == NativeMapDocument.LegacyPlayers);
            foreach (var version in new[] { 1, 2 })
            {
                var payload = version == 1 ? new byte[7 * LegacyProjectSchemas.Size("MAPOPTIONSV1")] : original.Payload.ToArray(); var target = Path.Combine(directory, $"geometry-ui-v{version}-{language}.dkm");
                File.WriteAllBytes(target, LegacySectionFile.Create(sections.Sections.Select(section => section.Signature == original.Signature ? new LegacySection(original.Signature, version, payload) : section)).ToArray());
                workspace.Document = NativeMapDocument.Load(target); Select(3); Expect(!camera.IsEnabled && build.IsEnabled, "Historical ignored camera enabled or verified build area disabled.");
                x1.Text = "1"; y1.Text = "2"; x2.Text = "3"; y2.Text = "4"; Click(build, "Editor.PlayerCoordinatesApply");
                Expect(workspace.Document.PlayerGeometries().Single(row => row.ID == 3).BuildEnd == new TilePoint(3, 4), "Historical build coordinates failed.");
            }
        }
        else { Click(camera, "Editor.PlayerCameraPick"); }
        workspace.Document = null; Layout(); Expect(!canvas.IsPickingRegion && !camera.IsEnabled && !build.IsEnabled && x.Text.Length == 0 && x1.Text.Length == 0 && stored.Text.Length == 0 && areas.Items.Count == 0, "Closed document retained coordinate authority/drafts.");
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} player camera/build numeric, canvas, existing area, defaults and translation");
    }
}
