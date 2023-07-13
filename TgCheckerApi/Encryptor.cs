using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace TgCheckerApi
{
    public static class Encryptor
    {
        private static readonly byte[] EncryptionKey = { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10 };

        public static string EncryptToken(string token, string userId, string username)
        {
            byte[] encryptedBytes;

            using (Aes aes = Aes.Create())
            {
                aes.Key = EncryptionKey;
                aes.GenerateIV();

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
                byte[] userIdBytes = Encoding.UTF8.GetBytes(userId);
                byte[] usernameBytes = Encoding.UTF8.GetBytes(username);

                byte[] encryptedTokenBytes = encryptor.TransformFinalBlock(tokenBytes, 0, tokenBytes.Length);
                byte[] encryptedUserIdBytes = encryptor.TransformFinalBlock(userIdBytes, 0, userIdBytes.Length);
                byte[] encryptedUsernameBytes = encryptor.TransformFinalBlock(usernameBytes, 0, usernameBytes.Length);

                encryptedBytes = CombineArrays(aes.IV, encryptedTokenBytes, encryptedUserIdBytes, encryptedUsernameBytes);
            }

            return Convert.ToBase64String(encryptedBytes);
        }

        public static Tuple<string, string, string> DecryptToken(string encryptedToken)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedToken);
            byte[] iv = new byte[16];
            byte[] encryptedTokenBytes = new byte[encryptedBytes.Length - 48];
            byte[] encryptedUserIdBytes = new byte[16];
            byte[] encryptedUsernameBytes = new byte[16];

            Buffer.BlockCopy(encryptedBytes, 0, iv, 0, 16);
            Buffer.BlockCopy(encryptedBytes, 16, encryptedTokenBytes, 0, encryptedTokenBytes.Length);
            Buffer.BlockCopy(encryptedBytes, 16 + encryptedTokenBytes.Length, encryptedUserIdBytes, 0, 16);
            Buffer.BlockCopy(encryptedBytes, 32 + encryptedTokenBytes.Length, encryptedUsernameBytes, 0, 16);

            string token;
            string userId;
            string username;

            using (Aes aes = Aes.Create())
            {
                aes.Key = EncryptionKey;
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                byte[] decryptedTokenBytes = decryptor.TransformFinalBlock(encryptedTokenBytes, 0, encryptedTokenBytes.Length);
                byte[] decryptedUserIdBytes = decryptor.TransformFinalBlock(encryptedUserIdBytes, 0, encryptedUserIdBytes.Length);
                byte[] decryptedUsernameBytes = decryptor.TransformFinalBlock(encryptedUsernameBytes, 0, encryptedUsernameBytes.Length);

                token = Encoding.UTF8.GetString(decryptedTokenBytes);
                userId = Encoding.UTF8.GetString(decryptedUserIdBytes);
                username = Encoding.UTF8.GetString(decryptedUsernameBytes);
            }

            return new Tuple<string, string, string>(token, userId, username);
        }

        private static byte[] CombineArrays(params byte[][] arrays)
        {
            int totalLength = arrays.Sum(a => a.Length);
            byte[] combinedArray = new byte[totalLength];

            int currentIndex = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, combinedArray, currentIndex, array.Length);
                currentIndex += array.Length;
            }

            return combinedArray;
        }
    }
}
