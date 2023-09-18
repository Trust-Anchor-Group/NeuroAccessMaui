using NeuroAccessMaui.Services.UI.Animations;

namespace NeuroAccessMaui.Services.UI.Behaviors;

public class EndAnimationBehavior : Behavior<VisualElement>
{
	private static VisualElement? associatedObject;

	protected override void OnAttachedTo(VisualElement Bindable)
	{
		base.OnAttachedTo(Bindable);
		associatedObject = Bindable;

		if (this.Animation is not null)
		{
			if (this.Animation.Target is null)
			{
				this.Animation.Target = associatedObject;
			}

			this.Animation.End();
		}
	}

	protected override void OnDetachingFrom(VisualElement Bindable)
	{
		associatedObject = null;
		base.OnDetachingFrom(Bindable);
	}

	public static readonly BindableProperty AnimationProperty =
	  BindableProperty.Create(nameof(Animation), typeof(AnimationBase), typeof(BeginAnimationBehavior), null,
		  BindingMode.TwoWay, null);

	public AnimationBase Animation
	{
		get { return (AnimationBase)this.GetValue(AnimationProperty); }
		set { this.SetValue(AnimationProperty, value); }
	}
}
