using System.Collections.ObjectModel;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroFeatures;
using NeuroFeatures.EventArguments;

namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract.Views
{
	/// <summary>
	/// Lists the tokens a contract created, under a heading. Tapping a token opens its details. The view is hidden until
	/// it has tokens to show.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ContractTokensView : BaseContentView
	{
		/// <summary>
		/// The most tokens the view lists.
		/// </summary>
		public const int MaxTokens = 25;

		/// <summary>
		/// ID of the contract whose tokens are listed. The view is hidden when it is null or empty.
		/// </summary>
		public static readonly BindableProperty ContractIdProperty = BindableProperty.Create(
			nameof(ContractId), typeof(string), typeof(ContractTokensView), null, propertyChanged: OnContractIdChanged);

		private readonly ObservableCollection<TokenItem> tokens = [];
		private int loadVersion;

		/// <summary>
		/// Creates a new instance of the <see cref="ContractTokensView"/> class.
		/// </summary>
		public ContractTokensView()
		{
			this.InitializeComponent();
			BindableLayout.SetItemsSource(this.TokenList, this.tokens);
		}

		/// <summary>
		/// ID of the contract whose tokens are listed. The view is hidden when it is null or empty.
		/// </summary>
		public string? ContractId
		{
			get => (string?)this.GetValue(ContractIdProperty);
			set => this.SetValue(ContractIdProperty, value);
		}

		/// <summary>
		/// Loads the tokens of the new contract.
		/// </summary>
		private static void OnContractIdChanged(BindableObject Bindable, object? OldValue, object? NewValue)
		{
			if (Bindable is ContractTokensView View)
				MainThread.BeginInvokeOnMainThread(async () => await View.LoadAsync(NewValue as string));
		}

		/// <summary>
		/// Loads the tokens a contract created from its Neuron, and shows them. Called on the main thread. A load that
		/// a newer one has replaced is ignored, and a failed load leaves the view hidden.
		/// </summary>
		/// <param name="ContractId">ID of the contract, or null to show nothing.</param>
		private async Task LoadAsync(string? ContractId)
		{
			int Version = ++this.loadVersion;

			this.tokens.Clear();
			this.IsVisible = false;

			if (string.IsNullOrEmpty(ContractId))
				return;

			try
			{
				TokensEventArgs e = await ServiceRef.XmppService.GetNeuroFeaturesForContract(ContractId, 0, MaxTokens);

				if (Version != this.loadVersion)
					return;

				if (!e.Ok)
				{
					ServiceRef.LogService.LogWarning("Tokens created by contract " + ContractId + " could not be loaded: " + e.ErrorText);
					return;
				}

				foreach (Token Token in e.Tokens ?? [])
				{
					ServiceRef.TokenNameCache.Remember(Token);
					this.tokens.Add(new TokenItem(Token) { Open = OpenTokenAsync });
				}

				this.IsVisible = this.tokens.Count > 0;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Opens a token's details, checking the token with its Neuron first.
		/// </summary>
		/// <param name="Item">The token that was tapped.</param>
		private static Task OpenTokenAsync(TokenItem Item)
		{
			return ServiceRef.NeuroWalletOrchestratorService.OpenTokenAsync(Item.TokenId, null);
		}
	}
}
