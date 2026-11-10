namespace DK2MapEditor.App.Editing;

/// <summary>Source FontHeight + 2 physical pixels, expressed in the current monitor's DIPs.</summary>
internal static class RewardImageSizing
{
    internal static double Size(double lineHeight,double dpiScale)
    {
        if(!double.IsFinite(lineHeight)||lineHeight<=0)throw new ArgumentOutOfRangeException(nameof(lineHeight));
        if(!double.IsFinite(dpiScale)||dpiScale<=0)throw new ArgumentOutOfRangeException(nameof(dpiScale));
        return (Math.Ceiling(lineHeight*dpiScale)+2)/dpiScale;
    }
}
