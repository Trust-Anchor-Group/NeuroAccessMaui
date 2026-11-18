using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Popups.Contacts;

namespace NeuroAccessMaui.UI.Popups.Groups
{
	/// <summary>
	/// A sheet with one or more text fields and a Save button, such as for the user's name in a group, or a group's name and
	/// description. The first field must not be empty.
	/// </summary>
	public sealed class GroupTextEditPopup : ContactSheetPopup
	{
		private readonly TaskCompletionSource<string[]?> result = new();
		private readonly List<Entry> entries = [];
		private readonly TemplatedButton saveButton;

		/// <summary>
		/// Creates the sheet.
		/// </summary>
		/// <param name="Title">The sheet's title.</param>
		/// <param name="Fields">Each field's label and its text to start with.</param>
		public GroupTextEditPopup(string Title, params (string Caption, string Text)[] Fields)
		{
			this.Add(this.CreateHeader(Title, null));

			foreach ((string Caption, string Text) in Fields)
				this.Add(this.CreateField(Caption, Text));

			Label SaveLabel = new()
			{
				Text = ServiceRef.Localizer[nameof(AppResources.ContactSave)].Value,
				FontFamily = "HaasGroteskMedium",
				FontSize = 16,
				TextColor = Colors.White,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};

			Border SaveFrame = new()
			{
				MinimumHeightRequest = 52,
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 14 },
				InputTransparent = true,
				Content = SaveLabel
			};
			SaveFrame.SetDynamicResource(VisualElement.BackgroundColorProperty, "ButtonAccessPrimarybgWL");

			this.saveButton = new TemplatedButton { Content = SaveFrame };
			SemanticProperties.SetDescription(this.saveButton, ServiceRef.Localizer[nameof(AppResources.ContactSave)].Value);
			this.saveButton.Clicked += async (Sender, e) =>
			{
				if (!this.CanSave())
					return;

				this.result.TrySetResult([.. this.entries.Select(Entry => Entry.Text?.Trim() ?? string.Empty)]);
				await this.CloseAsync();
			};

			this.Add(this.saveButton);
			this.UpdateSave();
		}

		/// <summary>
		/// Shows the sheet, and waits for the user to save or close it.
		/// </summary>
		/// <param name="Title">The sheet's title.</param>
		/// <param name="Fields">Each field's label and its text to start with.</param>
		/// <returns>The texts, trimmed, in the order of the fields, or null if the sheet was closed.</returns>
		public static async Task<string[]?> ShowAsync(string Title, params (string Caption, string Text)[] Fields)
		{
			GroupTextEditPopup Sheet = new(Title, Fields);
			await ShowSheetAsync(Sheet);
			return await Sheet.result.Task;
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync()
		{
			this.result.TrySetResult(null);
			return base.OnDisappearingAsync();
		}

		/// <summary>
		/// A label and a text field below it.
		/// </summary>
		private View CreateField(string Caption, string Text)
		{
			Label FieldLabel = CreateText(Caption, 14, true);
			FieldLabel.FontFamily = "HaasGroteskMedium";
			FieldLabel.Margin = new Thickness(4, 0, 0, 6);

			Entry Field = new()
			{
				Text = Text,
				Keyboard = Keyboard.Text,
				FontFamily = "HaasGroteskRegular",
				FontSize = 16,
				BackgroundColor = Colors.Transparent,
				VerticalOptions = LayoutOptions.Center
			};
			Field.SetDynamicResource(Entry.TextColorProperty, "ContentPrimaryWL");
			SemanticProperties.SetDescription(Field, Caption);
			Field.TextChanged += (Sender, e) => this.UpdateSave();
			this.entries.Add(Field);

			Border Frame = new()
			{
				Padding = new Thickness(14, 0),
				MinimumHeightRequest = 48,
				StrokeThickness = 1,
				StrokeShape = new RoundRectangle { CornerRadius = 14 },
				Content = Field
			};
			Frame.SetDynamicResource(Border.StrokeProperty, "SurfaceDividerWL");
			Frame.SetDynamicResource(VisualElement.BackgroundColorProperty, "SurfaceBackgroundWL");

			return new VerticalStackLayout
			{
				Children = { FieldLabel, Frame }
			};
		}

		private bool CanSave()
		{
			return this.entries.Count > 0 && !string.IsNullOrWhiteSpace(this.entries[0].Text);
		}

		private void UpdateSave()
		{
			bool Enabled = this.CanSave();

			if (this.saveButton is not null)
			{
				this.saveButton.IsEnabled = Enabled;
				this.saveButton.Opacity = Enabled ? 1 : 0.4;
			}
		}
	}
}
