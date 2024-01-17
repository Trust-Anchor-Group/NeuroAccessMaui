namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Data Template Selector, based on Service information.
	/// </summary>
	public class CarouselTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// First page
		/// </summary>
		public DataTemplate? Page1 { get; set; }

		/// <summary>
		/// Second page
		/// </summary>
		public DataTemplate? Page2 { get; set; }

		/// <summary>
		/// Third page
		/// </summary>
		public DataTemplate? Page3 { get; set; }

		/// <summary>
		/// Fourth page
		/// </summary>
		public DataTemplate? Page4 { get; set; }

		/// <summary>
		/// Fifth page
		/// </summary>
		public DataTemplate? Page5 { get; set; }

		/// <summary>
		/// Sixth page
		/// </summary>
		public DataTemplate? Page6 { get; set; }

		/// <summary>
		/// Seventh page
		/// </summary>
		public DataTemplate? Page7 { get; set; }

		/// <summary>
		/// Eigth page
		/// </summary>
		public DataTemplate? Page8 { get; set; }

		/// <summary>
		/// Ninth page
		/// </summary>
		public DataTemplate? Page9 { get; set; }

		/// <inheritdoc/>
		protected override DataTemplate? OnSelectTemplate(object item, BindableObject Container)
		{
			CarouselView? View = Container as CarouselView;
			int Position = View?.Position ?? 0;

			switch (Position)
			{
				case 0:return this.Page1;
				case 1:return this.Page2;
				case 2:return this.Page3;
				case 3:return this.Page4;
				case 4:return this.Page5;
				case 5:return this.Page6;
				case 6:return this.Page7;
				case 7:return this.Page8;
				case 8:return this.Page9;
				default:return null;
			}
		}
	}
}
