using Android.App;
using Android.Runtime;

namespace NeuroAccessMaui
{
#if DEBUG
	[Application(UsesCleartextTraffic = true)]
#else
	[Application]
#endif
	public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
	{
		protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
	}
}
