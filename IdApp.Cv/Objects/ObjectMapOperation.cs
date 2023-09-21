namespace IdApp.Cv.Objects
{
	/// <summary>
	/// Static class for Object Operations, implemented as extensions.
	/// </summary>
	public static partial class ObjectOperations
	{
		/// <summary>
		/// Computes an object map of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold to use when identifying objects.</param>
		public static ObjectMap ObjectMap(this Matrix<float> M, float Threshold)
		{
			return new ObjectMap(M, Threshold);
		}

		/// <summary>
		/// Computes an object map of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold to use when identifying objects.</param>
		public static ObjectMap ObjectMap(this Matrix<int> M, int Threshold)
		{
			return new ObjectMap(M, Threshold);
		}
	}
}
