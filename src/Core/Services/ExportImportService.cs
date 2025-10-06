using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JwtValidator.Core.Models;

namespace JwtValidator.Core.Services
{
    public class ExportImportService
    {
        private readonly string _defaultExportPath;
        private readonly List<TokenData> _generatedTokens;
        private readonly List<ClaimsTemplate> _claimsTemplates;

        public ExportImportService()
        {
            _defaultExportPath = Path.Combine(Directory.GetCurrentDirectory(), "exports");
            _generatedTokens = new List<TokenData>();
            _claimsTemplates = new List<ClaimsTemplate>();

            // Criar diretório de exports se não existir
            if (!Directory.Exists(_defaultExportPath))
            {
                Directory.CreateDirectory(_defaultExportPath);
            }
        }

        public void AddGeneratedToken(
            string subject,
            Dictionary<string, string> claims,
            string token,
            string? description = null
        )
        {
            var tokenData = new TokenData
            {
                Subject = subject,
                Claims = claims,
                Token = token,
                GeneratedAt = DateTime.UtcNow,
                Description = description,
            };

            _generatedTokens.Add(tokenData);
        }

        public void AddClaimsTemplate(
            string name,
            string description,
            Dictionary<string, string> claims
        )
        {
            var template = new ClaimsTemplate
            {
                Name = name,
                Description = description,
                Claims = claims,
                CreatedAt = DateTime.UtcNow,
            };

            _claimsTemplates.Add(template);
        }

        public async Task<string> ExportToFileAsync(
            string filePath,
            JwtSettings? jwtSettings = null,
            bool includeTokens = true,
            bool includeTemplates = true,
            bool includeSettings = true
        )
        {
            try
            {
                var exportData = new ExportData { Version = "1.0", ExportDate = DateTime.UtcNow };

                if (includeSettings && jwtSettings != null)
                {
                    exportData.JwtSettings = jwtSettings;
                }

                if (includeTokens)
                {
                    exportData.Tokens = _generatedTokens.ToList();
                }

                if (includeTemplates)
                {
                    exportData.ClaimsTemplates = _claimsTemplates.ToList();
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };

                var json = JsonSerializer.Serialize(exportData, options);
                await File.WriteAllTextAsync(filePath, json);

                return $"Export completed successfully to: {filePath}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Export failed: {ex.Message}");
            }
        }

        public async Task<ImportResult> ImportFromFileAsync(string filePath)
        {
            var result = new ImportResult();

            try
            {
                if (!File.Exists(filePath))
                {
                    result.Success = false;
                    result.Message = "Import file does not exist.";
                    return result;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var exportData = JsonSerializer.Deserialize<ExportData>(json, options);

                if (exportData == null)
                {
                    result.Success = false;
                    result.Message = "Invalid export file format.";
                    return result;
                }

                // Importar tokens
                if (exportData.Tokens != null)
                {
                    foreach (var token in exportData.Tokens)
                    {
                        _generatedTokens.Add(token);
                        result.TokensImported++;
                    }
                }

                // Importar templates
                if (exportData.ClaimsTemplates != null)
                {
                    foreach (var template in exportData.ClaimsTemplates)
                    {
                        _claimsTemplates.Add(template);
                        result.TemplatesImported++;
                    }
                }

                result.Success = true;
                result.Message =
                    $"Import completed successfully. Tokens: {result.TokensImported}, Templates: {result.TemplatesImported}";

                return result;
            }
            catch (JsonException ex)
            {
                result.Success = false;
                result.Message = $"Invalid JSON format: {ex.Message}";
                result.Errors.Add(ex.Message);
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Import failed: {ex.Message}";
                result.Errors.Add(ex.Message);
                return result;
            }
        }

        public List<TokenData> GetGeneratedTokens()
        {
            return _generatedTokens.ToList();
        }

        public List<ClaimsTemplate> GetClaimsTemplates()
        {
            return _claimsTemplates.ToList();
        }

        public ClaimsTemplate? GetClaimsTemplate(string name)
        {
            return _claimsTemplates.FirstOrDefault(t =>
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            );
        }

        public void ClearTokens()
        {
            _generatedTokens.Clear();
        }

        public void ClearTemplates()
        {
            _claimsTemplates.Clear();
        }

        public string GenerateDefaultExportPath()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(_defaultExportPath, $"jwt_export_{timestamp}.json");
        }

        public List<string> GetAvailableExportFiles()
        {
            if (!Directory.Exists(_defaultExportPath))
            {
                return new List<string>();
            }

            return Directory
                .GetFiles(_defaultExportPath, "*.json")
                .Select(Path.GetFileName)
                .Where(name => name != null)
                .Cast<string>()
                .OrderByDescending(name => name)
                .ToList();
        }
    }
}
