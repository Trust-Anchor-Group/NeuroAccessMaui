using Android.App;
using Android.Runtime;

namespace DevStorybook
{
	/// <summary>
	/// Provides the Android application entry point for the developer Storybook.
	/// </summary>
	[Application]
	public class MainApplication : MauiApplication
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MainApplication"/> class.
		/// </summary>
		/// <param name="Handle">The Java native handle.</param>
		/// <param name="Ownership">The handle ownership mode.</param>
		public MainApplication(IntPtr Handle, JniHandleOwnership Ownership)
			: base(Handle, Ownership)
		{
		}

		/// <inheritdoc/>
		protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
	}
}
