using System.Globalization;
using NeuroAccessMaui.Services.UI.Helpers;

namespace NeuroAccessMaui.Services.UI.Behaviors;

public class EntranceTransition : Behavior<VisualElement>
{
	const int delay = 100;
	const int translateFrom = 6;

	private VisualElement? associatedObject;
	private IEnumerable<VisualElement>? children;

	public static readonly BindableProperty DurationProperty =
		BindableProperty.Create(nameof(Duration), typeof(string), typeof(EntranceTransition), "500",
			BindingMode.TwoWay, null);

	public string Duration
	{
		get { return (string)this.GetValue(DurationProperty); }
		set { this.SetValue(DurationProperty, value); }
	}

	protected override void OnAttachedTo(VisualElement Bindable)
	{
		base.OnAttachedTo(Bindable);

		this.associatedObject = Bindable;
		this.children = VisualTreeHelper.GetChildren<VisualElement>(this.associatedObject);
		this.associatedObject.PropertyChanged += this.OnPropertyChanged;
	}

	protected override void OnDetachingFrom(VisualElement bindable)
	{
		this.StopAnimation();

		if (this.associatedObject is not null)
		{
			this.associatedObject.PropertyChanged -= this.OnPropertyChanged;
		}

		this.associatedObject = null;
		this.children = null;

		base.OnDetachingFrom(bindable);
	}

	async void OnPropertyChanged(object? Sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "Renderer")
		{
			await this.StartAnimationAsync();
		}
	}

	void SetInitialTransitionState()
	{
		if ((this.children is not null) && this.children.Any())
		{
			foreach (VisualElement child in this.children)
			{
				this.SetInitialTransitionState(child);
			}
		}
		else
		{
			this.SetInitialTransitionState(this.associatedObject);
		}
	}

	void SetInitialTransitionState(VisualElement? Element)
	{
		if ((Element is not null) && (this.associatedObject is not null))
		{
			Element.Opacity = 0;
			Element.TranslationX = this.associatedObject.TranslationX + translateFrom;
			Element.TranslationY = this.associatedObject.TranslationY + translateFrom;
		}
	}

	void StopAnimation()
	{
		if ((this.children is not null) && this.children.Any())
		{
			foreach (VisualElement child in this.children)
			{
				Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(child);
			}
		}
		else
		{
			if (this.associatedObject is not null)
			{
				Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(this.associatedObject);
			}
		}
	}

	async Task StartAnimationAsync()
	{
		this.StopAnimation();
		this.SetInitialTransitionState();

		if ((this.children is not null) && this.children.Any())
		{
			foreach (VisualElement child in this.children)
			{
				await this.AnimateItemAsync(child);
			}
		}
		else
		{
			if (!this.HasParentEntranceTransition(this.associatedObject))
			{
				await this.AnimateItemAsync(this.associatedObject);
			}
		}
	}

	async Task AnimateItemAsync(VisualElement? Element)
	{
		if (Element is not null)
		{
			await Task.Delay(delay);

			Animation ParentAnimation = new();

			Animation TranslateXAnimation = new(progress => Element.TranslationX = progress, Element.TranslationX, 0, Easing.SpringIn);
			Animation TranslateYAnimation = new(progress => Element.TranslationY = progress, Element.TranslationY, 0, Easing.SpringIn);
			Animation OpacityAnimation = new(progress => Element.Opacity = progress, 0, 1, Easing.CubicIn);

			ParentAnimation.Add(0, 0.75, TranslateXAnimation);
			ParentAnimation.Add(0, 0.75, TranslateYAnimation);
			ParentAnimation.Add(0, 1, OpacityAnimation);

			ParentAnimation.Commit(this.associatedObject, "EntranceTransition" + Element.Id, 16,
				Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture), null, (v, c) => this.StopAnimation());
		}
	}

	bool HasParentEntranceTransition(VisualElement? Element)
	{
		if (Element is not null)
		{
			VisualElement? Parent = VisualTreeHelper.GetParent<VisualElement>(Element);

			if (Parent is not null)
			{
				IList<Behavior> behaviors = Parent.Behaviors;
				return behaviors.OfType<EntranceTransition>().FirstOrDefault() is not null;
			}
		}

		return false;
	}
}
