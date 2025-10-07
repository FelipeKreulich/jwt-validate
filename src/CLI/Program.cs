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
using Spectre.Console;

class Program
{
    private static ExportImportService? _exportImportService;

    static async Task<int> Main(string[] args)
    {
        _exportImportService = new ExportImportService();

        if (args.Length == 0)
        {
            return await RunInteractiveMode();
        }

        return await RunCommandLineMode(args);
    }

    static async Task<int> RunInteractiveMode()
    {
        try
        {
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

                var op = AnsiConsole.Ask<string>("\n[cyan]Choose an option:[/]");

                switch (op)
                {
                    case "1":
                        GerarToken(service);
                        break;
                    case "2":
                        ValidarToken(service);
                        break;
                    case "3":
                        GerarTokenComTemplate(service);
                        break;
                    case "4":
                        GerenciarTemplates();
                        break;
                    case "0":
                        AnsiConsole.MarkupLine("\n[yellow]Exiting...[/]");
                        return 0;
                    default:
                        AnsiConsole.MarkupLine("[red]Invalid option.[/]");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
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

            _exportImportService?.AddGeneratedToken(
                subject,
                extraClaims ?? new Dictionary<string, string>(),
                token
            );

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
        AnsiConsole.Clear();
        
        var figlet = new FigletText("JWT Validator")
            .Centered()
            .Color(Color.Green);
        AnsiConsole.Write(figlet);
        
        var rule = new Rule("[bold blue]JWT Token Generator & Validator CLI[/]")
            .RuleStyle("grey");
        AnsiConsole.Write(rule);
        
        AnsiConsole.WriteLine();
    }

    static void MostrarTitulo(string titulo)
    {
        var panel = new Panel($"[bold cyan]{titulo}[/]")
            .BorderColor(Color.Blue)
            .RoundedBorder();
        AnsiConsole.Write(panel);
    }

    static void MostrarMenu()
    {
        var table = new Table()
            .BorderColor(Color.Grey)
            .Border(TableBorder.Rounded)
            .Title("[bold yellow]MAIN MENU[/]");
        
        table.AddColumn("[bold blue]Option[/]");
        table.AddColumn("[bold green]Description[/]");
        
        table.AddRow("[cyan]1[/]", "Generate new JWT token");
        table.AddRow("[cyan]2[/]", "Validate existing token");
        table.AddRow("[cyan]3[/]", "Generate token from template");
        table.AddRow("[cyan]4[/]", "Manage claims templates");
        table.AddRow("[red]0[/]", "Exit");
        
        AnsiConsole.Write(table);
    }

    static void GerarToken(JwtService service)
    {
        MostrarTitulo("Generate New Token");

        Console.Write("\nEnter the subject (ex: email or ID): ");
        var subject = Console.ReadLine() ?? "default";

        var claims = new Dictionary<string, string>();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n📝 Add custom claims (optional):");
        Console.ResetColor();
        Console.WriteLine("Enter claims in key=value format, one per line.");
        Console.WriteLine("Press Enter on empty line to finish.");
        Console.WriteLine("Examples: role=admin, department=IT, permissions=read,write\n");

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("> ");
            Console.ResetColor();
            var claimInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(claimInput))
                break;

            var parts = claimInput.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    claims[key] = value;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✅ Added: {key} = {value}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  ❌ Key and value cannot be empty");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  ❌ Invalid format. Use: key=value");
                Console.ResetColor();
            }
        }

        if (claims.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n📋 Claims to be included ({claims.Count}):");
            Console.ResetColor();
            foreach (var claim in claims)
            {
                Console.WriteLine($"  • {claim.Key}: {claim.Value}");
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(
                "\n⚠️  No custom claims added. Token will contain only standard claims."
            );
            Console.ResetColor();
        }

        Loading("Generating token");

        var token = service.GenerateToken(subject, claims.Count > 0 ? claims : null);

        _exportImportService?.AddGeneratedToken(subject, claims, token);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n🎉 Token generated successfully:\n");
        Console.ResetColor();

        Console.WriteLine(token);

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("\n💡 You can paste this token in https://jwt.io to inspect.");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("\n💾 Save token to file? (y/N): ");
        Console.ResetColor();
        var saveOption = Console.ReadLine()?.ToLower();

        if (saveOption == "y" || saveOption == "yes")
        {
            Console.Write("Enter filename (without extension): ");
            var filename = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(filename))
            {
                var filepath = $"{filename}.txt";
                try
                {
                    File.WriteAllText(filepath, token);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✅ Token saved to: {filepath}");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ Error saving file: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }
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

    static void GerarTokenComTemplate(JwtService service)
    {
        MostrarTitulo("Generate Token from Template");

        if (_exportImportService == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Export/Import service not available.");
            Console.ResetColor();
            return;
        }

        var templates = _exportImportService.GetClaimsTemplates();
        if (templates.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n⚠️  No templates available. Create one first using option 4.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine("\n📝 Available templates:");
        for (int i = 0; i < templates.Count; i++)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  [{i + 1}] {templates[i].Name}");
            Console.ResetColor();
            Console.WriteLine($"      {templates[i].Description}");
            Console.WriteLine($"      Claims: {string.Join(", ", templates[i].Claims.Keys)}");
        }

        Console.Write("\nSelect template number: ");
        var input = Console.ReadLine();

        if (
            !int.TryParse(input, out int templateIndex)
            || templateIndex < 1
            || templateIndex > templates.Count
        )
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid template selection.");
            Console.ResetColor();
            return;
        }

        var selectedTemplate = templates[templateIndex - 1];

        Console.Write("\nEnter the subject (ex: email or ID): ");
        var subject = Console.ReadLine() ?? "default";

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n📋 Using template '{selectedTemplate.Name}' with claims:");
        Console.ResetColor();
        foreach (var claim in selectedTemplate.Claims)
        {
            Console.WriteLine($"  • {claim.Key}: {claim.Value}");
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("\n➕ Add additional claims? (y/N): ");
        Console.ResetColor();
        var addMore = Console.ReadLine()?.ToLower();

        var finalClaims = new Dictionary<string, string>(selectedTemplate.Claims);

        if (addMore == "y" || addMore == "yes")
        {
            Console.WriteLine(
                "\nEnter additional claims (key=value format, empty line to finish):"
            );
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("> ");
                Console.ResetColor();
                var claimInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(claimInput))
                    break;

                var parts = claimInput.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        finalClaims[key] = value;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  ✅ Added: {key} = {value}");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  ❌ Invalid format. Use: key=value");
                    Console.ResetColor();
                }
            }
        }

        Loading("Generating token");

        var token = service.GenerateToken(subject, finalClaims);
        _exportImportService?.AddGeneratedToken(subject, finalClaims, token);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n🎉 Token generated successfully:\n");
        Console.ResetColor();
        Console.WriteLine(token);
    }

    static void GerenciarTemplates()
    {
        MostrarTitulo("Manage Claims Templates");

        if (_exportImportService == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Export/Import service not available.");
            Console.ResetColor();
            return;
        }

        while (true)
        {
            Console.WriteLine("\n========== TEMPLATE MENU ==========");
            Console.WriteLine(" [1] Create new template");
            Console.WriteLine(" [2] List existing templates");
            Console.WriteLine(" [3] View template details");
            Console.WriteLine(" [0] Back to main menu");
            Console.WriteLine("===================================");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\n > Choose an option: ");
            Console.ResetColor();
            var op = Console.ReadLine();

            switch (op)
            {
                case "1":
                    CriarTemplate();
                    break;
                case "2":
                    ListarTemplates();
                    break;
                case "3":
                    VisualizarTemplate();
                    break;
                case "0":
                    return;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid option.");
                    Console.ResetColor();
                    break;
            }
        }
    }

    static void CriarTemplate()
    {
        Console.WriteLine("\n📝 Create New Claims Template");
        Console.WriteLine(new string('-', 35));

        Console.Write("Template name: ");
        var name = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Template name is required.");
            Console.ResetColor();
            return;
        }

        Console.Write("Template description: ");
        var description = Console.ReadLine() ?? "";

        var claims = new Dictionary<string, string>();
        Console.WriteLine("\nEnter claims (key=value format, empty line to finish):");

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("> ");
            Console.ResetColor();
            var claimInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(claimInput))
                break;

            var parts = claimInput.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    claims[key] = value;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✅ Added: {key} = {value}");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  ❌ Invalid format. Use: key=value");
                Console.ResetColor();
            }
        }

        if (claims.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No claims added. Template not created.");
            Console.ResetColor();
            return;
        }

        _exportImportService?.AddClaimsTemplate(name, description, claims);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(
            $"\n✅ Template '{name}' created successfully with {claims.Count} claims!"
        );
        Console.ResetColor();
    }

    static void ListarTemplates()
    {
        var templates = _exportImportService?.GetClaimsTemplates() ?? new List<ClaimsTemplate>();

        Console.WriteLine($"\n📝 Available Templates ({templates.Count}):");
        Console.WriteLine(new string('-', 40));

        if (templates.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No templates found.");
            Console.ResetColor();
            return;
        }

        for (int i = 0; i < templates.Count; i++)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{i + 1}] {templates[i].Name}");
            Console.ResetColor();
            Console.WriteLine($"    {templates[i].Description}");
            Console.WriteLine($"    Claims: {templates[i].Claims.Count}");
            Console.WriteLine($"    Created: {templates[i].CreatedAt:yyyy-MM-dd HH:mm}");
            Console.WriteLine();
        }
    }

    static void VisualizarTemplate()
    {
        var templates = _exportImportService?.GetClaimsTemplates() ?? new List<ClaimsTemplate>();

        if (templates.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nNo templates available.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine("\n📋 Select template to view:");
        for (int i = 0; i < templates.Count; i++)
        {
            Console.WriteLine($"  [{i + 1}] {templates[i].Name}");
        }

        Console.Write("\nTemplate number: ");
        var input = Console.ReadLine();

        if (
            !int.TryParse(input, out int templateIndex)
            || templateIndex < 1
            || templateIndex > templates.Count
        )
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid template selection.");
            Console.ResetColor();
            return;
        }

        var template = templates[templateIndex - 1];

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n📝 Template: {template.Name}");
        Console.ResetColor();
        Console.WriteLine(new string('-', template.Name.Length + 12));
        Console.WriteLine($"Description: {template.Description}");
        Console.WriteLine($"Created: {template.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"\nClaims ({template.Claims.Count}):");

        foreach (var claim in template.Claims)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"  • {claim.Key}: ");
            Console.ResetColor();
            Console.WriteLine(claim.Value);
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
