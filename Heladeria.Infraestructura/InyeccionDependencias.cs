using Heladeria.Aplicacion.Caja;
using Heladeria.Infraestructura.Caja;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Infraestructura.VentasPos;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Compras;
using Heladeria.Aplicacion.Inventario;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Produccion;
using Heladeria.Aplicacion.Seguridad.Autenticacion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Infraestructura.Configuracion;
using Heladeria.Infraestructura.Compras;
using Heladeria.Infraestructura.Fase4;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Produccion;
using Heladeria.Infraestructura.Seguridad.Autenticacion;
using Heladeria.Infraestructura.Seguridad.Autorizacion;
using Heladeria.Infraestructura.Seguridad.Bootstrap;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heladeria.Infraestructura;

public static class InyeccionDependencias
{
    public const string NombreConexion = "Heladeria";

    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        bool habilitarReintentosSql = true)
    {
        var cadenaConexion = configuracion.GetConnectionString(NombreConexion);

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                $"Debe configurarse ConnectionStrings:{NombreConexion}. " +
                "En producción utilice variables de entorno o un proveedor de secretos.");
        }

        servicios.AddDbContext<HeladeriaDbContext>(opciones =>
            opciones.UseSqlServer(
                cadenaConexion,
                sqlServer =>
                {
                    if (habilitarReintentosSql)
                    {
                        sqlServer.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null);
                    }
                }));

        servicios
            .AddHealthChecks()
            .AddDbContextCheck<HeladeriaDbContext>("sql-server", tags: ["ready"]);

        servicios.AddSingleton(TimeProvider.System);
        servicios.AddScoped<Heladeria.Aplicacion.Reportes.IServicioReportes, Heladeria.Infraestructura.ReportesOperativos.ServicioReportes>();
        servicios.AddScoped<IPreparadorFase10, PreparadorFase10>();
        servicios.AddSingleton<IGeneradorTokensJwt, GeneradorTokensJwt>();
        servicios.AddScoped<IPasswordHasher<Usuarios>, PasswordHasher<Usuarios>>();
        servicios.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
        servicios.AddScoped<IServicioPermisos, ServicioPermisos>();
        servicios.AddScoped<IServicioValidacionSesion, ServicioValidacionSesion>();
        servicios.AddScoped<IServicioAuditoriaSeguridad, ServicioAuditoriaSeguridad>();
        servicios.AddScoped<IAuditoriaConfiguracion, AuditoriaConfiguracion>();
        servicios.AddScoped<IServicioEstadosTiposConfiguracion, ServicioEstadosTiposConfiguracion>();
        servicios.AddScoped<IServicioCatalogosConfiguracion, ServicioCatalogosConfiguracion>();
        servicios.AddScoped<IServicioOrganizacionConfiguracion, ServicioOrganizacionConfiguracion>();
        servicios.AddScoped<IAuditoriaFase4, AuditoriaFase4>();
        servicios.AddScoped<IServicioCatalogosProductos, ServicioCatalogosProductos>();
        servicios.AddScoped<IServicioProductos, ServicioProductos>();
        servicios.AddSingleton<CoordinadorAjustesInventario>();
        servicios.AddScoped<ServicioInventario>();
        servicios.AddScoped<IServicioInventario>(proveedor => proveedor.GetRequiredService<ServicioInventario>());
        servicios.AddScoped<IServicioProveedores, ServicioProveedores>();
        servicios.AddScoped<IServicioCompras, ServicioCompras>();
        servicios.AddScoped<IServicioRecetas, ServicioRecetas>();
        servicios.AddScoped<IServicioProduccion, ServicioProduccion>();
        servicios.AddScoped<BootstrapAdministrador>();
        servicios.AddScoped<IBootstrapAdministrador>(proveedor =>
            proveedor.GetRequiredService<BootstrapAdministrador>());
        servicios.AddScoped<IProvisionadorUsuarioConsulta>(proveedor =>
            proveedor.GetRequiredService<BootstrapAdministrador>());
        servicios.AddScoped<IPreparadorFase3>(proveedor =>
            proveedor.GetRequiredService<BootstrapAdministrador>());
        servicios.AddScoped<IPreparadorFase4, PreparadorFase4>();
        servicios.AddScoped<IPreparadorFase5, PreparadorFase5>();
        servicios.AddScoped<IPreparadorFase6, PreparadorFase6>();
        servicios.AddScoped<IPreparadorFase7, PreparadorFase7>();
        servicios.AddScoped<IServicioVentas, ServicioVentas>();
        servicios.AddScoped<Heladeria.Aplicacion.Clientes.IServicioClientes, Heladeria.Infraestructura.ClientesGestion.ServicioClientes>();
        servicios.AddScoped<IPreparadorFase8, PreparadorFase8>();
        servicios.AddScoped<IPreparadorFase9, PreparadorFase9>();
        servicios.AddScoped<ServicioCaja>();
        servicios.AddScoped<IServicioCaja>(p => p.GetRequiredService<ServicioCaja>());
        servicios.AddScoped<Heladeria.Aplicacion.Cartera.IServicioCartera, Heladeria.Infraestructura.Cartera.ServicioCartera>();

        return servicios;
    }
}
