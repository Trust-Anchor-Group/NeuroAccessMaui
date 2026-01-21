using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services;
using Waher.Script;

namespace NeuroAccessMaui.UI.Pages.Utility
{
	/// <summary>
	/// View model for an interactive script console that evaluates user-provided Waher.Script expressions
	/// and collects the resulting output.
	/// </summary>
	public partial class ScriptConsoleViewModel : BaseViewModel
	{
		/// <summary>
		/// All the resulting output for executed script promtps
		/// </summary>
		public ObservableCollection<string> PromptResults { get; } = [];

		/// <summary>
		/// Script input from the user
		/// </summary>
		[ObservableProperty]
		private string promptInput = "";

		private Variables variables = new Variables([]);

		/// <summary>
		/// Called when the view is appearing.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
		}

		/// <summary>
		/// Navigates back to the previous page.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		[RelayCommand]
		private async Task Back()
		{
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				await ServiceRef.NavigationService.GoBackAsync();
			});
		}

		/// <summary>
		/// Clears the current script output.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		[RelayCommand]
		private Task ClearOutput()
		{
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.PromptResults.Clear();
			});
		}

		/// <summary>
		/// Parses and executes the current prompt input (if any) and appends the result to the output.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		[RelayCommand]
		private async Task ExecutePrompt()
		{
			if (string.IsNullOrEmpty(this.PromptInput))
				return;

			string StrResult = "";
			try
			{
				object Result = await ScriptExtentions.SafeExecute(this.PromptInput, this.variables);
				StrResult = Result.ToString() ?? "";
			}
			catch (Exception e)
			{
				StrResult = e.Message;
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.PromptInput = "";
				this.PromptResults.Add(StrResult);
			});
		}
	}
}
