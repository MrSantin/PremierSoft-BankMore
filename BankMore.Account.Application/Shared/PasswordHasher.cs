using Konscious.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace BankMore.Account.Application.Shared;

public class PasswordHasher : IPasswordHasher
{
    private readonly string _pepper;
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private readonly int _parallelism;
    private readonly int _memorySize;
    private readonly int _iterations;

    public PasswordHasher(IConfiguration configuration)
    {
        _pepper = configuration["SecuritySettings:PasswordPepper"] ?? throw new ArgumentNullException("Pepper não configurado.");
        _parallelism = configuration.GetValue<int>("SecuritySettings:Argon2:Parallelism", 1);
        _memorySize = configuration.GetValue<int>("SecuritySettings:Argon2:MemorySize", 16384);
        _iterations = configuration.GetValue<int>("SecuritySettings:Argon2:Iterations", 3);
    }

    public (string Hash, string Salt) HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        var saltString = Convert.ToBase64String(saltBytes);

        var hashBytes = GerarHash(password, saltBytes);
        var hashString = Convert.ToBase64String(hashBytes);

        return (hashString, saltString);
    }

    public bool VerificarSenha(string password, string salt, string hashedPassword)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var hashBytes = Convert.FromBase64String(hashedPassword);

        var newHash = GerarHash(password, saltBytes);

        // Comparação segura contra ataques de tempo (Timing Attacks)
        return CryptographicOperations.FixedTimeEquals(hashBytes, newHash);
    }

    private byte[] GerarHash(string password, byte[] salt)
    {
        // Combinamos a senha com o Pepper do appsettings
        var passwordWithPepper = string.Concat(password, _pepper);

        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(passwordWithPepper));

        argon2.Salt = salt;
        argon2.DegreeOfParallelism = _parallelism;
        argon2.MemorySize = _memorySize;
        argon2.Iterations = _iterations;

        return argon2.GetBytes(HashSize);
    }

}
