using NeuroAccessMaui.UI.Popups.Permission;
using NeuroAccessMaui.Resources.Languages;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.AppPermissions
{
	[Singleton]
	internal sealed class PermissionService : IPermissionService
	{
		public PermissionService()
		{
		}

		public async Task<bool> CheckCameraPermissionAsync()
		{
			PermissionStatus Status = await Permissions.CheckStatusAsync<Permissions.Camera>();

			if (Status == PermissionStatus.Denied && DeviceInfo.Platform != DevicePlatform.iOS)
			{
				Status = await Permissions.RequestAsync<Permissions.Camera>();
			}
			else if (Status == PermissionStatus.Unknown)
			{
				Status = await Permissions.RequestAsync<Permissions.Camera>();
			}

			if (Status == PermissionStatus.Denied)
			{
				string Title = ServiceRef.Localizer[nameof(AppResources.CameraPermissionTitle)];
				string Description = ServiceRef.Localizer[nameof(AppResources.CameraPermissionDescription)];
				string DescriptionSecondary = ServiceRef.Localizer[nameof(AppResources.CameraPermissionDescriptionSecondary)];
					
				ShowPermissionPopup PermissionPopUp = new(Title, Description, DescriptionSecondary);
				await ServiceRef.UiService.PushAsync(PermissionPopUp);

				return false;
			}

			return true;
		} 
	}
}
