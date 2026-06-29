using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroAccessMaui.Camera
{
	/// <summary>
	/// Provides platform-specific camera discovery helpers.
	/// </summary>
	public static class CameraPlatformService
	{
		/// <summary>
		/// Retrieves the list of available cameras for the current platform.
		/// </summary>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>A list of camera descriptors.</returns>
		public static Task<IReadOnlyList<CameraDescriptor>> GetAvailableCamerasAsync(CancellationToken CancellationToken)
		{
#if ANDROID
			return GetAvailableCamerasAndroidAsync(CancellationToken);
#elif IOS
			return GetAvailableCamerasiOSAsync(CancellationToken);
#else
			return Task.FromResult<IReadOnlyList<CameraDescriptor>>(new List<CameraDescriptor>());
#endif
		}

#if ANDROID
		private static Task<IReadOnlyList<CameraDescriptor>> GetAvailableCamerasAndroidAsync(CancellationToken CancellationToken)
		{
			return Platforms.Android.AndroidCameraDiscovery.GetAvailableCamerasAsync(CancellationToken);
		}
#endif

#if IOS
		private static Task<IReadOnlyList<CameraDescriptor>> GetAvailableCamerasiOSAsync(CancellationToken CancellationToken)
		{
			return Platforms.iOS.IosCameraDiscovery.GetAvailableCamerasAsync(CancellationToken);
		}
#endif
	}
}
