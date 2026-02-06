using System;
using IdApp.Cv;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Statistics;
using IdApp.Cv.Transformations;
using IdApp.Cv.Transformations.Thresholds;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdApp.Cv.Test
{
	[TestClass]
	public class TransformationTests
	{
		[TestMethod]
		public void Test_01_Contrast()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\98_pass-nld.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			Bitmaps.ToImageFile(G2, "Results\\Transformation\\Test_01_Contrast.0.Before.png");
			G2.Range(out float Min, out float Max);
			G2.Contrast(Min, Max);
			Bitmaps.ToImageFile(G2, "Results\\Transformation\\Test_01_Contrast.1.After.png");
		}

		[TestMethod]
		public void Test_02_Threshold()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			Bitmaps.ToImageFile(G2, "Results\\Transformation\\Test_02_Threshold.0.Before.png");
			G2.Threshold(0.5f, 1);
			Bitmaps.ToImageFile(G2, "Results\\Transformation\\Test_02_Threshold.1.After.png");
		}

		[TestMethod]
		public void Test_03_AdaptiveThreshold()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			Bitmaps.ToImageFile(G2, "Results\\Transformation\\Test_03_AdaptiveThreshold.0.Before.png");
			G2.AdaptiveThreshold(0.2f, 21);
			Bitmaps.ToImageFile(G2, "Results\\Transformation\\Test_03_AdaptiveThreshold.1.After.png");
		}
	}
}
