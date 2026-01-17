using System;
using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.Services.Nfc.Ui
{
    /// <summary>
    /// Represents a decoded view of a single NDEF record for UI display and history.
    /// </summary>
    public sealed class NfcNdefRecordSnapshot
    {
        private int index;
        private string recordType = string.Empty;
        private string recordTnf = string.Empty;
        private string? wellKnownType;
        private string? contentType;
        private string? externalType;
        private string? uri;
        private string? text;
        private byte[]? payload;
        private bool isPayloadDerived;

        /// <summary>
        /// Initializes a new instance of the <see cref="NfcNdefRecordSnapshot"/> class.
        /// </summary>
        public NfcNdefRecordSnapshot()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NfcNdefRecordSnapshot"/> class.
        /// </summary>
        /// <param name="Index">Zero-based record index within the NDEF message.</param>
        /// <param name="RecordType">Human-readable record kind (e.g., URI, Text, MIME).</param>
        /// <param name="RecordTnf">NDEF TNF/type category (best-effort).</param>
        /// <param name="WellKnownType">Optional NFC Forum well-known type (e.g., "U", "T").</param>
        /// <param name="ContentType">Optional MIME content type.</param>
        /// <param name="ExternalType">Optional external type (e.g., "domain:type").</param>
        /// <param name="Uri">Optional decoded URI (if present).</param>
        /// <param name="Text">Optional decoded text (if present).</param>
        public NfcNdefRecordSnapshot(
            int Index,
            string RecordType,
            string RecordTnf,
            string? WellKnownType,
            string? ContentType,
            string? ExternalType,
            string? Uri,
            string? Text,
            byte[]? Payload = null,
            bool IsPayloadDerived = false)
        {
            this.index = Index;
            this.recordType = RecordType ?? string.Empty;
            this.recordTnf = RecordTnf ?? string.Empty;
            this.wellKnownType = WellKnownType;
            this.contentType = ContentType;
            this.externalType = ExternalType;
            this.uri = Uri;
            this.text = Text;
            this.payload = Payload;
            this.isPayloadDerived = IsPayloadDerived;
        }

        /// <summary>
        /// Gets or sets the zero-based record index within the NDEF message.
        /// </summary>
        public int Index
        {
            get => this.index;
            set => this.index = value;
        }

        /// <summary>
        /// Gets or sets the human-readable record kind.
        /// </summary>
        public string RecordType
        {
            get => this.recordType;
            set => this.recordType = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the NDEF TNF/type category (best-effort).
        /// </summary>
        public string RecordTnf
        {
            get => this.recordTnf;
            set => this.recordTnf = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the NFC Forum well-known type (e.g., "U", "T").
        /// </summary>
        public string? WellKnownType
        {
            get => this.wellKnownType;
            set => this.wellKnownType = value;
        }

        /// <summary>
        /// Gets or sets the MIME content type.
        /// </summary>
        public string? ContentType
        {
            get => this.contentType;
            set => this.contentType = value;
        }

        /// <summary>
        /// Gets or sets the external type identifier.
        /// </summary>
        public string? ExternalType
        {
            get => this.externalType;
            set => this.externalType = value;
        }

        /// <summary>
        /// Gets or sets the decoded URI.
        /// </summary>
        public string? Uri
        {
            get => this.uri;
            set => this.uri = value;
        }

        /// <summary>
        /// Gets or sets the decoded text.
        /// </summary>
        public string? Text
        {
            get => this.text;
            set => this.text = value;
        }

        /// <summary>
        /// Gets or sets the raw record payload bytes, if available.
        /// </summary>
        [DefaultValueNull]
        public byte[]? Payload
        {
            get => this.payload;
            set => this.payload = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="Payload"/> is derived (best-effort) rather than the original NFC payload.
        /// </summary>
        public bool IsPayloadDerived
        {
            get => this.isPayloadDerived;
            set => this.isPayloadDerived = value;
        }

        /// <summary>
        /// Gets the payload size in bytes.
        /// </summary>
        public int PayloadSizeBytes
        {
            get
            {
                if (this.payload is not null)
                    return this.payload.Length;

                if (!string.IsNullOrWhiteSpace(this.uri))
                    return System.Text.Encoding.UTF8.GetByteCount(this.uri);

                if (!string.IsNullOrWhiteSpace(this.text))
                    return System.Text.Encoding.UTF8.GetByteCount(this.text);

                return 0;
            }
        }

        /// <summary>
        /// Gets the payload encoded as Base64, if available.
        /// </summary>
        public string PayloadBase64
        {
            get
            {
                if (this.payload is null || this.payload.Length == 0)
                    return string.Empty;

                return Convert.ToBase64String(this.payload);
            }
        }

        /// <summary>
        /// Gets the payload encoded as hex, if available.
        /// </summary>
        public string PayloadHex
        {
            get
            {
                if (this.payload is null || this.payload.Length == 0)
                    return string.Empty;

                return Convert.ToHexString(this.payload);
            }
        }

        /// <summary>
        /// Gets a compact display string for UI.
        /// </summary>
        public string DisplayValue
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(this.uri))
                    return this.uri;

                if (!string.IsNullOrWhiteSpace(this.text))
                    return this.text;

                if (!string.IsNullOrWhiteSpace(this.contentType))
                    return this.contentType;

                if (!string.IsNullOrWhiteSpace(this.externalType))
                    return this.externalType;

                if (!string.IsNullOrWhiteSpace(this.wellKnownType))
                    return this.wellKnownType;

                return string.Empty;
            }
        }
    }
}
