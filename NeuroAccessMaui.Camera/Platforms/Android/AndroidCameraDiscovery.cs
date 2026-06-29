#if ANDROID
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Hardware.Camera2;
using static Android.Icu.Text.Transliterator;
using JavaBoolean = Java.Lang.Boolean;
using JavaFloat = Java.Lang.Float;
using JavaInteger = Java.Lang.Integer;

namespace NeuroAccessMaui.Camera.Platforms.Android
{
	/// <summary>
	/// Provides Android camera discovery.
	/// </summary>
	internal static class AndroidCameraDiscovery
	{
		public static Task<IReadOnlyList<CameraDescriptor>> GetAvailableCamerasAsync(CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			List<CameraDescriptor> Cameras = new List<CameraDescriptor>();
			Context Context = global::Android.App.Application.Context;
			CameraManager Manager = (CameraManager)Context.GetSystemService(Context.CameraService);
			string[] CameraIds = Manager.GetCameraIdList();

			foreach (string CameraId in CameraIds)
			{
				CancellationToken.ThrowIfCancellationRequested();

				CameraCharacteristics Characteristics = Manager.GetCameraCharacteristics(CameraId);
				JavaInteger? LensFacingValue = Characteristics.Get(CameraCharacteristics.LensFacing) as JavaInteger;
				CameraPosition Position = CameraPosition.Unknown;

				if (LensFacingValue is not null)
				{
					if (LensFacingValue.IntValue() == (int)LensFacing.Front)
						Position = CameraPosition.Front;
					else if (LensFacingValue.IntValue() == (int)LensFacing.Back)
						Position = CameraPosition.Rear;
				}

				JavaBoolean? FlashAvailable = Characteristics.Get(CameraCharacteristics.FlashInfoAvailable) as JavaBoolean;
				bool SupportsTorch = FlashAvailable?.BooleanValue() ?? false;

				int[]? FocusModes = (int[]?)Characteristics.Get(CameraCharacteristics.ControlAfAvailableModes);
				bool SupportsFocus = FocusModes is not null && FocusModes.Length > 0;

				JavaFloat? MaxZoom = Characteristics.Get(CameraCharacteristics.ScalerAvailableMaxDigitalZoom) as JavaFloat;
				bool SupportsZoom = MaxZoom is not null && MaxZoom.FloatValue() > 1f;

				CameraDescriptor Descriptor = new CameraDescriptor(CameraId, Position, SupportsTorch, SupportsFocus, SupportsZoom);
				Cameras.Add(Descriptor);
			}

			return Task.FromResult<IReadOnlyList<CameraDescriptor>>(Cameras);
		}
	}
}
#endif
