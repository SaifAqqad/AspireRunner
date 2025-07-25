using Spectre.Console.Rendering;

namespace AspireRunner.Tool;

public static class Widgets
{
    public static Color DefaultColor => Color.SlateBlue1;

    public static string DefaultColorText { get; } = DefaultColor.ToMarkup();

    public static Renderable RunnerVersion { get; } = new Markup($"[{DefaultColorText} dim]v{RunnerInfo.Version}[/]");

    private static readonly Renderable SmallHeader = Markup.FromInterpolated($"[{DefaultColorText}][link={RunnerInfo.ProjectUrl}]Aspire Runner[/][/]\n");
    private static readonly Renderable LargeHeader = new FigletText("Aspire Runner").LeftJustified().Color(DefaultColor);

    public static Renderable StatusSymbol(bool status)
    {
        var style = status ?
            new Style(Color.Green, decoration: Decoration.Bold) :
            new Style(Color.Red, decoration: Decoration.Bold);

        return new Markup("○", style);
    }

    public static Renderable KeyActionDescriptor(string key, string description)
    {
        return new Columns(new Text($"[{key}]"), new Text(description)).Collapse();
    }

    public static Renderable Header()
    {
        return AnsiConsole.Profile.Height <= 15 || AnsiConsole.Profile.Width <= 90 ? SmallHeader : LargeHeader;
    }

    public static Renderable Error(string error)
    {
        return new Markup($"\n[red][bold][[Error]][/] {error}[/]\n");
    }

    public static Renderable ErrorWidget(this Exception ex)
    {
        var message = ex.InnerException is not null ? ex.InnerException.Message : ex.Message;
        return new Markup($"[red][bold][[Error]][/] {message}[/]");
    }

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