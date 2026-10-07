using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace CropBreeding;

internal static class ErrorHandler
{
    private static readonly Dictionary<string, long> LastChat = new(StringComparer.Ordinal);
    private static readonly Core.ErrorThrottle LogThrottle = new();

    // Log new failures in full. While the same action keeps failing, summarize repeats
    // at most once per ten seconds. Chat remains independently rate-limited.
    internal static void Report(string action, Exception error)
    {
        try
        {
            Exception cause = error.GetBaseException();
            string signature = cause.GetType().FullName + "\n" + cause.Message + "\n" + cause.StackTrace;
            var decision = LogThrottle.Next(action, signature, Environment.TickCount64);
            if (decision.Full)
                ModEntry.Instance.Monitor.Log($"{action} failed; skipping this action's breeding changes.\n{error}", LogLevel.Error);
            else if (decision.Repeats > 0)
                ModEntry.Instance.Monitor.Log($"{action}: the same error repeated {decision.Repeats} times since the previous report. See the first error for details.", LogLevel.Error);
        }
        catch { /* The logger itself may be unavailable during startup/shutdown. */ }
        try
        {
            if (!ModEntry.Instance.Config.ShowErrorsInChat || !Context.IsWorldReady || Game1.chatBox == null) return;
            long now = Environment.TickCount64;
            if (LastChat.TryGetValue(action, out long last) && now - last < 10_000) return;
            LastChat[action] = now;
            string message = error.GetBaseException().Message.Replace('\r', ' ').Replace('\n', ' ');
            if (message.Length > 200) message = message[..200] + "…";
            // Local chat display only: never submit a command or broadcast to other players.
            Game1.chatBox.addMessage($"[Crop Breeding] {action}: {message}", Color.OrangeRed);
        }
        catch { /* Do not recurse if the chat UI is the failing component. */ }
    }

    internal static bool Try(string action, Action run)
    {
        try { run(); return true; }
        catch (Exception ex) { Report(action, ex); return false; }
    }
}
