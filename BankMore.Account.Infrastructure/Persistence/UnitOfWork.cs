using BankMore.Account.Application.Abstractions;
using BankMore.Account.Application.Shared;
using BankMore.Account.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore.Storage;

namespace BankMore.Account.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly BankMoreAccountContext _context;
    private IDbContextTransaction? _tx;

    public UnitOfWork(BankMoreAccountContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);

    public async Task<TransactionResult> ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct)
    {
        await BeginTransactionAsync(ct);
        try
        {
            await action(ct);
            await CommitTransactionAsync(ct);
            return TransactionResult.Ok();
        }
        catch (OperationCanceledException)
        {
            await RollbackTransactionAsync(ct);
            return TransactionResult.Fail("Operação cancelada.");
        }
        catch
        {
            await RollbackTransactionAsync(ct);
            return TransactionResult.Fail("Não foi possível concluir a operação. Tente novamente.");
        }
    }



    private async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_tx != null) return;

        _tx = await _context.Database.BeginTransactionAsync(ct);
    }

    private async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_tx is null) return;

        await _context.SaveChangesAsync(ct);
        await _tx.CommitAsync(ct);

        await _tx.DisposeAsync();
        _tx = null;
    }

    private async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_tx is null) return;

        await _tx.RollbackAsync(ct);

        await _tx.DisposeAsync();
        _tx = null;
    }
}

