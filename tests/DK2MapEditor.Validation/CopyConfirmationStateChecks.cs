using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class CopyConfirmationStateChecks
{
    private static void Expect(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
    public static void Run()
    {
        var current = new CopyBufferConfirmationState();
        Expect(current.RequiresConfirmation, "A fresh editor must ask before replacing an occupied buffer.");
        current.AcceptReplacement(false);
        Expect(current.RequiresConfirmation, "An ordinary Yes must not suppress the next question.");
        current.AcceptReplacement(true);
        Expect(!current.RequiresConfirmation && current.Configured, "Remembered Yes must suppress only this session.");
        // Saving preferences at any point during the session must not turn a
        // remembered dialog answer into a persistent menu choice.
        var saved = JsonSerializer.Serialize(current.Configured);
        var nextProcess = new CopyBufferConfirmationState(JsonSerializer.Deserialize<bool>(saved));
        Expect(nextProcess.RequiresConfirmation && !nextProcess.Suppressed && !current.RequiresConfirmation,
            "Saving/reopening inherited a remembered reply or reset the running session.");
        current.Configure(true);
        Expect(current.RequiresConfirmation && !current.Suppressed, "Explicitly reenabling questions must reset session suppression.");
        current.AcceptReplacement(true); current.Configure(false);
        Expect(!current.RequiresConfirmation && !current.Configured && !current.Suppressed, "Explicit menu disable must remain distinct from remembered Yes.");
        var disabled = new CopyBufferConfirmationState(JsonSerializer.Deserialize<bool>(JsonSerializer.Serialize(current.Configured)));
        Expect(!disabled.RequiresConfirmation && !disabled.Configured, "Explicit menu disable must survive a new process.");
        disabled.AcceptReplacement(true);
        Expect(!disabled.Configured && !disabled.Suppressed, "A late reply overrode an explicit menu disable.");
        disabled.Configure(true); disabled.AcceptReplacement(false);
        Expect(disabled.RequiresConfirmation, "An explicit menu reset did not restore ordinary questioning.");
    }
}
