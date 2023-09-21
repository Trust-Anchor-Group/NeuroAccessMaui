using IdApp.Cv.Arithmetics;
using IdApp.Cv.Basic;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Statistics;
using System;

namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Performs an image convolution on a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of convolution</param>
		/// <returns>New matrix containing the convolution result.</returns>
		public static Matrix<float> Convolute(this Matrix<float> M, Matrix<float> Kernel)
		{
			float Sum = Kernel.Sum();

			if (Sum != 1 && Sum != 0)
			{
				Kernel = Kernel.Copy();
				Kernel.ScalarDivision(Sum);
			}

			int KernelY, KernelHeight = Kernel.Height;
			int KernelX, KernelWidth = Kernel.Width;
			int KernelOffset = Kernel.Start;
			int KernelSkip = Kernel.Skip;
			float[] KernelData = Kernel.Data;
			int ResultWidth = M.Width - KernelWidth + 1;
			int ResultHeight = M.Height - KernelHeight + 1;
			Matrix<float> Result = new Matrix<float>(ResultWidth, ResultHeight);
			float Scalar;

			for (KernelY = 0; KernelY < KernelHeight; KernelY++, KernelOffset += KernelSkip)
			{
				for (KernelX = 0; KernelX < KernelWidth; KernelX++)
				{
					Scalar = KernelData[KernelOffset++];

					if (Scalar != 0)
						Result.WeightedAddition(M.Region(KernelX, KernelY, ResultWidth, ResultHeight), Scalar);
				}
			}

			return Result;
		}

		/// <summary>
		/// Performs an image convolution on a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of convolution</param>
		/// <returns>New matrix containing the convolution result.</returns>
		public static Matrix<float> Convolute(this Matrix<float> M, Matrix<int> Kernel)
		{
			float Sum = Kernel.Sum();

			if (Sum != 1 && Sum != 0)
			{
				Matrix<float> KernelF = Kernel.GrayScale();
				return M.Convolute(KernelF);
			}

			int KernelY, KernelHeight = Kernel.Height;
			int KernelX, KernelWidth = Kernel.Width;
			int KernelOffset = Kernel.Start;
			int KernelSkip = Kernel.Skip;
			int[] KernelData = Kernel.Data;
			int ResultWidth = M.Width - KernelWidth + 1;
			int ResultHeight = M.Height - KernelHeight + 1;
			Matrix<float> Result = new Matrix<float>(ResultWidth, ResultHeight);
			float Scalar;

			for (KernelY = 0; KernelY < KernelHeight; KernelY++, KernelOffset += KernelSkip)
			{
				for (KernelX = 0; KernelX < KernelWidth; KernelX++)
				{
					Scalar = KernelData[KernelOffset++];

					if (Scalar != 0)
						Result.WeightedAddition(M.Region(KernelX, KernelY, ResultWidth, ResultHeight), Scalar);
				}
			}

			return Result;
		}

		/// <summary>
		/// Performs an image convolution on a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of convolution</param>
		/// <returns>New matrix containing the convolution result.</returns>
		public static Matrix<int> Convolute(this Matrix<int> M, Matrix<int> Kernel)
		{
			long Sum = Kernel.Sum();
			if (Sum > int.MaxValue)
				throw new ArgumentException("Sum of kernel too big.", nameof(Kernel));

			int KernelY, KernelHeight = Kernel.Height;
			int KernelX, KernelWidth = Kernel.Width;
			int KernelOffset = Kernel.Start;
			int KernelSkip = Kernel.Skip;
			int[] KernelData = Kernel.Data;
			int ResultWidth = M.Width - KernelWidth + 1;
			int ResultHeight = M.Height - KernelHeight + 1;
			Matrix<int> Result = new Matrix<int>(ResultWidth, ResultHeight);
			int Scalar;

			for (KernelY = 0; KernelY < KernelHeight; KernelY++, KernelOffset += KernelSkip)
			{
				for (KernelX = 0; KernelX < KernelWidth; KernelX++)
				{
					Scalar = KernelData[KernelOffset++];

					if (Scalar != 0)
						Result.WeightedAddition(M.Region(KernelX, KernelY, ResultWidth, ResultHeight), Scalar);
				}
			}

			if (Sum != 0 && Sum != 1)
				Result.ScalarDivision((int)Sum);

			return Result;
		}

		/// <summary>
		/// Performs an image convolution on a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of convolution</param>
		/// <returns>New matrix containing the convolution result.</returns>
		public static Matrix<int> Convolute(this Matrix<int> M, Matrix<float> Kernel)
		{
			float Sum = Kernel.Sum();

			if (Sum != 1 && Sum != 0)
			{
				Kernel = Kernel.Copy();
				Kernel.ScalarDivision(Sum);
			}

			int KernelY, KernelHeight = Kernel.Height;
			int KernelX, KernelWidth = Kernel.Width;
			int KernelOffset = Kernel.Start;
			int KernelSkip = Kernel.Skip;
			float[] KernelData = Kernel.Data;
			int ResultWidth = M.Width - KernelWidth + 1;
			int ResultHeight = M.Height - KernelHeight + 1;
			Matrix<float> Result = new Matrix<float>(ResultWidth, ResultHeight);
			float Scalar;

			for (KernelY = 0; KernelY < KernelHeight; KernelY++, KernelOffset += KernelSkip)
			{
				for (KernelX = 0; KernelX < KernelWidth; KernelX++)
				{
					Scalar = KernelData[KernelOffset++];

					if (Scalar != 0)
						Result.WeightedAddition(M.Region(KernelX, KernelY, ResultWidth, ResultHeight), Scalar);
				}
			}

			float[] Data = Result.Data;
			int i, c = Data.Length;
			int[] DataFixed = new int[c];

			for (i = 0; i < c; i++)
				DataFixed[i] = (int)(Data[i] + 0.5f);

			return new Matrix<int>(ResultWidth, ResultHeight, DataFixed);
		}

		/// <summary>
		/// Performs an image convolution on a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of convolution</param>
		/// <returns>New matrix containing the convolution result.</returns>
		public static Matrix<float> Convolute(this Matrix<float> M, IMatrix Kernel)
		{
			if (Kernel is Matrix<float> K2)
				return M.Convolute(K2);
			else if (Kernel is Matrix<int> K3)
				return M.Convolute(K3);
			else
				throw new ArgumentException("Unsupported type: " + Kernel.GetType().FullName, nameof(Kernel));
		}

		/// <summary>
		/// Performs an image convolution on a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of convolution</param>
		/// <returns>New matrix containing the convolution result.</returns>
		public static Matrix<int> Convolute(this Matrix<int> M, IMatrix Kernel)
		{
			if (Kernel is Matrix<int> K2)
				return M.Convolute(K2);
			else if (Kernel is Matrix<float> K3)
				return M.Convolute(K3);
			else
				throw new ArgumentException("Unsupported type: " + Kernel.GetType().FullName, nameof(Kernel));
		}

		/// <summary>
		/// Performs an image convolution on a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of convolution</param>
		/// <returns>New matrix containing the convolution result.</returns>
		public static IMatrix Convolute(this IMatrix M, IMatrix Kernel)
		{
			if (M is Matrix<float> M2)
				return Convolute(M2, Kernel);
			else if (M is Matrix<int> M3)
				return Convolute(M3, Kernel);
			else
				throw new ArgumentException("Unsupported type: " + M.GetType().FullName, nameof(M));
		}

	}
}
