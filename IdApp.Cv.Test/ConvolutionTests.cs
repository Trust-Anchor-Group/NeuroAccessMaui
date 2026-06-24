using System;
using IdApp.Cv;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Transformations.Convolutions;
using IdApp.Cv.Transformations.Thresholds;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdApp.Cv.Test
{
	[TestClass]
	public class ConvolutionTests
	{
		[TestMethod]
		public void Test_01_Sharpen()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.Sharpen();
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_01_Sharpen.png");
		}

		[TestMethod]
		public void Test_02_Blur_3x3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.Blur();
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_02_Blur.png");
		}

		[TestMethod]
		public void Test_03_Blur_5x5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.Blur(5);
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_03_Blur_5x5.png");
		}


		[TestMethod]
		public void Test_04_Blur_7x7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.Blur(7);
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_04_Blur_7x7.png");
		}

		[TestMethod]
		public void Test_05_GaussianBlur_3x3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.GaussianBlur(3);
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_05_GaussianBlur_3x3.png");
		}

		[TestMethod]
		public void Test_06_GaussianBlur_5x5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.GaussianBlur(5);
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_06_GaussianBlur_5x5.png");
		}

		[TestMethod]
		public void Test_07_GaussianBlur_7x7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.GaussianBlur(7);
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_07_GaussianBlur_7x7.png");
		}

		[TestMethod]
		public void Test_08_DetectHorizontalLines()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.DetectHorizontalLines();
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_08_DetectHorizontalLines.png");
		}

		[TestMethod]
		public void Test_09_DetectVerticalLines()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.DetectVerticalLines();
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_09_DetectVerticalLines.png");
		}

		[TestMethod]
		public void Test_10_Detect45DegreeLines()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.Detect45DegreeLines();
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_10_Detect45DegreeLines.png");
		}

		[TestMethod]
		public void Test_11_Detect135DegreeLines()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.Detect135DegreeLines();
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_11_Detect135DegreeLines.png");
		}

		[TestMethod]
		public void Test_12_DetectEdges()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.DetectEdges();
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_12_DetectEdges.png");
		}

		[TestMethod]
		public void Test_13_DetectHorizontalLinesSobel()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.DetectEdgesSobelHorizontal();
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_13_DetectHorizontalLinesSobel.png");
		}

		[TestMethod]
		public void Test_14_DetectVerticalLinesSobel()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.DetectEdgesSobelVertical();
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_14_DetectVerticalLinesSobel.png");
		}

		[TestMethod]
		public void Test_15_DetectEdgesLaplacian()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.DetectEdgesLaplacian();
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_15_DetectEdgesLaplacian.png");
		}

		[TestMethod]
		public void Test_16_DetectEdgesLaplacianWithGauss()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.GaussianBlur(5, 1.25f);
			G = G.DetectEdgesLaplacian();
			((Matrix<float>)G).Threshold(0.1f);
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_16_DetectEdgesLaplacianWithGauss.png");
		}

		[TestMethod]
		public void Test_17_DetectHorizontalLinesSharr()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.DetectEdgesSharrHorizontal();
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_17_DetectHorizontalLinesSharr.png");
		}

		[TestMethod]
		public void Test_18_DetectVerticalLinesSharr()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.DetectEdgesSharrVertical();
			Bitmaps.ToImageFile(G, "Results\\Convolutions\\Test_18_DetectVerticalLinesSharr.png");
		}
	}
}
