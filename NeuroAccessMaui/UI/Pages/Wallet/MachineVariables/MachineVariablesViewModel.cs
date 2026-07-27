using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Waher.Script;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineVariables
{
	/// <summary>
	/// Displays the current state and variables for one token state-machine.
	/// </summary>
	public partial class MachineVariablesViewModel : BaseViewModel
	{
		private readonly string tokenId;

		/// <summary>
		/// Initializes a state-machine variable view.
		/// </summary>
		/// <param name="Args">Navigation arguments containing the initial state snapshot.</param>
		public MachineVariablesViewModel(MachineVariablesNavigationArgs? Args)
			: base()
		{
			this.Variables = [];
			this.tokenId = Args?.TokenId ?? string.Empty;

			if (Args is null)
			{
				this.HasError = true;
				this.ErrorMessage =
					ServiceRef.Localizer[nameof(AppResources.MachineVariablesUnavailable)];
				return;
			}

			this.Running = Args.Running;
			this.Ended = Args.Ended;
			this.CurrentState = Args.CurrentState;
			this.HasContent = true;

			if (Args.Variables is not null)
			{
				foreach (Variable Variable in Args.Variables)
					this.Variables.Add(new VariableModel(Variable.Name, Variable.ValueObject));
			}

			this.HasVariables = this.Variables.Count > 0;
		}

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			ServiceRef.XmppService.NeuroFeatureVariablesUpdated += this.Wallet_VariablesUpdated;
			ServiceRef.XmppService.NeuroFeatureStateUpdated += this.Wallet_StateUpdated;
		}

		/// <inheritdoc/>
		public override Task OnDisposeAsync()
		{
			ServiceRef.XmppService.NeuroFeatureVariablesUpdated -= this.Wallet_VariablesUpdated;
			ServiceRef.XmppService.NeuroFeatureStateUpdated -= this.Wallet_StateUpdated;

			return base.OnDisposeAsync();
		}

		private Task Wallet_StateUpdated(
			object? Sender,
			NeuroFeatures.EventArguments.NewStateEventArgs EventArguments)
		{
			if (string.IsNullOrEmpty(this.tokenId) ||
				!string.Equals(
					this.tokenId,
					EventArguments.TokenId,
					StringComparison.Ordinal))
			{
				return Task.CompletedTask;
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.CurrentState = EventArguments.NewState;
				this.Ended = string.IsNullOrEmpty(EventArguments.NewState);
				this.Running = !this.Ended;
			});

			return Task.CompletedTask;
		}

		private Task Wallet_VariablesUpdated(
			object? Sender,
			NeuroFeatures.EventArguments.VariablesUpdatedEventArgs EventArguments)
		{
			if (string.IsNullOrEmpty(this.tokenId) ||
				!string.Equals(
					this.tokenId,
					EventArguments.TokenId,
					StringComparison.Ordinal))
			{
				return Task.CompletedTask;
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				foreach (Variable Variable in EventArguments.Variables)
				{
					if (this.TryGetVariableModel(Variable.Name, out VariableModel? Model))
						Model.UpdateValue(Variable.ValueObject);
					else
						this.Variables.Add(new VariableModel(Variable.Name, Variable.ValueObject));
				}

				this.HasVariables = this.Variables.Count > 0;
			});

			return Task.CompletedTask;
		}

		private bool TryGetVariableModel(
			string Name,
			[NotNullWhen(true)] out VariableModel? Result)
		{
			foreach (VariableModel Model in this.Variables)
			{
				if (Model.Name == Name)
				{
					Result = Model;
					return true;
				}
			}

			Result = null;
			return false;
		}

		/// <summary>
		/// Gets the current variables.
		/// </summary>
		public ObservableCollection<VariableModel> Variables { get; }

		/// <summary>
		/// Gets or sets a value indicating whether state information is available.
		/// </summary>
		[ObservableProperty]
		private bool hasContent;

		/// <summary>
		/// Gets or sets a value indicating whether state information is unavailable.
		/// </summary>
		[ObservableProperty]
		private bool hasError;

		/// <summary>
		/// Gets or sets the localized unavailable-state explanation.
		/// </summary>
		[ObservableProperty]
		private string errorMessage = string.Empty;

		/// <summary>
		/// Gets or sets a value indicating whether current variables are available.
		/// </summary>
		[ObservableProperty]
		private bool hasVariables;

		/// <summary>
		/// Gets or sets a value indicating whether the state-machine is running.
		/// </summary>
		[ObservableProperty]
		private bool running;

		/// <summary>
		/// Gets or sets a value indicating whether the state-machine has ended.
		/// </summary>
		[ObservableProperty]
		private bool ended;

		/// <summary>
		/// Gets or sets the current state of the state-machine.
		/// </summary>
		[ObservableProperty]
		private string? currentState;
	}
}
