using System.Text;

namespace Aite.Core.Utils;

public class RC4Cipher
{   
    private readonly byte[] _key;
    
    public RC4Cipher(string key = "RC4Key")
    {
        // 使用UTF-8编码的密钥
        _key = Encoding.UTF8.GetBytes(key);
    }
    
    public string Encrypt(string plaintext)
    {
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] ciphertextBytes = RC4(plaintextBytes, _key);
        return BytesToHex(ciphertextBytes);
    }
    
    public string Decrypt(string ciphertext)
    {
        byte[] ciphertextBytes = HexToBytes(ciphertext);
        byte[] plaintextBytes = RC4(ciphertextBytes, _key);
        return Encoding.UTF8.GetString(plaintextBytes);
    }
    
    public string Decrypt(byte[] ciphertextBytes)
    {
        byte[] plaintextBytes = RC4(ciphertextBytes, _key);
        return Encoding.UTF8.GetString(plaintextBytes);
    }
    
    private byte[] RC4(byte[] data, byte[] key)
    {
        int[] S = new int[256];
        int i, j = 0, t;
        
        // 初始化S盒
        for (i = 0; i < 256; i++)
        {
            S[i] = i;
        }
        
        // 打乱S盒
        for (i = 0; i < 256; i++)
        {
            j = (j + S[i] + key[i % key.Length]) % 256;
            (S[i], S[j]) = (S[j], S[i]);
        }
        
        // 生成密钥流并加密/解密
        byte[] result = new byte[data.Length];
        i = j = 0;
        for (int k = 0; k < data.Length; k++)
        {
            i = (i + 1) % 256;
            j = (j + S[i]) % 256;
            (S[i], S[j]) = (S[j], S[i]);
            t = (S[i] + S[j]) % 256;
            result[k] = (byte)(data[k] ^ S[t]);
        }
        
        return result;
    }
    
    private byte[] HexToBytes(string hex)
    {
        if (hex.Length % 2 != 0)
        {
            throw new ArgumentException("Hex string must have even length");
        }
        
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }
    
    private string BytesToHex(byte[] bytes)
    {
        StringBuilder sb = new StringBuilder();
        foreach (byte b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}