namespace AspireRunner.Tool.Extensions;

public static class ConsoleExtensions
{
    public static void EmptyLines(this IAnsiConsole console, int count = 1)
    {
        while (count-- > 0)
        {
            console.WriteLine();
        }
    }

    public static void Render(this Layout layout)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(layout);
        AnsiConsole.Cursor.Hide();
    }
}