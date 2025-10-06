using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JwtValidator;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task<int> Main(string[] args)
    {
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
            var config = LoadConfiguration("appsettings.json");
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

        rootCommand.AddCommand(generateCommand);
        rootCommand.AddCommand(validateCommand);

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
