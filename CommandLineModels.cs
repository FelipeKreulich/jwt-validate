using System.CommandLine;

namespace JwtValidator
{
    public static class CommandLineOptions
    {
        public static readonly Option<string> SubjectOption = new(
            aliases: ["--subject", "-s"],
            description: "Subject for the JWT token (e.g., email or user ID)"
        )
        {
            IsRequired = false,
        };

        public static readonly Option<string> ClaimsOption = new(
            aliases: ["--claims", "-c"],
            description: "Additional claims in JSON format (e.g., '{\"role\":\"admin\",\"permissions\":\"read,write\"}')"
        )
        {
            IsRequired = false,
        };

        public static readonly Option<string> TokenOption = new(
            aliases: ["--token", "-t"],
            description: "JWT token to validate"
        )
        {
            IsRequired = false,
        };

        public static readonly Option<string> OutputOption = new(
            aliases: ["--output", "-o"],
            description: "Output file path to save the generated token"
        )
        {
            IsRequired = false,
        };

        public static readonly Option<bool> VerboseOption = new(
            aliases: ["--verbose", "-v"],
            description: "Enable verbose output"
        )
        {
            IsRequired = false,
        };

        public static readonly Option<bool> PrettyPrintOption = new(
            aliases: ["--pretty", "-p"],
            description: "Pretty print validation results"
        )
        {
            IsRequired = false,
        };

        public static readonly Option<string> ConfigFileOption = new(
            aliases: ["--config", "-f"],
            description: "Path to configuration file (default: appsettings.json)"
        )
        {
            IsRequired = false,
        };

        static CommandLineOptions()
        {
            ConfigFileOption.SetDefaultValue("appsettings.json");
        }
    }

    public class GenerateTokenOptions
    {
        public string Subject { get; set; } = "default";
        public string? Claims { get; set; }
        public string? OutputFile { get; set; }
        public bool Verbose { get; set; }
        public string ConfigFile { get; set; } = "appsettings.json";
    }

    public class ValidateTokenOptions
    {
        public string Token { get; set; } = "";
        public bool PrettyPrint { get; set; }
        public bool Verbose { get; set; }
        public string ConfigFile { get; set; } = "appsettings.json";
    }
}
