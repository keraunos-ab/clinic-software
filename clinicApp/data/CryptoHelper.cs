using System;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace clinicApp.data
{
    internal static class CryptoHelper
    {
        private const string Passphrase = "blaze123";
        private static readonly byte[] KeySalt = Encoding.UTF8.GetBytes("clinicApp_argon2_key_derivation_salt_v1");
        private static readonly byte[] PasswordSalt = Encoding.UTF8.GetBytes("clinicApp_argon2_password_hash_salt_v1");

        private static byte[]? _cachedKey;

        private static byte[] DeriveKey()
        {
            if (_cachedKey != null) return _cachedKey;

            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(Passphrase))
            {
                Salt = KeySalt,
                DegreeOfParallelism = 2,
                MemorySize = 65536,
                Iterations = 3
            };
            _cachedKey = argon2.GetBytes(32);
            return _cachedKey;
        }

        public static string Encrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                return plaintext;

            var key = DeriveKey();
            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
            RandomNumberGenerator.Fill(nonce);

            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize];

            using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

            return Convert.ToBase64String(result);
        }

        public static string Decrypt(string encryptedBase64)
        {
            if (string.IsNullOrEmpty(encryptedBase64))
                return encryptedBase64;

            var key = DeriveKey();
            var data = Convert.FromBase64String(encryptedBase64);

            var nonceSize = AesGcm.NonceByteSizes.MaxSize;
            var tagSize = AesGcm.TagByteSizes.MaxSize;

            var nonce = new byte[nonceSize];
            var tag = new byte[tagSize];
            var ciphertext = new byte[data.Length - nonceSize - tagSize];

            Buffer.BlockCopy(data, 0, nonce, 0, nonceSize);
            Buffer.BlockCopy(data, nonceSize, tag, 0, tagSize);
            Buffer.BlockCopy(data, nonceSize + tagSize, ciphertext, 0, ciphertext.Length);

            var plaintext = new byte[ciphertext.Length];
            using var aes = new AesGcm(key, tagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }

        public static string? EncryptOrNull(string? value)
        {
            return string.IsNullOrEmpty(value) ? null : Encrypt(value);
        }

        public static string SafeDecrypt(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return value ?? string.Empty;

            try
            {
                return Decrypt(value);
            }
            catch
            {
                return value;
            }
        }

        public static string? SafeDecryptOrNull(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            try
            {
                return Decrypt(value);
            }
            catch
            {
                return value;
            }
        }

        public static string HashPassword(string password)
        {
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = PasswordSalt,
                DegreeOfParallelism = 2,
                MemorySize = 65536,
                Iterations = 3
            };
            return Convert.ToBase64String(argon2.GetBytes(32));
        }

        public static byte[] EncryptBytes(byte[] plainBytes)
        {
            if (plainBytes == null || plainBytes.Length == 0)
                return plainBytes;

            var key = DeriveKey();
            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
            RandomNumberGenerator.Fill(nonce);

            var ciphertext = new byte[plainBytes.Length];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize];

            using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
            aes.Encrypt(nonce, plainBytes, ciphertext, tag);

            var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

            return result;
        }

        public static byte[] DecryptBytes(byte[] encryptedBytes)
        {
            if (encryptedBytes == null || encryptedBytes.Length == 0)
                return encryptedBytes;

            var key = DeriveKey();

            var nonceSize = AesGcm.NonceByteSizes.MaxSize;
            var tagSize = AesGcm.TagByteSizes.MaxSize;

            var nonce = new byte[nonceSize];
            var tag = new byte[tagSize];
            var ciphertext = new byte[encryptedBytes.Length - nonceSize - tagSize];

            Buffer.BlockCopy(encryptedBytes, 0, nonce, 0, nonceSize);
            Buffer.BlockCopy(encryptedBytes, nonceSize, tag, 0, tagSize);
            Buffer.BlockCopy(encryptedBytes, nonceSize + tagSize, ciphertext, 0, ciphertext.Length);

            var plaintext = new byte[ciphertext.Length];
            using var aes = new AesGcm(key, tagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return plaintext;
        }

        public static byte[]? SafeDecryptBytes(byte[]? value)
        {
            if (value == null || value.Length == 0)
                return value;

            try
            {
                return DecryptBytes(value);
            }
            catch
            {
                return value;
            }
        }
    }
}
