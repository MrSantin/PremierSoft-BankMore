using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Account.Domain.Repositories;

public interface IUsuarioRepository : IBankMoreAccountRepository<Usuario>
{
    Task<Usuario?> GetByCpfAsync(string cpf, CancellationToken ct);
}

