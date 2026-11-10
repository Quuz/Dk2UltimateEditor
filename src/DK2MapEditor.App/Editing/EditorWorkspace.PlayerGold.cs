using System.Globalization;
using System.Windows;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private readonly EditorPlayerGoldPolicy playerGoldPolicy;
    private PlayerGoldNoticeWindow? playerGoldWindow;
    private long playerGoldContext;
    private bool playerGoldPending;
    private NativeMapDocument? playerGoldCapacitySource;
    private Func<bool>? playerGoldHostCurrent;
    public Action<PlayerGoldNoticeWindow>? PlayerGoldPresenter {get;set;}
    private void ClosePlayerGoldWindow(){var window=playerGoldWindow;playerGoldWindow=null;window?.Close();}
    private void InvalidatePlayerGoldNotice(){playerGoldContext++;ClosePlayerGoldWindow();}
    internal void ConfigurePlayerGoldContext(NativeMapDocument? capacitySource,Func<bool>? hostCurrent)
    {InvalidatePlayerGoldNotice();playerGoldCapacitySource=capacitySource;playerGoldHostCurrent=hostCurrent;}
    private bool ReadPlayerChange(uint id,out PlayerSettingsChange change)
    {
        change=null!;
        if(PlayerPicker.SelectedItem is not PlayerView selected||selected.Player.ID!=id
            ||!int.TryParse(PlayerGold.Text,NumberStyles.Integer,CultureInfo.InvariantCulture,out var gold))return false;
        int? health=null;
        if(selected.Player.Health is not null)
        {if(!int.TryParse(PlayerHealth.Text,NumberStyles.Integer,CultureInfo.InvariantCulture,out var parsed))return false;health=parsed;}
        change=new(id,PlayerName.Text,gold,PlayerComputer.IsChecked==true,health);return true;
    }
    private bool ApplyPlayerGeneralWithNotice(uint id,out string? committedWarning)
    {
        committedWarning=null;
        if(document is not { } owner||playerGoldPending)return false;
        if(PlayerPicker.SelectedItem is not PlayerView selected||selected.Player.ID!=id){PlayerMessage("PlayerGold.Changed");return false;}
        if(!ReadPlayerChange(id,out var change))throw new FormatException("Enter valid player resource values.");
        var plan=owner.PrepareEditorPlayerSettings(change,playerGoldCapacitySource);var context=playerGoldContext;playerGoldPending=true;
        bool Current()=>ReferenceEquals(document,owner)&&context==playerGoldContext&&(playerGoldHostCurrent?.Invoke()??true)&&ReadPlayerChange(id,out var current)&&current==change&&plan.IsCurrent;
        try
        {
            using var notice=playerGoldPolicy.Begin(plan,Current);
            if(notice is not null)
            {
                if(!notice.RequiresNotice)notice.Acknowledge();
                else
                {
                    try
                    {
                        using var window=new PlayerGoldNoticeWindow(notice){Owner=Window.GetWindow(this)};playerGoldWindow=window;
                        try{if(PlayerGoldPresenter is { } presenter)presenter(window);else window.ShowDialog();}
                        finally{if(ReferenceEquals(playerGoldWindow,window))playerGoldWindow=null;}
                        if(window.Failure is { } failure)committedWarning=failure.Message;
                    }
                    catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.IOException or System.IO.InvalidDataException or FormatException or OverflowException)
                    {committedWarning=error.Message;}
                }
            }
            notice?.Dispose();
            // Source ignores the information box's return. Closing it does not
            // cancel Save; changed settings/documents still reject the old plan.
            if(!Current()){PlayerMessage("PlayerGold.Changed");return false;}
            try{owner.ApplyEditorPlayerSettings(plan);}
            catch(Exception error)when(plan.IsApplied){committedWarning=error.Message;}
            return true;
        }
        finally{playerGoldPending=false;}
    }
    private void PlayerGoldReset_Click(object sender,RoutedEventArgs args)
    {
        try{ClosePlayerGoldWindow();playerGoldPolicy.ResetSuppression();}
        catch(InvalidOperationException error){PlayerMessage("Editor.PlayerFailed",error.Message);}
    }
}
