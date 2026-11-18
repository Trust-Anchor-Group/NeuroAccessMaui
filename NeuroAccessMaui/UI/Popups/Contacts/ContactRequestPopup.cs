using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.UI.Controls;
using Waher.Networking.XMPP.Abuse;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Popups.Contacts
{
	/// <summary>
	/// Shows a request from someone who wants to add the user: who they are, what accepting means, and Accept, Not now, or
	/// Block or report.
	/// </summary>
	public sealed class ContactRequestPopup : ContactSheetPopup
	{
		private readonly ContactRequest request;

		/// <summary>
		/// Creates the sheet.
		/// </summary>
		/// <param name="Request">The request.</param>
		public ContactRequestPopup(ContactRequest Request)
		{
			this.request = Request;

			Grid Top = new() { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
			Top.Add(this.CreateCloseButton(), 1, 0);
			this.Add(Top);

			VerticalStackLayout Who = new()
			{
				Spacing = 10,
				Margin = new Thickness(8, -48, 8, 0),
				HorizontalOptions = LayoutOptions.Center,
				Children = { CreateAvatar(Request) }
			};

			Label Title = new()
			{
				Text = ServiceRef.Localizer[nameof(AppResources.ContactRequestWantsToAddYou), Request.Name],
				FontFamily = "SpaceGroteskBold",
				FontSize = 24,
				HorizontalTextAlignment = TextAlignment.Center,
				LineBreakMode = LineBreakMode.WordWrap
			};
			Title.SetDynamicResource(Label.TextColorProperty, "ContentPrimaryWL");
			SemanticProperties.SetHeadingLevel(Title, SemanticHeadingLevel.Level1);
			Who.Add(Title);

			if (Request.IsVerified)
				Who.Add(CreateVerifiedChip());

			Label Explanation = CreateText(ServiceRef.Localizer[nameof(AppResources.ContactRequestExplanation)], 16, false);
			Explanation.HorizontalTextAlignment = TextAlignment.Center;
			Who.Add(Explanation);

			this.Add(Who);

			List<View> Facts = [];

			if (Request.Identity is not null)
			{
				string Provider = ProviderName(Request.Identity);
				if (!string.IsNullOrEmpty(Provider))
					Facts.Add(CreateFact(ServiceRef.Localizer[nameof(AppResources.ContactRequestVerifiedBy)], Provider));

				string Shared = SharedFields(Request.Identity);
				if (!string.IsNullOrEmpty(Shared))
					Facts.Add(CreateFact(ServiceRef.Localizer[nameof(AppResources.ContactRequestSharesWithYou)], Shared));
			}

			if (Facts.Count > 0)
				this.Add(CreateFactsCard(Facts));

			Grid Buttons = new()
			{
				ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
				ColumnSpacing = 10
			};
			Buttons.Add(this.CreateButton(ServiceRef.Localizer[nameof(AppResources.ContactRequestNotNow)], false, () => this.CloseAsync()), 0, 0);
			Buttons.Add(this.CreateButton(ServiceRef.Localizer[nameof(AppResources.ContactRequestAccept)], true, () => this.CloseAsync(this.AcceptAsync)), 1, 0);
			this.Add(Buttons);

			this.Add(this.CreateBlockLink());
		}

		/// <summary>
		/// Shows a request.
		/// </summary>
		/// <param name="Request">The request.</param>
		public static Task ShowAsync(ContactRequest Request)
		{
			return ShowSheetAsync(new ContactRequestPopup(Request));
		}

		private async Task AcceptAsync()
		{
			await ContactRequests.AcceptAsync(this.request);
			await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.ContactRequestAccepted), this.request.Name]);
		}

		private async Task BlockOrReportAsync()
		{
			BlockChoice? Choice = await BlockOrReportPopup.ShowAsync(this.request.Name);
			if (!Choice.HasValue)
				return;

			ReportingReason? Reason = Choice.Value switch
			{
				BlockChoice.ReportSpam => ReportingReason.Spam,
				BlockChoice.ReportAbuse => ReportingReason.Abuse,
				BlockChoice.ReportOther => ReportingReason.Other,
				_ => null
			};

			await ContactRequests.BlockAsync(this.request, Reason);
			await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.ContactBlocked), this.request.Name]);
		}

		private static View CreateAvatar(ContactRequest Request)
		{
			Border Avatar = new()
			{
				WidthRequest = 88,
				HeightRequest = 88,
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 44 },
				HorizontalOptions = LayoutOptions.Center
			};
			Avatar.SetDynamicResource(VisualElement.BackgroundColorProperty, "TnPSuccessbgWL");
			AutomationProperties.SetIsInAccessibleTree(Avatar, false);

			if (!string.IsNullOrEmpty(Request.PhotoFile) && File.Exists(Request.PhotoFile))
			{
				Avatar.Content = new Microsoft.Maui.Controls.Image
				{
					Source = ImageSource.FromFile(Request.PhotoFile),
					Aspect = Aspect.AspectFill
				};
			}
			else
			{
				Label Initials = new()
				{
					Text = InitialsOf(Request.Name),
					FontFamily = "SpaceGroteskSemiBold",
					FontSize = 30,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				};
				Initials.SetDynamicResource(Label.TextColorProperty, "ContentAccessWL");
				Avatar.Content = Initials;
			}

			return Avatar;
		}

		private static View CreateVerifiedChip()
		{
			Microsoft.Maui.Controls.Shapes.Path Shield = new()
			{
				Data = (Geometry)new PathGeometryConverter().ConvertFromInvariantString("M12 2l8 3v6.5c0 5-3.4 8.7-8 10.5-4.6-1.8-8-5.5-8-10.5V5l8-3z M8 12l3 3 5-6")!,
				Aspect = Stretch.Uniform,
				WidthRequest = 16,
				HeightRequest = 16,
				StrokeThickness = 2,
				StrokeLineCap = PenLineCap.Round,
				StrokeLineJoin = PenLineJoin.Round,
				VerticalOptions = LayoutOptions.Center
			};
			Shield.SetDynamicResource(Microsoft.Maui.Controls.Shapes.Shape.StrokeProperty, "ContentAccessWL");

			Label Text = new()
			{
				Text = ServiceRef.Localizer[nameof(AppResources.ContactIdVerified)],
				FontFamily = "HaasGroteskMedium",
				FontSize = 14,
				VerticalOptions = LayoutOptions.Center
			};
			Text.SetDynamicResource(Label.TextColorProperty, "ContentAccessWL");

			Border Chip = new()
			{
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 15 },
				Padding = new Thickness(12, 5),
				HorizontalOptions = LayoutOptions.Center,
				Content = new HorizontalStackLayout { Spacing = 6, Children = { Shield, Text } }
			};
			Chip.SetDynamicResource(VisualElement.BackgroundColorProperty, "TnPSuccessbgWL");

			return Chip;
		}

		private static View CreateFact(string Label, string Value)
		{
			VerticalStackLayout Fact = new()
			{
				Padding = new Thickness(16, 10),
				Spacing = 2,
				Children = { CreateText(Label, 14, true), CreateText(Value, 16, false) }
			};

			return Fact;
		}

		private static View CreateFactsCard(List<View> Facts)
		{
			VerticalStackLayout Stack = new();

			for (int i = 0; i < Facts.Count; i++)
			{
				if (i > 0)
				{
					BoxView Line = new() { HeightRequest = 1, Margin = new Thickness(16, 0, 0, 0) };
					Line.SetDynamicResource(BoxView.ColorProperty, "SurfaceDividerWL");
					Stack.Add(Line);
				}

				Stack.Add(Facts[i]);
			}

			Border Card = new()
			{
				StrokeThickness = 1,
				StrokeShape = new RoundRectangle { CornerRadius = 20 },
				Content = Stack
			};
			Card.SetDynamicResource(Border.StrokeProperty, "SurfaceDividerWL");
			Card.SetDynamicResource(VisualElement.BackgroundColorProperty, "SurfaceBackgroundWL");

			return Card;
		}

		private View CreateButton(string Text, bool Primary, Func<Task> Action)
		{
			Label Caption = new()
			{
				Text = Text,
				FontFamily = "HaasGroteskMedium",
				FontSize = 16,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
				LineBreakMode = LineBreakMode.WordWrap
			};
			Caption.SetDynamicResource(Label.TextColorProperty, Primary ? "ButtonAccessPrimaryContentWL" : "ContentAccessWL");

			Border Body = new()
			{
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 14 },
				Padding = new Thickness(12, 8),
				MinimumHeightRequest = 52,
				InputTransparent = true,
				Content = Caption
			};
			Body.SetDynamicResource(VisualElement.BackgroundColorProperty, Primary ? "ButtonAccessPrimarybgWL" : "TnPSuccessbgWL");

			// A command, which the button keeps. The button's Clicked event only keeps weak references to its handlers, so a
			// handler that captures the action could be collected before the tap.
			TemplatedButton Button = new()
			{
				Content = Body,
				Command = new AsyncRelayCommand(Action)
			};
			SemanticProperties.SetDescription(Button, Text);

			return Button;
		}

		private View CreateBlockLink()
		{
			Label Caption = new()
			{
				Text = ServiceRef.Localizer[nameof(AppResources.ContactBlockOrReport)],
				FontFamily = "HaasGroteskMedium",
				FontSize = 16,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center
			};
			Caption.SetDynamicResource(Label.TextColorProperty, "TnPDangerContentWL");

			TemplatedButton Button = new()
			{
				MinimumHeightRequest = 48,
				Content = new Grid { MinimumHeightRequest = 48, InputTransparent = true, Children = { Caption } }
			};
			SemanticProperties.SetDescription(Button, Caption.Text);
			Button.Clicked += async (Sender, e) => await this.CloseAsync(this.BlockOrReportAsync);

			return Button;
		}

		/// <summary>
		/// The name of the service that checked an ID, from its address.
		/// </summary>
		private static string ProviderName(LegalIdentity Identity)
		{
			string Provider = Identity.Provider ?? string.Empty;
			int i = Provider.IndexOf('@');
			if (i >= 0)
				Provider = Provider[(i + 1)..];

			if (Provider.StartsWith("legal.", StringComparison.OrdinalIgnoreCase))
				Provider = Provider[6..];

			return Provider;
		}

		/// <summary>
		/// What an ID shares, as a short list of plain words.
		/// </summary>
		private static string SharedFields(LegalIdentity Identity)
		{
			List<string> Fields = [];

			if (!string.IsNullOrEmpty(Identity[Constants.XmppProperties.FirstName]) || !string.IsNullOrEmpty(Identity[Constants.XmppProperties.LastNames]) ||
				!string.IsNullOrEmpty(Identity[Constants.XmppProperties.OrgName]))
			{
				Fields.Add(ServiceRef.Localizer[nameof(AppResources.ContactImportFieldName)]);
			}

			if (Identity.Attachments?.Any(A => A.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? false) ?? false)
				Fields.Add(ServiceRef.Localizer[nameof(AppResources.ContactImportFieldPhoto)]);

			if (!string.IsNullOrEmpty(Identity[Constants.XmppProperties.Phone]))
				Fields.Add(ServiceRef.Localizer[nameof(AppResources.ContactImportFieldPhone)]);

			if (!string.IsNullOrEmpty(Identity[Constants.XmppProperties.EMail]))
				Fields.Add(ServiceRef.Localizer[nameof(AppResources.ContactImportFieldEmail)]);

			if (!string.IsNullOrEmpty(Identity[Constants.XmppProperties.Address]) || !string.IsNullOrEmpty(Identity[Constants.XmppProperties.City]))
				Fields.Add(ServiceRef.Localizer[nameof(AppResources.ContactImportFieldAddress)]);

			if (!string.IsNullOrEmpty(Identity[Constants.XmppProperties.Country]))
				Fields.Add(ServiceRef.Localizer[nameof(AppResources.ContactFieldCountry)]);

			string Text = string.Join(", ", Fields);
			return Text.Length > 0 ? char.ToUpper(Text[0], CultureInfo.CurrentCulture) + Text[1..] : Text;
		}

		/// <summary>
		/// The initials of a name: the first letters of its first two words.
		/// </summary>
		private static string InitialsOf(string Name)
		{
			string[] Words = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			string Result = string.Concat(Words.Take(2).Select(Word => char.ToUpperInvariant(Word[0])));
			return Result.Length > 0 ? Result : "?";
		}
	}
}
