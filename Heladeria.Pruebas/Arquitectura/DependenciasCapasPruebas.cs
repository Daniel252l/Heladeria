using System.Reflection;

namespace Heladeria.Pruebas.Arquitectura;

public sealed class DependenciasCapasPruebas
{
    [Fact]
    public void Dominio_NoDependeDeOtrasCapas()
    {
        var referencias = ObtenerReferencias(typeof(Heladeria.Dominio.EnsambladoReferencia).Assembly);

        Assert.DoesNotContain("Heladeria.Aplicacion", referencias);
        Assert.DoesNotContain("Heladeria.Infraestructura", referencias);
        Assert.DoesNotContain("Heladeria.Api", referencias);
    }

    [Fact]
    public void Aplicacion_NoDependeDeInfraestructuraNiApi()
    {
        var referencias = ObtenerReferencias(typeof(Heladeria.Aplicacion.EnsambladoReferencia).Assembly);

        Assert.DoesNotContain("Heladeria.Infraestructura", referencias);
        Assert.DoesNotContain("Heladeria.Api", referencias);
    }

    [Fact]
    public void Infraestructura_NoDependeDeApi()
    {
        var referencias = ObtenerReferencias(
            typeof(Heladeria.Infraestructura.Persistencia.HeladeriaDbContext).Assembly);

        Assert.DoesNotContain("Heladeria.Api", referencias);
    }

    private static HashSet<string> ObtenerReferencias(Assembly ensamblado) =>
        ensamblado
            .GetReferencedAssemblies()
            .Select(nombre => nombre.Name)
            .Where(nombre => nombre is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
}
