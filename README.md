# 🛡️ JWT Validator CLI

![JWT CLI Screenshot](assets/jwt-validate.png)

A simple **C# console application** that allows you to **generate and
validate JWT tokens** using configuration values from an
`appsettings.json` file.\
This tool is useful for developers who want to test JWTs locally and
understand how claims and validation work.

------------------------------------------------------------------------

## 🚀 Features

-   **Interactive Mode**: User-friendly menu-driven interface for JWT operations
-   **Command Line Mode**: Generate and validate tokens via command-line flags
-   **Multiple Signature Algorithms**: Support for HS256, HS512, and RS256 algorithms
-   **Generate JWT tokens** with custom claims and subjects
-   **Validate and inspect** existing JWT tokens with detailed claim information
-   **Configurable settings**: secret, issuer, audience, expiration time, and algorithm
-   **Visual feedback** with console animations and colored output
-   **File output**: Save generated tokens to files
-   **Verbose mode**: Detailed logging and information display
-   **Fully self-contained CLI app**

------------------------------------------------------------------------

## ⚙️ Configuration

The application reads settings from the `appsettings.json` file:

``` json
{
  "JwtSettings": {
    "Secret": "your-secret-key-here",
    "Issuer": "Api",
    "Audience": "Api",
    "ExpiryMinutes": 60,
    "Algorithm": "HS256",
    "PrivateKeyPath": "private.pem",
    "PublicKeyPath": "public.pem"
  }
}
```

  -----------------------------------------------------------------------
  Field                    Description
  ------------------------ ----------------------------------------------
  **Secret**               The secret key used to sign and validate JWT
                           tokens (Base64 or string). Required for HS256/HS512.

  **Issuer**               The issuer of the token.

  **Audience**             The intended audience of the token.

  **ExpiryMinutes**        How long the token remains valid (in minutes).

  **Algorithm**            Signature algorithm: HS256, HS512, or RS256.

  **PrivateKeyPath**       Path to RSA private key file (required for RS256).

  **PublicKeyPath**        Path to RSA public key file (required for RS256).
  -----------------------------------------------------------------------

------------------------------------------------------------------------

## 🧩 How to Run

1.  **Clone or download** this project.\

2.  Open a terminal in the project folder.\

3.  Make sure you have the **.NET SDK** installed.\
    You can verify it with:

    ``` bash
    dotnet --version
    ```

4.  Build and run the project:

    ``` bash
    dotnet run
    ```

------------------------------------------------------------------------

## 🕹️ Usage

The JWT Validator CLI supports two modes of operation:

### 📱 Interactive Mode

When you run the program without arguments, you'll see an interactive menu:

``` bash
dotnet run
```

    ============= MENU =============
     [1] Generate new JWT token
     [2] Validate existing token
     [0] Exit
    ================================

#### 🔑 Generate a Token

Choose option `1` and enter a subject (e.g., an email or user ID).\
The program will generate a new JWT token and display it in the console.

#### 🧾 Validate a Token

Choose option `2` and paste an existing token.\
The app will validate it and display whether it's valid, along with its claims.

### 💻 Command Line Mode

For automation and scripting, use command-line flags:

#### Generate Token

``` bash
# Basic token generation
dotnet run -- generate --subject "user@example.com"

# With custom claims
dotnet run -- generate --subject "admin@example.com" --claims '{"role":"admin","permissions":"read,write"}'

# Save to file
dotnet run -- generate --subject "user@example.com" --output "token.txt"

# Verbose output
dotnet run -- generate --subject "user@example.com" --verbose
```

#### Validate Token

``` bash
# Basic validation
dotnet run -- validate --token "your-jwt-token-here"

# Pretty print claims
dotnet run -- validate --token "your-jwt-token-here" --pretty

# Verbose validation
dotnet run -- validate --token "your-jwt-token-here" --verbose --pretty
```

#### Command Options

| Option | Short | Description |
|--------|-------|-------------|
| `--subject` | `-s` | Subject for the JWT token (e.g., email or user ID) |
| `--claims` | `-c` | Additional claims in JSON format |
| `--token` | `-t` | JWT token to validate |
| `--output` | `-o` | Output file path to save the generated token |
| `--verbose` | `-v` | Enable verbose output |
| `--pretty` | `-p` | Pretty print validation results |
| `--config` | `-f` | Path to configuration file (default: appsettings.json) |

#### Help

``` bash
# Show general help
dotnet run -- --help

# Show command-specific help
dotnet run -- generate --help
dotnet run -- validate --help
```

------------------------------------------------------------------------

## 🔐 Supported Algorithms

The JWT Validator supports multiple signature algorithms:

### HMAC Algorithms
- **HS256**: HMAC with SHA-256 (default)
- **HS512**: HMAC with SHA-512

For HMAC algorithms, configure the `Secret` in your `appsettings.json`.

### RSA Algorithm
- **RS256**: RSA with SHA-256

For RS256, you need to provide RSA key files:
1. Create RSA key pair:
   ``` bash
   # Generate private key
   openssl genrsa -out private.pem 2048
   
   # Generate public key
   openssl rsa -in private.pem -pubout -out public.pem
   ```

2. Update `appsettings.json`:
   ``` json
   {
     "JwtSettings": {
       "Algorithm": "RS256",
       "PrivateKeyPath": "private.pem",
       "PublicKeyPath": "public.pem",
       "Issuer": "Api",
       "Audience": "Api",
       "ExpiryMinutes": 60
     }
   }
   ```

------------------------------------------------------------------------

## 🧱 Project Structure

    .
    ├── Program.cs              # Main application with CLI and interactive modes
    ├── JwtService.cs           # JWT generation and validation logic
    ├── CommandLineModels.cs    # Command-line argument definitions
    ├── appsettings.json        # Configuration file
    └── README.md

------------------------------------------------------------------------

## 📦 Dependencies

-   `Microsoft.Extensions.Configuration` - Configuration management
-   `Microsoft.Extensions.Configuration.Json` - JSON configuration provider
-   `Microsoft.Extensions.Configuration.Binder` - Configuration binding
-   `Microsoft.Extensions.Configuration.EnvironmentVariables` - Environment variables support
-   `Microsoft.IdentityModel.Tokens` - JWT token handling
-   `System.IdentityModel.Tokens.Jwt` - JWT token creation and validation
-   `System.CommandLine` - Command-line argument parsing

Install them using NuGet if needed:

``` bash
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.Configuration.Binder
dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables
dotnet add package Microsoft.IdentityModel.Tokens
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package System.CommandLine
```

------------------------------------------------------------------------

## 📝 Examples

### Example 1: Generate Token with HS256

``` bash
dotnet run -- generate --subject "john.doe@example.com" --verbose
```

Output:
```
Loading configuration from: appsettings.json
Subject: john.doe@example.com
Generating token...
Token generated successfully:
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJqb2huLmRvZUBleGFtcGxlLmNvbSIs...
```

### Example 2: Validate Token with Pretty Print

``` bash
dotnet run -- validate --token "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." --pretty --verbose
```

Output:
```
Loading configuration from: appsettings.json
Validating token...
✅ Token is valid!

📋 Claims:
--------------------------------------------------
http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier: john.doe@example.com
jti: 3deae1ec-20a8-48bb-9669-340d68ecafda
iat: 1759791119
nbf: 1759791119
exp: 1759794719
iss: Api
aud: Api
--------------------------------------------------
```

### Example 3: Generate Token with Custom Claims

``` bash
dotnet run -- generate --subject "admin@example.com" --claims '{"role":"admin","department":"IT","permissions":"read,write,delete"}' --output "admin-token.txt"
```

### Example 4: Batch Token Generation

``` bash
# Generate multiple tokens for testing
dotnet run -- generate --subject "user1@example.com" --output "user1-token.txt"
dotnet run -- generate --subject "user2@example.com" --output "user2-token.txt"
dotnet run -- generate --subject "admin@example.com" --claims '{"role":"admin"}' --output "admin-token.txt"
```

------------------------------------------------------------------------

## 🧠 Author

Created by **Felipe** 💻\
A simple yet educational project for learning and testing JWT
authentication logic in C#.

------------------------------------------------------------------------

## 📜 License

This project is licensed under the **MIT License**.
