namespace NeuroAccessMaui.Services.UI.Behaviors;

public class ScrollViewScrollBehavior : Behavior<ScrollView>
{
	public static readonly BindableProperty ScrollXProperty =
		BindableProperty.Create(nameof(ScrollX), typeof(double), typeof(ScrollViewScrollBehavior), default(double),
			BindingMode.TwoWay, null);

	/// <summary>
	/// The horizontal scroll value in pixels.
	/// </summary>
	public double ScrollX
	{
		get { return (double)this.GetValue(ScrollXProperty); }
		set { this.SetValue(ScrollXProperty, value); }
	}

	public static readonly BindableProperty ScrollYProperty =
		BindableProperty.Create(nameof(ScrollY), typeof(double), typeof(ScrollViewScrollBehavior), default(double),
			BindingMode.TwoWay, null);

	/// <summary>
	/// The vertical scroll value in pixels.
	/// </summary>
	public double ScrollY
	{
		get { return (double)this.GetValue(ScrollYProperty); }
		set { this.SetValue(ScrollYProperty, value); }
	}

	public static readonly BindableProperty RelativeScrollXProperty =
	   BindableProperty.Create(nameof(RelativeScrollX), typeof(double), typeof(ScrollViewScrollBehavior), default(double),
		   BindingMode.TwoWay, null);

	/// <summary>
	/// The horizontal scroll value between 0 and 1.
	/// </summary>
	public double RelativeScrollX
	{
		get { return (double)this.GetValue(RelativeScrollXProperty); }
		set { this.SetValue(RelativeScrollXProperty, value); }
	}

	public static readonly BindableProperty RelativeScrollYProperty =
		BindableProperty.Create(nameof(RelativeScrollY), typeof(double), typeof(ScrollViewScrollBehavior), default(double),
			BindingMode.TwoWay, null);

	/// <summary>
	/// The vertical scroll value between 0 and 1.
	/// </summary>
	public double RelativeScrollY
	{
		get { return (double)this.GetValue(RelativeScrollYProperty); }
		set { this.SetValue(RelativeScrollYProperty, value); }
	}

	public static readonly BindableProperty PercentageScrollXProperty =
		BindableProperty.Create(nameof(PercentageScrollX), typeof(double), typeof(ScrollViewScrollBehavior), default(double),
			BindingMode.TwoWay, null);

	/// <summary>
	/// The horizontal scroll value between 0% and 100%.
	/// </summary>
	public double PercentageScrollX
	{
		get { return (double)this.GetValue(PercentageScrollXProperty); }
		set { this.SetValue(PercentageScrollXProperty, value); }
	}

	public static readonly BindableProperty PercentageScrollYProperty =
		BindableProperty.Create(nameof(PercentageScrollY), typeof(double), typeof(ScrollViewScrollBehavior), default(double),
		BindingMode.TwoWay, null);

	/// <summary>
	/// The vertical scroll value between 0% and 100%.
	/// </summary>
	public double PercentageScrollY
	{
		get { return (double)this.GetValue(PercentageScrollYProperty); }
		set { this.SetValue(PercentageScrollYProperty, value); }
	}

	protected override void OnAttachedTo(ScrollView Bindable)
	{
		base.OnAttachedTo(Bindable);
		Bindable.Scrolled += new EventHandler<ScrolledEventArgs>(this.OnScrolled);
	}

	protected override void OnDetachingFrom(ScrollView Bindable)
	{
		base.OnDetachingFrom(Bindable);

		Bindable.Scrolled -= new EventHandler<ScrolledEventArgs>(this.OnScrolled);
	}

	private void OnScrolled(object? Sender, ScrolledEventArgs e)
	{
		if (Sender is ScrollView ScrollView)
		{
			Size ContentSize = ScrollView.ContentSize;

			double ViewportHeight = ContentSize.Height - ScrollView.Height;
			double ViewportWidth = ContentSize.Width - ScrollView.Width;

			this.ScrollY = e.ScrollY;
			this.ScrollX = e.ScrollX;

			this.RelativeScrollY = ViewportHeight <= 0 ? 0 : e.ScrollY / ViewportHeight;
			this.RelativeScrollX = ViewportWidth <= 0 ? 0 : e.ScrollX / ViewportWidth;

			this.PercentageScrollX = this.RelativeScrollX * 100;
			this.PercentageScrollY = this.RelativeScrollY * 100;
		}
	}
}
