using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private sealed record JobSlotView(int Index, string Position, string Name, string Chance, string Mood, string Mana);
    private CreatureJobsDraft? jobsDraft;
    private static string JobsText(string key) => DefinitionText("Jobs." + key);
    private static DefinitionChoice JobChoice(string id)
    {
        var option = CreatureJobs.Options.SingleOrDefault(item => item.Name.Value == id);
        return new(id, "#" + id + " · " + (option?.Name.Name(Localization.LocalizationService.CurrentLanguage) ?? JobsText("Unknown")));
    }
    private void RefreshCreatureJobs()
    {
        if (DefinitionJobsPanel is null) return;
        var row = (DefinitionPicker.SelectedItem as DefinitionView)?.Row;
        var visible = row?.Family == "Creatures" && (DefinitionSection.SelectedItem as DefinitionChoice)?.ID == "Jobs";
        DefinitionJobsPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible)
        {
            if (document is null) jobsDraft = null;
            DefinitionJobsTable.ItemsSource = DefinitionJobsGroup.ItemsSource = DefinitionJobsType.ItemsSource = null;
            DefinitionJobsChance.Text = DefinitionJobsMood.Text = DefinitionJobsMana.Text = DefinitionJobsHint.Text = DefinitionJobsTotals.Text = "";
            DefinitionJobsApply.IsEnabled = DefinitionJobsAdd.IsEnabled = DefinitionJobsChange.IsEnabled = DefinitionJobsRemove.IsEnabled = false;
            DefinitionJobsAcknowledged.IsChecked = false;
            return;
        }
        var current = document!.CreateCreatureJobsDraft(row!); var retain = jobsDraft?.SameBaseline(current) == true;
        if (!retain) { jobsDraft = current; DefinitionJobsAcknowledged.IsChecked = false; }
        var group = (DefinitionJobsGroup.SelectedItem as DefinitionChoice)?.ID ?? "Happy";
        DefinitionJobsGroup.ItemsSource = CreatureJobs.Groups.Select(item => new DefinitionChoice(item.ID, JobsText(item.ID) + " · " + item.Capacity)).ToArray();
        DefinitionJobsGroup.SelectedItem = DefinitionJobsGroup.Items.Cast<DefinitionChoice>().Single(item => item.ID == group);
        LoadCreatureJobs(retain);
    }
    private void LoadCreatureJobs(bool retainInputs)
    {
        if (jobsDraft is null || DefinitionJobsGroup.SelectedItem is not DefinitionChoice groupChoice) return;
        var group = CreatureJobs.Group(groupChoice.ID); var oldSelected = DefinitionJobsTable.SelectedItems.Cast<JobSlotView>().Select(item => item.Index).ToArray();
        var oldType = (DefinitionJobsType.SelectedItem as DefinitionChoice)?.ID;
        var inputs = new[] { DefinitionJobsChance.Text, DefinitionJobsMood.Text, DefinitionJobsMana.Text };
        DefinitionJobsTable.Columns.Clear();
        foreach (var (property, key, width) in new[] { ("Position", "Slot", 65), ("Name", "Job", 360), ("Chance", "Chance", 130), ("Mood", "Mood", 145), ("Mana", "Mana", 145) }.Where(item => group.HasParameters || item.Item1 is "Position" or "Name"))
            DefinitionJobsTable.Columns.Add(new DataGridTextColumn { Header = JobsText(key), Binding = new Binding(property), MinWidth = width, Width = new(property == "Name" ? 3 : 1, DataGridLengthUnitType.Star) });
        var views = jobsDraft.Slots(group.ID).Select((slot, index) => new JobSlotView(index, (index + 1).ToString(CultureInfo.InvariantCulture), slot.Job.Length == 0 ? "—" : slot.Job == "0" ? JobsText("Empty") : JobChoice(slot.Job).Label,
            slot.Chance, CreatureJobs.DisplaySigned(slot.Mood), CreatureJobs.DisplaySigned(slot.Mana))).ToArray();
        DefinitionJobsTable.ItemsSource = views;
        foreach (var view in views.Where(view => retainInputs && oldSelected.Contains(view.Index))) DefinitionJobsTable.SelectedItems.Add(view);
        if (DefinitionJobsTable.SelectedItems.Count == 0) DefinitionJobsTable.SelectedItem = views.FirstOrDefault(view => jobsDraft.Slots(group.ID)[view.Index].Job is not ("0" or "")) ?? views[0];
        LoadCreatureJobInputs(retainInputs ? oldType : null);
        if (retainInputs) { DefinitionJobsChance.Text = inputs[0]; DefinitionJobsMood.Text = inputs[1]; DefinitionJobsMana.Text = inputs[2]; }
        DefinitionJobsParameters.Visibility = group.HasParameters ? Visibility.Visible : Visibility.Collapsed;
        DefinitionJobsParameters.IsEnabled=jobsDraft.Editable&&group.HasParameters;
        DefinitionJobsGroup.IsEnabled = DefinitionJobsType.IsEnabled = DefinitionJobsChance.IsEnabled = DefinitionJobsMood.IsEnabled = DefinitionJobsMana.IsEnabled = jobsDraft.Editable;
        DefinitionJobsApply.IsEnabled = jobsDraft.Editable; DefinitionJobsRevert.IsEnabled = jobsDraft.Editable && jobsDraft.Changed;
        DefinitionJobsTotals.Text = string.Join(" · ", jobsDraft.Totals.Select(total => JobsText(total.Group) + ": " + total.Total.ToString(CultureInfo.InvariantCulture) + "%"));
        var warning = jobsDraft.Totals.Any(total => total.NeedsAcknowledgement);
        DefinitionJobsAcknowledged.Visibility = warning && jobsDraft.Editable ? Visibility.Visible : Visibility.Collapsed;
        DefinitionJobsHint.Text = JobsText(!jobsDraft.Editable ? "ReadOnly" : warning ? "Warning" : "Hint") + (jobsDraft.Changed ? " " + JobsText("Draft") : "");
        RefreshCreatureJobButtons();
        if (CreatureHostedOwnerCurrent is not null) AdaptHostedCreaturePage("Jobs");
    }
    private void LoadCreatureJobInputs(string? retainedType = null)
    {
        if (jobsDraft is null || DefinitionJobsGroup.SelectedItem is not DefinitionChoice group) return;
        var selected = DefinitionJobsTable.SelectedItems.Count == 1 ? DefinitionJobsTable.SelectedItem as JobSlotView : null;
        var slot = selected is null ? null : jobsDraft.Slots(group.ID)[selected.Index];
        var types = CreatureJobs.Allowed(group.ID).Where(option => !jobsDraft.Slots(group.ID).Any(item => item.Job == option.Name.Value) || slot?.Job == option.Name.Value).Select(option => JobChoice(option.Name.Value)).ToList();
        var id = retainedType ?? (slot?.Job is not (null or "0" or "") ? slot.Job : null);
        if (id is not null && types.All(item => item.ID != id)) types.Add(JobChoice(id));
        DefinitionJobsType.ItemsSource = types; DefinitionJobsType.SelectedItem = types.FirstOrDefault(item => item.ID == id) ?? types.FirstOrDefault();
        DefinitionJobsChance.Text = slot?.Job is not (null or "0" or "") ? slot.Chance : "0";
        DefinitionJobsMood.Text = slot?.Job is not (null or "0" or "") ? CreatureJobs.DisplaySigned(slot.Mood) : "0";
        DefinitionJobsMana.Text = slot?.Job is not (null or "0" or "") ? CreatureJobs.DisplaySigned(slot.Mana) : "0";
    }
    private void RefreshCreatureJobButtons()
    {
        var group = (DefinitionJobsGroup.SelectedItem as DefinitionChoice)?.ID; var id = (DefinitionJobsType.SelectedItem as DefinitionChoice)?.ID;
        var editable = jobsDraft?.Editable == true && group is not null;
        DefinitionJobsAdd.IsEnabled = editable && jobsDraft!.Slots(group!).Any(item => item.Job == "0") && CreatureJobs.Allowed(group!).Any(item => item.Name.Value == id) && jobsDraft.Slots(group!).All(item => item.Job != id);
        DefinitionJobsChange.IsEnabled = editable && id is not null && DefinitionJobsTable.SelectedItems.Count == 1;
        DefinitionJobsRemove.IsEnabled = editable && DefinitionJobsTable.SelectedItems.Cast<JobSlotView>().Any(item => jobsDraft!.Slots(group!)[item.Index].Job != "0");
    }
    private void DefinitionJobsSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updatingDefinitions || DefinitionJobsTable is null) return;
        InvalidateDefinitionEditing(); DefinitionJobsAcknowledged.IsChecked = false; updatingDefinitions = true;
        try
        {
            if (sender == DefinitionJobsGroup) LoadCreatureJobs(false);
            else if (sender == DefinitionJobsTable) LoadCreatureJobInputs();
            RefreshCreatureJobButtons();
        }
        finally { updatingDefinitions = false; }
    }
    private TextBox? JobInput(string field)=>field switch{"Chance"=>DefinitionJobsChance,"Mood"=>DefinitionJobsMood,"Mana"=>DefinitionJobsMana,_=>null};
    private string? JobInputField(TextBox input)=>ReferenceEquals(input,DefinitionJobsChance)?"Chance":ReferenceEquals(input,DefinitionJobsMood)?"Mood":ReferenceEquals(input,DefinitionJobsMana)?"Mana":null;
    private bool JobInputCurrent(string field,TextBox input)
    {
        if(updatingDefinitions||document is null||jobsDraft?.Editable!=true||CreatureHostedOwnerCurrent?.Invoke()==false
            ||!ReferenceEquals(JobInput(field),input)||!input.IsEnabled||DefinitionJobsGroup.SelectedItem is not DefinitionChoice group||!CreatureJobs.Group(group.ID).HasParameters
            ||DefinitionPicker.SelectedItem is not DefinitionView view||view.Row.Family!="Creatures"||view.Row.Index!=jobsDraft.Index
            ||(DefinitionSection.SelectedItem as DefinitionChoice)?.ID!="Jobs")return false;
        try{return jobsDraft.SameBaseline(document.CreateCreatureJobsDraft(view.Row));}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException){return false;}
    }
    private void DefinitionJobsDraft_Changed(object sender,TextChangedEventArgs e)
    {
        if(updatingDefinitions)return;
        if(sender is TextBox input&&JobInputField(input) is { } field&&JobInputCurrent(field,input))
        {
            var text=CreatureJobInputs.Normalize(field,input.Text);
            if(text!=input.Text)
            {
                updatingDefinitions=true;
                try{input.Text=text;input.CaretIndex=text.Length;}
                finally{updatingDefinitions=false;}
            }
        }
        InvalidateDefinitionEditing();DefinitionJobsAcknowledged.IsChecked=false;
    }
    public bool StepCreatureJob(string field,int direction)
    {
        if(direction is not (-1 or 1)||JobInput(field) is not { } input||!JobInputCurrent(field,input))return false;
        input.Text=CreatureJobInputs.Step(field,input.Text,direction);input.CaretIndex=input.Text.Length;return true;
    }
    private void DefinitionJobsStep_Click(object sender,RoutedEventArgs e)
    {
        if(sender is FrameworkElement{Tag:string tag}&&tag.Split(':') is [var field,var text]
            &&int.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out var direction))StepCreatureJob(field,direction);
    }
    private void DefinitionJobsNumber_KeyDown(object sender,System.Windows.Input.KeyEventArgs e)
    {
        if(sender is TextBox input&&JobInputField(input) is { } field&&System.Windows.Input.Keyboard.Modifiers==System.Windows.Input.ModifierKeys.None
            &&e.Key is System.Windows.Input.Key.Up or System.Windows.Input.Key.Down&&StepCreatureJob(field,e.Key==System.Windows.Input.Key.Up?1:-1))e.Handled=true;
    }
    private void DefinitionJobsEdit_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || jobsDraft is null || DefinitionJobsGroup.SelectedItem is not DefinitionChoice group || DefinitionPicker.SelectedItem is not DefinitionView view) return;
        var operation = (sender as Button)?.Tag as string; var draft = jobsDraft;
        try
        {
            if (operation == "Apply")
            {
                var acknowledged = DefinitionJobsAcknowledged.IsChecked == true;
                if (!CommitFields()) return;
                if (!draft.SameBaseline(document.CreateCreatureJobsDraft(view.Row))) { DefinitionMessage("Failed", JobsText("Changed")); return; }
                document.ApplyCreatureJobs(draft, acknowledged); InvalidateDefinitionEditing(); RefreshDefinitions(); DefinitionMessage("Applied"); return;
            }
            if (operation == "Revert") jobsDraft = document.CreateCreatureJobsDraft(view.Row);
            else if (operation == "Remove") jobsDraft = document.RemoveCreatureJobs(draft, group.ID, DefinitionJobsTable.SelectedItems.Cast<JobSlotView>().Where(item => draft.Slots(group.ID)[item.Index].Job != "0").Select(item => item.Index));
            else if (DefinitionJobsType.SelectedItem is DefinitionChoice choice)
            {
                if (operation == "Add") jobsDraft = document.AddCreatureJob(draft, group.ID, uint.Parse(choice.ID, CultureInfo.InvariantCulture), DefinitionJobsChance.Text, DefinitionJobsMood.Text, DefinitionJobsMana.Text);
                else if (DefinitionJobsTable.SelectedItem is JobSlotView selected) jobsDraft = document.ChangeCreatureJob(draft, group.ID, selected.Index, choice.ID, DefinitionJobsChance.Text, DefinitionJobsMood.Text, DefinitionJobsMana.Text);
            }
            InvalidateDefinitionEditing(); DefinitionJobsAcknowledged.IsChecked = false; updatingDefinitions = true;
            try { LoadCreatureJobs(false); } finally { updatingDefinitions = false; }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or FormatException or OverflowException)
        { DefinitionMessage("Failed", exception.Message); }
    }
}
