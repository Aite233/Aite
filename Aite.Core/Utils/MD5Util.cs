using System.Security.Cryptography;
using System.Text;

namespace Aite.Core.Utils;

public class MD5Util
{
    public static string GetMD5Hash(string input)
    {
        using MD5 md5 = MD5.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);
        
        StringBuilder sb = new StringBuilder();
        foreach (byte b in hashBytes)
        {
            sb.Append(b.ToString("X2"));
        }
        
        return sb.ToString();
    }
    
    public static string GetMD5HashLower(string input)
    {
        return GetMD5Hash(input).ToLower();
    }
}