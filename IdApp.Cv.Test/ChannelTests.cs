using IdApp.Cv.Channels;

namespace IdApp.Cv.Test
{
	[TestClass]
	public class ChannelTests
	{
		[TestMethod]
		public void Test_01_Red()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\Red.png", 600, 600);
			Assert.AreEqual(typeof(uint), M.ElementType);
			Assert.AreEqual(100, M.Width);
			Assert.AreEqual(100, M.Height);

			Matrix<uint>? M2 = M as Matrix<uint>;
			Assert.IsNotNull(M2);

			foreach (uint i in M2.Data)
				Assert.AreEqual(0xff0000ff, i);

			Matrix<byte> M3 = M2.RedChannel();

			foreach (byte b in M3.Data)
				Assert.AreEqual(0xff, b);
		}

		[TestMethod]
		public void Test_02_Green()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\Green.png", 600, 600);
			Assert.AreEqual(typeof(uint), M.ElementType);
			Assert.AreEqual(100, M.Width);
			Assert.AreEqual(100, M.Height);

			Matrix<uint>? M2 = M as Matrix<uint>;
			Assert.IsNotNull(M2);

			foreach (uint i in M2.Data)
				Assert.AreEqual(0xff00ff00, i);

			Matrix<byte> M3 = M2.GreenChannel();

			foreach (byte b in M3.Data)
				Assert.AreEqual(0xff, b);
		}

		[TestMethod]
		public void Test_03_Blue()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\Blue.png", 600, 600);
			Assert.AreEqual(typeof(uint), M.ElementType);
			Assert.AreEqual(100, M.Width);
			Assert.AreEqual(100, M.Height);

			Matrix<uint>? M2 = M as Matrix<uint>;
			Assert.IsNotNull(M2);

			foreach (uint i in M2.Data)
				Assert.AreEqual(0xffff0000, i);

			Matrix<byte> M3 = M2.BlueChannel();

			foreach (byte b in M3.Data)
				Assert.AreEqual(0xff, b);
		}

		[TestMethod]
		public void Test_04_Semitransparent()
		{
			IMatrix M = Bitmaps.FromBitmapFile("TestData\\Semitransparent.png", 600, 600);
			Assert.AreEqual(typeof(uint), M.ElementType);
			Assert.AreEqual(100, M.Width);
			Assert.AreEqual(100, M.Height);

			Matrix<uint>? M2 = M as Matrix<uint>;
			Assert.IsNotNull(M2);

			foreach (uint i in M2.Data)
				Assert.AreEqual(0x80008000, i);     // Why not 0x8000ff00 ?

			Matrix<byte> M3 = M2.AlphaChannel();

			foreach (byte b in M3.Data)
				Assert.AreEqual(0x80, b);
		}
	}
}
