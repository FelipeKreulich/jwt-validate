# 🛡️ JWT Validator CLI

![JWT CLI Screenshot](assets/jwt-validate.png)

A simple **C# console application** that allows you to **generate and
validate JWT tokens** using configuration values from an
`appsettings.json` file.\
This tool is useful for developers who want to test JWTs locally and
understand how claims and validation work.

------------------------------------------------------------------------

## 🚀 Features

-   Generate JWT tokens with custom claims\
-   Validate and inspect existing JWT tokens\
-   Configurable secret, issuer, audience, and expiration time\
-   Visual feedback with console animations\
-   Fully self-contained CLI app

------------------------------------------------------------------------

## ⚙️ Configuration

The application reads settings from the `appsettings.json` file:

``` json
{
  "JwtSettings": {
    "Secret": "your-secret-key-here",
    "Issuer": "Api",
    "Audience": "Api",
    "ExpiryMinutes": 60
  }
}
```

  -----------------------------------------------------------------------
  Field                    Description
  ------------------------ ----------------------------------------------
  **Secret**               The secret key used to sign and validate JWT
                           tokens (Base64 or string).

  **Issuer**               The issuer of the token.

  **Audience**             The intended audience of the token.

  **ExpiryMinutes**        How long the token remains valid (in minutes).
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

When you start the program, you'll see a menu like this:

    ============= MENU =============
     [1] Generate new JWT token
     [2] Validate existing token
     [0] Exit
    ================================

### 🔑 Generate a Token

Choose option `1` and enter a subject (e.g., an email or user ID).\
The program will generate a new JWT token and display it in the console.

You can copy the token and paste it into <https://jwt.io> to inspect its
contents.

### 🧾 Validate a Token

Choose option `2` and paste an existing token.\
The app will validate it and display whether it's valid, along with its
claims.

------------------------------------------------------------------------

## 🧱 Project Structure

    .
    ├── Program.cs
    ├── JwtService.cs
    ├── appsettings.json
    └── README.md

------------------------------------------------------------------------

## 📦 Dependencies

-   `Microsoft.Extensions.Configuration`
-   `Microsoft.Extensions.Configuration.Json`
-   `Microsoft.IdentityModel.Tokens`
-   `System.IdentityModel.Tokens.Jwt`

Install them using NuGet if needed:

``` bash
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.IdentityModel.Tokens
dotnet add package System.IdentityModel.Tokens.Jwt
```

------------------------------------------------------------------------

## 🧠 Author

Created by **Felipe** 💻\
A simple yet educational project for learning and testing JWT
authentication logic in C#.

------------------------------------------------------------------------

## 📜 License

This project is licensed under the **MIT License**.
