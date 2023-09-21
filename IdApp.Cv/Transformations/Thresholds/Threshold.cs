namespace IdApp.Cv.Transformations.Thresholds
{
	/// <summary>
	/// Static class for Transformation Operations, implemented as extensions.
	/// </summary>
	public static partial class TransformationOperations
	{
		/// <summary>
		/// Creates a black and white image based on the threshold level provided in
		/// <paramref name="Threshold"/>. Values at or above the threshold value become 1, 
		/// and values below become 0.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		public static void Threshold(this Matrix<float> M, float Threshold)
		{
			if (Threshold < 0)
				M.Threshold(float.MinValue, -Threshold);
			else
				M.Threshold(Threshold, float.MaxValue);
		}

		/// <summary>
		/// Creates a black and white image based on the threshold levels provided in
		/// <paramref name="Min"/> and <paramref name="Max"/>.
		/// If Min &lt; Max, values between Min and Max become 1, and values outside of the
		/// range become 0. If Min &gt; Max, values between Max and Min become 0 and values
		/// outside of the range become 1.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Min">Minimum threshold value.</param>
		/// <param name="Max">Maximum threshold value.</param>
		public static void Threshold(this Matrix<float> M, float Min, float Max)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			float[] Data = M.Data;
			float v;

			if (Min <= Max)
			{
				for (y = 0; y < h; y++, Index += Skip)
				{
					for (x = 0; x < w; x++)
					{
						v = Data[Index];
						Data[Index++] = v >= Min && v <= Max ? 1 : 0;
					}
				}
			}
			else
			{
				for (y = 0; y < h; y++, Index += Skip)
				{
					for (x = 0; x < w; x++)
					{
						v = Data[Index];
						Data[Index++] = v >= Min || v <= Max ? 1 : 0;
					}
				}
			}
		}

		/// <summary>
		/// Creates a black and white image based on the threshold level provided in
		/// <paramref name="Threshold"/>. Values at or above the threshold value become 1, 
		/// and values below become 0.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		public static void Threshold(this Matrix<int> M, int Threshold)
		{
			if (Threshold < 0)
				M.Threshold(int.MinValue, -Threshold);
			else
				M.Threshold(Threshold, int.MaxValue);
		}

		/// <summary>
		/// Creates a black and white image based on the threshold levels provided in
		/// <paramref name="Min"/> and <paramref name="Max"/>.
		/// If Min &lt; Max, values between Min and Max become 1, and values outside of the
		/// range become 0. If Min &gt; Max, values between Max and Min become 0 and values
		/// outside of the range become 1.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Min">Minimum threshold value.</param>
		/// <param name="Max">Maximum threshold value.</param>
		public static void Threshold(this Matrix<int> M, int Min, int Max)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			int[] Data = M.Data;
			int v;

			if (Min <= Max)
			{
				for (y = 0; y < h; y++, Index += Skip)
				{
					for (x = 0; x < w; x++)
					{
						v = Data[Index];
						Data[Index++] = v >= Min && v <= Max ? 0x01000000 : 0;
					}
				}
			}
			else
			{
				for (y = 0; y < h; y++, Index += Skip)
				{
					for (x = 0; x < w; x++)
					{
						v = Data[Index];
						Data[Index++] = v >= Min || v <= Max ? 0x01000000 : 0;
					}
				}
			}
		}

	}
}
