using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace SimplePasswordManager
{
    internal class Loop
    {
        /// <summary>
        /// Starts the main application loop.
        /// </summary>
        public static void MainLoop()
        {
            try
            {
                MainMenu();
                if (Globals.debugMode)
                {
                    Console.WriteLine("[DEBUG] MainLoop started.");
                }
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in MainLoop: {ex.Message}");
                }
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                Back(true, "Press any key to continue.");
            }
        }

        /// <summary>
        /// Displays the main menu and handles user selections.
        /// </summary>
        private static void MainMenu()
        {
            try
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
                else if (key.Key == ConsoleKey.D)
                {
                    Console.Write("Enter login name to delete: ");
                    string loginName = Globals.ReadInput();
                    DeleteLogin(Path.Combine(Globals.loginsFolder, loginName));
                }
                else if (key.Key == ConsoleKey.V) { ViewLogins(); }
                else if (key.Key == ConsoleKey.S) { OpenSettings(); }
                else if (key.Key == ConsoleKey.L) { Init.Lock(); }
                else if (key.Key == ConsoleKey.Q) { Environment.Exit(0); }
                else { MainMenu(); }
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in MainMenu: {ex.Message}");
                }
                Console.WriteLine($"Error in menu: {ex.Message}");
                Back(true, "Press any key to continue.");
            }
        }

        /// <summary>
        /// Prompts the user to create a new login entry.
        /// </summary>
        private static void CreateLogin()
        {
            try
            {
                Globals.ClearConsole();
                Console.WriteLine("You are creating a new login.");

                Console.Write("Enter username/email (Esc to cancel): ");
                string username = Globals.ReadInput();
                if (username == null)
                {
                    Back(false);
                    return;
                }

                Console.Write("Enter password (Esc to cancel): ");
                string password = Globals.ReadPassword();
                if (password == null)
                {
                    Back(false);
                    return;
                }

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
                        break;
                    }
                }

                CreateLoginJSON(username, password, extraMessages.ToArray());
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in CreateLogin: {ex.Message}");
                }
                Console.WriteLine($"Error creating login: {ex.Message}");
                Back(true, "Press any key to continue.");
            }
        }

        /// <summary>
        /// Creates a JSON object for a login and saves it as an encrypted file.
        /// </summary>
        /// <param name="username">The username or email for the login.</param>
        /// <param name="password">The password for the login.</param>
        /// <param name="extraMessages">Optional extra messages associated with the login.</param>
        private static void CreateLoginJSON(string username, string password, params string[] extraMessages)
        {
            try
            {
                JsonObject j = new JsonObject
                {
                    ["username"] = username,
                    ["password"] = password
                };

                if (extraMessages != null && extraMessages.Length > 0)
                {
                    JsonArray messagesArray = new JsonArray();
                    foreach (string i in extraMessages) { messagesArray.Add(i); }
                    j["extraMessages"] = messagesArray;
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
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
                        if (string.IsNullOrWhiteSpace(fileName))
                        {
                            Console.WriteLine("Error: Login name cannot be empty.");
                            continue;
                        }

                        string path = Path.Combine(Globals.loginsFolder, fileName);
                        if (File.Exists(path))
                        {
                            Console.WriteLine("Error: A login with this name already exists.");
                            continue;
                        }

                        Globals.loginFiles.Add(fileName);
                        Encryption.EncryptString(Globals.password, jsonContent, path, fileName);

                        if (Globals.debugMode)
                        {
                            Console.WriteLine($"[DEBUG] Created login JSON and saved to {path}");
                        }

                        Globals.ClearConsole();
                        Back(true, $"Successfully created {fileName}, press any key to continue.");
                        break;
                    }
                    else if (key.Key == ConsoleKey.N)
                    {
                        Back(false);
                        break;
                    }
                }
            }
            catch (JsonException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] JsonException in CreateLoginJSON: {ex.Message}");
                }
                Console.WriteLine($"Error serializing login JSON: {ex.Message}");
                Back(true, "Press any key to continue.");
            }
            catch (IOException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] IOException in CreateLoginJSON: {ex.Message}");
                }
                Console.WriteLine($"Error saving login: {ex.Message}");
                Back(true, "Press any key to continue.");
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in CreateLoginJSON: {ex.Message}");
                }
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Back(true, "Press any key to continue.");
            }
        }

        /// <summary>
        /// Opens and displays details of an existing login, allowing editing or deletion.
        /// </summary>
        private static void OpenLogin()
        {
            try
            {
                Globals.ClearConsole();
                Console.WriteLine("Enter the name of the login you want to open: ");
                string loginToOpen = Console.ReadLine();
                string loginFilePath = Path.Combine(Globals.loginsFolder, loginToOpen);

                if (FindLogin(loginToOpen) && File.Exists(loginFilePath))
                {
                    string decryptedString = Encryption.DecryptString(Globals.password, loginFilePath);
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
                    Console.WriteLine();
                    Console.WriteLine($"Username: {username}");
                    Console.WriteLine($"Password: {password}");
                    Console.WriteLine();
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
                        string tempFolder = Path.Combine(Globals.rootFolder, $"temp_edit_{Guid.NewGuid().ToString()}");
                        string tempFilePath = Path.Combine(tempFolder, "login.json");

                        try
                        {
                            Directory.CreateDirectory(tempFolder);
                            File.WriteAllText(tempFilePath, decryptedString);

                            Console.WriteLine();
                            Console.WriteLine("Opening login details in default editor...");
                            Console.WriteLine("Please save and close the editor to apply changes.");

                            using (Process editorProcess = new Process())
                            {
                                editorProcess.StartInfo = new ProcessStartInfo
                                {
                                    FileName = tempFilePath,
                                    UseShellExecute = true
                                };
                                editorProcess.Start();
                                editorProcess.WaitForExit();
                            }

                            string editedContent = File.ReadAllText(tempFilePath);
                            JsonNode.Parse(editedContent); // Validate JSON

                            Console.WriteLine();
                            Console.WriteLine("Edited login item:");
                            Console.WriteLine(editedContent);
                            Console.WriteLine("Save these changes?");
                            ConsoleKeyInfo confirmKey = Globals.MenuBuilder("[Y]es", "[N]o");

                            if (confirmKey.Key == ConsoleKey.Y)
                            {
                                Encryption.EncryptString(Globals.password, editedContent, loginFilePath, Path.GetFileName(loginFilePath));
                                if (Globals.debugMode)
                                {
                                    Console.WriteLine($"[DEBUG] Saved edited login to {loginFilePath}");
                                }
                                Globals.ClearConsole();
                                Back(true, "Changes saved successfully, press any key to continue.");
                            }
                            else
                            {
                                Globals.ClearConsole();
                                Back(true, "Changes discarded, press any key to continue.");
                            }
                        }
                        catch (JsonException ex)
                        {
                            if (Globals.debugMode)
                            {
                                Console.WriteLine($"[DEBUG] JsonException in OpenLogin (edit): {ex.Message}");
                            }
                            Globals.ClearConsole();
                            Back(true, "Error: The edited content is not valid JSON. Changes discarded. Press any key to continue.");
                        }
                        catch (IOException ex)
                        {
                            if (Globals.debugMode)
                            {
                                Console.WriteLine($"[DEBUG] IOException in OpenLogin (edit): {ex.Message}");
                            }
                            Globals.ClearConsole();
                            Back(true, $"Error during editing: {ex.Message}, press any key to continue.");
                        }
                        finally
                        {
                            try
                            {
                                if (File.Exists(tempFilePath)) { File.Delete(tempFilePath); }
                                if (Directory.Exists(tempFolder)) { Directory.Delete(tempFolder); }
                                if (Globals.debugMode)
                                {
                                    Console.WriteLine($"[DEBUG] Cleaned up temporary directory {tempFolder}");
                                }
                            }
                            catch (Exception ex)
                            {
                                if (Globals.debugMode)
                                {
                                    Console.WriteLine($"[DEBUG] Failed to clean up temporary directory {tempFolder}: {ex.Message}");
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

                        if (string.IsNullOrWhiteSpace(newName))
                        {
                            Back(true, "Error: Login name cannot be empty or whitespace. Press any key to continue.");
                            return;
                        }

                        string newFilePath = Path.Combine(Globals.loginsFolder, newName);
                        if (File.Exists(newFilePath))
                        {
                            Back(true, "Error: A login with this name already exists. Press any key to continue.");
                            return;
                        }

                        try
                        {
                            File.Move(loginFilePath, newFilePath);
                            int index = Globals.loginFiles.FindIndex(f => f.Equals(loginToOpen, StringComparison.OrdinalIgnoreCase));
                            if (index >= 0)
                            {
                                Globals.loginFiles[index] = newName;
                            }
                            if (Globals.debugMode)
                            {
                                Console.WriteLine($"[DEBUG] Renamed login from {loginToOpen} to {newName}");
                            }
                            Back(true, $"Login successfully renamed to {newName}. Press any key to continue.");
                        }
                        catch (IOException ex)
                        {
                            if (Globals.debugMode)
                            {
                                Console.WriteLine($"[DEBUG] IOException in OpenLogin (rename): {ex.Message}");
                            }
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
                else
                {
                    if (Globals.debugMode)
                    {
                        Console.WriteLine($"[DEBUG] Login {loginToOpen} not found at {loginFilePath}");
                    }
                    Globals.ClearConsole();
                    Back(true, "Login not found or file does not exist, press any key to go back.");
                }
            }
            catch (CryptographicException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] CryptographicException in OpenLogin: {ex.Message}");
                }
                Globals.ClearConsole();
                Back(true, "Error: Failed to decrypt the login. The file may be corrupted or the password is incorrect. Press any key to go back.");
            }
            catch (JsonException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] JsonException in OpenLogin: {ex.Message}");
                }
                Globals.ClearConsole();
                Back(true, $"Error parsing login JSON: {ex.Message}, press any key to go back.");
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Unexpected error in OpenLogin: {ex.Message}");
                }
                Globals.ClearConsole();
                Back(true, $"Error: An unexpected error occurred: {ex.Message}, press any key to go back.");
            }
        }

        /// <summary>
        /// Displays a list of all login files.
        /// </summary>
        private static void ViewLogins()
        {
            try
            {
                Globals.ClearConsole();
                string[] files = Globals.loginFiles.ToArray();
                foreach (string i in files)
                {
                    Console.WriteLine(Path.GetFileName(i));
                }
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Displayed {files.Length} login files");
                }
                Back();
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in ViewLogins: {ex.Message}");
                }
                Console.WriteLine($"Error viewing logins: {ex.Message}");
                Back(true, "Press any key to continue.");
            }
        }

        /// <summary>
        /// Deletes a specified login file after user confirmation.
        /// </summary>
        /// <param name="loginPath">Path to the login file to delete.</param>
        private static void DeleteLogin(string loginPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(loginPath) && File.Exists(loginPath))
                {
                    while (true)
                    {
                        Console.WriteLine($"Do you want to delete {Path.GetFileName(loginPath)}");
                        ConsoleKeyInfo key = Globals.MenuBuilder("[Y]es", "[N]o");

                        if (key.Key == ConsoleKey.Y)
                        {
                            File.Delete(loginPath);
                            Globals.loginFiles.RemoveAll(f => f.Equals(Path.GetFileName(loginPath), StringComparison.OrdinalIgnoreCase));
                            if (Globals.debugMode)
                            {
                                Console.WriteLine($"[DEBUG] Deleted login file {loginPath}");
                            }
                            Console.WriteLine($"{Path.GetFileName(loginPath)} was successfully deleted.");
                            Back(true);
                            break;
                        }
                        else if (key.Key == ConsoleKey.N)
                        {
                            Back(false);
                            break;
                        }
                    }
                }
                else
                {
                    if (Globals.debugMode)
                    {
                        Console.WriteLine($"[DEBUG] Login path {loginPath} is invalid or does not exist");
                    }
                    Back(true, "Input detected as null or file does not exist, press any key to go back.");
                }
            }
            catch (IOException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] IOException in DeleteLogin: {ex.Message}");
                }
                Console.WriteLine($"Error deleting login: {ex.Message}");
                Back(true, "Press any key to continue.");
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in DeleteLogin: {ex.Message}");
                }
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Back(true, "Press any key to continue.");
            }
        }

        /// <summary>
        /// Opens the settings menu to configure debug mode and timeout.
        /// </summary>
        private static void OpenSettings()
        {
            try
            {
                while (true)
                {
                    ConsoleKeyInfo key = Globals.MenuBuilder(
                        "[D]ebug Mode (Current: " + (Globals.debugMode ? "Enabled" : "Disabled") + ")",
                        "[B]ack"
                    );

                    if (key.Key == ConsoleKey.D)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Enable debug mode? (Current: " + (Globals.debugMode ? "Enabled" : "Disabled") + ")");
                        ConsoleKeyInfo debugKey = Globals.MenuBuilder("[Y]es", "[N]o");

                        if (debugKey.Key == ConsoleKey.Y)
                        {
                            Globals.debugMode = true;
                            Globals.SaveConfig();
                            if (Globals.debugMode)
                            {
                                Console.WriteLine("[DEBUG] Debug mode enabled.");
                            }
                            Back(true, "Debug mode enabled. Press any key to continue.");
                        }
                        else if (debugKey.Key == ConsoleKey.N)
                        {
                            Globals.debugMode = false;
                            Globals.SaveConfig();
                            if (Globals.debugMode)
                            {
                                Console.WriteLine("[DEBUG] Debug mode disabled.");
                            }
                            Back(true, "Debug mode disabled. Press any key to continue.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in OpenSettings: {ex.Message}");
                }
                Console.WriteLine($"Error in settings: {ex.Message}");
                Back(true, "Press any key to continue.");
            }
        }

        /// <summary>
        /// Checks if a login file exists.
        /// </summary>
        /// <param name="searchTerm">The name of the login to search for.</param>
        /// <returns>True if the login exists, false otherwise.</returns>
        private static bool FindLogin(string searchTerm)
        {
            try
            {
                foreach (string i in Globals.loginFiles)
                {
                    if (searchTerm.Equals(Path.GetFileName(i), StringComparison.OrdinalIgnoreCase))
                    {
                        if (Globals.debugMode)
                        {
                            Console.WriteLine($"[DEBUG] Found login: {searchTerm}");
                        }
                        return true;
                    }
                }
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Login not found: {searchTerm}");
                }
                return false;
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in FindLogin: {ex.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Navigates back to the main menu, optionally displaying a message.
        /// </summary>
        /// <param name="showMessage">Whether to show a message before returning.</param>
        /// <param name="message">The message to display.</param>
        private static void Back(bool showMessage = true, string message = "Press any key to go back.")
        {
            try
            {
                if (showMessage)
                {
                    Console.WriteLine(message);
                    Console.ReadKey(true);
                }
                Globals.ClearConsole();
                MainMenu();
                if (Globals.debugMode)
                {
                    Console.WriteLine("[DEBUG] Returned to main menu.");
                }
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in Back: {ex.Message}");
                }
                Console.WriteLine($"Error navigating back: {ex.Message}");
                MainMenu();
            }
        }

        /// <summary>
        /// Copies text to the system clipboard.
        /// </summary>
        /// <param name="text">The text to copy.</param>
        /// <returns>True if the copy was successful, false otherwise.</returns>
        [STAThread]
        public static bool CopyToClipboard(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    Console.WriteLine("Copy Error: Input text is empty.");
                    if (Globals.debugMode)
                    {
                        Console.WriteLine("[DEBUG] CopyToClipboard failed: Empty text");
                    }
                    return false;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Globals.ExecuteCommand("clip", text);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Globals.ExecuteCommand("pbcopy", text);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Globals.ExecuteCommand("xclip -selection clipboard", text);
                }
                else
                {
                    Console.WriteLine("Copy Error: Unsupported platform.");
                    if (Globals.debugMode)
                    {
                        Console.WriteLine("[DEBUG] CopyToClipboard failed: Unsupported platform");
                    }
                    return false;
                }

                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Copied text to clipboard: {text}");
                }
                return true;
            }
            catch (ExternalException ex)
            {
                Console.WriteLine($"Copy Error: {ex.Message}");
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] ExternalException in CopyToClipboard: {ex.Message}");
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Copy Error: {ex.Message}");
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in CopyToClipboard: {ex.Message}");
                }
                return false;
            }
        }
    }
}