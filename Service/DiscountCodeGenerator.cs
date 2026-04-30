using System;
using System.Security.Cryptography;

namespace bookShop.Service;

public static class DiscountCodeGenerator
{
    // Avoid confusing characters (I, O, 0, 1)
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate()
    {
        Span<char> chars = stackalloc char[11];

        int pos = 0;
        for (int i = 0; i < 9; i++)
        {
            if (i == 3 || i == 6)
                chars[pos++] = '-';

            chars[pos++] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }

    public static bool IsValidFormat(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        code = code.Trim().ToUpperInvariant();
        if (code.Length != 11)
            return false;

        if (code[3] != '-' || code[7] != '-')
            return false;

        for (int i = 0; i < code.Length; i++)
        {
            if (i == 3 || i == 7)
                continue;

            if (Alphabet.IndexOf(code[i]) < 0)
                return false;
        }

        return true;
    }
}
