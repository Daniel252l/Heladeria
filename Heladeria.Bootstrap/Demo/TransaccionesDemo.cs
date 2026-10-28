using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Heladeria.Bootstrap.Demo;

/// <summary>
/// Exclusivo del DbContext privado del cargador. Los servicios participan en una sola transacción SQL;
/// sus Commit/Dispose locales no pueden confirmar ni cerrar la raíz. Un rollback local invalida toda la carga.
/// No se registra en API, infraestructura funcional ni bootstrap estructural.
/// </summary>
public sealed class TransaccionesDemo(IRelationalConnection conexion) : IRelationalTransactionManager
{
    public bool Fallida { get; private set; }
    public IDbContextTransaction? CurrentTransaction => conexion.CurrentTransaction;
    public async Task<IDbContextTransaction> AbrirRaizAsync(CancellationToken t)
    {
        if (CurrentTransaction is not null) throw new InvalidOperationException("La carga ya tiene una transacción.");
        Fallida = false;
        return await conexion.BeginTransactionAsync(IsolationLevel.Serializable, t);
    }
    public void ExigirValida()
    {
        if (Fallida) throw new InvalidOperationException("Una operación demo falló; no se permite commit parcial.");
    }
    private IDbContextTransaction Participar()
    {
        ExigirValida();
        return new Participacion(CurrentTransaction ?? throw new InvalidOperationException("Falta la transacción raíz demo."), () => Fallida = true);
    }
    public IDbContextTransaction BeginTransaction() => Participar();
    public IDbContextTransaction BeginTransaction(IsolationLevel isolationLevel) => Participar();
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Participar());
    public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default) =>
        Task.FromResult(Participar());
    public void CommitTransaction() => throw new InvalidOperationException("Solo el cargador confirma la raíz.");
    public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("Solo el cargador confirma la raíz.");
    public void RollbackTransaction() => Fallida = true;
    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) { Fallida = true; return Task.CompletedTask; }
    public IDbContextTransaction? UseTransaction(DbTransaction? transaction) => throw new NotSupportedException();
    public IDbContextTransaction? UseTransaction(DbTransaction? transaction, Guid transactionId) => throw new NotSupportedException();
    public Task<IDbContextTransaction?> UseTransactionAsync(DbTransaction? transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<IDbContextTransaction?> UseTransactionAsync(DbTransaction? transaction, Guid transactionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public void ResetState() { Fallida = false; }
    public Task ResetStateAsync(CancellationToken cancellationToken = default) { ResetState(); return Task.CompletedTask; }

    private sealed class Participacion(IDbContextTransaction raiz, Action fallar) : IDbContextTransaction, IInfrastructure<DbTransaction>
    {
        private bool terminada;
        public Guid TransactionId => raiz.TransactionId;
        public DbTransaction Instance => raiz.GetDbTransaction();
        public bool SupportsSavepoints => raiz.SupportsSavepoints;
        public void Commit() => terminada = true;
        public Task CommitAsync(CancellationToken cancellationToken = default) { cancellationToken.ThrowIfCancellationRequested(); Commit(); return Task.CompletedTask; }
        public void Rollback() { fallar(); terminada = true; }
        public Task RollbackAsync(CancellationToken cancellationToken = default) { Rollback(); return Task.CompletedTask; }
        public void Dispose() { if (!terminada) Rollback(); }
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        public void CreateSavepoint(string name) => raiz.CreateSavepoint(name);
        public Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default) => raiz.CreateSavepointAsync(name, cancellationToken);
        public void RollbackToSavepoint(string name) => raiz.RollbackToSavepoint(name);
        public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default) => raiz.RollbackToSavepointAsync(name, cancellationToken);
        public void ReleaseSavepoint(string name) => raiz.ReleaseSavepoint(name);
        public Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default) => raiz.ReleaseSavepointAsync(name, cancellationToken);
    }
}
