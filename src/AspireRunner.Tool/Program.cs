using AspireRunner;
using Microsoft.Extensions.Logging;

var dotnetCli = DotnetCli.TryCreate();

if (dotnetCli == null)
{
    Console.WriteLine("The dotnet CLI wasn't found.");
    return;
}

var aspireDashboard = new AspireDashboard(dotnetCli, new AspireDashboardOptions() { }, new ConsoleLogger<AspireDashboard>());

aspireDashboard.Start();


class ConsoleLogger<T> : ILogger<T>
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine(formatter(state, exception));
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}