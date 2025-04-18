using Microsoft.Maui.Controls.Shapes;


namespace NeuroAccessMaui.UI.Controls
{
	public class BottomSheetView : Grid
	{
		// Constant fallback header height in case the header hasn't been measured yet.
		private const double defaultHeaderHeight = 50;
		private const uint animationDuration = 200;
		private const double flickVelocityThreshold = 1;

		// Layout elements.
		private readonly Border cardBorder;
		private readonly ContentView headerContainer;
		private readonly ContentView contentPresenter;
		private readonly RowDefinition contentRow;

		// Fields for layout calculations.
		private double maxContentHeight;

		// Bindable property for the header background color (still available if needed).
		public static readonly BindableProperty HeaderBackgroundColorProperty =
			BindableProperty.Create(nameof(HeaderBackgroundColor), typeof(Color), typeof(BottomSheetView), Colors.LightGray);

		public Color HeaderBackgroundColor
		{
			get => (Color)this.GetValue(HeaderBackgroundColorProperty);
			set => this.SetValue(HeaderBackgroundColorProperty, value);
		}

		// Bindable property for controlling maximum expanded height.
		public static readonly BindableProperty MaxExpandedHeightProperty =
			BindableProperty.Create(nameof(MaxExpandedHeight), typeof(double), typeof(BottomSheetView), -1.0);

		/// <summary>
		/// If set (> 0), this value determines the maximum overall height (header + content).
		/// If left at or below 0, the control uses the available height.
		/// </summary>
		public double MaxExpandedHeight
		{
			get => (double)this.GetValue(MaxExpandedHeightProperty);
			set => this.SetValue(MaxExpandedHeightProperty, value);
		}

		// Bindable property for the header content.
		public static readonly BindableProperty HeaderContentProperty =
			BindableProperty.Create(nameof(HeaderContent), typeof(View), typeof(BottomSheetView), default(View));

		/// <summary>
		/// Gets or sets the header content of the bottom sheet.
		/// </summary>
		public View HeaderContent
		{
			get => (View)this.GetValue(HeaderContentProperty);
			set => this.SetValue(HeaderContentProperty, value);
		}

		public BottomSheetView()
		{
			this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

			// Transparent background so underlying content can be seen if needed.
			this.BackgroundColor = Colors.Transparent;

			// Initialize the Border that holds the entire bottom sheet content

			this.cardBorder = new Border
			{
				Style = AppStyles.BorderSet,
				Margin = new Thickness(0, 0, 0, 0),
				Padding = new Thickness(0, 0, 0, 0),
				StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16, 16, 0, 0) },
				VerticalOptions = LayoutOptions.End
			};


			// Create a grid with two rows.
			// Row 0 for the header is now Auto sized.
			Grid SheetGrid = [];
			SheetGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			this.contentRow = new RowDefinition { Height = new GridLength(0, GridUnitType.Absolute) };
			SheetGrid.RowDefinitions.Add(this.contentRow);

			SheetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });


			// Header container: a ContentView whose Content is bound to HeaderContent.
			this.headerContainer = new ContentView();
			this.headerContainer.SetBinding(ContentView.ContentProperty, new Binding(nameof(this.HeaderContent), source: this));

			// Add pan gesture to the header container.
			PanGestureRecognizer PanGesture = new();
			PanGesture.PanUpdated += this.OnPanUpdated;
			this.headerContainer.GestureRecognizers.Add(PanGesture);

			// If no header is provided, use a default header with a grab handle.
			this.HeaderContent ??= this.CreateDefaultHeaderContent();

			// Create a placeholder for the main content.
			this.contentPresenter = new ContentView();

			// Build the view hierarchy: header (row 0) and main content (row 1).
			SheetGrid.Add(this.headerContainer, 0, 0);
			SheetGrid.Add(this.contentPresenter, 0, 1);
			this.cardBorder.Content = SheetGrid;

			this.Add(this.cardBorder);

			// Update available content height when the frame size changes.
			this.cardBorder.SizeChanged += this.OnFrameSizeChanged;
		}

		/// <summary>
		/// Gets or sets the main content of the bottom sheet.
		/// </summary>
		public View MainContent
		{
			get => this.contentPresenter.Content;
			set => this.contentPresenter.Content = value;
		}

		/// <summary>
		/// Creates a simple default header with a grab handle indicator.
		/// </summary>
		private Grid CreateDefaultHeaderContent()
		{
			BoxView Grabber = new()
			{
				WidthRequest = 40,
				HeightRequest = 4,
				CornerRadius = 2,
				Color = Colors.Gray,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};

			// Wrap the grabber in a Grid (or other layout) so that it can size appropriately.
			return new Grid
			{
				Padding = new Thickness(0, 16),
				Children = { Grabber }
			};
		}

		/// <summary>
		/// When the frame's size changes, calculate the maximum available height for the main content.
		/// </summary>
		private void OnFrameSizeChanged(object? sender, EventArgs e)
		{
			double AllowedHeight = this.MaxExpandedHeight > 0 ? this.MaxExpandedHeight : this.GetAllowedHeight();

			// Get header height using the header container's current height.
			double HeaderHeight = this.headerContainer.Height;
			if (HeaderHeight <= 0)
			{
				// If not yet measured, fallback to default header height.
				HeaderHeight = defaultHeaderHeight;
			}

			double EffectiveTotalHeight = AllowedHeight;
			this.maxContentHeight = Math.Max(0, EffectiveTotalHeight - HeaderHeight);
		}

		/// <summary>
		/// Finds the allowed height from the visual tree by walking up parent elements.
		/// </summary>
		private double GetAllowedHeight()
		{
			double AllowedHeight = 0;
			VisualElement CurrentElement = this;

			if (this.lastHeightConstraint > 0)
				return this.lastHeightConstraint;

			while (CurrentElement.Parent is VisualElement Parent)
			{
				CurrentElement = Parent;
				if (CurrentElement.Height > 0)
				{
					AllowedHeight = CurrentElement.Height;
					break;
				}
			}

			// Fallback to MainPage height if no valid parent height was found.
			Page? MainPage = Application.Current?.Windows.Count > 0 ? Application.Current.Windows[0].Page : null;
			if (AllowedHeight <= 0 && MainPage is not null)
			{
				AllowedHeight = MainPage.Height;
			}

			return AllowedHeight;
		}

		private double lastHeightConstraint;

		protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
		{
			this.lastHeightConstraint = heightConstraint;

			// Let the Grid do its normal measuring
			return base.MeasureOverride(widthConstraint, heightConstraint);
		}

		private double previousPanY = 0;
		private double accumulatedTranslation = 0;
		private DateTime panStartTime;

		private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
		{
			switch (e.StatusType)
			{
				case GestureStatus.Started:
					this.panStartTime = DateTime.UtcNow;
					this.accumulatedTranslation = 0;
					// Store the initial value (works for both platforms)
					this.previousPanY = e.TotalY;
					break;

				case GestureStatus.Running:
					double DeltaY = 0;
					if (DeviceInfo.Platform == DevicePlatform.iOS)
					{
						// iOS: TotalY is cumulative so we compute the delta.
						DeltaY = e.TotalY - this.previousPanY;
						this.previousPanY = e.TotalY;
					}
					else
					{
						// Android: TotalY is already incremental.
						DeltaY = e.TotalY;
					}

					this.accumulatedTranslation += DeltaY;
					this.UpdateSheetPosition(DeltaY);
					break;

				case GestureStatus.Completed:
				case GestureStatus.Canceled:
					double ElapsedMs = (DateTime.UtcNow - this.panStartTime).TotalMilliseconds;
					double Velocity = ElapsedMs > 0 ? this.accumulatedTranslation / ElapsedMs : 0;
					this.FinalizeSheetPosition(Velocity);
					break;
			}
		}


		/// <summary>
		/// Updates the main content row height based on the incremental pan gesture translation.
		/// Dragging upward (negative deltaY) will increase the height.
		/// </summary>
		private void UpdateSheetPosition(double deltaY)
		{
			double NewHeight = this.contentRow.Height.Value - deltaY;
			NewHeight = Math.Max(0, Math.Min(NewHeight, this.maxContentHeight));
			this.contentRow.Height = new GridLength(NewHeight, GridUnitType.Absolute);
		}

		/// <summary>
		/// When the gesture is completed, determine whether to fully expand or collapse the sheet.
		/// </summary>
		/// <param name="velocity">The average velocity of the pan gesture (pixels/ms).</param>
		private void FinalizeSheetPosition(double velocity)
		{
			if (Math.Abs(velocity) > flickVelocityThreshold)
			{
				if (velocity < 0)
					this.AnimateSheet(this.maxContentHeight);
				else
					this.AnimateSheet(0);
			}
			else
			{
				double CurrentHeight = this.contentRow.Height.Value;
				double MidPoint = this.maxContentHeight / 2;
				this.AnimateSheet(CurrentHeight >= MidPoint ? this.maxContentHeight : 0);
			}
		}

		/// <summary>
		/// Animates the main content row's height from its current value to the target value.
		/// </summary>
		private void AnimateSheet(double targetHeight)
		{
			double StartingHeight = this.contentRow.Height.Value;
			Animation Animation = new(v =>
			{
				this.contentRow.Height = new GridLength(v, GridUnitType.Absolute);
			}, StartingHeight, targetHeight);

			Animation.Commit(this, "SheetAnimation", 16, animationDuration, Easing.SinOut);
		}
	}
}
