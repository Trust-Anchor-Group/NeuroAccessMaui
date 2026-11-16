using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Content;
using Waher.Script;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineVariables
{
	/// <summary>
	/// The view model for the page with a token's values now, grouped into money, dates and times, people and links, and
	/// other values. The values update live, and the page can also show the rows of a value that is a list.
	/// </summary>
	public partial class MachineVariablesViewModel : BaseViewModel
	{
		/// <summary>
		/// Words in value names that mean an amount of money.
		/// </summary>
		private static readonly string[] moneyWords =
			["Amount", "Price", "Cost", "Fee", "Total", "Sum", "Payment", "Deposit", "Commission", "Balance", "Invested", "Paid", "Budget"];

		private readonly string? tokenId;

		/// <summary>
		/// The view model for the page with a token's values now.
		/// </summary>
		/// <param name="Args">Navigation arguments</param>
		public MachineVariablesViewModel(MachineVariablesNavigationArgs? Args)
			: base()
		{
			this.Variables = [];
			this.Title = Args?.Title ?? ServiceRef.Localizer[nameof(AppResources.TokenValues)];

			if (Args is not null)
			{
				this.tokenId = Args.TokenId;
				this.Running = Args.Running;
				this.Ended = Args.Ended;
				this.CurrentState = Args.CurrentState;

				if (Args.Variables is not null)
				{
					foreach (Variable Variable in Args.Variables)
						this.Variables.Add(new VariableModel(Variable.Name, Variable.ValueObject));
				}
			}

			this.IsLive = this.tokenId is not null;
			this.UpdateLiveText();
			this.ApplyCurrency();
			this.UpdateGroups();
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

		private bool IsForThisToken(NeuroFeatures.EventArguments.StateMachineEventArgs e)
		{
			return this.tokenId is not null && string.Equals(e.TokenId, this.tokenId, StringComparison.OrdinalIgnoreCase);
		}

		private Task Wallet_StateUpdated(object? Sender, NeuroFeatures.EventArguments.NewStateEventArgs e)
		{
			if (!this.IsForThisToken(e))
				return Task.CompletedTask;

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.CurrentState = e.NewState;
				this.Ended = string.IsNullOrEmpty(e.NewState);
				this.Running = !this.Ended;
				this.UpdateLiveText();
			});

			return Task.CompletedTask;
		}

		private Task Wallet_VariablesUpdated(object? Sender, NeuroFeatures.EventArguments.VariablesUpdatedEventArgs e)
		{
			if (!this.IsForThisToken(e))
				return Task.CompletedTask;

			MainThread.BeginInvokeOnMainThread(() =>
			{
				bool Added = false;

				foreach (Variable Variable in e.Variables)
				{
					if (this.TryGetVariableModel(Variable.Name, out VariableModel? Model))
						Model.ChangeValue(Variable.ValueObject, true);
					else
					{
						this.Variables.Add(new VariableModel(Variable.Name, Variable.ValueObject));
						Added = true;
					}
				}

				this.ApplyCurrency();
				this.UpdateLiveText();

				if (Added)
					this.UpdateGroups();
			});

			return Task.CompletedTask;
		}

		private bool TryGetVariableModel(string Name, [NotNullWhen(true)] out VariableModel? Result)
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
		/// Shows amounts of money in the currency held by a value named "Currency", when there is one.
		/// </summary>
		private void ApplyCurrency()
		{
			string Currency = this.Variables.FirstOrDefault(Model =>
				string.Equals(Model.Name, "Currency", StringComparison.OrdinalIgnoreCase))?.Value as string ?? string.Empty;

			foreach (VariableModel Model in this.Variables)
			{
				if (IsMoney(Model))
					Model.SetUnit(Currency);
			}
		}

		/// <summary>
		/// Says when the values were last updated.
		/// </summary>
		private void UpdateLiveText()
		{
			string Time = DateTime.Now.ToString("t", CultureInfo.CurrentCulture);

			this.LiveText = this.Ended
				? ServiceRef.Localizer[nameof(AppResources.ValuesUpdated), Time]
				: ServiceRef.Localizer[nameof(AppResources.ValuesLiveUpdated), Time];
		}

		/// <summary>
		/// Sorts the values that match the search into their groups.
		/// </summary>
		private void UpdateGroups()
		{
			string Search = this.SearchText.Trim();
			CompareInfo Compare = CultureInfo.CurrentCulture.CompareInfo;

			List<VariableModel> Money = [];
			List<VariableModel> Dates = [];
			List<VariableModel> People = [];
			List<VariableModel> Other = [];

			foreach (VariableModel Model in this.Variables)
			{
				if (Search.Length > 0 &&
					Compare.IndexOf(Model.DisplayName, Search, CompareOptions.IgnoreCase) < 0 &&
					Compare.IndexOf(Model.DisplayValue, Search, CompareOptions.IgnoreCase) < 0)
				{
					continue;
				}

				if (IsMoney(Model))
					Money.Add(Model);
				else if (Model.Value is DateTime or TimeSpan or Duration)
					Dates.Add(Model);
				else if (IsPersonOrLink(Model))
					People.Add(Model);
				else
					Other.Add(Model);
			}

			List<VariableGroup> Groups = [];
			AddGroup(Groups, nameof(AppResources.ValuesMoney), Money);
			AddGroup(Groups, nameof(AppResources.ValuesDates), Dates);
			AddGroup(Groups, nameof(AppResources.ValuesPeople), People);
			AddGroup(Groups, nameof(AppResources.Other), Other);

			this.Groups = Groups;
		}

		/// <summary>
		/// Adds a group, if it has values.
		/// </summary>
		private static void AddGroup(List<VariableGroup> Groups, string TitleKey, List<VariableModel> Items)
		{
			if (Items.Count > 0)
				Groups.Add(new VariableGroup(ServiceRef.Localizer[TitleKey], Items));
		}

		/// <summary>
		/// If a value is an amount of money, judged by its name.
		/// </summary>
		private static bool IsMoney(VariableModel Model)
		{
			return Model.Value is decimal or double or float or int or long &&
				moneyWords.Any(Word => Model.Name.Contains(Word, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// If a value points to someone or something: an address such as an ID, a web link, or a list.
		/// </summary>
		private static bool IsPersonOrLink(VariableModel Model)
		{
			if (Model.IsList)
				return true;

			return Model.Value is string Text && !Text.Contains(' ', StringComparison.Ordinal) &&
				(Text.Contains('@', StringComparison.Ordinal) || Uri.IsWellFormedUriString(Text, UriKind.Absolute));
		}

		/// <summary>
		/// Shows the values that match the search.
		/// </summary>
		/// <param name="Value">The search text.</param>
		partial void OnSearchTextChanged(string Value)
		{
			this.UpdateGroups();
		}

		#region Properties

		/// <summary>
		/// Current variables
		/// </summary>
		public ObservableCollection<VariableModel> Variables { get; }

		/// <summary>
		/// The page title: "Values", or the name of the value whose rows are shown.
		/// </summary>
		[ObservableProperty]
		private string title = string.Empty;

		/// <summary>
		/// If the values are a token's, which update live.
		/// </summary>
		[ObservableProperty]
		private bool isLive;

		/// <summary>
		/// When the values were last updated, such as "Live · updated 14:05".
		/// </summary>
		[ObservableProperty]
		private string liveText = string.Empty;

		/// <summary>
		/// The text to find values by.
		/// </summary>
		[ObservableProperty]
		private string searchText = string.Empty;

		/// <summary>
		/// The values that match the search, in groups.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasNoMatches))]
		private IReadOnlyList<VariableGroup> groups = [];

		/// <summary>
		/// If no value matches the search.
		/// </summary>
		public bool HasNoMatches => this.Groups.Count == 0 && this.Variables.Count > 0;

		/// <summary>
		/// If the state-machine is running
		/// </summary>
		[ObservableProperty]
		private bool running;

		/// <summary>
		/// If the state-machine has ended
		/// </summary>
		[ObservableProperty]
		private bool ended;

		/// <summary>
		/// Current state of state-machine
		/// </summary>
		[ObservableProperty]
		private string? currentState;

		#endregion
	}
}
