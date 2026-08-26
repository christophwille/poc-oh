using System.ComponentModel.DataAnnotations;

namespace SampleCoreWcfNet10.Security;

public sealed class MtlsOptions
{
    public const string SectionName = "Mtls";

    /// <summary>Path to the private CA certificate (PEM/DER) that issues partner client certificates.</summary>
    [Required] public string CaCertificatePath { get; set; } = "";

    /// <summary>Explicit allow-list: a CA-issued certificate is only accepted if it is listed here.</summary>
    [MinLength(1)] public List<Partner> Partners { get; set; } = [];

    public sealed class Partner
    {
        [Required] public string Name { get; set; } = "";

        /// <summary>Hex SHA-256 fingerprint of the partner's client certificate.</summary>
        [Required] public string Sha256Thumbprint { get; set; } = "";
    }
}
