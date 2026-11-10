using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace DK2MapEditor.App.Editing;

internal sealed class ResizeAnchorImages:Canvas
{
    private (int Anchor,bool Width,bool Height)? previous;
    internal ResizeAnchorImages(){IsHitTestVisible=false;Focusable=false;}
    internal void Refresh(int anchor,bool shrinkWidth,bool shrinkHeight)
    {
        var state=(anchor,shrinkWidth,shrinkHeight);if(previous==state)return;previous=state;Children.Clear();
        foreach(var placement in ResizeAnchorImageLayout.Create(anchor,shrinkWidth,shrinkHeight))
        {
            var image=new Image{Source=OriginalControlIcons.For(placement.Resource),Width=16,Height=16};
            SetLeft(image,placement.Column*24-8);SetTop(image,placement.Row*24-8);Children.Add(image);
        }
    }
    internal static Style ButtonStyle()
    {
        var border=new FrameworkElementFactory(typeof(Border));border.Name="Frame";
        border.SetResourceReference(Border.BackgroundProperty,"Brush.PanelRaised");border.SetResourceReference(Border.BorderBrushProperty,"Brush.Border");border.SetValue(Border.BorderThicknessProperty,new Thickness(1));
        var template=new ControlTemplate(typeof(RadioButton)){VisualTree=border};
        var selected=new Trigger{Property=ToggleButton.IsCheckedProperty,Value=true};
        selected.Setters.Add(new Setter(Border.BackgroundProperty,new DynamicResourceExtension("Brush.Panel"),"Frame"));selected.Setters.Add(new Setter(Border.BorderBrushProperty,new DynamicResourceExtension("Brush.GoldBright"),"Frame"));selected.Setters.Add(new Setter(Border.BorderThicknessProperty,new Thickness(2),"Frame"));template.Triggers.Add(selected);
        var focus=new Trigger{Property=IsKeyboardFocusedProperty,Value=true};focus.Setters.Add(new Setter(Border.BorderBrushProperty,new DynamicResourceExtension("Brush.GoldBright"),"Frame"));template.Triggers.Add(focus);
        return new Style(typeof(RadioButton)){Setters={new Setter(Control.TemplateProperty,template)}};
    }
}
