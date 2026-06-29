using System;

namespace NeuroAccessMaui.Camera
{
	/// <summary>
	/// Describes a camera device exposed by the platform.
	/// </summary>
	public sealed class CameraDescriptor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CameraDescriptor"/> class.
		/// </summary>
		/// <param name="Id">The platform-specific camera identifier.</param>
		/// <param name="Position">The camera position.</param>
		/// <param name="SupportsTorch">Indicates whether the camera supports torch mode.</param>
		/// <param name="SupportsFocus">Indicates whether the camera supports focus point control.</param>
		/// <param name="SupportsZoom">Indicates whether the camera supports zoom control.</param>
		public CameraDescriptor(
			string Id,
			CameraPosition Position,
			bool SupportsTorch,
			bool SupportsFocus,
			bool SupportsZoom)
		{
			ArgumentNullException.ThrowIfNull(Id);

			this.Id = Id;
			this.Position = Position;
			this.SupportsTorch = SupportsTorch;
			this.SupportsFocus = SupportsFocus;
			this.SupportsZoom = SupportsZoom;
		}

		/// <summary>
		/// Gets the platform-specific camera identifier.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Gets the camera position.
		/// </summary>
		public CameraPosition Position { get; }

		/// <summary>
		/// Gets a value indicating whether the camera supports torch mode.
		/// </summary>
		public bool SupportsTorch { get; }

		/// <summary>
		/// Gets a value indicating whether the camera supports focus point control.
		/// </summary>
		public bool SupportsFocus { get; }

		/// <summary>
		/// Gets a value indicating whether the camera supports zoom control.
		/// </summary>
		public bool SupportsZoom { get; }
	}
}
