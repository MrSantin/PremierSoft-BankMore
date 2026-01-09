
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace BankMore.Account.Infrastructure.Security;

public static class RedisConnectionFactory
{
    public static IConnectionMultiplexer Create(IConfiguration config)
    {
        var section = config.GetSection("Redis");

        if (!section.Exists())
            throw new InvalidOperationException("Configuração Redis não encontrada.");

        var options = new ConfigurationOptions
        {
            EndPoints =
            {
                { section["Host"]!, int.Parse(section["Port"]!) }
            },
            Password = section["Password"],
            Ssl = bool.Parse(section["Ssl"] ?? "false"),
            AbortOnConnectFail = false
        };

        var connection = ConnectionMultiplexer.Connect(options);

        var db = connection.GetDatabase();
        db.Ping(); 

        return connection;
    }
}
