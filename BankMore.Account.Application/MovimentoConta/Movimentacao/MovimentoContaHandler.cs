using BankMore.Account.Application.Abstractions;
using BankMore.Account.Application.Services.Idempotencia;
using BankMore.Account.Application.Shared;
using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories;
using BankMore.Account.Domain.Repositories.Shared;
using System.Net;
using System.Text.Json;

namespace BankMore.Account.Application.MovimentoConta.Movimentacao
{
    public class MovimentoContaHandler : IAccountHandler<MovimentoContaCommand, ApiResult<object>>
    {
        private readonly IContaCorrenteRepository _contaCorrenteRepository;
        private readonly IBankMoreAccountRepository<Movimento> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdempotenciaRepository _idempotenciaRepository;
        private readonly IIdempotencyService _idempotencyService;

        public MovimentoContaHandler(IContaCorrenteRepository contaCorrenteRepository, IBankMoreAccountRepository<Movimento> repository,
            IIdempotenciaRepository idempotenciaRepository, IUnitOfWork unitOfWork, IIdempotencyService idempotencyService)
        {
            _contaCorrenteRepository = contaCorrenteRepository;
            _repository = repository;
            _idempotenciaRepository = idempotenciaRepository;
            _unitOfWork = unitOfWork;
            _idempotencyService = idempotencyService;
        }
        public async Task<ApiResult<object>> Handle(MovimentoContaCommand request, CancellationToken ct)
        {
            var dataMovimento = DateTime.Now;
            var requisicaoStr = $"Movimento|{request.ContaOrigem}|{request.ContaDestino}|{request.TipoMovimento}|{request.Valor}";

            var (idempotenciaValida, resultadoIdempotencia) =
                await _idempotencyService.ChecIdempotenciakAsync(request.IdIdempotencia, requisicaoStr, ct);

            if (!idempotenciaValida)
                return resultadoIdempotencia!;

            if (request.Valor <= 0)
                return ApiResult<object>.Fail(HttpStatusCode.BadRequest, AccountErrors.InvalidValue, "O valor deve ser positivo");

            var tipoMovimento = MapTipoMovimento(request.TipoMovimento);
            if (tipoMovimento is null)
                return ApiResult<object>.Fail(HttpStatusCode.BadRequest, AccountErrors.InvalidType, "Tipo de movimento inválido (use 'C' ou 'D')");

            var contaDestino = request.ContaDestino.HasValue
                ? await _contaCorrenteRepository.GetByNumeroContaAsync(request.ContaDestino.Value, ct)
                : await _contaCorrenteRepository.GetAsync(request.ContaOrigem, ct);

            if (contaDestino is null)
                return ApiResult<object>.Fail(HttpStatusCode.BadRequest, AccountErrors.InvalidAccount, "Conta não encontrada");

            if (!contaDestino.Ativo)
                return ApiResult<object>.Fail(HttpStatusCode.BadRequest, AccountErrors.InvalidAccount, "Conta inativa");

            var ehTransferencia = request.ContaOrigem != contaDestino.IdContaCorrente;

            if (ehTransferencia && tipoMovimento.Value == TipoMovimento.Debito)
                return ApiResult<object>.Fail(HttpStatusCode.BadRequest, AccountErrors.InvalidType, "Titularidade divergente para débito");

            var movimentos = PrepararMovimentos(request, contaDestino.IdContaCorrente, tipoMovimento.Value, dataMovimento);

            var apiResult = ehTransferencia
                ? ApiResult<object>.Ok(new ResultadoTransferenciaDto
                {
                    IdContaDestino = contaDestino.IdContaCorrente,
                    DataMovimento = dataMovimento
                })
                : ApiResult<object>.NoContent();

            var idempotencia = new Idempotencia
            {
                ChaveIdempotencia = request.IdIdempotencia,
                Requisicao = requisicaoStr,
                Resultado = JsonSerializer.Serialize(apiResult)
            };

            //Optei por realizar as movimentações em uma transação atômica por ser mais seguro que fazer separadamente e verificar se ambas foram bem sucedidas
            //Elimina também o custo de uma requisição a mais para verificação ou estorno
            var transactionResult = await _unitOfWork.ExecuteInTransactionAsync(async transCt =>
            {
                foreach (var mov in movimentos)
                    await _repository.CreateAsync(mov, transCt);

                await _idempotenciaRepository.CreateAsync(idempotencia, transCt);
            }, ct);

            return transactionResult.Success
                ? apiResult
                : ApiResult<object>.Fail(HttpStatusCode.InternalServerError, AccountErrors.InternalServerError, transactionResult.Message);
        }

        private TipoMovimento? MapTipoMovimento(string? tipo) =>
            tipo?.Trim().ToUpperInvariant() switch
            {
                "C" => TipoMovimento.Credito,
                "D" => TipoMovimento.Debito,
                _ => null
            };

        private List<Movimento> PrepararMovimentos(MovimentoContaCommand req, Guid idDestino, TipoMovimento tipo, DateTime data)
        {            
            var lista = new List<Movimento>();

            var ehTransferencia = req.ContaOrigem != idDestino;

            if (ehTransferencia)
            {
                lista.Add(new Movimento
                {
                    IdContaCorrente = req.ContaOrigem,
                    TipoMovimento = TipoMovimento.Debito,
                    Valor = req.Valor,
                    DataMovimento = data
                });

                lista.Add(new Movimento
                {
                    IdContaCorrente = idDestino,
                    TipoMovimento = TipoMovimento.Credito,
                    Valor = req.Valor,
                    DataMovimento = data
                });

                return lista;
            }

            lista.Add(new Movimento
            {
                IdContaCorrente = idDestino,
                TipoMovimento = tipo,
                Valor = req.Valor,
                DataMovimento = data
            });

            return lista;
        }

    }
}
