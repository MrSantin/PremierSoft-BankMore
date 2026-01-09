using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Transfer.Application.Shared;

public readonly record struct TransactionResult(bool Success, string Message)
{
    public static TransactionResult Ok() => new(true, string.Empty);
    public static TransactionResult Fail(string message) => new(false, message);
}
