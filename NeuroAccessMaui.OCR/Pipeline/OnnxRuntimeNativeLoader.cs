#if ANDROID
using Microsoft.Maui.ApplicationModel;
#endif

namespace NeuroAccessMaui.OCR.Pipeline
{
	/// <summary>
	/// Loads the ONNX Runtime native library once per process.
	/// </summary>
	/// <remarks>
	/// On Android, <c>System.loadLibrary</c> finds the app's native libraries through the class loader of the Java code
	/// that calls it. A .NET thread-pool thread has no Java code on its stack, so the call only searches the system
	/// library folders and fails. The library is therefore always loaded on the main thread.
	/// </remarks>
	internal static class OnnxRuntimeNativeLoader
	{
		private static readonly object syncRoot = new object();
		private static volatile bool loaded;

		/// <summary>
		/// Ensures the native library is loaded. Off the main thread this waits for the main thread, so callers must not
		/// hold a lock that the main thread may also take.
		/// </summary>
		internal static void EnsureLoaded()
		{
			if (loaded)
				return;

#if ANDROID
			if (MainThread.IsMainThread)
				Load();
			else
				MainThread.InvokeOnMainThreadAsync(Load).GetAwaiter().GetResult();
#else
			loaded = true;
#endif
		}

#if ANDROID
		private static void Load()
		{
			lock (syncRoot)
			{
				if (loaded)
					return;

				Java.Lang.JavaSystem.LoadLibrary("onnxruntime");
				loaded = true;
			}
		}
#endif
	}
}
