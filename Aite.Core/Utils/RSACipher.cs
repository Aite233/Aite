using System.Security.Cryptography;
using System.Text;

namespace Aite.Core.Utils;

public class RSACipher
{
    private readonly RSA _rsa;
    
    private const string DefaultPublicKeyPem = "RSAKey";
    
    public RSACipher()
    {
        _rsa = RSA.Create();
        _rsa.ImportFromPem(DefaultPublicKeyPem);
    }
    
    public RSACipher(int keySize)
    {
        _rsa = RSA.Create(keySize);
    }
    
    public RSACipher(string publicKeyXml)
    {
        _rsa = RSA.Create();
        _rsa.FromXmlString(publicKeyXml);
    }
    
    public RSACipher(string publicKeyXml, string privateKeyXml)
    {
        _rsa = RSA.Create();
        _rsa.FromXmlString(privateKeyXml);
    }
    
    public RSACipher(string publicKeyPem, bool isPem)
    {
        _rsa = RSA.Create();
        if (isPem)
        {
            _rsa.ImportFromPem(publicKeyPem);
        }
        else
        {
            _rsa.FromXmlString(publicKeyPem);
        }
    }
    
    public string GetPublicKeyXml()
    {
        return _rsa.ToXmlString(false);
    }
    
    public string GetPrivateKeyXml()
    {
        return _rsa.ToXmlString(true);
    }
    
    public string Encrypt(string plaintext)
    {
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] ciphertextBytes = _rsa.Encrypt(plaintextBytes, RSAEncryptionPadding.Pkcs1);
        return BytesToHex(ciphertextBytes);
    }
    
    public string Decrypt(string ciphertext)
    {
        byte[] ciphertextBytes = HexToBytes(ciphertext);
        byte[] plaintextBytes = _rsa.Decrypt(ciphertextBytes, RSAEncryptionPadding.Pkcs1);
        return Encoding.UTF8.GetString(plaintextBytes);
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
            sb.Append(b.ToString("X2"));
        }
        return sb.ToString();
    }
}