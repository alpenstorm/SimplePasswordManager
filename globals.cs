using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json;

namespace SimplePasswordManager
{
    internal class Globals
    {
        // save file variables
        public static bool isFirstLoad = true;
        public static bool debugMode = false;
        public static string version = "0.0.1a";
        public static int timeout = 300;

        //  filesystem structure visualization
        /*    
         *    rootFolder
         *         |
         *         |- configFolder
         *         |        |- configFile
         *         |- resourcesFolder
         *         |        |- passwordFolder
         *                  |        |- passwordFile
         *                  |        |- recoveryCodeFile
         *                  |- loginsFolder
         *                  |        |- logins (1 per file)
         *                  
         */

        public static string rootFolder;
        public static string tempFolder;
        
        // filesystem - config
        public static string configFolder;
        public static string configFile;

        // filesystem - resources
        public static string resourcesFolder;
        public static string passwordFolder;
        public static string passwordFile;
        public static string recoveryCodeFile;
        public static string loginsFolder;
        public static List<string> loginFiles;

        // password & recovery code
        public static string password;
        public static string recoveryCode;

        // clears console after t seconds
        public static void ClearConsole(int t = 0)
        {
            Console.Clear();
            Console.Beep();
            Thread.Sleep(t * 1000);
        }

        public static string ReadPassword()
        {
            StringBuilder password = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true); // true prevents displaying the key
                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password.Length--;
                        Console.Write("\b \b"); // Remove last * and replace with space
                    }
                }
                else
                {
                    password.Append(key.KeyChar); // Store actual character
                    Console.Write("*"); // Display * instead
                }
            }
            return password.ToString();
        }

        /// <summary>
        /// Reads user input from the console, displaying each character as typed for verification.
        /// Returns the input string or null if the user presses Escape to cancel.
        /// Supports backspace for editing.
        /// </summary>
        /// <returns>The input string, or null if Escape is pressed.</returns>
        public static string ReadInput()
        {
            StringBuilder input = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine(); // Move to next line
                    return input.ToString();
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    return null; // Indicate cancellation
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (input.Length > 0)
                    {
                        input.Length--;
                        Console.Write("\b \b"); // Remove last character from display
                    }
                }
                else
                {
                    input.Append(key.KeyChar);
                    Console.Write(key.KeyChar); // Display the typed character
                    if (debugMode)
                    {
                        Console.WriteLine($"[DEBUG] Added char: {key.KeyChar}");
                    }
                }
            }
        }

        /// <summary>
        /// Executes a shell command
        /// </summary>
        /// <param name="command">The clipboard command (e.g., clip, pbcopy, xclip).</param>
        /// <param name="input">The text to pipe into the command.</param>
        public static void ExecuteCommand(string command, string input)
        {
            // Create a process to run the command
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
            }
        }

        /// <summary>
        /// A utility function to create key-based menus.
        /// </summary>
        /// <param name="messages">Messages to display, as options, for example "[S]ettings", "[E]dit"</param>
        /// <returns>The console key that was pressed, for example, S, or E</returns>
        public static ConsoleKeyInfo MenuBuilder(params string[] messages)
        {
            foreach (string i in messages)
            {
                Console.WriteLine(i);
            }
            Console.Write(": ");
            return Console.ReadKey();
        }

        /// <summary>
        /// Saves the current configuration settings to the config.json file.
        /// </summary>
        public static void SaveConfig()
        {
            try
            {
                // Create JSON object for config
                JsonObject j = new JsonObject
                {
                    ["config"] = new JsonObject
                    {
                        ["first_load"] = isFirstLoad,
                        ["debug_mode"] = debugMode,
                        ["timeout"] = timeout,
                    },
                    ["version"] = version,
                };

                // Serialize JSON with indentation
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                };
                string jsonContent = j.ToJsonString(options);

                // Write to config file
                File.WriteAllText(configFile, jsonContent);

                if (debugMode)
                {
                    Console.WriteLine($"[DEBUG] Config saved to {configFile}:");
                    Console.WriteLine(jsonContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
                if (debugMode)
                {
                    Console.WriteLine($"[DEBUG] Exception details: {ex}");
                }
            }
        }
    }
}
