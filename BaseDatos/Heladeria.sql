CREATE DATABASE Heladeria;
GO

USE Heladeria;
GO


--Seguridad

CREATE TABLE ClaseEstados (
    Id      INT          NOT NULL CONSTRAINT PK_ClaseEstados PRIMARY KEY,
    Nombre  VARCHAR(200) NOT NULL,
    Activo  BIT          NOT NULL
);
GO

CREATE TABLE Modulos (
    Id          VARCHAR(20)  NOT NULL CONSTRAINT PK_Modulos PRIMARY KEY,
    Nombre      VARCHAR(50)  NOT NULL,
    Icono        VARCHAR(50)  NOT NULL,
    Activo      BIT          NOT NULL
);
GO

CREATE TABLE Estados (
    Id            INT          NOT NULL CONSTRAINT PK_Estados PRIMARY KEY,
    Nombre        VARCHAR(100) NOT NULL,
    Activo        BIT          NOT NULL,
    ClaseEstadoId INT          NOT NULL CONSTRAINT FK_Estados_ClaseEstadoId REFERENCES ClaseEstados(Id),
    ModuloId VARCHAR(20) NULL CONSTRAINT FK_Estados_ModuloId REFERENCES Modulos(Id)
);
GO

CREATE TABLE ClaseTipos (
    Id      INT          NOT NULL CONSTRAINT PK_ClaseTipos PRIMARY KEY,
    Nombre  VARCHAR(100) NOT NULL CONSTRAINT UQ_ClaseTipos_Nombre UNIQUE,
    EstadoId INT NOT NULL CONSTRAINT FK_ClaseTipos_Estado REFERENCES Estados(Id)
);
GO

CREATE TABLE NivelesDeAccesos (
    Id      INT          NOT NULL CONSTRAINT PK_NivelesDeAccesos PRIMARY KEY,
    Nombre  VARCHAR(100) NOT NULL
);
GO

CREATE TABLE Tipos (
    Id          INT          NOT NULL CONSTRAINT PK_Tipos PRIMARY KEY,
    Nombre      VARCHAR(100) NOT NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_Tipos_Estado REFERENCES Estados(Id),
    ModuloId    VARCHAR(20)  NULL CONSTRAINT FK_Tipos_ModuloId REFERENCES Modulos(Id),
    ClaseTipoId INT          NOT NULL CONSTRAINT FK_Tipos_ClaseTipoId REFERENCES ClaseTipos(Id),
    CONSTRAINT UQ_Tipos_Nombre_ClaseTipoId UNIQUE (ClaseTipoId, Nombre)
);
GO

CREATE TABLE Roles (
    Id                           VARCHAR(20) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    Nombre                       VARCHAR(50) NOT NULL CONSTRAINT UQ_Roles_Nombre UNIQUE,
    NivelDeAccesoId              INT         NOT NULL CONSTRAINT FK_Roles_NivelDeAccesoId REFERENCES NivelesDeAccesos(Id),
    CambioDeContrasenia          INT         NOT NULL CONSTRAINT CK_Roles_CambioDeContrasenia CHECK (CambioDeContrasenia > 0 AND CambioDeContrasenia < 49),
    TipoId                       INT         NOT NULL CONSTRAINT FK_Roles_TipoId REFERENCES Tipos(Id),
    AplicarConfiguracionesPorRol BIT         NOT NULL
);
GO

CREATE TABLE Usuarios (
    Id                          INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuarios PRIMARY KEY,
    Email                       VARCHAR(100)  NOT NULL CONSTRAINT UQ_Usuarios_Email UNIQUE,
    UserName                    VARCHAR(50)   NOT NULL CONSTRAINT UQ_Usuarios_UserName UNIQUE,
    IdentityId                  NVARCHAR(128) NOT NULL CONSTRAINT UQ_Usuarios_IdentityId UNIQUE,
    PasswordHash                NVARCHAR(MAX) NOT NULL,
    SecurityStamp               NVARCHAR(MAX) NULL,
    RoleId                      VARCHAR(20)   NOT NULL CONSTRAINT FK_Usuarios_RoleId REFERENCES Roles(Id),
    AccessFailedCount           INT           NOT NULL,
    HorarioInicio               TIME(7)       NOT NULL,
    HorarioFinal                TIME(7)       NOT NULL,
    UltimoCambioDeContrasenia   DATE          NULL,
    Imagen                      VARCHAR(50)   NOT NULL,
    [Soundex]                   VARCHAR(300)  NOT NULL,
    ContraseniaTemporal         BIT           NOT NULL,
    AsociadoId                  INT           NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_Usuarios_Estado REFERENCES Estados(Id),
    PasswordTemporal            VARCHAR(50)   NULL,
    GrupoId                     INT           NULL,
    TipoInicioSesionId          INT           NOT NULL CONSTRAINT FK_Usuarios_TipoInicioSesionId REFERENCES Tipos(Id),
    UtilizarConfiguracionDeRole BIT           NULL
);
GO

CREATE TABLE Configuraciones (
    Id                  INT          NOT NULL CONSTRAINT PK_Configuraciones PRIMARY KEY,
    TipoConfiguracionId INT          NOT NULL CONSTRAINT FK_Configuraciones_TipoConfiguracionId REFERENCES Tipos(Id),
    TipoValorId         INT          NULL CONSTRAINT FK_Configuraciones_TipoValorId REFERENCES Tipos(Id),
    ModuloId            VARCHAR(20)  NULL CONSTRAINT FK_Configuraciones_ModuloId REFERENCES Modulos(Id),
    Descripcion         VARCHAR(300) NOT NULL,
    CONSTRAINT UQ_Configuraciones_ModuloId_Descripcion UNIQUE (ModuloId, TipoConfiguracionId, Descripcion),
    PorDefecto          VARCHAR(300) NOT NULL,
    Visible             BIT          NOT NULL
);
GO

CREATE TABLE TipoNotificaciones (
    Id          VARCHAR(20)  NOT NULL CONSTRAINT PK_TipoNotificaciones PRIMARY KEY,
    TipoId      INT          NOT NULL CONSTRAINT UQ_TipoNotificaciones_TipoId UNIQUE CONSTRAINT FK_TipoNotificaciones_TipoId REFERENCES Tipos(Id),
    ModuloId    VARCHAR(20)  NOT NULL CONSTRAINT FK_TipoNotificaciones_ModuloId REFERENCES Modulos(Id),
    Descripcion VARCHAR(150) NOT NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_TipoNotificaciones_Estado REFERENCES Estados(Id),
    PorSucursal BIT          NOT NULL
);
GO

CREATE TABLE Reportes (
    Id          VARCHAR(20)   NOT NULL CONSTRAINT PK_Reportes PRIMARY KEY,
    ModuloId    VARCHAR(20)   NOT NULL CONSTRAINT FK_Reportes_ModuloId REFERENCES Modulos(Id),
    Descripcion VARCHAR(150)  NOT NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_Reportes_Estado REFERENCES Estados(Id),
    ClienteId   INT           NULL CONSTRAINT FK_Reportes_ClienteId REFERENCES Tipos(Id),
    Comentario  VARCHAR(1000) NULL
);
GO


CREATE TABLE AccionesSistema (
    Id          INT          IDENTITY(1,1) NOT NULL CONSTRAINT PK_AccionesSistema PRIMARY KEY,
    Codigo      VARCHAR(50)  NOT NULL CONSTRAINT UQ_AccionesSistema_Codigo UNIQUE,
    Nombre      VARCHAR(100) NOT NULL,
    EstadoId    INT          NOT NULL CONSTRAINT FK_AccionesSistema_Estado REFERENCES Estados(Id)
);
GO

CREATE TABLE Permisos (
    Id          BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_Permisos PRIMARY KEY,
    ModuloId    VARCHAR(20)  NOT NULL CONSTRAINT FK_Permisos_Modulo REFERENCES Modulos(Id),
    AccionId    INT          NOT NULL CONSTRAINT FK_Permisos_Accion REFERENCES AccionesSistema(Id),
    Recurso     VARCHAR(100) NOT NULL,
    Codigo      VARCHAR(150) NOT NULL CONSTRAINT UQ_Permisos_Codigo UNIQUE,
    EstadoId    INT          NOT NULL CONSTRAINT FK_Permisos_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_Permisos_Modulo_Accion_Recurso UNIQUE (ModuloId, AccionId, Recurso)
);
GO

CREATE TABLE RolesPermisos (
    Id          BIGINT      IDENTITY(1,1) NOT NULL CONSTRAINT PK_RolesPermisos PRIMARY KEY,
    RoleId      VARCHAR(20) NOT NULL CONSTRAINT FK_RolesPermisos_Role REFERENCES Roles(Id),
    PermisoId   BIGINT      NOT NULL CONSTRAINT FK_RolesPermisos_Permiso REFERENCES Permisos(Id),
    Permitido   BIT         NOT NULL,
    CONSTRAINT UQ_RolesPermisos_Role_Permiso UNIQUE (RoleId, PermisoId)
);
GO

CREATE TABLE SesionesUsuarios (
    Id                BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_SesionesUsuarios PRIMARY KEY,
    UsuarioId         INT           NOT NULL CONSTRAINT FK_SesionesUsuarios_Usuario REFERENCES Usuarios(Id),
    FechaInicio       DATETIME2(3)  NOT NULL,
    FechaFin          DATETIME2(3)  NULL,
    DireccionIp       VARCHAR(45)   NULL,
    Dispositivo       VARCHAR(200)  NULL,
    TokenIdentificador VARCHAR(200) NULL,
    Cerrada           BIT           NOT NULL
);
GO

CREATE TABLE HistorialAccesos (
    Id            BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_HistorialAccesos PRIMARY KEY,
    UsuarioId     INT           NULL CONSTRAINT FK_HistorialAccesos_Usuario REFERENCES Usuarios(Id),
    UserName      VARCHAR(50)   NULL,
    Fecha         DATETIME2(3)  NOT NULL,
    DireccionIp   VARCHAR(45)   NULL,
    Exitoso       BIT           NOT NULL,
    Motivo        VARCHAR(300)  NULL
);
GO

--Administracion

CREATE TABLE Departamentos (
    Id      INT          IDENTITY(1,1) NOT NULL CONSTRAINT PK_Departamentos PRIMARY KEY,
    Nombre  VARCHAR(50)  NOT NULL CONSTRAINT UQ_Departamentos_Nombre UNIQUE,
    Codigo  VARCHAR(2)   NOT NULL
);
GO

CREATE TABLE Municipios (
    Id             INT         IDENTITY(1,1) NOT NULL CONSTRAINT PK_Municipios PRIMARY KEY,
    DepartamentoId INT         NOT NULL CONSTRAINT FK_Municipios_DepartamentoId REFERENCES Departamentos(Id),
    Nombre         VARCHAR(50) NOT NULL,
    CONSTRAINT UQ_Municipios_DepartamentoId_Nombre UNIQUE (DepartamentoId, Nombre),
    Codigo         VARCHAR(4)  NOT NULL
);
GO

CREATE TABLE Empresas (
    Id              INT          IDENTITY(1,1) NOT NULL CONSTRAINT PK_Empresas PRIMARY KEY,
    RazonSocial     VARCHAR(150) NOT NULL,
    Eslogan         VARCHAR(150) NOT NULL,
    Direccion       VARCHAR(250) NOT NULL,
    Nit             VARCHAR(25)  NOT NULL CONSTRAINT UQ_Empresas_Nit UNIQUE,
    Dpi             VARCHAR(25)  NULL,
    Telefono        VARCHAR(100) NOT NULL,
    Email           VARCHAR(100) NOT NULL CONSTRAINT UQ_Empresas_Email UNIQUE,
    Imagen          VARCHAR(100) NOT NULL,
    Icono           VARCHAR(100) NOT NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_Empresas_Estado REFERENCES Estados(Id),
    ImagenReportes  VARCHAR(100) NOT NULL,
    RoleId          VARCHAR(20)  NOT NULL CONSTRAINT FK_Empresas_RoleId REFERENCES Roles(Id),
    ClienteId       INT          NULL CONSTRAINT FK_Empresas_ClienteId REFERENCES Tipos(Id),
    NombreComercial VARCHAR(150) NULL,
    [Url]           VARCHAR(150) NULL
);
GO

CREATE TABLE Sucursales (
    Id                               BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sucursales PRIMARY KEY,
    Nombre                           VARCHAR(150) NOT NULL,
    Direccion                        VARCHAR(250) NOT NULL,
    Telefono                         VARCHAR(100) NOT NULL,
    Email                            VARCHAR(100) NOT NULL,
    EmpresaId                        INT          NOT NULL CONSTRAINT FK_Sucursales_EmpresaId REFERENCES Empresas(Id),
    LugarId                          INT          NULL,
    MunicipioId INT NULL CONSTRAINT FK_Sucursales_MunicipioId REFERENCES Municipios(Id),
    DepartamentoId INT NULL CONSTRAINT FK_Sucursales_DepartamentoId REFERENCES Departamentos(Id),
    EsCentral                        BIT          NOT NULL,
    ClienteId                        INT          NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_Sucursales_Estado REFERENCES Estados(Id),
    ProveedorId                      INT          NOT NULL,
    EsBodega                         BIT          NOT NULL,
    SucursalCentralId BIGINT NULL CONSTRAINT FK_Sucursales_SucursalCentralId REFERENCES Sucursales(Id),
    Imagen                           VARCHAR(50)  NULL,
    SucursalTransporteId BIGINT NULL CONSTRAINT FK_Sucursales_SucursalTransporteId REFERENCES Sucursales(Id),
    RepresentanteLegal               VARCHAR(100) NULL,
    DireccionCorta                   VARCHAR(50)  NOT NULL,
    Codigo                           VARCHAR(25)  NULL,
    ImagenReportes                   VARCHAR(100) NULL,
    Transportes                      BIT          NOT NULL,
    CodigoNombre                     VARCHAR(175) NOT NULL
);
GO


CREATE TABLE Monedas (
    Id              INT          IDENTITY(1,1) NOT NULL CONSTRAINT PK_Monedas PRIMARY KEY,
    EmpresaId       INT          NOT NULL CONSTRAINT FK_Monedas_Empresa REFERENCES Empresas(Id),
    CodigoISO       VARCHAR(3)   NOT NULL,
    NombreMoneda    VARCHAR(50)  NOT NULL,
    Simbolo         VARCHAR(5)   NOT NULL,
    EsMonedaBase    BIT          NOT NULL,
    Decimales       TINYINT      NOT NULL CONSTRAINT CK_Monedas_Decimales CHECK (Decimales BETWEEN 0 AND 6),
    EstadoId        INT          NOT NULL CONSTRAINT FK_Monedas_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_Monedas_Empresa_Codigo UNIQUE (EmpresaId, CodigoISO)
);
GO

CREATE TABLE TiposCambio (
    Id              BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_TiposCambio PRIMARY KEY,
    EmpresaId       INT           NOT NULL CONSTRAINT FK_TiposCambio_Empresa REFERENCES Empresas(Id),
    MonedaOrigenId  INT           NOT NULL CONSTRAINT FK_TiposCambio_MonedaOrigen REFERENCES Monedas(Id),
    MonedaDestinoId INT           NOT NULL CONSTRAINT FK_TiposCambio_MonedaDestino REFERENCES Monedas(Id),
    Fecha           DATETIME2(3)  NOT NULL,
    TasaCompra      DECIMAL(19,8) NOT NULL CONSTRAINT CK_TiposCambio_TasaCompra CHECK (TasaCompra > 0),
    TasaVenta       DECIMAL(19,8) NOT NULL CONSTRAINT CK_TiposCambio_TasaVenta CHECK (TasaVenta > 0),
    UsuarioId       INT           NOT NULL CONSTRAINT FK_TiposCambio_Usuario REFERENCES Usuarios(Id),
    CONSTRAINT CK_TiposCambio_Monedas CHECK (MonedaOrigenId <> MonedaDestinoId)
);
GO

CREATE TABLE Impuestos (
    Id              INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_Impuestos PRIMARY KEY,
    EmpresaId       INT           NOT NULL CONSTRAINT FK_Impuestos_Empresa REFERENCES Empresas(Id),
    Codigo          VARCHAR(20)   NOT NULL,
    Nombre          VARCHAR(100)  NOT NULL,
    Porcentaje      DECIMAL(9,6)  NOT NULL CONSTRAINT CK_Impuestos_Porcentaje CHECK (Porcentaje >= 0 AND Porcentaje <= 100),
    IncluidoEnPrecio BIT          NOT NULL,
    FechaVigenciaDesde DATE       NOT NULL,
    FechaVigenciaHasta DATE       NULL,
    EstadoId        INT           NOT NULL CONSTRAINT FK_Impuestos_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_Impuestos_Empresa_Codigo UNIQUE (EmpresaId, Codigo),
    CONSTRAINT CK_Impuestos_Vigencia CHECK (FechaVigenciaHasta IS NULL OR FechaVigenciaHasta >= FechaVigenciaDesde)
);
GO

CREATE TABLE FormasPago (
    Id                  INT          IDENTITY(1,1) NOT NULL CONSTRAINT PK_FormasPago PRIMARY KEY,
    EmpresaId           INT          NOT NULL CONSTRAINT FK_FormasPago_Empresa REFERENCES Empresas(Id),
    Codigo              VARCHAR(20)  NOT NULL,
    Nombre              VARCHAR(80)  NOT NULL,
    RequiereReferencia  BIT          NOT NULL,
    AfectaCaja          BIT          NOT NULL,
    PermiteCambio       BIT          NOT NULL,
    EsCredito           BIT          NOT NULL,
    EstadoId            INT          NOT NULL CONSTRAINT FK_FormasPago_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_FormasPago_Empresa_Codigo UNIQUE (EmpresaId, Codigo)
);
GO

CREATE TABLE SeriesDocumentos (
    Id              BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeriesDocumentos PRIMARY KEY,
    EmpresaId       INT          NOT NULL CONSTRAINT FK_SeriesDocumentos_Empresa REFERENCES Empresas(Id),
    SucursalId      BIGINT       NULL CONSTRAINT FK_SeriesDocumentos_Sucursal REFERENCES Sucursales(Id),
    TipoDocumentoId INT          NOT NULL CONSTRAINT FK_SeriesDocumentos_TipoDocumento REFERENCES Tipos(Id),
    Serie           VARCHAR(20)  NOT NULL,
    Prefijo         VARCHAR(20)  NULL,
    Longitud        TINYINT      NOT NULL CONSTRAINT CK_SeriesDocumentos_Longitud CHECK (Longitud BETWEEN 1 AND 20),
    EstadoId        INT          NOT NULL CONSTRAINT FK_SeriesDocumentos_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_SeriesDocumentos UNIQUE (EmpresaId, SucursalId, TipoDocumentoId, Serie)
);
GO

CREATE TABLE CorrelativosDocumentos (
    Id                  BIGINT      IDENTITY(1,1) NOT NULL CONSTRAINT PK_CorrelativosDocumentos PRIMARY KEY,
    SerieDocumentoId    BIGINT      NOT NULL CONSTRAINT FK_CorrelativosDocumentos_Serie REFERENCES SeriesDocumentos(Id),
    UltimoNumero        BIGINT      NOT NULL CONSTRAINT CK_CorrelativosDocumentos_Ultimo CHECK (UltimoNumero >= 0),
    FechaModificacion   DATETIME2(3) NOT NULL,
    VersionFila         ROWVERSION  NOT NULL,
    CONSTRAINT UQ_CorrelativosDocumentos_Serie UNIQUE (SerieDocumentoId)
);
GO

--Seguridad

CREATE TABLE UsuariosSucursales (
    Id          BIGINT  IDENTITY(1,1) NOT NULL CONSTRAINT PK_UsuariosSucursales PRIMARY KEY,
    UsuarioId   INT     NOT NULL CONSTRAINT FK_UsuariosSucursales_Usuario REFERENCES Usuarios(Id),
    SucursalId  BIGINT  NOT NULL CONSTRAINT FK_UsuariosSucursales_Sucursal REFERENCES Sucursales(Id),
    EsPrincipal BIT     NOT NULL,
    EstadoId    INT     NOT NULL CONSTRAINT FK_UsuariosSucursales_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_UsuariosSucursales UNIQUE (UsuarioId, SucursalId)
);
GO

--Administracion

CREATE TABLE Empleados (
    Id                  INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_Empleados PRIMARY KEY,
    EmpresaId           INT           NOT NULL CONSTRAINT FK_Empleados_Empresa REFERENCES Empresas(Id),
    SucursalId          BIGINT        NOT NULL CONSTRAINT FK_Empleados_Sucursal REFERENCES Sucursales(Id),
    UsuarioId           INT           NULL CONSTRAINT FK_Empleados_Usuario REFERENCES Usuarios(Id),
    CodigoEmpleado      VARCHAR(20)   NOT NULL,
    CONSTRAINT UQ_Empleados_Codigo UNIQUE (EmpresaId, CodigoEmpleado),
    Nombres             VARCHAR(100)  NOT NULL,
    Apellidos           VARCHAR(100)  NOT NULL,
    Dpi                 VARCHAR(25)   NULL,
    CargoId             INT           NOT NULL CONSTRAINT FK_Empleados_Cargo REFERENCES Tipos(Id),
    SalarioBase         DECIMAL(12,2) NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_Empleados_Estado REFERENCES Estados(Id)
);
GO

CREATE TABLE AsistenciaPersonal (
    Id                  BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_AsistenciaPersonal PRIMARY KEY,
    EmpleadoId          INT           NOT NULL CONSTRAINT FK_Asistencia_Empleado REFERENCES Empleados(Id),
    SucursalId          BIGINT        NOT NULL CONSTRAINT FK_Asistencia_Sucursal REFERENCES Sucursales(Id),
    FechaHoraEntrada    DATETIME      NOT NULL,
    FechaHoraSalida     DATETIME      NULL,
    TipoTurnoId         INT           NOT NULL CONSTRAINT FK_Asistencia_Turno REFERENCES Tipos(Id),
    Aprobado            BIT           NOT NULL
);
GO

--Mantenimientos

CREATE TABLE ClasesCatalogos (
    Id                     INT          IDENTITY(1,1) NOT NULL CONSTRAINT PK_ClasesCatalogos PRIMARY KEY,
    Codigo                 VARCHAR(50)  NOT NULL CONSTRAINT UQ_ClasesCatalogos_Codigo UNIQUE,
    Nombre                 VARCHAR(100) NOT NULL CONSTRAINT UQ_ClasesCatalogos_Nombre UNIQUE,
    Descripcion            VARCHAR(300) NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_ClasesCatalogos_Estado REFERENCES Estados(Id),
    FechaCreacion          DATETIME2(3) NOT NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_ClasesCatalogos_CreadoPor REFERENCES Usuarios(Id),
    FechaModificacion      DATETIME2(3) NULL,
    ModificadoPorUsuarioId INT          NULL CONSTRAINT FK_ClasesCatalogos_ModificadoPor REFERENCES Usuarios(Id)
);
GO

CREATE TABLE Catalogos (
    Id                     BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_Catalogos PRIMARY KEY,
    ClaseCatalogoId        INT          NOT NULL CONSTRAINT FK_Catalogos_Clase REFERENCES ClasesCatalogos(Id),
    CONSTRAINT UQ_Catalogos_Clase_Id UNIQUE (ClaseCatalogoId, Id),
    EmpresaId              INT          NULL CONSTRAINT FK_Catalogos_Empresa REFERENCES Empresas(Id),
    CatalogoPadreId        BIGINT       NULL,
    CONSTRAINT CK_Catalogos_NoAutoReferencia CHECK (CatalogoPadreId IS NULL OR CatalogoPadreId <> Id),
    CONSTRAINT FK_Catalogos_Padre FOREIGN KEY (ClaseCatalogoId, CatalogoPadreId) REFERENCES Catalogos(ClaseCatalogoId, Id),
    Codigo                 VARCHAR(50)  NOT NULL,
    CONSTRAINT UQ_Catalogos_Clase_Empresa_Codigo UNIQUE (ClaseCatalogoId, EmpresaId, Codigo),
    Nombre                 VARCHAR(150) NOT NULL,
    CONSTRAINT UQ_Catalogos_Clase_Empresa_Nombre UNIQUE (ClaseCatalogoId, EmpresaId, Nombre),
    Descripcion            VARCHAR(500) NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_Catalogos_Estado REFERENCES Estados(Id),
    FechaCreacion          DATETIME2(3) NOT NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_Catalogos_CreadoPor REFERENCES Usuarios(Id),
    FechaModificacion      DATETIME2(3) NULL,
    ModificadoPorUsuarioId INT          NULL CONSTRAINT FK_Catalogos_ModificadoPor REFERENCES Usuarios(Id)
);
GO

CREATE TABLE FlujosEstados (
    Id                     INT          IDENTITY(1,1) NOT NULL CONSTRAINT PK_FlujosEstados PRIMARY KEY,
    Codigo                 VARCHAR(50)  NOT NULL CONSTRAINT UQ_FlujosEstados_Codigo UNIQUE,
    Nombre                 VARCHAR(100) NOT NULL CONSTRAINT UQ_FlujosEstados_Nombre UNIQUE,
    Descripcion            VARCHAR(300) NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_FlujosEstados_Estado REFERENCES Estados(Id),
    FechaCreacion          DATETIME2(3) NOT NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_FlujosEstados_CreadoPor REFERENCES Usuarios(Id),
    FechaModificacion      DATETIME2(3) NULL,
    ModificadoPorUsuarioId INT          NULL CONSTRAINT FK_FlujosEstados_ModificadoPor REFERENCES Usuarios(Id)
);
GO

CREATE TABLE EstadosFlujo (
    Id                     INT          IDENTITY(1,1) NOT NULL CONSTRAINT PK_EstadosFlujo PRIMARY KEY,
    FlujoEstadoId          INT          NOT NULL CONSTRAINT FK_EstadosFlujo_Flujo REFERENCES FlujosEstados(Id),
    CONSTRAINT UQ_EstadosFlujo_Flujo_Id UNIQUE (FlujoEstadoId, Id),
    Codigo                 VARCHAR(50)  NOT NULL,
    CONSTRAINT UQ_EstadosFlujo_Flujo_Codigo UNIQUE (FlujoEstadoId, Codigo),
    Nombre                 VARCHAR(100) NOT NULL,
    CONSTRAINT UQ_EstadosFlujo_Flujo_Nombre UNIQUE (FlujoEstadoId, Nombre),
    Descripcion            VARCHAR(300) NULL,
    Orden                  SMALLINT     NOT NULL CONSTRAINT CK_EstadosFlujo_Orden CHECK (Orden >= 0),
    EsInicial              BIT          NOT NULL,
    EsFinal                BIT          NOT NULL,
    PermiteEdicion         BIT          NOT NULL,
    RequiereMotivo         BIT          NOT NULL,
    EsSistema              BIT          NOT NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_EstadosFlujo_Estado REFERENCES Estados(Id),
    FechaCreacion          DATETIME2(3) NOT NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_EstadosFlujo_CreadoPor REFERENCES Usuarios(Id),
    FechaModificacion      DATETIME2(3) NULL,
    ModificadoPorUsuarioId INT          NULL CONSTRAINT FK_EstadosFlujo_ModificadoPor REFERENCES Usuarios(Id),
    VersionFila            ROWVERSION   NOT NULL
);
GO

CREATE TABLE EstadoTransiciones (
    Id                     BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_EstadoTransiciones PRIMARY KEY,
    FlujoEstadoId          INT          NOT NULL CONSTRAINT FK_EstadoTransiciones_Flujo REFERENCES FlujosEstados(Id),
    EstadoOrigenId         INT          NOT NULL,
    CONSTRAINT FK_EstadoTransiciones_Origen FOREIGN KEY (FlujoEstadoId, EstadoOrigenId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    EstadoDestinoId        INT          NOT NULL,
    CONSTRAINT UQ_EstadoTransiciones_Flujo_Origen_Destino UNIQUE (FlujoEstadoId, EstadoOrigenId, EstadoDestinoId),
    CONSTRAINT CK_EstadoTransiciones_Diferentes CHECK (EstadoOrigenId <> EstadoDestinoId),
    CONSTRAINT FK_EstadoTransiciones_Destino FOREIGN KEY (FlujoEstadoId, EstadoDestinoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    RequiereAutorizacion   BIT          NOT NULL,
    RequiereComentario     BIT          NOT NULL,
    PermisoCodigo          VARCHAR(100) NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_EstadoTransiciones_Estado REFERENCES Estados(Id),
    FechaCreacion          DATETIME2(3) NOT NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_EstadoTransiciones_CreadoPor REFERENCES Usuarios(Id),
    FechaModificacion      DATETIME2(3) NULL,
    ModificadoPorUsuarioId INT          NULL CONSTRAINT FK_EstadoTransiciones_ModificadoPor REFERENCES Usuarios(Id),
    VersionFila            ROWVERSION   NOT NULL
);
GO

--Inventario

CREATE TABLE UnidadesMedida (
    Id                     INT          IDENTITY(1,1) NOT NULL CONSTRAINT PK_UnidadesMedida PRIMARY KEY,
    EmpresaId              INT          NOT NULL CONSTRAINT FK_UnidadesMedida_Empresa REFERENCES Empresas(Id),
    CONSTRAINT UQ_Unidades_Empresa_Id UNIQUE (EmpresaId, Id),
    Codigo                 VARCHAR(20)  NOT NULL,
    CONSTRAINT UQ_Unidades_Empresa_Codigo UNIQUE (EmpresaId, Codigo),
    Nombre                 VARCHAR(100) NOT NULL,
    CONSTRAINT UQ_Unidades_Empresa_Nombre UNIQUE (EmpresaId, Nombre),
    Abreviatura            VARCHAR(20)  NOT NULL,
    PermiteDecimales       BIT          NOT NULL,
    CantidadDecimales      TINYINT      NOT NULL CONSTRAINT CK_Unidades_CantidadDecimales CHECK (CantidadDecimales BETWEEN 0 AND 6),
    EstadoId INT NOT NULL CONSTRAINT FK_UnidadesMedida_Estado REFERENCES Estados(Id),
    FechaCreacion          DATETIME2(3) NOT NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_UnidadesMedida_CreadoPor REFERENCES Usuarios(Id),
    FechaModificacion      DATETIME2(3) NULL,
    ModificadoPorUsuarioId INT          NULL CONSTRAINT FK_UnidadesMedida_ModificadoPor REFERENCES Usuarios(Id),
    VersionFila            ROWVERSION   NOT NULL
);
GO

CREATE TABLE ConversionesUnidad (
    Id              BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConversionesUnidad PRIMARY KEY,
    EmpresaId       INT           NOT NULL CONSTRAINT FK_ConversionesUnidad_Empresa REFERENCES Empresas(Id),
    UnidadOrigenId  INT           NOT NULL CONSTRAINT FK_ConversionesUnidad_Origen REFERENCES UnidadesMedida(Id),
    UnidadDestinoId INT           NOT NULL CONSTRAINT FK_ConversionesUnidad_Destino REFERENCES UnidadesMedida(Id),
    Factor          DECIMAL(19,8) NOT NULL CONSTRAINT CK_ConversionesUnidad_Factor CHECK (Factor > 0),
    EstadoId        INT           NOT NULL CONSTRAINT FK_ConversionesUnidad_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_ConversionesUnidad UNIQUE (EmpresaId, UnidadOrigenId, UnidadDestinoId),
    CONSTRAINT CK_ConversionesUnidad_Diferentes CHECK (UnidadOrigenId <> UnidadDestinoId)
);
GO

CREATE TABLE Categorias (
    Id                     INT          IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categorias PRIMARY KEY,
    EmpresaId              INT          NOT NULL CONSTRAINT FK_Categorias_Empresa REFERENCES Empresas(Id),
    CONSTRAINT UQ_Categorias_Empresa_Id UNIQUE (EmpresaId, Id),
    CategoriaPadreId       INT          NULL,
    CONSTRAINT CK_Categorias_NoAutoReferencia CHECK (CategoriaPadreId IS NULL OR CategoriaPadreId <> Id),
    CONSTRAINT FK_Categorias_Padre FOREIGN KEY (EmpresaId, CategoriaPadreId) REFERENCES Categorias(EmpresaId, Id),
    Codigo                 VARCHAR(25)  NOT NULL,
    CONSTRAINT UQ_Categorias_Empresa_Codigo UNIQUE (EmpresaId, Codigo),
    Nombre                 VARCHAR(150) NOT NULL,
    CONSTRAINT UQ_Categorias_Empresa_Nombre UNIQUE (EmpresaId, Nombre),
    Descripcion            VARCHAR(500) NULL,
    Nivel                  TINYINT      NOT NULL CONSTRAINT CK_Categorias_Nivel CHECK (Nivel BETWEEN 1 AND 10),
    EstadoId INT NOT NULL CONSTRAINT FK_Categorias_Estado REFERENCES Estados(Id),
    FechaCreacion          DATETIME2(3) NOT NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_Categorias_CreadoPor REFERENCES Usuarios(Id),
    FechaModificacion      DATETIME2(3) NULL,
    ModificadoPorUsuarioId INT          NULL CONSTRAINT FK_Categorias_ModificadoPor REFERENCES Usuarios(Id),
    VersionFila            ROWVERSION   NOT NULL
);
GO

CREATE TABLE Marcas (
    Id                     INT          IDENTITY(1,1) NOT NULL CONSTRAINT PK_Marcas PRIMARY KEY,
    EmpresaId              INT          NOT NULL CONSTRAINT FK_Marcas_Empresa REFERENCES Empresas(Id),
    CONSTRAINT UQ_Marcas_Empresa_Id UNIQUE (EmpresaId, Id),
    Codigo                 VARCHAR(25)  NOT NULL,
    CONSTRAINT UQ_Marcas_Empresa_Codigo UNIQUE (EmpresaId, Codigo),
    Nombre                 VARCHAR(150) NOT NULL,
    CONSTRAINT UQ_Marcas_Empresa_Nombre UNIQUE (EmpresaId, Nombre),
    Descripcion            VARCHAR(500) NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_Marcas_Estado REFERENCES Estados(Id),
    FechaCreacion          DATETIME2(3) NOT NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_Marcas_CreadoPor REFERENCES Usuarios(Id),
    FechaModificacion      DATETIME2(3) NULL,
    ModificadoPorUsuarioId INT          NULL CONSTRAINT FK_Marcas_ModificadoPor REFERENCES Usuarios(Id),
    VersionFila            ROWVERSION   NOT NULL
);
GO

CREATE TABLE TiposProductos (
    Id                     INT          IDENTITY(1,1) NOT NULL CONSTRAINT PK_TiposProducto PRIMARY KEY,
    EmpresaId              INT          NOT NULL CONSTRAINT FK_TiposProducto_Empresa REFERENCES Empresas(Id),
    CONSTRAINT UQ_TiposProducto_Empresa_Id UNIQUE (EmpresaId, Id),
    Codigo                 VARCHAR(25)  NOT NULL,
    CONSTRAINT UQ_TiposProducto_Empresa_Codigo UNIQUE (EmpresaId, Codigo),
    Nombre                 VARCHAR(100) NOT NULL,
    CONSTRAINT UQ_TiposProducto_Empresa_Nombre UNIQUE (EmpresaId, Nombre),
    ManejaExistencia       BIT          NOT NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_TiposProductos_Estado REFERENCES Estados(Id),
    FechaCreacion          DATETIME2(3) NOT NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_TiposProducto_CreadoPor REFERENCES Usuarios(Id),
    FechaModificacion      DATETIME2(3) NULL,
    ModificadoPorUsuarioId INT          NULL CONSTRAINT FK_TiposProducto_ModificadoPor REFERENCES Usuarios(Id),
    VersionFila            ROWVERSION   NOT NULL
);
GO

CREATE TABLE Presentaciones (
    Id                     INT          IDENTITY(1,1) NOT NULL CONSTRAINT PK_Presentaciones PRIMARY KEY,
    EmpresaId              INT          NOT NULL CONSTRAINT FK_Presentaciones_Empresa REFERENCES Empresas(Id),
    CONSTRAINT UQ_Presentaciones_Empresa_Id UNIQUE (EmpresaId, Id),
    Codigo                 VARCHAR(25)  NOT NULL,
    CONSTRAINT UQ_Presentaciones_Empresa_Codigo UNIQUE (EmpresaId, Codigo),
    Nombre                 VARCHAR(100) NOT NULL,
    CONSTRAINT UQ_Presentaciones_Empresa_Nombre UNIQUE (EmpresaId, Nombre),
    Descripcion            VARCHAR(300) NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_Presentaciones_Estado REFERENCES Estados(Id),
    FechaCreacion          DATETIME2(3) NOT NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_Presentaciones_CreadoPor REFERENCES Usuarios(Id),
    FechaModificacion      DATETIME2(3) NULL,
    ModificadoPorUsuarioId INT          NULL CONSTRAINT FK_Presentaciones_ModificadoPor REFERENCES Usuarios(Id),
    VersionFila            ROWVERSION   NOT NULL
);
GO

CREATE TABLE ConceptosMovimientos (
    Id                     INT          IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConceptosMovimiento PRIMARY KEY,
    EmpresaId              INT          NOT NULL CONSTRAINT FK_ConceptosMovimientos_Empresa REFERENCES Empresas(Id),
    CONSTRAINT UQ_Conceptos_Empresa_Id UNIQUE (EmpresaId, Id),
    Codigo                 VARCHAR(25)  NOT NULL,
    CONSTRAINT UQ_Conceptos_Empresa_Codigo UNIQUE (EmpresaId, Codigo),
    Nombre                 VARCHAR(120) NOT NULL,
    CONSTRAINT UQ_Conceptos_Empresa_Nombre UNIQUE (EmpresaId, Nombre),
    TipoOperacion          VARCHAR(15)  NOT NULL CONSTRAINT CK_Conceptos_TipoOperacion CHECK (TipoOperacion IN ('ENTRADA','SALIDA','TRASLADO','AJUSTE')),
    RequiereReferencia     BIT          NOT NULL,
    AfectaCosto            BIT          NOT NULL,
    PermiteCostoCero       BIT          NOT NULL,
    EsAutomatico           BIT          NOT NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_ConceptosMovimientos_Estado REFERENCES Estados(Id),
    FechaCreacion          DATETIME2(3) NOT NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_ConceptosMovimientos_CreadoPor REFERENCES Usuarios(Id),
    FechaModificacion      DATETIME2(3) NULL,
    ModificadoPorUsuarioId INT          NULL CONSTRAINT FK_ConceptosMovimientos_ModificadoPor REFERENCES Usuarios(Id),
    VersionFila            ROWVERSION   NOT NULL
);
GO

CREATE TABLE Bodegas (
    Id                     BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bodegas PRIMARY KEY,
    EmpresaId              INT          NOT NULL CONSTRAINT FK_Bodegas_Empresa REFERENCES Empresas(Id),
    CONSTRAINT UQ_Bodegas_Empresa_Id UNIQUE (EmpresaId, Id),
    SucursalId             BIGINT       NOT NULL CONSTRAINT FK_Bodegas_Sucursal REFERENCES Sucursales(Id),
    CONSTRAINT UQ_Bodegas_Empresa_Sucursal_Id UNIQUE (EmpresaId, SucursalId, Id),
    BodegaPadreId          BIGINT       NULL,
    CONSTRAINT CK_Bodegas_NoAutoReferencia CHECK (BodegaPadreId IS NULL OR BodegaPadreId <> Id),
    CONSTRAINT FK_Bodegas_Padre FOREIGN KEY (EmpresaId, BodegaPadreId) REFERENCES Bodegas(EmpresaId, Id),
    Codigo                 VARCHAR(25)  NOT NULL,
    CONSTRAINT UQ_Bodegas_Empresa_Codigo UNIQUE (EmpresaId, Codigo),
    Nombre                 VARCHAR(150) NOT NULL,
    CONSTRAINT UQ_Bodegas_Empresa_Sucursal_Nombre UNIQUE (EmpresaId, SucursalId, Nombre),
    Descripcion            VARCHAR(500) NULL,
    PermiteExistenciaNegativa BIT       NOT NULL,
    EsPrincipal            BIT          NOT NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_Bodegas_Estado REFERENCES Estados(Id),
    FechaCreacion          DATETIME2(3) NOT NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_Bodegas_CreadoPor REFERENCES Usuarios(Id),
    FechaModificacion      DATETIME2(3) NULL,
    ModificadoPorUsuarioId INT          NULL CONSTRAINT FK_Bodegas_ModificadoPor REFERENCES Usuarios(Id),
    VersionFila            ROWVERSION   NOT NULL
);
GO

CREATE TABLE Ubicaciones (
    Id                     BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ubicaciones PRIMARY KEY,
    EmpresaId              INT          NOT NULL CONSTRAINT FK_Ubicaciones_Empresa REFERENCES Empresas(Id),
    CONSTRAINT UQ_Ubicaciones_Empresa_Id UNIQUE (EmpresaId, Id),
    BodegaId               BIGINT       NOT NULL,
    CONSTRAINT UQ_Ubicaciones_Empresa_Bodega_Id UNIQUE (EmpresaId, BodegaId, Id),
    CONSTRAINT FK_Ubicaciones_Bodega FOREIGN KEY (EmpresaId, BodegaId) REFERENCES Bodegas(EmpresaId, Id),
    UbicacionPadreId       BIGINT       NULL,
    CONSTRAINT CK_Ubicaciones_NoAutoReferencia CHECK (UbicacionPadreId IS NULL OR UbicacionPadreId <> Id),
    CONSTRAINT FK_Ubicaciones_Padre FOREIGN KEY (EmpresaId, UbicacionPadreId) REFERENCES Ubicaciones(EmpresaId, Id),
    Codigo                 VARCHAR(40)  NOT NULL,
    CONSTRAINT UQ_Ubicaciones_Empresa_Bodega_Codigo UNIQUE (EmpresaId, BodegaId, Codigo),
    Nombre                 VARCHAR(150) NOT NULL,
    TipoUbicacionId        BIGINT       NOT NULL CONSTRAINT FK_Ubicaciones_Tipo REFERENCES Catalogos(Id),
    EsRecepcion            BIT          NOT NULL,
    EsDespacho             BIT          NOT NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_Ubicaciones_Estado REFERENCES Estados(Id),
    FechaCreacion          DATETIME2(3) NOT NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_Ubicaciones_CreadoPor REFERENCES Usuarios(Id),
    FechaModificacion      DATETIME2(3) NULL,
    ModificadoPorUsuarioId INT          NULL CONSTRAINT FK_Ubicaciones_ModificadoPor REFERENCES Usuarios(Id),
    VersionFila            ROWVERSION   NOT NULL
);
GO

CREATE TABLE Productos (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_Productos PRIMARY KEY,
    EmpresaId              INT           NOT NULL CONSTRAINT FK_Productos_Empresa REFERENCES Empresas(Id),
    CONSTRAINT UQ_Productos_Empresa_Id UNIQUE (EmpresaId, Id),
    CategoriaId            INT           NOT NULL,
    CONSTRAINT FK_Productos_Categoria FOREIGN KEY (EmpresaId, CategoriaId) REFERENCES Categorias(EmpresaId, Id),
    MarcaId                INT           NULL,
    CONSTRAINT FK_Productos_Marca FOREIGN KEY (EmpresaId, MarcaId) REFERENCES Marcas(EmpresaId, Id),
    TipoProductoId         INT           NOT NULL,
    CONSTRAINT FK_Productos_Tipo FOREIGN KEY (EmpresaId, TipoProductoId) REFERENCES TiposProductos(EmpresaId, Id),
    UnidadBaseId           INT           NOT NULL,
    CONSTRAINT FK_Productos_UnidadBase FOREIGN KEY (EmpresaId, UnidadBaseId) REFERENCES UnidadesMedida(EmpresaId, Id),
    TipoSeguimientoId      BIGINT        NOT NULL CONSTRAINT FK_Productos_TipoSeguimiento REFERENCES Catalogos(Id),
    MetodoValuacionId      BIGINT        NOT NULL CONSTRAINT FK_Productos_MetodoValuacion REFERENCES Catalogos(Id),
    ControlaVencimiento    BIT           NOT NULL,
    SKU                    VARCHAR(50)   NOT NULL,
    CONSTRAINT UQ_Productos_Empresa_SKU UNIQUE (EmpresaId, SKU),
    Nombre                 VARCHAR(200)  NOT NULL,
    Descripcion            VARCHAR(1000) NULL,
    Modelo                 VARCHAR(100)  NULL,
    Imagen                 VARCHAR(300)  NULL,
    Peso                   DECIMAL(19,6) NULL CONSTRAINT CK_Productos_Peso CHECK (Peso IS NULL OR Peso >= 0),
    EsComprable            BIT           NOT NULL,
    EsVendible             BIT           NOT NULL,
    PermiteDescuento       BIT           NOT NULL,
    EsCombo                BIT           DEFAULT 0 NOT NULL,
    PrecioVentaReferencial DECIMAL(10,2) NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_Productos_Estado REFERENCES Estados(Id),
    FechaCreacion          DATETIME2(3)  NOT NULL,
    CreadoPorUsuarioId     INT           NOT NULL CONSTRAINT FK_Productos_CreadoPor REFERENCES Usuarios(Id),
    FechaModificacion      DATETIME2(3)  NULL,
    ModificadoPorUsuarioId INT           NULL CONSTRAINT FK_Productos_ModificadoPor REFERENCES Usuarios(Id),
    VersionFila            ROWVERSION    NOT NULL
);
GO

CREATE TABLE ProductoPresentaciones (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductoPresentaciones PRIMARY KEY,
    EmpresaId              INT           NOT NULL CONSTRAINT FK_ProductoPresentaciones_Empresa REFERENCES Empresas(Id),
    CONSTRAINT UQ_ProductoPresentaciones_Empresa_Id UNIQUE (EmpresaId, Id),
    ProductoId             BIGINT        NOT NULL,
    CONSTRAINT UQ_ProductoPresentaciones_Producto_Id UNIQUE (EmpresaId, ProductoId, Id),
    CONSTRAINT FK_ProductoPresentaciones_Producto FOREIGN KEY (EmpresaId, ProductoId) REFERENCES Productos(EmpresaId, Id),
    PresentacionId         INT           NOT NULL,
    CONSTRAINT UQ_ProductoPresentaciones_Producto_Presentacion UNIQUE (EmpresaId, ProductoId, PresentacionId),
    CONSTRAINT FK_ProductoPresentaciones_Presentacion FOREIGN KEY (EmpresaId, PresentacionId) REFERENCES Presentaciones(EmpresaId, Id),
    UnidadMedidaId         INT           NOT NULL,
    CONSTRAINT FK_ProductoPresentaciones_Unidad FOREIGN KEY (EmpresaId, UnidadMedidaId) REFERENCES UnidadesMedida(EmpresaId, Id),
    FactorConversion       DECIMAL(19,6) NOT NULL CONSTRAINT CK_ProductoPresentaciones_Factor CHECK (FactorConversion > 0),
    EsPresentacionBase     BIT           NOT NULL,
    PermiteCompra          BIT           NOT NULL,
    PermiteVenta           BIT           NOT NULL,
    PrecioVentaEstandar    DECIMAL(10,2) NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_ProductoPresentaciones_Estado REFERENCES Estados(Id),
    FechaCreacion          DATETIME2(3)  NOT NULL,
    CreadoPorUsuarioId     INT           NOT NULL CONSTRAINT FK_ProductoPresentaciones_CreadoPor REFERENCES Usuarios(Id),
    FechaModificacion      DATETIME2(3)  NULL,
    ModificadoPorUsuarioId INT           NULL CONSTRAINT FK_ProductoPresentaciones_ModificadoPor REFERENCES Usuarios(Id),
    VersionFila            ROWVERSION    NOT NULL
);
GO

CREATE TABLE CodigosBarrasProductos (
    Id                     BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_CodigosBarrasProducto PRIMARY KEY,
    EmpresaId              INT          NOT NULL CONSTRAINT FK_CodigosBarrasProductos_Empresa REFERENCES Empresas(Id),
    ProductoId             BIGINT       NOT NULL,
    CONSTRAINT FK_CodigosBarras_Producto FOREIGN KEY (EmpresaId, ProductoId) REFERENCES Productos(EmpresaId, Id),
    ProductoPresentacionId BIGINT       NULL,
    CONSTRAINT FK_CodigosBarras_Presentacion FOREIGN KEY (EmpresaId, ProductoId, ProductoPresentacionId) REFERENCES ProductoPresentaciones(EmpresaId, ProductoId, Id),
    CodigoBarras           VARCHAR(100) NOT NULL,
    CONSTRAINT UQ_CodigosBarras_Empresa_Codigo UNIQUE (EmpresaId, CodigoBarras),
    TipoCodigoBarrasId     BIGINT       NOT NULL CONSTRAINT FK_CodigosBarras_Tipo REFERENCES Catalogos(Id),
    EsPrincipal            BIT          NOT NULL,
    EstadoId INT NOT NULL CONSTRAINT FK_CodigosBarrasProductos_Estado REFERENCES Estados(Id),
    FechaCreacion          DATETIME2(3) NOT NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_CodigosBarras_CreadoPor REFERENCES Usuarios(Id)
);
GO

CREATE TABLE CombosDetalle (
    Id                 BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_CombosDetalle PRIMARY KEY,
    EmpresaId          INT          NOT NULL CONSTRAINT FK_CombosDetalle_Empresa REFERENCES Empresas(Id),
    ComboProductoId    BIGINT       NOT NULL,
    CONSTRAINT FK_Combos_ProductoPadre FOREIGN KEY (EmpresaId, ComboProductoId) REFERENCES Productos(EmpresaId, Id),
    ProductoIncluidoId BIGINT       NOT NULL,
    CONSTRAINT FK_Combos_ProductoHijo FOREIGN KEY (EmpresaId, ProductoIncluidoId) REFERENCES Productos(EmpresaId, Id),
    Cantidad           INT          DEFAULT 1 NOT NULL
);
GO

CREATE TABLE ParametrosStockProductos (
    Id                        BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_ParametrosStockProducto PRIMARY KEY,
    EmpresaId                 INT           NOT NULL CONSTRAINT FK_ParametrosStockProductos_Empresa REFERENCES Empresas(Id),
    ProductoId                BIGINT        NOT NULL,
    CONSTRAINT FK_Parametros_Producto FOREIGN KEY (EmpresaId, ProductoId) REFERENCES Productos(EmpresaId, Id),
    BodegaId                  BIGINT        NOT NULL,
    CONSTRAINT UQ_Parametros_Producto_Bodega UNIQUE (EmpresaId, ProductoId, BodegaId),
    CONSTRAINT FK_Parametros_Bodega FOREIGN KEY (EmpresaId, BodegaId) REFERENCES Bodegas(EmpresaId, Id),
    UbicacionPredeterminadaId BIGINT        NULL,
    CONSTRAINT FK_Parametros_Ubicacion FOREIGN KEY (EmpresaId, UbicacionPredeterminadaId) REFERENCES Ubicaciones(EmpresaId, Id),
    StockMinimo               DECIMAL(19,6) NOT NULL,
    StockMaximo               DECIMAL(19,6) NULL,
    PuntoReorden              DECIMAL(19,6) NOT NULL,
    CantidadReorden           DECIMAL(19,6) NULL,
    DiasReposicion            SMALLINT      NULL,
    CONSTRAINT CK_Parametros_Stock CHECK (
        StockMinimo >= 0 AND PuntoReorden >= 0
        AND (StockMaximo IS NULL OR StockMaximo >= StockMinimo)
        AND (CantidadReorden IS NULL OR CantidadReorden > 0)
        AND (DiasReposicion IS NULL OR DiasReposicion >= 0)
        ),
    EstadoId INT NOT NULL CONSTRAINT FK_ParametrosStockProductos_Estado REFERENCES Estados(Id),
    FechaCreacion             DATETIME2(3)  NOT NULL,
    CreadoPorUsuarioId        INT           NOT NULL CONSTRAINT FK_ParametrosStockProductos_CreadoPor REFERENCES Usuarios(Id),
    FechaModificacion         DATETIME2(3)  NULL,
    ModificadoPorUsuarioId    INT           NULL CONSTRAINT FK_ParametrosStockProductos_ModificadoPor REFERENCES Usuarios(Id),
    VersionFila               ROWVERSION    NOT NULL
);
GO

CREATE TABLE Lotes (
    Id                     BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_Lotes PRIMARY KEY,
    EmpresaId              INT          NOT NULL CONSTRAINT FK_Lotes_Empresa REFERENCES Empresas(Id),
    CONSTRAINT UQ_Lotes_Empresa_Id UNIQUE (EmpresaId, Id),
    ProductoId             BIGINT       NOT NULL,
    CONSTRAINT UQ_Lotes_Producto_Id UNIQUE (EmpresaId, ProductoId, Id),
    CONSTRAINT FK_Lotes_Producto FOREIGN KEY (EmpresaId, ProductoId) REFERENCES Productos(EmpresaId, Id),
    NumeroLote             VARCHAR(100) NOT NULL,
    CONSTRAINT UQ_Lotes_Producto_Numero UNIQUE (EmpresaId, ProductoId, NumeroLote),
    FechaFabricacion       DATE         NULL,
    FechaVencimiento       DATE         NULL,
    CONSTRAINT CK_Lotes_Fechas CHECK (FechaVencimiento IS NULL OR FechaFabricacion IS NULL OR FechaVencimiento >= FechaFabricacion),
    FlujoEstadoId          INT          NOT NULL,
    EstadoId               INT          NOT NULL,
    CONSTRAINT FK_Lotes_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    Observaciones          VARCHAR(500) NULL,
    FechaCreacion          DATETIME2(3) NOT NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_Lotes_CreadoPor REFERENCES Usuarios(Id),
    VersionFila            ROWVERSION   NOT NULL
);
GO

CREATE TABLE SeriesProductos (
    Id                     BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeriesProducto PRIMARY KEY,
    EmpresaId              INT          NOT NULL CONSTRAINT FK_SeriesProductos_Empresa REFERENCES Empresas(Id),
    CONSTRAINT UQ_Series_Empresa_Id UNIQUE (EmpresaId, Id),
    ProductoId             BIGINT       NOT NULL,
    CONSTRAINT UQ_Series_Producto_Id UNIQUE (EmpresaId, ProductoId, Id),
    CONSTRAINT FK_Series_Producto FOREIGN KEY (EmpresaId, ProductoId) REFERENCES Productos(EmpresaId, Id),
    LoteId                 BIGINT       NULL,
    CONSTRAINT FK_Series_Lote FOREIGN KEY (EmpresaId, ProductoId, LoteId) REFERENCES Lotes(EmpresaId, ProductoId, Id),
    NumeroSerie            VARCHAR(150) NOT NULL,
    CONSTRAINT UQ_Series_Producto_Numero UNIQUE (EmpresaId, ProductoId, NumeroSerie),
    FlujoEstadoId          INT          NOT NULL,
    EstadoId               INT          NOT NULL,
    CONSTRAINT FK_Series_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    BodegaId               BIGINT       NULL,
    CONSTRAINT FK_Series_Bodega FOREIGN KEY (EmpresaId, BodegaId) REFERENCES Bodegas(EmpresaId, Id),
    UbicacionId            BIGINT       NULL,
    CONSTRAINT FK_Series_Ubicacion FOREIGN KEY (EmpresaId, UbicacionId) REFERENCES Ubicaciones(EmpresaId, Id),
    FechaIngreso           DATETIME2(3) NULL,
    FechaSalida            DATETIME2(3) NULL,
    CONSTRAINT CK_Series_Fechas CHECK (FechaSalida IS NULL OR FechaIngreso IS NULL OR FechaSalida >= FechaIngreso),
    FechaCreacion          DATETIME2(3) NOT NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_SeriesProductos_CreadoPor REFERENCES Usuarios(Id),
    VersionFila            ROWVERSION   NOT NULL
);
GO

CREATE TABLE Existencias (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_Existencias PRIMARY KEY,
    EmpresaId              INT           NOT NULL CONSTRAINT FK_Existencias_Empresa REFERENCES Empresas(Id),
    CONSTRAINT UQ_Existencias_Empresa_Id UNIQUE (EmpresaId, Id),
    ProductoId             BIGINT        NOT NULL,
    CONSTRAINT UQ_Existencias_Producto_Id UNIQUE (EmpresaId, ProductoId, Id),
    CONSTRAINT FK_Existencias_Producto FOREIGN KEY (EmpresaId, ProductoId) REFERENCES Productos(EmpresaId, Id),
    BodegaId               BIGINT        NOT NULL,
    CONSTRAINT FK_Existencias_Bodega FOREIGN KEY (EmpresaId, BodegaId) REFERENCES Bodegas(EmpresaId, Id),
    UbicacionId            BIGINT        NOT NULL,
    CONSTRAINT UQ_Existencias_Producto_Bodega_Ubicacion UNIQUE (EmpresaId, ProductoId, BodegaId, UbicacionId),
    CONSTRAINT FK_Existencias_Ubicacion FOREIGN KEY (EmpresaId, BodegaId, UbicacionId) REFERENCES Ubicaciones(EmpresaId, BodegaId, Id),
    Cantidad               DECIMAL(19,6) NOT NULL,
    CantidadReservada      DECIMAL(19,6) NOT NULL CONSTRAINT CK_Existencias_Reservada CHECK (CantidadReservada >= 0),
    CantidadDisponible     AS (Cantidad - CantidadReservada) PERSISTED,
    CostoPromedio          DECIMAL(19,6) NOT NULL CONSTRAINT CK_Existencias_Costo CHECK (CostoPromedio >= 0),
    UltimoMovimiento       DATETIME2(3)  NULL,
    VersionFila            ROWVERSION    NOT NULL
);
GO

CREATE TABLE ExistenciasLote (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExistenciasLote PRIMARY KEY,
    EmpresaId              INT           NOT NULL CONSTRAINT FK_ExistenciasLote_Empresa REFERENCES Empresas(Id),
    ProductoId             BIGINT        NOT NULL,
    ExistenciaId           BIGINT        NOT NULL,
    CONSTRAINT FK_ExistenciasLote_Existencia FOREIGN KEY (EmpresaId, ProductoId, ExistenciaId) REFERENCES Existencias(EmpresaId, ProductoId, Id),
    LoteId                 BIGINT        NOT NULL,
    CONSTRAINT UQ_ExistenciasLote_Existencia_Lote UNIQUE (EmpresaId, ExistenciaId, LoteId),
    CONSTRAINT FK_ExistenciasLote_Lote FOREIGN KEY (EmpresaId, ProductoId, LoteId) REFERENCES Lotes(EmpresaId, ProductoId, Id),
    Cantidad               DECIMAL(19,6) NOT NULL,
    CantidadReservada      DECIMAL(19,6) NOT NULL CONSTRAINT CK_ExistenciasLote_Reservada CHECK (CantidadReservada >= 0),
    CantidadDisponible     AS (Cantidad - CantidadReservada) PERSISTED,
    UltimoMovimiento       DATETIME2(3)  NULL,
    VersionFila            ROWVERSION    NOT NULL
);
GO

CREATE TABLE Movimientos (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_Movimientos PRIMARY KEY,
    EmpresaId              INT           NOT NULL CONSTRAINT FK_Movimientos_Empresa REFERENCES Empresas(Id),
    CONSTRAINT UQ_Movimientos_Empresa_Id UNIQUE (EmpresaId, Id),
    SucursalId             BIGINT        NOT NULL CONSTRAINT FK_Movimientos_Sucursal REFERENCES Sucursales(Id),
    ConceptoMovimientoId   INT           NOT NULL,
    CONSTRAINT FK_Movimientos_Concepto FOREIGN KEY (EmpresaId, ConceptoMovimientoId) REFERENCES ConceptosMovimientos(EmpresaId, Id),
    BodegaOrigenId         BIGINT        NULL,
    CONSTRAINT FK_Movimientos_BodegaOrigen FOREIGN KEY (EmpresaId, BodegaOrigenId) REFERENCES Bodegas(EmpresaId, Id),
    BodegaDestinoId        BIGINT        NULL,
    CONSTRAINT CK_Movimientos_Bodegas CHECK (BodegaOrigenId IS NOT NULL OR BodegaDestinoId IS NOT NULL),
    CONSTRAINT CK_Movimientos_BodegasDiferentes CHECK (BodegaOrigenId IS NULL OR BodegaDestinoId IS NULL OR BodegaOrigenId <> BodegaDestinoId),
    CONSTRAINT FK_Movimientos_BodegaDestino FOREIGN KEY (EmpresaId, BodegaDestinoId) REFERENCES Bodegas(EmpresaId, Id),
    SerieDocumento         VARCHAR(20)   NOT NULL,
    NumeroDocumento        BIGINT        NOT NULL CONSTRAINT CK_Movimientos_Numero CHECK (NumeroDocumento > 0),
    CONSTRAINT UQ_Movimientos_Documento UNIQUE (EmpresaId, SerieDocumento, NumeroDocumento),
    FechaMovimiento        DATETIME2(3)  NOT NULL,
    FlujoEstadoId          INT           NOT NULL,
    EstadoId               INT           NOT NULL,
    CONSTRAINT FK_Movimientos_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    ReferenciaExterna      VARCHAR(100)  NULL,
    ModuloOrigen           VARCHAR(30)   NULL,
    DocumentoOrigenId      BIGINT        NULL,
    Observaciones          VARCHAR(1000) NULL,
    AplicadoPorUsuarioId   INT           NULL CONSTRAINT FK_Movimientos_AplicadoPor REFERENCES Usuarios(Id),
    FechaAplicacion        DATETIME2(3)  NULL,
    CONSTRAINT CK_Movimientos_Aplicacion CHECK ((AplicadoPorUsuarioId IS NULL AND FechaAplicacion IS NULL) OR (AplicadoPorUsuarioId IS NOT NULL AND FechaAplicacion IS NOT NULL)),
    AnuladoPorUsuarioId    INT           NULL CONSTRAINT FK_Movimientos_AnuladoPor REFERENCES Usuarios(Id),
    FechaAnulacion         DATETIME2(3)  NULL,
    MotivoAnulacionId      BIGINT        NULL CONSTRAINT FK_Movimientos_MotivoAnulacion REFERENCES Catalogos(Id),
    CONSTRAINT CK_Movimientos_Anulacion CHECK ((AnuladoPorUsuarioId IS NULL AND FechaAnulacion IS NULL AND MotivoAnulacionId IS NULL) OR (AnuladoPorUsuarioId IS NOT NULL AND FechaAnulacion IS NOT NULL AND MotivoAnulacionId IS NOT NULL)),
    ComentarioAnulacion    VARCHAR(500)  NULL,
    FechaCreacion          DATETIME2(3)  NOT NULL,
    CreadoPorUsuarioId     INT           NOT NULL CONSTRAINT FK_Movimientos_CreadoPor REFERENCES Usuarios(Id)
);
GO

CREATE TABLE MovimientoDetalles (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_MovimientoDetalles PRIMARY KEY,
    EmpresaId              INT           NOT NULL CONSTRAINT FK_MovimientoDetalles_Empresa REFERENCES Empresas(Id),
    CONSTRAINT UQ_MovimientoDetalles_Empresa_Id UNIQUE (EmpresaId, Id),
    MovimientoId           BIGINT        NOT NULL,
    CONSTRAINT FK_MovimientoDetalles_Movimiento FOREIGN KEY (EmpresaId, MovimientoId) REFERENCES Movimientos(EmpresaId, Id),
    NumeroLinea            INT           NOT NULL CONSTRAINT CK_MovimientoDetalles_Linea CHECK (NumeroLinea > 0),
    CONSTRAINT UQ_MovimientoDetalles_Linea UNIQUE (EmpresaId, MovimientoId, NumeroLinea),
    ProductoId             BIGINT        NOT NULL,
    CONSTRAINT FK_MovimientoDetalles_Producto FOREIGN KEY (EmpresaId, ProductoId) REFERENCES Productos(EmpresaId, Id),
    ProductoPresentacionId BIGINT        NULL,
    CONSTRAINT FK_MovimientoDetalles_Presentacion FOREIGN KEY (EmpresaId, ProductoId, ProductoPresentacionId) REFERENCES ProductoPresentaciones(EmpresaId, ProductoId, Id),
    UnidadMedidaId         INT           NOT NULL,
    CONSTRAINT FK_MovimientoDetalles_Unidad FOREIGN KEY (EmpresaId, UnidadMedidaId) REFERENCES UnidadesMedida(EmpresaId, Id),
    UbicacionOrigenId      BIGINT        NULL,
    CONSTRAINT FK_MovimientoDetalles_UbicacionOrigen FOREIGN KEY (EmpresaId, UbicacionOrigenId) REFERENCES Ubicaciones(EmpresaId, Id),
    UbicacionDestinoId     BIGINT        NULL,
    CONSTRAINT FK_MovimientoDetalles_UbicacionDestino FOREIGN KEY (EmpresaId, UbicacionDestinoId) REFERENCES Ubicaciones(EmpresaId, Id),
    LoteId                 BIGINT        NULL,
    CONSTRAINT FK_MovimientoDetalles_Lote FOREIGN KEY (EmpresaId, ProductoId, LoteId) REFERENCES Lotes(EmpresaId, ProductoId, Id),
    Cantidad               DECIMAL(19,6) NOT NULL,
    FactorConversion       DECIMAL(19,6) NOT NULL,
    CantidadBase           DECIMAL(19,6) NOT NULL,
    CONSTRAINT CK_MovimientoDetalles_Cantidades CHECK (Cantidad > 0 AND FactorConversion > 0 AND CantidadBase > 0),
    CostoUnitario          DECIMAL(19,6) NOT NULL CONSTRAINT CK_MovimientoDetalles_Costo CHECK (CostoUnitario >= 0),
    CostoTotal             AS (CantidadBase * CostoUnitario) PERSISTED,
    Observaciones          VARCHAR(500)  NULL
);
GO

CREATE TABLE MovimientoDetalleSeries (
    Id                     BIGINT NOT NULL IDENTITY(1,1) CONSTRAINT PK_MovimientoDetalleSeries PRIMARY KEY,
    EmpresaId              INT    NOT NULL CONSTRAINT FK_MovimientoDetalleSeries_Empresa REFERENCES Empresas(Id),
    MovimientoDetalleId    BIGINT NOT NULL,
    CONSTRAINT FK_MovimientoDetalleSeries_Detalle FOREIGN KEY (EmpresaId, MovimientoDetalleId) REFERENCES MovimientoDetalles(EmpresaId, Id),
    SerieProductoId        BIGINT NOT NULL,
    CONSTRAINT UQ_MovimientoDetalleSeries UNIQUE (EmpresaId, MovimientoDetalleId, SerieProductoId),
    CONSTRAINT FK_MovimientoDetalleSeries_Serie FOREIGN KEY (EmpresaId, SerieProductoId) REFERENCES SeriesProductos(EmpresaId, Id)
);
GO

CREATE TABLE Kardex (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_Kardex PRIMARY KEY,
    EmpresaId              INT           NOT NULL CONSTRAINT FK_Kardex_Empresa REFERENCES Empresas(Id),
    ProductoId             BIGINT        NOT NULL,
    CONSTRAINT FK_Kardex_Producto FOREIGN KEY (EmpresaId, ProductoId) REFERENCES Productos(EmpresaId, Id),
    BodegaId               BIGINT        NOT NULL,
    CONSTRAINT FK_Kardex_Bodega FOREIGN KEY (EmpresaId, BodegaId) REFERENCES Bodegas(EmpresaId, Id),
    UbicacionId            BIGINT        NOT NULL,
    CONSTRAINT FK_Kardex_Ubicacion FOREIGN KEY (EmpresaId, UbicacionId) REFERENCES Ubicaciones(EmpresaId, Id),
    LoteId                 BIGINT        NULL,
    CONSTRAINT FK_Kardex_Lote FOREIGN KEY (EmpresaId, ProductoId, LoteId) REFERENCES Lotes(EmpresaId, ProductoId, Id),
    MovimientoDetalleId    BIGINT        NOT NULL,
    CONSTRAINT FK_Kardex_MovimientoDetalle FOREIGN KEY (EmpresaId, MovimientoDetalleId) REFERENCES MovimientoDetalles(EmpresaId, Id),
    FechaMovimiento        DATETIME2(3)  NOT NULL,
    TipoAfectacion VARCHAR(10) NOT NULL CONSTRAINT CK_Kardex_TipoAfectacion CHECK (TipoAfectacion IN ('ENTRADA','SALIDA')),
    CONSTRAINT UQ_Kardex_Detalle_Bodega_Afectacion UNIQUE (MovimientoDetalleId, BodegaId, TipoAfectacion),
    Cantidad               DECIMAL(19,6) NOT NULL CONSTRAINT CK_Kardex_Cantidad CHECK (Cantidad > 0),
    CostoUnitario          DECIMAL(19,6) NOT NULL,
    CostoTotal             AS (Cantidad * CostoUnitario) PERSISTED,
    SaldoCantidad          DECIMAL(19,6) NOT NULL,
    SaldoCosto             DECIMAL(19,6) NOT NULL,
    CONSTRAINT CK_Kardex_Costos CHECK (CostoUnitario >= 0 AND SaldoCosto >= 0),
    FechaRegistro          DATETIME2(3)  NOT NULL
);
GO


--Inventario Complementario

CREATE TABLE ReservasInventario (
    Id                     BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReservasInventario PRIMARY KEY,
    EmpresaId              INT          NOT NULL CONSTRAINT FK_ReservasInventario_Empresa REFERENCES Empresas(Id),
    SucursalId             BIGINT       NOT NULL CONSTRAINT FK_ReservasInventario_Sucursal REFERENCES Sucursales(Id),
    CodigoReserva          VARCHAR(50)  NOT NULL,
    ModuloOrigen           VARCHAR(30)  NOT NULL,
    DocumentoOrigenId      BIGINT       NULL,
    FechaReserva           DATETIME2(3) NOT NULL,
    FechaExpiracion        DATETIME2(3) NULL,
    FlujoEstadoId          INT          NOT NULL,
    EstadoId               INT          NOT NULL,
    CONSTRAINT FK_ReservasInventario_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    Observaciones          VARCHAR(500) NULL,
    CreadoPorUsuarioId     INT          NOT NULL CONSTRAINT FK_ReservasInventario_CreadoPor REFERENCES Usuarios(Id),
    LiberadoPorUsuarioId   INT          NULL CONSTRAINT FK_ReservasInventario_LiberadoPor REFERENCES Usuarios(Id),
    FechaLiberacion        DATETIME2(3) NULL,
    VersionFila            ROWVERSION   NOT NULL,
    CONSTRAINT UQ_ReservasInventario_Empresa_Codigo UNIQUE (EmpresaId, CodigoReserva),
    CONSTRAINT UQ_ReservasInventario_Empresa_Id UNIQUE (EmpresaId, Id),
    CONSTRAINT CK_ReservasInventario_Expiracion CHECK (FechaExpiracion IS NULL OR FechaExpiracion > FechaReserva)
);
GO

CREATE TABLE ReservaInventarioDetalles (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReservaInventarioDetalles PRIMARY KEY,
    EmpresaId              INT           NOT NULL CONSTRAINT FK_ReservaInventarioDetalles_Empresa REFERENCES Empresas(Id),
    ReservaId              BIGINT        NOT NULL,
    CONSTRAINT FK_ReservaInventarioDetalles_Reserva FOREIGN KEY (EmpresaId, ReservaId) REFERENCES ReservasInventario(EmpresaId, Id),
    NumeroLinea            INT           NOT NULL CONSTRAINT CK_ReservaInventarioDetalles_Linea CHECK (NumeroLinea > 0),
    ProductoId             BIGINT        NOT NULL CONSTRAINT FK_ReservaInventarioDetalles_Producto REFERENCES Productos(Id),
    BodegaId               BIGINT        NOT NULL CONSTRAINT FK_ReservaInventarioDetalles_Bodega REFERENCES Bodegas(Id),
    UbicacionId            BIGINT        NOT NULL CONSTRAINT FK_ReservaInventarioDetalles_Ubicacion REFERENCES Ubicaciones(Id),
    LoteId                 BIGINT        NULL CONSTRAINT FK_ReservaInventarioDetalles_Lote REFERENCES Lotes(Id),
    SerieProductoId        BIGINT        NULL CONSTRAINT FK_ReservaInventarioDetalles_Serie REFERENCES SeriesProductos(Id),
    Cantidad               DECIMAL(19,6) NOT NULL CONSTRAINT CK_ReservaInventarioDetalles_Cantidad CHECK (Cantidad > 0),
    CantidadAplicada       DECIMAL(19,6) NOT NULL,
    CantidadLiberada       DECIMAL(19,6) NOT NULL,
    VersionFila            ROWVERSION    NOT NULL,
    CONSTRAINT UQ_ReservaInventarioDetalles_Linea UNIQUE (EmpresaId, ReservaId, NumeroLinea),
    CONSTRAINT CK_ReservaInventarioDetalles_Aplicacion CHECK (CantidadAplicada >= 0 AND CantidadLiberada >= 0 AND CantidadAplicada + CantidadLiberada <= Cantidad)
);
GO

CREATE TABLE ConteosFisicos (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConteosFisicos PRIMARY KEY,
    EmpresaId              INT           NOT NULL CONSTRAINT FK_ConteosFisicos_Empresa REFERENCES Empresas(Id),
    SucursalId             BIGINT        NOT NULL CONSTRAINT FK_ConteosFisicos_Sucursal REFERENCES Sucursales(Id),
    BodegaId               BIGINT        NOT NULL CONSTRAINT FK_ConteosFisicos_Bodega REFERENCES Bodegas(Id),
    CodigoConteo           VARCHAR(50)   NOT NULL,
    TipoConteoId           BIGINT        NOT NULL CONSTRAINT FK_ConteosFisicos_Tipo REFERENCES Catalogos(Id),
    FechaCorte             DATETIME2(3)  NOT NULL,
    FlujoEstadoId          INT           NOT NULL,
    EstadoId               INT           NOT NULL,
    CONSTRAINT FK_ConteosFisicos_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    PermiteReconteo        BIT           NOT NULL,
    Observaciones          VARCHAR(1000) NULL,
    IniciadoPorUsuarioId   INT           NULL CONSTRAINT FK_ConteosFisicos_IniciadoPor REFERENCES Usuarios(Id),
    FechaInicio            DATETIME2(3)  NULL,
    CerradoPorUsuarioId    INT           NULL CONSTRAINT FK_ConteosFisicos_CerradoPor REFERENCES Usuarios(Id),
    FechaCierre            DATETIME2(3)  NULL,
    MovimientoAjusteId     BIGINT        NULL CONSTRAINT FK_ConteosFisicos_MovimientoAjuste REFERENCES Movimientos(Id),
    CreadoPorUsuarioId     INT           NOT NULL CONSTRAINT FK_ConteosFisicos_CreadoPor REFERENCES Usuarios(Id),
    VersionFila            ROWVERSION    NOT NULL,
    CONSTRAINT UQ_ConteosFisicos_Empresa_Codigo UNIQUE (EmpresaId, CodigoConteo),
    CONSTRAINT UQ_ConteosFisicos_Empresa_Id UNIQUE (EmpresaId, Id),
    CONSTRAINT CK_ConteosFisicos_Fechas CHECK (FechaCierre IS NULL OR FechaInicio IS NULL OR FechaCierre >= FechaInicio)
);
GO

CREATE TABLE ConteoFisicoDetalles (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConteoFisicoDetalles PRIMARY KEY,
    EmpresaId              INT           NOT NULL CONSTRAINT FK_ConteoFisicoDetalles_Empresa REFERENCES Empresas(Id),
    ConteoFisicoId         BIGINT        NOT NULL,
    CONSTRAINT FK_ConteoFisicoDetalles_Conteo FOREIGN KEY (EmpresaId, ConteoFisicoId) REFERENCES ConteosFisicos(EmpresaId, Id),
    NumeroLinea            INT           NOT NULL CONSTRAINT CK_ConteoFisicoDetalles_Linea CHECK (NumeroLinea > 0),
    ProductoId             BIGINT        NOT NULL CONSTRAINT FK_ConteoFisicoDetalles_Producto REFERENCES Productos(Id),
    UbicacionId            BIGINT        NOT NULL CONSTRAINT FK_ConteoFisicoDetalles_Ubicacion REFERENCES Ubicaciones(Id),
    LoteId                 BIGINT        NULL CONSTRAINT FK_ConteoFisicoDetalles_Lote REFERENCES Lotes(Id),
    SerieProductoId        BIGINT        NULL CONSTRAINT FK_ConteoFisicoDetalles_Serie REFERENCES SeriesProductos(Id),
    ExistenciaSistema      DECIMAL(19,6) NOT NULL,
    CantidadPrimerConteo   DECIMAL(19,6) NULL,
    CantidadReconteo       DECIMAL(19,6) NULL,
    CantidadFinal          DECIMAL(19,6) NULL,
    Diferencia             AS (CantidadFinal - ExistenciaSistema) PERSISTED,
    ContadoPorUsuarioId    INT           NULL CONSTRAINT FK_ConteoFisicoDetalles_ContadoPor REFERENCES Usuarios(Id),
    FechaConteo            DATETIME2(3)  NULL,
    Observaciones          VARCHAR(500)  NULL,
    CONSTRAINT UQ_ConteoFisicoDetalles_Linea UNIQUE (EmpresaId, ConteoFisicoId, NumeroLinea),
    CONSTRAINT CK_ConteoFisicoDetalles_Cantidades CHECK ((CantidadPrimerConteo IS NULL OR CantidadPrimerConteo >= 0) AND (CantidadReconteo IS NULL OR CantidadReconteo >= 0) AND (CantidadFinal IS NULL OR CantidadFinal >= 0))
);
GO

CREATE TABLE TrasladosInventario (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_TrasladosInventario PRIMARY KEY,
    EmpresaId          INT           NOT NULL CONSTRAINT FK_TrasladosInventario_Empresa REFERENCES Empresas(Id),
    SucursalOrigenId   BIGINT        NOT NULL CONSTRAINT FK_TrasladosInventario_SucursalOrigen REFERENCES Sucursales(Id),
    SucursalDestinoId  BIGINT        NOT NULL CONSTRAINT FK_TrasladosInventario_SucursalDestino REFERENCES Sucursales(Id),
    BodegaOrigenId     BIGINT        NOT NULL CONSTRAINT FK_TrasladosInventario_BodegaOrigen REFERENCES Bodegas(Id),
    BodegaDestinoId    BIGINT        NOT NULL CONSTRAINT FK_TrasladosInventario_BodegaDestino REFERENCES Bodegas(Id),
    NumeroTraslado     VARCHAR(30)   NOT NULL,
    FechaTraslado      DATETIME2(3)  NOT NULL,
    FlujoEstadoId      INT           NOT NULL,
    EstadoId           INT           NOT NULL,
    CONSTRAINT FK_TrasladosInventario_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    CreadoPorUsuarioId INT           NOT NULL CONSTRAINT FK_TrasladosInventario_CreadoPor REFERENCES Usuarios(Id),
    Observaciones      VARCHAR(500)  NULL,
    CONSTRAINT UQ_TrasladosInventario_Numero UNIQUE (EmpresaId, NumeroTraslado),
    CONSTRAINT CK_TrasladosInventario_Destinos CHECK (SucursalOrigenId <> SucursalDestinoId OR BodegaOrigenId <> BodegaDestinoId)
);
GO

CREATE TABLE TrasladoInventarioDetalles (
    Id              BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_TrasladoInventarioDetalles PRIMARY KEY,
    TrasladoId      BIGINT        NOT NULL CONSTRAINT FK_TrasladoInventarioDetalles_Traslado REFERENCES TrasladosInventario(Id),
    ProductoId      BIGINT        NOT NULL CONSTRAINT FK_TrasladoInventarioDetalles_Producto REFERENCES Productos(Id),
    UnidadMedidaId  INT           NOT NULL CONSTRAINT FK_TrasladoInventarioDetalles_Unidad REFERENCES UnidadesMedida(Id),
    LoteId          BIGINT        NULL CONSTRAINT FK_TrasladoInventarioDetalles_Lote REFERENCES Lotes(Id),
    Cantidad        DECIMAL(19,6) NOT NULL CONSTRAINT CK_TrasladoInventarioDetalles_Cantidad CHECK (Cantidad > 0)
);
GO

CREATE TABLE CostosProductos (
    Id              BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_CostosProductos PRIMARY KEY,
    EmpresaId       INT           NOT NULL CONSTRAINT FK_CostosProductos_Empresa REFERENCES Empresas(Id),
    ProductoId      BIGINT        NOT NULL CONSTRAINT FK_CostosProductos_Producto REFERENCES Productos(Id),
    CostoActual     DECIMAL(19,6) NOT NULL CONSTRAINT CK_CostosProductos_Costo CHECK (CostoActual >= 0),
    FechaCalculo    DATETIME2(3)  NOT NULL,
    MetodoValuacionId BIGINT      NULL CONSTRAINT FK_CostosProductos_Metodo REFERENCES Catalogos(Id),
    CONSTRAINT UQ_CostosProductos_Empresa_Producto UNIQUE (EmpresaId, ProductoId)
);
GO

CREATE TABLE HistoricoCostosProductos (
    Id              BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_HistoricoCostosProductos PRIMARY KEY,
    EmpresaId       INT           NOT NULL CONSTRAINT FK_HistoricoCostosProductos_Empresa REFERENCES Empresas(Id),
    ProductoId      BIGINT        NOT NULL CONSTRAINT FK_HistoricoCostosProductos_Producto REFERENCES Productos(Id),
    CostoAnterior   DECIMAL(19,6) NULL,
    CostoNuevo      DECIMAL(19,6) NOT NULL CONSTRAINT CK_HistoricoCostosProductos_Costo CHECK (CostoNuevo >= 0),
    FechaCambio     DATETIME2(3)  NOT NULL,
    OrigenCambio    VARCHAR(50)   NOT NULL,
    DocumentoOrigenId BIGINT      NULL,
    UsuarioId       INT           NULL CONSTRAINT FK_HistoricoCostosProductos_Usuario REFERENCES Usuarios(Id)
);
GO


--Precios

CREATE TABLE ListasPrecios (
    Id                  INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_ListasPrecios PRIMARY KEY,
    EmpresaId           INT           NOT NULL CONSTRAINT FK_ListasPrecios_Empresa REFERENCES Empresas(Id),
    Codigo              VARCHAR(20)   NOT NULL,
    Nombre              VARCHAR(100)  NOT NULL,
    VigenciaDesde       DATE          NOT NULL,
    VigenciaHasta       DATE          NULL,
    AplicaSucursalId    BIGINT        NULL CONSTRAINT FK_ListasPrecios_Sucursal REFERENCES Sucursales(Id),
    CanalVentaId        INT           NULL CONSTRAINT FK_ListasPrecios_Canal REFERENCES Tipos(Id),
    MonedaId            INT           NOT NULL CONSTRAINT FK_ListasPrecios_Moneda REFERENCES Monedas(Id),
    EstadoId            INT           NOT NULL CONSTRAINT FK_ListasPrecios_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_ListasPrecios_Codigo UNIQUE (EmpresaId, Codigo),
    CONSTRAINT CK_ListasPrecios_Vigencia CHECK (VigenciaHasta IS NULL OR VigenciaHasta >= VigenciaDesde)
);
GO

CREATE TABLE PreciosDetalle (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_PreciosDetalle PRIMARY KEY,
    ListaPrecioId          INT           NOT NULL CONSTRAINT FK_PreciosDetalle_Lista REFERENCES ListasPrecios(Id),
    ProductoId             BIGINT        NOT NULL CONSTRAINT FK_PreciosDetalle_Producto REFERENCES Productos(Id),
    ProductoPresentacionId BIGINT        NULL CONSTRAINT FK_PreciosDetalle_ProductoPresentacion REFERENCES ProductoPresentaciones(Id),
    PrecioVenta            DECIMAL(12,2) NOT NULL CONSTRAINT CK_PreciosDetalle_Precio CHECK (PrecioVenta >= 0),
    CONSTRAINT UQ_PreciosDetalle UNIQUE (ListaPrecioId, ProductoId, ProductoPresentacionId)
);
GO

CREATE TABLE HistoricoPreciosVenta (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_HistoricoPreciosVenta PRIMARY KEY,
    EmpresaId              INT           NOT NULL CONSTRAINT FK_HistoricoPreciosVenta_Empresa REFERENCES Empresas(Id),
    ProductoId             BIGINT        NOT NULL CONSTRAINT FK_HistoricoPreciosVenta_Producto REFERENCES Productos(Id),
    ProductoPresentacionId BIGINT        NULL CONSTRAINT FK_HistoricoPreciosVenta_Presentacion REFERENCES ProductoPresentaciones(Id),
    PrecioAnterior         DECIMAL(12,2) NULL,
    PrecioNuevo            DECIMAL(12,2) NOT NULL CONSTRAINT CK_HistoricoPreciosVenta_Precio CHECK (PrecioNuevo >= 0),
    FechaCambio            DATETIME2(3)  NOT NULL,
    UsuarioId              INT           NOT NULL CONSTRAINT FK_HistoricoPreciosVenta_Usuario REFERENCES Usuarios(Id)
);
GO

CREATE TABLE ProductoImpuestos (
    Id          BIGINT  IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductoImpuestos PRIMARY KEY,
    ProductoId  BIGINT  NOT NULL CONSTRAINT FK_ProductoImpuestos_Producto REFERENCES Productos(Id),
    ImpuestoId  INT     NOT NULL CONSTRAINT FK_ProductoImpuestos_Impuesto REFERENCES Impuestos(Id),
    EstadoId    INT     NOT NULL CONSTRAINT FK_ProductoImpuestos_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_ProductoImpuestos UNIQUE (ProductoId, ImpuestoId)
);
GO

--Clientes

CREATE TABLE Clientes (
    Id                 INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_Clientes PRIMARY KEY,
    EmpresaId          INT           NOT NULL CONSTRAINT FK_Clientes_Empresa REFERENCES Empresas(Id),
    CodigoCliente      VARCHAR(20)   NOT NULL,
    NombreCliente      VARCHAR(150)  NOT NULL,
    Nit                VARCHAR(20)   NULL,
    Telefono           VARCHAR(20)   NULL,
    Correo             VARCHAR(100)  NULL,
    LimiteCredito      DECIMAL(12,2) NOT NULL CONSTRAINT CK_Clientes_LimiteCredito CHECK (LimiteCredito >= 0),
    DiasCredito        SMALLINT      NOT NULL CONSTRAINT CK_Clientes_DiasCredito CHECK (DiasCredito >= 0),
    ListaPrecioId      INT           NULL CONSTRAINT FK_Clientes_ListaPrecio REFERENCES ListasPrecios(Id),
    EstadoCreditoId    INT           NULL CONSTRAINT FK_Clientes_EstadoCredito REFERENCES Estados(Id),
    EstadoId           INT           NOT NULL CONSTRAINT FK_Clientes_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_ClientesCodigo UNIQUE (EmpresaId, CodigoCliente)
);
GO

CREATE TABLE DireccionesClientes (
    Id              BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_DireccionesClientes PRIMARY KEY,
    ClienteId       INT          NOT NULL CONSTRAINT FK_DireccionesClientes_Cliente REFERENCES Clientes(Id),
    TipoDireccionId INT          NULL CONSTRAINT FK_DireccionesClientes_Tipo REFERENCES Tipos(Id),
    Direccion       VARCHAR(250) NOT NULL,
    MunicipioId     INT          NULL CONSTRAINT FK_DireccionesClientes_Municipio REFERENCES Municipios(Id),
    Referencia      VARCHAR(250) NULL,
    EsPrincipal     BIT          NOT NULL,
    EstadoId        INT          NOT NULL CONSTRAINT FK_DireccionesClientes_Estado REFERENCES Estados(Id)
);
GO

CREATE TABLE ContactosClientes (
    Id          BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContactosClientes PRIMARY KEY,
    ClienteId   INT          NOT NULL CONSTRAINT FK_ContactosClientes_Cliente REFERENCES Clientes(Id),
    Nombre      VARCHAR(150) NOT NULL,
    Telefono    VARCHAR(20)  NULL,
    Correo      VARCHAR(100) NULL,
    Cargo       VARCHAR(100) NULL,
    EsPrincipal BIT         NOT NULL,
    EstadoId    INT         NOT NULL CONSTRAINT FK_ContactosClientes_Estado REFERENCES Estados(Id)
);
GO

--Proveedores

CREATE TABLE Proveedores (
    Id                  INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_Proveedores PRIMARY KEY,
    EmpresaId           INT           NOT NULL CONSTRAINT FK_Proveedores_Empresa REFERENCES Empresas(Id),
    CodigoProveedor     VARCHAR(20)   NOT NULL,
    NombreProveedor     VARCHAR(150)  NOT NULL,
    Nit                 VARCHAR(20)   NULL,
    Telefono            VARCHAR(20)   NULL,
    Correo              VARCHAR(100)  NULL,
    Direccion           VARCHAR(250)  NULL,
    DiasCredito         SMALLINT      NOT NULL CONSTRAINT CK_Proveedores_DiasCredito CHECK (DiasCredito >= 0),
    LimiteCredito       DECIMAL(12,2) NULL CONSTRAINT CK_Proveedores_LimiteCredito CHECK (LimiteCredito IS NULL OR LimiteCredito >= 0),
    MonedaId            INT           NULL CONSTRAINT FK_Proveedores_Moneda REFERENCES Monedas(Id),
    EstadoId            INT           NOT NULL CONSTRAINT FK_Proveedores_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_ProveedoresCodigo UNIQUE (EmpresaId, CodigoProveedor)
);
GO

CREATE TABLE ContactosProveedores (
    Id           BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContactosProveedores PRIMARY KEY,
    ProveedorId  INT          NOT NULL CONSTRAINT FK_ContactosProveedores_Proveedor REFERENCES Proveedores(Id),
    Nombre       VARCHAR(150) NOT NULL,
    Telefono     VARCHAR(20)  NULL,
    Correo       VARCHAR(100) NULL,
    Cargo        VARCHAR(100) NULL,
    EsPrincipal  BIT          NOT NULL,
    EstadoId     INT          NOT NULL CONSTRAINT FK_ContactosProveedores_Estado REFERENCES Estados(Id)
);
GO

ALTER TABLE Sucursales
ADD CONSTRAINT FK_Sucursales_ClienteId FOREIGN KEY (ClienteId) REFERENCES Clientes(Id),
    CONSTRAINT FK_Sucursales_ProveedorId FOREIGN KEY (ProveedorId) REFERENCES Proveedores(Id);
GO

CREATE TABLE ProveedorProductos (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProveedorProductos PRIMARY KEY,
    ProveedorId        INT           NOT NULL CONSTRAINT FK_ProveedorProductos_Proveedor REFERENCES Proveedores(Id),
    ProductoId         BIGINT        NOT NULL CONSTRAINT FK_ProveedorProductos_Producto REFERENCES Productos(Id),
    CodigoProveedor    VARCHAR(50)   NULL,
    PrecioUltimaCompra DECIMAL(12,2) NULL,
    EsPreferido        BIT           NOT NULL,
    EstadoId           INT           NOT NULL CONSTRAINT FK_ProveedorProductos_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_ProveedorProductos UNIQUE (ProveedorId, ProductoId)
);
GO

--Compras

CREATE TABLE OrdenesCompra (
    Id                   BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenesCompra PRIMARY KEY,
    EmpresaId            INT           NOT NULL CONSTRAINT FK_OrdenesCompra_Empresa REFERENCES Empresas(Id),
    SucursalId           BIGINT        NOT NULL CONSTRAINT FK_OrdenesCompra_Sucursal REFERENCES Sucursales(Id),
    ProveedorId          INT           NOT NULL CONSTRAINT FK_OrdenesCompra_Proveedor REFERENCES Proveedores(Id),
    NumeroOrden          VARCHAR(30)   NOT NULL,
    FechaEmision         DATETIME2(3)  NOT NULL,
    FechaEntregaEsperada DATETIME2(3)  NULL,
    MonedaId             INT           NOT NULL CONSTRAINT FK_OrdenesCompra_Moneda REFERENCES Monedas(Id),
    TipoCambio           DECIMAL(19,8) NOT NULL CONSTRAINT CK_OrdenesCompra_TipoCambio CHECK (TipoCambio > 0),
    SubTotal             DECIMAL(12,2) NOT NULL,
    Impuestos            DECIMAL(12,2) NOT NULL,
    TotalEstimado        DECIMAL(12,2) NOT NULL,
    FlujoEstadoId        INT           NOT NULL,
    EstadoId             INT           NOT NULL,
    CONSTRAINT FK_OrdenesCompra_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    UsuarioCreacionId    INT           NOT NULL CONSTRAINT FK_OrdenesCompra_Usuario REFERENCES Usuarios(Id),
    CONSTRAINT UQ_OrdenesCompra_Numero UNIQUE (EmpresaId, NumeroOrden)
);
GO

CREATE TABLE OrdenesCompraDetalle (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenesCompraDetalle PRIMARY KEY,
    OrdenCompraId          BIGINT        NOT NULL CONSTRAINT FK_OrdenesCompraDetalle_Orden REFERENCES OrdenesCompra(Id),
    NumeroLinea            INT           NOT NULL CONSTRAINT CK_OrdenesCompraDetalle_Linea CHECK (NumeroLinea > 0),
    ProductoId             BIGINT        NOT NULL CONSTRAINT FK_OrdenesCompraDetalle_Producto REFERENCES Productos(Id),
    ProductoPresentacionId BIGINT        NULL CONSTRAINT FK_OrdenesCompraDetalle_Presentacion REFERENCES ProductoPresentaciones(Id),
    Cantidad               DECIMAL(19,6) NOT NULL CONSTRAINT CK_OrdenesCompraDetalle_Cantidad CHECK (Cantidad > 0),
    PrecioUnitario         DECIMAL(12,2) NOT NULL CONSTRAINT CK_OrdenesCompraDetalle_Precio CHECK (PrecioUnitario >= 0),
    Descuento              DECIMAL(12,2) NOT NULL,
    Impuesto               DECIMAL(12,2) NOT NULL,
    TotalLinea             AS ((Cantidad * PrecioUnitario) - Descuento + Impuesto) PERSISTED,
    CONSTRAINT UQ_OrdenesCompraDetalle_Linea UNIQUE (OrdenCompraId, NumeroLinea)
);
GO

CREATE TABLE RecepcionesCompra (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_RecepcionesCompra PRIMARY KEY,
    EmpresaId          INT           NOT NULL CONSTRAINT FK_RecepcionesCompra_Empresa REFERENCES Empresas(Id),
    SucursalId         BIGINT        NOT NULL CONSTRAINT FK_RecepcionesCompra_Sucursal REFERENCES Sucursales(Id),
    OrdenCompraId      BIGINT        NOT NULL CONSTRAINT FK_RecepcionesCompra_Orden REFERENCES OrdenesCompra(Id),
    NumeroRecepcion    VARCHAR(30)   NOT NULL,
    FechaRecepcion     DATETIME2(3)  NOT NULL,
    RecibidoPorUsuarioId INT         NOT NULL CONSTRAINT FK_RecepcionesCompra_Usuario REFERENCES Usuarios(Id),
    FlujoEstadoId      INT           NOT NULL,
    EstadoId           INT           NOT NULL,
    CONSTRAINT FK_RecepcionesCompra_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    Observaciones      VARCHAR(500)  NULL,
    CONSTRAINT UQ_RecepcionesCompra_Numero UNIQUE (EmpresaId, NumeroRecepcion)
);
GO

CREATE TABLE RecepcionCompraDetalles (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_RecepcionCompraDetalles PRIMARY KEY,
    RecepcionCompraId      BIGINT        NOT NULL CONSTRAINT FK_RecepcionCompraDetalles_Recepcion REFERENCES RecepcionesCompra(Id),
    OrdenCompraDetalleId   BIGINT        NOT NULL CONSTRAINT FK_RecepcionCompraDetalles_OrdenDetalle REFERENCES OrdenesCompraDetalle(Id),
    ProductoId             BIGINT        NOT NULL CONSTRAINT FK_RecepcionCompraDetalles_Producto REFERENCES Productos(Id),
    CantidadRecibida       DECIMAL(19,6) NOT NULL CONSTRAINT CK_RecepcionCompraDetalles_Cantidad CHECK (CantidadRecibida > 0),
    LoteId                 BIGINT        NULL CONSTRAINT FK_RecepcionCompraDetalles_Lote REFERENCES Lotes(Id),
    FechaVencimiento       DATE          NULL,
    CostoUnitario         DECIMAL(19,6) NOT NULL CONSTRAINT CK_RecepcionCompraDetalles_Costo CHECK (CostoUnitario >= 0)
);
GO

CREATE TABLE FacturasProveedores (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_FacturasProveedores PRIMARY KEY,
    EmpresaId          INT           NOT NULL CONSTRAINT FK_FacturasProveedores_Empresa REFERENCES Empresas(Id),
    ProveedorId        INT           NOT NULL CONSTRAINT FK_FacturasProveedores_Proveedor REFERENCES Proveedores(Id),
    OrdenCompraId      BIGINT        NULL CONSTRAINT FK_FacturasProveedores_Orden REFERENCES OrdenesCompra(Id),
    Serie              VARCHAR(30)   NULL,
    NumeroDocumento    VARCHAR(50)   NOT NULL,
    FechaDocumento     DATE          NOT NULL,
    FechaVencimiento   DATE          NULL,
    MonedaId           INT           NOT NULL CONSTRAINT FK_FacturasProveedores_Moneda REFERENCES Monedas(Id),
    TipoCambio         DECIMAL(19,8) NOT NULL CONSTRAINT CK_FacturasProveedores_TipoCambio CHECK (TipoCambio > 0),
    SubTotal           DECIMAL(12,2) NOT NULL,
    Impuestos          DECIMAL(12,2) NOT NULL,
    Total              DECIMAL(12,2) NOT NULL,
    EstadoId           INT           NOT NULL CONSTRAINT FK_FacturasProveedores_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_FacturasProveedores_Documento UNIQUE (EmpresaId, ProveedorId, Serie, NumeroDocumento)
);
GO

CREATE TABLE HistoricoPreciosCompra (
    Id            BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_HistoricoPreciosCompra PRIMARY KEY,
    EmpresaId     INT           NOT NULL CONSTRAINT FK_HistoricoPreciosCompra_Empresa REFERENCES Empresas(Id),
    ProveedorId   INT           NOT NULL CONSTRAINT FK_HistoricoPreciosCompra_Proveedor REFERENCES Proveedores(Id),
    ProductoId    BIGINT        NOT NULL CONSTRAINT FK_HistoricoPreciosCompra_Producto REFERENCES Productos(Id),
    PrecioCompra  DECIMAL(12,2) NOT NULL CONSTRAINT CK_HistoricoPreciosCompra_Precio CHECK (PrecioCompra >= 0),
    MonedaId      INT           NOT NULL CONSTRAINT FK_HistoricoPreciosCompra_Moneda REFERENCES Monedas(Id),
    Fecha         DATETIME2(3)  NOT NULL,
    RecepcionCompraId BIGINT    NULL CONSTRAINT FK_HistoricoPreciosCompra_Recepcion REFERENCES RecepcionesCompra(Id)
);
GO

--CuentasPorPagar

CREATE TABLE CuentasPorPagar (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_CuentasPorPagar PRIMARY KEY,
    EmpresaId          INT           NOT NULL CONSTRAINT FK_CuentasPorPagar_Empresa REFERENCES Empresas(Id),
    ProveedorId        INT           NOT NULL CONSTRAINT FK_CuentasPorPagar_Proveedor REFERENCES Proveedores(Id),
    FacturaProveedorId BIGINT        NOT NULL CONSTRAINT FK_CuentasPorPagar_Factura REFERENCES FacturasProveedores(Id),
    FechaEmision       DATE          NOT NULL,
    FechaVencimiento   DATE          NOT NULL,
    MontoOriginal      DECIMAL(12,2) NOT NULL CONSTRAINT CK_CuentasPorPagar_Monto CHECK (MontoOriginal >= 0),
    SaldoPendiente     DECIMAL(12,2) NOT NULL CONSTRAINT CK_CuentasPorPagar_Saldo CHECK (SaldoPendiente >= 0),
    FlujoEstadoId      INT           NOT NULL,
    EstadoId           INT           NOT NULL,
    CONSTRAINT FK_CuentasPorPagar_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    CONSTRAINT UQ_CuentasPorPagar_Factura UNIQUE (FacturaProveedorId)
);
GO

--Ventas

CREATE TABLE Cotizaciones (
    Id                BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cotizaciones PRIMARY KEY,
    EmpresaId         INT           NOT NULL CONSTRAINT FK_Cotizaciones_Empresa REFERENCES Empresas(Id),
    SucursalId        BIGINT        NOT NULL CONSTRAINT FK_Cotizaciones_Sucursal REFERENCES Sucursales(Id),
    ClienteId         INT           NULL CONSTRAINT FK_Cotizaciones_Cliente REFERENCES Clientes(Id),
    NumeroCotizacion  VARCHAR(30)   NOT NULL,
    Fecha             DATETIME2(3)  NOT NULL,
    VigenciaHasta     DATE          NULL,
    MonedaId          INT           NOT NULL CONSTRAINT FK_Cotizaciones_Moneda REFERENCES Monedas(Id),
    Total             DECIMAL(12,2) NOT NULL,
    FlujoEstadoId     INT           NOT NULL,
    EstadoId          INT           NOT NULL,
    CONSTRAINT FK_Cotizaciones_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    CreadoPorUsuarioId INT          NOT NULL CONSTRAINT FK_Cotizaciones_Usuario REFERENCES Usuarios(Id),
    CONSTRAINT UQ_Cotizaciones_Numero UNIQUE (EmpresaId, NumeroCotizacion)
);
GO

CREATE TABLE CotizacionDetalles (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_CotizacionDetalles PRIMARY KEY,
    CotizacionId           BIGINT        NOT NULL CONSTRAINT FK_CotizacionDetalles_Cotizacion REFERENCES Cotizaciones(Id),
    NumeroLinea            INT           NOT NULL CONSTRAINT CK_CotizacionDetalles_Linea CHECK (NumeroLinea > 0),
    ProductoId             BIGINT        NOT NULL CONSTRAINT FK_CotizacionDetalles_Producto REFERENCES Productos(Id),
    ProductoPresentacionId BIGINT        NULL CONSTRAINT FK_CotizacionDetalles_Presentacion REFERENCES ProductoPresentaciones(Id),
    Cantidad               DECIMAL(19,6) NOT NULL CONSTRAINT CK_CotizacionDetalles_Cantidad CHECK (Cantidad > 0),
    PrecioUnitario         DECIMAL(12,2) NOT NULL,
    Descuento              DECIMAL(12,2) NOT NULL,
    Impuesto               DECIMAL(12,2) NOT NULL,
    TotalLinea             AS ((Cantidad * PrecioUnitario) - Descuento + Impuesto) PERSISTED,
    CONSTRAINT UQ_CotizacionDetalles_Linea UNIQUE (CotizacionId, NumeroLinea)
);
GO

CREATE TABLE PedidosVenta (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_PedidosVenta PRIMARY KEY,
    EmpresaId          INT           NOT NULL CONSTRAINT FK_PedidosVenta_Empresa REFERENCES Empresas(Id),
    SucursalId         BIGINT        NOT NULL CONSTRAINT FK_PedidosVenta_Sucursal REFERENCES Sucursales(Id),
    ClienteId          INT           NULL CONSTRAINT FK_PedidosVenta_Cliente REFERENCES Clientes(Id),
    CotizacionId       BIGINT        NULL CONSTRAINT FK_PedidosVenta_Cotizacion REFERENCES Cotizaciones(Id),
    NumeroPedido       VARCHAR(30)   NOT NULL,
    FechaPedido        DATETIME2(3)  NOT NULL,
    FechaEntrega       DATETIME2(3)  NULL,
    Total              DECIMAL(12,2) NOT NULL,
    FlujoEstadoId      INT           NOT NULL,
    EstadoId           INT           NOT NULL,
    CONSTRAINT FK_PedidosVenta_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    CreadoPorUsuarioId INT           NOT NULL CONSTRAINT FK_PedidosVenta_Usuario REFERENCES Usuarios(Id),
    CONSTRAINT UQ_PedidosVenta_Numero UNIQUE (EmpresaId, NumeroPedido)
);
GO

CREATE TABLE PedidoVentaDetalles (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_PedidoVentaDetalles PRIMARY KEY,
    PedidoVentaId          BIGINT        NOT NULL CONSTRAINT FK_PedidoVentaDetalles_Pedido REFERENCES PedidosVenta(Id),
    NumeroLinea            INT           NOT NULL CONSTRAINT CK_PedidoVentaDetalles_Linea CHECK (NumeroLinea > 0),
    ProductoId             BIGINT        NOT NULL CONSTRAINT FK_PedidoVentaDetalles_Producto REFERENCES Productos(Id),
    ProductoPresentacionId BIGINT        NULL CONSTRAINT FK_PedidoVentaDetalles_Presentacion REFERENCES ProductoPresentaciones(Id),
    Cantidad               DECIMAL(19,6) NOT NULL CONSTRAINT CK_PedidoVentaDetalles_Cantidad CHECK (Cantidad > 0),
    PrecioUnitario         DECIMAL(12,2) NOT NULL,
    Descuento              DECIMAL(12,2) NOT NULL,
    Impuesto               DECIMAL(12,2) NOT NULL,
    TotalLinea             AS ((Cantidad * PrecioUnitario) - Descuento + Impuesto) PERSISTED,
    CONSTRAINT UQ_PedidoVentaDetalles_Linea UNIQUE (PedidoVentaId, NumeroLinea)
);
GO

--Caja

CREATE TABLE Cajas (
    Id              BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cajas PRIMARY KEY,
    EmpresaId       INT          NOT NULL CONSTRAINT FK_Cajas_Empresa REFERENCES Empresas(Id),
    SucursalId      BIGINT       NOT NULL CONSTRAINT FK_Cajas_Sucursal REFERENCES Sucursales(Id),
    Codigo          VARCHAR(20)  NOT NULL,
    Nombre          VARCHAR(100) NOT NULL,
    EstadoId        INT          NOT NULL CONSTRAINT FK_Cajas_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_Cajas_Sucursal_Codigo UNIQUE (SucursalId, Codigo)
);
GO

CREATE TABLE TurnosCaja (
    Id              BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_TurnosCaja PRIMARY KEY,
    CajaId          BIGINT        NOT NULL CONSTRAINT FK_TurnosCaja_Caja REFERENCES Cajas(Id),
    UsuarioId       INT           NOT NULL CONSTRAINT FK_TurnosCaja_Usuario REFERENCES Usuarios(Id),
    FechaApertura   DATETIME2(3)  NOT NULL,
    MontoInicial    DECIMAL(12,2) NOT NULL CONSTRAINT CK_TurnosCaja_MontoInicial CHECK (MontoInicial >= 0),
    FechaCierre     DATETIME2(3)  NULL,
    MontoEsperado   DECIMAL(12,2) NULL,
    MontoFisico     DECIMAL(12,2) NULL,
    Diferencia      AS (CASE WHEN MontoEsperado IS NULL OR MontoFisico IS NULL THEN NULL ELSE MontoFisico - MontoEsperado END) PERSISTED,
    FlujoEstadoId   INT           NOT NULL,
    EstadoId        INT           NOT NULL,
    CONSTRAINT FK_TurnosCaja_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    CONSTRAINT CK_TurnosCaja_Fechas CHECK (FechaCierre IS NULL OR FechaCierre >= FechaApertura)
);
GO

CREATE TABLE DenominacionesMoneda (
    Id          BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_DenominacionesMoneda PRIMARY KEY,
    MonedaId    INT           NOT NULL CONSTRAINT FK_DenominacionesMoneda_Moneda REFERENCES Monedas(Id),
    Valor       DECIMAL(12,2) NOT NULL CONSTRAINT CK_DenominacionesMoneda_Valor CHECK (Valor > 0),
    TipoId      INT           NULL CONSTRAINT FK_DenominacionesMoneda_Tipo REFERENCES Tipos(Id),
    EstadoId    INT           NOT NULL CONSTRAINT FK_DenominacionesMoneda_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_DenominacionesMoneda UNIQUE (MonedaId, Valor)
);
GO

CREATE TABLE MovimientosCaja (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_MovimientosCaja PRIMARY KEY,
    TurnoCajaId        BIGINT        NOT NULL CONSTRAINT FK_MovimientosCaja_Turno REFERENCES TurnosCaja(Id),
    TipoMovimientoId   INT           NOT NULL CONSTRAINT FK_MovimientosCaja_Tipo REFERENCES Tipos(Id),
    Fecha              DATETIME2(3)  NOT NULL,
    Monto              DECIMAL(12,2) NOT NULL CONSTRAINT CK_MovimientosCaja_Monto CHECK (Monto > 0),
    FormaPagoId        INT           NULL CONSTRAINT FK_MovimientosCaja_FormaPago REFERENCES FormasPago(Id),
    Referencia         VARCHAR(100)  NULL,
    Descripcion        VARCHAR(300)  NULL,
    UsuarioId          INT           NOT NULL CONSTRAINT FK_MovimientosCaja_Usuario REFERENCES Usuarios(Id)
);
GO

CREATE TABLE ArqueosCaja (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_ArqueosCaja PRIMARY KEY,
    TurnoCajaId        BIGINT        NOT NULL CONSTRAINT FK_ArqueosCaja_Turno REFERENCES TurnosCaja(Id),
    Fecha              DATETIME2(3)  NOT NULL,
    MontoEsperado      DECIMAL(12,2) NOT NULL,
    MontoContado       DECIMAL(12,2) NOT NULL,
    Diferencia         AS (MontoContado - MontoEsperado) PERSISTED,
    UsuarioId          INT           NOT NULL CONSTRAINT FK_ArqueosCaja_Usuario REFERENCES Usuarios(Id),
    Observaciones      VARCHAR(500)  NULL
);
GO

CREATE TABLE ArqueoCajaDetalles (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_ArqueoCajaDetalles PRIMARY KEY,
    ArqueoCajaId       BIGINT        NOT NULL CONSTRAINT FK_ArqueoCajaDetalles_Arqueo REFERENCES ArqueosCaja(Id),
    DenominacionId     BIGINT        NOT NULL CONSTRAINT FK_ArqueoCajaDetalles_Denominacion REFERENCES DenominacionesMoneda(Id),
    Cantidad           INT           NOT NULL CONSTRAINT CK_ArqueoCajaDetalles_Cantidad CHECK (Cantidad >= 0),
    Monto              DECIMAL(12,2) NOT NULL CONSTRAINT CK_ArqueoCajaDetalles_Monto CHECK (Monto >= 0),
    CONSTRAINT UQ_ArqueoCajaDetalles UNIQUE (ArqueoCajaId, DenominacionId)
);
GO

--Ventas

CREATE TABLE Ventas (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ventas PRIMARY KEY,
    EmpresaId          INT           NOT NULL CONSTRAINT FK_Ventas_Empresa REFERENCES Empresas(Id),
    SucursalId         BIGINT        NOT NULL CONSTRAINT FK_Ventas_Sucursal REFERENCES Sucursales(Id),
    TurnoCajaId        BIGINT        NULL CONSTRAINT FK_Ventas_TurnoCaja REFERENCES TurnosCaja(Id),
    ClienteId          INT           NULL CONSTRAINT FK_Ventas_Cliente REFERENCES Clientes(Id),
    PedidoVentaId      BIGINT        NULL CONSTRAINT FK_Ventas_Pedido REFERENCES PedidosVenta(Id),
    NumeroFactura      VARCHAR(50)   NOT NULL,
    FechaVenta         DATETIME2(3)  NOT NULL,
    MonedaId           INT           NOT NULL CONSTRAINT FK_Ventas_Moneda REFERENCES Monedas(Id),
    TipoCambio         DECIMAL(19,8) NOT NULL CONSTRAINT CK_Ventas_TipoCambio CHECK (TipoCambio > 0),
    EsCredito          BIT           NOT NULL,
    SubTotal           DECIMAL(12,2) NOT NULL,
    Descuento          DECIMAL(12,2) NOT NULL,
    Impuestos          DECIMAL(12,2) NOT NULL,
    TotalVenta         DECIMAL(12,2) NOT NULL,
    FlujoEstadoId      INT           NOT NULL,
    EstadoId           INT           NOT NULL,
    CONSTRAINT FK_Ventas_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    UsuarioCreacionId  INT           NOT NULL CONSTRAINT FK_Ventas_Usuario REFERENCES Usuarios(Id),
    CONSTRAINT UQ_VentasFactura UNIQUE (EmpresaId, NumeroFactura)
);
GO

CREATE TABLE VentasDetalle (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_VentasDetalle PRIMARY KEY,
    VentaId                BIGINT        NOT NULL CONSTRAINT FK_VentasDetalle_Venta REFERENCES Ventas(Id),
    NumeroLinea            INT           NOT NULL CONSTRAINT CK_VentasDetalle_Linea CHECK (NumeroLinea > 0),
    ProductoId             BIGINT        NOT NULL CONSTRAINT FK_VentasDetalle_Producto REFERENCES Productos(Id),
    ProductoPresentacionId BIGINT        NULL CONSTRAINT FK_VentasDetalle_Presentacion REFERENCES ProductoPresentaciones(Id),
    Cantidad               DECIMAL(19,6) NOT NULL CONSTRAINT CK_VentasDetalle_Cantidad CHECK (Cantidad > 0),
    PrecioUnitario         DECIMAL(12,2) NOT NULL,
    PorcentajeDescuento    DECIMAL(5,2)  NOT NULL,
    MontoDescuento         DECIMAL(12,2) NOT NULL,
    Impuesto               DECIMAL(12,2) NOT NULL,
    SubTotal               AS ((Cantidad * PrecioUnitario) - MontoDescuento + Impuesto) PERSISTED,
    CostoUnitario          DECIMAL(19,6) NULL,
    CONSTRAINT UQ_VentasDetalle_Linea UNIQUE (VentaId, NumeroLinea)
);
GO

CREATE TABLE VentaImpuestos (
    Id          BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_VentaImpuestos PRIMARY KEY,
    VentaId     BIGINT        NOT NULL CONSTRAINT FK_VentaImpuestos_Venta REFERENCES Ventas(Id),
    ImpuestoId  INT           NOT NULL CONSTRAINT FK_VentaImpuestos_Impuesto REFERENCES Impuestos(Id),
    Porcentaje  DECIMAL(9,6)  NOT NULL,
    BaseImponible DECIMAL(12,2) NOT NULL,
    Monto       DECIMAL(12,2) NOT NULL,
    CONSTRAINT UQ_VentaImpuestos UNIQUE (VentaId, ImpuestoId)
);
GO

--Facturacion Electronica Opcional

--CREATE TABLE FacturasElectronicas (
--    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_FacturasElectronicas PRIMARY KEY,
--    VentaId            BIGINT        NOT NULL CONSTRAINT FK_FacturasElectronicas_Venta REFERENCES Ventas(Id),
--    UuidFiscal         VARCHAR(100)  NOT NULL CONSTRAINT UQ_FacturasElectronicas_Uuid UNIQUE,
--    FolioFiscal        VARCHAR(50)   NULL,
--    FechaCertificacion DATETIME2(3)  NOT NULL,
--    XmlGenerado        VARCHAR(MAX)  NULL,
--    FirmaElectronica   VARCHAR(500)  NULL,
--    EstadoId           INT           NOT NULL CONSTRAINT FK_FacturasElectronicas_Estado REFERENCES Estados(Id)
--);
--GO

--Pagos

CREATE TABLE Pagos (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pagos PRIMARY KEY,
    EmpresaId          INT           NOT NULL CONSTRAINT FK_Pagos_Empresa REFERENCES Empresas(Id),
    SucursalId         BIGINT        NULL CONSTRAINT FK_Pagos_Sucursal REFERENCES Sucursales(Id),
    ClienteId          INT           NULL CONSTRAINT FK_Pagos_Cliente REFERENCES Clientes(Id),
    ProveedorId        INT           NULL CONSTRAINT FK_Pagos_Proveedor REFERENCES Proveedores(Id),
    Fecha              DATETIME2(3)  NOT NULL,
    MonedaId           INT           NOT NULL CONSTRAINT FK_Pagos_Moneda REFERENCES Monedas(Id),
    TipoCambio         DECIMAL(19,8) NOT NULL CONSTRAINT CK_Pagos_TipoCambio CHECK (TipoCambio > 0),
    MontoTotal         DECIMAL(12,2) NOT NULL CONSTRAINT CK_Pagos_Monto CHECK (MontoTotal > 0),
    TipoPagoOperacionId INT          NOT NULL CONSTRAINT FK_Pagos_TipoOperacion REFERENCES Tipos(Id),
    TurnoCajaId        BIGINT        NULL CONSTRAINT FK_Pagos_TurnoCaja REFERENCES TurnosCaja(Id),
    UsuarioId          INT           NOT NULL CONSTRAINT FK_Pagos_Usuario REFERENCES Usuarios(Id),
    EstadoId           INT           NOT NULL CONSTRAINT FK_Pagos_Estado REFERENCES Estados(Id),
    CONSTRAINT CK_Pagos_Tercero CHECK ((ClienteId IS NOT NULL AND ProveedorId IS NULL) OR (ClienteId IS NULL AND ProveedorId IS NOT NULL))
);
GO

CREATE TABLE PagoDetalles (
    Id              BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_PagoDetalles PRIMARY KEY,
    PagoId          BIGINT        NOT NULL CONSTRAINT FK_PagoDetalles_Pago REFERENCES Pagos(Id),
    FormaPagoId     INT           NOT NULL CONSTRAINT FK_PagoDetalles_FormaPago REFERENCES FormasPago(Id),
    Monto           DECIMAL(12,2) NOT NULL CONSTRAINT CK_PagoDetalles_Monto CHECK (Monto > 0),
    Referencia      VARCHAR(100)  NULL,
    BancoId         INT           NULL,
    CuentaBancariaId BIGINT       NULL
);
GO

--CuentasPorCobrar

CREATE TABLE CuentasPorCobrar (
    Id               BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_CuentasPorCobrar PRIMARY KEY,
    EmpresaId        INT           NOT NULL CONSTRAINT FK_CuentasPorCobrar_Empresa REFERENCES Empresas(Id),
    ClienteId        INT           NOT NULL CONSTRAINT FK_CuentasPorCobrar_Cliente REFERENCES Clientes(Id),
    VentaId          BIGINT        NOT NULL CONSTRAINT FK_CuentasPorCobrar_Venta REFERENCES Ventas(Id),
    FechaEmision     DATE          NOT NULL,
    FechaVencimiento DATE          NOT NULL,
    MontoOriginal    DECIMAL(12,2) NOT NULL CONSTRAINT CK_CuentasPorCobrar_Monto CHECK (MontoOriginal >= 0),
    SaldoPendiente   DECIMAL(12,2) NOT NULL CONSTRAINT CK_CuentasPorCobrar_Saldo CHECK (SaldoPendiente >= 0),
    FlujoEstadoId    INT           NOT NULL,
    EstadoId         INT           NOT NULL,
    CONSTRAINT FK_CuentasPorCobrar_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    CONSTRAINT UQ_CuentasPorCobrar_Venta UNIQUE (VentaId)
);
GO

CREATE TABLE AplicacionesCuentasPorCobrar (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_AplicacionesCuentasPorCobrar PRIMARY KEY,
    CuentaPorCobrarId  BIGINT        NOT NULL CONSTRAINT FK_AplicacionesCuentasPorCobrar_Cuenta REFERENCES CuentasPorCobrar(Id),
    PagoId             BIGINT        NOT NULL CONSTRAINT FK_AplicacionesCuentasPorCobrar_Pago REFERENCES Pagos(Id),
    FechaAplicacion    DATETIME2(3)  NOT NULL,
    MontoAplicado      DECIMAL(12,2) NOT NULL CONSTRAINT CK_AplicacionesCuentasPorCobrar_Monto CHECK (MontoAplicado > 0),
    UsuarioId          INT           NOT NULL CONSTRAINT FK_AplicacionesCuentasPorCobrar_Usuario REFERENCES Usuarios(Id),
    CONSTRAINT UQ_AplicacionesCuentasPorCobrar UNIQUE (CuentaPorCobrarId, PagoId)
);
GO

CREATE TABLE AplicacionesCuentasPorPagar (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_AplicacionesCuentasPorPagar PRIMARY KEY,
    CuentaPorPagarId   BIGINT        NOT NULL CONSTRAINT FK_AplicacionesCuentasPorPagar_Cuenta REFERENCES CuentasPorPagar(Id),
    PagoId             BIGINT        NOT NULL CONSTRAINT FK_AplicacionesCuentasPorPagar_Pago REFERENCES Pagos(Id),
    FechaAplicacion    DATETIME2(3)  NOT NULL,
    MontoAplicado      DECIMAL(12,2) NOT NULL CONSTRAINT CK_AplicacionesCuentasPorPagar_Monto CHECK (MontoAplicado > 0),
    UsuarioId          INT           NOT NULL CONSTRAINT FK_AplicacionesCuentasPorPagar_Usuario REFERENCES Usuarios(Id),
    CONSTRAINT UQ_AplicacionesCuentasPorPagar UNIQUE (CuentaPorPagarId, PagoId)
);
GO

--Bancos

CREATE TABLE Bancos (
    Id          INT          IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bancos PRIMARY KEY,
    EmpresaId   INT          NOT NULL CONSTRAINT FK_Bancos_Empresa REFERENCES Empresas(Id),
    Codigo      VARCHAR(20)  NOT NULL,
    Nombre      VARCHAR(150) NOT NULL,
    EstadoId    INT          NOT NULL CONSTRAINT FK_Bancos_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_Bancos_Empresa_Codigo UNIQUE (EmpresaId, Codigo)
);
GO

CREATE TABLE CuentasBancarias (
    Id              BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_CuentasBancarias PRIMARY KEY,
    BancoId         INT          NOT NULL CONSTRAINT FK_CuentasBancarias_Banco REFERENCES Bancos(Id),
    MonedaId        INT          NOT NULL CONSTRAINT FK_CuentasBancarias_Moneda REFERENCES Monedas(Id),
    NumeroCuenta    VARCHAR(50)  NOT NULL,
    NombreCuenta    VARCHAR(150) NOT NULL,
    TipoCuentaId    INT          NULL CONSTRAINT FK_CuentasBancarias_Tipo REFERENCES Tipos(Id),
    EstadoId        INT          NOT NULL CONSTRAINT FK_CuentasBancarias_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_CuentasBancarias_Numero UNIQUE (BancoId, NumeroCuenta)
);
GO

CREATE TABLE MovimientosBancarios (
    Id                BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_MovimientosBancarios PRIMARY KEY,
    CuentaBancariaId  BIGINT        NOT NULL CONSTRAINT FK_MovimientosBancarios_Cuenta REFERENCES CuentasBancarias(Id),
    Fecha             DATETIME2(3)  NOT NULL,
    TipoMovimientoId  INT           NOT NULL CONSTRAINT FK_MovimientosBancarios_Tipo REFERENCES Tipos(Id),
    Monto             DECIMAL(12,2) NOT NULL CONSTRAINT CK_MovimientosBancarios_Monto CHECK (Monto > 0),
    Referencia        VARCHAR(100)  NULL,
    PagoId            BIGINT        NULL CONSTRAINT FK_MovimientosBancarios_Pago REFERENCES Pagos(Id),
    Descripcion       VARCHAR(300)  NULL,
    UsuarioId         INT           NOT NULL CONSTRAINT FK_MovimientosBancarios_Usuario REFERENCES Usuarios(Id)
);
GO

ALTER TABLE PagoDetalles
ADD CONSTRAINT FK_PagoDetalles_Banco FOREIGN KEY (BancoId) REFERENCES Bancos(Id),
    CONSTRAINT FK_PagoDetalles_CuentaBancaria FOREIGN KEY (CuentaBancariaId) REFERENCES CuentasBancarias(Id);
GO

--Promociones

CREATE TABLE Promociones (
    Id                     BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_Promociones PRIMARY KEY,
    EmpresaId              INT           NOT NULL CONSTRAINT FK_Promociones_Empresa REFERENCES Empresas(Id),
    Codigo                 VARCHAR(20)   NOT NULL,
    Nombre                 VARCHAR(150)  NOT NULL,
    TipoPromocionId        INT           NOT NULL CONSTRAINT FK_Promociones_Tipo REFERENCES Tipos(Id),
    DescuentoPorcentaje    DECIMAL(5,2)  NULL,
    DescuentoMonto         DECIMAL(12,2) NULL,
    PrecioEspecial         DECIMAL(12,2) NULL,
    CantidadMinima         DECIMAL(19,6) NULL,
    MontoMinimo            DECIMAL(12,2) NULL,
    LimiteUsos             INT           NULL,
    LimitePorCliente       INT           NULL,
    FechaInicio            DATETIME2(3)  NOT NULL,
    FechaFin               DATETIME2(3)  NOT NULL,
    AplicaTodasSucursales  BIT           NOT NULL,
    EstadoId               INT           NOT NULL CONSTRAINT FK_Promociones_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_Promociones_Empresa_Codigo UNIQUE (EmpresaId, Codigo),
    CONSTRAINT CK_Promociones_Vigencia CHECK (FechaFin >= FechaInicio)
);
GO

CREATE TABLE PromocionProductos (
    Id            BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PromocionProductos PRIMARY KEY,
    PromocionId   BIGINT NOT NULL CONSTRAINT FK_PromocionProductos_Promocion REFERENCES Promociones(Id),
    ProductoId    BIGINT NOT NULL CONSTRAINT FK_PromocionProductos_Producto REFERENCES Productos(Id),
    EsRequerido   BIT    NOT NULL,
    CONSTRAINT UQ_PromocionProductos UNIQUE (PromocionId, ProductoId)
);
GO

CREATE TABLE PromocionCategorias (
    Id            BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PromocionCategorias PRIMARY KEY,
    PromocionId   BIGINT NOT NULL CONSTRAINT FK_PromocionCategorias_Promocion REFERENCES Promociones(Id),
    CategoriaId   INT    NOT NULL CONSTRAINT FK_PromocionCategorias_Categoria REFERENCES Categorias(Id),
    CONSTRAINT UQ_PromocionCategorias UNIQUE (PromocionId, CategoriaId)
);
GO

CREATE TABLE PromocionSucursales (
    Id            BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PromocionSucursales PRIMARY KEY,
    PromocionId   BIGINT NOT NULL CONSTRAINT FK_PromocionSucursales_Promocion REFERENCES Promociones(Id),
    SucursalId    BIGINT NOT NULL CONSTRAINT FK_PromocionSucursales_Sucursal REFERENCES Sucursales(Id),
    CONSTRAINT UQ_PromocionSucursales UNIQUE (PromocionId, SucursalId)
);
GO

CREATE TABLE PromocionHorarios (
    Id           BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PromocionHorarios PRIMARY KEY,
    PromocionId  BIGINT NOT NULL CONSTRAINT FK_PromocionHorarios_Promocion REFERENCES Promociones(Id),
    DiaSemana    TINYINT NOT NULL CONSTRAINT CK_PromocionHorarios_Dia CHECK (DiaSemana BETWEEN 1 AND 7),
    HoraInicio   TIME    NOT NULL,
    HoraFin      TIME    NOT NULL,
    CONSTRAINT UQ_PromocionHorarios UNIQUE (PromocionId, DiaSemana, HoraInicio),
    CONSTRAINT CK_PromocionHorarios_Horas CHECK (HoraFin > HoraInicio)
);
GO

--Devoluciones y Ajustes Comerciales

CREATE TABLE DevolucionesVentas (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_DevolucionesVentas PRIMARY KEY,
    EmpresaId          INT           NOT NULL CONSTRAINT FK_DevolucionesVentas_Empresa REFERENCES Empresas(Id),
    VentaId            BIGINT        NOT NULL CONSTRAINT FK_DevolucionesVentas_Venta REFERENCES Ventas(Id),
    NumeroDevolucion   VARCHAR(30)   NOT NULL,
    Fecha              DATETIME2(3)  NOT NULL,
    Total              DECIMAL(12,2) NOT NULL,
    MotivoId           BIGINT        NULL CONSTRAINT FK_DevolucionesVentas_Motivo REFERENCES Catalogos(Id),
    FlujoEstadoId      INT           NOT NULL,
    EstadoId           INT           NOT NULL,
    CONSTRAINT FK_DevolucionesVentas_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    UsuarioId          INT           NOT NULL CONSTRAINT FK_DevolucionesVentas_Usuario REFERENCES Usuarios(Id),
    CONSTRAINT UQ_DevolucionesVentas_Numero UNIQUE (EmpresaId, NumeroDevolucion)
);
GO

CREATE TABLE DevolucionVentaDetalles (
    Id               BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_DevolucionVentaDetalles PRIMARY KEY,
    DevolucionVentaId BIGINT        NOT NULL CONSTRAINT FK_DevolucionVentaDetalles_Devolucion REFERENCES DevolucionesVentas(Id),
    VentaDetalleId    BIGINT        NOT NULL CONSTRAINT FK_DevolucionVentaDetalles_VentaDetalle REFERENCES VentasDetalle(Id),
    Cantidad          DECIMAL(19,6) NOT NULL CONSTRAINT CK_DevolucionVentaDetalles_Cantidad CHECK (Cantidad > 0),
    Monto             DECIMAL(12,2) NOT NULL CONSTRAINT CK_DevolucionVentaDetalles_Monto CHECK (Monto >= 0)
);
GO

CREATE TABLE NotasCredito (
    Id             BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotasCredito PRIMARY KEY,
    EmpresaId      INT           NOT NULL CONSTRAINT FK_NotasCredito_Empresa REFERENCES Empresas(Id),
    ClienteId      INT           NOT NULL CONSTRAINT FK_NotasCredito_Cliente REFERENCES Clientes(Id),
    VentaId        BIGINT        NULL CONSTRAINT FK_NotasCredito_Venta REFERENCES Ventas(Id),
    NumeroNota     VARCHAR(30)   NOT NULL,
    Fecha          DATETIME2(3)  NOT NULL,
    Monto          DECIMAL(12,2) NOT NULL CONSTRAINT CK_NotasCredito_Monto CHECK (Monto > 0),
    Motivo         VARCHAR(300)  NOT NULL,
    EstadoId       INT           NOT NULL CONSTRAINT FK_NotasCredito_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_NotasCredito_Numero UNIQUE (EmpresaId, NumeroNota)
);
GO

CREATE TABLE NotasDebito (
    Id             BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotasDebito PRIMARY KEY,
    EmpresaId      INT           NOT NULL CONSTRAINT FK_NotasDebito_Empresa REFERENCES Empresas(Id),
    ClienteId      INT           NOT NULL CONSTRAINT FK_NotasDebito_Cliente REFERENCES Clientes(Id),
    VentaId        BIGINT        NULL CONSTRAINT FK_NotasDebito_Venta REFERENCES Ventas(Id),
    NumeroNota     VARCHAR(30)   NOT NULL,
    Fecha          DATETIME2(3)  NOT NULL,
    Monto          DECIMAL(12,2) NOT NULL CONSTRAINT CK_NotasDebito_Monto CHECK (Monto > 0),
    Motivo         VARCHAR(300)  NOT NULL,
    EstadoId       INT           NOT NULL CONSTRAINT FK_NotasDebito_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_NotasDebito_Numero UNIQUE (EmpresaId, NumeroNota)
);
GO

--Produccion

CREATE TABLE Recetas (
    Id                   BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_Recetas PRIMARY KEY,
    EmpresaId            INT           NOT NULL CONSTRAINT FK_Recetas_Empresa REFERENCES Empresas(Id),
    ProductoTerminadoId  BIGINT        NOT NULL CONSTRAINT FK_Recetas_ProductoTerminado REFERENCES Productos(Id),
    ProductoPresentacionRendimientoId BIGINT NULL CONSTRAINT FK_Recetas_ProductoPresentacionRendimiento REFERENCES ProductoPresentaciones(Id),
    Codigo               VARCHAR(30)   NOT NULL,
    Nombre               VARCHAR(150)  NOT NULL,
    Rendimiento          DECIMAL(19,6) NOT NULL CONSTRAINT CK_Recetas_Rendimiento CHECK (Rendimiento > 0),
    UnidadRendimientoId  INT           NOT NULL CONSTRAINT FK_Recetas_UnidadRendimiento REFERENCES UnidadesMedida(Id),
    FactorConversion     DECIMAL(19,6) NOT NULL CONSTRAINT CK_Recetas_FactorConversion CHECK (FactorConversion > 0),
    RendimientoBase      DECIMAL(19,6) NOT NULL CONSTRAINT CK_Recetas_RendimientoBase CHECK (RendimientoBase > 0),
    Version              INT           NOT NULL CONSTRAINT CK_Recetas_Version CHECK (Version > 0),
    VigenteDesde         DATE          NOT NULL,
    VigenteHasta         DATE          NULL,
    EstadoId             INT           NOT NULL CONSTRAINT FK_Recetas_Estado REFERENCES Estados(Id),
    VersionFila          ROWVERSION    NOT NULL,
    CONSTRAINT UQ_Recetas_Empresa_Codigo_Version UNIQUE (EmpresaId, Codigo, Version),
    CONSTRAINT CK_Recetas_Vigencia CHECK (VigenteHasta IS NULL OR VigenteHasta >= VigenteDesde)
);
GO

CREATE UNIQUE INDEX UQ_Recetas_Empresa_Producto_Vigente
    ON Recetas(EmpresaId, ProductoTerminadoId)
    WHERE VigenteHasta IS NULL;
GO

CREATE TABLE RecetaDetalles (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_RecetaDetalles PRIMARY KEY,
    RecetaId           BIGINT        NOT NULL CONSTRAINT FK_RecetaDetalles_Receta REFERENCES Recetas(Id),
    ProductoInsumoId   BIGINT        NOT NULL CONSTRAINT FK_RecetaDetalles_Producto REFERENCES Productos(Id),
    ProductoPresentacionId BIGINT     NULL CONSTRAINT FK_RecetaDetalles_ProductoPresentacion REFERENCES ProductoPresentaciones(Id),
    UnidadMedidaId     INT           NOT NULL CONSTRAINT FK_RecetaDetalles_Unidad REFERENCES UnidadesMedida(Id),
    CantidadRequerida  DECIMAL(19,6) NOT NULL CONSTRAINT CK_RecetaDetalles_Cantidad CHECK (CantidadRequerida > 0),
    FactorConversion   DECIMAL(19,6) NOT NULL CONSTRAINT CK_RecetaDetalles_FactorConversion CHECK (FactorConversion > 0),
    CantidadBase       DECIMAL(19,6) NOT NULL CONSTRAINT CK_RecetaDetalles_CantidadBase CHECK (CantidadBase > 0),
    Observacion        VARCHAR(300)  NULL,
    PorcentajeDesperdicioTecnico DECIMAL(5,2) NOT NULL CONSTRAINT CK_RecetaDetalles_Desperdicio CHECK (PorcentajeDesperdicioTecnico >= 0 AND PorcentajeDesperdicioTecnico <= 100),
    CONSTRAINT UQ_RecetaDetalles UNIQUE (RecetaId, ProductoInsumoId)
);
GO

CREATE TABLE OrdenesProduccion (
    Id                    BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenesProduccion PRIMARY KEY,
    EmpresaId             INT           NOT NULL CONSTRAINT FK_OrdenesProduccion_Empresa REFERENCES Empresas(Id),
    SucursalId            BIGINT        NOT NULL CONSTRAINT FK_OrdenesProduccion_Sucursal REFERENCES Sucursales(Id),
    BodegaProduccionId    BIGINT        NOT NULL CONSTRAINT FK_OrdenesProduccion_Bodega REFERENCES Bodegas(Id),
    ProductoAProducirId   BIGINT        NOT NULL CONSTRAINT FK_OrdenesProduccion_Producto REFERENCES Productos(Id),
    RecetaId              BIGINT        NOT NULL CONSTRAINT FK_OrdenesProduccion_Receta REFERENCES Recetas(Id),
    ProductoPresentacionId BIGINT       NULL CONSTRAINT FK_OrdenesProduccion_ProductoPresentacion REFERENCES ProductoPresentaciones(Id),
    NumeroOrden           VARCHAR(30)   NOT NULL,
    CantidadPlanificada   DECIMAL(19,6) NOT NULL CONSTRAINT CK_OrdenesProduccion_CantidadPlanificada CHECK (CantidadPlanificada > 0),
    FactorConversion      DECIMAL(19,6) NOT NULL CONSTRAINT CK_OrdenesProduccion_FactorConversion CHECK (FactorConversion > 0),
    CantidadPlanificadaBase DECIMAL(19,6) NOT NULL CONSTRAINT CK_OrdenesProduccion_CantidadPlanificadaBase CHECK (CantidadPlanificadaBase > 0),
    CantidadProducida     DECIMAL(19,6) NULL,
    FechaInicio           DATETIME2(3)  NOT NULL,
    FechaFin              DATETIME2(3)  NULL,
    FlujoEstadoId         INT           NOT NULL,
    EstadoId              INT           NOT NULL,
    CONSTRAINT FK_OrdenesProduccion_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    ResponsableUsuarioId  INT           NOT NULL CONSTRAINT FK_OrdenesProduccion_Responsable REFERENCES Usuarios(Id),
    VersionFila           ROWVERSION    NOT NULL,
    CONSTRAINT UQ_OrdenesProduccion_Numero UNIQUE (EmpresaId, NumeroOrden)
);
GO

CREATE TABLE OrdenProduccionDetalles (
    Id                    BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenProduccionDetalles PRIMARY KEY,
    OrdenProduccionId     BIGINT        NOT NULL CONSTRAINT FK_OrdenProduccionDetalles_Orden REFERENCES OrdenesProduccion(Id),
    ProductoInsumoId      BIGINT        NOT NULL CONSTRAINT FK_OrdenProduccionDetalles_Producto REFERENCES Productos(Id),
    ProductoPresentacionId BIGINT       NULL CONSTRAINT FK_OrdenProduccionDetalles_ProductoPresentacion REFERENCES ProductoPresentaciones(Id),
    UnidadMedidaId        INT           NOT NULL CONSTRAINT FK_OrdenProduccionDetalles_Unidad REFERENCES UnidadesMedida(Id),
    CantidadPlanificada   DECIMAL(19,6) NOT NULL CONSTRAINT CK_OrdenProduccionDetalles_Cantidad CHECK (CantidadPlanificada > 0),
    FactorConversion      DECIMAL(19,6) NOT NULL CONSTRAINT CK_OrdenProduccionDetalles_FactorConversion CHECK (FactorConversion > 0),
    CantidadPlanificadaBase DECIMAL(19,6) NOT NULL CONSTRAINT CK_OrdenProduccionDetalles_CantidadBase CHECK (CantidadPlanificadaBase > 0),
    CostoPlanificado      DECIMAL(19,6) NULL CONSTRAINT CK_OrdenProduccionDetalles_Costo CHECK (CostoPlanificado IS NULL OR CostoPlanificado >= 0),
    CONSTRAINT UQ_OrdenProduccionDetalles UNIQUE (OrdenProduccionId, ProductoInsumoId)
);
GO

CREATE TABLE ConsumosProduccion (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConsumosProduccion PRIMARY KEY,
    OrdenProduccionId  BIGINT        NOT NULL CONSTRAINT FK_ConsumosProduccion_Orden REFERENCES OrdenesProduccion(Id),
    ProductoId         BIGINT        NOT NULL CONSTRAINT FK_ConsumosProduccion_Producto REFERENCES Productos(Id),
    LoteId             BIGINT        NULL CONSTRAINT FK_ConsumosProduccion_Lote REFERENCES Lotes(Id),
    UnidadMedidaId     INT           NOT NULL CONSTRAINT FK_ConsumosProduccion_Unidad REFERENCES UnidadesMedida(Id),
    CantidadConsumida  DECIMAL(19,6) NOT NULL CONSTRAINT CK_ConsumosProduccion_Cantidad CHECK (CantidadConsumida > 0),
    CostoUnitario      DECIMAL(19,6) NOT NULL CONSTRAINT CK_ConsumosProduccion_Costo CHECK (CostoUnitario >= 0),
    FechaConsumo       DATETIME2(3)  NOT NULL
);
GO

CREATE TABLE ProduccionTerminada (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProduccionTerminada PRIMARY KEY,
    OrdenProduccionId  BIGINT        NOT NULL CONSTRAINT FK_ProduccionTerminada_Orden REFERENCES OrdenesProduccion(Id),
    ProductoId         BIGINT        NOT NULL CONSTRAINT FK_ProduccionTerminada_Producto REFERENCES Productos(Id),
    LoteId             BIGINT        NULL CONSTRAINT FK_ProduccionTerminada_Lote REFERENCES Lotes(Id),
    CantidadProducida  DECIMAL(19,6) NOT NULL CONSTRAINT CK_ProduccionTerminada_Cantidad CHECK (CantidadProducida > 0),
    CostoMateriaPrima  DECIMAL(19,6) NOT NULL,
    CostoManoObra      DECIMAL(19,6) NOT NULL,
    CostoIndirecto     DECIMAL(19,6) NOT NULL,
    CostoTotal         AS (CostoMateriaPrima + CostoManoObra + CostoIndirecto) PERSISTED,
    FechaFinalizacion  DATETIME2(3)  NOT NULL
);
GO

CREATE UNIQUE INDEX UQ_ProduccionTerminada_Orden
    ON ProduccionTerminada(OrdenProduccionId);
GO

CREATE TABLE CostosProduccion (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_CostosProduccion PRIMARY KEY,
    OrdenProduccionId  BIGINT        NOT NULL CONSTRAINT FK_CostosProduccion_Orden REFERENCES OrdenesProduccion(Id),
    TipoCostoId        INT           NOT NULL CONSTRAINT FK_CostosProduccion_Tipo REFERENCES Tipos(Id),
    Descripcion        VARCHAR(200)  NOT NULL,
    Monto              DECIMAL(19,6) NOT NULL CONSTRAINT CK_CostosProduccion_Monto CHECK (Monto >= 0),
    FechaRegistro      DATETIME2(3)  NOT NULL,
    UsuarioId          INT           NOT NULL CONSTRAINT FK_CostosProduccion_Usuario REFERENCES Usuarios(Id)
);
GO

--Fidelizacion

CREATE TABLE ProgramasFidelidad (
    Id                BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProgramasFidelidad PRIMARY KEY,
    EmpresaId         INT           NOT NULL CONSTRAINT FK_ProgramasFidelidad_Empresa REFERENCES Empresas(Id),
    Codigo            VARCHAR(20)   NOT NULL,
    Nombre            VARCHAR(100)  NOT NULL,
    MontoPorPunto     DECIMAL(12,2) NOT NULL CONSTRAINT CK_ProgramasFidelidad_MontoPorPunto CHECK (MontoPorPunto > 0),
    ValorPunto        DECIMAL(12,4) NOT NULL CONSTRAINT CK_ProgramasFidelidad_ValorPunto CHECK (ValorPunto >= 0),
    FechaInicio       DATE          NOT NULL,
    FechaFin          DATE          NULL,
    EstadoId          INT           NOT NULL CONSTRAINT FK_ProgramasFidelidad_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_ProgramasFidelidad_Empresa_Codigo UNIQUE (EmpresaId, Codigo)
);
GO

CREATE TABLE NivelesFidelidad (
    Id                 BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_NivelesFidelidad PRIMARY KEY,
    ProgramaId         BIGINT       NOT NULL CONSTRAINT FK_NivelesFidelidad_Programa REFERENCES ProgramasFidelidad(Id),
    Nombre             VARCHAR(100) NOT NULL,
    PuntosMinimos      INT          NOT NULL CONSTRAINT CK_NivelesFidelidad_Puntos CHECK (PuntosMinimos >= 0),
    Multiplicador      DECIMAL(9,4) NOT NULL CONSTRAINT CK_NivelesFidelidad_Multiplicador CHECK (Multiplicador > 0),
    EstadoId           INT          NOT NULL CONSTRAINT FK_NivelesFidelidad_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_NivelesFidelidad UNIQUE (ProgramaId, Nombre)
);
GO

CREATE TABLE ClientesFidelidad (
    Id                 BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ClientesFidelidad PRIMARY KEY,
    ClienteId          INT    NOT NULL CONSTRAINT FK_ClientesFidelidad_Cliente REFERENCES Clientes(Id),
    ProgramaId         BIGINT NOT NULL CONSTRAINT FK_ClientesFidelidad_Programa REFERENCES ProgramasFidelidad(Id),
    NivelId            BIGINT NULL CONSTRAINT FK_ClientesFidelidad_Nivel REFERENCES NivelesFidelidad(Id),
    SaldoPuntos        INT    NOT NULL,
    EstadoId           INT    NOT NULL CONSTRAINT FK_ClientesFidelidad_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_ClientesFidelidad UNIQUE (ClienteId, ProgramaId)
);
GO

CREATE TABLE MovimientosPuntos (
    Id                 BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_MovimientosPuntos PRIMARY KEY,
    ClienteFidelidadId BIGINT       NOT NULL CONSTRAINT FK_MovimientosPuntos_ClienteFidelidad REFERENCES ClientesFidelidad(Id),
    VentaId            BIGINT       NULL CONSTRAINT FK_MovimientosPuntos_Venta REFERENCES Ventas(Id),
    TipoMovimientoId   INT          NOT NULL CONSTRAINT FK_MovimientosPuntos_Tipo REFERENCES Tipos(Id),
    Puntos             INT          NOT NULL,
    FechaMovimiento    DATETIME2(3) NOT NULL,
    Descripcion        VARCHAR(200) NULL
);
GO

--Autorizaciones

CREATE TABLE ReglasAutorizacion (
    Id                 BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReglasAutorizacion PRIMARY KEY,
    EmpresaId          INT           NOT NULL CONSTRAINT FK_ReglasAutorizacion_Empresa REFERENCES Empresas(Id),
    ModuloId           VARCHAR(20)   NOT NULL CONSTRAINT FK_ReglasAutorizacion_Modulo REFERENCES Modulos(Id),
    Codigo             VARCHAR(50)   NOT NULL,
    Nombre             VARCHAR(150)  NOT NULL,
    TipoReglaId        INT           NOT NULL CONSTRAINT FK_ReglasAutorizacion_Tipo REFERENCES Tipos(Id),
    ValorUmbral        DECIMAL(19,6) NULL,
    RoleAutorizadorId  VARCHAR(20)   NULL CONSTRAINT FK_ReglasAutorizacion_Role REFERENCES Roles(Id),
    EstadoId           INT           NOT NULL CONSTRAINT FK_ReglasAutorizacion_Estado REFERENCES Estados(Id),
    CONSTRAINT UQ_ReglasAutorizacion_Empresa_Codigo UNIQUE (EmpresaId, Codigo)
);
GO

CREATE TABLE SolicitudesAutorizacion (
    Id                  BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_SolicitudesAutorizacion PRIMARY KEY,
    ReglaAutorizacionId BIGINT        NOT NULL CONSTRAINT FK_SolicitudesAutorizacion_Regla REFERENCES ReglasAutorizacion(Id),
    Entidad             VARCHAR(100)  NOT NULL,
    EntidadId           BIGINT        NOT NULL,
    Motivo              VARCHAR(500)  NULL,
    SolicitadoPorUsuarioId INT        NOT NULL CONSTRAINT FK_SolicitudesAutorizacion_SolicitadoPor REFERENCES Usuarios(Id),
    FechaSolicitud      DATETIME2(3)  NOT NULL,
    AutorizadoPorUsuarioId INT        NULL CONSTRAINT FK_SolicitudesAutorizacion_AutorizadoPor REFERENCES Usuarios(Id),
    FechaResolucion     DATETIME2(3)  NULL,
    FlujoEstadoId       INT           NOT NULL,
    EstadoId            INT           NOT NULL,
    CONSTRAINT FK_SolicitudesAutorizacion_Estado FOREIGN KEY (FlujoEstadoId, EstadoId) REFERENCES EstadosFlujo(FlujoEstadoId, Id),
    ComentarioResolucion VARCHAR(500) NULL
);
GO

--Documentos

CREATE TABLE DocumentosAdjuntos (
    Id              BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_DocumentosAdjuntos PRIMARY KEY,
    EmpresaId       INT           NULL CONSTRAINT FK_DocumentosAdjuntos_Empresa REFERENCES Empresas(Id),
    ModuloId        VARCHAR(20)   NULL CONSTRAINT FK_DocumentosAdjuntos_Modulo REFERENCES Modulos(Id),
    Entidad         VARCHAR(100)  NOT NULL,
    EntidadId       BIGINT        NOT NULL,
    NombreArchivo   VARCHAR(255)  NOT NULL,
    RutaArchivo     VARCHAR(500)  NOT NULL,
    TipoMime        VARCHAR(100)  NULL,
    TamanioBytes    BIGINT        NULL CONSTRAINT CK_DocumentosAdjuntos_Tamanio CHECK (TamanioBytes IS NULL OR TamanioBytes >= 0),
    FechaCarga      DATETIME2(3)  NOT NULL,
    UsuarioId       INT           NOT NULL CONSTRAINT FK_DocumentosAdjuntos_Usuario REFERENCES Usuarios(Id),
    EstadoId        INT           NOT NULL CONSTRAINT FK_DocumentosAdjuntos_Estado REFERENCES Estados(Id)
);
GO

--Auditoria y Notificaciones

CREATE TABLE Notificaciones (
    Id                 BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notificaciones PRIMARY KEY,
    TipoNotificacionId VARCHAR(20)  NOT NULL CONSTRAINT FK_Notificaciones_TipoNotificacionId REFERENCES TipoNotificaciones(Id),
    Fecha              DATE         NOT NULL,
    Comentario         VARCHAR(500) NOT NULL,
    Enlace             VARCHAR(200) NOT NULL,
    SucursalId         BIGINT       NULL CONSTRAINT FK_Notificaciones_SucursalId REFERENCES Sucursales(Id),
    Generado           BIT          NOT NULL
);
GO

CREATE TABLE Bitacoras (
    Id                BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bitacoras PRIMARY KEY,
    EmpresaId         INT           NULL CONSTRAINT FK_Bitacoras_Empresa REFERENCES Empresas(Id),
    SucursalId        BIGINT        NULL CONSTRAINT FK_Bitacoras_Sucursal REFERENCES Sucursales(Id),
    ModuloId          VARCHAR(20)   NULL CONSTRAINT FK_Bitacoras_Modulo REFERENCES Modulos(Id),
    Entidad           VARCHAR(100)  NOT NULL,
    EntidadId         VARCHAR(100)  NULL,
    Accion            VARCHAR(50)   NOT NULL,
    Fecha             DATETIME2(3)  NOT NULL,
    UsuarioId         INT           NULL CONSTRAINT FK_Bitacoras_Usuario REFERENCES Usuarios(Id),
    DireccionIp       VARCHAR(45)   NULL,
    Equipo            VARCHAR(200)  NULL,
    ValorAnterior     VARCHAR(MAX)  NULL,
    ValorNuevo        VARCHAR(MAX)  NULL,
    Comentario        VARCHAR(500)  NULL
);
GO

-- Ampliaciones Database First de Fase 7, equivalentes al script de reparación.
/* Fase 7 — Ventas/POS. No ejecuta bootstrap ni crea datos comerciales.
   Ejecutar en ventana de mantenimiento, tras respaldo, sobre la BD seleccionada.
   No rellena snapshots históricos: lo desconocido permanece NULL.
   DDL y consumo de columnas se compilan separadamente dentro de una transacción. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.Ventas', N'U') IS NULL OR OBJECT_ID(N'dbo.VentasDetalle', N'U') IS NULL
       OR OBJECT_ID(N'dbo.VentaImpuestos', N'U') IS NULL OR OBJECT_ID(N'dbo.Impuestos', N'U') IS NULL
        THROW 51070, 'Falta el esquema base de ventas. No se realizaron cambios.', 1;
    DECLARE @res int;
    EXEC @res = sys.sp_getapplock @Resource = N'Heladeria.Schema.Fase7',
        @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000;
    IF @res < 0 THROW 51070, 'No se pudo reservar la actualización de esquema.', 1;

    IF COL_LENGTH(N'dbo.Ventas', N'BodegaId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD BodegaId BIGINT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'BodegaId' AND (TYPE_NAME(system_type_id) <> N'bigint' OR max_length <> 8 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.BodegaId incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'UbicacionId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD UbicacionId BIGINT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'UbicacionId' AND (TYPE_NAME(system_type_id) <> N'bigint' OR max_length <> 8 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.UbicacionId incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'ClaveOperacion') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD ClaveOperacion UNIQUEIDENTIFIER NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'ClaveOperacion' AND (TYPE_NAME(system_type_id) <> N'uniqueidentifier' OR max_length <> 16 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.ClaveOperacion incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'FechaModificacion') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD FechaModificacion DATETIME2(3) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'FechaModificacion' AND (TYPE_NAME(system_type_id) <> N'datetime2' OR scale <> 3 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.FechaModificacion incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'ModificadoPorUsuarioId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD ModificadoPorUsuarioId INT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'ModificadoPorUsuarioId' AND (TYPE_NAME(system_type_id) <> N'int' OR max_length <> 4 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.ModificadoPorUsuarioId incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'FechaCompletada') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD FechaCompletada DATETIME2(3) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'FechaCompletada' AND (TYPE_NAME(system_type_id) <> N'datetime2' OR scale <> 3 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.FechaCompletada incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'CompletadoPorUsuarioId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD CompletadoPorUsuarioId INT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'CompletadoPorUsuarioId' AND (TYPE_NAME(system_type_id) <> N'int' OR max_length <> 4 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.CompletadoPorUsuarioId incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'FechaAnulacion') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD FechaAnulacion DATETIME2(3) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'FechaAnulacion' AND (TYPE_NAME(system_type_id) <> N'datetime2' OR scale <> 3 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.FechaAnulacion incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'AnuladoPorUsuarioId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD AnuladoPorUsuarioId INT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'AnuladoPorUsuarioId' AND (TYPE_NAME(system_type_id) <> N'int' OR max_length <> 4 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.AnuladoPorUsuarioId incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'MotivoAnulacion') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD MotivoAnulacion VARCHAR(500) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'MotivoAnulacion' AND (TYPE_NAME(system_type_id) <> N'varchar' OR max_length <> 500 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.MotivoAnulacion incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'VersionFila') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD VersionFila ROWVERSION NOT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'VersionFila' AND (TYPE_NAME(system_type_id) <> N'timestamp' OR max_length <> 8 OR is_nullable <> 0))
        THROW 51070, 'Columna Ventas.VersionFila incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'FactorConversion') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD FactorConversion DECIMAL(19,6) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'FactorConversion' AND (TYPE_NAME(system_type_id) <> N'decimal' OR max_length <> 9 OR precision <> 19 OR scale <> 6 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.FactorConversion incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'CantidadBase') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD CantidadBase DECIMAL(19,6) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'CantidadBase' AND (TYPE_NAME(system_type_id) <> N'decimal' OR max_length <> 9 OR precision <> 19 OR scale <> 6 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.CantidadBase incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'UnidadMedidaId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD UnidadMedidaId INT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'UnidadMedidaId' AND (TYPE_NAME(system_type_id) <> N'int' OR max_length <> 4 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.UnidadMedidaId incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'UnidadBaseId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD UnidadBaseId INT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'UnidadBaseId' AND (TYPE_NAME(system_type_id) <> N'int' OR max_length <> 4 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.UnidadBaseId incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'ManejaExistencia') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD ManejaExistencia BIT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'ManejaExistencia' AND (TYPE_NAME(system_type_id) <> N'bit' OR max_length <> 1 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.ManejaExistencia incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'SubtotalNeto') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD SubtotalNeto DECIMAL(12,2) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'SubtotalNeto' AND (TYPE_NAME(system_type_id) <> N'decimal' OR max_length <> 9 OR precision <> 12 OR scale <> 2 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.SubtotalNeto incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'DescuentoPrecioCatalogo') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD DescuentoPrecioCatalogo DECIMAL(12,2) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'DescuentoPrecioCatalogo' AND (TYPE_NAME(system_type_id) <> N'decimal' OR max_length <> 9 OR precision <> 12 OR scale <> 2 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.DescuentoPrecioCatalogo incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'TotalLinea') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD TotalLinea DECIMAL(12,2) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'TotalLinea' AND (TYPE_NAME(system_type_id) <> N'decimal' OR max_length <> 9 OR precision <> 12 OR scale <> 2 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.TotalLinea incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'CostoTotal') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD CostoTotal DECIMAL(19,6) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'CostoTotal' AND (TYPE_NAME(system_type_id) <> N'decimal' OR max_length <> 9 OR precision <> 19 OR scale <> 6 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.CostoTotal incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'FuentePrecio') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD FuentePrecio VARCHAR(30) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'FuentePrecio' AND (TYPE_NAME(system_type_id) <> N'varchar' OR max_length <> 30 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.FuentePrecio incompatible; revisar manualmente.', 1;
    IF OBJECT_ID(N'dbo.FK_Ventas_BodegaId_Fase7', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas WITH CHECK ADD CONSTRAINT FK_Ventas_BodegaId_Fase7 FOREIGN KEY (BodegaId) REFERENCES dbo.Bodegas(Id);';
    IF OBJECT_ID(N'dbo.FK_Ventas_UbicacionId_Fase7', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas WITH CHECK ADD CONSTRAINT FK_Ventas_UbicacionId_Fase7 FOREIGN KEY (UbicacionId) REFERENCES dbo.Ubicaciones(Id);';
    IF OBJECT_ID(N'dbo.FK_Ventas_ModificadoPorUsuarioId_Fase7', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas WITH CHECK ADD CONSTRAINT FK_Ventas_ModificadoPorUsuarioId_Fase7 FOREIGN KEY (ModificadoPorUsuarioId) REFERENCES dbo.Usuarios(Id);';
    IF OBJECT_ID(N'dbo.FK_Ventas_CompletadoPorUsuarioId_Fase7', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas WITH CHECK ADD CONSTRAINT FK_Ventas_CompletadoPorUsuarioId_Fase7 FOREIGN KEY (CompletadoPorUsuarioId) REFERENCES dbo.Usuarios(Id);';
    IF OBJECT_ID(N'dbo.FK_Ventas_AnuladoPorUsuarioId_Fase7', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas WITH CHECK ADD CONSTRAINT FK_Ventas_AnuladoPorUsuarioId_Fase7 FOREIGN KEY (AnuladoPorUsuarioId) REFERENCES dbo.Usuarios(Id);';
    IF OBJECT_ID(N'dbo.FK_VentasDetalle_UnidadMedidaId_Fase7', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle WITH CHECK ADD CONSTRAINT FK_VentasDetalle_UnidadMedidaId_Fase7 FOREIGN KEY (UnidadMedidaId) REFERENCES dbo.UnidadesMedida(Id);';
    IF OBJECT_ID(N'dbo.FK_VentasDetalle_UnidadBaseId_Fase7', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle WITH CHECK ADD CONSTRAINT FK_VentasDetalle_UnidadBaseId_Fase7 FOREIGN KEY (UnidadBaseId) REFERENCES dbo.UnidadesMedida(Id);';

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'UX_Ventas_ClaveOperacion')
        EXEC sys.sp_executesql N'CREATE UNIQUE INDEX UX_Ventas_ClaveOperacion ON dbo.Ventas(EmpresaId, ClaveOperacion) WHERE ClaveOperacion IS NOT NULL;';
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'UX_Ventas_ClaveOperacion' AND (is_unique=0 OR is_disabled=1 OR has_filter=0))
        THROW 51070, 'Indice de idempotencia incompatible.', 1;

    IF OBJECT_ID(N'dbo.VentaDetalleImpuestos', N'U') IS NULL
        EXEC sys.sp_executesql N'CREATE TABLE dbo.VentaDetalleImpuestos(
            Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VentaDetalleImpuestos PRIMARY KEY,
            VentaDetalleId BIGINT NOT NULL CONSTRAINT FK_VentaDetalleImpuestos_Detalle REFERENCES dbo.VentasDetalle(Id),
            ImpuestoId INT NOT NULL CONSTRAINT FK_VentaDetalleImpuestos_Impuesto REFERENCES dbo.Impuestos(Id),
            Codigo VARCHAR(20) NOT NULL,
            Nombre VARCHAR(100) NOT NULL,
            Porcentaje DECIMAL(9,6) NOT NULL CONSTRAINT CK_VentaDetalleImpuestos_Porcentaje CHECK(Porcentaje BETWEEN 0 AND 100),
            IncluidoEnPrecio BIT NOT NULL,
            BaseImponible DECIMAL(12,2) NOT NULL CONSTRAINT CK_VentaDetalleImpuestos_Base CHECK(BaseImponible>=0),
            Monto DECIMAL(12,2) NOT NULL CONSTRAINT CK_VentaDetalleImpuestos_Monto CHECK(Monto>=0),
            CONSTRAINT UQ_VentaDetalleImpuestos UNIQUE(VentaDetalleId, ImpuestoId)
        );';
    IF OBJECT_ID(N'dbo.CK_VentasDetalle_Snapshot_Fase7', N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle WITH CHECK ADD CONSTRAINT CK_VentasDetalle_Snapshot_Fase7 CHECK ((FactorConversion IS NULL OR FactorConversion>0) AND (CantidadBase IS NULL OR CantidadBase>0) AND (CostoTotal IS NULL OR CostoTotal>=0) AND (SubtotalNeto IS NULL OR SubtotalNeto>=0) AND (TotalLinea IS NULL OR TotalLinea>=0) AND (DescuentoPrecioCatalogo IS NULL OR DescuentoPrecioCatalogo>=0));';
    IF OBJECT_ID(N'dbo.CK_Ventas_Almacen_Fase7', N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas WITH CHECK ADD CONSTRAINT CK_Ventas_Almacen_Fase7 CHECK ((BodegaId IS NULL AND UbicacionId IS NULL) OR (BodegaId IS NOT NULL AND UbicacionId IS NOT NULL));';

 
    -- Un objeto existente solo se reutiliza si su definición mantiene las garantías de esta fase.
    DECLARE @columnasIncompatibles nvarchar(2048);
    SELECT @columnasIncompatibles = STRING_AGG(CONVERT(nvarchar(max), CONCAT(esperado.nombre, N': tipo=', TYPE_NAME(c.system_type_id), N', longitud=',c.max_length,N', nullable=',c.is_nullable,N', identidad=',c.is_identity,N', precisión=',c.precision,N', escala=',c.scale)), N'; ') FROM (VALUES
          (N'Id', N'bigint', 8, 0, 0, 1),
          (N'VentaDetalleId', N'bigint', 8, 0, 0, 0),
          (N'ImpuestoId', N'int', 4, 0, 0, 0),
          (N'Codigo', N'varchar', 20, 0, 0, 0),
          (N'Nombre', N'varchar', 100, 0, 0, 0),
          (N'Porcentaje', N'decimal', 5, 9, 6, 0),
          (N'IncluidoEnPrecio', N'bit', 1, 0, 0, 0),
          (N'BaseImponible', N'decimal', 9, 12, 2, 0),
          (N'Monto', N'decimal', 9, 12, 2, 0)
        ) AS esperado(nombre, tipo, longitud, precisionEsperada, escala, identidad)
        LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.VentaDetalleImpuestos') AND c.name=esperado.nombre
        WHERE c.column_id IS NULL OR TYPE_NAME(c.system_type_id)<>esperado.tipo
          OR c.max_length<>esperado.longitud OR c.is_nullable<>0 OR c.is_identity<>esperado.identidad
          OR (esperado.tipo=N'decimal' AND (c.precision<>esperado.precisionEsperada OR c.scale<>esperado.escala))
    ;
    IF @columnasIncompatibles IS NOT NULL
    BEGIN
        SET @columnasIncompatibles = N'VentaDetalleImpuestos incompatible: ' + @columnasIncompatibles;
        THROW 51070, @columnasIncompatibles, 1;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VentaDetalleImpuestos') AND name=N'UQ_VentaDetalleImpuestos')
        EXEC sys.sp_executesql N'CREATE UNIQUE INDEX UQ_VentaDetalleImpuestos ON dbo.VentaDetalleImpuestos(VentaDetalleId, ImpuestoId);';
    IF OBJECT_ID(N'dbo.FK_VentaDetalleImpuestos_Detalle', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentaDetalleImpuestos WITH CHECK ADD CONSTRAINT FK_VentaDetalleImpuestos_Detalle FOREIGN KEY(VentaDetalleId) REFERENCES dbo.VentasDetalle(Id);';
    IF OBJECT_ID(N'dbo.FK_VentaDetalleImpuestos_Impuesto', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentaDetalleImpuestos WITH CHECK ADD CONSTRAINT FK_VentaDetalleImpuestos_Impuesto FOREIGN KEY(ImpuestoId) REFERENCES dbo.Impuestos(Id);';

    IF EXISTS (
        SELECT 1 FROM (VALUES
          (N'FK_Ventas_BodegaId_Fase7', N'Ventas', N'BodegaId', N'Bodegas'),
          (N'FK_Ventas_UbicacionId_Fase7', N'Ventas', N'UbicacionId', N'Ubicaciones'),
          (N'FK_Ventas_ModificadoPorUsuarioId_Fase7', N'Ventas', N'ModificadoPorUsuarioId', N'Usuarios'),
          (N'FK_Ventas_CompletadoPorUsuarioId_Fase7', N'Ventas', N'CompletadoPorUsuarioId', N'Usuarios'),
          (N'FK_Ventas_AnuladoPorUsuarioId_Fase7', N'Ventas', N'AnuladoPorUsuarioId', N'Usuarios'),
          (N'FK_VentasDetalle_UnidadMedidaId_Fase7', N'VentasDetalle', N'UnidadMedidaId', N'UnidadesMedida'),
          (N'FK_VentasDetalle_UnidadBaseId_Fase7', N'VentasDetalle', N'UnidadBaseId', N'UnidadesMedida'),
          (N'FK_VentaDetalleImpuestos_Detalle', N'VentaDetalleImpuestos', N'VentaDetalleId', N'VentasDetalle'),
          (N'FK_VentaDetalleImpuestos_Impuesto', N'VentaDetalleImpuestos', N'ImpuestoId', N'Impuestos')
        ) esperado(nombre, tabla, columna, destino)
        LEFT JOIN sys.foreign_keys fk ON fk.object_id=OBJECT_ID(N'dbo.'+esperado.nombre)
        LEFT JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=fk.object_id AND fc.constraint_column_id=1
        WHERE fk.object_id IS NULL OR fk.parent_object_id<>OBJECT_ID(N'dbo.'+esperado.tabla)
          OR fk.referenced_object_id<>OBJECT_ID(N'dbo.'+esperado.destino)
          OR COL_NAME(fc.parent_object_id,fc.parent_column_id)<>esperado.columna
          OR COL_NAME(fc.referenced_object_id,fc.referenced_column_id)<>N'Id'
          OR fk.is_disabled=1 OR fk.is_not_trusted=1 OR fk.delete_referential_action<>0 OR fk.update_referential_action<>0
          OR (SELECT COUNT(*) FROM sys.foreign_key_columns f WHERE f.constraint_object_id=fk.object_id)<>1
    ) THROW 51070, 'Clave foránea Fase 7 incompatible o no verificada.', 1;

    IF EXISTS (
        SELECT 1 FROM (VALUES
          (N'Ventas',N'UX_Ventas_ClaveOperacion',N'EmpresaId',N'ClaveOperacion',1),
          (N'VentaDetalleImpuestos',N'UQ_VentaDetalleImpuestos',N'VentaDetalleId',N'ImpuestoId',0)
        ) esperado(tabla, indice, primera, segunda, filtrado)
        LEFT JOIN sys.indexes i ON i.object_id=OBJECT_ID(N'dbo.'+esperado.tabla) AND i.name=esperado.indice
        WHERE i.index_id IS NULL OR i.is_unique<>1 OR i.is_disabled<>0 OR i.has_filter<>esperado.filtrado
          OR INDEX_COL(N'dbo.'+esperado.tabla,i.index_id,1)<>esperado.primera
          OR INDEX_COL(N'dbo.'+esperado.tabla,i.index_id,2)<>esperado.segunda
          OR (SELECT COUNT(*) FROM sys.index_columns c WHERE c.object_id=i.object_id AND c.index_id=i.index_id AND c.key_ordinal>0)<>2
          OR (esperado.filtrado=1 AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(UPPER(i.filter_definition),N'[',N''),N']',N''),N'(',N''),N')',N''),N' ',N'')<>N'CLAVEOPERACIONISNOTNULL')
    ) THROW 51070, 'Índice Fase 7 existente con claves o filtro incompatible.', 1;

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name IN (
        N'CK_VentasDetalle_Snapshot_Fase7',N'CK_Ventas_Almacen_Fase7',
        N'CK_VentaDetalleImpuestos_Porcentaje',N'CK_VentaDetalleImpuestos_Base',N'CK_VentaDetalleImpuestos_Monto')
        AND (is_disabled=1 OR is_not_trusted=1))
        THROW 51070, 'Restricción Fase 7 deshabilitada o no verificada.', 1;

    COMMIT TRANSACTION;
    PRINT N'FASE 7: esquema de ventas preparado. No se crearon ventas ni catalogos. Ejecute --preparar-fase7 explicitamente despues de revisar la configuracion.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

GO
-- FASE 8 CAJA. Ejecutar explícitamente en la base seleccionada, después de Fase 7.
-- No crea datos de negocio ni concilia históricos. Revisar y respaldar antes de aplicar.
-- Sin GO interno: todo DDL y validación pertenecen a UNA transacción.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lock int;
    EXEC @lock=sys.sp_getapplock @Resource=N'Heladeria.Esquema.Fase8',
        @LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000;
    IF @lock<0 THROW 51080, 'No se obtuvo la reserva del esquema de Caja.', 1;
    IF EXISTS(SELECT 1 FROM (VALUES(N'Cajas'),(N'TurnosCaja'),(N'MovimientosCaja'),(N'ArqueosCaja'),
        (N'Pagos'),(N'PagoDetalles'),(N'FormasPago'),(N'Ventas'),(N'Monedas'),(N'Usuarios')) t(nombre)
        WHERE OBJECT_ID(N'dbo.'+t.nombre,N'U') IS NULL)
        OR COL_LENGTH(N'dbo.Ventas',N'FechaCompletada') IS NULL
        THROW 51080, 'Se requiere el esquema previo con Fase 7; no se crearán tablas equivalentes.', 1;
    IF EXISTS(SELECT CajaId FROM dbo.TurnosCaja WHERE FechaCierre IS NULL GROUP BY CajaId HAVING COUNT(*)>1)
        THROW 51080, 'Existen varias sesiones abiertas de una caja. Conciliar antes de aplicar Fase 8.', 1;
    IF EXISTS(SELECT 1 FROM dbo.Cajas c JOIN dbo.Sucursales s ON s.Id=c.SucursalId WHERE c.EmpresaId<>s.EmpresaId)
        THROW 51080, 'Caja con empresa y sucursal incompatibles; requiere conciliación.', 1;

    DECLARE @columnas TABLE(tabla sysname,columna sysname,declaracion nvarchar(100),tipo sysname,
        longitud smallint,precision tinyint,escala tinyint,nulo bit);
    INSERT @columnas VALUES
        (N'TurnosCaja',N'MonedaId',N'int',N'int',4,10,0,1),
        (N'TurnosCaja',N'UsuarioCierreId',N'int',N'int',4,10,0,1),
        (N'TurnosCaja',N'FechaModificacion',N'datetime2(3)',N'datetime2',7,23,3,1),
        (N'TurnosCaja',N'NumeroApertura',N'varchar(30)',N'varchar',30,0,0,1),
        (N'TurnosCaja',N'NumeroCierre',N'varchar(30)',N'varchar',30,0,0,1),
        (N'TurnosCaja',N'TotalIngresos',N'decimal(12,2)',N'decimal',9,12,2,1),
        (N'TurnosCaja',N'TotalEgresos',N'decimal(12,2)',N'decimal',9,12,2,1),
        (N'TurnosCaja',N'ArqueoCierreId',N'bigint',N'bigint',8,19,0,1),
        (N'TurnosCaja',N'VersionFila',N'rowversion',N'timestamp',8,0,0,0),
        (N'Pagos',N'VentaId',N'bigint',N'bigint',8,19,0,1),
        (N'Pagos',N'ClaveOperacion',N'uniqueidentifier',N'uniqueidentifier',16,0,0,1),
        (N'PagoDetalles',N'MontoRecibido',N'decimal(12,2)',N'decimal',9,12,2,1),
        (N'PagoDetalles',N'EsEfectivo',N'bit',N'bit',1,1,0,1),
        (N'MovimientosCaja',N'PagoDetalleId',N'bigint',N'bigint',8,19,0,1),
        (N'MovimientosCaja',N'CodigoConcepto',N'varchar(30)',N'varchar',30,0,0,1),
        (N'MovimientosCaja',N'EsEfectivo',N'bit',N'bit',1,1,0,1),
        (N'MovimientosCaja',N'NumeroDocumento',N'varchar(30)',N'varchar',30,0,0,1),
        (N'ArqueosCaja',N'VersionTurno',N'varbinary(8)',N'varbinary',8,0,0,1);
    DECLARE @tabla sysname,@columna sysname,@declaracion nvarchar(100),@nulo bit,@sql nvarchar(max);
    DECLARE columnas CURSOR LOCAL FAST_FORWARD FOR SELECT tabla,columna,declaracion,nulo FROM @columnas;
    OPEN columnas;
    FETCH NEXT FROM columnas INTO @tabla,@columna,@declaracion,@nulo;
    WHILE @@FETCH_STATUS=0
    BEGIN
        IF COL_LENGTH(N'dbo.'+@tabla,@columna) IS NULL
        BEGIN
            SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@tabla)+N' ADD '+QUOTENAME(@columna)+N' '+@declaracion+
                CASE WHEN @nulo=1 THEN N' NULL;' ELSE N' NOT NULL;' END;
            EXEC sys.sp_executesql @sql;
        END;
        FETCH NEXT FROM columnas INTO @tabla,@columna,@declaracion,@nulo;
    END;
    CLOSE columnas; DEALLOCATE columnas;
    IF EXISTS(SELECT 1 FROM @columnas e JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.'+e.tabla) AND c.name=e.columna
        WHERE TYPE_NAME(c.system_type_id)<>e.tipo OR c.max_length<>e.longitud OR c.precision<>e.precision
        OR c.scale<>e.escala OR c.is_nullable<>e.nulo OR c.is_computed=1 OR c.is_identity=1
        OR c.default_object_id<>0)
        THROW 51080, 'Columna Fase 8 preexistente incompatible. No se cambiarán tipos ni datos automáticamente.', 1;

    DECLARE @foraneas TABLE(tabla sysname,columna sysname,destino sysname,nombre sysname);
    INSERT @foraneas VALUES
        (N'TurnosCaja',N'MonedaId',N'Monedas',N'FK_TurnosCaja_Monedas_MonedaId'),
        (N'TurnosCaja',N'UsuarioCierreId',N'Usuarios',N'FK_TurnosCaja_Usuarios_UsuarioCierreId'),
        (N'TurnosCaja',N'ArqueoCierreId',N'ArqueosCaja',N'FK_TurnosCaja_ArqueosCaja_ArqueoCierreId'),
        (N'Pagos',N'VentaId',N'Ventas',N'FK_Pagos_Ventas_VentaId'),
        (N'MovimientosCaja',N'PagoDetalleId',N'PagoDetalles',N'FK_MovimientosCaja_PagoDetalles_PagoDetalleId');
    DECLARE @destino sysname,@nombre sysname;
    DECLARE foraneas CURSOR LOCAL FAST_FORWARD FOR SELECT tabla,columna,destino,nombre FROM @foraneas;
    OPEN foraneas;
    FETCH NEXT FROM foraneas INTO @tabla,@columna,@destino,@nombre;
    WHILE @@FETCH_STATUS=0
    BEGIN
        IF OBJECT_ID(N'dbo.'+@nombre,N'F') IS NULL
        BEGIN
            SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@tabla)+N' WITH CHECK ADD CONSTRAINT '+QUOTENAME(@nombre)+
                N' FOREIGN KEY('+QUOTENAME(@columna)+N') REFERENCES dbo.'+QUOTENAME(@destino)+N'(Id);';
            EXEC sys.sp_executesql @sql;
        END;
        IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys k JOIN sys.foreign_key_columns c ON c.constraint_object_id=k.object_id
            WHERE k.name=@nombre AND k.parent_object_id=OBJECT_ID(N'dbo.'+@tabla)
            AND k.referenced_object_id=OBJECT_ID(N'dbo.'+@destino) AND k.is_disabled=0 AND k.is_not_trusted=0
            AND k.delete_referential_action=0 AND k.update_referential_action=0
            AND COL_NAME(c.parent_object_id,c.parent_column_id)=@columna AND COL_NAME(c.referenced_object_id,c.referenced_column_id)=N'Id'
            AND (SELECT COUNT(*) FROM sys.foreign_key_columns f WHERE f.constraint_object_id=k.object_id)=1)
            THROW 51080, 'Clave foránea Fase 8 incompatible, deshabilitada o no verificada.', 1;
        FETCH NEXT FROM foraneas INTO @tabla,@columna,@destino,@nombre;
    END;
    CLOSE foraneas; DEALLOCATE foraneas;

    DECLARE @checks TABLE(tabla sysname,nombre sysname,definicion nvarchar(max));
    INSERT @checks VALUES
        (N'TurnosCaja',N'CK_TurnosCaja_Snapshot_Fase8',N'(MonedaId IS NULL AND NumeroApertura IS NULL AND NumeroCierre IS NULL AND UsuarioCierreId IS NULL AND TotalIngresos IS NULL AND TotalEgresos IS NULL AND ArqueoCierreId IS NULL) OR (MonedaId IS NOT NULL AND NumeroApertura IS NOT NULL AND FechaModificacion IS NOT NULL AND ((FechaCierre IS NULL AND NumeroCierre IS NULL AND UsuarioCierreId IS NULL AND TotalIngresos IS NULL AND TotalEgresos IS NULL AND ArqueoCierreId IS NULL AND MontoEsperado IS NULL AND MontoFisico IS NULL) OR (FechaCierre IS NOT NULL AND NumeroCierre IS NOT NULL AND UsuarioCierreId IS NOT NULL AND TotalIngresos IS NOT NULL AND TotalIngresos>=0 AND TotalEgresos IS NOT NULL AND TotalEgresos>=0 AND ArqueoCierreId IS NOT NULL AND MontoEsperado IS NOT NULL AND MontoEsperado>=0 AND MontoFisico IS NOT NULL AND MontoFisico>=0)))'),
        (N'Pagos',N'CK_Pagos_Tercero',N'(VentaId IS NULL AND ClaveOperacion IS NULL AND ((ClienteId IS NOT NULL AND ProveedorId IS NULL) OR (ClienteId IS NULL AND ProveedorId IS NOT NULL))) OR (VentaId IS NOT NULL AND ClaveOperacion IS NOT NULL AND ProveedorId IS NULL AND TurnoCajaId IS NOT NULL AND SucursalId IS NOT NULL)'),
        (N'PagoDetalles',N'CK_PagoDetalles_Snapshot_Fase8',N'(MontoRecibido IS NULL AND EsEfectivo IS NULL) OR (MontoRecibido IS NOT NULL AND EsEfectivo IS NOT NULL AND MontoRecibido>=Monto AND (EsEfectivo=1 OR MontoRecibido=Monto))'),
        (N'MovimientosCaja',N'CK_MovimientosCaja_Snapshot_Fase8',N'(CodigoConcepto IS NULL AND EsEfectivo IS NULL AND NumeroDocumento IS NULL AND PagoDetalleId IS NULL) OR (CodigoConcepto IS NOT NULL AND EsEfectivo IS NOT NULL AND NumeroDocumento IS NOT NULL AND FormaPagoId IS NOT NULL AND ((CodigoConcepto=''COBRO_VENTA'' AND PagoDetalleId IS NOT NULL) OR ((CodigoConcepto=''APERTURA_CAJA'' OR CodigoConcepto=''INGRESO_CAJA'' OR CodigoConcepto=''EGRESO_CAJA'') AND PagoDetalleId IS NULL AND EsEfectivo=1)))'),
        (N'ArqueosCaja',N'CK_ArqueosCaja_Version_Fase8',N'VersionTurno IS NULL OR (DATALENGTH(VersionTurno)=8 AND MontoEsperado>=0 AND MontoContado>=0)');
    -- Fase 9 amplía estas dos restricciones; una revalidación de Fase 8 no debe degradarlas.
    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Pagos_Huella_Fase9' AND parent_object_id=OBJECT_ID(N'dbo.Pagos') AND is_disabled=0 AND is_not_trusted=0)
    BEGIN
        UPDATE @checks SET definicion=N'(VentaId IS NULL AND ClaveOperacion IS NULL AND ((ClienteId IS NOT NULL AND ProveedorId IS NULL) OR (ClienteId IS NULL AND ProveedorId IS NOT NULL))) OR (VentaId IS NOT NULL AND ClaveOperacion IS NOT NULL AND ProveedorId IS NULL AND TurnoCajaId IS NOT NULL AND SucursalId IS NOT NULL) OR (VentaId IS NULL AND ClaveOperacion IS NOT NULL AND HuellaSolicitud IS NOT NULL AND TurnoCajaId IS NOT NULL AND SucursalId IS NOT NULL AND ((ClienteId IS NOT NULL AND ProveedorId IS NULL) OR (ClienteId IS NULL AND ProveedorId IS NOT NULL)))' WHERE nombre=N'CK_Pagos_Tercero';
        UPDATE @checks SET definicion=N'(CodigoConcepto IS NULL AND EsEfectivo IS NULL AND NumeroDocumento IS NULL AND PagoDetalleId IS NULL) OR (CodigoConcepto IS NOT NULL AND EsEfectivo IS NOT NULL AND NumeroDocumento IS NOT NULL AND FormaPagoId IS NOT NULL AND (((CodigoConcepto=''COBRO_VENTA'' OR CodigoConcepto=''COBRO_CUENTA_POR_COBRAR'' OR (CodigoConcepto=''PAGO_CUENTA_POR_PAGAR'' AND EsEfectivo=1)) AND PagoDetalleId IS NOT NULL) OR ((CodigoConcepto=''APERTURA_CAJA'' OR CodigoConcepto=''INGRESO_CAJA'' OR CodigoConcepto=''EGRESO_CAJA'') AND PagoDetalleId IS NULL AND EsEfectivo=1)))' WHERE nombre=N'CK_MovimientosCaja_Snapshot_Fase8';
    END;
    DECLARE @esperada nvarchar(max),@def nvarchar(max),@normal nvarchar(max),@normalEsperada nvarchar(max),@canon nvarchar(max);
    DECLARE restricciones CURSOR LOCAL FAST_FORWARD FOR SELECT tabla,nombre,definicion FROM @checks;
    OPEN restricciones;
    FETCH NEXT FROM restricciones INTO @tabla,@nombre,@esperada;
    WHILE @@FETCH_STATUS=0
    BEGIN
        -- Compilar la expresión esperada en tabla temporal vacía permite comparar su forma canónica.
        -- Conserva paréntesis lógicos: quitarlos podría ocultar un cambio de precedencia AND/OR.
        SET @sql=N'SELECT TOP (0) * INTO #F8Check FROM dbo.'+QUOTENAME(@tabla)+N';
            ALTER TABLE #F8Check ADD CHECK ('+@esperada+N');
            SELECT @salida=definition FROM tempdb.sys.check_constraints
                WHERE parent_object_id=OBJECT_ID(N''tempdb..#F8Check'');';
        EXEC sys.sp_executesql @sql,N'@salida nvarchar(max) OUTPUT',@salida=@canon OUTPUT;
        SET @normalEsperada=REPLACE(REPLACE(REPLACE(REPLACE(UPPER(@canon),N' ',N''),CHAR(9),N''),CHAR(13),N''),CHAR(10),N'');
        SET @def=NULL;
        SELECT @def=definition FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.'+@tabla) AND name=@nombre;
        SET @normal=REPLACE(REPLACE(REPLACE(REPLACE(UPPER(@def),N' ',N''),CHAR(9),N''),CHAR(13),N''),CHAR(10),N'');
        -- Única sustitución deliberada: permitir POS anónimo manteniendo la regla para otros pagos.
        IF @nombre=N'CK_Pagos_Tercero' AND @def IS NOT NULL AND @normal<>@normalEsperada
        BEGIN
            EXEC sys.sp_executesql N'SELECT TOP(0) * INTO #F8CheckAnterior FROM dbo.Pagos;
                ALTER TABLE #F8CheckAnterior ADD CHECK ((ClienteId IS NOT NULL AND ProveedorId IS NULL) OR (ClienteId IS NULL AND ProveedorId IS NOT NULL));
                SELECT @salida=definition FROM tempdb.sys.check_constraints WHERE parent_object_id=OBJECT_ID(N''tempdb..#F8CheckAnterior'');',
                N'@salida nvarchar(max) OUTPUT',@salida=@canon OUTPUT;
            IF @normal<>REPLACE(REPLACE(REPLACE(REPLACE(UPPER(@canon),N' ',N''),CHAR(9),N''),CHAR(13),N''),CHAR(10),N'')
                THROW 51080, 'CK_Pagos_Tercero personalizada; no se reemplazará sin revisión.', 1;
            ALTER TABLE dbo.Pagos DROP CONSTRAINT CK_Pagos_Tercero;
            SET @def=NULL;
        END;
        IF @def IS NULL
        BEGIN
            SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@tabla)+N' WITH CHECK ADD CONSTRAINT '+QUOTENAME(@nombre)+N' CHECK ('+@esperada+N');';
            EXEC sys.sp_executesql @sql;
        END
        ELSE IF @normal<>@normalEsperada
        BEGIN
            DECLARE @mensaje nvarchar(2048)=N'Restricción Fase 8 preexistente incompatible: '+@nombre;
            THROW 51080, @mensaje, 1;
        END;
        IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.'+@tabla)
            AND name=@nombre AND (is_disabled=1 OR is_not_trusted=1))
            THROW 51080, 'Restricción Fase 8 deshabilitada o no verificada.', 1;
        FETCH NEXT FROM restricciones INTO @tabla,@nombre,@esperada;
    END;
    CLOSE restricciones; DEALLOCATE restricciones;

    DECLARE @indices TABLE(tabla sysname,nombre sysname,claves nvarchar(100),filtro nvarchar(100));
    INSERT @indices VALUES
        (N'TurnosCaja',N'UX_TurnosCaja_CajaAbierta',N'CajaId',N'[FechaCierre] IS NULL'),
        (N'Pagos',N'UX_Pagos_Venta',N'VentaId',N'[VentaId] IS NOT NULL'),
        (N'Pagos',N'UX_Pagos_ClaveOperacion',N'EmpresaId,ClaveOperacion',N'[ClaveOperacion] IS NOT NULL'),
        (N'MovimientosCaja',N'UX_MovimientosCaja_PagoDetalle',N'PagoDetalleId',N'[PagoDetalleId] IS NOT NULL');
    DECLARE @claves nvarchar(100),@filtro nvarchar(100),@reales nvarchar(max);
    DECLARE indices CURSOR LOCAL FAST_FORWARD FOR SELECT tabla,nombre,claves,filtro FROM @indices;
    OPEN indices;
    FETCH NEXT FROM indices INTO @tabla,@nombre,@claves,@filtro;
    WHILE @@FETCH_STATUS=0
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.'+@tabla) AND name=@nombre)
        BEGIN
            SET @sql=N'CREATE UNIQUE INDEX '+QUOTENAME(@nombre)+N' ON dbo.'+QUOTENAME(@tabla)+N'('+@claves+N') WHERE '+@filtro+N';';
            EXEC sys.sp_executesql @sql;
        END;
        SET @def=@filtro; SET @normalEsperada=REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(UPPER(@def),N'[',N''),N']',N''),N'(',N''),N')',N''),N' ',N''),CHAR(13),N''),CHAR(10),N'');
        SELECT @def=filter_definition FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.'+@tabla) AND name=@nombre;
        SET @normal=REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(UPPER(@def),N'[',N''),N']',N''),N'(',N''),N')',N''),N' ',N''),CHAR(13),N''),CHAR(10),N'');
        SELECT @reales=STRING_AGG(CONVERT(nvarchar(max),c.name),N',') WITHIN GROUP(ORDER BY ic.key_ordinal)
        FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0
        JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
        WHERE i.object_id=OBJECT_ID(N'dbo.'+@tabla) AND i.name=@nombre;
        IF @reales IS NULL OR @reales<>@claves OR @normal IS NULL OR @normal<>@normalEsperada
            OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.'+@tabla) AND name=@nombre
                AND is_unique=1 AND is_disabled=0 AND has_filter=1 AND ignore_dup_key=0)
            THROW 51080, 'Índice Fase 8 incompatible. No se reemplazará silenciosamente.', 1;
        FETCH NEXT FROM indices INTO @tabla,@nombre,@claves,@filtro;
    END;
    CLOSE indices; DEALLOCATE indices;
    COMMIT TRANSACTION;
    PRINT N'FASE 8: esquema Caja validado. Sin datos operativos. Revise y ejecute --preparar-fase8 explícitamente.';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- FASE 9 CXC CXP. Ejecutar explícitamente en la base seleccionada, después de Fase 8.
-- No crea datos de negocio ni concilia históricos. Revisar y respaldar antes de aplicar.
-- Sin GO interno: todo DDL y validación pertenecen a UNA transacción.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lock int;
    EXEC @lock=sys.sp_getapplock @Resource=N'Heladeria.Esquema.Fase9',
        @LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000;
    IF @lock<0 THROW 51090, 'No se obtuvo la reserva del esquema de Caja.', 1;
    IF EXISTS(SELECT 1 FROM (VALUES(N'CuentasPorCobrar'),(N'CuentasPorPagar'),(N'FacturasProveedores'),
        (N'AplicacionesCuentasPorCobrar'),(N'AplicacionesCuentasPorPagar'),(N'Pagos'),(N'OrdenesCompra'),(N'Ventas')) t(nombre)
        WHERE OBJECT_ID(N'dbo.'+t.nombre,N'U') IS NULL)
        OR COL_LENGTH(N'dbo.Pagos',N'ClaveOperacion') IS NULL
        THROW 51090, 'Se requiere el esquema existente y Fase 8. No se crean tablas equivalentes.', 1;

    DECLARE @columnas TABLE(tabla sysname,columna sysname,declaracion nvarchar(100),tipo sysname,
        longitud smallint,precision tinyint,escala tinyint,nulo bit);
    INSERT @columnas VALUES
        (N'Ventas',N'FechaVencimientoCredito',N'date',N'date',3,10,0,1),
        (N'OrdenesCompra',N'EsCredito',N'bit',N'bit',1,1,0,1),
        (N'OrdenesCompra',N'VersionFila',N'rowversion',N'timestamp',8,0,0,0),
        (N'Pagos',N'HuellaSolicitud',N'varbinary(32)',N'varbinary',32,0,0,1),
        (N'Pagos',N'Observacion',N'nvarchar(500)',N'nvarchar',1000,0,0,1),
        (N'FacturasProveedores',N'ClaveOperacion',N'uniqueidentifier',N'uniqueidentifier',16,0,0,1),
        (N'FacturasProveedores',N'HuellaSolicitud',N'varbinary(32)',N'varbinary',32,0,0,1),
        (N'CuentasPorCobrar',N'SucursalId',N'bigint',N'bigint',8,19,0,1),
        (N'CuentasPorCobrar',N'MonedaId',N'int',N'int',4,10,0,1),
        (N'CuentasPorCobrar',N'UsuarioCreacionId',N'int',N'int',4,10,0,1),
        (N'CuentasPorCobrar',N'FechaCreacion',N'datetime2(3)',N'datetime2',7,23,3,1),
        (N'CuentasPorCobrar',N'UsuarioModificacionId',N'int',N'int',4,10,0,1),
        (N'CuentasPorCobrar',N'FechaModificacion',N'datetime2(3)',N'datetime2',7,23,3,1),
        (N'CuentasPorCobrar',N'VersionFila',N'rowversion',N'timestamp',8,0,0,0),
        (N'CuentasPorPagar',N'SucursalId',N'bigint',N'bigint',8,19,0,1),
        (N'CuentasPorPagar',N'MonedaId',N'int',N'int',4,10,0,1),
        (N'CuentasPorPagar',N'UsuarioCreacionId',N'int',N'int',4,10,0,1),
        (N'CuentasPorPagar',N'FechaCreacion',N'datetime2(3)',N'datetime2',7,23,3,1),
        (N'CuentasPorPagar',N'UsuarioModificacionId',N'int',N'int',4,10,0,1),
        (N'CuentasPorPagar',N'FechaModificacion',N'datetime2(3)',N'datetime2',7,23,3,1),
        (N'CuentasPorPagar',N'VersionFila',N'rowversion',N'timestamp',8,0,0,0);
    DECLARE @tabla sysname,@columna sysname,@declaracion nvarchar(100),@nulo bit,@sql nvarchar(max);
    DECLARE columnas CURSOR LOCAL FAST_FORWARD FOR SELECT tabla,columna,declaracion,nulo FROM @columnas;
    OPEN columnas;
    FETCH NEXT FROM columnas INTO @tabla,@columna,@declaracion,@nulo;
    WHILE @@FETCH_STATUS=0
    BEGIN
        IF COL_LENGTH(N'dbo.'+@tabla,@columna) IS NULL
        BEGIN
            SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@tabla)+N' ADD '+QUOTENAME(@columna)+N' '+@declaracion+
                CASE WHEN @nulo=1 THEN N' NULL;' ELSE N' NOT NULL;' END;
            EXEC sys.sp_executesql @sql;
        END;
        FETCH NEXT FROM columnas INTO @tabla,@columna,@declaracion,@nulo;
    END;
    CLOSE columnas; DEALLOCATE columnas;
    IF EXISTS(SELECT 1 FROM @columnas e JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.'+e.tabla) AND c.name=e.columna
        WHERE TYPE_NAME(c.system_type_id)<>e.tipo OR c.max_length<>e.longitud OR c.precision<>e.precision
        OR c.scale<>e.escala OR c.is_nullable<>e.nulo OR c.is_computed=1 OR c.is_identity=1
        OR c.default_object_id<>0)
        THROW 51090, 'Columna Fase 9 preexistente incompatible. No se cambiarán tipos ni datos automáticamente.', 1;

    DECLARE @foraneas TABLE(tabla sysname,columna sysname,destino sysname,nombre sysname);
    INSERT @foraneas VALUES
        (N'CuentasPorCobrar',N'SucursalId',N'Sucursales',N'FK_CuentasPorCobrar_Sucursales_SucursalId'),
        (N'CuentasPorCobrar',N'MonedaId',N'Monedas',N'FK_CuentasPorCobrar_Monedas_MonedaId'),
        (N'CuentasPorCobrar',N'UsuarioCreacionId',N'Usuarios',N'FK_CuentasPorCobrar_Usuarios_UsuarioCreacionId'),
        (N'CuentasPorCobrar',N'UsuarioModificacionId',N'Usuarios',N'FK_CuentasPorCobrar_Usuarios_UsuarioModificacionId'),
        (N'CuentasPorPagar',N'SucursalId',N'Sucursales',N'FK_CuentasPorPagar_Sucursales_SucursalId'),
        (N'CuentasPorPagar',N'MonedaId',N'Monedas',N'FK_CuentasPorPagar_Monedas_MonedaId'),
        (N'CuentasPorPagar',N'UsuarioCreacionId',N'Usuarios',N'FK_CuentasPorPagar_Usuarios_UsuarioCreacionId'),
        (N'CuentasPorPagar',N'UsuarioModificacionId',N'Usuarios',N'FK_CuentasPorPagar_Usuarios_UsuarioModificacionId');
    DECLARE @destino sysname,@nombre sysname;
    DECLARE foraneas CURSOR LOCAL FAST_FORWARD FOR SELECT tabla,columna,destino,nombre FROM @foraneas;
    OPEN foraneas;
    FETCH NEXT FROM foraneas INTO @tabla,@columna,@destino,@nombre;
    WHILE @@FETCH_STATUS=0
    BEGIN
        IF OBJECT_ID(N'dbo.'+@nombre,N'F') IS NULL
        BEGIN
            SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@tabla)+N' WITH CHECK ADD CONSTRAINT '+QUOTENAME(@nombre)+
                N' FOREIGN KEY('+QUOTENAME(@columna)+N') REFERENCES dbo.'+QUOTENAME(@destino)+N'(Id);';
            EXEC sys.sp_executesql @sql;
        END;
        IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys k JOIN sys.foreign_key_columns c ON c.constraint_object_id=k.object_id
            WHERE k.name=@nombre AND k.parent_object_id=OBJECT_ID(N'dbo.'+@tabla)
            AND k.referenced_object_id=OBJECT_ID(N'dbo.'+@destino) AND k.is_disabled=0 AND k.is_not_trusted=0
            AND k.delete_referential_action=0 AND k.update_referential_action=0
            AND COL_NAME(c.parent_object_id,c.parent_column_id)=@columna AND COL_NAME(c.referenced_object_id,c.referenced_column_id)=N'Id'
            AND (SELECT COUNT(*) FROM sys.foreign_key_columns f WHERE f.constraint_object_id=k.object_id)=1)
            THROW 51090, 'Clave foránea Fase 9 incompatible, deshabilitada o no verificada.', 1;
        FETCH NEXT FROM foraneas INTO @tabla,@columna,@destino,@nombre;
    END;
    CLOSE foraneas; DEALLOCATE foraneas;

    DECLARE @checks TABLE(tabla sysname,nombre sysname,definicion nvarchar(max));
    INSERT @checks VALUES
        (N'Pagos',N'CK_Pagos_Tercero',N'(VentaId IS NULL AND ClaveOperacion IS NULL AND ((ClienteId IS NOT NULL AND ProveedorId IS NULL) OR (ClienteId IS NULL AND ProveedorId IS NOT NULL))) OR (VentaId IS NOT NULL AND ClaveOperacion IS NOT NULL AND ProveedorId IS NULL AND TurnoCajaId IS NOT NULL AND SucursalId IS NOT NULL) OR (VentaId IS NULL AND ClaveOperacion IS NOT NULL AND HuellaSolicitud IS NOT NULL AND TurnoCajaId IS NOT NULL AND SucursalId IS NOT NULL AND ((ClienteId IS NOT NULL AND ProveedorId IS NULL) OR (ClienteId IS NULL AND ProveedorId IS NOT NULL)))'),
        (N'MovimientosCaja',N'CK_MovimientosCaja_Snapshot_Fase8',N'(CodigoConcepto IS NULL AND EsEfectivo IS NULL AND NumeroDocumento IS NULL AND PagoDetalleId IS NULL) OR (CodigoConcepto IS NOT NULL AND EsEfectivo IS NOT NULL AND NumeroDocumento IS NOT NULL AND FormaPagoId IS NOT NULL AND (((CodigoConcepto=''COBRO_VENTA'' OR CodigoConcepto=''COBRO_CUENTA_POR_COBRAR'' OR (CodigoConcepto=''PAGO_CUENTA_POR_PAGAR'' AND EsEfectivo=1)) AND PagoDetalleId IS NOT NULL) OR ((CodigoConcepto=''APERTURA_CAJA'' OR CodigoConcepto=''INGRESO_CAJA'' OR CodigoConcepto=''EGRESO_CAJA'') AND PagoDetalleId IS NULL AND EsEfectivo=1)))'),
        (N'Pagos',N'CK_Pagos_Huella_Fase9',N'HuellaSolicitud IS NULL OR (DATALENGTH(HuellaSolicitud)=32 AND ClaveOperacion IS NOT NULL AND VentaId IS NULL)'),
        (N'FacturasProveedores',N'CK_FacturasProveedores_Operacion_Fase9',N'(ClaveOperacion IS NULL AND HuellaSolicitud IS NULL) OR (ClaveOperacion IS NOT NULL AND HuellaSolicitud IS NOT NULL AND DATALENGTH(HuellaSolicitud)=32 AND OrdenCompraId IS NOT NULL AND FechaVencimiento IS NOT NULL AND FechaVencimiento>=FechaDocumento AND Total>0)'),
        (N'CuentasPorCobrar',N'CK_CuentasPorCobrar_Metadata_Fase9',N'(FechaCreacion IS NULL AND SucursalId IS NULL AND MonedaId IS NULL AND UsuarioCreacionId IS NULL AND UsuarioModificacionId IS NULL AND FechaModificacion IS NULL) OR (FechaCreacion IS NOT NULL AND SucursalId IS NOT NULL AND MonedaId IS NOT NULL AND UsuarioCreacionId IS NOT NULL AND MontoOriginal>0 AND SaldoPendiente>=0 AND SaldoPendiente<=MontoOriginal AND FechaVencimiento>=FechaEmision AND ((UsuarioModificacionId IS NULL AND FechaModificacion IS NULL) OR (UsuarioModificacionId IS NOT NULL AND FechaModificacion IS NOT NULL)))'),
        (N'CuentasPorPagar',N'CK_CuentasPorPagar_Metadata_Fase9',N'(FechaCreacion IS NULL AND SucursalId IS NULL AND MonedaId IS NULL AND UsuarioCreacionId IS NULL AND UsuarioModificacionId IS NULL AND FechaModificacion IS NULL) OR (FechaCreacion IS NOT NULL AND SucursalId IS NOT NULL AND MonedaId IS NOT NULL AND UsuarioCreacionId IS NOT NULL AND MontoOriginal>0 AND SaldoPendiente>=0 AND SaldoPendiente<=MontoOriginal AND FechaVencimiento>=FechaEmision AND ((UsuarioModificacionId IS NULL AND FechaModificacion IS NULL) OR (UsuarioModificacionId IS NOT NULL AND FechaModificacion IS NOT NULL)))');
    DECLARE @esperada nvarchar(max),@def nvarchar(max),@normal nvarchar(max),@normalEsperada nvarchar(max),@canon nvarchar(max);
    DECLARE restricciones CURSOR LOCAL FAST_FORWARD FOR SELECT tabla,nombre,definicion FROM @checks;
    OPEN restricciones;
    FETCH NEXT FROM restricciones INTO @tabla,@nombre,@esperada;
    WHILE @@FETCH_STATUS=0
    BEGIN
        -- Compilar la expresión esperada en tabla temporal vacía permite comparar su forma canónica.
        -- Conserva paréntesis lógicos: quitarlos podría ocultar un cambio de precedencia AND/OR.
        SET @sql=N'SELECT TOP (0) * INTO #F9Check FROM dbo.'+QUOTENAME(@tabla)+N';
            ALTER TABLE #F9Check ADD CHECK ('+@esperada+N');
            SELECT @salida=definition FROM tempdb.sys.check_constraints
                WHERE parent_object_id=OBJECT_ID(N''tempdb..#F9Check'');';
        EXEC sys.sp_executesql @sql,N'@salida nvarchar(max) OUTPUT',@salida=@canon OUTPUT;
        SET @normalEsperada=REPLACE(REPLACE(REPLACE(REPLACE(UPPER(@canon),N' ',N''),CHAR(9),N''),CHAR(13),N''),CHAR(10),N'');
        SET @def=NULL;
        SELECT @def=definition FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.'+@tabla) AND name=@nombre;
        SET @normal=REPLACE(REPLACE(REPLACE(REPLACE(UPPER(@def),N' ',N''),CHAR(9),N''),CHAR(13),N''),CHAR(10),N'');
        -- Solo se permite ampliar las dos expresiones exactas aprobadas en Fase 8.
        IF @nombre IN (N'CK_Pagos_Tercero',N'CK_MovimientosCaja_Snapshot_Fase8') AND @def IS NOT NULL AND @normal<>@normalEsperada
        BEGIN
            DECLARE @anterior nvarchar(max)=CASE WHEN @nombre=N'CK_Pagos_Tercero' THEN N'(VentaId IS NULL AND ClaveOperacion IS NULL AND ((ClienteId IS NOT NULL AND ProveedorId IS NULL) OR (ClienteId IS NULL AND ProveedorId IS NOT NULL))) OR (VentaId IS NOT NULL AND ClaveOperacion IS NOT NULL AND ProveedorId IS NULL AND TurnoCajaId IS NOT NULL AND SucursalId IS NOT NULL)' ELSE N'(CodigoConcepto IS NULL AND EsEfectivo IS NULL AND NumeroDocumento IS NULL AND PagoDetalleId IS NULL) OR (CodigoConcepto IS NOT NULL AND EsEfectivo IS NOT NULL AND NumeroDocumento IS NOT NULL AND FormaPagoId IS NOT NULL AND ((CodigoConcepto=''COBRO_VENTA'' AND PagoDetalleId IS NOT NULL) OR ((CodigoConcepto=''APERTURA_CAJA'' OR CodigoConcepto=''INGRESO_CAJA'' OR CodigoConcepto=''EGRESO_CAJA'') AND PagoDetalleId IS NULL AND EsEfectivo=1)))' END;
            SET @sql=N'SELECT TOP(0) * INTO #F9CheckAnterior FROM dbo.'+QUOTENAME(@tabla)+N';
                ALTER TABLE #F9CheckAnterior ADD CHECK ('+@anterior+N');
                SELECT @salida=definition FROM tempdb.sys.check_constraints WHERE parent_object_id=OBJECT_ID(N''tempdb..#F9CheckAnterior'');';
            EXEC sys.sp_executesql @sql,N'@salida nvarchar(max) OUTPUT',@salida=@canon OUTPUT;
            IF @normal<>REPLACE(REPLACE(REPLACE(REPLACE(UPPER(@canon),N' ',N''),CHAR(9),N''),CHAR(13),N''),CHAR(10),N'')
                THROW 51090, 'Restricción financiera personalizada; no se reemplazará sin revisión.', 1;
            SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@tabla)+N' DROP CONSTRAINT '+QUOTENAME(@nombre)+N';';
            EXEC sys.sp_executesql @sql;
            SET @def=NULL;
        END;
        IF @def IS NULL
        BEGIN
            SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@tabla)+N' WITH CHECK ADD CONSTRAINT '+QUOTENAME(@nombre)+N' CHECK ('+@esperada+N');';
            EXEC sys.sp_executesql @sql;
        END
        ELSE IF @normal<>@normalEsperada
        BEGIN
            DECLARE @mensaje nvarchar(2048)=N'Restricción Fase 9 preexistente incompatible: '+@nombre;
            THROW 51090, @mensaje, 1;
        END;
        IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.'+@tabla)
            AND name=@nombre AND (is_disabled=1 OR is_not_trusted=1))
            THROW 51090, 'Restricción Fase 9 deshabilitada o no verificada.', 1;
        FETCH NEXT FROM restricciones INTO @tabla,@nombre,@esperada;
    END;
    CLOSE restricciones; DEALLOCATE restricciones;

    DECLARE @indices TABLE(tabla sysname,nombre sysname,claves nvarchar(100),filtro nvarchar(100));
    INSERT @indices VALUES
        (N'FacturasProveedores',N'UX_FacturasProveedores_ClaveOperacion',N'EmpresaId,ClaveOperacion',N'[ClaveOperacion] IS NOT NULL'),
        (N'FacturasProveedores',N'UX_FacturasProveedores_OrdenFase9',N'OrdenCompraId',N'[ClaveOperacion] IS NOT NULL AND [OrdenCompraId] IS NOT NULL');
    DECLARE @claves nvarchar(100),@filtro nvarchar(100),@reales nvarchar(max);
    DECLARE indices CURSOR LOCAL FAST_FORWARD FOR SELECT tabla,nombre,claves,filtro FROM @indices;
    OPEN indices;
    FETCH NEXT FROM indices INTO @tabla,@nombre,@claves,@filtro;
    WHILE @@FETCH_STATUS=0
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.'+@tabla) AND name=@nombre)
        BEGIN
            SET @sql=N'CREATE UNIQUE INDEX '+QUOTENAME(@nombre)+N' ON dbo.'+QUOTENAME(@tabla)+N'('+@claves+N') WHERE '+@filtro+N';';
            EXEC sys.sp_executesql @sql;
        END;
        SET @def=@filtro; SET @normalEsperada=REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(UPPER(@def),N'[',N''),N']',N''),N'(',N''),N')',N''),N' ',N''),CHAR(13),N''),CHAR(10),N'');
        SELECT @def=filter_definition FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.'+@tabla) AND name=@nombre;
        SET @normal=REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(UPPER(@def),N'[',N''),N']',N''),N'(',N''),N')',N''),N' ',N''),CHAR(13),N''),CHAR(10),N'');
        SELECT @reales=STRING_AGG(CONVERT(nvarchar(max),c.name),N',') WITHIN GROUP(ORDER BY ic.key_ordinal)
        FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0
        JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
        WHERE i.object_id=OBJECT_ID(N'dbo.'+@tabla) AND i.name=@nombre;
        IF @reales IS NULL OR @reales<>@claves OR @normal IS NULL OR @normal<>@normalEsperada
            OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.'+@tabla) AND name=@nombre
                AND is_unique=1 AND is_disabled=0 AND has_filter=1 AND ignore_dup_key=0)
            THROW 51090, 'Índice Fase 9 incompatible. No se reemplazará silenciosamente.', 1;
        FETCH NEXT FROM indices INTO @tabla,@nombre,@claves,@filtro;
    END;
    CLOSE indices; DEALLOCATE indices;
    COMMIT TRANSACTION;
    PRINT N'FASE 9: esquema CxC/CxP validado. Sin datos operativos. Revise y ejecute --preparar-fase9 explícitamente.';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

GO

-- AJUSTE D COMPOSICION VENTA. Ejecutar explícitamente en la base seleccionada y con respaldo.
-- No crea productos, catálogos ni ventas. No convierte recetas de producción.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF @@TRANCOUNT <> 0 THROW 51110, 'Ejecute Ajuste D fuera de otra transacción.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.CombosDetalle', N'U') IS NULL OR OBJECT_ID(N'dbo.VentasDetalle', N'U') IS NULL
        OR OBJECT_ID(N'dbo.MovimientoDetalles', N'U') IS NULL
        THROW 51110, 'Falta el esquema existente de Productos/Ventas/Inventario.', 1;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.CombosDetalle') AND name=N'Cantidad'
        AND NOT (is_nullable=0 AND (system_type_id=56 OR system_type_id=106 AND precision=19 AND scale=6)))
        THROW 51110, 'CombosDetalle.Cantidad tiene un tipo incompatible.', 1;
    DECLARE @defaultCantidad sysname, @defCantidad nvarchar(max), @ddlCantidad nvarchar(max);
    SELECT @defaultCantidad=d.name,@defCantidad=d.definition FROM sys.default_constraints d
        JOIN sys.columns c ON c.object_id=d.parent_object_id AND c.column_id=d.parent_column_id
        WHERE d.parent_object_id=OBJECT_ID(N'dbo.CombosDetalle') AND c.name=N'Cantidad';
    IF @defCantidad IS NOT NULL AND REPLACE(REPLACE(REPLACE(@defCantidad,N'(',N''),N')',N''),N' ',N'')<>N'1'
        THROW 51110, 'Default de Cantidad incompatible; no se reemplazará silenciosamente.', 1;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.CombosDetalle') AND name=N'Cantidad' AND system_type_id=56)
    BEGIN
        IF @defaultCantidad IS NOT NULL
        BEGIN
            SET @ddlCantidad=N'ALTER TABLE dbo.CombosDetalle DROP CONSTRAINT '+QUOTENAME(@defaultCantidad);
            EXEC sys.sp_executesql @ddlCantidad;
        END;
        ALTER TABLE dbo.CombosDetalle ALTER COLUMN Cantidad decimal(19,6) NOT NULL;
        SET @ddlCantidad=N'ALTER TABLE dbo.CombosDetalle ADD CONSTRAINT '+QUOTENAME(COALESCE(@defaultCantidad,N'DF_CombosDetalle_Cantidad'))+N' DEFAULT(1) FOR Cantidad';
        EXEC sys.sp_executesql @ddlCantidad;
    END;

    IF COL_LENGTH(N'dbo.CombosDetalle', N'ProductoPresentacionId') IS NULL
        ALTER TABLE dbo.CombosDetalle ADD ProductoPresentacionId bigint NULL;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'EsCompuesto') IS NULL
        ALTER TABLE dbo.VentasDetalle ADD EsCompuesto bit NULL;
    IF COL_LENGTH(N'dbo.MovimientoDetalles', N'VentaDetalleId') IS NULL
        ALTER TABLE dbo.MovimientoDetalles ADD VentaDetalleId bigint NULL;
    IF COL_LENGTH(N'dbo.MovimientoDetalles', N'UnidadBaseId') IS NULL
        ALTER TABLE dbo.MovimientoDetalles ADD UnidadBaseId int NULL;

    IF EXISTS (SELECT 1 FROM (VALUES (N'CombosDetalle',N'ProductoPresentacionId',127),
        (N'VentasDetalle',N'EsCompuesto',104),(N'MovimientoDetalles',N'VentaDetalleId',127),
        (N'MovimientoDetalles',N'UnidadBaseId',56)) esperado(tabla,columna,tipo)
        LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.'+esperado.tabla) AND c.name=esperado.columna
        WHERE c.column_id IS NULL OR c.system_type_id<>esperado.tipo OR c.is_nullable<>1 OR c.is_computed<>0)
        THROW 51110, 'Una columna existente del Ajuste D es incompatible.', 1;

    -- Batch compilado después de ALTER. Nunca se referencia estáticamente una columna recién añadida.
    EXEC sys.sp_executesql N'
        IF EXISTS (SELECT 1 FROM dbo.CombosDetalle WHERE Cantidad<=0 OR ComboProductoId=ProductoIncluidoId)
            THROW 51110, ''Composición histórica inválida: cantidad o autorreferencia. No se inventarán valores.'', 1;
        IF EXISTS (SELECT 1 FROM dbo.CombosDetalle GROUP BY EmpresaId,ComboProductoId,ProductoIncluidoId,ProductoPresentacionId HAVING COUNT(*)>1)
            THROW 51110, ''Composición histórica duplicada; concilie antes de aplicar Ajuste D.'', 1;
        IF EXISTS (SELECT 1 FROM dbo.CombosDetalle a JOIN dbo.CombosDetalle b ON a.EmpresaId=b.EmpresaId AND a.ProductoIncluidoId=b.ComboProductoId)
            THROW 51110, ''Existen composiciones anidadas/ciclos; Ajuste D admite solo componentes terminales.'', 1;
        IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N''dbo.CombosDetalle'') AND name=N''CK_CombosDetalle_Composicion'')
            ALTER TABLE dbo.CombosDetalle WITH CHECK ADD CONSTRAINT CK_CombosDetalle_Composicion CHECK(Cantidad>0 AND ComboProductoId<>ProductoIncluidoId);
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N''dbo.CombosDetalle'') AND name=N''FK_CombosDetalle_Presentacion'')
            ALTER TABLE dbo.CombosDetalle WITH CHECK ADD CONSTRAINT FK_CombosDetalle_Presentacion
                FOREIGN KEY(EmpresaId,ProductoIncluidoId,ProductoPresentacionId) REFERENCES dbo.ProductoPresentaciones(EmpresaId,ProductoId,Id);
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N''dbo.MovimientoDetalles'') AND name=N''FK_MovimientoDetalles_VentaDetalle'')
            ALTER TABLE dbo.MovimientoDetalles WITH CHECK ADD CONSTRAINT FK_MovimientoDetalles_VentaDetalle FOREIGN KEY(VentaDetalleId) REFERENCES dbo.VentasDetalle(Id);
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N''dbo.MovimientoDetalles'') AND name=N''FK_MovimientoDetalles_UnidadBase'')
            ALTER TABLE dbo.MovimientoDetalles WITH CHECK ADD CONSTRAINT FK_MovimientoDetalles_UnidadBase FOREIGN KEY(EmpresaId,UnidadBaseId) REFERENCES dbo.UnidadesMedida(EmpresaId,Id);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N''dbo.CombosDetalle'') AND name=N''UQ_CombosDetalle_Componente'')
            CREATE UNIQUE INDEX UQ_CombosDetalle_Componente ON dbo.CombosDetalle(EmpresaId,ComboProductoId,ProductoIncluidoId,ProductoPresentacionId);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N''dbo.MovimientoDetalles'') AND name=N''IX_MovimientoDetalles_VentaDetalle'')
            CREATE INDEX IX_MovimientoDetalles_VentaDetalle ON dbo.MovimientoDetalles(VentaDetalleId);
    ';

    DECLARE @tabla sysname, @nombre sysname, @claves nvarchar(200), @unico bit, @reales nvarchar(max);
    DECLARE indicesD CURSOR LOCAL FAST_FORWARD FOR SELECT * FROM (VALUES
        (N'CombosDetalle',N'UQ_CombosDetalle_Componente',N'EmpresaId,ComboProductoId,ProductoIncluidoId,ProductoPresentacionId',CONVERT(bit,1)),
        (N'MovimientoDetalles',N'IX_MovimientoDetalles_VentaDetalle',N'VentaDetalleId',CONVERT(bit,0))) v(tabla,nombre,claves,unico);
    OPEN indicesD; FETCH NEXT FROM indicesD INTO @tabla,@nombre,@claves,@unico;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SELECT @reales=STRING_AGG(CONVERT(nvarchar(max),c.name),N',') WITHIN GROUP(ORDER BY ic.key_ordinal)
        FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0
        JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
        WHERE i.object_id=OBJECT_ID(N'dbo.'+@tabla) AND i.name=@nombre;
        IF @reales IS NULL OR @reales<>@claves OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.'+@tabla)
            AND name=@nombre AND is_unique=@unico AND has_filter=0 AND is_disabled=0 AND ignore_dup_key=0)
            THROW 51110, 'Índice Ajuste D incompatible; no se reemplazará silenciosamente.', 1;
        FETCH NEXT FROM indicesD INTO @tabla,@nombre,@claves,@unico;
    END;
    CLOSE indicesD; DEALLOCATE indicesD;
    DECLARE @referenciada sysname, @relaciones nvarchar(300);
    DECLARE clavesD CURSOR LOCAL FAST_FORWARD FOR SELECT * FROM (VALUES
        (N'CombosDetalle',N'FK_CombosDetalle_Presentacion',N'ProductoPresentaciones',N'EmpresaId=EmpresaId,ProductoIncluidoId=ProductoId,ProductoPresentacionId=Id'),
        (N'MovimientoDetalles',N'FK_MovimientoDetalles_VentaDetalle',N'VentasDetalle',N'VentaDetalleId=Id'),
        (N'MovimientoDetalles',N'FK_MovimientoDetalles_UnidadBase',N'UnidadesMedida',N'EmpresaId=EmpresaId,UnidadBaseId=Id')) v(tabla,nombre,referenciada,relaciones);
    OPEN clavesD; FETCH NEXT FROM clavesD INTO @tabla,@nombre,@referenciada,@relaciones;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SELECT @reales=STRING_AGG(CONVERT(nvarchar(max),p.name+N'='+r.name),N',') WITHIN GROUP(ORDER BY fc.constraint_column_id)
        FROM sys.foreign_keys f JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=f.object_id
        JOIN sys.columns p ON p.object_id=fc.parent_object_id AND p.column_id=fc.parent_column_id
        JOIN sys.columns r ON r.object_id=fc.referenced_object_id AND r.column_id=fc.referenced_column_id
        WHERE f.parent_object_id=OBJECT_ID(N'dbo.'+@tabla) AND f.name=@nombre AND f.referenced_object_id=OBJECT_ID(N'dbo.'+@referenciada);
        IF @reales IS NULL OR @reales<>@relaciones THROW 51110, 'Clave foránea Ajuste D incompatible.', 1;
        FETCH NEXT FROM clavesD INTO @tabla,@nombre,@referenciada,@relaciones;
    END;
    CLOSE clavesD; DEALLOCATE clavesD;
    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name IN(N'FK_CombosDetalle_Presentacion',N'FK_MovimientoDetalles_VentaDetalle',N'FK_MovimientoDetalles_UnidadBase')
        AND (is_disabled=1 OR is_not_trusted=1 OR delete_referential_action<>0 OR update_referential_action<>0))
        THROW 51110, 'Restricción Ajuste D deshabilitada o incompatible.', 1;
    DECLARE @check nvarchar(max)=(SELECT definition FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.CombosDetalle') AND name=N'CK_CombosDetalle_Composicion' AND is_disabled=0 AND is_not_trusted=0);
    SET @check=REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@check,N'[',N''),N']',N''),N'(',N''),N')',N''),N' ',N'');
    IF @check IS NULL OR @check<>N'Cantidad>0ANDComboProductoId<>ProductoIncluidoId'
        THROW 51110, 'Restricción de composición incompatible.', 1;
    COMMIT TRANSACTION;
    PRINT N'AJUSTE D: composición de venta y trazabilidad listas. Sin datos demo ni cambios financieros.';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
