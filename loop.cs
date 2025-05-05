using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

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
            else if (key.Key == ConsoleKey.O) { /* open a login */}
            else if (key.Key == ConsoleKey.D) { /* delete a login */ }
            else if (key.Key == ConsoleKey.V) { ViewLogins(); }
            else if (key.Key == ConsoleKey.S) { /* open settings */ }
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
                Globals.ClearConsole();
                MainMenu();
                return;
            }

            // Password input
            Console.Write("Enter password (Esc to cancel): ");
            string password = Globals.ReadInput();
            if (password == null)
            {
                Globals.ClearConsole();
                MainMenu();
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
                foreach (string i in extraMessages)
                {
                    messagesArray.Add(i);
                }
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
                }

                else if (key.Key == ConsoleKey.N) 
                {
                    Globals.ClearConsole();
                    MainMenu();
                    break;
                }

                else { continue; }
            }
        }

        private static void ViewLogins()
        {
            string[] files = Globals.loginFiles.ToArray();
            foreach (string i in files) { Console.WriteLine(i); }
            Console.WriteLine("Press any key to go back");
            Console.ReadKey(true);
            Globals.ClearConsole();
            MainMenu();
        }
    }
}
