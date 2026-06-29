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
		private const string ModelAssetPath = "OcrModels/mrzscanner_recognition_20250221_fp32.onnx";
		private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789<";
		private const int PadClassIndex = 0;
		private const int EndOfSequenceClassIndex = 1;
		private const int SeparatorClassIndex = 2;
		private const int CharacterStartIndex = 3;
		private const int DefaultInputWidth = 640;
		private const int DefaultInputHeight = 64;

		private readonly OnnxModelAssetLoader modelAssetLoader;
		private readonly object sessionSyncRoot = new object();
		private InferenceSession? session;

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
				this.session?.Dispose();
				this.session = null;
			}
		}

		internal OcrTextRecognitionResult Recognize(Matrix<float> MrzImage, CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(MrzImage);
			CancellationToken.ThrowIfCancellationRequested();

			InferenceSession Session = this.GetSession(CancellationToken);
			string InputName = Session.InputMetadata.Keys.First();
			NodeMetadata InputMetadata = Session.InputMetadata[InputName];
			int InputWidth = ResolveInputDimension(InputMetadata, 3, DefaultInputWidth);
			int InputHeight = ResolveInputDimension(InputMetadata, 2, DefaultInputHeight);
			DenseTensor<float> InputTensor = CreateInputTensor(MrzImage, InputWidth, InputHeight);
			List<NamedOnnxValue> Inputs = new List<NamedOnnxValue>
			{
				NamedOnnxValue.CreateFromTensor(InputName, InputTensor)
			};
			Dictionary<string, string> Metadata = new Dictionary<string, string>
			{
				["OnnxRecognizer"] = "DocsaidLab.MRZScanner",
				["OnnxRecognizerModel"] = "mrzscanner_recognition_20250221_fp32.onnx",
				["OnnxRecognizerInputName"] = InputName,
				["OnnxRecognizerInputWidth"] = InputWidth.ToString(CultureInfo.InvariantCulture),
				["OnnxRecognizerInputHeight"] = InputHeight.ToString(CultureInfo.InvariantCulture)
			};

			using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> Outputs = Session.Run(Inputs);
			foreach (DisposableNamedOnnxValue Output in Outputs)
			{
				if (Output.Value is not Tensor<float> Tensor)
					continue;

				LineRecognitionResult Result = Decode(Tensor);
				Metadata["OnnxRecognizerOutputName"] = Output.Name;
				Metadata["OnnxRecognizerOutputShape"] = string.Join("x", Tensor.Dimensions.ToArray().Select(static Dimension => Dimension.ToString(CultureInfo.InvariantCulture)));
				Metadata["OnnxRecognizerLineCount"] = Result.Lines.Count.ToString(CultureInfo.InvariantCulture);
				for (int Index = 0; Index < Result.Lines.Count; Index++)
				{
					Metadata["OnnxRecognizerLine" + Index.ToString(CultureInfo.InvariantCulture) + "Text"] = Result.Lines[Index];
					Metadata["OnnxRecognizerLine" + Index.ToString(CultureInfo.InvariantCulture) + "Confidence"] = Result.Confidence.ToString("0.000000", CultureInfo.InvariantCulture);
				}

				return new OcrTextRecognitionResult(Result.RawText, Result.Lines, Result.Confidence, Metadata);
			}

			Metadata["OnnxRecognizerLineCount"] = "0";
			return new OcrTextRecognitionResult(string.Empty, Array.Empty<string>(), 0f, Metadata);
		}

		private InferenceSession GetSession(CancellationToken CancellationToken)
		{
			lock (this.sessionSyncRoot)
			{
				if (this.session is not null)
					return this.session;

				OnnxRuntimeNativeLoader.EnsureLoaded();
				string ModelPath = this.modelAssetLoader.GetCachedAssetPath(ModelAssetPath, CancellationToken);
				SessionOptions SessionOptions = new SessionOptions
				{
					GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
				};
				this.session = new InferenceSession(ModelPath, SessionOptions);
				return this.session;
			}
		}

		private static int ResolveInputDimension(NodeMetadata Metadata, int Index, int Fallback)
		{
			int[] Dimensions = Metadata.Dimensions.ToArray();
			if (Dimensions.Length > Index && Dimensions[Index] > 0)
				return Dimensions[Index];

			return Fallback;
		}

		private static DenseTensor<float> CreateInputTensor(Matrix<float> Image, int InputWidth, int InputHeight)
		{
			Matrix<float> Resized = OcrImagePipelineUtilities.Resize(Image, InputWidth, InputHeight);
			DenseTensor<float> Tensor = new DenseTensor<float>(new[] { 1, 3, InputHeight, InputWidth });
			for (int y = 0; y < InputHeight; y++)
			{
				for (int x = 0; x < InputWidth; x++)
				{
					float Value = Math.Clamp(Resized[x, y], 0f, 1f);
					Tensor[0, 0, y, x] = Value;
					Tensor[0, 1, y, x] = Value;
					Tensor[0, 2, y, x] = Value;
				}
			}

			return Tensor;
		}

		private static LineRecognitionResult Decode(Tensor<float> Tensor)
		{
			int[] Dimensions = Tensor.Dimensions.ToArray();
			if (Dimensions.Length < 2)
				return new LineRecognitionResult(string.Empty, Array.Empty<string>(), 0f);

			int TimeSteps;
			int ClassCount;
			if (Dimensions.Length == 3)
			{
				TimeSteps = Dimensions[1];
				ClassCount = Dimensions[2];
			}
			else
			{
				TimeSteps = Dimensions[0];
				ClassCount = Dimensions[1];
			}

			float[] Buffer = Tensor.ToArray();
			List<char> Characters = new List<char>();
			float ConfidenceSum = 0f;
			int ConfidenceCount = 0;
			for (int TimeStep = 0; TimeStep < TimeSteps; TimeStep++)
			{
				int RowOffset = TimeStep * ClassCount;
				int BestIndex = 0;
				float BestProbability = float.MinValue;
				float MaximumLogit = float.MinValue;
				for (int ClassIndex = 0; ClassIndex < ClassCount; ClassIndex++)
					MaximumLogit = Math.Max(MaximumLogit, Buffer[RowOffset + ClassIndex]);

				double ProbabilitySum = 0d;
				for (int ClassIndex = 0; ClassIndex < ClassCount; ClassIndex++)
					ProbabilitySum += Math.Exp(Buffer[RowOffset + ClassIndex] - MaximumLogit);

				for (int ClassIndex = 0; ClassIndex < ClassCount; ClassIndex++)
				{
					float Probability = (float)(Math.Exp(Buffer[RowOffset + ClassIndex] - MaximumLogit) / Math.Max(double.Epsilon, ProbabilitySum));
					if (Probability > BestProbability)
					{
						BestProbability = Probability;
						BestIndex = ClassIndex;
					}
				}

				if (BestIndex == EndOfSequenceClassIndex)
					break;
				if (BestIndex == PadClassIndex)
					continue;

				if (BestIndex == SeparatorClassIndex)
					Characters.Add('\n');
				else
				{
					int AlphabetIndex = BestIndex - CharacterStartIndex;
					if (AlphabetIndex >= 0 && AlphabetIndex < Alphabet.Length)
						Characters.Add(Alphabet[AlphabetIndex]);
				}

				ConfidenceSum += BestProbability;
				ConfidenceCount++;
			}

			string RawText = new string(Characters.ToArray()).Trim('\n');
			List<string> Lines = RawText
				.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToList();
			float Confidence = ConfidenceCount == 0 ? 0f : ConfidenceSum / ConfidenceCount;
			return new LineRecognitionResult(RawText, Lines, Confidence);
		}

		private readonly record struct LineRecognitionResult(string RawText, IReadOnlyList<string> Lines, float Confidence);
	}
}
