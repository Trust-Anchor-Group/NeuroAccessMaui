using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.UI.Popups.Permission;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.AppPermissions;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.AppPermissions
{
	[Singleton]
	internal sealed class PermissionService : LoadableService, IPermissionService
	{
		public PermissionService()
		{
		}

		public async Task<bool> CheckCameraPermission()
		{
			PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Camera>();
			if (status == PermissionStatus.Denied)
			{
				if (DeviceInfo.Platform != DevicePlatform.iOS)
				{
					status = await Permissions.RequestAsync<Permissions.Camera>();
				}

				if (status == PermissionStatus.Denied)
				{
					string title = ServiceRef.Localizer[nameof(AppResources.CameraPermissionTitle)];
					string description = ServiceRef.Localizer[nameof(AppResources.CameraPermissionDescription)];
					
					ShowPermissionPopup permissionPopUp = new(title, description);
					await ServiceRef.UiService.PushAsync(permissionPopUp);

					return false;
				}
			}

			return true;
		} 
	}
}
