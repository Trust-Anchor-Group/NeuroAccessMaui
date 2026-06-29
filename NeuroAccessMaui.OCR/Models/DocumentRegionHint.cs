namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents an expected document region using normalized coordinates relative to the source image.
	/// </summary>
	public sealed class DocumentRegionHint
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentRegionHint"/> class.
		/// </summary>
		/// <param name="Left">The normalized left coordinate.</param>
		/// <param name="Top">The normalized top coordinate.</param>
		/// <param name="Width">The normalized width.</param>
		/// <param name="Height">The normalized height.</param>
		public DocumentRegionHint(float Left, float Top, float Width, float Height)
		{
			if (!float.IsFinite(Left) || Left < 0f || Left >= 1f)
				throw new ArgumentOutOfRangeException(nameof(Left));
			if (!float.IsFinite(Top) || Top < 0f || Top >= 1f)
				throw new ArgumentOutOfRangeException(nameof(Top));
			if (!float.IsFinite(Width) || Width <= 0f || Left + Width > 1f)
				throw new ArgumentOutOfRangeException(nameof(Width));
			if (!float.IsFinite(Height) || Height <= 0f || Top + Height > 1f)
				throw new ArgumentOutOfRangeException(nameof(Height));

			this.Left = Left;
			this.Top = Top;
			this.Width = Width;
			this.Height = Height;
		}

		/// <summary>
		/// Gets the normalized left coordinate.
		/// </summary>
		public float Left
		{
			get;
		}

		/// <summary>
		/// Gets the normalized top coordinate.
		/// </summary>
		public float Top
		{
			get;
		}

		/// <summary>
		/// Gets the normalized width.
		/// </summary>
		public float Width
		{
			get;
		}

		/// <summary>
		/// Gets the normalized height.
		/// </summary>
		public float Height
		{
			get;
		}
	}
}
