namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Represents a strongly typed key used for identifying animations.
	/// </summary>
	public readonly record struct AnimationKey
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AnimationKey"/> struct.
		/// </summary>
		/// <param name="value">Raw key value.</param>
		/// <exception cref="ArgumentException">Thrown if the key is null or whitespace.</exception>
		public AnimationKey(string RawValue)
		{
			if (string.IsNullOrWhiteSpace(RawValue))
				throw new ArgumentException("Animation key cannot be null or whitespace.", nameof(RawValue));

			this.Value = RawValue;
		}

		/// <summary>
		/// Gets the raw key value.
		/// </summary>
		public string Value { get; }

		/// <summary>
		/// Converts the key to its string representation.
		/// </summary>
		/// <returns>Key as string.</returns>
		public override string ToString() => this.Value;

		/// <summary>
		/// Implicit conversion helper enabling string to <see cref="AnimationKey"/> assignments.
		/// </summary>
		public static implicit operator AnimationKey(string RawValue) => new AnimationKey(RawValue);
	}
}
