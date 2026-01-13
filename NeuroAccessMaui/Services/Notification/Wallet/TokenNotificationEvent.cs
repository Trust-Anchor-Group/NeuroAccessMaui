using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroFeatures;
using System.Text;
using System.Xml;
using Waher.Persistence.Attributes;
using NeuroAccessMaui.Services.UI;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.UI;
using NeuroFeatures.EventArguments;

namespace NeuroAccessMaui.Services.Notification.Wallet
{
	/// <summary>
	/// Abstract base class for token notification events.
	/// </summary>
	public abstract class TokenNotificationEvent : WalletNotificationEvent
	{
		private Token? token;
		private Task<Token?>? tokenParseTask;

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
			get => this.token;

			set
			{
				this.token = value;
				this.tokenParseTask = null;

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
		/// Gets the parsed token asynchronously, using cached XML if available.
		/// </summary>
		/// <returns>The parsed token, or null if parsing fails or no XML is available.</returns>
		public async Task<Token?> GetTokenAsync()
		{
			if (this.token is not null)
				return this.token;

			if (string.IsNullOrEmpty(this.TokenXml))
				return null;

			if (this.tokenParseTask is null)
				this.tokenParseTask = this.ParseTokenAsync();

			Token? ParsedToken = await this.tokenParseTask;
			if (ParsedToken is not null)
				this.token = ParsedToken;

			return this.token;
		}

		private async Task<Token?> ParseTokenAsync()
		{
			if (string.IsNullOrEmpty(this.TokenXml))
				return null;

			XmlDocument Doc = new()
			{
				PreserveWhitespace = true
			};
			Doc.LoadXml(this.TokenXml);

			return await Token.TryParse(Doc.DocumentElement);
		}

		/// <summary>
		/// Gets an icon for the category of event.
		/// </summary>
		/// <returns>Icon</returns>
		public override Task<Geometry> GetCategoryIcon()
		{
			return Task.FromResult(Geometries.TokenIconPath);
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
			if (string.IsNullOrEmpty(this.TokenId))
				return;

			Token? Token = await this.GetTokenAsync();
			if (Token is null)
			{
				Token = await ServiceRef.XmppService.GetNeuroFeature(this.TokenId);
				this.token = Token;
			}

			if (Token is null)
				return;

			if (!ServiceRef.NotificationService.TryGetNotificationEvents(NotificationEventType.Wallet, this.TokenId, out NotificationEvent[]? Events))
				Events = [];

			TokenDetailsNavigationArgs Args = new(new TokenItem(Token, Events));

			await ServiceRef.NavigationService.GoToAsync(nameof(TokenDetailsPage), Args, BackMethod.Pop);
		}
	}
}
