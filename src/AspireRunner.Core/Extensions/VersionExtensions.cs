namespace AspireRunner.Core.Extensions;

public static class VersionExtensions
{
    public static bool IsCompatibleWith(this Version version, Version other)
    {
        if (version.Major != other.Major)
        {
            return false;
        }

        if (version.Minor < other.Minor)
        {
            return false;
        }

        return true;
    }
}