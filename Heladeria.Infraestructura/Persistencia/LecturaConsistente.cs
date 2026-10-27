using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.Persistencia;

internal static class LecturaConsistente
{
    // Sin escrituras ni configuración SNAPSHOT en SQL. Respeta una transacción exterior existente.
    internal static Task<T> EjecutarAsync<T>(HeladeriaDbContext db, Func<Task<T>> leer, CancellationToken t) =>
        !db.Database.IsRelational() || db.Database.CurrentTransaction is not null ? leer()
        : db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, t);
            var resultado = await leer();
            await tx.CommitAsync(t);
            return resultado;
        });
}
