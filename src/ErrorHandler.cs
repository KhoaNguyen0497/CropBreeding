using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace CropBreeding;

internal static class ErrorHandler
{
    private static readonly Dictionary<string, long> LastChat = new(StringComparer.Ordinal);

    // Logging must never become a second gameplay error. Full details always go to SMAPI;
    // repeat chat notices for the same action are limited to one per ten seconds.
    internal static void Report(string action, Exception error)
    {
        try { ModEntry.Instance.Monitor.Log($"{action} failed; skipping this action's breeding changes.\n{error}", LogLevel.Error); }
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
