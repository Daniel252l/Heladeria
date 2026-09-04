namespace Heladeria.Infraestructura.Inventario;

public sealed class CoordinadorAjustesInventario
{
    private readonly SemaphoreSlim[] _bloqueos = Enumerable.Range(0, 257)
        .Select(_ => new SemaphoreSlim(1, 1))
        .ToArray();

    public async Task<IAsyncDisposable> AdquirirAsync(string clave, CancellationToken token)
    {
        var indice = (StringComparer.Ordinal.GetHashCode(clave) & int.MaxValue) % _bloqueos.Length;
        var bloqueo = _bloqueos[indice];
        await bloqueo.WaitAsync(token);
        return new Liberador(bloqueo);
    }

    private sealed class Liberador(SemaphoreSlim bloqueo) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            bloqueo.Release();
            return ValueTask.CompletedTask;
        }
    }
}
