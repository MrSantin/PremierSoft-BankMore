using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Account.Application.Shared;

public interface IPasswordHasher : IApplicationService
{
    (string Hash, string Salt) HashPassword(string password);
    bool VerificarSenha(string password, string salt, string hash);
}
