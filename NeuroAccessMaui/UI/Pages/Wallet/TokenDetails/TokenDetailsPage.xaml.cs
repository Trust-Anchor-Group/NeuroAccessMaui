﻿using System.Globalization;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// A page that allows the user to view information about a token.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TokenDetailsPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="TokenDetailsPage"/> class.
		/// </summary>
		public TokenDetailsPage()
		{
			this.ContentPageModel = new TokenDetailsViewModel(this, ServiceRef.UiService.PopLatestArgs<TokenDetailsNavigationArgs>());
			this.InitializeComponent();
		}

		/// <summary>
		/// Adds a Legal ID record in the parts section.
		/// 
		/// (Grouped collections do not work. Need to do this manually. TODO: MVVC when this is possible.)
		/// </summary>
		/// <param name="Model">View model</param>
		/// <param name="Label">Label</param>
		/// <param name="FriendlyName">Friendly name</param>
		/// <param name="LegalId">Legal ID</param>
		internal void AddLegalId(TokenDetailsViewModel Model, string Label, string FriendlyName, string LegalId)
		{
			int Row = this.PartsGrid.RowDefinitions.Count;
			Label Lbl;

			this.PartsGrid.RowDefinitions.Add(new RowDefinition()
			{
				Height = GridLength.Auto
			});

			this.PartsGrid.Add(new Label()
			{
				Text = Label,
				Style = AppStyles.KeyLabel
			}, 0, Row);

			this.PartsGrid.Add(Lbl = new Label()
			{
				Text = FriendlyName,
				Style = AppStyles.ClickableValueLabel
			}, 1, Row);

			TapGestureRecognizer Tap = new();
			Lbl.GestureRecognizers.Add(Tap);
			Tap.Command = Model.ViewIdCommand;
			Tap.CommandParameter = LegalId;
		}

		/// <summary>
		/// Adds a JID record in the parts section.
		/// 
		/// (Grouped collections do not work. Need to do this manually. TODO: MVVC when this is possible.)
		/// </summary>
		/// <param name="Model">View model</param>
		/// <param name="Label">Label</param>
		/// <param name="Jid">XMPP Network address (JID)</param>
		/// <param name="LegalId">Legal ID</param>
		/// <param name="FriendlyName">Friendly name</param>
		internal void AddJid(TokenDetailsViewModel Model, string Label, string Jid, string LegalId, string FriendlyName)
		{
			int Row = this.PartsGrid.RowDefinitions.Count;
			Label Lbl;

			this.PartsGrid.RowDefinitions.Add(new RowDefinition()
			{
				Height = GridLength.Auto
			});

			this.PartsGrid.Add(new Label()
			{
				Text = Label,
				Style = AppStyles.KeyLabel
			}, 0, Row);

			this.PartsGrid.Add(Lbl = new Label()
			{
				Text = Jid,
				Style = AppStyles.ClickableValueLabel
			}, 1, Row);

			TapGestureRecognizer Tap = new();
			Lbl.GestureRecognizers.Add(Tap);
			Tap.Command = Model.OpenChatCommand;
			Tap.CommandParameter = Jid + " | " + LegalId + " | " + FriendlyName;
		}

		/// <summary>
		/// Adds a clickable link record in the parts section.
		/// 
		/// (Grouped collections do not work. Need to do this manually. TODO: MVVC when this is possible.)
		/// </summary>
		/// <param name="Model">View model</param>
		/// <param name="Label">Label</param>
		/// <param name="LinkUri">Link URI</param>
		internal void AddLink(TokenDetailsViewModel Model, string Label, string LinkUri)
		{
			int Row = this.GeneralInfoGrid.RowDefinitions.Count;
			Label Lbl;

			this.GeneralInfoGrid.RowDefinitions.Add(new RowDefinition()
			{
				Height = GridLength.Auto
			});

			this.GeneralInfoGrid.Add(new Label()
			{
				Text = Label,
				Style = AppStyles.KeyLabel
			}, 0, Row);

			this.GeneralInfoGrid.Add(Lbl = new Label()
			{
				Text = LinkUri,
				Style = AppStyles.ClickableValueLabel
			}, 1, Row);

			TapGestureRecognizer Tap = new();
			Lbl.GestureRecognizers.Add(Tap);
			Tap.Command = Model.OpenLinkCommand;
			Tap.CommandParameter = LinkUri;
		}

		/// <summary>
		/// Adds a tag name/value pair.
		/// 
		/// (Grouped collections do not work. Need to do this manually. TODO: MVVC when this is possible.)
		/// </summary>
		/// <param name="Model">View model</param>
		/// <param name="Label">Label</param>
		/// <param name="Value">Value</param>
		internal void AddTag(TokenDetailsViewModel Model, string Label, string Value)
		{
			int Row = this.GeneralInfoGrid.RowDefinitions.Count;
			Label Lbl;

			this.GeneralInfoGrid.RowDefinitions.Add(new RowDefinition()
			{
				Height = GridLength.Auto
			});

			this.GeneralInfoGrid.Add(new Label()
			{
				Text = Label,
				Style = AppStyles.KeyLabel
			}, 0, Row);

			this.GeneralInfoGrid.Add(Lbl = new Label()
			{
				Text = Value,
				Style = AppStyles.ValueLabel
			}, 1, Row);

			if (Uri.TryCreate(Value, UriKind.Absolute, out Uri? RefUri) &&
				RefUri.Scheme.ToLower(CultureInfo.InvariantCulture) is string s &&
				(s == "http" || s == "https"))
			{
				Lbl.Style = AppStyles.ClickableValueLabel;

				TapGestureRecognizer Tap = new();
				Lbl.GestureRecognizers.Add(Tap);
				Tap.Command = Model.OpenLinkCommand;
				Tap.CommandParameter = Value;
			}
		}

	}
}
