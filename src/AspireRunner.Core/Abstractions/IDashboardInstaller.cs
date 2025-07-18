namespace AspireRunner.Core.Abstractions;

public interface IDashboardInstaller
{
    /// <summary>
    /// Checks if the latest version of the dashboard is installed, and if not, installs it to the runner's path.
    /// </summary>
    /// <returns>A tuple containing a success flag, the latest version available, and the currently installed version (if any).</returns>
    /// <exception cref="ApplicationException">Thrown If no versions are fetched from the nuget repo, could be caused by a network issue or the repo simply not having any versions of the package available</exception>
    Task<(bool Success, Version Latest, Version? Installed)> EnsureLatestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads and installs the specified dashboard version to the runner's path.
    /// </summary>
    /// <param name="version">The dashboard version to install</param>
    /// <returns><c>true</c> if the dashboard was installed successfully, <c>false</c> otherwise</returns>
    Task<bool> InstallAsync(Version version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the specified dashboard version from the runner's path if it's installed.
    /// </summary>
    /// <param name="version">The dashboard version to remove</param>
    /// <returns><c>true</c> If the version is removed successfully, <c>false</c> otherwise</returns>
    Task<bool> RemoveAsync(Version version, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the available dashboard versions in descending order (newest to oldest).
    /// </summary>
    /// <param name="includePreRelease">Whether to include pre-release versions, defaults to false</param>
    Task<Version[]> GetAvailableVersionsAsync(bool includePreRelease = false, CancellationToken cancellationToken = default);
}