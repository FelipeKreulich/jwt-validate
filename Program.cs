using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;

class Program
{
    static void Main(string[] args)
    {
        // Carrega configurações
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

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
                    return;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid option.");
                    Console.ResetColor();
                    break;
            }
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
