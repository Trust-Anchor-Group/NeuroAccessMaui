using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using SkiaSharp;
using System.ComponentModel;
using System.Text;
using Waher.Events;
using Waher.Script;
using Waher.Script.Graphs;
using Waher.Script.Objects.VectorSpaces;
using Waher.Things.SensorData;

namespace NeuroAccessMaui.UI.Pages.Things.ReadSensor.Model
{
	/// <summary>
	/// Represents a set of historical field values.
	/// </summary>
	public class GraphModel : INotifyPropertyChanged, IDisposable
	{
		private readonly SortedDictionary<DateTime, Field> fieldValues = [];
		private readonly SortedDictionary<DateTime, Field> minFieldValues = [];
		private readonly SortedDictionary<DateTime, Field> maxFieldValues = [];
		private readonly string fieldName;
		private Timer? timer = null;
		private ImageSource? image = null;

		/// <summary>
		/// Represents a set of historical field values.
		/// </summary>
		/// <param name="Field">Field added</param>
		public GraphModel(Field Field)
		{
			this.fieldName = Field.Name;

			this.Add(Field);
		}

		/// <summary>
		/// Field Name
		/// </summary>
		public string FieldName => this.fieldName;

		/// <summary>
		/// Image Source
		/// </summary>
		public ImageSource? Image => this.image;

		/// <summary>
		/// If graph has an image.
		/// </summary>
		public bool HasImage => this.image is not null;

		/// <summary>
		/// Adds a historical field value.
		/// </summary>
		/// <param name="Field">Field Value.</param>
		public void Add(Field Field)
		{
			lock (this.fieldValues)
			{
				this.fieldValues[Field.Timestamp] = Field;
			}

			this.Invalidate();
		}

		/// <summary>
		/// Adds a historical minimum field value.
		/// </summary>
		/// <param name="Field">Field Value.</param>
		public void AddMin(Field Field)
		{
			lock (this.fieldValues)
			{
				this.minFieldValues[Field.Timestamp] = Field;
			}

			this.Invalidate();
		}

		/// <summary>
		/// Adds a historical maximum field value.
		/// </summary>
		/// <param name="Field">Field Value.</param>
		public void AddMax(Field Field)
		{
			lock (this.fieldValues)
			{
				this.maxFieldValues[Field.Timestamp] = Field;
			}

			this.Invalidate();
		}

		private void Invalidate()
		{
			this.timer?.Dispose();
			this.timer = null;

			this.timer = new Timer(this.GenerateGraph, null, 500, Timeout.Infinite);
		}

		private async void GenerateGraph(object? _)
		{
			try
			{
				Variables v = [];
				Expression Exp;
				bool HasMin = false;
				bool HasMax = false;

				lock (this.fieldValues)
				{
					int c = this.fieldValues.Count;
					DateTime[] Timepoints = new DateTime[c];
					Field[] Values = new Field[c];

					this.fieldValues.Keys.CopyTo(Timepoints, 0);
					this.fieldValues.Values.CopyTo(Values, 0);

					v["x"] = new DateTimeVector(Timepoints);
					v["y"] = new ObjectVector(Values);

					if (this.minFieldValues.Count > 0)
					{
						c = this.minFieldValues.Count;

						Timepoints = new DateTime[c];
						Values = new Field[c];

						this.minFieldValues.Keys.CopyTo(Timepoints, 0);
						this.minFieldValues.Values.CopyTo(Values, 0);

						v["xMin"] = new DateTimeVector(Timepoints);
						v["yMin"] = new ObjectVector(Values);

						HasMin = true;
					}

					if (this.maxFieldValues.Count > 0)
					{
						c = this.maxFieldValues.Count;

						Timepoints = new DateTime[c];
						Values = new Field[c];

						this.maxFieldValues.Keys.CopyTo(Timepoints, 0);
						this.maxFieldValues.Values.CopyTo(Values, 0);

						v["xMax"] = new DateTimeVector(Timepoints);
						v["yMax"] = new ObjectVector(Values);

						HasMax = true;
					}
				}

				StringBuilder sb = new();
				bool Found = false;

				sb.AppendLine("y:=y.Value;");
				sb.AppendLine("G:=scatter2d(x,y,'Black',7);");

				if (HasMin)
				{
					sb.AppendLine("yMin:=yMin.Value;");
					sb.AppendLine("G+=scatter2d(xMin,yMin,'Black',7);");
				}

				if (HasMax)
				{
					sb.AppendLine("yMax:=yMax.Value;");
					sb.AppendLine("G+=scatter2d(xMax,yMax,'Black',7);");
				}

				if (HasMin)
					sb.AppendLine("G+=plot2dline(xMin,yMin,'Blue',5);");

				if (HasMax)
					sb.AppendLine("G+=plot2dline(xMax,yMax,'Red',5);");

				sb.AppendLine("G+=plot2dline(x,y,'Green',5);");
				sb.Append("G.LabelX:='");
				sb.Append(ServiceRef.Localizer[nameof(AppResources.Time)]);
				sb.AppendLine("';");
				sb.Append("G.LabelY:='");

				foreach (Field F in this.fieldValues.Values)
				{
					if (F is QuantityField Q)
					{
						sb.Append(Q.Unit);
						Found = true;
						break;
					}
				}

				if (!Found)
					sb.Append(this.fieldName.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t").Replace("'", "\\'"));

				sb.AppendLine("';");
				sb.AppendLine("G");

				Exp = new Expression(sb.ToString());

				Graph? Graph = await Exp.EvaluateAsync(v) as Graph;

				if (Graph is not null)
				{
					GraphSettings Settings = new()
					{
						Width = 1280,
						Height = 720,
						AxisColor = SKColors.Black,         // TODO: Light & Dark Theme
						BackgroundColor = SKColors.White,   // TODO: Light & Dark Theme
						GridColor = SKColors.LightGray,     // TODO: Light & Dark Theme
						AxisWidth = 5,
						FontName = "Arial",                 // TODO: App font
						LabelFontSize = 40,
						GridWidth = 3
					};

					PixelInformation Pixels = Graph.CreatePixels(Settings);
					byte[] Png = Pixels.EncodeAsPng();

					MainThread.BeginInvokeOnMainThread(() =>
					{
						bool OldWasNull = this.image is null;
						this.image = ImageSource.FromStream(() => new MemoryStream(Png));

						this.RaisePropertyChanged(nameof(this.Image));
						if (OldWasNull)
							this.RaisePropertyChanged(nameof(this.HasImage));
					});
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		private void RaisePropertyChanged(string Name)
		{
			try
			{
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			this.timer?.Dispose();
			this.timer = null;
		}

		/// <summary>
		/// Event raised when a property has changed.
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;
	}
}
