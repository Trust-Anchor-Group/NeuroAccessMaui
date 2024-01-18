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

			return Position switch
			{
				0 => this.Page1,
				1 => this.Page2,
				2 => this.Page3,
				3 => this.Page4,
				4 => this.Page5,
				5 => this.Page6,
				6 => this.Page7,
				7 => this.Page8,
				8 => this.Page9,
				_ => null,
			};
		}
	}
}
