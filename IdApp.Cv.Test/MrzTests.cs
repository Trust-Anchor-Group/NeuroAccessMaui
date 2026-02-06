using IdApp.Cv;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.IO;

namespace IdApp.Cv.Test
{
	[TestClass]
	public class MrzTests
	{
		public void ExtractMrz(int TestNr, string FileName)
		{
			if (!FileName.StartsWith("C:", true, CultureInfo.InvariantCulture))
				FileName = "TestData\\" + FileName;

			IMatrix Bmp = Bitmaps.FromBitmapFile(FileName, 600, 600);
			Matrix<float> Grayscale = (Matrix<float>)Bmp.GrayScale();
			IMatrix Mrz = Grayscale.ExtractMrzRegion();
			Assert.IsNotNull(Mrz);
			Bitmaps.ToImageFile(Mrz, "Results\\Mrz\\" + TestNr.ToString("D2", CultureInfo.InvariantCulture) + "." + Path.ChangeExtension(Path.GetFileName(FileName), "png"));
			Assert.IsTrue(Mrz is Matrix<float>);
		}

		[TestMethod]
		[Ignore]
		public void Test_00()
		{
			string s = File.ReadAllText(@"C:\Temp\1\2.txt");
			byte[] Bin = Convert.FromBase64String(s);
			File.WriteAllBytes(@"C:\Temp\1\2.png", Bin);
			this.ExtractMrz(0, @"C:\Temp\1\2.png");
		}

		[TestMethod]
		public void Test_01_100_visa_usa()
		{
			this.ExtractMrz(1, "100_visa-usa.jpg");
		}

		[TestMethod]
		public void Test_02_0_id_esp()
		{
			this.ExtractMrz(2, "0_id-esp.png");
		}

		[TestMethod]
		public void Test_03_0_pass_lva()
		{
			this.ExtractMrz(3, "0_pass-lva.jpg");
		}

		[TestMethod]
		public void Test_04_24_pass_egy()
		{
			this.ExtractMrz(4, "24_pass-egy.jpg");
		}

		[TestMethod]
		public void Test_05_25_pass_uto()
		{
			this.ExtractMrz(5, "25_pass-uto.jpg");
		}

		[TestMethod]
		public void Test_06_27_pass_gbr()
		{
			this.ExtractMrz(6, "27_pass-gbr.jpg");
		}

		[TestMethod]
		public void Test_07_33_id_usa()
		{
			this.ExtractMrz(7, "33_id-usa.jpg");
		}

		[TestMethod]
		public void Test_08_43_pass_fra()
		{
			this.ExtractMrz(8, "43_pass-fra.jpg");
		}

		[TestMethod]
		public void Test_09_43_pass_twn()
		{
			this.ExtractMrz(9, "43_pass-twn.jpg");
		}

		[TestMethod]
		public void Test_10_53_id_d()
		{
			this.ExtractMrz(10, "53_id-d.jpg");
		}

		[TestMethod]
		public void Test_11_62_pass_aus()
		{
			this.ExtractMrz(11, "62_pass-aus.png");
		}

		[TestMethod]
		public void Test_12_62_pass_pol()
		{
			this.ExtractMrz(12, "62_pass-pol.png");
		}

		[TestMethod]
		public void Test_13_77_card_cmw()
		{
			this.ExtractMrz(13, "77_card-cmw.png");
		}

		[TestMethod]
		public void Test_14_79_pass_can()
		{
			this.ExtractMrz(14, "79_pass-can.jpg");
		}

		[TestMethod]
		public void Test_15_79_pass_hun()
		{
			this.ExtractMrz(15, "79_pass-hun.png");
		}

		[TestMethod]
		public void Test_16_98_pass_nld()
		{
			this.ExtractMrz(16, "98_pass-nld.jpg");
		}

		[TestMethod]
		public void Test_17_100_id_che()
		{
			this.ExtractMrz(17, "100_id-che.jpg");
		}

		[TestMethod]
		public void Test_18_100_id_mac()
		{
			this.ExtractMrz(18, "100_id-mac.jpg");
		}

		[TestMethod]
		public void Test_19_100_id_rou()
		{
			this.ExtractMrz(19, "100_id-rou.jpg");
		}

		[TestMethod]
		public void Test_20_100_id_si()
		{
			this.ExtractMrz(20, "100_id-si.jpg");
		}

		[TestMethod]
		public void Test_21_100_id_usa()
		{
			this.ExtractMrz(21, "100_id-usa.jpg");
		}

		[TestMethod]
		public void Test_22_100_pass2_uto()
		{
			this.ExtractMrz(22, "100_pass2-uto.jpg");
		}

		[TestMethod]
		public void Test_23_100_pass_bdr()
		{
			this.ExtractMrz(23, "100_pass-bdr.jpg");
		}

		[TestMethod]
		public void Test_24_100_pass_chn()
		{
			this.ExtractMrz(24, "100_pass-chn.jpg");
		}

		[TestMethod]
		public void Test_25_100_pass_cze()
		{
			this.ExtractMrz(25, "100_pass-cze.jpg");
		}

		[TestMethod]
		public void Test_26_100_pass_cze2()
		{
			this.ExtractMrz(26, "100_pass-cze2.jpg");
		}

		[TestMethod]
		public void Test_27_100_pass_fin()
		{
			this.ExtractMrz(27, "100_pass-fin.png");
		}

		[TestMethod]
		public void Test_28_100_pass_hrv()
		{
			this.ExtractMrz(28, "100_pass-hrv.jpg");
		}

		[TestMethod]
		public void Test_29_100_pass_isl()
		{
			this.ExtractMrz(29, "100_pass-isl.png");
		}

		[TestMethod]
		public void Test_30_100_pass_ltu()
		{
			this.ExtractMrz(30, "100_pass-ltu.jpg");
		}

		[TestMethod]
		public void Test_31_100_pass_lux()
		{
			this.ExtractMrz(31, "100_pass-lux.jpg");
		}

		[TestMethod]
		public void Test_32_100_pass_polx()
		{
			this.ExtractMrz(32, "100_pass-polx.jpg");
		}

		[TestMethod]
		public void Test_33_100_pass_uto()
		{
			this.ExtractMrz(33, "100_pass-uto.jpg");
		}

		[TestMethod]
		public void Test_34_100_visa_polx()
		{
			this.ExtractMrz(34, "100_visa-polx.jpg");
		}

		[TestMethod]
		public void Test_35_mrz_original()
		{
			this.ExtractMrz(34, "mrz_original.jpg");
		}
	}
}
