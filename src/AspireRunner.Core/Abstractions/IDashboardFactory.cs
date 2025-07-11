namespace AspireRunner.Core.Abstractions;

public interface IDashboardFactory
{
    Task<Dashboard?> CreateDashboardAsync(DashboardOptions options);
}