using System;
using System.IO;

namespace AvatarNamecard.AvatarPackage
{
    /// <summary>Optional PNG metadata, bounded before any native image decoding.</summary>
    public static class AvatarPackageThumbnail
    {
        public const int MaxSide = 512;
        public const int MaxBytes = MaxSide * MaxSide * 4 + 65536;

        public static byte[] Decode(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return null;
            if (base64.Length > ((MaxBytes + 2) / 3) * 4)
                throw new InvalidDataException("Avatar thumbnail exceeds the size limit.");
            byte[] bytes;
            try { bytes = Convert.FromBase64String(base64); }
            catch (FormatException e) { throw new InvalidDataException("Invalid avatar thumbnail encoding.", e); }
            ValidatePng(bytes);
            return bytes;
        }

        public static void ValidatePng(byte[] bytes)
        {
            var signature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
            if (bytes == null || bytes.Length < 33 || bytes.Length > MaxBytes)
                throw new InvalidDataException("Invalid avatar thumbnail size.");
            for (var i = 0; i < signature.Length; i++)
                if (bytes[i] != signature[i]) throw new InvalidDataException("Avatar thumbnail must be a PNG image.");
            if (ReadUInt32(bytes, 8) != 13 || bytes[12] != 'I' || bytes[13] != 'H' || bytes[14] != 'D' || bytes[15] != 'R')
                throw new InvalidDataException("Invalid avatar thumbnail header.");
            var width = ReadUInt32(bytes, 16);
            var height = ReadUInt32(bytes, 20);
            if (width == 0 || height == 0 || width > MaxSide || height > MaxSide)
                throw new InvalidDataException("Avatar thumbnail dimensions exceed the limit.");
        }

        private static uint ReadUInt32(byte[] bytes, int offset) =>
            ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];
    }
}
