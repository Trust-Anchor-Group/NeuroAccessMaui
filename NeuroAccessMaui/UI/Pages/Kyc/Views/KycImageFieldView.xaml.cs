using System.ComponentModel;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Kyc.Views
{
	/// <summary>
	/// Shows a KYC photo field as a card with a document illustration when empty and a preview when filled.
	/// </summary>
	/// <remarks>
	/// The binding context is an <see cref="ObservableImageField"/>. Code-behind only picks the illustration and plays
	/// the photo-added animation; capture and validation stay in the field view model.
	/// </remarks>
	public partial class KycImageFieldView : ContentView
	{
		private const string faceIllustration = "kyc_doc_face.svg";
		private const string passportIllustration = "kyc_doc_passport.svg";
		private const string idFrontIllustration = "kyc_doc_id_front.svg";
		private const string idBackIllustration = "kyc_doc_id_back.svg";
		private const string licenseIllustration = "kyc_doc_license.svg";
		private const string genericIllustration = "kyc_doc_generic.svg";

		private ObservableImageField? field;
		private bool hadImage;

		/// <summary>
		/// Initializes a new instance of the <see cref="KycImageFieldView"/> class.
		/// </summary>
		public KycImageFieldView()
		{
			this.InitializeComponent();
			this.Loaded += this.OnLoaded;
			this.Unloaded += this.OnUnloaded;
		}

		/// <inheritdoc/>
		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();

			this.Detach();
			this.field = this.BindingContext as ObservableImageField;
			this.hadImage = this.field?.HasImage == true;
			this.Illustration.Source = ResolveIllustration(this.field);

			if (this.IsLoaded)
				this.Attach();
		}

		/// <summary>
		/// Picks a document illustration from the field's special type, identifier, and mapping keys.
		/// </summary>
		/// <param name="Field">The photo field.</param>
		/// <returns>The file name of the SVG illustration.</returns>
		private static string ResolveIllustration(ObservableImageField? Field)
		{
			if (Field is null)
				return genericIllustration;

			List<string> Keys = [Field.SpecialType ?? string.Empty, Field.Id];
			foreach (KycMapping Mapping in Field.Mappings)
				Keys.Add(Mapping.Key ?? string.Empty);

			string Hints = string.Join(' ', Keys).ToLowerInvariant();
			bool IsBack = Hints.Contains("back", StringComparison.Ordinal);

			if (Field.CameraOnly || ContainsAny(Hints, "profilephoto", "profile_photo", "selfie", "face", "portrait"))
				return faceIllustration;

			if (Hints.Contains("passport", StringComparison.Ordinal))
				return passportIllustration;

			if (ContainsAny(Hints, "license", "licence"))
				return IsBack ? idBackIllustration : licenseIllustration;

			if (ContainsAny(Hints, "idcard", "id_card", "identitycard", "identity_card"))
				return IsBack ? idBackIllustration : idFrontIllustration;

			return genericIllustration;
		}

		private static bool ContainsAny(string Text, params string[] Values)
		{
			foreach (string Value in Values)
			{
				if (Text.Contains(Value, StringComparison.Ordinal))
					return true;
			}

			return false;
		}

		private static bool IsReducedMotion
		{
			get
			{
				try
				{
					return ServiceRef.Provider?.GetService<IMotionSettings>()?.ReduceMotion == true;
				}
				catch (Exception)
				{
					return false;
				}
			}
		}

		private void OnLoaded(object? Sender, EventArgs E)
		{
			this.Attach();
		}

		private void OnUnloaded(object? Sender, EventArgs E)
		{
			this.Detach();
		}

		private void Attach()
		{
			if (this.field is null)
				return;

			this.field.PropertyChanged -= this.OnFieldPropertyChanged;
			this.field.PropertyChanged += this.OnFieldPropertyChanged;
			this.hadImage = this.field.HasImage;
		}

		private void Detach()
		{
			if (this.field is not null)
				this.field.PropertyChanged -= this.OnFieldPropertyChanged;
		}

		private void OnFieldPropertyChanged(object? Sender, PropertyChangedEventArgs E)
		{
			if (E.PropertyName != nameof(ObservableImageField.HasImage))
				return;

			this.Dispatcher.Dispatch(() =>
			{
				bool HasImage = this.field?.HasImage == true;
				bool IsNewPhoto = HasImage && !this.hadImage;
				this.hadImage = HasImage;

				if (IsNewPhoto)
					_ = this.RevealPhotoAsync();
			});
		}

		/// <summary>
		/// Fades the new photo in and pops the check badge to confirm that the photo was accepted.
		/// </summary>
		/// <returns>A task that completes when the animation ends.</returns>
		private async Task RevealPhotoAsync()
		{
			this.Preview.CancelAnimations();
			this.Badge.CancelAnimations();

			if (IsReducedMotion)
			{
				this.Preview.Opacity = 1;
				this.Badge.Scale = 1;
				return;
			}

			try
			{
				this.Preview.Opacity = 0;
				this.Preview.Scale = 0.96;
				this.Badge.Scale = 0.3;
				this.Badge.Opacity = 0;

				await Task.WhenAll(
					this.Preview.FadeToAsync(1, 260, Easing.CubicOut),
					this.Preview.ScaleToAsync(1, 260, Easing.CubicOut));
				await Task.WhenAll(
					this.Badge.FadeToAsync(1, 120, Easing.CubicOut),
					this.Badge.ScaleToAsync(1, 360, Easing.SpringOut));
			}
			catch (Exception)
			{
				this.Preview.Opacity = 1;
				this.Preview.Scale = 1;
				this.Badge.Opacity = 1;
				this.Badge.Scale = 1;
			}
		}
	}
}
