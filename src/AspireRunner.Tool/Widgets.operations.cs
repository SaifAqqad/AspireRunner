using Spectre.Console.Extensions;
using Spectre.Console.Rendering;

namespace AspireRunner.Tool;

public partial class Widgets
{
    public static void Write(string markup, bool newLine = false) => Write(new Markup(markup), newLine);

    public static void WriteInterpolated(FormattableString markup, bool newLine = false) => Write(Markup.FromInterpolated(markup), newLine);

    public static void Write(Renderable renderable, bool newLine = false)
    {
        AnsiConsole.Write(renderable);

        if (newLine)
        {
            AnsiConsole.WriteLine();
        }
    }

    public static void Write(IEnumerable<Renderable> renderables, bool newLine = false)
    {
        foreach (var renderable in renderables)
        {
            AnsiConsole.Write(renderable);
        }

        if (newLine)
        {
            AnsiConsole.WriteLine();
        }
    }

    public static void WriteLines(int count = 1)
    {
        while (count-- > 0)
        {
            AnsiConsole.WriteLine();
        }
    }

    public static void Render(this Layout layout)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(layout);
        AnsiConsole.Cursor.Hide();
    }

    public static async Task<T> ShowSpinner<T>(this Task<T> task, bool withResult = false)
    {
        var spinnerInternal = (await task.Spinner(Spinner.Known.Dots, PrimaryColor))!;
        if (withResult)
        {
            AnsiConsole.Write(SuccessCheck());
            AnsiConsole.WriteLine();
        }

        return spinnerInternal;
    }

    public static bool IsConsoleSmall()
    {
        return AnsiConsole.Profile.Height <= 17 || AnsiConsole.Profile.Width <= 90;
    }
}