using System.Globalization;
using IdApp.Cv;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NeuroAccessMaui.OCR.Models;

namespace NeuroAccessMaui.OCR.Pipeline
{
	/// <summary>
	/// Runs the DocsaidLab MRZScanner ONNX recognition model for corrected MRZ crops.
	/// </summary>
	public sealed class MrzScannerMrzRecognizer : IDisposable
	{
		private const string modelAssetPath = "OcrModels/mrzscanner_recognition_20250221_fp32.onnx";
		private const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789<";
		private const int padClassIndex = 0;
		private const int endOfSequenceClassIndex = 1;
		private const int separatorClassIndex = 2;
		private const int characterStartIndex = 3;
		private const int defaultInputWidth = 640;
		private const int defaultInputHeight = 64;

		private readonly OnnxModelAssetLoader modelAssetLoader;
		private readonly object sessionSyncRoot = new object();
		private RecognizerSession? session;

		/// <summary>
		/// Initializes a new instance of the <see cref="MrzScannerMrzRecognizer"/> class.
		/// </summary>
		/// <param name="ModelAssetLoader">The packaged model asset loader.</param>
		public MrzScannerMrzRecognizer(OnnxModelAssetLoader ModelAssetLoader)
		{
			ArgumentNullException.ThrowIfNull(ModelAssetLoader);

			this.modelAssetLoader = ModelAssetLoader;
		}

		/// <summary>
		/// Releases resources owned by the recognizer.
		/// </summary>
		public void Dispose()
		{
			lock (this.sessionSyncRoot)
			{
				this.session?.Session.Dispose();
				this.session = null;
			}
		}

		/// <summary>
		/// Gets the image size the recognition model expects, so crops can be produced at that size directly.
		/// </summary>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>The model input width and height.</returns>
		internal (int Width, int Height) GetInputSize(CancellationToken CancellationToken)
		{
			RecognizerSession Session = this.GetSession(CancellationToken);
			return (Session.InputWidth, Session.InputHeight);
		}

		internal OcrTextRecognitionResult Recognize(Matrix<float> MrzImage, CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(MrzImage);
			CancellationToken.ThrowIfCancellationRequested();

			RecognizerSession Session = this.GetSession(CancellationToken);
			float[] InputBuffer = CreateInputBuffer(MrzImage, Session.InputWidth, Session.InputHeight);
			DenseTensor<float> InputTensor = new DenseTensor<float>(InputBuffer, new[] { 1, 3, Session.InputHeight, Session.InputWidth });
			List<NamedOnnxValue> Inputs = new List<NamedOnnxValue>
			{
				NamedOnnxValue.CreateFromTensor(Session.InputName, InputTensor)
			};
			Dictionary<string, string> Metadata = new Dictionary<string, string>
			{
				["OnnxRecognizer"] = "DocsaidLab.MRZScanner",
				["OnnxRecognizerModel"] = "mrzscanner_recognition_20250221_fp32.onnx",
				["OnnxRecognizerInputWidth"] = Session.InputWidth.ToString(CultureInfo.InvariantCulture),
				["OnnxRecognizerInputHeight"] = Session.InputHeight.ToString(CultureInfo.InvariantCulture)
			};

			using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> Outputs = Session.Session.Run(Inputs);
			foreach (DisposableNamedOnnxValue Output in Outputs)
			{
				if (Output.Value is not Tensor<float> Tensor)
					continue;

				LineRecognitionResult Result = Decode(Tensor);
				Metadata["OnnxRecognizerOutputShape"] = string.Join("x", Tensor.Dimensions.ToArray().Select(static Dimension => Dimension.ToString(CultureInfo.InvariantCulture)));
				Metadata["OnnxRecognizerLineCount"] = Result.Lines.Count.ToString(CultureInfo.InvariantCulture);
				Metadata["OnnxRecognizerConfidence"] = Result.Confidence.ToString("0.000000", CultureInfo.InvariantCulture);
				return new OcrTextRecognitionResult(Result.RawText, Result.Lines, Result.Confidence, Metadata);
			}

			Metadata["OnnxRecognizerLineCount"] = "0";
			return new OcrTextRecognitionResult(string.Empty, Array.Empty<string>(), 0f, Metadata);
		}

		private RecognizerSession GetSession(CancellationToken CancellationToken)
		{
			// Loaded before taking the lock, since loading may wait for the main thread.
			OnnxRuntimeNativeLoader.EnsureLoaded();
			lock (this.sessionSyncRoot)
			{
				if (this.session is not null)
					return this.session;

				string ModelPath = this.modelAssetLoader.GetCachedAssetPath(modelAssetPath, CancellationToken);
				SessionOptions SessionOptions = new SessionOptions
				{
					GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
				};
				InferenceSession InferenceSession = new InferenceSession(ModelPath, SessionOptions);
				string InputName = InferenceSession.InputMetadata.Keys.First();
				int[] Dimensions = InferenceSession.InputMetadata[InputName].Dimensions.ToArray();
				this.session = new RecognizerSession(
					InferenceSession,
					InputName,
					ResolveInputDimension(Dimensions, 3, defaultInputWidth),
					ResolveInputDimension(Dimensions, 2, defaultInputHeight));
				return this.session;
			}
		}

		private static int ResolveInputDimension(int[] Dimensions, int Index, int Fallback)
		{
			if (Dimensions.Length > Index && Dimensions[Index] > 0)
				return Dimensions[Index];

			return Fallback;
		}

		/// <summary>
		/// Copies the crop into a 1x3xHxW buffer, repeating the gray value across the three color planes. Crops that do not
		/// already match the model input size are resized first.
		/// </summary>
		private static float[] CreateInputBuffer(Matrix<float> Image, int InputWidth, int InputHeight)
		{
			Matrix<float> Input = Image.Width == InputWidth && Image.Height == InputHeight
				? Image
				: OcrImagePipelineUtilities.Resize(Image, InputWidth, InputHeight);
			int PlaneSize = InputWidth * InputHeight;
			float[] Buffer = new float[PlaneSize * 3];
			int BufferIndex = 0;
			for (int y = 0; y < InputHeight; y++)
			{
				int SourceIndex = Input.StartIndex(0, y);
				for (int x = 0; x < InputWidth; x++)
					Buffer[BufferIndex++] = Math.Clamp(Input.Data[SourceIndex++], 0f, 1f);
			}

			Array.Copy(Buffer, 0, Buffer, PlaneSize, PlaneSize);
			Array.Copy(Buffer, 0, Buffer, PlaneSize * 2, PlaneSize);
			return Buffer;
		}

		private static LineRecognitionResult Decode(Tensor<float> Tensor)
		{
			int[] Dimensions = Tensor.Dimensions.ToArray();
			if (Dimensions.Length < 2)
				return new LineRecognitionResult(string.Empty, Array.Empty<string>(), 0f);

			int TimeSteps = Dimensions[^2];
			int ClassCount = Dimensions[^1];
			float[] Buffer = Tensor.ToArray();
			List<char> Characters = new List<char>();
			float ConfidenceSum = 0f;
			int ConfidenceCount = 0;
			for (int TimeStep = 0; TimeStep < TimeSteps; TimeStep++)
			{
				int RowOffset = TimeStep * ClassCount;
				int BestIndex = 0;
				float MaximumLogit = float.MinValue;
				for (int ClassIndex = 0; ClassIndex < ClassCount; ClassIndex++)
				{
					float Logit = Buffer[RowOffset + ClassIndex];
					if (Logit > MaximumLogit)
					{
						MaximumLogit = Logit;
						BestIndex = ClassIndex;
					}
				}

				if (BestIndex == endOfSequenceClassIndex)
					break;
				if (BestIndex == padClassIndex)
					continue;

				double ExponentSum = 0d;
				for (int ClassIndex = 0; ClassIndex < ClassCount; ClassIndex++)
					ExponentSum += Math.Exp(Buffer[RowOffset + ClassIndex] - MaximumLogit);

				if (BestIndex == separatorClassIndex)
					Characters.Add('\n');
				else
				{
					int AlphabetIndex = BestIndex - characterStartIndex;
					if (AlphabetIndex >= 0 && AlphabetIndex < alphabet.Length)
						Characters.Add(alphabet[AlphabetIndex]);
				}

				ConfidenceSum += (float)(1d / Math.Max(1d, ExponentSum));
				ConfidenceCount++;
			}

			string RawText = new string(Characters.ToArray()).Trim('\n');
			List<string> Lines = RawText
				.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToList();
			float Confidence = ConfidenceCount == 0 ? 0f : ConfidenceSum / ConfidenceCount;
			return new LineRecognitionResult(RawText, Lines, Confidence);
		}

		private sealed record RecognizerSession(InferenceSession Session, string InputName, int InputWidth, int InputHeight);

		private readonly record struct LineRecognitionResult(string RawText, IReadOnlyList<string> Lines, float Confidence);
	}
}
