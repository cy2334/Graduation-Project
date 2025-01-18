using System.Security.Cryptography;

namespace GrpcTestService.Authentication;

public class RsaKeyHelper
{
    public RSA PrivateKey { get; set; }
    public RSA PublicKey { get; set; }

    public RsaKeyHelper(string  privateKeyPath, string publicKeyPath)
    {
        PrivateKey = LoadPrivateKey(privateKeyPath);
        PublicKey = LoadPublicKey(publicKeyPath);
    }

    private static RSA LoadPrivateKey(string privateKeyPath)
    {
        var privateKey = File.ReadAllBytes(privateKeyPath);
        var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKey, out _);
        return rsa;
    }

    private static RSA LoadPublicKey(string publicKeyPath)
    {
        var publickey = File.ReadAllBytes(publicKeyPath);
        var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(publickey, out _);
        return rsa;
    }
}