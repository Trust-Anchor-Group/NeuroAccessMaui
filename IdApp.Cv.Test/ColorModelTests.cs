using System;
using IdApp.Cv;
using IdApp.Cv.ColorModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdApp.Cv.Test
{
	[TestClass]
	public class ColorModelTests
	{
		[TestMethod]
		public void Test_01_Grayscale()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass2-uto.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Bitmaps.ToImageFile(G, "Results\\ColorModels\\Test_01_Grayscale.png");
		}

		[TestMethod]
		public void Test_02_ReduceColors()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass2-uto.jpg", 600, 600);
			IMatrix G = M.ReduceColors(4);
			Bitmaps.ToImageFile(G, "Results\\ColorModels\\Test_02_ReduceColors.png");
		}

		[TestMethod]
		public void Test_03_ReduceColorsBW()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass2-uto.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.ReduceColors(4);
			Bitmaps.ToImageFile(G, "Results\\ColorModels\\Test_03_ReduceColorsBW.png");
		}

		[TestMethod]
		public void Test_04_ReduceColorsBinary()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass2-uto.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.ReduceColors(2);
			Bitmaps.ToImageFile(G, "Results\\ColorModels\\Test_04_ReduceColorsBinary.png");
		}

		[TestMethod]
		public void Test_05_Grayscale_FP()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass2-uto.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			Bitmaps.ToImageFile(G, "Results\\ColorModels\\Test_05_Grayscale_FP.png");
		}

		[TestMethod]
		public void Test_06_ReduceColorsBW_FP()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass2-uto.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.ReduceColors(4);
			Bitmaps.ToImageFile(G, "Results\\ColorModels\\Test_06_ReduceColorsBW_FP.png");
		}

		[TestMethod]
		public void Test_07_ReduceColorsBinary_FP()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\100_pass2-uto.jpg", 600, 600);
			IMatrix G = M.GrayScaleFixed();
			G = G.ReduceColors(2);
			Bitmaps.ToImageFile(G, "Results\\ColorModels\\Test_07_ReduceColorsBinary_FP.png");
		}
	}
}
