using System.Globalization;
using NeuroAccessMaui.Animations;

namespace NeuroAccessMaui.UI.Controls
{
// AnimatedCheckbox.cs
public class AnimatedCheckbox : ContentView
{
    readonly GraphicsView _gfx;
    readonly CheckboxDrawable _drawable;
    double _scale = 1.0;

    public static readonly BindableProperty IsCheckedProperty =
        BindableProperty.Create(nameof(IsChecked), typeof(bool), typeof(AnimatedCheckbox), false,
            propertyChanged: (b, o, n) => ((AnimatedCheckbox)b).AnimateTo((bool)n));

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public static readonly BindableProperty StrokeColorProperty =
        BindableProperty.Create(
            nameof(StrokeColor),
            typeof(Color),
            typeof(AnimatedCheckbox),
            Colors.Black,
            propertyChanged: (b, o, n) => ((AnimatedCheckbox)b).OnStrokeColorChanged((Color)n));

    public static readonly BindableProperty FillColorProperty =
        BindableProperty.Create(
            nameof(FillColor),
            typeof(Color),
            typeof(AnimatedCheckbox),
            Colors.Transparent,
            propertyChanged: (b, o, n) => ((AnimatedCheckbox)b).OnFillColorChanged((Color)n));

    public static readonly BindableProperty StrokeWidthProperty =
        BindableProperty.Create(
            nameof(StrokeWidth),
            typeof(float),
            typeof(AnimatedCheckbox),
            2f,
            propertyChanged: (b, o, n) => ((AnimatedCheckbox)b).OnStrokeWidthChanged((float)n));

    public Color StrokeColor
    {
        get => (Color)GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    public Color FillColor
    {
        get => (Color)GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }

    public float StrokeWidth
    {
        get => (float)GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    public AnimatedCheckbox()
    {
        _drawable = new CheckboxDrawable();
        _gfx = new GraphicsView { Drawable = _drawable, HeightRequest = 28, WidthRequest = 28 };
        Content = new Grid { Children = { _gfx } };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, __) => IsChecked = !IsChecked;
        // Attach to both the container and the graphics view to ensure taps are captured
        GestureRecognizers.Add(tap);
        _gfx.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => IsChecked = !IsChecked) });

        // Accessibility
        this.SetBinding(SemanticProperties.DescriptionProperty, new Binding(nameof(this.IsChecked), source: this, converter: new BoolToStr("Checked", "Unchecked")));
    }

    void OnStrokeColorChanged(Color color)
    {
        if (_drawable is not null)
        {
            _drawable.StrokeColor = color;
            _gfx?.Invalidate();
        }
    }

    void OnFillColorChanged(Color color)
    {
        if (_drawable is not null)
        {
            _drawable.FillColor = color;
            _gfx?.Invalidate();
        }
    }

    void OnStrokeWidthChanged(float width)
    {
        if (_drawable is not null)
        {
            _drawable.StrokeWidth = width;
            _gfx?.Invalidate();
        }
    }

    /// <summary>
    /// Draws the check mark in or out, with a small pop, or jumps straight to the end state when motion is reduced.
    /// </summary>
    /// <param name="TargetChecked">The checked state to show.</param>
    private void AnimateTo(bool TargetChecked)
    {
        double From = this._drawable.Progress;
        double To = TargetChecked ? 1.0 : 0.0;

        if (MotionPreferences.ReduceMotion)
        {
            this.AbortAnimation("chk");
            this._drawable.Progress = To;
            this._scale = 1.0;
            this.Scale = 1.0;
            this._gfx.Invalidate();
            return;
        }

        Animation Draw = new Animation(Progress =>
        {
            this._drawable.Progress = Progress;
            this._gfx.Invalidate();
        }, From, To, Easing.CubicOut);

        Animation Pop = new Animation(Value => { this._scale = Value; this.Scale = this._scale; }, 1, 1.06, Easing.CubicOut);
        Animation PopBack = new Animation(Value => { this._scale = Value; this.Scale = this._scale; }, 1.06, 1, Easing.CubicIn);

        Animation Parent = new Animation();
        Parent.Add(0, 1, Draw);
        Parent.Add(0, 0.5, Pop);
        Parent.Add(0.5, 1, PopBack);

        Parent.Commit(this, "chk", length: 180);
    }

    /// <summary>
    /// Converts the checked state to the text used as the control's accessibility description.
    /// </summary>
    /// <param name="TrueText">The text used when the value is <c>true</c>.</param>
    /// <param name="FalseText">The text used when the value is <c>false</c> or not a boolean.</param>
    private sealed class BoolToStr(string TrueText, string FalseText) : IValueConverter
    {
        /// <summary>
        /// Converts a boolean to its description text.
        /// </summary>
        /// <param name="Value">The value to convert.</param>
        /// <param name="TargetType">The binding target type.</param>
        /// <param name="Parameter">Not used.</param>
        /// <param name="Culture">Not used.</param>
        /// <returns>The text for <c>true</c>, or the text for <c>false</c> when the value is anything else.</returns>
        public object Convert(object? Value, Type TargetType, object? Parameter, CultureInfo Culture) =>
            Value is bool B && B ? TrueText : FalseText;

        /// <summary>
        /// Not supported; the description is one-way.
        /// </summary>
        /// <param name="Value">Not used.</param>
        /// <param name="TargetType">Not used.</param>
        /// <param name="Parameter">Not used.</param>
        /// <param name="Culture">Not used.</param>
        /// <returns>Never returns.</returns>
        /// <exception cref="NotImplementedException">Always thrown.</exception>
        public object ConvertBack(object? Value, Type TargetType, object? Parameter, CultureInfo Culture) =>
            throw new NotImplementedException();
    }
}

// CheckboxDrawable.cs
public class CheckboxDrawable : IDrawable
{
    public double Progress { get; set; } = 0;            // 0..1
    public Color StrokeColor { get; set; } = Colors.Black;
    public Color FillColor { get; set; } = Colors.Transparent;
    public float StrokeWidth { get; set; } = 2f;
    const float Corner = 6f;

    public void Draw(ICanvas canvas, RectF rect)
    {
        canvas.SaveState();

        // Box
        var box = rect.Inflate(-2, -2);
        canvas.FillColor = FillColor;
        canvas.StrokeColor = StrokeColor;
        canvas.StrokeSize = StrokeWidth;
        canvas.FillRoundedRectangle(box, Corner);
        canvas.DrawRoundedRectangle(box, Corner);

        // Checkmark path points (relative)
        // Points tuned for a 28x28 box; scale to rect
        var p1 = new PointF(box.X + box.Width * 0.26f, box.Y + box.Height * 0.54f);
        var p2 = new PointF(box.X + box.Width * 0.44f, box.Y + box.Height * 0.72f);
        var p3 = new PointF(box.X + box.Width * 0.76f, box.Y + box.Height * 0.34f);

        // Draw “from nothing” by interpolating along the polyline length
        var total = Distance(p1, p2) + Distance(p2, p3);
        var drawLen = (float)(total * Progress);

        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        // Segment 1
        var d1 = Distance(p1, p2);
        if (drawLen > 0)
        {
            if (drawLen <= d1)
            {
                var t = drawLen / d1;
                var mid = Lerp(p1, p2, t);
                canvas.DrawLine(p1, mid);
            }
            else
            {
                canvas.DrawLine(p1, p2);
                // Segment 2
                var rem = drawLen - d1;
                var t = Math.Clamp(rem / Distance(p2, p3), 0, 1);
                var mid = Lerp(p2, p3, t);
                canvas.DrawLine(p2, mid);
            }
        }

        canvas.RestoreState();
    }

    static float Distance(PointF a, PointF b) => (float)Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));
    static PointF Lerp(PointF a, PointF b, float t) => new(a.X + (b.X-a.X)*t, a.Y + (b.Y-a.Y)*t);
}

}
