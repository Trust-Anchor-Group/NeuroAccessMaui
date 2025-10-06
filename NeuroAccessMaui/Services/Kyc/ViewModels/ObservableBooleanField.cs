namespace NeuroAccessMaui.Services.Kyc.ViewModels
{
    /// <summary>
    /// Boolean field (true/false)
    /// </summary>
    public class ObservableBooleanField : ObservableKycField
    {
        public override string? StringValue
        {
            get => this.BoolValue?.ToString().ToLowerInvariant();
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    this.BoolValue = null;
                }
                else if (bool.TryParse(value, out bool bv))
                {
                    this.BoolValue = bv;
                }
            }
        }

        public bool? BoolValue
        {
            get => this.RawValue as bool?;
            set => this.RawValue = value;
        }
    }
}
