using IdApp.Cv.Arithmetics;
using IdApp.Cv.Basic;
using IdApp.Cv.Transformations.Thresholds;

namespace IdApp.Cv.Transformations.Morphological
{
	/// <summary>
	/// Static class for Morphological Operations, implemented as extensions.
	/// </summary>
	public static partial class MorphologicalOperations
	{
		/// <summary>
		/// Highlights features in a matrix, utilizing a sequence of morphological filters.
		/// 3x3 morphological shapes are used during the transformation.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		public static Matrix<float> HighlightFeatures_3x3(this Matrix<float> M, float Threshold)
		{
			return M.HighlightFeatures_3x3(Threshold, 0f, 1f);
		}

		/// <summary>
		/// Highlights features in a matrix, utilizing a sequence of morphological filters.
		/// 3x3 morphological shapes are used during the transformation.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		/// <param name="MinThreshold">Minimum threshold</param>
		/// <param name="MaxThreshold">Maximum threshold</param>
		public static Matrix<float> HighlightFeatures_3x3(this Matrix<float> M, float Threshold,
			float MinThreshold, float MaxThreshold)
		{
			Matrix<float> Image1 = M.
				Dilate(Shape.Cross_3x3, MaxThreshold).
				Erode(Shape.Diamond_3x3, MinThreshold);

			Matrix<float> Image2 = M.
				Dilate(Shape.X_3x3, MaxThreshold).
				Erode(3, 3, MinThreshold);

			Image1.AbsoluteDifference(Image2);
			Image1.Threshold(Threshold);

			return Image1;
		}

		/// <summary>
		/// Highlights features in a matrix, utilizing a sequence of morphological filters.
		/// 5x5 morphological shapes are used during the transformation.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		public static Matrix<float> HighlightFeatures_5x5(this Matrix<float> M, float Threshold)
		{
			return M.HighlightFeatures_5x5(Threshold, 0f, 1f);
		}

		/// <summary>
		/// Highlights features in a matrix, utilizing a sequence of morphological filters.
		/// 5x5 morphological shapes are used during the transformation.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		/// <param name="MinThreshold">Minimum threshold</param>
		/// <param name="MaxThreshold">Maximum threshold</param>
		public static Matrix<float> HighlightFeatures_5x5(this Matrix<float> M, float Threshold,
			float MinThreshold, float MaxThreshold)
		{
			Matrix<float> Image1 = M.
				Dilate(Shape.Cross_5x5, MaxThreshold).
				Erode(Shape.Diamond_5x5, MinThreshold);

			Matrix<float> Image2 = M.
				Dilate(Shape.X_5x5, MaxThreshold).
				Erode(5, 5, MinThreshold);

			Image1.AbsoluteDifference(Image2);
			Image1.Threshold(Threshold);

			return Image1;
		}

		/// <summary>
		/// Highlights features in a matrix, utilizing a sequence of morphological filters.
		/// 7x7 morphological shapes are used during the transformation.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		public static Matrix<float> HighlightFeatures_7x7(this Matrix<float> M, float Threshold)
		{
			return M.HighlightFeatures_7x7(Threshold, 0f, 1f);
		}

		/// <summary>
		/// Highlights features in a matrix, utilizing a sequence of morphological filters.
		/// 7x7 morphological shapes are used during the transformation.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		/// <param name="MinThreshold">Minimum threshold</param>
		/// <param name="MaxThreshold">Maximum threshold</param>
		public static Matrix<float> HighlightFeatures_7x7(this Matrix<float> M, float Threshold,
			float MinThreshold, float MaxThreshold)
		{
			Matrix<float> Image1 = M.
				Dilate(Shape.Cross_7x7, MaxThreshold).
				Erode(Shape.Diamond_7x7, MinThreshold);

			Matrix<float> Image2 = M.
				Dilate(Shape.X_7x7, MaxThreshold).
				Erode(7, 7, MinThreshold);

			Image1.AbsoluteDifference(Image2);
			Image1.Threshold(Threshold);

			return Image1;
		}

		/// <summary>
		/// Highlights features in a matrix, utilizing a sequence of morphological filters.
		/// 3x3 morphological shapes are used during the transformation.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		public static Matrix<int> HighlightFeatures_3x3(this Matrix<int> M, int Threshold)
		{
			return M.HighlightFeatures_3x3(Threshold, 0, 0x01000000);
		}

		/// <summary>
		/// Highlights features in a matrix, utilizing a sequence of morphological filters.
		/// 3x3 morphological shapes are used during the transformation.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		/// <param name="MinThreshold">Minimum threshold</param>
		/// <param name="MaxThreshold">Maximum threshold</param>
		public static Matrix<int> HighlightFeatures_3x3(this Matrix<int> M, int Threshold,
			int MinThreshold, int MaxThreshold)
		{
			Matrix<int> Image1 = M.
				Dilate(Shape.Cross_3x3, MaxThreshold).
				Erode(Shape.Diamond_3x3, MinThreshold);

			Matrix<int> Image2 = M.
				Dilate(Shape.X_3x3, MaxThreshold).
				Erode(3, 3, MinThreshold);

			Image1.AbsoluteDifference(Image2);
			Image1.Threshold(Threshold);

			return Image1;
		}

		/// <summary>
		/// Highlights features in a matrix, utilizing a sequence of morphological filters.
		/// 5x5 morphological shapes are used during the transformation.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		public static Matrix<int> HighlightFeatures_5x5(this Matrix<int> M, int Threshold)
		{
			return M.HighlightFeatures_5x5(Threshold, 0, 0x01000000);
		}

		/// <summary>
		/// Highlights features in a matrix, utilizing a sequence of morphological filters.
		/// 5x5 morphological shapes are used during the transformation.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		/// <param name="MinThreshold">Minimum threshold</param>
		/// <param name="MaxThreshold">Maximum threshold</param>
		public static Matrix<int> HighlightFeatures_5x5(this Matrix<int> M, int Threshold,
			int MinThreshold, int MaxThreshold)
		{
			Matrix<int> Image1 = M.
				Dilate(Shape.Cross_5x5, MaxThreshold).
				Erode(Shape.Diamond_5x5, MinThreshold);

			Matrix<int> Image2 = M.
				Dilate(Shape.X_5x5, MaxThreshold).
				Erode(5, 5, MinThreshold);

			Image1.AbsoluteDifference(Image2);
			Image1.Threshold(Threshold);

			return Image1;
		}

		/// <summary>
		/// Highlights features in a matrix, utilizing a sequence of morphological filters.
		/// 7x7 morphological shapes are used during the transformation.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		public static Matrix<int> HighlightFeatures_7x7(this Matrix<int> M, int Threshold)
		{
			return M.HighlightFeatures_7x7(Threshold, 0, 0x01000000);
		}

		/// <summary>
		/// Highlights features in a matrix, utilizing a sequence of morphological filters.
		/// 7x7 morphological shapes are used during the transformation.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		/// <param name="MinThreshold">Minimum threshold</param>
		/// <param name="MaxThreshold">Maximum threshold</param>
		public static Matrix<int> HighlightFeatures_7x7(this Matrix<int> M, int Threshold,
			int MinThreshold, int MaxThreshold)
		{
			Matrix<int> Image1 = M.
				Dilate(Shape.Cross_7x7, MaxThreshold).
				Erode(Shape.Diamond_7x7, MinThreshold);

			Matrix<int> Image2 = M.
				Dilate(Shape.X_7x7, MaxThreshold).
				Erode(7, 7, MinThreshold);

			Image1.AbsoluteDifference(Image2);
			Image1.Threshold(Threshold);

			return Image1;
		}
	}
}
