using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroFeatures.NoteCommands;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// A note command of a token's state machine, shown as a button in token details.
	/// </summary>
	public partial class NoteCommandItem : ObservableObject
	{
		private readonly Func<NoteCommandItem, Task> run;

		/// <summary>
		/// A note command of a token's state machine, shown as a button in token details.
		/// </summary>
		/// <param name="Command">The note command. Its scripts have passed <see cref="NoteScriptValidator"/>.</param>
		/// <param name="AssignedNames">Names the command's scripts assign that aren't parameters.</param>
		/// <param name="Run">Runs the command when the button is tapped.</param>
		public NoteCommandItem(NoteCommand Command, string[] AssignedNames, Func<NoteCommandItem, Task> Run)
		{
			this.Command = Command;
			this.AssignedNames = AssignedNames;
			this.run = Run;
			this.Title = NoteCommandRunner.SelectText(Command.Title) ?? Command.Id ?? string.Empty;
			this.ToolTip = NoteCommandRunner.SelectText(Command.ToolTip);
		}

		/// <summary>
		/// The note command.
		/// </summary>
		public NoteCommand Command { get; }

		/// <summary>
		/// Names the command's scripts assign that aren't parameters.
		/// </summary>
		public string[] AssignedNames { get; }

		/// <summary>
		/// The command's title, in the user's language when it has one.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// What the command does, in the user's language when it has one.
		/// </summary>
		public string? ToolTip { get; }

		/// <summary>
		/// What screen readers say about the button: the tool tip when there is one, and the title otherwise.
		/// </summary>
		public string AccessibleDescription => this.ToolTip ?? this.Title;

		/// <summary>
		/// If the command asks the user for values before it runs.
		/// </summary>
		public bool HasParameters => this.Command.HasParameters;

		/// <summary>
		/// If the command is running.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(RunCommand))]
		private bool isRunning;

		/// <summary>
		/// If the command can be run now.
		/// </summary>
		public bool CanRun => !this.IsRunning;

		/// <summary>
		/// Runs the command.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanRun))]
		private async Task Run()
		{
			this.IsRunning = true;

			try
			{
				await this.run(this);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				this.IsRunning = false;
			}
		}
	}
}
