using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages.Main.Calculator;
using System.ComponentModel;
using Waher.Content;

namespace NeuroAccessMaui.UI.Pages.Wallet.BuyEDaler
{
	/// <summary>
	/// The view model to bind to for buying eDaler.
	/// </summary>
	public partial class BuyEDalerViewModel : XmppViewModel
	{
		private TaskCompletionSource<decimal?>? result;
		private bool buyButtonPressed = false;

		/// <summary>
		/// Creates an instance of the <see cref="BuyEDalerViewModel"/> class.
		/// </summary>
		public BuyEDalerViewModel()
			: base()
		{
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (ServiceRef.UiService.TryGetArgs(out BuyEDalerNavigationArgs? Args))
			{
				this.Currency = Args.Currency;
				this.result = Args.Result;
			}

			this.Amount = 0;
			this.AmountText = string.Empty;
			this.AmountOk = false;
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.result?.TrySetResult(this.buyButtonPressed ? this.Amount : null);

			await base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// Amount of eDaler to process
		/// </summary>
		[ObservableProperty]
		private decimal amount;

		/// <summary>
		/// If <see cref="Amount"/> is OK.
		/// </summary>
		[ObservableProperty]
		private bool amountOk;

		/// <summary>
		/// <see cref="Amount"/> as text.
		/// </summary>
		[ObservableProperty]
		private string? amountText;

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			switch (e.PropertyName)
			{
				case nameof(this.AmountText):
					if (CommonTypes.TryParse(this.AmountText, out decimal d) && d > 0)
					{
						this.Amount = d;
						this.AmountOk = true;
					}
					else
						this.AmountOk = false;
					break;
			}
		}

		/// <summary>
		/// Currency of <see cref="Amount"/>.
		/// </summary>
		[ObservableProperty]
		private string? currency;

		#endregion

		/// <summary>
		/// Opens the calculator for calculating the value of a numerical property.
		/// </summary>
		/// <param name="Parameter">Property to calculate</param>
		[RelayCommand]
		public async Task OpenCalculator(object Parameter)
		{
			try
			{
				switch (Parameter?.ToString())
				{
					case "AmountText":
						CalculatorNavigationArgs Args = new(this, nameof(this.AmountText));

						await ServiceRef.UiService.GoToAsync(nameof(CalculatorPage), Args, BackMethod.Pop);
						break;
				}
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// The command to bind to for buying eDaler.
		/// </summary>
		[RelayCommand(CanExecute = nameof(AmountOk))]
		private async Task Buy()
		{
			this.buyButtonPressed = true;
			await ServiceRef.UiService.GoBackAsync();
		}

	}
}
