using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Transfer.Application.Shared;

public static class TransferErrors
{
    public const string InvalidAccount = "INVALID_ACCOUNT";
    public const string InactiveAccount = "INACTIVE_ACCOUNT";
    public const string InvalidValue = "INVALID_VALUE";
    public const string Forbidden = "FORBIDDEN";
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
}
