using NeuroAccessMaui.Services;
using SkiaSharp.Extended.UI.Controls;

namespace NeuroAccessMaui.UI.Pages.Identity.ViewIdentity
{
	/// <summary>
	/// A page to display when the user wants to view an identity.
	/// </summary>
	public partial class ViewIdentityPage
	{

		/// <summary>
		/// Creates a new instance of the <see cref="ViewIdentityPage"/> class.
		/// </summary>
		public ViewIdentityPage()
		{
			this.InitializeComponent();
			this.ContentPageModel = new ViewIdentityViewModel(ServiceRef.NavigationService.PopLatestArgs<ViewIdentityNavigationArgs>());
		}
		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height);
			try
			{
				// Set the size of the rainbow view
				this.RainbowView.WidthRequest = height + 200;
				this.RainbowView.HeightRequest = width + 200;

				// Prepare the confetti system:
				this.ConfettiView.Systems?.Clear();

				double BandHeight = 40;
				double BandWidth = 40;

				// Bottom‑left rectangle:
				Rect BottomLeftRect = new Rect(
					x: 0,
					y: height - BandHeight,
					width: BandWidth,
					height: BandHeight);

				// Bottom‑right rectangle:
				Rect BottomRightRect = new Rect(
					x: width - BandWidth,
					y: height - BandHeight,
					width: BandWidth,
					height: BandHeight);

				this.ConfettiView.Systems?.Add(this.CreateSideSystem(SKConfettiEmitterSide.Left, BottomLeftRect));
				this.ConfettiView.Systems?.Add(this.CreateSideSystem(SKConfettiEmitterSide.Right, BottomRightRect));
			}
			catch (Exception ex)
			{
				// Handle any exceptions that may occur during the update
				Console.WriteLine($"Error updating rainbow view: {ex.Message}");
			}
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync()
		{
			//!!! this.PhotoViewer.HidePhotos();
			return base.OnDisappearingAsync();
		}


		private SKConfettiSystem CreateSideSystem(SKConfettiEmitterSide side, Rect p)
		{
			SKConfettiSystem Sys = new SKConfettiSystem
			{
				// 2‑second gravity‑driven burst, then fade
				Emitter = SKConfettiEmitter.Stream(100, 2),
				Lifetime = 4,
				FadeOut = true,
				Gravity = new Point(0, 40),
				MinimumInitialVelocity = 500,
				MaximumInitialVelocity = 800,
				MinimumRotationVelocity = 0,
				MaximumRotationVelocity = 360,
				MaximumVelocity = 800,

				// colors, physics & shapes
				Colors = new SKConfettiColorCollection
				{
					Colors.Red, Colors.Yellow, Colors.Green, Colors.Blue
				},
				Physics = new SKConfettiPhysicsCollection
				{
					new SKConfettiPhysics (8,1),
					new SKConfettiPhysics (12,0.6)
				},
				Shapes = new SKConfettiShapeCollection
				{
					new SKConfettiSquareShape(),
					new SKConfettiCircleShape()
				},

				// spawn from the exact side line by default:
				EmitterBounds = new SKConfettiEmitterBounds(p)
			};

			// angle them inward horizontally:
			if (side == SKConfettiEmitterSide.Left)
			{
				// Fan into the center from bottom‐left:
				Sys.StartAngle = 280;
				Sys.EndAngle = 320;
			}
			else
			{
				// Fan into the center from bottom‐right:
				Sys.StartAngle = 220;
				Sys.EndAngle = 260;
			}

			return Sys;
		}

		private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
		{
			try
			{
				if (this.ContentPageModel is ViewIdentityViewModel)
				{
					this.BottomSheet?.ToggleExpanded();
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

	}
}
