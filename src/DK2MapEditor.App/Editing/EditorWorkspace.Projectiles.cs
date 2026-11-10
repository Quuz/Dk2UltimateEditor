using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private GlobalProjectileState? projectileState;
    private static string ProjectileText(string key) => DefinitionText("Projectile." + key);
    private static DefinitionChoice ProjectileChoice(ValueChoice choice) => new(choice.Value, "#" + uint.Parse(choice.Value, CultureInfo.InvariantCulture).ToString("X2") + " · " + choice.Name(Localization.LocalizationService.CurrentLanguage));
    private static DefinitionChoice UnknownProjectileChoice(string id) => new(id, "#" + id + " · " + ProjectileText("Unknown"));
    private void RefreshProjectiles()
    {
        if (DefinitionProjectilePanel is null) return;
        var row = (DefinitionPicker.SelectedItem as DefinitionView)?.Row;
        var slots = row is null ? [] : GlobalProjectiles.Slots(row.Family);
        DefinitionProjectilePanel.Visibility = slots.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        if (slots.Count == 0)
        {
            projectileState = null; DefinitionProjectileSlot.ItemsSource = DefinitionProjectileType.ItemsSource = DefinitionProjectileTarget.ItemsSource = DefinitionProjectileLevel.ItemsSource = null;
            DefinitionProjectileData1.Text = DefinitionProjectileData2.Text = DefinitionProjectileHint.Text = ""; DefinitionProjectileApply.IsEnabled = false;
            return;
        }
        var slot = (DefinitionProjectileSlot.SelectedItem as DefinitionChoice)?.ID ?? "Shot";
        var selectedType = (DefinitionProjectileType.SelectedItem as DefinitionChoice)?.ID;
        var first = DefinitionProjectileData1.Text; var second = DefinitionProjectileData2.Text;
        DefinitionProjectileSlot.ItemsSource = slots.Select(id => new DefinitionChoice(id, ProjectileText(id))).ToArray();
        DefinitionProjectileSlot.SelectedItem = DefinitionProjectileSlot.Items.Cast<DefinitionChoice>().FirstOrDefault(item => item.ID == slot) ?? DefinitionProjectileSlot.Items[0];
        slot = ((DefinitionChoice)DefinitionProjectileSlot.SelectedItem).ID;
        var current = document!.GlobalProjectile(row!, slot); var retainDraft = projectileState == current;
        projectileState = current;
        var type = retainDraft ? selectedType ?? current.Type : current.Type;
        var types = GlobalProjectiles.Options.Select(option => ProjectileChoice(option.Name)).ToList();
        if (current.Type.Length > 0 && types.All(item => item.ID != current.Type)) types.Add(UnknownProjectileChoice(current.Type));
        DefinitionProjectileType.ItemsSource = types; DefinitionProjectileType.SelectedItem = types.FirstOrDefault(item => item.ID == type);
        DefinitionProjectileType.IsEnabled = current.Editable; DefinitionProjectileSlot.IsEnabled = current.Editable;
        var option = GlobalProjectiles.Find(type);
        LoadProjectileForm(retainDraft ? first : option is null ? current.Data1 : GlobalProjectiles.DisplayParameter(option, current.Data1, false), retainDraft ? second : option is null ? current.Data2 : GlobalProjectiles.DisplayParameter(option, current.Data2, true));
    }
    private void LoadProjectileForm(string first, string second)
    {
        var option = GlobalProjectiles.Find((DefinitionProjectileType.SelectedItem as DefinitionChoice)?.ID ?? "");
        var form = option?.Form ?? -1; var editable = projectileState?.Editable == true && option is not null;
        if (option is not null)
        {
            var inputs = GlobalProjectiles.EditorInputs(option, first, second);
            first = inputs.Data1; second = inputs.Data2;
        }
        DefinitionProjectileApply.IsEnabled = editable;
        DefinitionProjectileData1.Text = first; DefinitionProjectileData2.Text = second;
        DefinitionProjectileLabel1.Text = ProjectileText(form is 2 or 3 ? "Creature" : form == 4 ? "Object" : "Label1." + (option?.Label1 ?? 0));
        DefinitionProjectileLabel2.Text = ProjectileText(form == 2 ? "Count" : form == 3 ? "Level" : form == 4 ? "Gold" : "Label2." + (option?.Label2 ?? 0));
        DefinitionProjectileFirst.Visibility = form == 255 ? Visibility.Collapsed : Visibility.Visible;
        DefinitionProjectileSecond.Visibility = form is 1 or 255 ? Visibility.Collapsed : Visibility.Visible;
        DefinitionProjectileData1.Visibility = form is 2 or 3 or 4 ? Visibility.Collapsed : Visibility.Visible;
        DefinitionProjectileTarget.Visibility = form is 2 or 3 or 4 ? Visibility.Visible : Visibility.Collapsed;
        DefinitionProjectileData2.Visibility = form == 3 ? Visibility.Collapsed : Visibility.Visible;
        DefinitionProjectileLevel.Visibility = form == 3 ? Visibility.Visible : Visibility.Collapsed;
        DefinitionProjectileData1.IsEnabled = editable && form is 0 or 1;
        DefinitionProjectileData2.IsEnabled = editable && form != 255 && (form != 4 || first == "1");
        DefinitionProjectileData1Steps.Visibility=option is not null&&GlobalProjectileInputs.IsNumber(option,false)?Visibility.Visible:Visibility.Collapsed;
        DefinitionProjectileData2Steps.Visibility=option is not null&&GlobalProjectileInputs.IsNumber(option,true)?Visibility.Visible:Visibility.Collapsed;
        DefinitionProjectileData1Steps.IsEnabled=DefinitionProjectileData1.IsEnabled;
        DefinitionProjectileData2Steps.IsEnabled=DefinitionProjectileData2.IsEnabled;
        DefinitionProjectileTarget.IsEnabled = DefinitionProjectileLevel.IsEnabled = editable;
        var targets = option is null ? [] : GlobalProjectiles.Targets(option).Select(ProjectileChoice).ToList();
        if (form is 2 or 3 or 4 && first.Length > 0 && targets.All(item => item.ID != first)) targets.Add(UnknownProjectileChoice(first));
        DefinitionProjectileTarget.ItemsSource = targets; DefinitionProjectileTarget.SelectedItem = targets.FirstOrDefault(item => item.ID == first);
        var levels = Enumerable.Range(1, 10).Select(level => new DefinitionChoice(level.ToString(CultureInfo.InvariantCulture), level.ToString(CultureInfo.InvariantCulture))).ToList();
        if (form == 3 && second.Length > 0 && levels.All(item => item.ID != second)) levels.Add(UnknownProjectileChoice(second));
        DefinitionProjectileLevel.ItemsSource = levels; DefinitionProjectileLevel.SelectedItem = levels.FirstOrDefault(item => item.ID == second);
        DefinitionProjectileHint.Text = ProjectileText(projectileState?.Editable != true ? "ReadOnly" : option is null ? "UnknownHint" : form == 255 ? "NoneHint" : "Hint");
        if (editable && form is 0 or 1) DefinitionProjectileHint.Text += " · " + (option!.Signed1 ? "−32768 … 32767" : "0 … 32767");
        if (editable && form == 0) DefinitionProjectileHint.Text += " / " + (option!.Signed2 ? "−32768 … 32767" : "0 … 32767");
        if (editable && form == 2) DefinitionProjectileHint.Text += " · 0 … 100";
        if (editable && form == 3) DefinitionProjectileHint.Text += " · 1 … 10";
        if (editable && form == 4) DefinitionProjectileHint.Text += " · " + ProjectileText("GoldOnly") + " · 0 … " + (document?.IsLegacyProject == true ? "80000" : "65535");
    }
    private void DefinitionProjectileSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updatingDefinitions || DefinitionProjectilePanel is null) return;
        InvalidateDefinitionEditing(); updatingDefinitions = true;
        try
        {
            if (sender == DefinitionProjectileSlot) RefreshProjectiles();
            else LoadProjectileForm(DefinitionProjectileData1.Text, DefinitionProjectileData2.Text);
        }
        finally { updatingDefinitions = false; }
    }
    private void DefinitionProjectileTarget_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updatingDefinitions || (sender as ComboBox)?.SelectedItem is not DefinitionChoice choice) return;
        InvalidateDefinitionEditing(); updatingDefinitions = true;
        try
        {
            if (sender == DefinitionProjectileLevel) DefinitionProjectileData2.Text = choice.ID;
            else LoadProjectileForm(choice.ID, DefinitionProjectileData2.Text);
        }
        finally { updatingDefinitions = false; }
    }
    private bool ProjectileInputCurrent(TextBox input,bool second,out GlobalProjectileOption? option)
    {
        option=GlobalProjectiles.Find((DefinitionProjectileType.SelectedItem as DefinitionChoice)?.ID??"");
        if(updatingDefinitions||document is null||CreatureHostedOwnerCurrent?.Invoke()==false||projectileState?.Editable!=true
            ||option is null||!GlobalProjectileInputs.IsNumber(option,second)||!input.IsEnabled||input.Visibility!=Visibility.Visible
            ||!ReferenceEquals(input,second?DefinitionProjectileData2:DefinitionProjectileData1)
            ||second&&option.Form==4&&DefinitionProjectileData1.Text!="1"
            ||DefinitionPicker.SelectedItem is not DefinitionView view||DefinitionProjectileSlot.SelectedItem is not DefinitionChoice slot)return false;
        try{return document.GlobalProjectile(view.Row,slot.ID)==projectileState;}
        catch(Exception error)when(error is ArgumentException or InvalidDataException or InvalidOperationException){return false;}
    }
    private void DefinitionProjectileInput_Changed(object sender,TextChangedEventArgs e)
    {
        if(updatingDefinitions)return;
        if(sender is TextBox input&&ProjectileInputCurrent(input,ReferenceEquals(input,DefinitionProjectileData2),out var option))
        {
            var text=GlobalProjectileInputs.Normalize(option!,ReferenceEquals(input,DefinitionProjectileData2),input.Text);
            if(text!=input.Text)
            {
                updatingDefinitions=true;
                try{input.Text=text;input.CaretIndex=text.Length;}
                finally{updatingDefinitions=false;}
            }
        }
        InvalidateDefinitionEditing();
    }
    public bool StepProjectileNumber(bool second,int direction)
    {
        var input=second?DefinitionProjectileData2:DefinitionProjectileData1;
        if(direction is not (-1 or 1)||!ProjectileInputCurrent(input,second,out var option))return false;
        input.Text=GlobalProjectileInputs.Step(option!,second,input.Text,direction);input.CaretIndex=input.Text.Length;return true;
    }
    private void DefinitionProjectileStep_Click(object sender,RoutedEventArgs e)
    {
        if(sender is Control{Tag:string tag}&&tag.Split(':') is [var field,var delta]
            &&int.TryParse(delta,CultureInfo.InvariantCulture,out var direction))StepProjectileNumber(field=="2",direction);
    }
    private void DefinitionProjectileInput_KeyDown(object sender,System.Windows.Input.KeyEventArgs e)
    {
        if(System.Windows.Input.Keyboard.Modifiers==System.Windows.Input.ModifierKeys.None
            &&e.Key is System.Windows.Input.Key.Up or System.Windows.Input.Key.Down
            &&sender is TextBox input&&StepProjectileNumber(ReferenceEquals(input,DefinitionProjectileData2),e.Key==System.Windows.Input.Key.Up?1:-1))e.Handled=true;
    }
    private void DefinitionProjectileApply_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || DefinitionPicker.SelectedItem is not DefinitionView view || DefinitionProjectileSlot.SelectedItem is not DefinitionChoice slot || DefinitionProjectileType.SelectedItem is not DefinitionChoice type) return;
        try
        {
            var before = document.GlobalProjectile(view.Row, slot.ID);
            if (!CommitFields()) return;
            if (document.GlobalProjectile(view.Row, slot.ID) != before) { DefinitionMessage("Failed", ProjectileText("Changed")); return; }
            document.SetEditorGlobalProjectile(view.Row.Family, view.Row.Index, slot.ID, uint.Parse(type.ID, CultureInfo.InvariantCulture), DefinitionProjectileData1.Text, DefinitionProjectileData2.Text);
            InvalidateDefinitionEditing(); RefreshDefinitions(); DefinitionMessage("Applied");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or FormatException or OverflowException)
        { DefinitionMessage("Failed", exception.Message); }
    }
}
