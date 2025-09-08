using System.Security.Cryptography;
using System.Text;

public static class HashUtil
{
    public static string MD5(string input)
    {
        if (input == null) return string.Empty;
        using (var md5 = MD5CryptoServiceProvider.Create())
        {
            byte[] data = Encoding.UTF8.GetBytes(input);
            byte[] hash = md5.ComputeHash(data);
            var sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }
    }
}