using Spectre.Console;

namespace PackageSmith.App.UX;

public static class StateMachine
{
	public static int Run()
	{
		while (true)
		{
			AnsiConsole.Clear();
			ShowMenu();

			var choice = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("[steelblue]Select an option:[/]")
					.AddChoices(new[]
					{
						"New Package",
						"Settings",
						"Exit"
					}));

			switch (choice)
			{
				case "New Package":
					NewPackageFlow();
					break;
				case "Settings":
					SettingsFlow();
					break;
				case "Exit":
					return 0;
			}
		}
	}

	private static void ShowMenu()
	{
		var panel = new Panel("[steelblue]PackageSmith Main Menu[/]")
			.Border(BoxBorder.Rounded)
			.Header("[steelblue1 bold]PKSMITH[/]", Justify.Center)
			.Expand();

		AnsiConsole.Write(panel);
		AnsiConsole.WriteLine();
	}

	private static void NewPackageFlow()
	{
		AnsiConsole.MarkupLine("[steelblue]Create New Package[/]");
		AnsiConsole.Prompt(new TextPrompt<string>("Press [steelblue]Enter[/] to continue...")
			.AllowEmpty());
	}

	private static void SettingsFlow()
	{
		AnsiConsole.MarkupLine("[steelblue]Settings[/]");
		AnsiConsole.Prompt(new TextPrompt<string>("Press [steelblue]Enter[/] to continue...")
			.AllowEmpty());
	}
}
