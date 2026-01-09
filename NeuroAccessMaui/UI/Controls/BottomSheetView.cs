using CommunityToolkit.Maui.Core;
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

		// Fields for layout calculations.
		private double sheetHeight;
		private bool isExpanded = false;

		public bool IsExpanded => this.isExpanded;

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
		/// If set (&gt; 0), this value determines the maximum overall height (header + content).
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
				Style = AppStyles.BottomBarBorder,
				Margin = new Thickness(0, 0, 0, 0),
				Padding = new Thickness(0, 0, 0, 0),
				StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16, 16, 0, 0) },
				VerticalOptions = LayoutOptions.End
			};

			// Create a grid with two rows.
			// Row 0 for the header is now Auto sized.
			Grid SheetGrid = [];
			SheetGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			SheetGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

			SheetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

			// Header container: a ContentView whose Content is bound to HeaderContent.
			this.headerContainer = new ContentView();
			this.headerContainer.SetBinding(ContentView.ContentProperty, new Binding(nameof(this.HeaderContent), source: this));

			// Add pan gesture to the header container.
			PanGestureRecognizer PanGesture = new();
			PanGesture.PanUpdated += this.OnPanUpdated;
			this.headerContainer.GestureRecognizers.Add(PanGesture);

			// Add tap gesture to toggle open/closed
			TapGestureRecognizer TapGesture = new();
			TapGesture.Tapped += this.OnHeaderTapped;
			this.headerContainer.GestureRecognizers.Add(TapGesture);

			// If no header is provided, use a default header with a grab handle.
			this.HeaderContent ??= this.CreateDefaultHeaderContent();

			// Create a placeholder for the main content.
			this.contentPresenter = new ContentView();

			// Build the view hierarchy: header (row 0) and main content (row 1).
			SheetGrid.Add(this.headerContainer, 0, 0);
			SheetGrid.Add(this.contentPresenter, 0, 1);
			this.cardBorder.Content = SheetGrid;

			this.Add(this.cardBorder);

			// Update sheet height and set collapsed position when the frame size changes.
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
		/// When the frame's size changes, calculate the total height of the sheet and set its translation accordingly.
		/// </summary>
		private void OnFrameSizeChanged(object? sender, EventArgs e)
		{
			double AllowedHeight = this.MaxExpandedHeight > 0 ? this.MaxExpandedHeight : this.GetAllowedHeight();

			// If the allowed height is set, constrain the cardBorder height.
			if (AllowedHeight > 0)
			{
				this.cardBorder.HeightRequest = AllowedHeight;
				this.sheetHeight = AllowedHeight;
			}
			else
			{
				this.cardBorder.HeightRequest = -1;
				this.sheetHeight = this.cardBorder.Height;
			}

			// Move to correct initial position
			if (!this.isExpanded)
				this.SetTranslationToCollapsed();
			else
				this.SetTranslationToExpanded();
		}

		/// <summary>
		/// Finds the allowed height from the visual tree by walking up parent elements.
		/// </summary>
		private double GetAllowedHeight()
		{
			double AllowedHeight = 0;
			VisualElement CurrentElement = this;

			// Check if this controls height is constrained
			if (this.lastHeightConstraint > 0)
				return this.lastHeightConstraint;

			// Adopt to parents height
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

		/// <summary>
		/// Saves the last height constraint provided during measure pass. Used for calculating available height.
		/// </summary>
		protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
		{
			this.lastHeightConstraint = heightConstraint;

			// Let the Grid do its normal measuring
			return base.MeasureOverride(widthConstraint, heightConstraint);
		}

		private double initialPanY = 0;
		private double initialTranslationY = 0;
		private double previousPanY = 0;

		private DateTime panStartTime;

		/// <summary>
		/// Handles pan gestures by updating the sheet's TranslationY property, 
		/// which slides the sheet without triggering layout calculations.
		/// </summary>


		private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
		{
			switch (e.StatusType)
			{
				case GestureStatus.Started:
					this.panStartTime = DateTime.UtcNow;
					this.previousPanY = e.TotalY;
					this.initialTranslationY = this.cardBorder.TranslationY;
					break;

				case GestureStatus.Running:
					double DeltaY;

					if (DeviceInfo.Platform == DevicePlatform.iOS)
					{
						// iOS: TotalY is cumulative, so we compute delta from last event.
						DeltaY = e.TotalY - this.previousPanY;
						this.previousPanY = e.TotalY;
					}
					else
					{
						// Android: TotalY is incremental (already a delta).
						DeltaY = e.TotalY;
					}

					double TargetY = this.cardBorder.TranslationY + DeltaY;
					double HeaderHeight = this.headerContainer.Height > 0 ? this.headerContainer.Height : defaultHeaderHeight;
					double CollapsedY = this.sheetHeight - HeaderHeight;
					TargetY = Math.Max(0, Math.Min(TargetY, CollapsedY));
					this.cardBorder.TranslationY = TargetY;
					break;

				case GestureStatus.Completed:
				case GestureStatus.Canceled:
					double HeaderHeightEnd = this.headerContainer.Height > 0 ? this.headerContainer.Height : defaultHeaderHeight;
					double CollapsedYEnd = this.sheetHeight - HeaderHeightEnd;
					double MidPoint = CollapsedYEnd / 2;
					double CurrentY = this.cardBorder.TranslationY;

					if (CurrentY >= MidPoint)
					{
						this.AnimateToCollapsed();
						this.isExpanded = false;
					}
					else
					{
						this.AnimateToExpanded();
						this.isExpanded = true;
					}
					break;
			}
		}

		/// <summary>
		/// Handles tap on header to toggle expand/collapse. 
		/// Uses the translation-based logic.
		/// </summary>
		private void OnHeaderTapped(object? sender, EventArgs e)
		{
			// If already expanded, collapse; otherwise expand fully.
			this.FinalizeSheetPositionTranslation(this.isExpanded ? (flickVelocityThreshold + 1) : -(flickVelocityThreshold + 1));
		}

		/// <summary>
		/// Animates the TranslationY of the bottom sheet to the collapsed position.
		/// </summary>
		private void AnimateToCollapsed()
		{
			double HeaderHeight = this.headerContainer.Height > 0 ? this.headerContainer.Height : defaultHeaderHeight;
			double CollapsedY = this.sheetHeight - HeaderHeight;
			this.cardBorder.TranslateToAsync(0, CollapsedY, animationDuration, Easing.SinOut);
		}

		/// <summary>
		/// Animates the TranslationY of the bottom sheet to the expanded position (fully visible).
		/// </summary>
		private void AnimateToExpanded()
		{
			this.cardBorder.TranslateToAsync(0, 0, animationDuration, Easing.SinOut);
		}

		/// <summary>
		/// Sets the sheet position to collapsed state without animation.
		/// </summary>
		private void SetTranslationToCollapsed()
		{
			double HeaderHeight = this.headerContainer.Height > 0 ? this.headerContainer.Height : defaultHeaderHeight;
			double CollapsedY = this.sheetHeight - HeaderHeight;
			this.cardBorder.TranslationY = CollapsedY;
		}

		/// <summary>
		/// Sets the sheet position to expanded state without animation.
		/// </summary>
		private void SetTranslationToExpanded()
		{
			this.cardBorder.TranslationY = 0;
		}

		/// <summary>
		/// Toggles the expanded state of the current object.
		/// </summary>
		public void ToggleExpanded()
		{
			if (this.isExpanded)
			{
				this.AnimateToCollapsed();
				this.isExpanded = false;
			}
			else
			{
				this.AnimateToExpanded();
				this.isExpanded = true;
			}
		}

		/// <summary>
		/// When the gesture is completed, determine whether to fully expand or collapse the sheet using translation.
		/// </summary>
		/// <param name="velocity">The average velocity of the pan gesture (pixels/ms).</param>
		private void FinalizeSheetPositionTranslation(double velocity)
		{
			double HeaderHeight = this.headerContainer.Height > 0 ? this.headerContainer.Height : defaultHeaderHeight;
			double CollapsedY = this.sheetHeight - HeaderHeight;
			double MidPoint = CollapsedY / 2;
			double CurrentY = this.cardBorder.TranslationY;

			if (Math.Abs(velocity) > flickVelocityThreshold)
			{
				if (velocity < 0)
				{
					this.AnimateToExpanded();
					this.isExpanded = true;
				}
				else
				{
					this.AnimateToCollapsed();
					this.isExpanded = false;
				}
			}
			else
			{
				if (CurrentY >= MidPoint)
				{
					this.AnimateToCollapsed();
					this.isExpanded = false;
				}
				else
				{
					this.AnimateToExpanded();
					this.isExpanded = true;
				}
			}
		}
	}
}
