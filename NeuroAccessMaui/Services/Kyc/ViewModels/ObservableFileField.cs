using System.Collections.Generic;
using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.Services.Kyc.ViewModels
{
    public class ObservableFileField : ObservableKycField
    {
        public override string? StringValue
        {
            get => this.RawValue as string;
            set => this.RawValue = value;
        }
        public int? MaxFileSizeMB
        {
            get
            {
                object? Value;
                return this.Metadata.TryGetValue("MaxFileSizeMB", out Value) ? Value as int? : null;
            }
        }
        public string[]? AllowedFileTypes
        {
            get
            {
                object? Value;
                return this.Metadata.TryGetValue("AllowedFileTypes", out Value) ? Value as string[] : null;
            }
        }
    }
}
