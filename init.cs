using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Text.Json.Serialization.Metadata;

namespace SimplePasswordManager
{
    internal class Init
    {
        /// <summary>
        /// Handles the first-time setup of the application.
        /// </summary>
        public static void FirstLoad()
        {
            try
            {
                Globals.isFirstLoad = false;

                JsonObject j = new JsonObject
                {
                    ["config"] = new JsonObject
                    {
                        ["first_load"] = Globals.isFirstLoad,
                        ["debug_mode"] = Globals.debugMode
                    },
                    ["version"] = Globals.version,
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                };
                string jsonContent = j.ToJsonString(options);

                Console.WriteLine("Detected first load, creating initial config...");
                Console.WriteLine("Config created: ");
                Console.WriteLine(jsonContent);

                File.WriteAllText(Globals.configFile, jsonContent);
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Initial config saved to {Globals.configFile}");
                }

                Console.Write("Press any key to continue: ");
                Console.ReadKey(true);
                Globals.ClearConsole();

                Console.WriteLine("Please create a password.");
                Globals.password = CreatePasswordDialog();
                Encryption.EncryptString(Globals.password, Globals.password, Globals.passwordFile, "password");

                Globals.recoveryCode = Encryption.GenerateRandomString(24);
                Console.WriteLine("Your recovery code is: ");
                Console.WriteLine(Globals.recoveryCode);
                Console.WriteLine("Please save it somewhere you will remember to use in case you forget your password");
                Console.WriteLine("Press any key to continue... ");
                Console.ReadKey(true);

                Encryption.EncryptString(Globals.recoveryCode, Globals.recoveryCode, Globals.recoveryCodeFile, "recovery_code");
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Password and recovery code saved.");
                }

                Globals.ClearConsole();
            }
            catch (IOException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] IOException in FirstLoad: {ex.Message}");
                }
                Console.WriteLine($"Error saving initial config: {ex.Message}");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey(true);
                Environment.Exit(1);
            }
            catch (JsonException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] JsonException in FirstLoad: {ex.Message}");
                }
                Console.WriteLine($"Error creating config JSON: {ex.Message}");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey(true);
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in FirstLoad: {ex.Message}");
                }
                Console.WriteLine($"Unexpected error during setup: {ex.Message}");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey(true);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Loads the configuration from the config.json file.
        /// </summary>
        public static void LoadConfig()
        {
            try
            {
                AssignFolderLocations();
                AssignFileLocations();

                if (File.Exists(Globals.configFile))
                {
                    string jsonString = File.ReadAllText(Globals.configFile);
                    SaveFile saveFile = JsonSerializer.Deserialize<SaveFile>(jsonString);

                    Globals.isFirstLoad = saveFile.config.isFirstLoad;
                    Globals.debugMode = saveFile.config.debugMode;

                    if (Globals.debugMode)
                    {
                        Console.WriteLine($"[DEBUG] Loaded config from {Globals.configFile}: isFirstLoad={Globals.isFirstLoad}, debugMode={Globals.debugMode}");
                    }
                }
                else
                {
                    if (Globals.debugMode)
                    {
                        Console.WriteLine($"[DEBUG] Config file not found at {Globals.configFile}, will create new config.");
                    }
                }
            }
            catch (IOException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] IOException in LoadConfig: {ex.Message}");
                }
                Console.WriteLine($"Error loading config: {ex.Message}");
                throw;
            }
            catch (JsonException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] JsonException in LoadConfig: {ex.Message}");
                }
                Console.WriteLine($"Error parsing config JSON: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in LoadConfig: {ex.Message}");
                }
                Console.WriteLine($"Unexpected error loading config: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Prompts the user to create and confirm a new password.
        /// </summary>
        /// <returns>The confirmed password.</returns>
        private static string CreatePasswordDialog()
        {
            try
            {
                while (true)
                {
                    Console.Write("Enter new password: ");
                    string firstInput = Globals.ReadPassword();
                    Console.WriteLine();

                    if (string.IsNullOrEmpty(firstInput) || firstInput.Length < 8)
                    {
                        Console.WriteLine("Password is too weak, please create one at least 8 characters long.");
                        if (Globals.debugMode)
                        {
                            Console.WriteLine("[DEBUG] Password too weak in CreatePasswordDialog.");
                        }
                        continue;
                    }

                    Console.Write("Confirm password: ");
                    string secondInput = Globals.ReadPassword();
                    Console.WriteLine();

                    if (firstInput != secondInput)
                    {
                        Console.WriteLine("Passwords do not match. Please try again.");
                        if (Globals.debugMode)
                        {
                            Console.WriteLine("[DEBUG] Passwords do not match in CreatePasswordDialog.");
                        }
                        continue;
                    }

                    if (Globals.debugMode)
                    {
                        Console.WriteLine("[DEBUG] Password created successfully.");
                    }
                    return firstInput;
                }
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in CreatePasswordDialog: {ex.Message}");
                }
                Console.WriteLine($"Error creating password: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Locks the application by clearing the console and prompting for authentication.
        /// </summary>
        public static void Lock()
        {
            try
            {
                Globals.ClearConsole();
                Unlock();
                if (Globals.debugMode)
                {
                    Console.WriteLine("[DEBUG] Application locked.");
                }
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in Lock: {ex.Message}");
                }
                Console.WriteLine($"Error locking application: {ex.Message}");
                Unlock();
            }
        }

        /// <summary>
        /// Prompts the user to unlock the application using a password or recovery code.
        /// </summary>
        public static void Unlock()
        {
            try
            {
                while (true)
                {
                    ConsoleKeyInfo key = Globals.MenuBuilder("[P]assword", "[R]ecovery code");

                    if (key.Key == ConsoleKey.P)
                    {
                        Console.WriteLine();
                        Console.Write("Enter Password: ");
                        string passwordInput = Globals.ReadPassword();
                        if (VerifyPassword(passwordInput, Globals.passwordFile))
                        {
                            Globals.password = passwordInput;
                            if (Globals.debugMode)
                            {
                                Console.WriteLine("[DEBUG] Password verified successfully.");
                            }
                            Globals.ClearConsole();
                            return;
                        }
                        else
                        {
                            Console.WriteLine();
                            Console.WriteLine("Wrong password! Press any key to try again.");
                            Console.ReadKey(true);
                            if (Globals.debugMode)
                            {
                                Console.WriteLine("[DEBUG] Incorrect password entered.");
                            }
                            Globals.ClearConsole();
                            continue;
                        }
                    }
                    else if (key.Key == ConsoleKey.R)
                    {
                        Console.WriteLine();
                        Console.Write("Enter recovery code: ");
                        string recoveryInput = Globals.ReadPassword();
                        if (VerifyPassword(recoveryInput, Globals.recoveryCodeFile))
                        {
                            Console.WriteLine();
                            Console.WriteLine("You've entered a recovery code, please reset your password before continuing!");
                            Globals.password = CreatePasswordDialog();
                            Globals.recoveryCode = Encryption.GenerateRandomString(24);

                            if (File.Exists(Globals.passwordFile)) { File.Delete(Globals.passwordFile); }
                            if (File.Exists(Globals.recoveryCodeFile)) { File.Delete(Globals.recoveryCodeFile); }

                            Encryption.EncryptString(Globals.password, Globals.password, Globals.passwordFile, "password");
                            Encryption.EncryptString(Globals.recoveryCode, Globals.recoveryCode, Globals.recoveryCodeFile, "password");

                            Console.WriteLine("Your new recovery code is: ");
                            Console.WriteLine(Globals.recoveryCode);
                            Console.WriteLine("Please save it somewhere you will remember to use in case you forget your password");
                            Console.WriteLine("Press any key to continue... ");
                            Console.ReadKey(true);
                            if (Globals.debugMode)
                            {
                                Console.WriteLine("[DEBUG] Recovery code used, new password and recovery code set.");
                            }
                            Globals.ClearConsole();
                            return;
                        }
                        else
                        {
                            Console.WriteLine();
                            Console.WriteLine("Invalid recovery code! Press any key to try again.");
                            Console.ReadKey(true);
                            if (Globals.debugMode)
                            {
                                Console.WriteLine("[DEBUG] Incorrect recovery code entered.");
                            }
                            Globals.ClearConsole();
                            continue;
                        }
                    }
                    else
                    {
                        if (Globals.debugMode)
                        {
                            Console.WriteLine($"[DEBUG] Invalid unlock option selected: {key.KeyChar}");
                        }
                        Globals.ClearConsole();
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in Unlock: {ex.Message}");
                }
                Console.WriteLine($"Error unlocking application: {ex.Message}");
                Globals.ClearConsole();
                return;
            }
        }

        /// <summary>
        /// Verifies a password or recovery code against an encrypted file.
        /// </summary>
        /// <param name="passwordToCheck">The password or code to verify.</param>
        /// <param name="fileToCheck">Path to the encrypted file.</param>
        /// <returns>True if the password/code is correct, false otherwise.</returns>
        private static bool VerifyPassword(string passwordToCheck, string fileToCheck)
        {
            try
            {
                string decrypted = Encryption.DecryptString(passwordToCheck, fileToCheck);
                bool isValid = decrypted == passwordToCheck;
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] VerifyPassword result for {fileToCheck}: {isValid}");
                }
                return isValid;
            }
            catch (CryptographicException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] CryptographicException in VerifyPassword: {ex.Message}");
                }
                return false;
            }
            catch (FileNotFoundException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] FileNotFound in VerifyPassword: {ex.Message}");
                }
                return false;
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in VerifyPassword: {ex.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Assigns folder paths for the application's filesystem structure.
        /// </summary>
        private static void AssignFolderLocations()
        {
            try
            {
                List<string> folderLocations = new List<string>();

                Globals.rootFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SimplePasswordManager");
                Globals.configFolder = Path.Combine(Globals.rootFolder, "config");
                Globals.resourcesFolder = Path.Combine(Globals.rootFolder, "resources");
                Globals.passwordFolder = Path.Combine(Globals.resourcesFolder, "passwords");
                Globals.loginsFolder = Path.Combine(Globals.resourcesFolder, "logins");

                folderLocations.Add(Globals.rootFolder);
                folderLocations.Add(Globals.configFolder);
                folderLocations.Add(Globals.resourcesFolder);
                folderLocations.Add(Globals.passwordFolder);
                folderLocations.Add(Globals.loginsFolder);

                foreach (string folder in folderLocations)
                {
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                        if (Globals.debugMode)
                        {
                            Console.WriteLine($"[DEBUG] Created directory: {folder}");
                        }
                    }
                    else
                    {
                        if (Globals.debugMode)
                        {
                            Console.WriteLine($"[DEBUG] Directory exists: {folder}");
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] IOException in AssignFolderLocations: {ex.Message}");
                }
                Console.WriteLine($"Error creating directories: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in AssignFolderLocations: {ex.Message}");
                }
                Console.WriteLine($"Unexpected error in directory setup: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Assigns file paths and populates the login files list.
        /// </summary>
        private static void AssignFileLocations()
        {
            try
            {
                Globals.configFile = Path.Combine(Globals.configFolder, "config.json");
                Globals.passwordFile = Path.Combine(Globals.passwordFolder, "password");
                Globals.recoveryCodeFile = Path.Combine(Globals.passwordFolder, "recovery_code");
                Globals.loginFiles = new List<string>(Directory.GetFiles(Globals.loginsFolder));

                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Assigned file locations: config={Globals.configFile}, password={Globals.passwordFile}, recovery={Globals.recoveryCodeFile}");
                    Console.WriteLine($"[DEBUG] Found {Globals.loginFiles.Count} login files.");
                }
            }
            catch (IOException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] IOException in AssignFileLocations: {ex.Message}");
                }
                Console.WriteLine($"Error accessing login files: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in AssignFileLocations: {ex.Message}");
                }
                Console.WriteLine($"Unexpected error in file setup: {ex.Message}");
                throw;
            }
        }
    }
}