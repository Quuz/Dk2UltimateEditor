namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private BandLayout RequireEditorHeroBandInputLayout(NativeRecord band)
    {
        var layout=GetBandLayout(band);
        if(!layout.Project)return RequireEditorHeroBandLayout(band);
        if(band.File.UInt32(4) is <1 or >5)throw new InvalidDataException("Unverified historical hero-band layout.");
        _=ContiguousBandCount(layout);return layout;
    }
    private void UpgradePrivateEditorHeroBandSection()
    {
        if(!IsLegacyProject)return;
        var file=projectSections.Single(f=>f.Signature==LegacyBands);var version=file.UInt32(4);
        if(version==5)return;
        if(version is <1 or >4)throw new InvalidDataException("Unverified historical hero-band conversion.");
        // Reuse the source reader conversion inside the independent parent
        // copy only. Other project sections and literal scripts stay intact.
        file.Bytes=UpgradeBands(file,(int)version);
        if(files.Sum(f=>(long)f.Length)>128L*1024*1024 || projectSections.Sum(f=>(long)f.Length)+12>MaximumFileBytes)
            throw new InvalidDataException("Historical band draft exceeds its safe size limit.");
        _=Records();
    }
}
