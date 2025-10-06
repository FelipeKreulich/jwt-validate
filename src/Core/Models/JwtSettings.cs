namespace JwtValidator.Core.Models
{
    public class JwtSettings
    {
        public string Secret { get; set; } = "";
        public string Issuer { get; set; } = "";
        public string Audience { get; set; } = "";
        public int ExpiryMinutes { get; set; }
        public string Algorithm { get; set; } = "HS256"; // default
        public string? PrivateKeyPath { get; set; }
        public string? PublicKeyPath { get; set; }
    }
}
