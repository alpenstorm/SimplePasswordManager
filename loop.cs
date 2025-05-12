using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SimplePasswordManager
{
    internal class Loop
    {
        public static void MainLoop() { MainMenu(); }

        private static void MainMenu()
        {
            ConsoleKeyInfo key = Globals.MenuBuilder(
                "[C]reate a new login",
                "[O]pen a login",
                "[D]elete a login",
                "[V]iew all logins",
                "[S]ettings",
                "[L]ock the app",
                "[Q]uit the app"
                );

            if (key.Key == ConsoleKey.C) { CreateLogin(); }
            else if (key.Key == ConsoleKey.O) { OpenLogin(); }
            else if (key.Key == ConsoleKey.D) { DeleteLogin(Console.ReadLine()); }
            else if (key.Key == ConsoleKey.V) { ViewLogins(); }
            else if (key.Key == ConsoleKey.S) { OpenSettings(); }
            else if (key.Key == ConsoleKey.L) { Init.Lock(); }
            else if (key.Key == ConsoleKey.Q) { Environment.Exit(0); }
            else { MainMenu(); }
        }

        private static void CreateLogin()
        {
            Globals.ClearConsole();
            Console.WriteLine("You are creating a new login.");

            // Username input
            Console.Write("Enter username/email (Esc to cancel): ");
            string username = Globals.ReadInput();
            if (username == null)
            {
                Back(false);
                return;
            }

            // Password input
            Console.Write("Enter password (Esc to cancel): ");
            string password = Globals.ReadInput();
            if (password == null)
            {
                Back(false);
                return;
            }

            // Extra messages
            List<string> extraMessages = new List<string>();
            while (true)
            {
                Console.Write("Add extra message (Enter to skip, Esc to finish): ");
                string message = Globals.ReadInput();
                if (message == null)
                {
                    CreateLoginJSON(username, password, extraMessages.ToArray());
                    return;
                }

                if (!string.IsNullOrEmpty(message))
                {
                    extraMessages.Add(message);
                }
                else
                {
                    break; // Empty input skips to finish
                }
            }

            CreateLoginJSON(username, password, extraMessages.ToArray());
        }

        private static void CreateLoginJSON(
            string username,
            string password,
            params string[] extraMessages)
        {
            // create initial JSON
            JsonObject j = new JsonObject
            {
                ["username"] = username,
                ["password"] = password
            };

            // add extra messages if any exist
            if (extraMessages != null && extraMessages.Length > 0)
            {
                    JsonArray messagesArray = new JsonArray();
                    foreach (string i in extraMessages) { messagesArray.Add(i); }
                    j["extraMessages"] = messagesArray;
                }

            // serialize it
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver() // Add TypeInfoResolver
            };
            string jsonContent = j.ToJsonString(options);

            Console.WriteLine();
            Console.WriteLine("Login item created: ");
            Console.WriteLine(jsonContent);
            Console.WriteLine();
            Console.WriteLine("Is this OK?");

            while (true)
            {
                ConsoleKeyInfo key = Globals.MenuBuilder("[Y]es", "[N]o");
                Console.WriteLine();

                if (key.Key == ConsoleKey.Y)
                {
                    Console.Write("Please provide a name for the login: ");
                    string fileName = Console.ReadLine();
                    string path = Path.Combine(Globals.loginsFolder, fileName);
                    Globals.loginFiles.Add(fileName);
                    Encryption.EncryptString(Globals.password, jsonContent, path, fileName);
                    
                    Globals.ClearConsole();
                    Back(true, $"Successfully created {fileName}, press any key to continue.");
                    break;
                }

                else if (key.Key == ConsoleKey.N)
                {
                    Back(false);
                    break;
                }

                else { continue; }
            }
        }

        private static void OpenLogin()
        {
            Globals.ClearConsole();
            Console.WriteLine("Enter the name of the login you want to open: ");
            string loginToOpen = Console.ReadLine();

            // Construct the full file path
            string loginFilePath = Path.Combine(Globals.loginsFolder, loginToOpen);

            // Check if the login exists
            if (FindLogin(loginToOpen) && File.Exists(loginFilePath))
            {
                try
                {
                    string decryptedString = Encryption.DecryptString(Globals.password, loginFilePath);
                    // Parse the JSON content for display
                    JsonNode jsonNode = JsonNode.Parse(decryptedString);
                    JsonObject jsonObject = jsonNode.AsObject();
                    string username = jsonObject["username"]?.ToString() ?? "";
                    string password = jsonObject["password"]?.ToString() ?? "";
                    JsonArray extraMessagesArray = jsonObject["extraMessages"]?.AsArray();
                    List<string> extraMessages = extraMessagesArray != null
                        ? extraMessagesArray.Select(m => m?.ToString()).Where(m => m != null).ToList()
                        : new List<string>();

                    Console.WriteLine();
                    Console.WriteLine("Login Details:");
                    Console.WriteLine($"Username: {username}");
                    Console.WriteLine($"Password: {password}");
                    if (extraMessages.Count > 0)
                    {
                        Console.WriteLine("Extra Messages:");
                        for (int i = 0; i < extraMessages.Count; i++)
                        {
                            Console.WriteLine($"  {i + 1}. {extraMessages[i]}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Extra Messages: None");
                    }

                    ConsoleKeyInfo key = Globals.MenuBuilder("[E]dit", "Edit [N]ame", "[D]elete", "[C]opy", "[B]ack");

                    if (key.Key == ConsoleKey.E)
                    {
                        // Create a unique temporary directory
                        string tempFolder = Path.Combine(Globals.rootFolder, $"temp_edit_{Guid.NewGuid().ToString()}");
                        string tempFilePath = Path.Combine(tempFolder, "login.json");

                        try
                        {
                            // Create temporary directory
                            Directory.CreateDirectory(tempFolder);

                            // Write decrypted JSON to temporary file
                            File.WriteAllText(tempFilePath, decryptedString);

                            Console.WriteLine();
                            Console.WriteLine("Opening login details in default editor...");
                            Console.WriteLine("Please save and close the editor to apply changes.");

                            // Start the default editor
                            using (Process editorProcess = new Process())
                            {
                                editorProcess.StartInfo = new ProcessStartInfo
                                {
                                    FileName = tempFilePath,
                                    UseShellExecute = true
                                };
                                editorProcess.Start();
                                editorProcess.WaitForExit(); // Wait for the editor to close
                            }

                            // Read the edited content
                            string editedContent = File.ReadAllText(tempFilePath);

                            // Validate the edited JSON
                            try
                            {
                                JsonNode.Parse(editedContent); // Ensure it's valid JSON
                            }
                            catch (JsonException)
                            {
                                Globals.ClearConsole();
                                Back(true, "Error: The edited content is not valid JSON. Changes discarded. Press any key to continue.");
                                return;
                            }

                            // Confirm saving changes
                            Console.WriteLine();
                            Console.WriteLine("Edited login item:");
                            Console.WriteLine(editedContent);
                            Console.WriteLine("Save these changes?");
                            ConsoleKeyInfo confirmKey = Globals.MenuBuilder("[Y]es", "[N]o");

                            if (confirmKey.Key == ConsoleKey.Y)
                            {
                                // Re-encrypt and overwrite the original file
                                Encryption.EncryptString(Globals.password, editedContent, loginFilePath, Path.GetFileName(loginFilePath));
                                Globals.ClearConsole();
                                Back(true, "Changes saved successfully, press any key to continue.");
                            }
                            else
                            {
                                Globals.ClearConsole();
                                Back(true, "Changes discarded, press any key to continue.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Globals.ClearConsole();
                            Back(true, $"Error during editing: {ex.Message}, press any key to continue.");
                        }
                        finally
                        {
                            // Clean up temporary files and directory
                            try
                            {
                                if (File.Exists(tempFilePath)) { File.Delete(tempFilePath); }
                                if (Directory.Exists(tempFolder)) { Directory.Delete(tempFolder); }
                            }
                            catch (Exception ex)
                            {
                                if (Globals.debugMode)
                                {
                                    Console.WriteLine($"Warning: Failed to clean up temporary directory {tempFolder}. {ex.Message}");
                                }
                            }
                        }
                    }
                    else if (key.Key == ConsoleKey.N)
                    {
                        Console.WriteLine();
                        Console.Write("Enter new name for the login (Esc to cancel): ");
                        string newName = Globals.ReadInput();
                        if (newName == null)
                        {
                            Back(true, "Rename cancelled. Press any key to continue.");
                            return;
                        }

                        // Check for empty or whitespace name
                        if (string.IsNullOrWhiteSpace(newName))
                        {
                            Back(true, "Error: Login name cannot be empty or whitespace. Press any key to continue.");
                            return;
                        }

                        // Check if the new name already exists
                        string newFilePath = Path.Combine(Globals.loginsFolder, newName);
                        if (File.Exists(newFilePath))
                        {
                            Back(true, "Error: A login with this name already exists. Press any key to continue.");
                            return;
                        }

                        try
                        {
                            // Move the file to the new name
                            File.Move(loginFilePath, newFilePath);

                            // Update Globals.loginFiles
                            int index = Globals.loginFiles.FindIndex(f => f.Equals(loginToOpen, StringComparison.OrdinalIgnoreCase));
                            if (index >= 0)
                            {
                                Globals.loginFiles[index] = newName;
                            }
                            Back(true, $"Login successfully renamed to {newName}. Press any key to continue.");
                        }
                        catch (Exception ex)
                        {
                            Globals.ClearConsole();
                            Back(true, $"Error renaming login: {ex.Message}, press any key to continue.");
                        }
                    }
                    else if (key.Key == ConsoleKey.D)
                    {
                        Globals.ClearConsole();
                        DeleteLogin(loginFilePath);
                    }
                    else if (key.Key == ConsoleKey.C)
                    {
                        bool usernameCopied = CopyToClipboard(username);
                        if (usernameCopied)
                        {
                            Console.WriteLine("Success, copied username to clipboard. Press any key to continue");
                            Console.ReadKey(true);
                        }
                        bool passwordCopied = CopyToClipboard(password);
                        if (passwordCopied)
                        {
                            Console.WriteLine("Success, copied password to clipboard. Press any key to go back");
                            Back(false);
                        }
                    }
                    else if (key.Key == ConsoleKey.B) { Back(false); }
                }
                catch (CryptographicException)
                {
                    Globals.ClearConsole();
                    Back(true, "Error: Failed to decrypt the login. The file may be corrupted or the password is incorrect. \nPress any key to go back.");
                }
                catch (Exception ex)
                {
                    Globals.ClearConsole();
                    Back(true, $"Error: An unexpected error occurred: {ex.Message}, press any key to go back.");
                }
            }
            else
            {
                Globals.ClearConsole();
                Back(true, "Login not found or file does not exist, press any key to go back.");
            }
        }

        private static void ViewLogins()
        {
            Globals.ClearConsole();
            string[] files = Globals.loginFiles.ToArray();
            foreach (string i in files) { Console.WriteLine(Path.GetFileName(i)); }
            Back();
        }

        private static void DeleteLogin(string loginPath)
        {
            if (!string.IsNullOrEmpty(loginPath))
            {
                if (File.Exists(loginPath))
                {
                    while (true)
                    {
                        Console.WriteLine($"Do you want to delete {Path.GetFileName(loginPath)}");
                        ConsoleKeyInfo key = Globals.MenuBuilder("[Y]es", "[N]o");

                        if (key.Key == ConsoleKey.Y)
                        {
                            File.Delete(loginPath);
                            Console.WriteLine($"{Path.GetFileName(loginPath)} was successfully deleted.");
                            Back(true);
                            break;
                        }
                        else if (key.Key == ConsoleKey.N) 
                        { 
                            Back(false);
                            break;
                        }
                        else { continue; }
                    }
                }
            }
            else { Back(true, "Input detected as null, press any key to go back."); }
        }

        private static void OpenSettings()
        {
            while (true)
            {
                ConsoleKeyInfo key = Globals.MenuBuilder(
                    "[D]ebug Mode (Current: " + (Globals.debugMode ? "Enabled" : "Disabled") + ")",
                    "[T]imeout (Current: " + Globals.timeout + " seconds)",
                    "[B]ack"
                );

                if (key.Key == ConsoleKey.D)
                {
                    // Toggle debug mode
                    Console.WriteLine();
                    Console.Write("Enable debug mode? (Current: " + (Globals.debugMode ? "Enabled" : "Disabled") + ")");
                    ConsoleKeyInfo debugKey = Globals.MenuBuilder("[Y]es", "[N]o");

                    if (debugKey.Key == ConsoleKey.Y)
                    {
                        Globals.debugMode = true;
                        Globals.SaveConfig();
                        Back(true, "Debug mode enabled. Press any key to continue.");
                    }
                    else if (debugKey.Key == ConsoleKey.N)
                    {
                        Globals.debugMode = false;
                        Globals.SaveConfig();
                        Back(true, "Debug mode disabled. Press any key to continue.");
                    }
                }
                else if (key.Key == ConsoleKey.T)
                {
                    // Edit timeout
                    Console.WriteLine();
                    Console.Write("Enter new timeout in seconds (0 to disable, Esc to cancel): ");
                    string input = Globals.ReadInput();
                    if (input == null)
                    {
                        Back(false);
                        continue;
                    }

                    if (int.TryParse(input, out int newTimeout) && newTimeout >= 0)
                    {
                        Globals.timeout = newTimeout;
                        Globals.SaveConfig();
                        Back(true, $"Timeout set to {newTimeout} seconds. Press any key to continue.");
                    }
                    else
                    {
                        Back(true, "Invalid input. Timeout must be a non-negative number. Press any key to continue.");
                    }
                }
                else if (key.Key == ConsoleKey.B)
                {
                    Back(false);
                    break;
                }
            }
        }

        private static bool FindLogin(string searchTerm)
        {
            foreach (string i in Globals.loginFiles)
            {
                if (searchTerm.Equals(Path.GetFileName(i), StringComparison.OrdinalIgnoreCase)) { return true; }
            }
            return false;
        }

        /// <summary>
        /// Helper function for going back to the main menu.
        /// </summary>
        /// <param name="showMessage">Show the message?</param>
        /// <param name="message">The message to display for prompting to go back.</param>
        private static void Back(bool showMessage = true, string message = "Press any key to go back.")
        {
            if (showMessage)
            { 
                Console.WriteLine(message); 
                Console.ReadKey(true);
            }
            Globals.ClearConsole();
            MainMenu();
        }

        [STAThread]
        /// <summary>
        /// Copies the specified text to the system clipboard.
        /// Works on Windows, macOS, and Linux.
        /// </summary>
        /// <param name="text">The text to copy to the clipboard.</param>
        /// <returns>True if the copy operation was successful, false otherwise.</returns>
        public static bool CopyToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                Console.WriteLine("Copy Error: Input text is empty.");
                return false;
            }

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Windows: Use clip.exe
                    Globals.ExecuteCommand("clip", text);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // macOS: Use pbcopy
                    Globals.ExecuteCommand("pbcopy", text);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Linux: Use xclip (assumes xclip is installed)
                    Globals.ExecuteCommand("xclip -selection clipboard", text);
                }
                else
                {
                    // Unsupported platform
                    Console.WriteLine("Copy Error: Unsupported platoform (what are you running?).");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                // Handle any errors (e.g., command not found, permissions)
                Console.WriteLine($"Copy Error: {ex.Message}");
                return false;
            }
        }
    }
}