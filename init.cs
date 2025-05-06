using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Text.Json.Serialization.Metadata;

namespace SimplePasswordManager
{
    internal class Init
    {
        public static void FirstLoad()
        {
            // set first_load to false
            Globals.isFirstLoad = false;

            // create default config file JSON
            JsonObject j = new JsonObject
            {
                ["config"] = new JsonObject
                {
                    ["first_load"] = Globals.isFirstLoad,
                    ["debug_mode"] = Globals.debugMode,
                    ["timeout"] = Globals.timeout,
                },
                ["version"] = Globals.version,
            };

            // serialize it
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };
            string jsonContent = j.ToJsonString(options);

            Console.WriteLine("Detected first load, creating initial config...");
            Console.WriteLine("Config created: ");
            Console.WriteLine(jsonContent);

            // save it
            File.WriteAllText(Globals.configFile, jsonContent);
            Console.Write("Press any key to continue: ");
            Console.ReadKey(true);
            Globals.ClearConsole();
            
            // start the create password dialog
            Console.WriteLine("Please create a password.");
            Globals.password = CreatePasswordDialog();
            Encryption.EncryptString(
                Globals.password, 
                Globals.password, 
                Globals.passwordFile, 
                "password");

            // create a recovery code
            Globals.recoveryCode = Encryption.GenerateRandomString(24);
            
            Console.WriteLine("Your recovery code is: ");
            Console.WriteLine(Globals.recoveryCode);
            Console.WriteLine("Please save it somewhere you will remember to use in case you forget your password");
            Console.WriteLine("Press any key to continue... ");
            Console.ReadKey(true);

            // save it
            Encryption.EncryptString(
                Globals.recoveryCode, 
                Globals.recoveryCode, 
                Globals.recoveryCodeFile, 
                "recovery_code");

            Globals.ClearConsole();
            Unlock();
        }

        public static void LoadConfig()
        {
            AssignFolderLocations();
            
            // we're only assigning the config file path here because this runs before FirstLoad()
            // checking to see if we have a config before trying to create one
            // so if we don't, we'll just use this path to create one
            AssignFileLocations();
          
            if (File.Exists(Globals.configFile))
            {
                // load the JSON file and deserialize it to get the variables
                string jsonString = File.ReadAllText(Globals.configFile);
                SaveFile saveFile = JsonSerializer.Deserialize<SaveFile>(jsonString);

                // assign the variables from the JSON file

                // this should always be false when we load it
                // otherwise the base file doesn't exist
                Globals.isFirstLoad = saveFile.config.isFirstLoad;
                Globals.timeout = saveFile.config.timeout;
                Globals.debugMode = saveFile.config.debugMode;
            }
        }

        private static string CreatePasswordDialog()
        {
            while (true)
            {
                // first prompt for password
                Console.Write("Enter new password: ");
                string firstInput = Globals.ReadPassword();
                Console.WriteLine();

                // validate first input
                if (firstInput == "" || firstInput.Length < 8)
                {
                    Console.WriteLine("Password is too weak, please create one at least 8 characters long.");
                    continue; // retry
                }

                // prompt for confirmation
                Console.Write("Confirm password: ");
                string secondInput = Globals.ReadPassword();
                Console.WriteLine();

                // check if passwords match
                if (firstInput != secondInput)
                {
                    Console.WriteLine("Passwords do not match. Please try again.");
                    continue; // retry
                }

                return firstInput; // valid and confirmed password
            }
        }

        public static void Lock()
        {
            Globals.ClearConsole();
            Unlock();
        }

        public static void Unlock()
        {
            while (true)
            {
                // prompt to unlock the password manager
                ConsoleKeyInfo key = Globals.MenuBuilder("[P]assword", "[R]ecovery code");

                if (key.Key == ConsoleKey.P)
                {
                    Console.WriteLine();
                    Console.Write("Enter Password: ");
                    string passwordInput = Globals.ReadPassword();
                    if (VerifyPassword(passwordInput, Globals.passwordFile)) 
                    {
                        Globals.password = passwordInput;
                        Globals.ClearConsole();
                        return; 
                    }

                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine("Wrong password! Press any key to try again.");
                        Console.ReadKey(true);
                        Globals.ClearConsole();
                        continue;
                    }
                }

                else if (key.Key == ConsoleKey.R)
                {
                    Console.WriteLine();
                    Console.Write("Enter recovery code: ");
                    
                    if (VerifyPassword(Globals.ReadPassword(), Globals.recoveryCodeFile))
                    {
                        Console.WriteLine();
                        Console.WriteLine("You've entered a recovery code, please reset your password before continuing!");
                        Globals.password = CreatePasswordDialog();
                        Globals.recoveryCode = Encryption.GenerateRandomString(24);

                        // these should exist, but check anyways
                        if (File.Exists(Globals.passwordFile)) { File.Delete(Globals.passwordFile); }
                        if (File.Exists(Globals.recoveryCodeFile)) { File.Delete(Globals.recoveryCodeFile); }

                        Encryption.EncryptString(
                            Globals.password, 
                            Globals.password, 
                            Globals.passwordFile,
                            "password");
                        Encryption.EncryptString(
                            Globals.recoveryCode, 
                            Globals.recoveryCode, 
                            Globals.recoveryCodeFile,
                            "password");

                        Console.WriteLine("Your new recovery code is: ");
                        Console.WriteLine(Globals.recoveryCode);
                        Console.WriteLine("Please save it somewhere you will remember to use in case you forget your password");
                        Console.WriteLine("Press any key to continue... ");
                        Console.ReadKey(true);
                        Globals.ClearConsole();
                        
                        return;
                    }
                }

                else
                {
                    Globals.ClearConsole();
                    continue;
                }
            }
        }

        private static bool VerifyPassword(string passwordToCheck, string fileToCheck)
        {
            try
            {
                string decrypted = Encryption.DecryptString(passwordToCheck, fileToCheck);
                return decrypted == passwordToCheck;
            }
            catch (CryptographicException)
            {
                return false; // Incorrect password
            }
            catch (FileNotFoundException)
            {
                return false; // File missing
            }
        }

        private static void AssignFolderLocations()
        {
            // create list
            List<string> folderLocations = new List<string>();

            // assign folder paths
            Globals.rootFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SimplePasswordManager");
            Globals.configFolder = Path.Combine(Globals.rootFolder, "config");
            Globals.resourcesFolder = Path.Combine(Globals.rootFolder, "resources");
            Globals.passwordFolder = Path.Combine(Globals.resourcesFolder, "passwords");
            Globals.loginsFolder = Path.Combine(Globals.resourcesFolder, "logins");

            // add them to the list
            folderLocations.Add(Globals.rootFolder);
            folderLocations.Add(Globals.configFolder);
            folderLocations.Add(Globals.resourcesFolder);
            folderLocations.Add(Globals.passwordFolder);
            folderLocations.Add(Globals.loginsFolder);

            // find if base directories are empty
            foreach (string i in folderLocations)
            {
                if (!Directory.Exists(i))
                {
                    Directory.CreateDirectory(i);
                    if (Globals.debugMode) { Console.WriteLine($"Created directory: {i}"); }
                }
                else
                {
                    if (Globals.debugMode) { Console.WriteLine($"Directory exists: {i}"); }
                }
            }
        }

        private static void AssignFileLocations()
        {
            Globals.configFile = Path.Combine(Globals.configFolder, "config.json");
            Globals.passwordFile = Path.Combine(Globals.passwordFolder, "password");
            Globals.recoveryCodeFile = Path.Combine(Globals.passwordFolder, "recovery_code");

            // add files from logins folder
            Globals.loginFiles = new List<string>(Directory.GetFiles(Globals.loginsFolder));
        }
    }
}