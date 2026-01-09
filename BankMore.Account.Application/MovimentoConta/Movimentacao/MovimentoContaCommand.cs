using BankMore.Account.Application.Shared;
using BankMore.Account.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace BankMore.Account.Application.MovimentoConta.Movimentacao
{
    public class MovimentoContaCommand : IAccountCommand
    {
        public decimal Valor { get; set; } = default!;
        [MaxLength(1)]
        public string TipoMovimento { get; set; } = default!;
        [JsonIgnore]
        public Guid ContaOrigem { get; set; } = default!;
        public int? ContaDestino { get; set; }
        public Guid IdIdempotencia { get; set; } = default!;
    }
}
