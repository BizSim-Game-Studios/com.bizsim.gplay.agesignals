// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BizSim.GPlay.AgeSignals
{
    /// <summary>
    /// AES-256-CBC encrypted cache provider that stores restriction flags in PlayerPrefs.
    /// Key is derived via PBKDF2 from a stable per-install GUID (stored in PlayerPrefs)
    /// and a configurable salt. IV is prepended to ciphertext and stored as Base64.
    /// If decryption fails (e.g., data corruption), returns null to force a fresh API call.
    ///
    /// <b>Why not <c>SystemInfo.deviceUniqueIdentifier</c>?</b>
    /// On Android 10+ the device ID can change after factory reset or is not guaranteed
    /// to be stable across app reinstalls. A per-install GUID stored in PlayerPrefs
    /// is deterministic for the lifetime of the install and survives OS updates.
    /// On reinstall, both the GUID and the encrypted cache are lost — which is correct
    /// because the cache should be invalidated on reinstall anyway.
    /// </summary>
    public class EncryptedPlayerPrefsCacheProvider : IAgeSignalsCacheProvider
    {
        private const string PREFS_KEY = "AgeSignals_Cache_Enc";
        private const string KEY_ID_PREFS_KEY = "AgeSignals_KeyId";
        private const int KEY_SIZE = 256; // bits
        private const int BLOCK_SIZE = 128; // bits
        // 1000 iterations is sufficient for local PlayerPrefs encryption where the
        // "password" is a random GUID (128 bits of entropy). The 10,000+ OWASP
        // recommendation targets weak user-chosen passwords on server-side stores.
        // On low-end Android devices, 10,000 iterations caused 20-100ms stalls in Awake().
        private const int PBKDF2_ITERATIONS = 1000;

        private readonly byte[] _salt;
        private byte[] _cachedKey;

        /// <summary>
        /// Initializes a new instance with an app-specific default salt.
        /// Combines a fixed prefix with <see cref="Application.identifier"/> (bundle ID)
        /// so that different apps using this package produce different encryption keys.
        /// </summary>
        public EncryptedPlayerPrefsCacheProvider()
            : this("BizSimAgeSignals_" + Application.identifier) { }

        /// <summary>
        /// Initializes a new instance with a custom salt for PBKDF2 key derivation.
        /// </summary>
        /// <param name="salt">UTF-8 string used as the PBKDF2 salt. Must be consistent across sessions.</param>
        public EncryptedPlayerPrefsCacheProvider(string salt)
        {
            _salt = Encoding.UTF8.GetBytes(salt);
        }

        /// <inheritdoc />
        public AgeRestrictionFlags Load()
        {
            string stored = PlayerPrefs.GetString(PREFS_KEY, "");
            if (string.IsNullOrEmpty(stored)) return null;

            try
            {
                string json = Decrypt(stored);
                if (string.IsNullOrEmpty(json)) return null;
                return JsonUtility.FromJson<AgeRestrictionFlags>(json);
            }
            catch
            {
                // Decryption failed — corrupt data or key changed
                return null;
            }
        }

        /// <inheritdoc />
        public void Save(AgeRestrictionFlags flags)
        {
            string json = JsonUtility.ToJson(flags);
            string encrypted = Encrypt(json);
            PlayerPrefs.SetString(PREFS_KEY, encrypted);
            PlayerPrefs.Save();
        }

        /// <inheritdoc />
        public void Clear()
        {
            PlayerPrefs.DeleteKey(PREFS_KEY);
            // Note: KEY_ID_PREFS_KEY is intentionally NOT deleted here.
            // The key ID should persist for the lifetime of the install so that
            // future Save() calls can still encrypt with the same key.
            // It is only lost on app reinstall (PlayerPrefs cleared by OS).
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Derives a 256-bit AES key from the per-install GUID using PBKDF2-SHA256.
        /// The result is cached for the lifetime of this provider instance to avoid
        /// redundant KDF work on repeated Load/Save calls within the same session.
        /// </summary>
        private byte[] DeriveKey()
        {
            if (_cachedKey != null) return _cachedKey;

            string password = GetOrCreateKeyId();
            using var kdf = new Rfc2898DeriveBytes(password, _salt, PBKDF2_ITERATIONS, HashAlgorithmName.SHA256);
            _cachedKey = kdf.GetBytes(KEY_SIZE / 8);
            return _cachedKey;
        }

        /// <summary>
        /// Returns a stable per-install GUID used as the PBKDF2 password.
        /// Created once on first use and persisted in PlayerPrefs.
        /// Lost on app reinstall — which correctly invalidates the encrypted cache.
        /// </summary>
        private static string GetOrCreateKeyId()
        {
            string keyId = PlayerPrefs.GetString(KEY_ID_PREFS_KEY, "");
            if (!string.IsNullOrEmpty(keyId))
                return keyId;

            keyId = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(KEY_ID_PREFS_KEY, keyId);
            PlayerPrefs.Save();
            return keyId;
        }

        /// <summary>
        /// Encrypts the given plaintext using AES-256-CBC with a random IV.
        /// Returns a Base64 string containing the IV prepended to the ciphertext.
        /// </summary>
        /// <param name="plaintext">UTF-8 string to encrypt.</param>
        /// <returns>Base64-encoded string of <c>[IV || ciphertext]</c>.</returns>
        private string Encrypt(string plaintext)
        {
            byte[] key = DeriveKey();
            using var aes = Aes.Create();
            aes.KeySize = KEY_SIZE;
            aes.BlockSize = BLOCK_SIZE;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] ciphertext = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

            // Prepend IV to ciphertext
            byte[] result = new byte[aes.IV.Length + ciphertext.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(ciphertext, 0, result, aes.IV.Length, ciphertext.Length);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypts a Base64-encoded <c>[IV || ciphertext]</c> string back to plaintext.
        /// Returns <c>null</c> if the input is too short to contain a valid IV.
        /// </summary>
        /// <param name="base64">Base64-encoded encrypted payload produced by <see cref="Encrypt"/>.</param>
        /// <returns>Decrypted UTF-8 string, or <c>null</c> on failure.</returns>
        private string Decrypt(string base64)
        {
            byte[] key = DeriveKey();
            byte[] combined = Convert.FromBase64String(base64);

            int ivLength = BLOCK_SIZE / 8;
            if (combined.Length < ivLength) return null;

            byte[] iv = new byte[ivLength];
            byte[] ciphertext = new byte[combined.Length - ivLength];
            Buffer.BlockCopy(combined, 0, iv, 0, ivLength);
            Buffer.BlockCopy(combined, ivLength, ciphertext, 0, ciphertext.Length);

            using var aes = Aes.Create();
            aes.KeySize = KEY_SIZE;
            aes.BlockSize = BLOCK_SIZE;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            byte[] plaintextBytes = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
            return Encoding.UTF8.GetString(plaintextBytes);
        }
    }
}
