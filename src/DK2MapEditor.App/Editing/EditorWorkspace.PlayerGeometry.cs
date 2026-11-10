using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private sealed record PendingPlayerGeometry(NativeMapDocument Document, uint ID, bool Camera, byte[] Payload);
    private PendingPlayerGeometry? pendingPlayerGeometry;
    private uint? playerGeometryDraftID;

    private void InitializePlayerGeometry()
    {
        Canvas.RegionPicked += (_, args) =>
        {
            var pending = pendingPlayerGeometry; pendingPlayerGeometry = null;
            if (pending is null || document != pending.Document) return;
            ApplyPlayerGeometry(() =>
            {
                if (pending.Camera) document.SetPlayerCamera(pending.ID, new(args.Bounds.X, args.Bounds.Y), pending.Payload);
                else document.SetPlayerBuildArea(pending.ID, args.Bounds, pending.Payload);
            });
            ShowRegionStatus(playerStatusKey ?? "Editor.PlayerApplied"); WorkspaceTabs.SelectedIndex = 7;
        };
        Canvas.RegionPickCanceled += (_, _) =>
        {
            if (pendingPlayerGeometry is null) return;
            pendingPlayerGeometry = null; PlayerMessage("Editor.RegionCanceled"); ShowRegionStatus("Editor.RegionCanceled");
        };
    }
    private void CancelPlayerGeometryPick()
    { if (pendingPlayerGeometry is not null) Canvas.CancelRegionPick(); pendingPlayerGeometry = null; }

    private void RefreshPlayerGeometry(PlayerSettingsRow? player)
    {
        var row = player is null ? null : document?.PlayerGeometries().SingleOrDefault(row => row.ID == player.ID && row.Record.Offset == player.Record.Offset);
        PlayerCameraControls.IsEnabled = row?.CameraEditable == true; PlayerBuildControls.IsEnabled = row?.BuildAreaEditable == true;
        if (row is null || playerGeometryDraftID != row.ID)
        {
            playerGeometryDraftID = row?.ID;
            void Text(TextBox control, int? value) => control.Text = value?.ToString(CultureInfo.InvariantCulture) ?? "";
            Text(PlayerCameraX, row?.EffectiveCamera.X); Text(PlayerCameraY, row?.EffectiveCamera.Y);
            Text(PlayerBuildX1, row?.EffectiveBuildStart.X); Text(PlayerBuildY1, row?.EffectiveBuildStart.Y);
            Text(PlayerBuildX2, row?.EffectiveBuildEnd.X); Text(PlayerBuildY2, row?.EffectiveBuildEnd.Y);
        }
        var selectedArea = (PlayerBuildAreaPicker.SelectedItem as RegionView)?.Region.Record;
        var areas = RegionPicker.Items.Cast<RegionView>().Where(view => view.Region.Kind == MapRegionKind.Area).ToArray();
        PlayerBuildAreaPicker.ItemsSource = areas;
        PlayerBuildAreaPicker.SelectedItem = areas.FirstOrDefault(view => view.Region.Record.File == selectedArea?.File && view.Region.Record.Offset == selectedArea.Offset);
        if (PlayerBuildAreaPicker.SelectedIndex < 0 && areas.Length > 0) PlayerBuildAreaPicker.SelectedIndex = 0;
        string Default(bool automatic) => automatic ? " · " + LocalizationService.Get("Editor.PlayerCoordinateDefault") : "";
        PlayerGeometryStored.Text = row is null ? "" : $"{LocalizationService.Get("Editor.PlayerCamera")}: ({row.Camera.X}, {row.Camera.Y}){Default(row.DefaultCamera)} → ({row.EffectiveCamera.X}, {row.EffectiveCamera.Y}) · {LocalizationService.Get("Editor.PlayerBuildArea")}: ({row.BuildStart.X}, {row.BuildStart.Y})–({row.BuildEnd.X}, {row.BuildEnd.Y}){Default(row.DefaultBuildArea)}";
    }
    private void ApplyPlayerGeometry(Action operation)
    {
        try
        {
            operation(); playerGeometryDraftID = null; RefreshPlayers(); PlayerMessage("Editor.PlayerApplied");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or OverflowException or FormatException)
        { PlayerMessage("Editor.PlayerFailed", exception.Message); }
    }
    private void PlayerGeometryEdit_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || PlayerPicker.SelectedItem is not PlayerView selected || !CommitFields()) return;
        CancelRegionEditing();
        ApplyPlayerGeometry(() =>
        {
            int Number(TextBox field) => int.Parse(field.Text, NumberStyles.Integer, CultureInfo.InvariantCulture);
            var action = (sender as Button)?.Tag as string;
            if (action == "CameraDefault") document.SetPlayerCamera(selected.Player.ID, null);
            else if (action == "Camera") document.SetPlayerCamera(selected.Player.ID, new(Number(PlayerCameraX), Number(PlayerCameraY)));
            else if (action == "BuildDefault") document.SetPlayerBuildArea(selected.Player.ID, null);
            else
            {
                var bounds = action switch
                {
                    "Selection" => Canvas.Selection ?? throw new ArgumentException("Select a rectangle on the map first."),
                    "Area" => (PlayerBuildAreaPicker.SelectedItem as RegionView)?.Region.Bounds ?? throw new ArgumentException("Select an existing map area first."),
                    _ => ReadBounds()
                };
                document.SetPlayerBuildArea(selected.Player.ID, bounds);
            }
            TileRect ReadBounds()
            {
                var x = Number(PlayerBuildX1); var y = Number(PlayerBuildY1);
                var endX = Number(PlayerBuildX2); var endY = Number(PlayerBuildY2);
                // Inclusive endpoints; do not silently swap or truncate invalid input.
                if (endX < x || endY < y) throw new ArgumentException("Build-area endpoints must be ordered.");
                return new(x, y, checked(endX - x + 1), checked(endY - y + 1));
            }
        });
    }
    private void PlayerGeometryPick_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || PlayerPicker.SelectedItem is not PlayerView selected || !CommitFields()) return;
        var camera = (sender as Button)?.Tag as string == "Camera";
        var row = document.PlayerGeometries().SingleOrDefault(row => row.ID == selected.Player.ID);
        if (row is null || !(camera ? row.CameraEditable : row.BuildAreaEditable)) return;
        CancelRegionEditing(); Canvas.BeginRegionPick(camera);
        pendingPlayerGeometry = new(document, row.ID, camera, row.Record.File.Data.Slice(row.Record.Offset, row.Record.Length).ToArray());
        PlayerMessage(camera ? "Editor.PlayerCameraPickHint" : "Editor.PlayerBuildPickHint");
        WorkspaceTabs.SelectedIndex = 0; ShowRegionStatus(playerStatusKey!);
    }
}
