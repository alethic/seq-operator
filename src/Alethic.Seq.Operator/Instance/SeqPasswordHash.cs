using System;
using System.Security.Cryptography;

namespace Alethic.Seq.Operator.Instance
{

    /// <summary>
    /// Copy of the 'PasswordHash' algorithm from Seq CLI.
    /// </summary>
    static class SeqPasswordHash
    {

        /// <summary>
        /// Calculates the hash of the password with the given salt.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        public static byte[] Calculate(string password, byte[] salt)
        {
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, 500000, HashAlgorithmName.SHA512, 64);
        }

        /// <summary>
        /// Creates a Base64 hash from the given hash and salt.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static string ToBase64(byte[] hash, byte[] salt)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));
            if (salt == null)
                throw new ArgumentNullException(nameof(salt));
            if (hash.Length >= byte.MaxValue)
                throw new ArgumentException("A maximum hash size of 254 bytes is supported.");

            var inArray = (Span<byte>)stackalloc byte[1 + hash.Length + salt.Length];
            inArray[0] = (byte)hash.Length;
            hash.CopyTo(inArray.Slice(1));
            salt.CopyTo(inArray.Slice(1 + hash.Length));
            return Convert.ToBase64String(inArray);
        }

        /// <summary>
        /// Converts a Base64 hash into its hash and salt components.
        /// </summary>
        /// <param name="base64"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static (byte[] hash, byte[] salt) FromBase64(string base64)
        {
            var array = base64 != null ? Convert.FromBase64String(base64) : throw new ArgumentNullException(nameof(base64));
            if (array.Length == 0)
                throw new ArgumentException("An empty string is not a valid Base64-encoded salted hash.");

            var num = array[0] != byte.MaxValue ? array[0] : throw new ArgumentException("The Base64 string does not encode a valid salted hash.");
            if (array.Length < 1 + num)
                throw new ArgumentException("The format of the encoded hash is invalid.");

            return (
                array[1..(1 + num)],
                array[(1 + num)..]
            );
        }

    }

}
