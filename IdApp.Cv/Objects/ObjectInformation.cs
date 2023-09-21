using IdApp.Cv.Basic;

namespace IdApp.Cv.Objects
{
	/// <summary>
	/// Contains information about an object.
	/// </summary>
	public class ObjectInformation
	{
		private readonly ObjectMap map;
		private readonly ushort nr;
		private readonly int x0;
		private readonly int y0;
		private int nrPixels = 0;
		private Point[] contour = null;
		private int minX;
		private int maxX;
		private int minY;
		private int maxY;

		/// <summary>
		/// Contains information about an object.
		/// </summary>
		/// <param name="Nr">Object Number.</param>
		/// <param name="X0">X-Coordinate where object was first detected.</param>
		/// <param name="Y0">Y-Coordinate where object was first detected.</param>
		/// <param name="Map">Object Map</param>
		public ObjectInformation(ushort Nr, int X0, int Y0, ObjectMap Map)
		{
			this.nr = Nr;
			this.x0 = this.minX = this.maxX = X0;
			this.y0 = this.minY = this.maxY = Y0;
			this.map = Map;
		}

		internal void AddPixels(int x1, int x2, int y)
		{
			this.nrPixels += x2 - x1 + 1;

			if (x1 < this.minX)
				this.minX = x1;

			if (x2 > this.maxX)
				this.maxX = x2;

			if (y < this.minY)
				this.minY = y;
			else if (y > this.maxY)
				this.maxY = y;
		}

		/// <summary>
		/// Object number in image
		/// </summary>
		public ushort Nr => this.nr;

		/// <summary>
		/// Number of pixels in object.
		/// </summary>
		public int NrPixels => this.nrPixels;

		/// <summary>
		/// Smallest X-coordinate of bounding box
		/// </summary>
		public int MinX => this.minX;

		/// <summary>
		/// Largest X-coordinate of bounding box
		/// </summary>
		public int MaxX => this.maxX;

		/// <summary>
		/// Smallest Y-coordinate of bounding box
		/// </summary>
		public int MinY => this.minY;

		/// <summary>
		/// Largest Y-coordinate of bounding box
		/// </summary>
		public int MaxY => this.maxY;

		/// <summary>
		/// X-Coordinate where object was first detected.
		/// </summary>
		public int X0 => this.x0;

		/// <summary>
		/// Y-Coordinate where object was first detected.
		/// </summary>
		public int Y0 => this.y0;

		/// <summary>
		/// Width of object.
		/// </summary>
		public int Width => this.maxX - this.minX + 1;

		/// <summary>
		/// Height of object.
		/// </summary>
		public int Height => this.maxY - this.minY + 1;

		/// <summary>
		/// Object Map containing information about the objects in an image.
		/// </summary>
		public ObjectMap Map => this.map;

		/// <summary>
		/// Contour of the object.
		/// </summary>
		public Point[] Contour
		{
			get
			{
				if (this.contour is null)
					this.contour = this.map.FindContour(this.x0, this.y0, this.nr);

				return this.contour;
			}
		}
	}
}
