using System;
using IdApp.Cv;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Transformations.Linear;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdApp.Cv.Test
{
	[TestClass]
	public class LinearTransformationTests
	{
		[TestMethod]
		public void Test_01_Half()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\98_pass-nld.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);

			Matrix<float> T = new Matrix<float>(3, 3, new float[]
			{
				0.5f, 0,    0,
				0,    0.5f, 0,
				0,    0,    1
			});

			G2 = G2.LinearTransform(T, M.Width / 2, M.Height / 2);

			Bitmaps.ToImageFile(G2, "Results\\LinearTransformation\\Test_01_Half.png");
		}

		[TestMethod]
		public void Test_02_Double()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\98_pass-nld.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);

			Matrix<float> T = new Matrix<float>(3, 3, new float[]
			{
				2, 0, 0,
				0, 2, 0,
				0, 0, 1
			});

			G2 = G2.LinearTransform(T, M.Width * 2, M.Height * 2);

			Bitmaps.ToImageFile(G2, "Results\\LinearTransformation\\Test_02_Double.png");
		}

		[TestMethod]
		public void Test_03_Rotate45()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\98_pass-nld.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);

			Matrix<float> T = new Matrix<float>(3, 3, new float[]
			{
				0.7071067811865476f, 0.7071067811865475f, -50.18585822512665f,
				-0.7071067811865475f, 0.7071067811865476f, 208.84062043356593f,
				0, 0, 1
			});

			G2 = G2.LinearTransform(T, M.Width, M.Height);

			Bitmaps.ToImageFile(G2, "Results\\LinearTransformation\\Test_03_Rotate45.png");
		}

		[TestMethod]
		public void Test_04_Rotate90()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\98_pass-nld.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);

			G2 = G2.Rotate90();

			Bitmaps.ToImageFile(G2, "Results\\LinearTransformation\\Test_04_Rotate90.png");
		}

		[TestMethod]
		public void Test_05_Rotate180()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\98_pass-nld.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);

			G2 = G2.Rotate180();

			Bitmaps.ToImageFile(G2, "Results\\LinearTransformation\\Test_05_Rotate180.png");
		}

		[TestMethod]
		public void Test_06_Rotate270()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\98_pass-nld.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);

			G2 = G2.Rotate270();

			Bitmaps.ToImageFile(G2, "Results\\LinearTransformation\\Test_06_Rotate270.png");
		}
	}
}
