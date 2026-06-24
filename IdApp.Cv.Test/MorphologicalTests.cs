using System;
using IdApp.Cv;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Transformations.Morphological;
using IdApp.Cv.Transformations.Thresholds;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdApp.Cv.Test
{
	[TestClass]
	public class MorphologicalTests
	{
		[TestMethod]
		public void Test_01_Erode3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Erode(3);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_01_Erode3.png");
		}

		[TestMethod]
		public void Test_02_Erode5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Erode(5);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_02_Erode5.png");
		}

		[TestMethod]
		public void Test_03_Erode7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Erode(7);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_03_Erode7.png");
		}

		[TestMethod]
		public void Test_04_Dilate3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Dilate(3);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_04_Dilate3.png");
		}

		[TestMethod]
		public void Test_05_Dilate5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Dilate(5);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_05_Dilate5.png");
		}

		[TestMethod]
		public void Test_06_Dilate7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Dilate(7);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_06_Dilate7.png");
		}

		[TestMethod]
		public void Test_07_Open3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Open(3);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_07_Open3.png");
		}

		[TestMethod]
		public void Test_08_Open5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Open(5);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_08_Open5.png");
		}

		[TestMethod]
		public void Test_09_Open7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Open(7);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_09_Open7.png");
		}

		[TestMethod]
		public void Test_10_Close3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Close(3);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_10_Close3.png");
		}

		[TestMethod]
		public void Test_11_Close5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Close(5);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_11_Close5.png");
		}

		[TestMethod]
		public void Test_12_Close7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Close(7);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_12_Close7.png");
		}

		[TestMethod]
		public void Test_13_HighlightFeatures3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.HighlightFeatures_3x3(-0.1f);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_13_HighlightFeatures3.png");
		}

		[TestMethod]
		public void Test_14_HighlightFeatures5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.HighlightFeatures_5x5(-0.1f);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_14_HighlightFeatures5.png");
		}

		[TestMethod]
		public void Test_15_HighlightFeatures7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.HighlightFeatures_7x7(-0.1f);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_15_HighlightFeatures7.png");
		}

		[TestMethod]
		public void Test_16_WhiteHat3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.WhiteHat(3);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_16_WhiteHat3.png");
		}

		[TestMethod]
		public void Test_17_WhiteHat5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.WhiteHat(5);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_17_WhiteHat5.png");
		}

		[TestMethod]
		public void Test_18_WhiteHat7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.WhiteHat(7);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_18_WhiteHat7.png");
		}

		[TestMethod]
		public void Test_19_WhiteHat21()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.WhiteHat(21);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_19_WhiteHat21.png");
		}

		[TestMethod]
		public void Test_20_WhiteHat41()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.WhiteHat(41);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_20_WhiteHat41.png");
		}

		[TestMethod]
		public void Test_21_BlackHat3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.BlackHat(3);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_21_BlackHat3.png");
		}

		[TestMethod]
		public void Test_22_BlackHat5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.BlackHat(5);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_22_BlackHat5.png");
		}

		[TestMethod]
		public void Test_23_BlackHat7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.BlackHat(7);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_23_BlackHat7.png");
		}

		[TestMethod]
		public void Test_24_BlackHat21()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.BlackHat(21);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_24_BlackHat21.png");
		}

		[TestMethod]
		public void Test_25_BlackHat41()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.BlackHat(41);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_25_BlackHat41.png");
		}

		[TestMethod]
		public void Test_26_MorphologicalGradient3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.MorphologicalGradient(3);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_26_MorphologicalGradient3.png");
		}

		[TestMethod]
		public void Test_27_MorphologicalGradient5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.MorphologicalGradient(5);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_27_MorphologicalGradient5.png");
		}

		[TestMethod]
		public void Test_28_MorphologicalGradient7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.MorphologicalGradient(7);
			Bitmaps.ToImageFile(G2, "Results\\Morphological\\Test_28_MorphologicalGradient7.png");
		}
	}
}
