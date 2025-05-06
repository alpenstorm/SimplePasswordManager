using System.Security.Cryptography;

namespace SimplePasswordManager
{
    internal class Encryption
    {
        private const int keySize = 256;
        private const int blockSize = 128;
        private const int derivationIterations = 100000;

        public static void Encrypt(
            string inputFilePath, 
            string outputFilePath, 
            string password)
        {
            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException("Input file not found.", inputFilePath);
            }

            // open and read input file
            byte[] fileBytes = File.ReadAllBytes(inputFilePath);
            byte[] salt = GenerateRandomSalt();
            byte[] iv = GenerateRandomIV();

            // derive key
            byte[] key = DeriveKey(password, salt);
            
            // create AES encryptor and give it variables
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = keySize;
                aes.BlockSize = blockSize;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                // create file stream and give it variables
                using (var outputStream = new FileStream(
                    outputFilePath, 
                    FileMode.Create, 
                    FileAccess.Write))
                {
                    // write salt and IV to the beginning of a file
                    outputStream.Write(salt, 0, salt.Length);
                    outputStream.Write(iv, 0, iv.Length);

                    // create crypto stream and give it variables
                    using (var cryptoStream = new CryptoStream(
                        outputStream,
                        aes.CreateEncryptor(),
                        CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(fileBytes, 0, fileBytes.Length);
                        cryptoStream.FlushFinalBlock();
                    }
                }
            }
        }

        public static void Decrypt(string inputFilePath, string outputFilePath, string password)
        {
            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException("Input file not found.", inputFilePath);
            }

            using (var inputStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
            {
                byte[] salt = new byte[16];
                byte[] iv = new byte[16];
                inputStream.Read(salt, 0, salt.Length);
                inputStream.Read(iv, 0, iv.Length);

                byte[] key = DeriveKey(password, salt);

                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = keySize;
                    aes.BlockSize = blockSize;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = key;
                    aes.IV = iv;

                    using (var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                    {
                        using (var cryptoStream = new CryptoStream(outputStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            try
                            {
                                inputStream.CopyTo(cryptoStream);
                                cryptoStream.FlushFinalBlock();
                            }
                            catch (CryptographicException ex)
                            {
                                throw new CryptographicException("AES decryption failed; incorrect password or corrupted data.", ex);
                            }
                        }
                    }
                }
            }
        }

        public static void EncryptString(
            string encryptionPassword,
            string encryptableString,
            string locationToSave,
            string fileName)
        {
            // Use a unique temporary directory to avoid conflicts
            string tempFolder = Path.Combine(Globals.rootFolder, $"temp_{Guid.NewGuid().ToString()}");
            string tempFile = Path.Combine(tempFolder, fileName);

            try
            {
                // Create temporary directory
                Directory.CreateDirectory(tempFolder);

                // Write the string to a temporary file
                File.WriteAllText(tempFile, encryptableString);

                // Encrypt the temporary file
                Encrypt(tempFile, locationToSave, encryptionPassword);

                if (Globals.debugMode)
                {
                    Console.WriteLine($"String saved to {locationToSave}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Error: Access denied to {tempFolder}. Please ensure the application has permission to write to this directory.");
                throw;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error: Failed to create or write to temporary file at {tempFolder}. {ex.Message}");
                throw;
            }
            finally
            {
                // Clean up temporary files and directory
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                    if (Directory.Exists(tempFolder))
                    {
                        Directory.Delete(tempFolder);
                    }
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

        public static string DecryptString(string encryptionPassword, string fileToDecrypt)
        {
            string tempFolder = Path.Combine(Globals.rootFolder, $"temp_{Guid.NewGuid().ToString()}");
            string tempFile = Path.Combine(tempFolder, "temp_file");

            try
            {
                Directory.CreateDirectory(tempFolder);
                Decrypt(fileToDecrypt, tempFile, encryptionPassword);
                string decryptedString = File.ReadAllText(tempFile);
                return decryptedString;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Error: Access denied to {tempFolder}. Please ensure the application has permission to write to this directory.");
                throw;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                    if (Directory.Exists(tempFolder))
                    {
                        Directory.Delete(tempFolder);
                    }
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

        public static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            Random random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // generate a random salt
        private static byte[] GenerateRandomSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        // generate a random IV
        private static byte[] GenerateRandomIV()
        {
            byte[] iv = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }
            return iv;
        }

        // derive the key from the password
        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                derivationIterations,
                HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(keySize / 8);
            }
        }
    }
}
