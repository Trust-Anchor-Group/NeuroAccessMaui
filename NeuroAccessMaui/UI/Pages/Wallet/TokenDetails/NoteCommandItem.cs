using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Controls;
using NeuroFeatures.NoteCommands;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// A note command of a token's state machine, shown as a row in the Actions card of token details.
	/// </summary>
	public partial class NoteCommandItem : ObservableObject, IListCardItem
	{
		private readonly Func<NoteCommandItem, Task> run;
		private readonly Func<NoteCommandItem, Task<bool>> isAvailable;

		/// <summary>
		/// A note command of a token's state machine, shown as a row in the Actions card of token details.
		/// </summary>
		/// <param name="Command">The note command. It has passed <see cref="NoteCommandRunner.CanRun"/>.</param>
		/// <param name="AssignedNames">Names the command's scripts assign that aren't parameters.</param>
		/// <param name="Run">Runs the command when the button is tapped.</param>
		/// <param name="IsAvailable">Checks if the command applies in the token's current state.</param>
		public NoteCommandItem(NoteCommand Command, string[] AssignedNames, Func<NoteCommandItem, Task> Run,
			Func<NoteCommandItem, Task<bool>> IsAvailable)
		{
			this.Command = Command;
			this.AssignedNames = AssignedNames;
			this.run = Run;
			this.isAvailable = IsAvailable;
			this.Title = NoteCommandRunner.GetTitle(Command);
			this.ToolTip = NoteCommandRunner.SelectText(Command.ToolTip);
			this.Monogram = GetMonogram(this.Title);
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
		/// The command's title, in the user's language when it has one, without a "..." at the end.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// What the command does, in the user's language when it has one.
		/// </summary>
		public string? ToolTip { get; }

		/// <summary>
		/// If the command says what it does.
		/// </summary>
		public bool HasToolTip => !string.IsNullOrEmpty(this.ToolTip);

		/// <summary>
		/// The first letter of the title, shown on the row's tile.
		/// </summary>
		public string Monogram { get; }

		/// <summary>
		/// What screen readers say about the row: the title, what the command does, and if it asks for values.
		/// </summary>
		public string AccessibleDescription
		{
			get
			{
				string Result = this.HasToolTip ? this.Title + ", " + this.ToolTip : this.Title;

				if (this.HasParameters)
					Result += " " + ServiceRef.Localizer[nameof(AppResources.NoteCommandAsksForValues)];

				return Result;
			}
		}

		/// <summary>
		/// If the command asks the user for values before it runs. The row ends with a chevron.
		/// </summary>
		public bool HasParameters => this.Command.HasParameters;

		/// <summary>
		/// If the command runs when the row is tapped, after its confirmation if it has one. The row ends with "Run".
		/// </summary>
		public bool RunsAtOnce => !this.Command.HasParameters;

		/// <summary>
		/// Where the row sits in the Actions card.
		/// </summary>
		[ObservableProperty]
		private ListCardPosition cardPosition;

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
		/// Checks if the command applies in the token's current state.
		/// </summary>
		/// <returns>If the command applies.</returns>
		public Task<bool> IsAvailableAsync()
		{
			return this.isAvailable(this);
		}

		/// <summary>
		/// Gets the first letter of a title, as one text element so letters made of several characters stay whole.
		/// </summary>
		/// <param name="Title">The title.</param>
		/// <returns>The letter, in upper case, or an empty string.</returns>
		private static string GetMonogram(string Title)
		{
			string Trimmed = Title.Trim();
			if (string.IsNullOrEmpty(Trimmed))
				return string.Empty;

			return StringInfo.GetNextTextElement(Trimmed, 0).ToUpper(CultureInfo.CurrentCulture);
		}

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
