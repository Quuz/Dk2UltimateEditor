using System.Windows;
using System.IO;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private HeroBandWindow? heroBandWindow;
    public Action<HeroBandWindow>? HeroBandPresenter { get; set; }
    public Func<NativeRecord,bool>? HeroBandDeletionConfirmation { get; set; }
    private void CloseHeroBandWindow(){heroBandWindow?.Close();heroBandWindow=null;}
    private IReadOnlyList<HeroBandDisplayImageRow> HeroBandPreview(NativeRecord record)
    {
        try{return document!.ReadHeroBandDisplay(record).Select(row=>HeroBandDisplayImages.For(row.Index,row.Values,LocalizationService.CurrentLanguage,key=>LocalizationService.Get(key))).ToArray();}
        catch(Exception error)when(error is ArgumentException or InvalidDataException or InvalidOperationException or FormatException or OverflowException)
        {return [];}
    }
    private void HeroBandModify_Click(object sender,RoutedEventArgs e)
    {
        if(RecordList.SelectedItem is RecordView view && NativeMapDocument.IsBand(view.Record))OpenHeroBand(view.Record);
    }
    private void DeleteHeroBand(NativeRecord record)
    {
        if(document is null)return;
        var accepted=HeroBandDeletionConfirmation?.Invoke(record)??DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this),LocalizationService.Get("HeroBand.DeleteConfirm"),
            LocalizationService.Get("HeroBand.Modify"),MessageBoxButton.YesNo,MessageBoxImage.Warning)==MessageBoxResult.Yes;
        if(!accepted)return;
        RecordOperation(()=>{document.DeleteEditorHeroBand(record);return null;});
    }
    private void OpenHeroBand(NativeRecord? record=null)
    {
        if(document is null || !CommitFields())return;
        CloseHeroBandWindow();var context=document;
        RecordOperation(()=>
        {
            var window=new HeroBandWindow(context,record,()=>ReferenceEquals(document,context)&&IsLoaded,()=>CommitFields()){Owner=Window.GetWindow(this)};
            heroBandWindow=window;
            window.Closed+=(_,_)=>{if(ReferenceEquals(heroBandWindow,window))heroBandWindow=null;};
            if(HeroBandPresenter is { } presenter)presenter(window);else window.ShowDialog();
            return window.SavedRecord;
        });
    }
}
