using System.Windows.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails;

namespace NeuroAccessMaui.UI.Popups.Tokens.Flow
{
	/// <summary>
	/// Shows every step of a token's flow, with what moves it on from each, and the step it is in now.
	/// </summary>
	public class TokenFlowPopup : BasicPopup
	{
		/// <summary>
		/// Shows every step of a token's flow.
		/// </summary>
		/// <param name="Steps">The steps, in order, with their ways out described.</param>
		/// <param name="DiagramCommand">Opens the flow as a diagram drawn by the token's Neuron.</param>
		public TokenFlowPopup(IReadOnlyList<TokenFlowStepItem> Steps, ICommand DiagramCommand)
		{
			this.CardMaxHeightFraction = 0.9;

			VerticalStackLayout List = new()
			{
				Spacing = AppStyles.SmallSpacing
			};

			foreach (TokenFlowStepItem Step in Steps)
				List.Add(CreateStepView(Step));

			Grid Buttons = new()
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = GridLength.Star }
				},
				ColumnSpacing = AppStyles.SmallSpacing
			};
			Buttons.Add(new TextButton
			{
				LabelData = ServiceRef.Localizer[nameof(AppResources.FlowDiagram)],
				Style = AppStyles.SecondaryButton,
				Command = new Command(async () =>
				{
					await ServiceRef.PopupService.PopAsync();

					if (DiagramCommand.CanExecute(null))
						DiagramCommand.Execute(null);
				})
			}, 0, 0);
			Buttons.Add(new TextButton
			{
				LabelData = ServiceRef.Localizer[nameof(AppResources.Close)],
				Style = AppStyles.FilledTextButton,
				Command = new Command(async () => await ServiceRef.PopupService.PopAsync())
			}, 1, 0);

			Grid Layout = new()
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Star },
					new RowDefinition { Height = GridLength.Auto }
				},
				RowSpacing = AppStyles.SmallSpacing * 2
			};
			Layout.Add(new Label
			{
				Text = ServiceRef.Localizer[nameof(AppResources.StateDiagram)],
				Style = AppStyles.SectionTitleLabel
			}, 0, 0);
			Layout.Add(new ScrollView { Content = List }, 0, 1);
			Layout.Add(Buttons, 0, 2);

			this.CardContent = Layout;
		}

		/// <summary>
		/// Creates the card of a step: its name, a chip when the token is in it now or the flow ends there, and what moves
		/// it on.
		/// </summary>
		/// <param name="Step">The step.</param>
		/// <returns>The card.</returns>
		private static View CreateStepView(TokenFlowStepItem Step)
		{
			HorizontalStackLayout Title = new()
			{
				Spacing = AppStyles.SmallSpacing
			};
			Title.Add(new Label
			{
				Text = Step.Name,
				Style = AppStyles.SectionTitleLabel,
				VerticalOptions = LayoutOptions.Center
			});

			if (Step.IsCurrent)
				Title.Add(CreateChip(ServiceRef.Localizer[nameof(AppResources.FlowNow)], "TnPSuccessbgWL", "TnPSuccessContentWL"));
			else if (Step.IsEnd)
				Title.Add(CreateChip(ServiceRef.Localizer[nameof(AppResources.FlowEnd)], "TnPInfobgWL", "TnPInfoContentWL"));

			VerticalStackLayout Content = new()
			{
				Spacing = 4,
				Children = { Title }
			};

			if (Step.HasExitsText)
			{
				Content.Add(new Label
				{
					Text = Step.ExitsText,
					Style = AppStyles.InfoLabel,
					LineBreakMode = LineBreakMode.WordWrap
				});
			}

			Border Card = new()
			{
				Padding = new Thickness(12),
				StrokeThickness = Step.IsCurrent ? 2 : 1,
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
				Content = Content
			};
			Card.SetDynamicResource(Border.StrokeProperty, Step.IsCurrent ? "ContentAccessWL" : "SurfaceDividerWL");
			Card.SetDynamicResource(VisualElement.BackgroundColorProperty, "SurfaceElevation1WL");

			return Card;
		}

		/// <summary>
		/// Creates a small rounded label.
		/// </summary>
		private static Border CreateChip(string Text, string BackgroundKey, string TextKey)
		{
			Label Label = new()
			{
				Text = Text,
				FontSize = 12
			};
			Label.SetDynamicResource(Label.TextColorProperty, TextKey);

			Border Chip = new()
			{
				StrokeThickness = 0,
				Padding = new Thickness(8, 2),
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
				VerticalOptions = LayoutOptions.Center,
				Content = Label
			};
			Chip.SetDynamicResource(VisualElement.BackgroundColorProperty, BackgroundKey);

			return Chip;
		}
	}
}
