using System;
using IdApp.Cv;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Transformations.Convolutions;
using IdApp.Cv.Transformations.Thresholds;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdApp.Cv.Test
{
	[TestClass]
	public class ConvolutionFixedTests
	{
		[TestMethod]
		public void Test_01_Sharpen()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.Sharpen();
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_01_Sharpen.png");
		}

		[TestMethod]
		public void Test_02_Blur_3x3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.Blur();
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_02_Blur.png");
		}

		[TestMethod]
		public void Test_03_Blur_5x5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.Blur(5);
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_03_Blur_5x5.png");
		}


		[TestMethod]
		public void Test_04_Blur_7x7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.Blur(7);
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_04_Blur_7x7.png");
		}

		[TestMethod]
		public void Test_05_GaussianBlur_3x3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.GaussianBlur(3);
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_05_GaussianBlur_3x3.png");
		}

		[TestMethod]
		public void Test_06_GaussianBlur_5x5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.GaussianBlur(5);
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_06_GaussianBlur_5x5.png");
		}

		[TestMethod]
		public void Test_07_GaussianBlur_7x7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.GaussianBlur(7);
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_07_GaussianBlur_7x7.png");
		}

		[TestMethod]
		public void Test_08_DetectHorizontalLines()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.DetectHorizontalLines();
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_08_DetectHorizontalLines.png");
		}

		[TestMethod]
		public void Test_09_DetectVerticalLines()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.DetectVerticalLines();
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_09_DetectVerticalLines.png");
		}

		[TestMethod]
		public void Test_10_Detect45DegreeLines()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.Detect45DegreeLines();
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_10_Detect45DegreeLines.png");
		}

		[TestMethod]
		public void Test_11_Detect135DegreeLines()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.Detect135DegreeLines();
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_11_Detect135DegreeLines.png");
		}

		[TestMethod]
		public void Test_12_DetectEdges()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.DetectEdges();
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_12_DetectEdges.png");
		}

		[TestMethod]
		public void Test_13_DetectHorizontalLinesSobel()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.DetectEdgesSobelHorizontal();
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_13_DetectHorizontalLinesSobel.png");
		}

		[TestMethod]
		public void Test_14_DetectVerticalLinesSobel()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.DetectEdgesSobelVertical();
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_14_DetectVerticalLinesSobel.png");
		}

		[TestMethod]
		public void Test_15_DetectEdgesLaplacian()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.DetectEdgesLaplacian();
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_15_DetectEdgesLaplacian.png");
		}

		[TestMethod]
		public void Test_16_DetectEdgesLaplacianWithGauss()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.GaussianBlur(5, 1.25f);
			G = G.DetectEdgesLaplacian();
			((Matrix<int>)G).Threshold(0x1000000 / 10);
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_16_DetectEdgesLaplacianWithGauss.png");
		}

		[TestMethod]
		public void Test_17_DetectHorizontalLinesSharr()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.DetectEdgesSharrHorizontal();
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_17_DetectHorizontalLinesSharr.png");
		}

		[TestMethod]
		public void Test_18_DetectVerticalLinesSharr()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.DetectEdgesSharrVertical();
			Bitmaps.ToImageFile(G, "Results\\ConvolutionsFixed\\Test_18_DetectVerticalLinesSharr.png");
		}
	}
}
