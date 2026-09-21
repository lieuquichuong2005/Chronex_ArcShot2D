using System;
using System.Text;

namespace Chronex.Services.Networking
{
    public static class RoomCodeGenerator
    {
        // Bỏ 0/O và 1/I để tránh người chơi đọc/nhập nhầm.
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int Length = 6;

        public static string Generate()
        {
            var random = new Random();
            var sb = new StringBuilder(Length);

            for (int i = 0; i < Length; i++)
            {
                sb.Append(Alphabet[random.Next(Alphabet.Length)]);
            }

            return sb.ToString();
        }
    }
}