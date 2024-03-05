using System.ComponentModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.EDalerReceived
{
	/// <summary>
	/// The view model to bind to for displaying information about an incoming balance change.
	/// </summary>
	public partial class EDalerReceivedViewModel : XmppViewModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="EDalerReceivedViewModel"/> class.
		/// </summary>
		/// <param name="Args">Navigation arguments</param>
		public EDalerReceivedViewModel(EDalerBalanceNavigationArgs? Args)
			: base()
		{
			this.Amount = Args?.Balance?.Amount;
			this.Currency = Args?.Balance?.Currency;
			this.Timestamp = Args?.Balance?.Timestamp;

			if (Args?.Balance?.Event is null)
			{
				this.HasId = false;
				this.HasFrom = false;
				this.HasMessage = false;
				this.HasChange = false;
			}
			else
			{
				this.Id = Args.Balance?.Event?.TransactionId;
				this.From = Args.Balance?.Event?.Remote;
				this.Message = Args.Balance?.Event?.Message;
				this.Change = Args.Balance?.Event?.Change;

				this.HasId = true;
				this.HasFrom = true;
				this.HasMessage = true;
				this.HasChange = true;
			}

			StringBuilder Url = new();

			Url.Append("https://");
			Url.Append(ServiceRef.TagProfile.Domain);
			Url.Append("/Images/eDalerFront200.png");

			this.EDalerFrontGlyph = Url.ToString();

			Url.Clear();
			Url.Append("https://");
			Url.Append(ServiceRef.TagProfile.Domain);
			Url.Append("/Images/eDalerBack200.png");

			this.EDalerBackGlyph = Url.ToString();
		}

		#region Properties

		/// <summary>
		/// Amount of eDaler to process
		/// </summary>
		[ObservableProperty]
		private decimal? amount;

		/// <summary>
		/// Change in balance represented by current event.
		/// </summary>
		[ObservableProperty]
		private decimal? change;

		/// <summary>
		/// If <see cref="Change"/> is defined.
		/// </summary>
		[ObservableProperty]
		private bool hasChange;

		/// <summary>
		/// Currency of eDaler to process
		/// </summary>
		[ObservableProperty]
		private string? currency;

		/// <summary>
		/// When code was created.
		/// </summary>
		[ObservableProperty]
		private DateTime? timestamp;

		/// <summary>
		/// Globally unique identifier of code
		/// </summary>
		[ObservableProperty]
		private Guid? id;

		/// <summary>
		/// If <see cref="Id"/> is defined.
		/// </summary>
		[ObservableProperty]
		private bool hasId;

		/// <summary>
		/// From who eDaler is to be transferred
		/// </summary>
		[ObservableProperty]
		private string? from;

		/// <summary>
		/// If <see cref="From"/> is defined.
		/// </summary>
		[ObservableProperty]
		private bool hasFrom;

		/// <summary>
		/// Message of eDaler to process
		/// </summary>
		[ObservableProperty]
		private string? message;

		/// <summary>
		/// HasMessage of eDaler to process
		/// </summary>
		[ObservableProperty]
		private bool hasMessage;

		/// <summary>
		/// eDaler glyph URL
		/// </summary>
		[ObservableProperty]
		private string? eDalerFrontGlyph;

		/// <summary>
		/// eDaler glyph URL
		/// </summary>
		[ObservableProperty]
		private string? eDalerBackGlyph;

		#endregion

		/// <inheritdoc/>
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			switch (e.PropertyName)
			{
				case nameof(this.IsConnected):
					this.AcceptCommand.NotifyCanExecuteChanged();
					break;
			}
		}

		/// <summary>
		/// The command to bind to for Accepting a payment
		/// </summary>
		[RelayCommand(CanExecute = nameof(IsConnected))]
		private Task Accept()
		{
			return this.GoBack();
		}
	}
}
