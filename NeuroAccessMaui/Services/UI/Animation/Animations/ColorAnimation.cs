using System.Globalization;
using NeuroAccessMaui.Services.UI.Extensions;

namespace NeuroAccessMaui.Services.UI.Animations;

public class ColorAnimation : AnimationBase
{
	public static readonly BindableProperty ToColorProperty =
		BindableProperty.Create(nameof(ToColor), typeof(Color), typeof(ColorAnimation), Colors.Transparent,
			BindingMode.TwoWay, null);

	public Color ToColor
	{
		get { return (Color)this.GetValue(ToColorProperty); }
		set { this.SetValue(ToColorProperty, value); }
	}

	protected override Task BeginAnimation()
	{
		if (this.Target is null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		Color FromColor = this.Target.BackgroundColor;

		return Task.Run(() =>
		{
			this.Dispatcher.Dispatch(async () =>
			{
				await this.Target.ColorTo(FromColor, this.ToColor, c => this.Target.BackgroundColor = c,
					Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture));
			});
		});
	}
}
