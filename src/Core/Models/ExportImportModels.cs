using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text.Json;

namespace JwtValidator.Core.Models
{
    public class ExportData
    {
        public string Version { get; set; } = "1.0";
        public DateTime ExportDate { get; set; } = DateTime.UtcNow;
        public JwtSettings? JwtSettings { get; set; }
        public List<TokenData>? Tokens { get; set; }
        public List<ClaimsTemplate>? ClaimsTemplates { get; set; }
    }

    public class TokenData
    {
        public string Subject { get; set; } = "";
        public Dictionary<string, string> Claims { get; set; } = new();
        public string Token { get; set; } = "";
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string? Description { get; set; }
    }

    public class ClaimsTemplate
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public Dictionary<string, string> Claims { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int TokensImported { get; set; }
        public int TemplatesImported { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public static class ExportImportOptions
    {
        public static readonly Option<string> ExportFileOption = new(
            aliases: ["--export-file", "-e"],
            description: "File path for export"
        )
        {
            IsRequired = false,
        };

        public static readonly Option<string> ImportFileOption = new(
            aliases: ["--import-file", "-i"],
            description: "File path for import"
        )
        {
            IsRequired = false,
        };

        public static readonly Option<bool> IncludeTokensOption = new(
            aliases: ["--include-tokens"],
            description: "Include generated tokens in export"
        )
        {
            IsRequired = false,
        };

        public static readonly Option<bool> IncludeTemplatesOption = new(
            aliases: ["--include-templates"],
            description: "Include claims templates in export"
        )
        {
            IsRequired = false,
        };

        public static readonly Option<bool> IncludeSettingsOption = new(
            aliases: ["--include-settings"],
            description: "Include JWT settings in export"
        )
        {
            IsRequired = false,
        };
    }
}
