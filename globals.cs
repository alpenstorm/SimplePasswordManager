using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json;
using System.Timers;

namespace SimplePasswordManager
{
    internal class Globals
    {
        // Save file variables
        public static bool isFirstLoad = true;
        public static bool debugMode = false;
        public static string version = "0.0.1a";

        // Filesystem structure
        public static string rootFolder;
        public static string tempFolder;
        public static string configFolder;
        public static string configFile;
        public static string resourcesFolder;
        public static string passwordFolder;
        public static string passwordFile;
        public static string recoveryCodeFile;
        public static string loginsFolder;
        public static List<string> loginFiles;

        // Password & recovery code
        public static string password;
        public static string recoveryCode;

        /// <summary>
        /// Clears the console after an optional delay.
        /// </summary>
        /// <param name="t">Delay in seconds before clearing the console.</param>
        public static void ClearConsole(int t = 0)
        {
            try
            {
                Console.Clear();
                Console.Beep();
                if (t > 0)
                {
                    Thread.Sleep(t * 1000);
                }
                if (debugMode)
                {
                    Console.WriteLine($"[DEBUG] Console cleared with {t} second delay.");
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error clearing console: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Reads a password from the console, displaying asterisks for each character.
        /// Returns the password string.
        /// </summary>
        /// <returns>The entered password.</returns>
        public static string ReadPassword()
        {
            StringBuilder password = new StringBuilder();
            try
            {
                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        if (debugMode)
                        {
                            Console.WriteLine("[DEBUG] Password input completed.");
                        }
                        Console.WriteLine();
                        break;
                    }
                    else if (key.Key == ConsoleKey.Backspace)
                    {
                        if (password.Length > 0)
                        {
                            password.Length--;
                            Console.Write("\b \b");
                            if (debugMode)
                            {
                                Console.WriteLine("[DEBUG] Backspace in password input.");
                            }
                        }
                    }
                    else
                    {
                        password.Append(key.KeyChar);
                        Console.Write("*");
                        if (debugMode)
                        {
                            Console.WriteLine($"[DEBUG] Password char added.");
                        }
                    }
                }
                return password.ToString();
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error reading password: {ex.Message}");
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Reads user input from the console, displaying each character as typed.
        /// Returns the input string or null if Escape is pressed.
        /// </summary>
        /// <returns>The input string, or null if Escape is pressed.</returns>
        public static string ReadInput()
        {
            StringBuilder input = new StringBuilder();
            try
            {
                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        if (debugMode)
                        {
                            Console.WriteLine("[DEBUG] Input completed.");
                        }
                        return input.ToString();
                    }
                    else if (key.Key == ConsoleKey.Escape)
                    {
                        if (debugMode)
                        {
                            Console.WriteLine("[DEBUG] Input cancelled with Escape.");
                        }
                        return null;
                    }
                    else if (key.Key == ConsoleKey.Backspace)
                    {
                        if (input.Length > 0)
                        {
                            input.Length--;
                            Console.Write("\b \b");
                            if (debugMode)
                            {
                                Console.WriteLine("[DEBUG] Backspace in input.");
                            }
                        }
                    }
                    else
                    {
                        input.Append(key.KeyChar);
                        Console.Write(key.KeyChar);
                        if (debugMode)
                        {
                            Console.WriteLine($"[DEBUG] Added char: {key.KeyChar}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error reading input: {ex.Message}");
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Executes a shell command to copy text to the clipboard.
        /// </summary>
        /// <param name="command">The clipboard command (e.g., clip, pbcopy, xclip).</param>
        /// <param name="input">The text to pipe into the command.</param>
        /// <exception cref="ExternalException">Thrown if the command execution fails.</exception>
        public static void ExecuteCommand(string command, string input)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash",
                        Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"/c echo {input} | {command}" : $"-c \"echo -n {input} | {command}\"",
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    process.Start();
                    process.WaitForExit();
                    if (debugMode)
                    {
                        Console.WriteLine($"[DEBUG] Executed command: {command}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error executing command '{command}': {ex.Message}");
                }
                throw new ExternalException($"Failed to execute command: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a key-based menu and returns the pressed key.
        /// </summary>
        /// <param name="messages">Menu options to display (e.g., "[S]ettings").</param>
        /// <returns>The ConsoleKeyInfo of the pressed key.</returns>
        public static ConsoleKeyInfo MenuBuilder(params string[] messages)
        {
            try
            {
                foreach (string message in messages)
                {
                    Console.WriteLine(message);
                }
                Console.Write(": ");
                ConsoleKeyInfo key = Console.ReadKey();
                if (debugMode)
                {
                    Console.WriteLine($"[DEBUG] Menu selection: {key.KeyChar}");
                }
                Console.WriteLine();
                return key;
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in MenuBuilder: {ex.Message}");
                }
                return new ConsoleKeyInfo();
            }
        }

        /// <summary>
        /// Saves the current configuration to the config.json file.
        /// </summary>
        /// <exception cref="IOException">Thrown if writing to the config file fails.</exception>
        public static void SaveConfig()
        {
            try
            {
                JsonObject j = new JsonObject
                {
                    ["config"] = new JsonObject
                    {
                        ["first_load"] = isFirstLoad,
                        ["debug_mode"] = debugMode
                    },
                    ["version"] = version,
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                };
                string jsonContent = j.ToJsonString(options);

                File.WriteAllText(configFile, jsonContent);
                if (debugMode)
                {
                    Console.WriteLine($"[DEBUG] Config saved to {configFile}:");
                    Console.WriteLine(jsonContent);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
                if (debugMode)
                {
                    Console.WriteLine($"[DEBUG] IOException in SaveConfig: {ex}");
                }
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
                if (debugMode)
                {
                    Console.WriteLine($"[DEBUG] Exception in SaveConfig: {ex}");
                }
                throw;
            }
        }
    }
}