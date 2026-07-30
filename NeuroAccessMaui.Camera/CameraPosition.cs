namespace NeuroAccessMaui.Camera
{
	/// <summary>
	/// Specifies the position of a camera relative to the device.
	/// </summary>
	public enum CameraPosition
	{
		/// <summary>
		/// The camera position is not known.
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// The camera faces the user.
		/// </summary>
		Front = 1,

		/// <summary>
		/// The camera faces away from the user.
		/// </summary>
		Rear = 2
	}
}
