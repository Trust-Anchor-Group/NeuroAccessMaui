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
			PermissionStatus Status = await Permissions.CheckStatusAsync<Permissions.Camera>();
			if (Status == PermissionStatus.Denied)
			{
				if (DeviceInfo.Platform != DevicePlatform.iOS)
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
			}

			return true;
		} 
	}
}
