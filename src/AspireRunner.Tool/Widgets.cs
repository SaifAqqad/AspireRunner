using Microsoft.Extensions.Logging;
using Spectre.Console.Rendering;

namespace AspireRunner.Tool;

public static partial class Widgets
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
        return AnsiConsole.Profile.Height <= 17 || AnsiConsole.Profile.Width <= 90 ? SmallHeader : LargeHeader;
    }

    public static Renderable Error(string error)
    {
        return new Markup($"\n[red bold]Error[/] {error}\n");
    }

    public static Renderable SuccessCheck()
    {
        return new Markup("[green]✓[/]");
    }

    public static Renderable ErrorCross()
    {
        return new Markup("[red]✕[/]");
    }

    public static Renderable ErrorWidget(this Exception ex)
    {
        var message = ex.InnerException is not null ? ex.InnerException.Message : ex.Message;
        return new Markup($"[red bold]Error[/] {message}");
    }

    public static IRenderable LogRecord(LogRecord logRecord)
    {
        var color = logRecord.Level switch
        {
            LogLevel.Error or LogLevel.Critical => Color.Red,
            LogLevel.Warning => Color.Yellow,
            _ => DefaultColor
        };

        return Markup.FromInterpolated($"[{color.ToMarkup()} bold]{logRecord.Level}[/] {logRecord.Message.Trim()}");
    }

    public static Renderable Widget(this string text)
    {
        return new Markup(text);
    }
}