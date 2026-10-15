namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// The FadeView is a user control that holds two child controls: a <see cref="FrontView"/> and a <see cref="BackView"/>.
	/// It can perform a fade animation from one view to another. In order to fade the view, simply set the <see cref="IsFrontView"/> property.
	/// </summary>
	public class FadeView : ContentView
	{
		private const uint durationInMs = 250 / 2;
		private readonly Easing easingType = Easing.Linear;

		private readonly Grid contentHolder;
		private View? frontView;
		private View? backView;
		private bool isFading;

		/// <summary>
		/// Creates an instance of the <see cref="FadeView"/> control.
		/// </summary>
		public FadeView()
		{
			this.contentHolder = new Grid { BackgroundColor = Colors.Transparent };
			this.Content = this.contentHolder;
			this.BackgroundColor = Colors.Transparent;
		}

		/// <summary>
		///
		/// </summary>
		public static readonly BindableProperty IsFrontViewProperty =
			BindableProperty.Create(nameof(IsFrontView), typeof(bool), typeof(FadeView), true, propertyChanged: (b, oldValue, newValue) =>
			{
				FadeView fv = (FadeView)b;

				if (!fv.isFading)
				{
					if (fv.IsFrontView)
						fv.FadeFromBackToFront();
					else
						fv.FadeFromFrontToBack();
				}
			});


		/// <summary>
		/// Set to true is the front vew must be shown.
		/// </summary>
		public bool IsFrontView
		{
			get => (bool)this.GetValue(IsFrontViewProperty);
			set => this.SetValue(IsFrontViewProperty, value);
		}

		/// <summary>
		///
		/// </summary>
		public static readonly BindableProperty FrontViewProperty =
			BindableProperty.Create(nameof(FrontView), typeof(View), typeof(FadeView), default(View), propertyChanged: (b, oldValue, newValue) =>
			{
				FadeView fv = (FadeView)b;
				if (newValue is not null)
				{
					if (fv.frontView is not null)
					{
						fv.contentHolder.Children.Remove(fv.frontView);
					}
					fv.frontView = (View)newValue;

					fv.AddChild(fv.frontView);
				}
				else
				{
					if (fv.frontView is not null)
					{
						fv.contentHolder.Children.Remove(fv.frontView);
					}

					fv.frontView = null;
				}

				fv.OrganizeViews();
			});

		/// <summary>
		/// The view displayed on the 'front' side of this user control.
		/// </summary>
		public View? FrontView
		{
			get => (View?)this.GetValue(FrontViewProperty);
			set => this.SetValue(FrontViewProperty, value);
		}

		/// <summary>
		///
		/// </summary>
		public static readonly BindableProperty BackViewProperty =
			BindableProperty.Create(nameof(BackView), typeof(View), typeof(FadeView), default(View), propertyChanged: (b, oldValue, newValue) =>
			{
				FadeView fv = (FadeView)b;
				if (newValue is not null)
				{
					if (fv.backView is not null)
					{
						fv.contentHolder.Children.Remove(fv.backView);
					}

					fv.backView = (View)newValue;
					fv.AddChild(fv.backView);
				}
				else
				{
					if (fv.backView is not null)
					{
						fv.contentHolder.Children.Remove(fv.backView);
					}

					fv.backView = null;
				}

				fv.OrganizeViews();
			});

		/// <summary>
		/// The view displayed on the 'back' side of this user control.
		/// </summary>
		public View? BackView
		{
			get => (View?)this.GetValue(BackViewProperty);
			set => this.SetValue(BackViewProperty, value);
		}

		private void AddChild(View childView)
		{
			this.contentHolder.Children.Add(childView);
		}

		private void OrganizeViews()
		{
			if (this.backView is not null && this.frontView is not null)
			{
				//this.contentHolder.RaiseChild(this.IsFrontView ? this.frontView : this.backView);
			}
		}

		private async void FadeFromFrontToBack()
		{
			this.isFading = true;

			await this.FadeFrontToBack1();

			// Change the visible content
			this.OrganizeViews();

			await this.FadeFrontToBack2();

			this.isFading = false;
		}

		private async void FadeFromBackToFront()
		{
			this.isFading = true;

			await this.FadeBackToFront1();

			// Change the visible content
			this.OrganizeViews();

			await this.FadeBackToFront2();

			this.isFading = false;
		}

		#region Animation

		private async Task FadeFrontToBack1()
		{
			this.FrontView!.CancelAnimations();
			this.BackView!.CancelAnimations();

			this.FrontView!.Opacity = 1;
			this.BackView!.Opacity = 0;

			List<Task> Tasks = 
			[
				Task.Run(async () => await this.FrontView.FadeToAsync(0.5, durationInMs, this.easingType)),
				Task.Run(async () => await this.BackView.FadeToAsync(0.5, durationInMs, this.easingType))
			];

			await Task.WhenAll(Tasks);
		}

		private async Task FadeFrontToBack2()
		{
			this.FrontView!.CancelAnimations();
			this.BackView!.CancelAnimations();

			this.FrontView!.Opacity = 0.5;
			this.BackView!.Opacity = 0.5;

			List<Task> Tasks = 
			[
				Task.Run(async () => await this.FrontView.FadeToAsync(0, durationInMs, this.easingType)),
				Task.Run(async () => await this.BackView.FadeToAsync(1, durationInMs, this.easingType))
			];

			await Task.WhenAll(Tasks);
		}


		private async Task FadeBackToFront1()
		{
			this.FrontView!.CancelAnimations();
			this.BackView!.CancelAnimations();

			this.FrontView!.Opacity = 0;
			this.BackView!.Opacity = 1;

			List<Task> Tasks = 
			[
				Task.Run(async () => await this.FrontView.FadeToAsync(0.5, durationInMs, this.easingType)),
				Task.Run(async () => await this.BackView.FadeToAsync(0.5, durationInMs, this.easingType))
			];

			await Task.WhenAll(Tasks);
		}

		private async Task FadeBackToFront2()
		{
			this.FrontView!.CancelAnimations();
			this.BackView!.CancelAnimations();

			this.FrontView!.Opacity = 0.5;
			this.BackView!.Opacity = 0.5;

			List<Task> Tasks =
			[
				Task.Run(async () => await this.FrontView.FadeToAsync(1, durationInMs, this.easingType)),
				Task.Run(async () => await this.BackView.FadeToAsync(0, durationInMs, this.easingType))
			];

			await Task.WhenAll(Tasks);
		}

		#endregion
	}
}
