namespace IdApp.Cv.Statistics
{
	/// <summary>
	/// Static class for Statistical Operations, implemented as extensions.
	/// </summary>
	public static partial class StatisticsOperations
	{
		/// <summary>
		/// Computes the average value of all values in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static float Average(this Matrix<float> M)
		{
			return M.Sum() / (M.Width * M.Height);
		}

		/// <summary>
		/// Computes the average value of all values in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static int Average(this Matrix<int> M)
		{
			return (int)(M.Sum() / (M.Width * M.Height));
		}
	}
}
