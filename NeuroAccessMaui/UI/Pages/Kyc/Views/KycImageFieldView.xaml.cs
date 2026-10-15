using System.ComponentModel;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Kyc.Views
{
	/// <summary>
	/// Shows a KYC image field as a photo card: a document illustration while empty, and the photo with a check badge once
	/// added.
	/// </summary>
	/// <remarks>
	/// The binding context is an <see cref="ObservableImageField"/>. Code-behind only picks the illustration and plays the
	/// photo-added animation; capture, processing, and validation stay in the field view model.
	/// </remarks>
	public partial class KycImageFieldView : ContentView
	{
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
			this.hadImage = this.field?.HasCapturedImage == true;
			this.Illustration.Source = ResolveIllustration(this.field?.CaptureProfile ?? KycCaptureProfile.Generic);

			if (this.IsLoaded)
				this.Attach();
		}

		/// <summary>
		/// Picks the illustration shown while the field has no photo.
		/// </summary>
		/// <param name="Profile">What the field captures.</param>
		/// <returns>The file name of the SVG illustration.</returns>
		private static string ResolveIllustration(KycCaptureProfile Profile)
		{
			return Profile switch
			{
				KycCaptureProfile.Selfie => "kyc_doc_face.svg",
				KycCaptureProfile.Passport => "kyc_doc_passport.svg",
				KycCaptureProfile.IdCardFront => "kyc_doc_id_front.svg",
				KycCaptureProfile.IdCardBack => "kyc_doc_id_back.svg",
				_ => "kyc_doc_generic.svg"
			};
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
			this.hadImage = this.field.HasCapturedImage;
		}

		private void Detach()
		{
			if (this.field is not null)
				this.field.PropertyChanged -= this.OnFieldPropertyChanged;
		}

		private void OnFieldPropertyChanged(object? Sender, PropertyChangedEventArgs E)
		{
			if (E.PropertyName != nameof(ObservableImageField.HasCapturedImage))
				return;

			this.Dispatcher.Dispatch(() =>
			{
				bool HasImage = this.field?.HasCapturedImage == true;
				bool IsNewPhoto = HasImage && !this.hadImage;
				this.hadImage = HasImage;

				if (IsNewPhoto)
					_ = this.RevealPhotoAsync();
			});
		}

		/// <summary>
		/// Fades the new photo in and pops the check badge to confirm that the photo was added. With reduced motion the
		/// photo and badge appear at once.
		/// </summary>
		/// <returns>A task that completes when the animation ends.</returns>
		private async Task RevealPhotoAsync()
		{
			this.Preview.CancelAnimations();
			this.Badge.CancelAnimations();

			if (MotionPreferences.ReduceMotion)
			{
				this.ResetPhotoVisuals();
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
				this.ResetPhotoVisuals();
			}
		}

		private void ResetPhotoVisuals()
		{
			this.Preview.Opacity = 1;
			this.Preview.Scale = 1;
			this.Badge.Opacity = 1;
			this.Badge.Scale = 1;
		}
	}
}
