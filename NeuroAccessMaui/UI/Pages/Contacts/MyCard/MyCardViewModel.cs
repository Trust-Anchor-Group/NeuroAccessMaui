using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Profiles;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Photos;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contacts.MyCard
{
	/// <summary>
	/// The view model of the page where the user sets up their own card: the name and photo others see, and which of them
	/// to share. Nothing is shared until the user turns it on.
	/// </summary>
	public partial class MyCardViewModel : BaseViewModel
	{
		private MyProfile? profile;
		private byte[]? photo;

		/// <summary>
		/// The name others see.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(Initials))]
		private string name = string.Empty;

		/// <summary>
		/// If the name is shared with others.
		/// </summary>
		[ObservableProperty]
		private bool shareName;

		/// <summary>
		/// If the photo is shared with others.
		/// </summary>
		[ObservableProperty]
		private bool sharePhoto;

		/// <summary>
		/// The photo, to show.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasPhoto))]
		private ImageSource? photoSource;

		/// <summary>
		/// If the card is being saved.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
		private bool isSaving;

		/// <summary>
		/// If there is a photo.
		/// </summary>
		public bool HasPhoto => this.PhotoSource is not null;

		/// <summary>
		/// If the user's ID has a photo that can be used.
		/// </summary>
		public bool HasIdPhoto => ServiceRef.TagProfile.LegalIdentity?.Attachments?.Any(Attachment =>
			Attachment.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? false) ?? false;

		/// <summary>
		/// The letters shown when there is no photo.
		/// </summary>
		public string Initials
		{
			get
			{
				string[] Words = this.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

				return Words.Length switch
				{
					0 => ServiceRef.Localizer[nameof(AppResources.ChatMe)],
					1 => Words[0][..1].ToUpper(CultureInfo.CurrentCulture),
					_ => (Words[0][..1] + Words[^1][..1]).ToUpper(CultureInfo.CurrentCulture)
				};
			}
		}

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			try
			{
				this.profile = await ProfileService.GetAsync();
				this.photo = this.profile.Photo;

				string? Path = this.photo is null ? null : await PhotoCache.StoreAsync(this.photo);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.Name = this.profile.Name;
					this.ShareName = this.profile.ShareName;
					this.SharePhoto = this.profile.SharePhoto;
					this.PhotoSource = Path is null ? null : ImageSource.FromFile(Path);
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Lets the user pick a photo from the phone.
		/// </summary>
		[RelayCommand]
		private async Task ChoosePhotoAsync()
		{
			try
			{
				IReadOnlyList<FileResult> Chosen = await MediaPicker.PickPhotosAsync(new MediaPickerOptions()
				{
					SelectionLimit = 1
				});
				FileResult? Picked = Chosen.FirstOrDefault();

				if (Picked is not null)
					await this.UsePhotoAsync(Picked);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Lets the user take a photo with the camera.
		/// </summary>
		[RelayCommand]
		private async Task TakePhotoAsync()
		{
			try
			{
				if (!MediaPicker.Default.IsCaptureSupported)
					return;

				FileResult? Taken = await MediaPicker.Default.CapturePhotoAsync();
				if (Taken is not null)
					await this.UsePhotoAsync(Taken);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Uses the photo in the user's ID.
		/// </summary>
		[RelayCommand]
		private async Task UseIdPhotoAsync()
		{
			try
			{
				LegalIdentity? Identity = ServiceRef.TagProfile.LegalIdentity;
				if (Identity?.Attachments is null)
					return;

				(string? File, _, _) = await PhotosLoader.LoadPhotoAsTemporaryFile(Identity.Attachments,
					ProfilePhotos.Size * 2, ProfilePhotos.Size * 2);

				if (!string.IsNullOrEmpty(File))
					await this.SetPhotoAsync(await System.IO.File.ReadAllBytesAsync(File));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Takes the photo away.
		/// </summary>
		[RelayCommand]
		private void RemovePhoto()
		{
			this.photo = null;
			this.PhotoSource = null;
			this.SharePhoto = false;
		}

		private async Task UsePhotoAsync(FileResult Picked)
		{
			using Stream Input = await Picked.OpenReadAsync();
			using MemoryStream Buffer = new();
			await Input.CopyToAsync(Buffer);

			await this.SetPhotoAsync(Buffer.ToArray());
		}

		/// <summary>
		/// Makes the small square photo from a picture, and shows it.
		/// </summary>
		private async Task SetPhotoAsync(byte[] Data)
		{
			byte[]? Made = await Task.Run(() => ProfilePhotos.Make(Data));
			if (Made is null)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ProfilePhoto)],
					ServiceRef.Localizer[nameof(AppResources.ProfilePhotoUnreadable)]);
				return;
			}

			string Path = await PhotoCache.StoreAsync(Made);

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.photo = Made;
				this.PhotoSource = ImageSource.FromFile(Path);
			});
		}

		private bool CanSave() => !this.IsSaving;

		/// <summary>
		/// Saves the card, and shares what the user has chosen.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanSave))]
		private async Task SaveAsync()
		{
			this.IsSaving = true;

			try
			{
				MyProfile Profile = this.profile ?? await ProfileService.GetAsync();

				Profile.Name = this.Name.Trim();
				Profile.ShareName = this.ShareName && Profile.Name.Length > 0;
				Profile.Photo = this.photo;
				Profile.SharePhoto = this.SharePhoto && this.photo is not null;

				await ProfileService.SaveAsync(Profile);
				await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.ProfileSaved)]);
				await this.GoBack();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
			finally
			{
				this.IsSaving = false;
			}
		}

		/// <summary>
		/// Shows the user's ID, with the code others scan to add them.
		/// </summary>
		[RelayCommand]
		private async Task ShowMyIdAsync()
		{
			LegalIdentity? Own = ServiceRef.TagProfile.LegalIdentity;
			if (Own is not null)
				await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Own));
		}
	}
}
