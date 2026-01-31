using Spectre.Console;

namespace PackageSmith.UI;

public static class ProgressManager
{
    public static void ShowProgress(string title, Action<IProgressContext> action)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        AnsiConsole.Markup($"\r[{StyleManager.Dim.ToMarkup()}]{title}...[/]");
        AnsiConsole.Write("\x1b[?25l"); // Hide cursor

        try
        {
            action(new ProgressContext());
        }
        finally
        {
            stopwatch.Stop();
            AnsiConsole.Write("\x1b[?25h"); // Show cursor
            AnsiConsole.MarkupLine($"\r[{StyleManager.Dim.ToMarkup()}]{title}...[/] [{StyleManager.Success.ToMarkup()}{StyleManager.Success} Done ({FormatDuration(stopwatch.Elapsed)})[/]\n");
        }
    }

    public static T ShowProgress<T>(string title, Func<IProgressContext, T> action)
    {
        T? result = default;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        AnsiConsole.Markup($"\r[{StyleManager.Dim.ToMarkup()}]{title}...[/]");
        AnsiConsole.Write("\x1b[?25l"); // Hide cursor

        try
        {
            result = action(new ProgressContext());
        }
        finally
        {
            stopwatch.Stop();
            AnsiConsole.Write("\x1b[?25h"); // Show cursor
            AnsiConsole.MarkupLine($"\r[{StyleManager.Dim.ToMarkup()}]{title}...[/] [{StyleManager.Success.ToMarkup()}]{StyleManager.Success} Done ({FormatDuration(stopwatch.Elapsed)})[/]\n");
        }

        return result!;
    }

    public static void ShowProgressMultiple(string title, Dictionary<string, Action<ProgressTask>> tasks)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        AnsiConsole.Markup($"\r[{StyleManager.Dim.ToMarkup()}]{title}...[/]");
        AnsiConsole.Write("\x1b[?25l"); // Hide cursor

        try
        {
            foreach (var (name, action) in tasks)
            {
                action(null!);
            }
        }
        finally
        {
            stopwatch.Stop();
            AnsiConsole.Write("\x1b[?25h"); // Show cursor
            AnsiConsole.MarkupLine($"\r[{StyleManager.Dim.ToMarkup()}]{title}...[/] [{StyleManager.Success.ToMarkup()}]{StyleManager.Success} Done ({FormatDuration(stopwatch.Elapsed)})[/]\n");
        }
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
            return $"{duration.TotalMilliseconds:F0}ms";
        if (duration.TotalMinutes < 1)
            return $"{duration.TotalSeconds:F1}s";
        return $"{duration.TotalMinutes:F1}m";
    }
}

public interface IProgressContext
{
    ProgressTask? Task { get; }
    void Increment(double amount = 1);
    void SetValue(double value);
    void SetStatus(string status);
}

public sealed class ProgressContext : IProgressContext
{
    public ProgressTask? Task { get; }

    public ProgressContext(ProgressTask task = null!)
    {
        Task = task;
    }

    public void Increment(double amount = 1) => Task?.Increment(amount);
    public void SetValue(double value)
    {
        if (Task != null) Task.Value = value;
    }
    public void SetStatus(string status)
    {
        // In-place status updates not available in Spectre.Console ProgressTask
        // This is a no-op for the minimal progress style
    }
}
