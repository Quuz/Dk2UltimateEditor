using System.Buffers.Binary;
using System.Reflection;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;
internal static class HelpSectionVersionsChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    public static void Run()
    {
        var directory=Path.Combine(Path.GetTempPath(),"DK2HelpVersions-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var app=Path.Combine(directory,"App");var docs=Path.Combine(directory,"Documentation");Directory.CreateDirectory(app);Directory.CreateDirectory(docs);
            foreach(var lang in new[]{"RU","EN"})File.WriteAllText(Path.Combine(docs,"USER_GUIDE_"+lang+".md"),"# Guide");
            Check(EditorHelpRules.FindGuide(app,"ru")==Path.Combine(docs,"USER_GUIDE_RU.md")&&EditorHelpRules.FindGuide(app,"en")==Path.Combine(docs,"USER_GUIDE_EN.md"),"Packaged guide did not use the selected language and sibling Documentation.");
            var page=Path.Combine(docs,"USER_GUIDE_RU.md");File.WriteAllText(Path.Combine(docs,"DETAILS.md"),"details");File.WriteAllText(Path.Combine(directory,"outside.md"),"outside");
            Check(EditorHelpRules.LocalLink(docs,page,"DETAILS.md#part")==Path.Combine(docs,"DETAILS.md")&&EditorHelpRules.LocalLink(docs,page,"../outside.md")==null
                &&EditorHelpRules.LocalLink(docs,page,"https://example.com/guide.md")==null&&EditorHelpRules.LocalLink(docs,page,"missing.md")==null,"Local guide links escaped the packaged docs or lost an existing linked page.");
            File.Delete(Path.Combine(docs,"USER_GUIDE_RU.md"));Check(EditorHelpRules.FindGuide(app,"ru")==null,"Missing RU guide silently chose a different language.");
            var root=Path.Combine(directory,"source");Directory.CreateDirectory(Path.Combine(root,"docs"));Directory.CreateDirectory(Path.Combine(root,"src","output"));File.WriteAllText(Path.Combine(root,"DK2MapEditor.slnx"),"<Solution/>");
            var guide=Path.Combine(root,"docs","USER_GUIDE_EN.md");File.WriteAllText(guide,"# Guide");Check(EditorHelpRules.FindGuide(Path.Combine(root,"src","output"),"en")==guide,"Source build did not locate its canonical guide.");
            var project=NativeMapDocument.CreateNewMap(new("Versions","Versions",18,16));
            var before=project.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var revision=project.EditRevision;var history=project.GetHistory();var rows=EditorSectionVersions.Capture(project);
            Check(rows.Count==9&&rows.Single(r=>r.Section.Key=="MapInfo") is {Input:7,Working:7,State:"Current"},"Current project did not expose actual metadata versions.");
            Check(rows.Single(r=>r.Section.Key=="Globals") is {Input:null,Working:1,State:"WorkingOnly"},"Generated optional section was falsely reported as input data.");
            Check(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&project.EditRevision==revision&&project.GetHistory().ChangeCount==history.ChangeCount&&project.GetHistory().Undo.Count==history.Undo.Count,"Version projection mutated live data/history.");
            Check(EditorSectionVersions.Capture(NativeMapDocument.CreateNewMap(new("NativeVersions","Native versions",18,16,NativeFormat:true))).All(r=>r.Input is null&&r.Working is null&&r.State=="NotApplicable"),"Native header words were guessed as project section versions.");
            var field=typeof(NativeFile).GetField("<Bytes>k__BackingField",BindingFlags.NonPublic|BindingFlags.Instance)!;
            var info=project.Files.Single(f=>f.Signature==NativeMapDocument.LegacyInfo);var original=info.Data.ToArray();
            foreach(var (value,state) in new[]{(6u,"Older"),(8u,"Newer"),(0u,"Invalid")})
            {var changed=original.ToArray();BinaryPrimitives.WriteUInt32LittleEndian(changed.AsSpan(4),value);field.SetValue(info,changed);Check(EditorSectionVersions.Capture(project).Single(r=>r.Section.Key=="MapInfo").State==state,"Version state guessed a supported layout instead of comparing the actual version word.");}
            field.SetValue(info,original);
            field.SetValue(info,original[..8]);Check(EditorSectionVersions.Capture(project).Single(r=>r.Section.Key=="MapInfo") is {Working:null,State:"Invalid"},"Truncated working header was reported as an absent section or read past its buffer.");field.SetValue(info,original);
            var input=project.MainFile.Data.ToArray();var raw=LegacySectionFile.Parse(input);
            var duplicate=LegacySectionFile.Create(raw.Sections.Concat(raw.Sections.Where(s=>s.Signature==NativeMapDocument.LegacyInfo))).ToArray();
            field.SetValue(project.MainFile,duplicate);Check(EditorSectionVersions.Capture(project).Single(r=>r.Section.Key=="MapInfo") is {Input:null,State:"Ambiguous"},"Duplicate source sections were silently collapsed.");field.SetValue(project.MainFile,input);
            foreach(var bytes in new[]{input[..11],input[..^1],new byte[12]})Check(EditorSectionVersions.InputVersions(bytes)==null,"Truncated or foreign container accepted.");
            var overflow=input.ToArray();BinaryPrimitives.WriteUInt32LittleEndian(overflow.AsSpan(20),uint.MaxValue);Check(EditorSectionVersions.InputVersions(overflow)==null,"Oversized section crossed its buffer.");
        }
        finally{Directory.Delete(directory,true);}
    }
}
