#if IOS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVFoundation;

namespace NeuroAccessMaui.Camera.Platforms.iOS
{
	/// <summary>
	/// Provides iOS camera discovery.
	/// </summary>
	internal static class IosCameraDiscovery
	{
		public static Task<IReadOnlyList<CameraDescriptor>> GetAvailableCamerasAsync(CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			List<CameraDescriptor> Cameras = new List<CameraDescriptor>();
			AVCaptureDeviceType[] DeviceTypes = new[]
			{
				AVCaptureDeviceType.BuiltInWideAngleCamera,
				AVCaptureDeviceType.BuiltInUltraWideCamera,
				AVCaptureDeviceType.BuiltInDualCamera,
				AVCaptureDeviceType.BuiltInDualWideCamera,
				AVCaptureDeviceType.BuiltInTripleCamera,
				AVCaptureDeviceType.BuiltInTrueDepthCamera
			};
			AVCaptureDeviceDiscoverySession Session = AVCaptureDeviceDiscoverySession.Create(DeviceTypes, AVMediaTypes.Video, AVCaptureDevicePosition.Unspecified);
			HashSet<string> UniqueDeviceIds = new HashSet<string>(StringComparer.Ordinal);

			foreach (AVCaptureDevice Device in Session.Devices)
			{
				CancellationToken.ThrowIfCancellationRequested();
				if (!UniqueDeviceIds.Add(Device.UniqueID))
					continue;

				CameraPosition Position = Device.Position switch
				{
					AVCaptureDevicePosition.Front => CameraPosition.Front,
					AVCaptureDevicePosition.Back => CameraPosition.Rear,
					_ => CameraPosition.Unknown
				};

				bool SupportsTorch = Device.HasTorch;
				bool SupportsFocus = Device.FocusPointOfInterestSupported;
				bool SupportsZoom = Device.ActiveFormat.VideoMaxZoomFactor > 1.0f;

				CameraDescriptor Descriptor = new CameraDescriptor(Device.UniqueID, Position, SupportsTorch, SupportsFocus, SupportsZoom);
				Cameras.Add(Descriptor);
			}

			IReadOnlyList<CameraDescriptor> OrderedCameras = Cameras
				.OrderByDescending(Descriptor => Descriptor.Position == CameraPosition.Rear)
				.ThenByDescending(Descriptor => Descriptor.Position == CameraPosition.Front)
				.ToList();

			return Task.FromResult(OrderedCameras);
		}
	}
}
#endif
