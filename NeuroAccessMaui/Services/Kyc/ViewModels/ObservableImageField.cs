using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.UI.Pages.Kyc;
using NeuroAccessMaui.UI.Pages.Utility.Images;

namespace NeuroAccessMaui.Services.Kyc.ViewModels
{
	/// <summary>
	/// Observable image field used by dynamic KYC templates. Holds the photo as Base64 and exposes the presentation state
	/// of the photo card in the form.
	/// </summary>
	public partial class ObservableImageField : ObservableFileField
	{
		/// <summary>
		/// Gets or sets the field value encoded as Base64 image bytes.
		/// </summary>
		public override string? StringValue
		{
			get => this.RawValue as string;
			set
			{
				if (string.IsNullOrWhiteSpace(value))
				{
					this.RawValue = null;
					this.ImageSource = null;
					return;
				}

				byte[] Bytes;
				try
				{
					Bytes = Convert.FromBase64String(value);
				}
				catch (FormatException)
				{
					// A value that cannot be decoded cannot be shown or sent, so it counts as no photo and a required
					// field reports it as missing.
					this.RawValue = null;
					this.ImageSource = null;
					return;
				}

				this.RawValue = value;
				this.ImageSource = ImageSource.FromStream(() => new MemoryStream(Bytes));
			}
		}

		/// <summary>
		/// Gets the target output width from the field metadata, when configured.
		/// </summary>
		public int? TargetWidth
		{
			get
			{
				object? Value;
				return this.Metadata.TryGetValue("TargetWidth", out Value) ? Value as int? : null;
			}
		}

		/// <summary>
		/// Gets the target output height from the field metadata, when configured.
		/// </summary>
		public int? TargetHeight
		{
			get
			{
				object? Value;
				return this.Metadata.TryGetValue("TargetHeight", out Value) ? Value as int? : null;
			}
		}

		/// <summary>
		/// Gets the configured aspect ratio, as width divided by height, from the field metadata.
		/// </summary>
		public double? AspectRatio
		{
			get
			{
				object? Value;
				return this.Metadata.TryGetValue("AspectRatio", out Value) ? Value as double? : null;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the field metadata asks for cropping, when configured.
		/// </summary>
		public bool? ShouldCrop
		{
			get
			{
				object? Value;
				return this.Metadata.TryGetValue("ShouldCrop", out Value) ? Value as bool? : null;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this image field must use the app-owned camera flow.
		/// </summary>
		public bool CameraOnly => this.GetBooleanMetadataValue("CameraOnly") ?? false;

		/// <summary>
		/// Gets a value indicating whether the captured image bytes should be attached without recompression.
		/// </summary>
		public bool PreserveOriginalCapture => this.GetBooleanMetadataValue("PreserveOriginalCapture") ?? false;

		/// <summary>
		/// Gets a value indicating whether the field should use the image cropper before storing its value.
		/// </summary>
		public bool ShouldUseCropper => !this.CameraOnly && !this.PreserveOriginalCapture && this.ShouldCrop != false;

		/// <summary>
		/// Gets what the field captures, from the <c>CaptureProfile</c> metadata or else from its attachment mapping.
		/// </summary>
		public KycCaptureProfile CaptureProfile
		{
			get
			{
				if (this.Metadata.TryGetValue("CaptureProfile", out object? Value) &&
					Value is string Text &&
					Enum.TryParse(Text, true, out KycCaptureProfile MetadataProfile))
				{
					return MetadataProfile;
				}

				if (this.HasMapping("ProfilePhoto"))
					return KycCaptureProfile.Selfie;
				if (this.HasMapping("Passport"))
					return KycCaptureProfile.Passport;
				if (this.HasMapping("IdCardFront") || this.HasMapping("DriverLicenseFront"))
					return KycCaptureProfile.IdCardFront;
				if (this.HasMapping("IdCardBack") || this.HasMapping("DriverLicenseBack"))
					return KycCaptureProfile.IdCardBack;

				return KycCaptureProfile.Generic;
			}
		}

		/// <summary>
		/// Gets the title of the photo card: the field label, or a title that matches the capture profile.
		/// </summary>
		public string CaptureTitleText
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(this.Label?.Text))
					return this.Label.Text;

				if (this.CaptureProfile == KycCaptureProfile.Selfie)
					return ServiceRef.Localizer[nameof(AppResources.TakePhotoOfYourself)];

				return this.Placeholder?.Text ?? string.Empty;
			}
		}

		/// <summary>
		/// Gets a value indicating whether gallery upload is offered. Fields that require the app camera never offer it.
		/// </summary>
		public bool AllowUploadEffective => this.AllowUpload && !this.CameraOnly;

		/// <summary>
		/// Gets the aspect ratio, as width divided by height, of the photo card in the form.
		/// </summary>
		public double PreviewAspectRatio
		{
			get
			{
				if (this.AspectRatio is double Ratio && Ratio > 0)
					return Ratio;

				switch (this.CaptureProfile)
				{
					case KycCaptureProfile.Selfie:
						return 0.75;

					case KycCaptureProfile.Passport:
						return 1.42;

					case KycCaptureProfile.IdCardFront:
					case KycCaptureProfile.IdCardBack:
						return 1.586;
				}

				if (this.TargetWidth is int Width && this.TargetHeight is int Height && Width > 0 && Height > 0)
					return Width / (double)Height;

				return 1.0;
			}
		}

		/// <summary>
		/// Gets the maximum height of the photo card in the form, so tall photos do not push the rest of the page away.
		/// </summary>
		public double PreviewHeightRequest => this.CaptureProfile switch
		{
			KycCaptureProfile.Selfie => 320,
			KycCaptureProfile.IdCardFront or KycCaptureProfile.IdCardBack => 220,
			_ => 260
		};

		/// <summary>
		/// Gets a value indicating whether a photo has been added.
		/// </summary>
		public bool HasCapturedImage => this.ImageSource is not null;

		/// <summary>
		/// Gets the text of the camera action: retake once a photo is added, otherwise take photo.
		/// </summary>
		public string CaptureButtonText => this.HasCapturedImage
			? ServiceRef.Localizer[nameof(AppResources.KycImageRetakeButton)]
			: ServiceRef.Localizer[nameof(AppResources.TakePhoto)];

		/// <summary>
		/// Gets a value indicating whether the camera action can be used.
		/// </summary>
		public bool CanTakePhoto => !this.IsImageProcessing;

		/// <summary>
		/// Gets a value indicating whether the gallery action can be used.
		/// </summary>
		public bool CanPickPhoto => this.AllowUploadEffective && !this.IsImageProcessing;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasCapturedImage))]
		[NotifyPropertyChangedFor(nameof(CaptureButtonText))]
		private ImageSource? imageSource;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(AllowUploadEffective))]
		[NotifyPropertyChangedFor(nameof(CanPickPhoto))]
		private bool allowUpload = true;

		/// <summary>
		/// Gets or sets a value indicating whether a selected photo is being read and stored.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanTakePhoto))]
		[NotifyPropertyChangedFor(nameof(CanPickPhoto))]
		[NotifyCanExecuteChangedFor(nameof(TakePhotoCommand))]
		[NotifyCanExecuteChangedFor(nameof(PickPhotoCommand))]
		private bool isImageProcessing;

		[RelayCommand(CanExecute = nameof(CanPickPhoto))]
		private async Task PickPhoto()
		{
			if (this.CameraOnly || this.IsImageProcessing)
				return;

			try
			{
				IReadOnlyList<FileResult> FileResults = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions()
				{
					Title = ServiceRef.Localizer[nameof(AppResources.PickPhotoOfYourself)]
				});
				FileResult? FileResult = FileResults.FirstOrDefault();

				if (FileResult is null)
					return;

				byte[]? InputBin = await this.ReadFileAsync(FileResult);
				if (InputBin is null)
					throw new Exception("Failed to read photo stream");

				if (this.ShouldUseCropper)
				{
					TaskCompletionSource<byte[]?> Tcs = new();
					await ServiceRef.NavigationService.GoToAsync(
						nameof(ImageCroppingPage),
						new ImageCroppingNavigationArgs(ImageSource.FromStream(() => new MemoryStream(InputBin)), Tcs)
					);

					byte[] OutputBin = await Tcs.Task ?? throw new Exception("Failed to crop photo");
					await this.SetImageValueAsync(OutputBin);
				}
				else
				{
					await this.SetImageValueAsync(InputBin);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		[RelayCommand(CanExecute = nameof(CanTakePhoto))]
		private async Task TakePhoto()
		{
			if (this.IsImageProcessing)
				return;

			bool Permitted = await ServiceRef.PermissionService.CheckCameraPermissionAsync();

			if (!Permitted)
				return;

			try
			{
				if (this.CameraOnly)
				{
					TaskCompletionSource<byte[]?> Tcs = new();
					await ServiceRef.NavigationService.GoToAsync(nameof(KycProfilePhotoCameraPage), new KycProfilePhotoCameraNavigationArgs
					{
						CompletionSource = Tcs,
						CaptureProfile = this.CaptureProfile,
						Title = this.Label?.Text,
						GuideHint = this.Hint?.Text
					});

					byte[]? CapturedBytes = await Tcs.Task;
					if (CapturedBytes is null)
						return;

					await this.SetImageValueAsync(CapturedBytes);
					return;
				}

				FileResult? FileResult = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions()
				{
					Title = ServiceRef.Localizer[nameof(AppResources.TakePhotoOfYourself)]
				});

				if (FileResult is null)
					return;

				byte[]? InputBin = await this.ReadFileAsync(FileResult);
				if (InputBin is null)
					throw new Exception("Failed to read photo stream");

				if (this.ShouldUseCropper)
				{
					TaskCompletionSource<byte[]?> Tcs = new();
					await ServiceRef.NavigationService.GoToAsync(nameof(ImageCroppingPage), new ImageCroppingNavigationArgs(ImageSource.FromStream(() => new MemoryStream(InputBin)), Tcs));

					byte[] OutputBin = await Tcs.Task ?? throw new Exception("Failed to crop photo");
					await this.SetImageValueAsync(OutputBin);
				}
				else
				{
					await this.SetImageValueAsync(InputBin);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.FailedToLoadPhoto)]);
			}
		}

		/// <summary>
		/// Reads a picked or captured file while the photo card shows that the photo is being processed.
		/// </summary>
		/// <param name="FileResult">The file to read.</param>
		/// <returns>The file bytes, or <c>null</c> if the stream could not be read.</returns>
		private async Task<byte[]?> ReadFileAsync(FileResult FileResult)
		{
			this.IsImageProcessing = true;
			try
			{
				using Stream FileStream = await FileResult.OpenReadAsync();
				return await ToByteArrayAsync(FileStream);
			}
			finally
			{
				this.IsImageProcessing = false;
			}
		}

		/// <summary>
		/// Stores the photo as the field value and validates the field right away, so the card leaves its invalid state.
		/// </summary>
		/// <param name="ImageBytes">The photo bytes.</param>
		/// <returns>A task that completes when the value is stored.</returns>
		private async Task SetImageValueAsync(byte[] ImageBytes)
		{
			this.IsImageProcessing = true;
			try
			{
				string Base64 = await Task.Run(() => Convert.ToBase64String(ImageBytes));
				this.RawValue = Base64;
				this.ImageSource = ImageSource.FromStream(() => new MemoryStream(ImageBytes));
				this.ForceSynchronousValidation();
			}
			finally
			{
				this.IsImageProcessing = false;
			}
		}

		private bool HasMapping(string Key)
		{
			return this.Mappings.Any(Mapping => string.Equals(Mapping.Key, Key, StringComparison.OrdinalIgnoreCase));
		}

		private bool? GetBooleanMetadataValue(string Key)
		{
			if (!this.Metadata.TryGetValue(Key, out object? Value))
			{
				return null;
			}

			if (Value is bool BoolValue)
			{
				return BoolValue;
			}

			if (Value is string StringValue && bool.TryParse(StringValue, out bool ParsedBool))
			{
				return ParsedBool;
			}

			return null;
		}

		private static async Task<byte[]?> ToByteArrayAsync(System.IO.Stream stream)
		{
			if (stream is null)
				return null;
			using (System.IO.MemoryStream MemoryStream = new System.IO.MemoryStream())
			{
				await stream.CopyToAsync(MemoryStream);
				return MemoryStream.ToArray();
			}
		}
	}
}
