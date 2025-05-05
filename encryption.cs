using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
            string fileName
            )
        {
            // creating the temporary password file and removing it after encrypting
            string tempFolder = Path.Combine(Globals.rootFolder, "temp");
            string tempFile = Path.Combine(tempFolder, $"{fileName}");

            Directory.CreateDirectory(tempFolder);
            File.WriteAllText(tempFile, encryptableString);

            Encrypt(tempFile, locationToSave, encryptionPassword);

            if (Globals.debugMode) { Console.WriteLine($"String saved to {locationToSave}"); }

            File.Delete(tempFile);
            Directory.Delete(tempFolder);
        }

        public static string DecryptString(string encryptionPassword, string fileToDecrypt)
        {
            string tempFolder = Path.Combine(Globals.rootFolder, "temp");
            string tempFile = Path.Combine(tempFolder, $"temp_file_{Guid.NewGuid().ToString()}.tmp"); // Unique file name

            try
            {
                // Ensure the temp folder exists
                Directory.CreateDirectory(tempFolder);

                if (Globals.debugMode)
                {
                    Console.WriteLine($"Decrypting {fileToDecrypt} to {tempFile}");
                }

                // Decrypt the file to the temporary location
                Decrypt(fileToDecrypt, tempFile, encryptionPassword);

                // Read the decrypted content
                if (!File.Exists(tempFile))
                {
                    throw new FileNotFoundException($"Temporary file was not created: {tempFile}");
                }

                string decryptedString = File.ReadAllText(tempFile);
                return decryptedString;
            }
            finally
            {
                // Clean up
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                    if (Directory.Exists(tempFolder) && !Directory.EnumerateFileSystemEntries(tempFolder).Any())
                    {
                        Directory.Delete(tempFolder);
                    }
                }
                catch (IOException ex)
                {
                    if (Globals.debugMode)
                    {
                        Console.WriteLine($"Warning: Could not delete temp files: {ex.Message}");
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
