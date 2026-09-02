using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController]
[Route("api/estado")]
public sealed class EstadoSistemaController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<EstadoSistemaRespuesta>(StatusCodes.Status200OK)]
    public ActionResult<EstadoSistemaRespuesta> Obtener() =>
        Ok(new EstadoSistemaRespuesta(true, "Heladeria.Api"));
}

public sealed record EstadoSistemaRespuesta(bool Exito, string Servicio);
