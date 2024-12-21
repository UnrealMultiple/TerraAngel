using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace TerraAngel;

internal class UpdateChecker
{
    private static string GitHubVersionFile => "https://raw.githubusercontent.com/UnrealMultiple/TerraAngel/refs/heads/master/TerraAngelPatches/VERSION";

    public static Version? NextUpdateVersion;

    public static async Task<bool> IsUpdateAvailableAsync()
    {
        if (ClientLoader.TerraAngelVersion is null)
        {
            return false;
        }

        ClientLoader.Console.WriteLine(GetString("Checking for updates"));

        try
        {
            using HttpClient httpClient = new HttpClient();

            NextUpdateVersion = Version.Parse(await httpClient.GetStringAsync(GitHubVersionFile));

            bool updateAvailable = NextUpdateVersion > ClientLoader.TerraAngelVersion;

            if (updateAvailable)
            {
                ClientLoader.Console.WriteLine(GetString("Updates available"));
            }

            return updateAvailable;
        }
        catch
        {
            ClientLoader.Console.WriteLine(GetString("Error occured while checking for updates"));
            return false;
        }
    }
}