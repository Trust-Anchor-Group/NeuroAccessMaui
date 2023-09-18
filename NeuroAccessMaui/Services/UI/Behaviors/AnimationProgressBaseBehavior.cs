using NeuroAccessMaui.Services.UI.Animations;

namespace NeuroAccessMaui.Services.UI.Behaviors;

public abstract class AnimationProgressBaseBehavior : Behavior<VisualElement>
{
	public static readonly BindableProperty ProgressProperty =
		   BindableProperty.Create(nameof(Progress), typeof(double?), typeof(AnimationProgressBaseBehavior), default(double),
			   BindingMode.TwoWay, null, OnChanged);

	public double? Progress
	{
		get { return (double?)this.GetValue(ProgressProperty); }
		set { this.SetValue(ProgressProperty, value); }
	}

	public static readonly BindableProperty MinimumProperty =
		BindableProperty.Create(nameof(Minimum), typeof(double), typeof(AnimationProgressBaseBehavior), 0.0d,
			BindingMode.TwoWay, null);

	public double Minimum
	{
		get { return (double)this.GetValue(MinimumProperty); }
		set { this.SetValue(MinimumProperty, value); }
	}

	public static readonly BindableProperty MaximumProperty =
	   BindableProperty.Create(nameof(Maximum), typeof(double), typeof(AnimationProgressBaseBehavior), 100.0d,
		   BindingMode.TwoWay, null);

	public double Maximum
	{
		get { return (double)this.GetValue(MaximumProperty); }
		set { this.SetValue(MaximumProperty, value); }
	}

	public static readonly BindableProperty EasingProperty =
	   BindableProperty.Create(nameof(Easing), typeof(EasingType), typeof(AnimationProgressBaseBehavior), EasingType.Linear,
		   BindingMode.TwoWay, null);

	public EasingType Easing
	{
		get { return (EasingType)this.GetValue(EasingProperty); }
		set { this.SetValue(EasingProperty, value); }
	}

	public static readonly BindableProperty TargetPropertyProperty =
	  BindableProperty.Create(nameof(TargetProperty), typeof(BindableProperty), typeof(AnimationProgressBaseBehavior), null,
		  BindingMode.TwoWay, null);

	public BindableProperty TargetProperty
	{
		get { return (BindableProperty)this.GetValue(TargetPropertyProperty); }
		set { this.SetValue(TargetPropertyProperty, value); }
	}

	public VisualElement? Target
	{
		get;
		private set;
	}

	protected override void OnAttachedTo(VisualElement Bindable)
	{
		this.Target = Bindable;

		this.Update();
		base.OnAttachedTo(Bindable);
	}

	protected static void OnChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((AnimationProgressBaseBehavior)Bindable).Update();
	}

	protected override void OnDetachingFrom(VisualElement Bindable)
	{
		base.OnDetachingFrom(Bindable);
		this.Target = null;
	}

	protected abstract void OnUpdate();

	protected void Update()
	{
		if ((this.Target is not null) && this.Progress.HasValue)
		{
			this.OnUpdate();
		}
	}
}
