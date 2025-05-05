using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;

namespace SimplePasswordManager
{
    internal class Loop
    {
        public static void MainLoop() { MainMenu(); }

        private static void MainMenu()
        {
            ConsoleKeyInfo key = Globals.MenuBuilder(
                "[C]reate a new login",
                "[O]pen a login,"
                "[D]elete a login",
                "[V]iew all logins",
                "[L]ock the app",
                "[Q]uit the app"
                );
            
            if (key.Key == ConsoleKey.C) { CreateLogin(); }
            else if (key.Key == ConsoleKey.O) { /* open a login */}
            else if (key.Key == ConsoleKey.D) { /* delete a login */ }
            else if (key.Key == ConsoleKey.V) { ViewLogins(); }
            else if (key.Key == ConsoleKey.L) { Init.Lock(); }
            else if (key.Key == ConsoleKey.Q) { Environment.Exit(0); }
            else { MainMenu(); }
        }

        private static void CreateLogin()
        {
            Globals.ClearConsole();
            Console.WriteLine("You are creating a new login.");
            Console.Write("Enter username/email: ");

            // check for escape key before reading username
            ConsoleKeyInfo key;
            string username = "";
            while (true)
            {
                key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                {
                    MainMenu();
                    return;
                }
                if (key.Key == ConsoleKey.Enter)
                {
                    username = Console.ReadLine();
                    break;
                }
                username += key.KeyChar;
                Console.Write(key.KeyChar);
            }

            Console.Write("Enter password: ");
            string password = "";
            while (true)
            {
                key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                {
                    MainMenu();
                    return;
                }
                if (key.Key == ConsoleKey.Enter)
                {
                    password = Console.ReadLine();
                    break;
                }
                password += key.KeyChar;
                Console.Write(key.KeyChar);
            }

            List<string> extraMessages = new List<string>();
            while (true)
            {
                Console.Write("Add extra message (press Enter to skip, Escape to finish): ");
                string message = "";
                while (true)
                {
                    key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        CreateLoginJSON(username, password, extraMessages.ToArray());
                        return;
                    }
                    if (key.Key == ConsoleKey.Enter)
                    {
                        message = Console.ReadLine();
                        break;
                    }
                    message += key.KeyChar;
                    Console.Write(key.KeyChar);
                }

                if (!string.IsNullOrEmpty(message))
                {
                    extraMessages.Add(message);
                }
                else
                {
                    break; // empty input skips to finish
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
            string jsonContent = j.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

            Console.WriteLine("Login item created: ");
            Console.WriteLine(jsonContent);
            Console.WriteLine("Is this OK?");
            
            while (true) 
            {
                ConsoleKeyInfo key = Globals.MenuBuilder("[Y]es", "[N]o");

                if (key.Key == ConsoleKey.Y)
                {
                    Console.Write("Please provide a name for the login");
                    Console.Write(": ");
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
            Console.ReadKey();
            Globals.ClearConsole();
            MainMenu();
        }
    }
}
