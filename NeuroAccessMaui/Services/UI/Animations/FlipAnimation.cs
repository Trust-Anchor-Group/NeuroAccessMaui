using System.Globalization;

namespace NeuroAccessMaui.Services.UI.Animations;

public class FlipAnimation : AnimationBase
{
	public enum FlipDirection
	{
		Left,
		Right
	}

	public static readonly BindableProperty DirectionProperty =
	  BindableProperty.Create(nameof(Direction), typeof(FlipDirection), typeof(FlipAnimation), FlipDirection.Right,
		  BindingMode.TwoWay, null);

	public FlipDirection Direction
	{
		get { return (FlipDirection)this.GetValue(DirectionProperty); }
		set { this.SetValue(DirectionProperty, value); }
	}

	protected override Task BeginAnimation()
	{
		if (this.Target is null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		return Task.Run(() =>
		{
			this.Dispatcher.Dispatch(() =>
			{
				this.Target.Animate("Flip", this.Flip(), 16,
					Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture));
			});
		});
	}

	internal Animation Flip()
	{
		Animation Animation = new();

		Animation.WithConcurrent((f) => this.Target.Opacity = f, 0.5, 1);
		Animation.WithConcurrent((f) => this.Target.RotationY = f, (this.Direction == FlipDirection.Left) ? 90 : -90, 0, Microsoft.Maui.Easing.Linear);

		return Animation;
	}
}
