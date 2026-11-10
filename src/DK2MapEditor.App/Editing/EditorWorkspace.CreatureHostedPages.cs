using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    internal long CreatureHostedInputRevision { get; private set; }
    internal Func<bool>? CreatureHostedOwnerCurrent { get; set; }
    internal FrameworkElement DetachCreaturePage(string section, int index)
    {
        WorkspaceTabs.SelectedIndex = 9;
        DefinitionFamily.SelectedItem = DefinitionFamily.Items.Cast<DefinitionChoice>().Single(item => item.ID == "Creatures");
        DefinitionPicker.SelectedItem = DefinitionPicker.Items.Cast<DefinitionView>().Single(item => item.Row.Index == index);
        DefinitionSection.SelectedItem = DefinitionSection.Items.Cast<DefinitionChoice>().Single(item => item.ID == section);
        var panel = section switch { "Attacks" => DefinitionAttackPanel, "Jobs" => DefinitionJobsPanel, _ => DefinitionOptionPanel };
        ((Panel)panel.Parent).Children.Remove(panel); panel.IsExpanded = true;
        foreach (var name in section switch { "Attacks" => new[]{"DefinitionAttackApply","DefinitionAttackRevert"}, "Jobs" => new[]{"DefinitionJobsApply","DefinitionJobsRevert","DefinitionJobsAcknowledged"}, _ => new[]{"DefinitionOptionApply","DefinitionOptionRevert"} })
            ((FrameworkElement)FindName(name)).Visibility = Visibility.Collapsed;
        DefinitionAttackType.Width = 360;
        AdaptHostedCreaturePage(section);
        return panel;
    }
    private void AdaptHostedCreaturePage(string section)
    {
        if(section!="Jobs")return;
        foreach(var column in DefinitionJobsTable.Columns)
        {
            var property=(column as DataGridTextColumn)?.Binding is Binding binding?binding.Path.Path:"";
            var label=property is "Chance" or "Mood" or "Mana"?LocalizationService.Get("CreatureGlobals.Job"+property):column.Header is TextBlock header?header.Text:column.Header?.ToString()??"";
            var width=property switch{"Name"=>150,"Chance"=>80,"Mood"=>100,"Mana"=>80,_=>60};
            column.MinWidth=width;column.Header=new TextBlock{Text=label,TextWrapping=TextWrapping.Wrap,MaxWidth=width-12};
        }
        DefinitionJobsAcknowledged.Visibility=Visibility.Collapsed;
    }
    internal IReadOnlyList<CreatureGlobalsJobWarning> HostedJobWarnings => jobsDraft is null ? [] : jobsDraft.Totals
        .Where(total => total.NeedsAcknowledgement).Select(total => new CreatureGlobalsJobWarning(jobsDraft.Index,total.Group,total.Total)).ToArray();
    internal bool ApplyHostedCreaturePage(string section)
    {
        if(document is null || DefinitionPicker.SelectedItem is not DefinitionView view) return false;
        try
        {
            if(section == "Attacks" && attackState is { } attacks)
            {
                CaptureAttackForm();var prepared=attacks;
                foreach(var slot in CreatureAttacks.Slots)
                {var form=attackForms[slot];prepared=document.UpdateCreatureAttack(prepared,slot,form.Type,form.Inputs.Select(item=>new CreatureAttackInput(item.Key,item.Value)));}
                document.ApplyCreatureAttacks(prepared);return true;
            }
            if(CreatureOptions.Groups.Contains(section) && creatureOptionForms.ContainsKey(section))
            {
                CaptureCreatureOptions();var form=creatureOptionForms[section];
                document.ApplyCreatureOptions(document.UpdateCreatureOptions(form.Baseline,form.Inputs.Select(item=>new CreatureOptionInput(item.Key,item.Value))));return true;
            }
            if(section == "Jobs" && jobsDraft is { } jobs)
            {
                // Add/Change/Remove submit tuples explicitly, as original Add/Remove do.
                // The common parent checks source totals before this private write.
                document.ApplyCreatureJobs(jobs,true);return true;
            }
            return false;
        }
        catch(Exception error) when(error is ArgumentException or InvalidDataException or InvalidOperationException or FormatException or OverflowException)
        {DefinitionMessage("Failed",error.Message);return false;}
    }
    internal void RefreshHostedCreaturePage(string section)
    {
        updatingDefinitions=true;
        try
        {
            if(section=="Attacks")RefreshCreatureAttacks();
            else if(section=="Jobs"){RefreshCreatureJobs();DefinitionJobsAcknowledged.Visibility=Visibility.Collapsed;}
            else RefreshCreatureOptions();
        }
        finally{updatingDefinitions=false;}
        AdaptHostedCreaturePage(section);
        CreatureHostedInputRevision++;
    }
}
