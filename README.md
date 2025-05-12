# SimplePasswordManager

## Overview
SimplePasswordManager is a lightweight, console-based password manager written in C#. It provides a secure way to store and manage login credentials using AES-256 encryption. The application is designed to be simple, user-friendly, and secure, with features like password encryption, recovery codes, and a minimalistic, terminal-based interface. 
I've used Grok AI to write boilerplate code and look things up in documentation, as this is my first C# project (if you don't count Unity stuff). In fact, Grok AI wrote most of this README.

## Features
- **Secure Storage**: Encrypts all sensitive data (passwords, recovery codes, and login details) using AES-256 encryption with CBC mode and PKCS7 padding.
- **Password Management**: Create, view, and delete login credentials with optional extra messages for each login.
- **Recovery Code**: Generates a recovery code for account access in case the primary password is forgotten.
- **Configurable Settings**: Supports a configuration file (`config.json`) to manage settings like debug mode and timeout.
- **Cross-Platform**: Stores data in the user's local application data directory, making it compatible across different operating systems.
- **Console Interface**: Simple menu-driven interface for easy navigation and interaction.

## Directory Structure (in local app data)
```
SimplePasswordManager/
├── config/
│   └── config.json         # Configuration file
└── resources/
    ├── passwords/
    │   ├── password        # Encrypted master password
    │   └── recovery_code   # Encrypted recovery code
    └── logins/
        └── [login_files]   # Encrypted login credential files
```

## Installation
1. **Clone the Repository**:
   ```bash
   git clone https://github.com/alpenstorm/SimplePasswordManager.git
   ```
2. **Open in Visual Studio**:
   - Open the solution file (`SimplePasswordManager.sln`) in Visual Studio.
   - Ensure you have .NET Framework or .NET Core installed (compatible with the project).
3. **Build and Run**:
   - Build the solution in Visual Studio.
   - Run the application to start the console interface.
Or you can download a pre-built binary for [Windows](https://github.com/alpenstorm/SimplePasswordManager/releases/download/v1.0.0/install.exe) and OSX (soon).

## Usage
1. **Initial Setup**:
   - On first launch, the application creates the necessary directories and configuration files.
   - You will be prompted to create a master password (minimum 8 characters).
   - A recovery code will be generated and displayed; save it securely.
2. **Main Menu**:
   - `[C]reate a new login`: Add a new login with a username, password, and optional extra messages.
   - `[O]pen a login`: View a specific login's details.
   - `[D]elete a login`: Remove a login.
   - `[V]iew all logins`: List all saved login file names.
   - `[S]ettings`: Modify application settings.
   - `[L]ock the app`: Lock the application, requiring the master password or recovery code to unlock.
   - `[Q]uit the app`: Exit the application.
3. **Unlocking**:
   - Use your master password or recovery code to unlock the application.
   - If using a recovery code, you’ll need to reset your password and receive a new recovery code.

## Security
- **Encryption**: All sensitive data is encrypted using AES-256 with a key derived from the master password or recovery code using PBKDF2 (100,000 iterations, SHA256).
- **Temporary Files**: Temporary files used during encryption/decryption are securely deleted after use.
- **Recovery Code**: A 24-character random string for emergency access, encrypted and stored securely.

## Configuration
The `config.json` file in the `config/` directory stores application settings:
```json
{
  "config": {
    "first_load": false,
    "debug_mode": false
  },
  "version": "0.0.1a"
}
```
- `first_load`: Indicates if the application is running for the first time.
- `debug_mode`: Enables debug output for troubleshooting.
- `version`: Application version.

## Development
- **Language**: C#
- **Dependencies**: .NET Framework or .NET Core, System.Security.Cryptography, System.Text.Json
- **Source Files**:
  - `program.cs`: Entry point of the application.
  - `globals.cs`: Global variables and utility methods.
  - `encryption.cs`: AES encryption and decryption logic.
  - `init.cs`: Initialization and configuration loading.
  - `loop.cs`: Main application loop and menu logic.
  - `saveFile.cs`: Data models for configuration deserialization.
All functions have been documented.

## Contributing
Contributions are welcome! Please follow these steps:
1. Fork the repository.
2. Create a new branch (`git checkout -b feature/your-feature`).
3. Make your changes, keeping code simple and documented.
4. Test your changes thoroughly.
5. Submit a pull request with a clear description of your changes.

## License
This project is licensed under the GPL 3.0 License. See the [LICENSE](LICENSE) file for details.

## Contact
For questions or feedback, please contact alpenstorm.sh1ry0n@gmail.com or open an issue on the GitHub repository.

---
*Built with security and simplicity in mind by alpenstorm (and Grok AI).*
