using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JwtValidator.Core.Models;
using JwtValidator.Core.Services;
using Microsoft.Extensions.Configuration;

class Program
{
    private static ExportImportService? _exportImportService;

    static async Task<int> Main(string[] args)
    {
        // Inicializa o serviço de export/import
        _exportImportService = new ExportImportService();

        // Se não há argumentos, executa o modo interativo
        if (args.Length == 0)
        {
            return await RunInteractiveMode();
        }

        // Se há argumentos, executa o modo de linha de comando
        return await RunCommandLineMode(args);
    }

    static async Task<int> RunInteractiveMode()
    {
        try
        {
            // Carrega configurações
            var config = LoadConfiguration("config/appsettings.json");
            var jwtSettings =
                config.GetSection("JwtSettings").Get<JwtSettings>() ?? throw new Exception(
                    "JwtSettings Not Found"
                );
            var service = new JwtService(jwtSettings);

            MostrarCabecalho();

            while (true)
            {
                MostrarMenu();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\n > Choose an option: ");
                Console.ResetColor();
                var op = Console.ReadLine();

                switch (op)
                {
                    case "1":
                        GerarToken(service);
                        break;
                    case "2":
                        ValidarToken(service);
                        break;
                    case "0":
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\nExiting...");
                        Console.ResetColor();
                        return 0;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid option.");
                        Console.ResetColor();
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    static async Task<int> RunCommandLineMode(string[] args)
    {
        var rootCommand = CreateRootCommand();
        return await rootCommand.InvokeAsync(args);
    }

    static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("JWT Validator CLI - Generate and validate JWT tokens");

        // Comando para gerar token
        var generateCommand = new Command("generate", "Generate a new JWT token")
        {
            CommandLineOptions.SubjectOption,
            CommandLineOptions.ClaimsOption,
            CommandLineOptions.OutputOption,
            CommandLineOptions.VerboseOption,
            CommandLineOptions.ConfigFileOption,
        };

        generateCommand.SetHandler(
            async (
                string subject,
                string? claims,
                string? outputFile,
                bool verbose,
                string configFile
            ) =>
            {
                await HandleGenerateCommand(subject, claims, outputFile, verbose, configFile);
            },
            CommandLineOptions.SubjectOption,
            CommandLineOptions.ClaimsOption,
            CommandLineOptions.OutputOption,
            CommandLineOptions.VerboseOption,
            CommandLineOptions.ConfigFileOption
        );

        // Comando para validar token
        var validateCommand = new Command("validate", "Validate a JWT token")
        {
            CommandLineOptions.TokenOption,
            CommandLineOptions.PrettyPrintOption,
            CommandLineOptions.VerboseOption,
            CommandLineOptions.ConfigFileOption,
        };

        validateCommand.SetHandler(
            async (string token, bool prettyPrint, bool verbose, string configFile) =>
            {
                await HandleValidateCommand(token, prettyPrint, verbose, configFile);
            },
            CommandLineOptions.TokenOption,
            CommandLineOptions.PrettyPrintOption,
            CommandLineOptions.VerboseOption,
            CommandLineOptions.ConfigFileOption
        );

        // Comando para exportar dados
        var exportCommand = new Command("export", "Export tokens, templates, and settings")
        {
            ExportImportCommandLineOptions.ExportFileOption,
            ExportImportCommandLineOptions.IncludeTokensOption,
            ExportImportCommandLineOptions.IncludeTemplatesOption,
            ExportImportCommandLineOptions.IncludeSettingsOption,
            CommandLineOptions.VerboseOption,
            CommandLineOptions.ConfigFileOption,
        };

        exportCommand.SetHandler(
            async (
                string? exportFile,
                bool includeTokens,
                bool includeTemplates,
                bool includeSettings,
                bool verbose,
                string configFile
            ) =>
            {
                await HandleExportCommand(
                    exportFile,
                    includeTokens,
                    includeTemplates,
                    includeSettings,
                    verbose,
                    configFile
                );
            },
            ExportImportCommandLineOptions.ExportFileOption,
            ExportImportCommandLineOptions.IncludeTokensOption,
            ExportImportCommandLineOptions.IncludeTemplatesOption,
            ExportImportCommandLineOptions.IncludeSettingsOption,
            CommandLineOptions.VerboseOption,
            CommandLineOptions.ConfigFileOption
        );

        // Comando para importar dados
        var importCommand = new Command("import", "Import tokens, templates, and settings")
        {
            ExportImportCommandLineOptions.ImportFileOption,
            CommandLineOptions.VerboseOption,
        };

        importCommand.SetHandler(
            async (string? importFile, bool verbose) =>
            {
                await HandleImportCommand(importFile, verbose);
            },
            ExportImportCommandLineOptions.ImportFileOption,
            CommandLineOptions.VerboseOption
        );

        // Comando para criar template
        var createTemplateCommand = new Command("create-template", "Create a claims template")
        {
            ExportImportCommandLineOptions.TemplateNameOption,
            ExportImportCommandLineOptions.TemplateDescriptionOption,
            CommandLineOptions.ClaimsOption,
            CommandLineOptions.VerboseOption,
        };

        createTemplateCommand.SetHandler(
            async (
                string? templateName,
                string? templateDescription,
                string? claims,
                bool verbose
            ) =>
            {
                await HandleCreateTemplateCommand(
                    templateName,
                    templateDescription,
                    claims,
                    verbose
                );
            },
            ExportImportCommandLineOptions.TemplateNameOption,
            ExportImportCommandLineOptions.TemplateDescriptionOption,
            CommandLineOptions.ClaimsOption,
            CommandLineOptions.VerboseOption
        );

        // Comando para listar dados
        var listCommand = new Command("list", "List tokens, templates, or exports")
        {
            new Option<string>("--type", "Type to list: tokens, templates, exports").FromAmong(
                "tokens",
                "templates",
                "exports"
            ),
            CommandLineOptions.VerboseOption,
        };

        listCommand.SetHandler(
            async (string? type, bool verbose) =>
            {
                await HandleListCommand(type, verbose);
            },
            new Option<string>("--type", "Type to list: tokens, templates, exports").FromAmong(
                "tokens",
                "templates",
                "exports"
            ),
            CommandLineOptions.VerboseOption
        );

        rootCommand.AddCommand(generateCommand);
        rootCommand.AddCommand(validateCommand);
        rootCommand.AddCommand(exportCommand);
        rootCommand.AddCommand(importCommand);
        rootCommand.AddCommand(createTemplateCommand);
        rootCommand.AddCommand(listCommand);

        return rootCommand;
    }

    static IConfiguration LoadConfiguration(string configFile)
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configFile, optional: false)
            .AddEnvironmentVariables()
            .Build();
    }

    static async Task HandleGenerateCommand(
        string subject,
        string? claims,
        string? outputFile,
        bool verbose,
        string configFile
    )
    {
        try
        {
            if (verbose)
            {
                Console.WriteLine($"Loading configuration from: {configFile}");
                Console.WriteLine($"Subject: {subject}");
                if (!string.IsNullOrEmpty(claims))
                    Console.WriteLine($"Additional claims: {claims}");
                if (!string.IsNullOrEmpty(outputFile))
                    Console.WriteLine($"Output file: {outputFile}");
            }

            var config = LoadConfiguration(configFile);
            var jwtSettings =
                config.GetSection("JwtSettings").Get<JwtSettings>() ?? throw new Exception(
                    "JwtSettings Not Found"
                );
            var service = new JwtService(jwtSettings);

            // Parse claims se fornecidas
            Dictionary<string, string>? extraClaims = null;
            if (!string.IsNullOrEmpty(claims))
            {
                try
                {
                    extraClaims = JsonSerializer.Deserialize<Dictionary<string, string>>(claims);
                }
                catch (JsonException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(
                        "Error: Invalid JSON format for claims. Please use format like: {\"role\":\"admin\",\"permissions\":\"read\"}"
                    );
                    Console.ResetColor();
                    return;
                }
            }

            if (verbose)
                Console.WriteLine("Generating token...");

            var token = service.GenerateToken(subject, extraClaims);

            // Salvar no serviço de export/import
            _exportImportService?.AddGeneratedToken(
                subject,
                extraClaims ?? new Dictionary<string, string>(),
                token
            );

            // Salvar em arquivo se especificado
            if (!string.IsNullOrEmpty(outputFile))
            {
                await File.WriteAllTextAsync(outputFile, token);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Token generated and saved to: {outputFile}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Token generated successfully:");
                Console.ResetColor();
                Console.WriteLine(token);
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static async Task HandleValidateCommand(
        string token,
        bool prettyPrint,
        bool verbose,
        string configFile
    )
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(
                    "Error: Token is required for validation. Use --token or -t option."
                );
                Console.ResetColor();
                return;
            }

            if (verbose)
            {
                Console.WriteLine($"Loading configuration from: {configFile}");
                Console.WriteLine("Validating token...");
            }

            var config = LoadConfiguration(configFile);
            var jwtSettings =
                config.GetSection("JwtSettings").Get<JwtSettings>() ?? throw new Exception(
                    "JwtSettings Not Found"
                );
            var service = new JwtService(jwtSettings);

            var result = service.ValidateToken(token);

            if (result.IsValid)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Token is valid!");
                Console.ResetColor();

                if (prettyPrint)
                {
                    Console.WriteLine("\n📋 Claims:");
                    Console.WriteLine(new string('-', 50));
                    foreach (var claim in result.Principal!.Claims)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write($"{claim.Type}: ");
                        Console.ResetColor();
                        Console.WriteLine(claim.Value);
                    }
                    Console.WriteLine(new string('-', 50));
                }
                else
                {
                    Console.WriteLine("Claims found:");
                    foreach (var claim in result.Principal!.Claims)
                    {
                        Console.WriteLine($" - {claim.Type}: {claim.Value}");
                    }
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Token is invalid: {result.Error}");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static async Task HandleExportCommand(
        string? exportFile,
        bool includeTokens,
        bool includeTemplates,
        bool includeSettings,
        bool verbose,
        string configFile
    )
    {
        try
        {
            if (_exportImportService == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Export/Import service not initialized.");
                Console.ResetColor();
                return;
            }

            // Determinar arquivo de export
            var filePath = exportFile ?? _exportImportService.GenerateDefaultExportPath();

            JwtSettings? jwtSettings = null;
            if (includeSettings)
            {
                var config = LoadConfiguration(configFile);
                jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>();
            }

            if (verbose)
            {
                Console.WriteLine($"Exporting to: {filePath}");
                Console.WriteLine($"Include tokens: {includeTokens}");
                Console.WriteLine($"Include templates: {includeTemplates}");
                Console.WriteLine($"Include settings: {includeSettings}");
            }

            var result = await _exportImportService.ExportToFileAsync(
                filePath,
                jwtSettings,
                includeTokens,
                includeTemplates,
                includeSettings
            );

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ {result}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static async Task HandleImportCommand(string? importFile, bool verbose)
    {
        try
        {
            if (_exportImportService == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Export/Import service not initialized.");
                Console.ResetColor();
                return;
            }

            if (string.IsNullOrEmpty(importFile))
            {
                var availableFiles = _exportImportService.GetAvailableExportFiles();
                if (availableFiles.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No export files found in exports directory.");
                    Console.ResetColor();
                    return;
                }

                Console.WriteLine("Available export files:");
                for (int i = 0; i < availableFiles.Count; i++)
                {
                    Console.WriteLine($"  [{i + 1}] {availableFiles[i]}");
                }

                Console.Write("Select file number or enter file path: ");
                var input = Console.ReadLine();

                if (
                    int.TryParse(input, out int fileIndex)
                    && fileIndex > 0
                    && fileIndex <= availableFiles.Count
                )
                {
                    importFile = Path.Combine("exports", availableFiles[fileIndex - 1]);
                }
                else if (!string.IsNullOrEmpty(input))
                {
                    importFile = input;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid selection.");
                    Console.ResetColor();
                    return;
                }
            }

            if (verbose)
            {
                Console.WriteLine($"Importing from: {importFile}");
            }

            var result = await _exportImportService.ImportFromFileAsync(importFile);

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ {result.Message}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ {result.Message}");
                Console.ResetColor();

                if (result.Errors.Count > 0)
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"  - {error}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static async Task HandleCreateTemplateCommand(
        string? templateName,
        string? templateDescription,
        string? claims,
        bool verbose
    )
    {
        try
        {
            if (_exportImportService == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Export/Import service not initialized.");
                Console.ResetColor();
                return;
            }

            if (string.IsNullOrEmpty(templateName))
            {
                Console.Write("Enter template name: ");
                templateName = Console.ReadLine();
                if (string.IsNullOrEmpty(templateName))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Template name is required.");
                    Console.ResetColor();
                    return;
                }
            }

            if (string.IsNullOrEmpty(templateDescription))
            {
                Console.Write("Enter template description: ");
                templateDescription = Console.ReadLine() ?? "";
            }

            Dictionary<string, string>? claimsDict = null;
            if (!string.IsNullOrEmpty(claims))
            {
                try
                {
                    claimsDict = JsonSerializer.Deserialize<Dictionary<string, string>>(claims);
                }
                catch (JsonException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Invalid JSON format for claims.");
                    Console.ResetColor();
                    return;
                }
            }

            if (claimsDict == null || claimsDict.Count == 0)
            {
                Console.WriteLine(
                    "Enter claims (key=value format, one per line, empty line to finish):"
                );
                claimsDict = new Dictionary<string, string>();

                while (true)
                {
                    Console.Write("> ");
                    var claimInput = Console.ReadLine();
                    if (string.IsNullOrEmpty(claimInput))
                        break;

                    var parts = claimInput.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        claimsDict[parts[0].Trim()] = parts[1].Trim();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Invalid format. Use: key=value");
                        Console.ResetColor();
                    }
                }
            }

            _exportImportService.AddClaimsTemplate(templateName, templateDescription, claimsDict);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(
                $"✅ Template '{templateName}' created successfully with {claimsDict.Count} claims."
            );
            Console.ResetColor();

            if (verbose)
            {
                Console.WriteLine("\nTemplate details:");
                Console.WriteLine($"Name: {templateName}");
                Console.WriteLine($"Description: {templateDescription}");
                Console.WriteLine("Claims:");
                foreach (var claim in claimsDict)
                {
                    Console.WriteLine($"  - {claim.Key}: {claim.Value}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static async Task HandleListCommand(string? type, bool verbose)
    {
        try
        {
            if (_exportImportService == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Export/Import service not initialized.");
                Console.ResetColor();
                return;
            }

            switch (type?.ToLower())
            {
                case "tokens":
                    var tokens = _exportImportService.GetGeneratedTokens();
                    Console.WriteLine($"📋 Generated Tokens ({tokens.Count}):");
                    Console.WriteLine(new string('-', 50));

                    foreach (var token in tokens)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"Subject: {token.Subject}");
                        Console.ResetColor();
                        Console.WriteLine($"Generated: {token.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
                        if (!string.IsNullOrEmpty(token.Description))
                            Console.WriteLine($"Description: {token.Description}");
                        if (token.Claims.Count > 0)
                        {
                            Console.WriteLine("Claims:");
                            foreach (var claim in token.Claims)
                            {
                                Console.WriteLine($"  - {claim.Key}: {claim.Value}");
                            }
                        }
                        Console.WriteLine(new string('-', 30));
                    }
                    break;

                case "templates":
                    var templates = _exportImportService.GetClaimsTemplates();
                    Console.WriteLine($"📝 Claims Templates ({templates.Count}):");
                    Console.WriteLine(new string('-', 50));

                    foreach (var template in templates)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"Name: {template.Name}");
                        Console.ResetColor();
                        Console.WriteLine($"Description: {template.Description}");
                        Console.WriteLine($"Created: {template.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine($"Claims ({template.Claims.Count}):");
                        foreach (var claim in template.Claims)
                        {
                            Console.WriteLine($"  - {claim.Key}: {claim.Value}");
                        }
                        Console.WriteLine(new string('-', 30));
                    }
                    break;

                case "exports":
                    var exportFiles = _exportImportService.GetAvailableExportFiles();
                    Console.WriteLine($"📁 Export Files ({exportFiles.Count}):");
                    Console.WriteLine(new string('-', 50));

                    foreach (var file in exportFiles)
                    {
                        var filePath = Path.Combine("exports", file);
                        var fileInfo = new FileInfo(filePath);
                        Console.WriteLine($"📄 {file}");
                        Console.WriteLine($"   Size: {fileInfo.Length} bytes");
                        Console.WriteLine(
                            $"   Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}"
                        );
                        Console.WriteLine();
                    }
                    break;

                default:
                    Console.WriteLine("Available list types:");
                    Console.WriteLine("  tokens    - List generated tokens");
                    Console.WriteLine("  templates - List claims templates");
                    Console.WriteLine("  exports   - List export files");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static void MostrarCabecalho()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(
            @"
     ____.          __    ____   ____      .__  .__    .___       __           ___.              _____          __                         
    |    |_  _  ___/  |_  \   \ /   /____  |  | |__| __| _/____ _/  |_  ____   \_ |__ ___.__.   /  _  \   _____/  |_____________  ________ 
    |    \ \/ \/ /\   __\  \   Y   /\__  \ |  | |  |/ __ |\__  \\   __\/ __ \   | __ <   |  |  /  /_\  \ /    \   __\_  __ \__  \ \___   / 
/\__|    |\     /  |  |     \     /  / __ \|  |_|  / /_/ | / __ \|  | \  ___/   | \_\ \___  | /    |    \   |  \  |  |  | \// __ \_/    /  
\________| \/\_/   |__|      \___/  (____  /____/__\____ |(____  /__|  \___  >  |___  / ____| \____|__  /___|  /__|  |__|  (____  /_____ \ 
                                         \/             \/     \/          \/       \/\/              \/     \/                 \/      \/ 
        "
        );
        Console.ResetColor();

        MostrarTitulo("JWT Validator CLI");
    }

    static void MostrarTitulo(string titulo)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(new string('-', titulo.Length + 4));
        Console.WriteLine($"| {titulo} |");
        Console.WriteLine(new string('-', titulo.Length + 4));
        Console.ResetColor();
    }

    static void MostrarMenu()
    {
        Console.WriteLine("\n============= MENU =============");
        Console.WriteLine(" [1] Generate new JWT token");
        Console.WriteLine(" [2] Validate existing token");
        Console.WriteLine(" [0] Exit");
        Console.WriteLine("================================");
    }

    static void GerarToken(JwtService service)
    {
        MostrarTitulo("Generate New Token");

        Console.Write("\nEnter the subject (ex: email or ID): ");
        var subject = Console.ReadLine() ?? "default";

        var claims = new Dictionary<string, string> { { "role", "user" } };

        Loading("Generating token");

        var token = service.GenerateToken(subject, claims);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nToken generated successfully:\n");
        Console.ResetColor();

        Console.WriteLine(token);

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("\nYou can paste this token in https://jwt.io to inspect.");
        Console.ResetColor();
    }

    static void ValidarToken(JwtService service)
    {
        MostrarTitulo("Validate JWT Token");

        Console.Write("\nEnter the token to be validated:\n> ");
        var token = Console.ReadLine() ?? "";

        Loading("Validating token");

        var result = service.ValidateToken(token);

        if (result.IsValid)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✅ Token valid! Claims found:");
            Console.ResetColor();

            foreach (var claim in result.Principal!.Claims)
            {
                Console.WriteLine($" - {claim.Type}: {claim.Value}");
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Token invalid: {result.Error}");
            Console.ResetColor();
        }
    }

    static void Loading(string mensagem = "Processing", int pontos = 3)
    {
        Console.Write(mensagem);
        for (int i = 0; i < pontos; i++)
        {
            Console.Write(".");
            Thread.Sleep(300);
        }
        Console.WriteLine();
    }
}
