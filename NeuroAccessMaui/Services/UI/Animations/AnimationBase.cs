namespace NeuroAccessMaui.Services.UI.Animations;

public abstract class AnimationBase : BindableObject
{
	private CancellationTokenSource? animateTimerCancellationTokenSource;

	public static readonly BindableProperty TargetProperty =
		BindableProperty.Create(nameof(Target), typeof(VisualElement), typeof(AnimationBase), null,
			BindingMode.TwoWay, null);

	public VisualElement Target
	{
		get { return (VisualElement)this.GetValue(TargetProperty); }
		set { this.SetValue(TargetProperty, value); }
	}

	public static readonly BindableProperty DurationProperty =
		BindableProperty.Create(nameof(Duration), typeof(string), typeof(AnimationBase), "1000",
			BindingMode.TwoWay, null);

	public string Duration
	{
		get { return (string)this.GetValue(DurationProperty); }
		set { this.SetValue(DurationProperty, value); }
	}

	public static readonly BindableProperty EasingProperty =
		BindableProperty.Create(nameof(Easing), typeof(EasingType), typeof(AnimationBase), EasingType.Linear,
			BindingMode.TwoWay, null);

	public EasingType Easing
	{
		get { return (EasingType)this.GetValue(EasingProperty); }
		set { this.SetValue(EasingProperty, value); }
	}

	public static readonly BindableProperty DelayProperty =
	  BindableProperty.Create(nameof(Delay), typeof(int), typeof(AnimationBase), 0, propertyChanged: (bindable, oldValue, newValue) =>
		  ((AnimationBase)bindable).Delay = (int)newValue);

	public int Delay
	{
		get { return (int)this.GetValue(DelayProperty); }
		set { this.SetValue(DelayProperty, value); }
	}

	public static readonly BindableProperty RepeatForeverProperty =
	  BindableProperty.Create(nameof(RepeatForever), typeof(bool), typeof(AnimationBase), false, propertyChanged: (bindable, oldValue, newValue) =>
		  ((AnimationBase)bindable).RepeatForever = (bool)newValue);

	public bool RepeatForever
	{
		get { return (bool)this.GetValue(RepeatForeverProperty); }
		set { this.SetValue(RepeatForeverProperty, value); }
	}

	protected abstract Task BeginAnimation();

	public async Task Begin()
	{
		if (this.Delay > 0)
		{
			await Task.Delay(this.Delay);
		}

		if (!this.RepeatForever)
		{
			await this.BeginAnimation();
		}
		else
		{
			this.RepeatAnimation(new CancellationTokenSource());
		}
	}

	public void End()
	{
		Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(this.Target);

		this.animateTimerCancellationTokenSource?.Cancel();
	}

	internal void RepeatAnimation(CancellationTokenSource tokenSource)
	{
		this.animateTimerCancellationTokenSource = tokenSource;

		this.Dispatcher.Dispatch(async () =>
		{
			if (!this.animateTimerCancellationTokenSource.IsCancellationRequested)
			{
				await this.BeginAnimation();

				this.RepeatAnimation(this.animateTimerCancellationTokenSource);
			}
		});
	}
}
