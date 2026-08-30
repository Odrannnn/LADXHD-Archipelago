using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.Archipelago;
using ProjectZ.InGame.Overlay;
using ProjectZ.InGame.Things;

internal static class ArchipelagoConnectionWarningTests
{
    private const string IconResource = "ProjectZ.Archipelago.icon.png";
    private const string LicenseResource = "ProjectZ.Archipelago.icon.LICENSE";
    private const string IconSha256 = "55B8E964106CDE2F52839E9BB5CDCCEB95E427DCE00A71892B116C91118E9273";
    private const string LicenseSha256 = "F66C14CEC56600DEC0A63C5DC14D9FE3DC4EFBCDFBF34B100B6FCF1F1E3DED00";

    public static void Run()
    {
        Check(!ArchipelagoManager.ShouldShowConnectionWarning(false, false) &&
              !ArchipelagoManager.ShouldShowConnectionWarning(false, true),
            "Vanilla saves must never show an Archipelago connection warning.");
        Check(ArchipelagoManager.ShouldShowConnectionWarning(true, false) &&
              !ArchipelagoManager.ShouldShowConnectionWarning(true, true),
            "A bound save must warn until its validated session is connected.");
        Check(ArchipelagoManager.ShouldShowConnectionWarning(true, false) &&
              !ArchipelagoManager.ShouldShowConnectionWarning(false, false),
            "Changing from a disconnected bound save to a vanilla save must not retain its warning.");

        var warningText = (string)typeof(HUDOverlay)
            .GetField("ConnectionWarningText", BindingFlags.NonPublic | BindingFlags.Static)
            .GetRawConstantValue();
        Check(warningText == "Archipelago not connected",
            "The persistent warning must use the intended concise HUD text.");

        CheckLayout(new Rectangle(0, 0, 1600, 900), new Vector2(176, 16), 2);
        CheckLayout(new Rectangle(120, 64, 160, 128), new Vector2(176, 16), 2);
        CheckLayout(new Rectangle(0, 0, 2944, 1840), new Vector2(176, 16), 3);

        var assembly = typeof(Resources).Assembly;
        CheckResource(assembly, IconResource, IconSha256, png: true);
        CheckResource(assembly, LicenseResource, LicenseSha256, png: false);
    }

    private static void CheckLayout(Rectangle hudBounds, Vector2 textSize, float preferredScale)
    {
        var method = typeof(HUDOverlay).GetMethod("GetConnectionWarningLayout",
            BindingFlags.NonPublic | BindingFlags.Static);
        var layout = ((ValueTuple<Rectangle, float>)method.Invoke(null,
            [hudBounds, textSize, preferredScale]));
        Check(layout.Item2 > 0 && layout.Item2 <= preferredScale,
            "The connection-warning scale must be positive and cannot exceed the HUD scale.");
        Check(layout.Item1.Width > 0 && layout.Item1.Height > 0 &&
              layout.Item1.Left >= hudBounds.Left && layout.Item1.Right <= hudBounds.Right &&
              layout.Item1.Top >= hudBounds.Top && layout.Item1.Bottom <= hudBounds.Bottom,
            "The connection-warning banner must remain inside the active HUD window at every tested scale.");
    }

    private static void CheckResource(Assembly assembly, string resourceName, string expectedSha256, bool png)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        Check(stream != null, $"The Archipelago {resourceName} resource must be embedded in Core.");
        using var contents = new MemoryStream();
        stream.CopyTo(contents);
        var bytes = contents.ToArray();
        var verifiedBytes = png
            ? bytes
            : Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\r", "\n", StringComparison.Ordinal));
        Check(Convert.ToHexString(SHA256.HashData(verifiedBytes)) == expectedSha256,
            $"The embedded {resourceName} resource must retain its reviewed content.");
        if (png)
        {
            Check(bytes.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                "The embedded Archipelago icon must be a PNG.");
        }
        else
        {
            Check(Encoding.UTF8.GetString(bytes).StartsWith("MIT License", StringComparison.Ordinal),
                "The embedded Archipelago icon license must retain its MIT notice.");
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
