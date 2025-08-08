using Microsoft.Extensions.Logging;
using Spectre.Console.Rendering;

namespace AspireRunner.Tool;

public static partial class Widgets
{
    public static Color PrimaryColor => Color.SlateBlue1;

    public static string PrimaryColorText { get; } = PrimaryColor.ToMarkup();

    public static Renderable RunnerVersion { get; } = new Markup($"[{PrimaryColorText} dim]v{RunnerInfo.Version}[/]");

    public static Renderable SmallHeader { get; } = Markup.FromInterpolated($"[{PrimaryColorText}][link={RunnerInfo.ProjectUrl}]Aspire Runner[/][/]\n");

    public static Renderable LargeHeader { get; } = new FigletText("Aspire Runner").LeftJustified().Color(PrimaryColor);

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
        return IsSmol() ? SmallHeader : LargeHeader;
    }

    public static Renderable Error(string error)
    {
        return new Markup($"\n[red bold]Error[/] {error}\n");
    }

    public static Renderable Link(string url, string? text = null, int? maxWidth = null)
    {
        var display = string.IsNullOrWhiteSpace(text) ? url : text;
        if (maxWidth.HasValue)
        {
            if (display.Length > maxWidth.Value)
            {
                display = $"{display[..(maxWidth.Value - 2)]}...";
            }
        }

        return Markup.FromInterpolated($"[link={url}]{display}[/]");
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
        var (backgroundColor, textColor) = logRecord.Level switch
        {
            LogLevel.Error or LogLevel.Critical => (Color.Red, Color.Black),
            LogLevel.Warning => (Color.Yellow, Color.Black),
            _ => (Color.Default, PrimaryColor)
        };

        return Markup.FromInterpolated($"[{textColor.ToMarkup()} on {backgroundColor.ToMarkup()} bold] {logRecord.Level.ShortName()} [/] {logRecord.Message.Trim()}");
    }

    public static Renderable TableColumn(IRenderable[] content, HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        return new Align(new Columns(content).Collapse(), alignment);
    }

    public static Renderable Widget(this string text)
    {
        return new Markup(text);
    }

    public static string ShortName(this LogLevel level) => level switch
    {
        LogLevel.Warning => "Warn",
        LogLevel.Critical => "Crtcl",
        LogLevel.Information => "Info",
        _ => level.ToString()
    };
}