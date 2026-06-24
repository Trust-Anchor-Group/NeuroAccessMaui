using IdApp.Cv;
using IdApp.Cv.Arithmetics;
using IdApp.Cv.Basic;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Objects;
using IdApp.Cv.Transformations;
using IdApp.Cv.Transformations.Convolutions;
using IdApp.Cv.Transformations.Morphological;
using IdApp.Cv.Transformations.Thresholds;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace IdApp.Cv.Test
{
	[TestClass]
	public class MrzMethodTests
	{
		private const string SourceFile = "TestData\\mrz_original.jpg";
		//private const string SourceFile = "TestData\\100_visa-usa.jpg";
		//private const string SourceFile = "TestData\\0_pass-lva.jpg";
		//private const string SourceFile = "TestData\\24_pass-egy.jpg";
		//private const string SourceFile = "TestData\\27_pass-gbr.jpg";
		//private const string SourceFile = "C:\\Temp\\1\\1.png";
		//private const string SourceFile = "C:\\Temp\\1\\2.png";

		// See also: https://www.pyimagesearch.com/2015/11/30/detecting-machine-readable-zones-in-passport-images/

		[TestMethod]
		public void Test_01_Resize()
		{
			IMatrix M = Bitmaps.FromBitmapFile(SourceFile, 600, 600);
			Bitmaps.ToImageFile(M, "Results\\MrzMethod\\Test_01_Resize.png");
		}

		[TestMethod]
		public void Test_02_Gray()
		{
			IMatrix M = Bitmaps.FromBitmapFile(SourceFile, 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			Bitmaps.ToImageFile(G, "Results\\MrzMethod\\Test_02_Gray.png");
		}

		[TestMethod]
		public void Test_03_Blur()
		{
			IMatrix M = Bitmaps.FromBitmapFile(SourceFile, 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			Bitmaps.ToImageFile(G, "Results\\MrzMethod\\Test_03_Blur.png");
		}

		[TestMethod]
		public void Test_04_BlackHat()
		{
			IMatrix M = Bitmaps.FromBitmapFile(SourceFile, 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			Bitmaps.ToImageFile(G, "Results\\MrzMethod\\Test_04_BlackHat.png");
		}

		[TestMethod]
		public void Test_05_Sharr()
		{
			IMatrix M = Bitmaps.FromBitmapFile(SourceFile, 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrHorizontal();     // Detect horizontal edges=detect vertical changes
			Bitmaps.ToImageFile(G, "Results\\MrzMethod\\Test_05_Sharr.png");
		}

		[TestMethod]
		public void Test_06_Abs()
		{
			IMatrix M = Bitmaps.FromBitmapFile(SourceFile, 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			Bitmaps.ToImageFile(G, "Results\\MrzMethod\\Test_06_Abs.png");
		}

		[TestMethod]
		public void Test_07_Contrast()
		{
			IMatrix M = Bitmaps.FromBitmapFile(SourceFile, 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			Bitmaps.ToImageFile(G, "Results\\MrzMethod\\Test_07_Contrast.png");
		}

		[TestMethod]
		public void Test_08_Close()
		{
			IMatrix M = Bitmaps.FromBitmapFile(SourceFile, 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			Bitmaps.ToImageFile(G, "Results\\MrzMethod\\Test_08_Close.png");
		}

		[TestMethod]
		public void Test_09_OtsuThreshold()
		{
			IMatrix M = Bitmaps.FromBitmapFile(SourceFile, 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			float Otsu = G.OtsuThreshold();
			Console.Out.WriteLine("Otsu threshold: " + Otsu);
			G.Threshold(Otsu);
			Bitmaps.ToImageFile(G, "Results\\MrzMethod\\Test_09_OtsuThreshold.png");
		}

		[TestMethod]
		public void Test_10_ObjectMap()
		{
			IMatrix M = Bitmaps.FromBitmapFile(SourceFile, 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G.Threshold(G.OtsuThreshold());

			Matrix<uint> Borders = new Matrix<uint>(G.Width, G.Height);
			ObjectMap ObjectMap = G.ObjectMap(0.5f);
			ushort i;
			int c = ObjectMap.NrObjects;

			Borders.Fill(0xff000000);

			for (i = 0; i < c; i++)
			{
				ObjectInformation Info = ObjectMap[i];

				foreach (Point Point in Info.Contour)
					Borders[Point.X, Point.Y] = 0xff80ff80;
			}

			Bitmaps.ToImageFile(Borders, "Results\\MrzMethod\\Test_10_ObjectMap.png");
		}

		[TestMethod]
		public void Test_11_ReduceContours()
		{
			IMatrix M = Bitmaps.FromBitmapFile(SourceFile, 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G.Threshold(G.OtsuThreshold());

			Matrix<uint> Borders = new Matrix<uint>(G.Width, G.Height);
			ObjectMap ObjectMap = G.ObjectMap(0.5f);
			ObjectInformation[] Objects = ObjectMap.Objects;

			Array.Sort(Objects, (o1, o2) => o1.MinY - o2.MinY);
			Borders.Fill(0xff000000);

			foreach (ObjectInformation Object in Objects)
			{
				float Aspect = ((float)Object.Width) / Object.Height;
				if (Aspect < 5)
					continue;

				float RelativeWidth = ((float)Object.Width) / G.Width;
				if (RelativeWidth < 0.75f)
					continue;
				
				foreach (Point Point in Object.Contour)
					Borders[Point.X, Point.Y] = 0xff80ff80;

				int i = (5 * Math.Max(M.Width, M.Height) + 300) / 600;
				Point[] Reduced = Object.Contour.Reduce(i);
				Point[] Reduced2 = Reduced.Reduce(i);
				Point Last = Reduced2[^1];
				uint Color = Reduced2.Length == 4 ? 0xffffa0a0 : 0xffa0a0ff;

				foreach (Point Point in Reduced2)
				{
					Borders.Line(Last.X, Last.Y, Point.X, Point.Y, Color);
					Last = Point;
				}
			}

			Bitmaps.ToImageFile(Borders, "Results\\MrzMethod\\Test_11_ReduceContours.png");
		}

		[TestMethod]
		public void Test_12_Candidate()
		{
			Matrix<uint> M = (Matrix<uint>)Bitmaps.FromBitmapFile(SourceFile, 600, 600);
			Matrix<float> G = M.GrayScale();
			G = G.GaussianBlur(3);
			Matrix<float> ForOcr = G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G.Threshold(G.OtsuThreshold());

			ObjectMap ObjectMap = G.ObjectMap(0.5f);
			ObjectInformation[] Objects = ObjectMap.Objects;

			Array.Sort(Objects, (o1, o2) => o1.MinY - o2.MinY);

			List<ushort> Found = new List<ushort>();

			foreach (ObjectInformation Object in Objects)
			{
				float Aspect = ((float)Object.Width) / Object.Height;
				if (Aspect < 5)
					continue;

				float RelativeWidth = ((float)Object.Width) / G.Width;
				if (RelativeWidth < 0.75f)
					continue;

				//int i = (5 * Math.Max(M.Width, M.Height) + 300) / 600;
				//Point[] Reduced = Object.Contour.Reduce(i);
				//Point[] Reduced2 = Reduced.Reduce(i);
				//if (Reduced2.Length != 4)
				//	continue;

				Found.Add(Object.Nr);
			}

			if (Found.Count == 0)
			{
				if (File.Exists("Results\\MrzMethod\\Test_12_Candidate.png"))
					File.Delete("Results\\MrzMethod\\Test_12_Candidate.png");

				Assert.Fail("Candidates not found.");
			}
			else
			{
				Matrix<float> SubRegion = ForOcr.Region((ForOcr.Width - G.Width) / 2, (ForOcr.Height - G.Height) / 2, G.Width, G.Height);
				IMatrix Obj = ObjectMap.Extract(Found.ToArray(), SubRegion);
				Bitmaps.ToImageFile(Obj, "Results\\MrzMethod\\Test_12_Candidate.png");
				return;
			}
		}

	}
}
