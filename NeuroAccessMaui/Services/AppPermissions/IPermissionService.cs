using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.AppPermissions;
using NeuroAccessMaui.Services.Settings;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.AppPermissions
{
	/// <summary>
	/// Handles common runtime settings that need to be persisted during sessions.
	/// </summary>
	[DefaultImplementation(typeof(PermissionService))]
	public interface IPermissionService : ILoadableService
	{
		Task<bool> CheckCameraPermission();
	}
}
