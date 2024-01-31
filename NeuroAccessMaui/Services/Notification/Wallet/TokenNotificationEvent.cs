using NeuroAccessMaui.Services.Navigation;
using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroFeatures;
using System.Text;
using System.Xml;
using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.Services.Notification.Wallet
{
	/// <summary>
	/// Abstract base class for token notification events.
	/// </summary>
	public abstract class TokenNotificationEvent : WalletNotificationEvent
	{
		private Token? token;

		/// <summary>
		/// Abstract base class for token notification events.
		/// </summary>
		public TokenNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Abstract base class for token notification events.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public TokenNotificationEvent(TokenEventArgs e)
			: base(e)
		{
			this.TokenId = e.Token.TokenId;
			this.FriendlyName = e.Token.FriendlyName;
			this.TokenCategory = e.Token.Category;
			this.token = e.Token;
			this.Category = e.Token.TokenId;

			StringBuilder Xml = new();
			e.Token.Serialize(Xml);
			this.TokenXml = Xml.ToString();
		}

		/// <summary>
		/// Abstract base class for token notification events.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public TokenNotificationEvent(StateMachineEventArgs e)
			: base(e)
		{
			this.TokenId = e.TokenId;
			this.token = null;
			this.TokenXml = null;
			this.Category = e.TokenId;
		}

		/// <summary>
		/// Token ID
		/// </summary>
		public string? TokenId { get; set; }

		/// <summary>
		/// Category of token
		/// </summary>
		public string? TokenCategory { get; set; }

		/// <summary>
		/// Friendly Name of token
		/// </summary>
		public string? FriendlyName { get; set; }

		/// <summary>
		/// XML of token.
		/// </summary>
		public string? TokenXml { get; set; }

		/// <summary>
		/// Parsed token
		/// </summary>
		[IgnoreMember]
		public Token? Token
		{
			get
			{
				if (this.token is null && !string.IsNullOrEmpty(this.TokenXml))
				{
					XmlDocument Doc = new()
					{
						PreserveWhitespace = true
					};
					Doc.LoadXml(this.TokenXml);

					if (Token.TryParse(Doc.DocumentElement, out Token T))
						this.token = T;
				}

				return this.token;
			}

			set
			{
				this.token = value;

				if (value is null)
					this.TokenXml = null;
				else
				{
					StringBuilder Xml = new();
					value.Serialize(Xml);
					this.TokenXml = Xml.ToString();
				}
			}
		}

		/// <summary>
		/// Gets an icon for the category of event.
		/// </summary>
		/// <returns>Icon</returns>
		public override Task<string> GetCategoryIcon()
		{
			return Task.FromResult("✻");	// TODO: SVG icon
		}

		/// <summary>
		/// Gets a descriptive text for the event.
		/// </summary>
		public override Task<string> GetDescription()
		{
			if (!string.IsNullOrEmpty(this.FriendlyName))
				return Task.FromResult(this.FriendlyName);
			else if (!string.IsNullOrEmpty(this.TokenCategory))
				return Task.FromResult(this.TokenCategory);
			else
				return Task.FromResult(this.TokenId ?? string.Empty);
		}

		/// <summary>
		/// Opens the event.
		/// </summary>
		public override async Task Open()
		{
			this.Token ??= await ServiceRef.XmppService.GetNeuroFeature(this.TokenId);

			if (!ServiceRef.NotificationService.TryGetNotificationEvents(NotificationEventType.Wallet, this.TokenId, out NotificationEvent[]? Events))
				Events = [];

			TokenDetailsNavigationArgs Args = new(new TokenItem(this.Token, ServiceReferences, Events));

			await ServiceRef.NavigationService.GoToAsync(nameof(TokenDetailsPage), Args, BackMethod.Pop);
		}
	}
}
