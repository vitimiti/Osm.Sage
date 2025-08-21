using System.Diagnostics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Osm.Sage.BrowserDispatch;

/// <summary>
/// Provides utility methods to open web browsers and navigate to a specified URL.
/// </summary>
[PublicAPI]
public static class BrowserLauncher
{
    /// <summary>
    /// Opens the default web browser and navigates to the specified URL.
    /// </summary>
    /// <param name="url">The URL to be opened in the web browser. This should be a valid URI string.</param>
    public static void Open([UriString] string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }
}
