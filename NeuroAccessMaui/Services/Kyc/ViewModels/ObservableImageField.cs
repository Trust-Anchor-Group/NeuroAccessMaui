using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.UI.Pages.Utility.Images;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services.Kyc.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.UI.Pages.Kyc;

namespace NeuroAccessMaui.Services.Kyc.ViewModels
{
    public partial class ObservableImageField : ObservableFileField
    {
        public override string? StringValue
        {
            get => this.RawValue as string;
            set {
				this.RawValue = value;
				this.ImageSource = ImageSource.FromStream(() => new MemoryStream(Convert.FromBase64String(this.RawValue as string ?? string.Empty)));
			}
        }

        public int? TargetWidth
        {
            get
            {
                object? Value;
                return this.Metadata.TryGetValue("TargetWidth", out Value) ? Value as int? : null;
            }
        }
        public int? TargetHeight
        {
            get
            {
                object? Value;
                return this.Metadata.TryGetValue("TargetHeight", out Value) ? Value as int? : null;
            }
        }
        public double? AspectRatio
        {
            get
            {
                object? Value;
                return this.Metadata.TryGetValue("AspectRatio", out Value) ? Value as double? : null;
            }
        }
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

		[ObservableProperty]
        private ImageSource? imageSource;

		[ObservableProperty]
		private bool allowUpload = true;

		[RelayCommand]
        private async Task PickPhoto()
        {
			if (this.CameraOnly)
				return;

            try
            {
                FileResult? FileResult = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions()
                {
                    Title = ServiceRef.Localizer[nameof(AppResources.PickPhotoOfYourself)]
                });

                if (FileResult is null)
                    return;

                Stream FileStream = await FileResult.OpenReadAsync();
                byte[] InputBin = await ToByteArrayAsync(FileStream) ?? throw new Exception("Failed to read photo stream");

				if (this.ShouldUseCropper)
				{
					TaskCompletionSource<byte[]?> Tcs = new();
					await ServiceRef.NavigationService.GoToAsync(
						nameof(ImageCroppingPage),
						new ImageCroppingNavigationArgs(ImageSource.FromStream(() => new MemoryStream(InputBin)), Tcs)
					);

					byte[] OutputBin = await Tcs.Task ?? throw new Exception("Failed to crop photo");
					this.SetImageValue(OutputBin);
				}
				else
				{
					this.SetImageValue(InputBin);
				}
            }
            catch (Exception Ex)
            {
                ServiceRef.LogService.LogException(Ex);
                await ServiceRef.UiService.DisplayException(Ex);
            }
        }

        [RelayCommand]
        private async Task TakePhoto()
        {
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
						CompletionSource = Tcs
					});

					byte[]? CapturedBytes = await Tcs.Task;
					if (CapturedBytes is null)
						return;

					this.SetImageValue(CapturedBytes);
					return;
				}

				FileResult? FileResult = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions()
				{
					Title = ServiceRef.Localizer[nameof(AppResources.TakePhotoOfYourself)]
				});

				if (FileResult is null)
					return;

				Stream FileStream = await FileResult.OpenReadAsync();
				byte[] InputBin = await ToByteArrayAsync(FileStream) ?? throw new Exception("Failed to read photo stream");

				if (this.ShouldUseCropper)
				{
					TaskCompletionSource<byte[]?> Tcs = new();
					await ServiceRef.NavigationService.GoToAsync(nameof(ImageCroppingPage), new ImageCroppingNavigationArgs(ImageSource.FromStream(() => new MemoryStream(InputBin)), Tcs));

					byte[] OutputBin = await Tcs.Task ?? throw new Exception("Failed to crop photo");
					this.SetImageValue(OutputBin);
				}
				else
				{
					this.SetImageValue(InputBin);
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

		private void SetImageValue(byte[] ImageBytes)
		{
			this.RawValue = Convert.ToBase64String(ImageBytes);
			this.ImageSource = ImageSource.FromStream(() => new MemoryStream(ImageBytes));
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
