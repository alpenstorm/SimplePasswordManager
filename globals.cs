using System;
using System.IO;
using System.Text;

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

        public static ConsoleKeyInfo MenuBuilder(params string[] messages)
        {
            foreach (string i in messages)
            {
                Console.WriteLine(i);
            }
            Console.WriteLine(": ");
            return Console.ReadKey();
        }
    }
}
