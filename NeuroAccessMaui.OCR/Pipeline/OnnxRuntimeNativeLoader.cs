namespace NeuroAccessMaui.OCR.Pipeline
{
	internal static class OnnxRuntimeNativeLoader
	{
		private static readonly object syncRoot = new object();
		private static bool loaded;

		internal static void EnsureLoaded()
		{
			lock (syncRoot)
			{
				if (loaded)
					return;

#if ANDROID
				Java.Lang.JavaSystem.LoadLibrary("onnxruntime");
#endif
				loaded = true;
			}
		}
	}
}
