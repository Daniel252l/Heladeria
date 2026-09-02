using Microsoft.AspNetCore.Authorization;

namespace Heladeria.Api.Seguridad.Autorizacion;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequierePermisoAttribute : AuthorizeAttribute
{
    public RequierePermisoAttribute(string permiso)
    {
        Policy = ProveedorPoliticasPermisos.CrearNombrePolitica(permiso);
    }
}

