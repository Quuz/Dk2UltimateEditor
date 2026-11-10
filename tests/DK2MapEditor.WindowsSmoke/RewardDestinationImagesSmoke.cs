using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Compiled preparation only. Execution requires permission to instantiate WPF.
    private static void CheckRewardDestinationImages(bool native,string language)
    {
        var document=NativeMapDocument.CreateNewMap(new("RewardUI","RewardUI",20,20,NativeFormat:native));
        foreach(var kind in new[]{"Trigger","Action"})
            if(native)document.AddRecord(RecordTemplate.All.Single(t=>t.Structure==kind),new(0,0));else document.AddLegacyRecord(kind,new(0,0));
        var bytes=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var state=(document.EditRevision,document.EditorChangeCount,document.CanUndo,document.CanRedo);
        var window=new MapLevelOptionsWindow(document,()=>true,()=>true);var workspace=new EditorWorkspace{Document=document};
        try
        {
            foreach(var field in new[]{"RewardPrev1","RewardPrev2","RewardPrev3","RewardNext1","RewardNext2","RewardNext3"})
            {
                var combo=(ComboBox)window.FindName("LevelOption"+field);Expect(combo.Items.Count==49&&combo.ItemTemplate is not null,"Reward choices/template missing.");
                for(var index=0;index<combo.Items.Count;index++)
                {
                    var row=combo.Items[index];var resource=(int?)row.GetType().GetProperty("ImageResource")!.GetValue(row);
                    Expect(resource==MapLevelOptions.Rewards[index].Icon,"Reward image changed item identity/order.");
                }
            }
            var type=typeof(MapLevelOptionsWindow).Assembly.GetType("DK2MapEditor.App.Editing.FontSizedRewardImage",true)!;
            var glyph=(Image)Activator.CreateInstance(type)!;var resourceProperty=type.GetProperty("ImageResource")!;
            glyph.SetValue(System.Windows.Documents.TextElement.FontFamilyProperty,new FontFamily("Segoe UI"));glyph.SetValue(System.Windows.Documents.TextElement.FontSizeProperty,13d);resourceProperty.SetValue(glyph,401);
            glyph.Measure(new(double.PositiveInfinity,double.PositiveInfinity));var first=glyph.DesiredSize;var bitmap=(BitmapSource)glyph.Source;
            Expect(bitmap.IsFrozen&&bitmap is not TransformedBitmap,"Reward bitmap resized before font-relative drawing.");
            glyph.SetValue(System.Windows.Documents.TextElement.FontSizeProperty,26d);glyph.Measure(new(double.PositiveInfinity,double.PositiveInfinity));Expect(glyph.DesiredSize.Width>first.Width,"Reward ignores inherited font size.");
            var full=glyph.DesiredSize;resourceProperty.SetValue(glyph,null);glyph.Measure(new(double.PositiveInfinity,double.PositiveInfinity));Expect(glyph.Source is null&&glyph.DesiredSize==full,"None reward collapsed source indentation.");
            resourceProperty.SetValue(glyph,401);Expect(ReferenceEquals(bitmap,glyph.Source),"Original reward bitmap cache not reused.");
            foreach(var name in new[]{"ScriptOwnerPicker","ScriptMovePicker","ScriptTargetPicker"})
            {
                var combo=Control<ComboBox>(workspace,name);Expect(combo.ItemTemplate is not null&&string.IsNullOrEmpty(combo.DisplayMemberPath),"Scenario list image template hidden by display path.");
                foreach(var row in combo.Items.Cast<object>())
                {
                    var image=(BitmapSource?)row.GetType().GetProperty("Icon")!.GetValue(row);
                    Expect(image is null||image is{PixelWidth:16,PixelHeight:16,IsFrozen:true},"Destination image not source TrImages16.");
                }
            }
            LocalizationService.SetLanguage(language=="en"?"ru":"en");workspace.RefreshLanguage();
            Expect(!window.HasPendingInputs,"Image/language refresh modified reward draft.");
            LocalizationService.SetLanguage(language);workspace.RefreshLanguage();
        }
        finally{window.Close();workspace.Document=null;}
        Expect(bytes.All(data=>data.Key.Data.Span.SequenceEqual(data.Value))&&state==(document.EditRevision,document.EditorChangeCount,document.CanUndo,document.CanRedo),"Image browsing changed map/history.");
    }
}
