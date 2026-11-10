namespace DK2MapEditor.Core.Editing;

/// <summary>The explicit menu preference may persist; the source REQUEST_COPY
/// remembered affirmative reply belongs only to the current editor session.</summary>
public sealed class CopyBufferConfirmationState(bool configured = true)
{
    public bool Configured { get; private set; } = configured;
    public bool Suppressed { get; private set; }
    public bool RequiresConfirmation => Configured && !Suppressed;

    public void Configure(bool enabled)
    { Configured = enabled; Suppressed = false; }

    /// <summary>Call only after the current affirmative reply has successfully
    /// replaced the buffer. Declined, stale or failed commands do not remember.</summary>
    public void AcceptReplacement(bool remember)
    {
        // A settings callback may explicitly disable questions while the
        // replacement completes. Recording its reply must not fail after the
        // buffer has already changed or override that explicit preference.
        Suppressed = Configured && remember;
    }
}
