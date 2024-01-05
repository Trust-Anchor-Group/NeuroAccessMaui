using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.UI.Photos;
using NeuroAccessMaui.UI.Pages.Registration;
using SkiaSharp;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Applications
{
	/// <summary>
	/// The view model to bind to for when displaying the an application for a Personal ID.
	/// </summary>
	public partial class ApplyIdViewModel : RegisterIdentityModel
	{
		private const string profilePhotoFileName = "ProfilePhoto.jpg";
		private readonly string localPhotoFileName;
		private readonly PhotosLoader photosLoader;
		private LegalIdentityAttachment? photo;

		/// <summary>
		/// Creates an instance of the <see cref="ApplyIdViewModel"/> class.
		/// </summary>
		public ApplyIdViewModel()
			: base()
		{
			this.localPhotoFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), profilePhotoFileName);
			this.photosLoader = new PhotosLoader();
		}

		protected override async Task OnInitialize()
		{
			if (ServiceRef.TagProfile.IdentityApplication is not null)
			{
				if (ServiceRef.TagProfile.IdentityApplication.IsDiscarded())
					await ServiceRef.TagProfile.SetIdentityApplication(null, true);
			}

			LegalIdentity? IdentityReference;

			if (ServiceRef.TagProfile.IdentityApplication is not null)
			{
				IdentityReference = ServiceRef.TagProfile.IdentityApplication;
				this.ApplicationSent = true;
			}
			else
			{
				this.ApplicationSent = false;
				IdentityReference = ServiceRef.TagProfile.LegalIdentity;
			}

			if (IdentityReference is not null)
			{
				await this.SetProperties(IdentityReference, true);

				if (string.IsNullOrEmpty(this.OrgCountryCode))
				{
					this.OrgCountryCode = this.CountryCode;
					this.OrgCountryName = this.CountryName;
				}
			}

			ApplyIdNavigationArgs? Args = ServiceRef.NavigationService.PopLatestArgs<ApplyIdNavigationArgs>();

			if (Args is not null)
			{
				this.Personal = Args.Personal;
				this.Organizational = Args.Organizational;
			}
			else if (IdentityReference is not null)
			{
				this.Organizational = IdentityReference.IsOrganizational();
				this.Personal = !this.Organizational;
			}

			this.RequiresOrgName = this.Organizational;
			this.RequiresOrgDepartment = this.Organizational;
			this.RequiresOrgRole = this.Organizational;
			this.RequiresOrgNumber = this.Organizational;

			ServiceRef.XmppService.IdentityApplicationChanged += this.XmppService_IdentityApplicationChanged;

			await base.OnInitialize();

			if (!this.HasApplicationAttributes && this.IsConnected)
				await Task.Run(this.LoadApplicationAttributes);
		}

		protected override Task OnDispose()
		{
			this.photosLoader.CancelLoadPhotos();

			ServiceRef.XmppService.IdentityApplicationChanged -= this.XmppService_IdentityApplicationChanged;

			return base.OnDispose();
		}

		private Task XmppService_IdentityApplicationChanged(object Sender, LegalIdentityEventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.ApplicationSent = ServiceRef.TagProfile.IdentityApplication is not null;

			});

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		protected override async Task XmppService_ConnectionStateChanged(object? Sender, XmppState NewState)
		{
			await base.XmppService_ConnectionStateChanged(Sender, NewState);
			this.OnPropertyChanged(nameof(this.ApplicationSentAndConnected));
		}

		/// <inheritdoc/>
		protected override async Task OnConnected()
		{
			await base.OnConnected();

			if (!this.HasApplicationAttributes && this.IsConnected)
				await Task.Run(this.LoadApplicationAttributes);
		}

		/// <inheritdoc/>
		public override void SetIsBusy(bool IsBusy)
		{
			base.SetIsBusy(IsBusy);
			this.NotifyCommandsCanExecuteChanged();
		}

		protected override async Task SetProperties(LegalIdentity Identity, bool ClearPropertiesNotFound)
		{
			await base.SetProperties(Identity, ClearPropertiesNotFound);

			if (Identity?.Attachments is not null)
			{
				Photo? First = await this.photosLoader.LoadPhotos(Identity.Attachments, SignWith.LatestApprovedIdOrCurrentKeys);

				if (First is null)
				{
					if (ClearPropertiesNotFound)
					{
						this.photo = null;
						this.Image = null;
						this.ImageBin = null;
						this.HasPhoto = false;
					}
				}
				else
				{
					this.Image = First.Source;
					this.ImageBin = First.Binary;
					this.HasPhoto = true;
				}
			}
		}

		private async Task LoadApplicationAttributes()
		{
			try
			{
				IdApplicationAttributesEventArgs e = await ServiceRef.XmppService.GetIdApplicationAttributes();
				if (e.Ok)
				{
					MainThread.BeginInvokeOnMainThread(() =>
					{
						bool RequiresFirstName = false;
						bool RequiresMiddleNames = false;
						bool RequiresLastNames = false;
						bool RequiresPersonalNumber = false;
						bool RequiresAddress = false;
						bool RequiresAddress2 = false;
						bool RequiresZipCode = false;
						bool RequiresArea = false;
						bool RequiresCity = false;
						bool RequiresRegion = false;
						bool RequiresCountry = false;

						foreach (string Name in e.RequiredProperties)
						{
							switch (Name)
							{
								case Constants.XmppProperties.FirstName:
									RequiresFirstName = true;
									break;

								case Constants.XmppProperties.MiddleNames:
									RequiresMiddleNames = true;
									break;

								case Constants.XmppProperties.LastNames:
									RequiresLastNames = true;
									break;

								case Constants.XmppProperties.PersonalNumber:
									RequiresPersonalNumber = true;
									break;

								case Constants.XmppProperties.Address:
									RequiresAddress = true;
									break;

								case Constants.XmppProperties.Address2:
									RequiresAddress2 = true;
									break;

								case Constants.XmppProperties.Area:
									RequiresArea = true;
									break;

								case Constants.XmppProperties.City:
									RequiresCity = true;
									break;

								case Constants.XmppProperties.ZipCode:
									RequiresZipCode = true;
									break;

								case Constants.XmppProperties.Region:
									RequiresRegion = true;
									break;

								case Constants.XmppProperties.Country:
									RequiresCountry = true;
									break;
							}
						}

						this.PeerReview = e.PeerReview;
						this.NrPhotos = e.NrPhotos;
						this.NrReviewers = e.NrReviewers;
						this.RequiresCountryIso3166 = e.Iso3166;
						this.RequiresFirstName = RequiresFirstName;
						this.RequiresMiddleNames = RequiresMiddleNames;
						this.RequiresLastNames = RequiresLastNames;
						this.RequiresPersonalNumber = RequiresPersonalNumber;
						this.RequiresAddress = RequiresAddress;
						this.RequiresAddress2 = RequiresAddress2;
						this.RequiresZipCode = RequiresZipCode;
						this.RequiresArea = RequiresArea;
						this.RequiresCity = RequiresCity;
						this.RequiresRegion = RequiresRegion;
						this.RequiresCountry = RequiresCountry;
						this.RequiresOrgAddress = this.Organizational && RequiresAddress;
						this.RequiresOrgAddress2 = this.Organizational && RequiresAddress2;
						this.RequiresOrgZipCode = this.Organizational && RequiresZipCode;
						this.RequiresOrgArea = this.Organizational && RequiresArea;
						this.RequiresOrgCity = this.Organizational && RequiresCity;
						this.RequiresOrgRegion = this.Organizational && RequiresRegion;
						this.RequiresOrgCountry = this.Organizational && RequiresCountry;
						this.HasApplicationAttributes = true;
					});
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		#region Properties

		/// <summary>
		/// If the user consents to the processing of the information.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private bool consent;

		/// <summary>
		/// If the user affirms information provided is correct.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private bool correct;

		/// <summary>
		/// If the view model has ID Application attributes loaded.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private bool hasApplicationAttributes;

		/// <summary>
		/// Number of photos required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private int nrPhotos;

		/// <summary>
		/// Number of reviewers required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private int nrReviewers;

		/// <summary>
		/// If peer review is permitted.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private bool peerReview;

		/// <summary>
		/// If an ID application has been sent.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyCanExecuteChangedFor(nameof(ScanQrCodeCommand))]
		[NotifyCanExecuteChangedFor(nameof(FeaturedPeerReviewersCommand))]
		[NotifyCanExecuteChangedFor(nameof(RevokeApplicationCommand))]
		[NotifyPropertyChangedFor(nameof(CanEdit))]
		[NotifyPropertyChangedFor(nameof(CanRemovePhoto))]
		[NotifyPropertyChangedFor(nameof(CanTakePhoto))]
		[NotifyPropertyChangedFor(nameof(ApplicationSentAndConnected))]
		private bool applicationSent;

		/// <summary>
		/// If application is personal.
		/// </summary>
		[ObservableProperty]
		private bool personal;

		/// <summary>
		/// If application is organizational.
		/// </summary>
		[ObservableProperty]
		private bool organizational;

		/// <summary>
		/// If a photo is available.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanRemovePhoto))]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyCanExecuteChangedFor(nameof(RemovePhotoCommand))]
		private bool hasPhoto;

		/// <summary>
		/// Photo
		/// </summary>
		[ObservableProperty]
		private ImageSource? image;

		/// <summary>
		/// Binary representation of photo
		/// </summary>
		[ObservableProperty]
		private byte[]? imageBin;

		/// <summary>
		/// Rotation of <see cref="Image"/>
		/// </summary>
		[ObservableProperty]
		private int imageRotation;

		/// <summary>
		/// If the form can be edited.
		/// </summary>
		public bool CanEdit => !this.ApplicationSent;

		/// <summary>
		/// If the form can be edited.
		/// </summary>
		public bool CanRemovePhoto => this.CanEdit && this.HasPhoto;

		/// <summary>
		/// If a photo can be taken.
		/// </summary>
		public bool CanTakePhoto => this.CanEdit && MediaPicker.IsCaptureSupported;

		/// <summary>
		/// If application has been sent and app is connected.
		/// </summary>
		public bool ApplicationSentAndConnected => this.ApplicationSent && this.IsConnected;

		#endregion

		#region Commands

		/// <summary>
		/// Used to find out if an ICommand can execute
		/// </summary>
		public override bool CanApply
		{
			get
			{
				if (!this.CanExecuteCommands || !this.Consent || !this.Correct || this.ApplicationSent || !this.HasPhoto)
					return false;

				if (this.HasApplicationAttributes)
				{
					if (!this.FirstNameOk ||
						!this.MiddleNamesOk ||
						!this.LastNamesOk ||
						!this.PersonalNumberOk ||
						!this.AddressOk ||
						!this.Address2Ok ||
						!this.ZipCodeOk ||
						!this.AreaOk ||
						!this.CityOk ||
						!this.RegionOk ||
						!this.CountryOk)
					{
						return false;
					}

					if (this.RequiresCountryIso3166 && !ISO_3166_1.TryGetCountryByCode(this.CountryCode, out _))
						return false;

					if (this.Organizational)
					{
						if (!this.OrgNameOk ||
							!this.OrgDepartmentOk ||
							!this.OrgRoleOk ||
							!this.OrgNumberOk ||
							!this.OrgAddressOk ||
							!this.OrgAddress2Ok ||
							!this.OrgZipCodeOk ||
							!this.OrgAreaOk ||
							!this.OrgCityOk ||
							!this.OrgRegionOk ||
							!this.OrgCountryOk)
						{
							return false;
						}
					}
				}

				return true;
			}
		}

		/// <summary>
		/// Executes the application command.
		/// </summary>
		protected override async Task Apply()
		{
			if (this.ApplicationSent)
				return;

			if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToSendThisIdApplication)]))
				return;

			if (!await App.AuthenticateUser(true))
				return;

			try
			{
				LegalIdentityAttachment[] Photos = this.photo is null ? [] : [this.photo];

				this.SetIsBusy(true);

				(bool Succeeded, LegalIdentity? AddedIdentity) = await ServiceRef.NetworkService.TryRequest(() =>
					ServiceRef.XmppService.AddLegalIdentity(this, false, Photos));

				if (Succeeded && AddedIdentity is not null)
				{
					await ServiceRef.TagProfile.SetIdentityApplication(AddedIdentity, true);
					this.ApplicationSent = true;

					if (this.HasPhoto)
					{
						Attachment? FirstImage = AddedIdentity.Attachments.GetFirstImageAttachment();

						if (FirstImage is not null && this.ImageBin is not null)
							await ServiceRef.AttachmentCacheService.Add(FirstImage.Url, AddedIdentity.Id, true, this.ImageBin, FirstImage.ContentType);
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
			finally
			{
				this.SetIsBusy(false);
			}
		}

		/// <summary>
		/// Revokes the current application.
		/// </summary>
		[RelayCommand(CanExecute = nameof(ApplicationSent))]
		private async Task RevokeApplication()
		{
			LegalIdentity? Application = ServiceRef.TagProfile.IdentityApplication;
			if (Application is null)
			{
				this.ApplicationSent = false;
				return;
			}

			if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToRevokeTheCurrentIdApplication)]))
				return;

			if (!await App.AuthenticateUser(true))
				return;

			try
			{
				this.SetIsBusy(true);

				await ServiceRef.XmppService.ObsoleteLegalIdentity(Application.Id);

				await ServiceRef.TagProfile.SetIdentityApplication(null, true);
				this.ApplicationSent = false;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
			finally
			{
				this.SetIsBusy(false);
			}
		}

		/// <summary>
		/// Scan a QR-code belonging to a peer
		/// </summary>
		[RelayCommand(CanExecute = nameof(ApplicationSent))]
		private async Task ScanQrCode()
		{
			string? Url = await Services.UI.QR.QrCode.ScanQrCode(nameof(AppResources.QrPageTitleScanPeerId),
				[
					Constants.UriSchemes.IotId
				]);

			if (string.IsNullOrEmpty(Url) || !Constants.UriSchemes.StartsWithIdScheme(Url))
				return;

			string? ReviewerId = Constants.UriSchemes.RemoveScheme(Url);
			LegalIdentity? ToReview = ServiceRef.TagProfile.IdentityApplication;
			if (ToReview is null || string.IsNullOrEmpty(ReviewerId))
				return;

			try
			{
				this.SetIsBusy(true);

				await ServiceRef.XmppService.PetitionPeerReviewId(ReviewerId, ToReview, Guid.NewGuid().ToString(),
					ServiceRef.Localizer[nameof(AppResources.CouldYouPleaseReviewMyIdentityInformation)]);

				await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.PetitionSent)],
					ServiceRef.Localizer[nameof(AppResources.APetitionHasBeenSentToYourPeer)]);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
			finally
			{
				this.SetIsBusy(false);
			}
		}

		/// <summary>
		/// Takes a new photo
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanTakePhoto))]
		private async Task TakePhoto()
		{
			if (!this.CanTakePhoto)
				return;

			try
			{
				FileResult Result = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions()
				{
					Title = ServiceRef.Localizer[nameof(AppResources.TakePhotoOfYourself)]
				});

				if (Result is null)
					return;

				await this.AddPhoto(Result.FullPath, true);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		/// <summary>
		/// Adds a photo from the specified path to use as a profile photo.
		/// </summary>
		/// <param name="Bin">Binary content</param>
		/// <param name="ContentType">Content-Type</param>
		/// <param name="Rotation">Rotation to use, to display the image correctly.</param>
		/// <param name="saveLocalCopy">Set to <c>true</c> to save a local copy, <c>false</c> otherwise.</param>
		/// <param name="showAlert">Set to <c>true</c> to show an alert if photo is too large; <c>false</c> otherwise.</param>
		protected internal async Task AddPhoto(byte[] Bin, string ContentType, int Rotation, bool saveLocalCopy, bool showAlert)
		{
			if (Bin.Length > ServiceRef.TagProfile.HttpFileUploadMaxSize)
			{
				if (showAlert)
					await ServiceRef.UiSerializer.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.PhotoIsTooLarge)]);

				return;
			}

			this.RemovePhoto(saveLocalCopy);

			if (saveLocalCopy)
			{
				try
				{
					File.WriteAllBytes(this.localPhotoFileName, Bin);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			}

			this.photo = new LegalIdentityAttachment(this.localPhotoFileName, ContentType, Bin);
			this.ImageRotation = Rotation;
			this.Image = ImageSource.FromStream(() => new MemoryStream(Bin));
			this.ImageBin = Bin;
			this.HasPhoto = true;
		}

		/// <summary>
		/// Adds a photo from the specified path to use as a profile photo.
		/// </summary>
		/// <param name="FilePath">The full path to the file.</param>
		/// <param name="SaveLocalCopy">Set to <c>true</c> to save a local copy, <c>false</c> otherwise.</param>
		protected internal async Task AddPhoto(string FilePath, bool SaveLocalCopy)
		{
			SKData? ImageData = null;

			try
			{
				bool FallbackOriginal = true;

				if (SaveLocalCopy)
				{
					// try to downscale and comress the image
					using FileStream InputStream = File.OpenRead(FilePath);
					ImageData = CompressImage(InputStream);

					if (ImageData is not null)
					{
						FallbackOriginal = false;
						await this.AddPhoto(ImageData.ToArray(), Constants.MimeTypes.Jpeg, 0, SaveLocalCopy, true);
					}
				}

				if (FallbackOriginal)
				{
					byte[] Bin = File.ReadAllBytes(FilePath);
					if (!InternetContent.TryGetContentType(Path.GetExtension(FilePath), out string ContentType))
						ContentType = "application/octet-stream";

					await this.AddPhoto(Bin, ContentType, PhotosLoader.GetImageRotation(Bin), SaveLocalCopy, true);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.FailedToLoadPhoto)]);
			}
			finally
			{
				ImageData?.Dispose();
			}
		}

		private static SKData? CompressImage(Stream inputStream)
		{
			try
			{
				using SKManagedStream ManagedStream = new(inputStream);
				using SKData ImageData = SKData.Create(ManagedStream);

				SKCodec Codec = SKCodec.Create(ImageData);
				SKBitmap SkBitmap = SKBitmap.Decode(ImageData);

				SkBitmap = HandleOrientation(SkBitmap, Codec.EncodedOrigin);

				bool Resize = false;
				int Height = SkBitmap.Height;
				int Width = SkBitmap.Width;

				// downdsample to FHD
				if ((Width >= Height) && (Width > 1920))
				{
					Height = (int)(Height * (1920.0 / Width) + 0.5);
					Width = 1920;
					Resize = true;
				}
				else if ((Height > Width) && (Height > 1920))
				{
					Width = (int)(Width * (1920.0 / Height) + 0.5);
					Height = 1920;
					Resize = true;
				}

				if (Resize)
				{
					SKImageInfo Info = SkBitmap.Info;
					SKImageInfo NewInfo = new(Width, Height, Info.ColorType, Info.AlphaType, Info.ColorSpace);
					SkBitmap = SkBitmap.Resize(NewInfo, SKFilterQuality.High);
				}

				return SkBitmap.Encode(SKEncodedImageFormat.Jpeg, 80);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				return null;
			}
		}

		private static SKBitmap HandleOrientation(SKBitmap Bitmap, SKEncodedOrigin Orientation)
		{
			SKBitmap Rotated;

			switch (Orientation)
			{
				case SKEncodedOrigin.BottomRight:
					Rotated = new SKBitmap(Bitmap.Width, Bitmap.Height);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.RotateDegrees(180, Bitmap.Width / 2, Bitmap.Height / 2);
						Surface.DrawBitmap(Bitmap, 0, 0);
					}
					break;

				case SKEncodedOrigin.RightTop:
					Rotated = new SKBitmap(Bitmap.Height, Bitmap.Width);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.Translate(Rotated.Width, 0);
						Surface.RotateDegrees(90);
						Surface.DrawBitmap(Bitmap, 0, 0);
					}
					break;

				case SKEncodedOrigin.LeftBottom:
					Rotated = new SKBitmap(Bitmap.Height, Bitmap.Width);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.Translate(0, Rotated.Height);
						Surface.RotateDegrees(270);
						Surface.DrawBitmap(Bitmap, 0, 0);
					}
					break;

				default:
					return Bitmap;
			}

			return Rotated;
		}

		private void RemovePhoto(bool RemoveFileOnDisc)
		{
			try
			{
				this.photo = null;
				this.Image = null;
				this.ImageBin = null;
				this.HasPhoto = false;

				if (RemoveFileOnDisc && File.Exists(this.localPhotoFileName))
					File.Delete(this.localPhotoFileName);
			}
			catch (Exception e)
			{
				ServiceRef.LogService.LogException(e);
			}
		}

		/// <summary>
		/// Takes a new photo
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanEdit))]
		private async Task PickPhoto()
		{
			try
			{
				FileResult Result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions()
				{
					Title = ServiceRef.Localizer[nameof(AppResources.PickPhotoOfYourself)]
				});

				if (Result is null)
					return;

				await this.AddPhoto(Result.FullPath, true);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		/// <summary>
		/// Removes the current photo.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanRemovePhoto))]
		private void RemovePhoto()
		{
			this.RemovePhoto(true);
		}

		/// <summary>
		/// Select from a list of featured peer reviewers.
		/// </summary>
		[RelayCommand(CanExecute = nameof(ApplicationSent))]
		private static async Task FeaturedPeerReviewers()
		{
			// TODO
		}

		#endregion
	}
}
