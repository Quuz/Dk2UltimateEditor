using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared integration cases only: the agent does not run a WPF process.
    private static void CheckRegionCoordinates(bool native,string language)
    {
        foreach(var kind in Enum.GetValues<MapRegionKind>())foreach(var add in new[]{false,true})
        {
            var doc=NativeMapDocument.CreateNewMap(new("CoordinateUI","Coordinate UI",24,16,NativeFormat:native));
            var region=doc.AddMapRegion(kind,new(3,4,1,1));var current=true;
            var bytes=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
            void Exact()=>Expect(bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history,"Coordinate UI draft changed owner before acceptance or after Cancel/Undo.");
            RegionPropertiesWindow Open()=>add?new(doc,doc.CreateRegionCreationSession(kind,new(3,4,1,1)),()=>current,()=>true,()=>null)
                :new(doc,region,()=>current,()=>true,()=>null);
            TextBox Field(RegionPropertiesWindow w,string name)=>(TextBox)w.FindName("Region"+name);
            void Step(RegionPropertiesWindow w,string name,string direction)=>((RepeatButton)w.FindName("Region"+name+direction)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            var window=Open();
            try
            {
                var x=Field(window,"X1");var y=Field(window,"Y1");x.Text="999";y.Text="999";
                var invalid=new TextCompositionEventArgs(Keyboard.PrimaryDevice,new TextComposition(InputManager.Current,x,"x"))
                    {RoutedEvent=TextCompositionManager.PreviewTextInputEvent};x.RaiseEvent(invalid);
                Expect(invalid.Handled,"Source ES_NUMBER typing filter was not connected.");
                Expect(x.Text=="23"&&y.Text=="15","Immediate clamp used the wrong field/extents.");
                if(kind==MapRegionKind.Gate)
                    Expect(Field(window,"X2").Text=="23"&&Field(window,"Y2").Text=="15"&&!Field(window,"X2").IsEnabled
                        &&!((RepeatButton)window.FindName("RegionX2Increase")).IsEnabled,"Gate endpoints or disabled spinner did not follow source coordinates.");
                else
                {
                    Field(window,"X2").Text="999";Field(window,"Y2").Text="999";
                    Expect(Field(window,"X2").Text=="23"&&Field(window,"Y2").Text=="15","End coordinate clamp was not connected.");
                }
                Step(window,"X1","Increase");Expect(x.Text=="23","Up wrapped the right map border.");
                Step(window,"X1","Decrease");Expect(x.Text=="22","Down did not decrement one unit.");
                x.Text="0";Step(window,"X1","Decrease");Expect(x.Text=="0","Down crossed zero.");
                x.Text="";Expect(x.Text=="","Typing callback destroyed an intermediate empty field.");Step(window,"X1","Increase");Expect(x.Text=="1","Step from empty was not source zero + 1.");
                x.Text="9";y.Text="8";
                if(kind!=MapRegionKind.Gate){Field(window,"X2").Text="9";Field(window,"Y2").Text="8";}
                LocalizationService.SetLanguage(language=="en"?"ru":"en");
                Expect(x.Text=="9"&&y.Text=="8"&&window.IsDraftCurrent,"Language changed coordinate draft.");
                Expect(((RepeatButton)window.FindName("RegionX1Increase")).ToolTip.ToString()==LocalizationService.Get("RegionProperties.Increase"),"Spinner tooltip did not translate.");
                LocalizationService.SetLanguage(language);Exact();window.Close();Exact();
            }
            finally{window.Close();}
            window=Open();
            try
            {
                Field(window,"X1").Text="999";Field(window,"Y1").Text="999";
                if(kind!=MapRegionKind.Gate){Field(window,"X2").Text="999";Field(window,"Y2").Text="999";}
                Exact();Expect(window.TryApply(),"Normalized UI fields did not apply.");
                Expect(doc.MapRegions().Count(r=>r.Kind==kind&&r.Bounds==new TileRect(23,15,1,1))==1
                    &&doc.EditorChangeCount==unchecked(count+1)&&doc.GetHistory().Undo.Count==history+1,"Normalized UI Apply changed another region or split Undo.");
            }
            finally{window.Close();}
            doc.Undo();Exact();
            window=Open();
            try
            {
                current=false;Step(window,"X1","Increase");Expect(Field(window,"X1").Text=="3"&&!window.TryApply(),"Stale owner retained coordinate command authority.");Exact();
            }
            finally{window.Close();}
        }
    }
}
