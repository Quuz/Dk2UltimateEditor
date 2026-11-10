using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only: opening/rendering controls remains manual acceptance.
    private static void CheckMapPaletteImages(bool native,string language)
    {
        var document=NativeMapDocument.CreateNewMap(new("PaletteUI","PaletteUI",20,20,NativeFormat:native));
        var bytes=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var revision=document.EditRevision;var count=document.EditorChangeCount;
        var workspace=new EditorWorkspace{Document=document};
        var terrain=Control<ComboBox>(workspace,"TerrainPicker");var owner=Control<ComboBox>(workspace,"OwnerPicker");var variant=Control<ComboBox>(workspace,"VariantPicker");var tools=Control<ComboBox>(workspace,"ToolPicker");var templates=Control<ComboBox>(workspace,"TemplatePicker");
        BitmapSource? Icon(object row)=>(BitmapSource?)row.GetType().GetProperty("Icon")!.GetValue(row);
        void Exact()=>Expect(bytes.All(data=>data.Key.Data.Span.SequenceEqual(data.Value))&&document.EditRevision==revision&&document.EditorChangeCount==count,"Palette browsing changed map/history.");
        try
        {
            Expect(terrain.ItemTemplate is not null&&tools.ItemTemplate is not null&&templates.ItemTemplate is not null&&string.IsNullOrEmpty(terrain.DisplayMemberPath)&&string.IsNullOrEmpty(tools.DisplayMemberPath)&&string.IsNullOrEmpty(templates.DisplayMemberPath),"Palette template conflicts with text display path.");
            Expect(terrain.Items.Count==41&&tools.Items.Count==10,"Image support changed modern choice counts.");
            foreach(var row in tools.Items.Cast<object>())
            {
                var tool=(DrawingTool)Get<int>(row,"ID");var bitmap=Icon(row);var resource=MapPaletteImageResources.ForDrawingTool(tool.ToString());
                Expect(resource is null?bitmap is null:bitmap is {PixelWidth:20,PixelHeight:20,IsFrozen:true},"Tool image size/fallback differs from source.");
            }
            foreach(var row in templates.Items.Cast<object>())
            {
                var template=(RecordTemplate?)row.GetType().GetProperty("Template")!.GetValue(row);var kind=template?.Structure??(string?)row.GetType().GetProperty("LegacyKind")!.GetValue(row);
                Expect(MapPaletteImageResources.ForTemplate(kind) is null?Icon(row) is null:Icon(row) is {PixelWidth:20,PixelHeight:20,IsFrozen:true},"MiscTools template image/size incorrect.");
            }
            tools.SelectedValue=(int)DrawingTool.Rectangle;terrain.SelectedValue=9;owner.SelectedValue=3;variant.SelectedIndex=0;
            var wall=terrain.Items.Cast<object>().Single(row=>Get<int>(row,"ID")==9);var notifications=0;
            ((INotifyPropertyChanged)wall).PropertyChanged+=(_,args)=>{if(args.PropertyName=="Icon")notifications++;};
            var old=Icon(wall);owner.SelectedValue=4;Expect(notifications>0&&!ReferenceEquals(old,Icon(wall)),"Owner change did not notify/rebind wall image.");
            Expect(ReferenceEquals(Icon(wall),OriginalScenarioImages.For(MapPaletteImageResources.ForTerrain(9,4,1))),"Wall icon uses stale player ID.");
            variant.SelectedIndex=1;Expect(Icon(wall) is null,"Unavailable source variant got a guessed image.");variant.SelectedIndex=0;
            LocalizationService.SetLanguage(language=="en"?"ru":"en");workspace.RefreshLanguage();
            Expect((int)tools.SelectedValue==(int)DrawingTool.Rectangle&&(int)terrain.SelectedValue==9&&(int)owner.SelectedValue==4&&variant.SelectedIndex==0,"Palette translation changed tool/terrain/owner/variant choices.");Exact();
            LocalizationService.SetLanguage(language);workspace.RefreshLanguage();
        }
        finally{workspace.Document=null;}
        Exact();
    }
}
