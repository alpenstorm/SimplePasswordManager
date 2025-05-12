using System.Security.Cryptography;
using System.IO;

namespace SimplePasswordManager
{
    internal class Encryption
    {
        private const int keySize = 256;
        private const int blockSize = 128;
        private const int derivationIterations = 100000;

        /// <summary>
        /// Encrypts a file using AES encryption with the provided password.
        /// </summary>
        /// <param name="inputFilePath">Path to the input file to encrypt.</param>
        /// <param name="outputFilePath">Path where the encrypted file will be saved.</param>
        /// <param name="password">Password used for encryption.</param>
        /// <exception cref="FileNotFoundException">Thrown if the input file does not exist.</exception>
        /// <exception cref="CryptographicException">Thrown if encryption fails.</exception>
        /// <exception cref="IOException">Thrown if file operations fail.</exception>
        public static void Encrypt(string inputFilePath, string outputFilePath, string password)
        {
            try
            {
                if (!File.Exists(inputFilePath))
                {
                    throw new FileNotFoundException("Input file not found.", inputFilePath);
                }

                byte[] fileBytes = File.ReadAllBytes(inputFilePath);
                byte[] salt = GenerateRandomSalt();
                byte[] iv = GenerateRandomIV();
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
                        outputStream.Write(salt, 0, salt.Length);
                        outputStream.Write(iv, 0, iv.Length);

                        using (var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(fileBytes, 0, fileBytes.Length);
                            cryptoStream.FlushFinalBlock();
                        }
                    }
                }
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Encrypted file from {inputFilePath} to {outputFilePath}");
                }
            }
            catch (FileNotFoundException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] FileNotFound in Encrypt: {ex.Message}");
                }
                throw;
            }
            catch (IOException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] IOException in Encrypt: {ex.Message}");
                }
                throw;
            }
            catch (CryptographicException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] CryptographicException in Encrypt: {ex.Message}");
                }
                throw;
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Unexpected error in Encrypt: {ex.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Decrypts a file using AES decryption with the provided password.
        /// </summary>
        /// <param name="inputFilePath">Path to the encrypted input file.</param>
        /// <param name="outputFilePath">Path where the decrypted file will be saved.</param>
        /// <param name="password">Password used for decryption.</param>
        /// <exception cref="FileNotFoundException">Thrown if the input file does not exist.</exception>
        /// <exception cref="CryptographicException">Thrown if decryption fails (e.g., wrong password).</exception>
        /// <exception cref="IOException">Thrown if file operations fail.</exception>
        public static void Decrypt(string inputFilePath, string outputFilePath, string password)
        {
            try
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
                                inputStream.CopyTo(cryptoStream);
                                cryptoStream.FlushFinalBlock();
                            }
                        }
                    }
                }
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Decrypted file from {inputFilePath} to {outputFilePath}");
                }
            }
            catch (FileNotFoundException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] FileNotFound in Decrypt: {ex.Message}");
                }
                throw;
            }
            catch (IOException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] IOException in Decrypt: {ex.Message}");
                }
                throw;
            }
            catch (CryptographicException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] CryptographicException in Decrypt: {ex.Message}");
                }
                throw new CryptographicException("AES decryption failed; incorrect password or corrupted data.", ex);
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Unexpected error in Decrypt: {ex.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Encrypts a string and saves it to a file.
        /// </summary>
        /// <param name="encryptionPassword">Password used for encryption.</param>
        /// <param name="encryptableString">String to encrypt.</param>
        /// <param name="locationToSave">Path where the encrypted file will be saved.</param>
        /// <param name="fileName">Name of the temporary file.</param>
        /// <exception cref="UnauthorizedAccessException">Thrown if access to the directory is denied.</exception>
        /// <exception cref="IOException">Thrown if file operations fail.</exception>
        public static void EncryptString(string encryptionPassword, string encryptableString, string locationToSave, string fileName)
        {
            string tempFolder = Path.Combine(Globals.rootFolder, $"temp_{Guid.NewGuid().ToString()}");
            string tempFile = Path.Combine(tempFolder, fileName);

            try
            {
                Directory.CreateDirectory(tempFolder);
                File.WriteAllText(tempFile, encryptableString);
                Encrypt(tempFile, locationToSave, encryptionPassword);

                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] String encrypted and saved to {locationToSave}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Error: Access denied to {tempFolder}. Please ensure the application has permission to write to this directory.");
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] UnauthorizedAccess in EncryptString: {ex.Message}");
                }
                throw;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error: Failed to create or write to temporary file at {tempFolder}. {ex.Message}");
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] IOException in EncryptString: {ex.Message}");
                }
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

        /// <summary>
        /// Decrypts a file and returns the decrypted string.
        /// </summary>
        /// <param name="encryptionPassword">Password used for decryption.</param>
        /// <param name="fileToDecrypt">Path to the encrypted file.</param>
        /// <returns>The decrypted string.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if access to the directory is denied.</exception>
        /// <exception cref="IOException">Thrown if file operations fail.</exception>
        /// <exception cref="CryptographicException">Thrown if decryption fails.</exception>
        public static string DecryptString(string encryptionPassword, string fileToDecrypt)
        {
            string tempFolder = Path.Combine(Globals.rootFolder, $"temp_{Guid.NewGuid().ToString()}");
            string tempFile = Path.Combine(tempFolder, "temp_file");

            try
            {
                Directory.CreateDirectory(tempFolder);
                Decrypt(fileToDecrypt, tempFile, encryptionPassword);
                string decryptedString = File.ReadAllText(tempFile);
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Decrypted string from {fileToDecrypt}");
                }
                return decryptedString;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Error: Access denied to {tempFolder}. Please ensure the application has permission to write to this directory.");
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] UnauthorizedAccess in DecryptString: {ex.Message}");
                }
                throw;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error: Failed to read or write to temporary file at {tempFolder}. {ex.Message}");
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] IOException in DecryptString: {ex.Message}");
                }
                throw;
            }
            catch (CryptographicException ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] CryptographicException in DecryptString: {ex.Message}");
                }
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

        /// <summary>
        /// Generates a random string of specified length.
        /// </summary>
        /// <param name="length">Length of the random string.</param>
        /// <returns>A random string containing letters, numbers, and special characters.</returns>
        public static string GenerateRandomString(int length)
        {
            try
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
                Random random = new Random();
                string result = new string(Enumerable.Repeat(chars, length)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Generated random string of length {length}");
                }
                return result;
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in GenerateRandomString: {ex.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Generates a random salt for encryption.
        /// </summary>
        /// <returns>A 16-byte random salt.</returns>
        private static byte[] GenerateRandomSalt()
        {
            try
            {
                byte[] salt = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }
                if (Globals.debugMode)
                {
                    Console.WriteLine("[DEBUG] Generated random salt.");
                }
                return salt;
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in GenerateRandomSalt: {ex.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Generates a random initialization vector (IV) for encryption.
        /// </summary>
        /// <returns>A 16-byte random IV.</returns>
        private static byte[] GenerateRandomIV()
        {
            try
            {
                byte[] iv = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv);
                }
                if (Globals.debugMode)
                {
                    Console.WriteLine("[DEBUG] Generated random IV.");
                }
                return iv;
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in GenerateRandomIV: {ex.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Derives an encryption key from a password and salt using PBKDF2.
        /// </summary>
        /// <param name="password">The password to derive the key from.</param>
        /// <param name="salt">The salt used in key derivation.</param>
        /// <returns>A derived key of length keySize/8 bytes.</returns>
        private static byte[] DeriveKey(string password, byte[] salt)
        {
            try
            {
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, derivationIterations, HashAlgorithmName.SHA256))
                {
                    byte[] key = pbkdf2.GetBytes(keySize / 8);
                    if (Globals.debugMode)
                    {
                        Console.WriteLine("[DEBUG] Derived encryption key.");
                    }
                    return key;
                }
            }
            catch (Exception ex)
            {
                if (Globals.debugMode)
                {
                    Console.WriteLine($"[DEBUG] Error in DeriveKey: {ex.Message}");
                }
                throw;
            }
        }
    }
}