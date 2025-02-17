using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.UI.Popups.Permission;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.AppPermissions
{
	[Singleton]
	internal sealed class PermissionService : IPermissionService
	{
		public PermissionService()
		{
		}

		/// <summary>
		/// Checks and requests a permission of type TPermission.
		/// </summary>
		/// <typeparam name="TPermission">The permission type to check.</typeparam>
		/// <returns>The final status of the permission.</returns>
		private static async Task<PermissionStatus> CheckAndRequestPermissionAsync<TPermission>()
			 where TPermission : Permissions.BasePermission, new()
		{
			PermissionStatus Status = await Permissions.CheckStatusAsync<TPermission>();

			// On platforms other than iOS, a Denied status might be temporary,
			// so we try to request it.
			if ((Status == PermissionStatus.Denied && DeviceInfo.Platform != DevicePlatform.iOS) ||
				 Status == PermissionStatus.Unknown)
			{
				Status = await Permissions.RequestAsync<TPermission>();
			}

			return Status;
		}

		/// <summary>
		/// Checks the camera permission, requests it if necessary, and shows a rationale popup if denied.
		/// </summary>
		/// <returns>True if permission is granted; otherwise, false.</returns>
		public async Task<bool> CheckCameraPermissionAsync()
		{
			PermissionStatus Status = await CheckAndRequestPermissionAsync<Permissions.Camera>();

			if (Status == PermissionStatus.Granted)
				return true;

			// Prepare localized strings for the permission rationale
			string title = ServiceRef.Localizer[nameof(AppResources.CameraPermissionTitle)];
			string description = ServiceRef.Localizer[nameof(AppResources.CameraPermissionDescription)];
			string descriptionSecondary = ServiceRef.Localizer[nameof(AppResources.CameraPermissionDescriptionSecondary)];

			// Display the permission popup so the user can enable it from the settings.
			ShowPermissionPopup permissionPopup = new(title, description, descriptionSecondary);
			await ServiceRef.UiService.PushAsync(permissionPopup);

			Status = await CheckAndRequestPermissionAsync<Permissions.Camera>();

			return Status == PermissionStatus.Granted;

		}
	}
}
