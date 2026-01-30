using Spectre.Console;

namespace PackageSmith.UI;

public static class ProgressManager
{
    public static void ShowProgress(string title, Action<IProgressContext> action)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(true)
            .Start(ctx =>
            {
                var task = ctx.AddTask(title)
                    .MaxValue(100);

                action(new ProgressContext(task));
            });
        AnsiConsole.MarkupLine($"[{StyleManager.SuccessColor.ToMarkup()}]{StyleManager.IconSuccess} {title} complete[/]");
        AnsiConsole.WriteLine();
    }

    public static T ShowProgress<T>(string title, Func<IProgressContext, T> action)
    {
        T? result = default;

        AnsiConsole.WriteLine();
        AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(true)
            .Start(ctx =>
            {
                var task = ctx.AddTask(title)
                    .MaxValue(100);

                result = action(new ProgressContext(task));
            });
        AnsiConsole.MarkupLine($"[{StyleManager.SuccessColor.ToMarkup()}]{StyleManager.IconSuccess} {title} complete[/]");
        AnsiConsole.WriteLine();

        return result!;
    }

    public static void ShowProgressMultiple(string title, Dictionary<string, Action<ProgressTask>> tasks)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(true)
            .Start(ctx =>
            {
                var mainTask = ctx.AddTask(title)
                    .MaxValue(tasks.Count * 100);

                var completed = 0;
                foreach (var (name, action) in tasks)
                {
                    var subTask = ctx.AddTask($"  {name}", autoStart: false)
                        .MaxValue(100);

                    subTask.StartTask();

                    action(subTask);

                    completed++;
                    mainTask.Value = completed * 100;
                }
            });
        AnsiConsole.MarkupLine($"[{StyleManager.SuccessColor.ToMarkup()}]{StyleManager.IconSuccess} {title} complete[/]");
        AnsiConsole.WriteLine();
    }
}

public interface IProgressContext
{
    ProgressTask Task { get; }
    void Increment(double amount = 1);
    void SetValue(double value);
    void SetStatus(string status);
}

public sealed class ProgressContext : IProgressContext
{
    public ProgressTask Task { get; }

    public ProgressContext(ProgressTask task)
    {
        Task = task;
    }

    public void Increment(double amount = 1) => Task.Increment(amount);
    public void SetValue(double value) => Task.Value = value;
    public void SetStatus(string status)
    {
        // Spectre.Console ProgressTask doesn't have StatusText
        // We can track status separately if needed
    }
}
