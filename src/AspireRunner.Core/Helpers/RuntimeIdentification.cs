using System.Runtime.InteropServices;

namespace AspireRunner.Core.Helpers;

public static class RuntimeIdentification
{
    public static string OsIdentifier
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "win";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "osx";
            return "unknown";
        }
    }

    public static string Rid => $"{OsIdentifier}-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}";
}