-- ===========================================
-- MÓDULO SEGURIDAD & AUTENTICACIÓN
-- ===========================================
CREATE TABLE Usuario (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    Contrasena NVARCHAR(255) NOT NULL,
    RutaAvatar NVARCHAR(MAX) NULL,
    Activo BIT NOT NULL DEFAULT 1
);

CREATE TABLE rol (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(100) UNIQUE NOT NULL
);

CREATE TABLE ruta (
    ruta VARCHAR(100) PRIMARY KEY,
    descripcion VARCHAR(255) NOT NULL
);

CREATE TABLE rol_usuario (
    fkemail NVARCHAR(150) NOT NULL REFERENCES Usuario (Email) ON UPDATE CASCADE ON DELETE CASCADE,
    fkidrol INT NOT NULL REFERENCES rol (id),
    PRIMARY KEY (fkemail, fkidrol)
);

CREATE TABLE rutarol (
    ruta VARCHAR(100) NOT NULL REFERENCES ruta (ruta) ON UPDATE CASCADE ON DELETE CASCADE,
    rol VARCHAR(100) NOT NULL REFERENCES rol (nombre) ON UPDATE CASCADE ON DELETE CASCADE,
    PRIMARY KEY (ruta, rol)
);

CREATE TABLE Auditoria (
    Id BIGINT PRIMARY KEY IDENTITY(1,1),
    TablaAfectada NVARCHAR(100) NOT NULL,
    Accion NVARCHAR(10) NOT NULL,
    RegistroId INT NOT NULL,
    DatosAnteriores NVARCHAR(MAX),
    DatosNuevos NVARCHAR(MAX),
    UsuarioId NVARCHAR(100),
    FechaAuditoria DATETIME2 DEFAULT GETDATE(),
    IpAddress NVARCHAR(50),
    UserAgent NVARCHAR(500)
);

CREATE INDEX IX_Auditoria_TablaAfectada ON Auditoria(TablaAfectada);
CREATE INDEX IX_Auditoria_Fecha ON Auditoria(FechaAuditoria);
CREATE INDEX IX_Auditoria_RegistroId ON Auditoria(RegistroId, TablaAfectada);

-- ===========================================
-- MÓDULO GESTIÓN HUMANA
-- ===========================================

CREATE TABLE TipoResponsable (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Titulo NVARCHAR(50) NOT NULL,
    Descripcion NVARCHAR(255) NOT NULL,
    CONSTRAINT UQ_TipoResponsable_Titulo UNIQUE (Titulo)
);

CREATE TABLE Responsable (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IdTipoResponsable INT NOT NULL,
    IdUsuario INT NOT NULL,
    Nombre NVARCHAR(255) NOT NULL,
    CONSTRAINT FK_Responsable_TipoResponsable FOREIGN KEY (IdTipoResponsable) REFERENCES TipoResponsable(Id),
    CONSTRAINT FK_Responsable_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario(Id) ON DELETE CASCADE
);

-- ===========================================
-- MÓDULO PROYECTOS
-- ===========================================

CREATE TABLE TipoProyecto (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(150) NOT NULL,
    Descripcion NVARCHAR(255) NOT NULL,
    CONSTRAINT UQ_TipoProyecto_Nombre UNIQUE (Nombre)
);

CREATE TABLE Estado (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL,
    Descripcion NVARCHAR(255) NOT NULL,
    CONSTRAINT UQ_Estado_Nombre UNIQUE (Nombre)
);

CREATE TABLE Proyecto (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IdProyectoPadre INT NULL,
    IdResponsable INT NOT NULL,
    IdTipoProyecto INT NOT NULL,
    Codigo NVARCHAR(50) NULL,
    Titulo NVARCHAR(255) NOT NULL,
    Descripcion NVARCHAR(MAX) NULL,
    FechaInicio DATE NULL,
    FechaFinPrevista DATE NULL,
    FechaModificacion DATE NULL,
    FechaFinalizacion DATE NULL,
    RutaLogo NVARCHAR(MAX) NULL,
    CONSTRAINT FK_Proyecto_ProyectoPadre FOREIGN KEY (IdProyectoPadre) REFERENCES Proyecto(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Proyecto_Responsable FOREIGN KEY (IdResponsable) REFERENCES Responsable(Id),
    CONSTRAINT FK_Proyecto_TipoProyecto FOREIGN KEY (IdTipoProyecto) REFERENCES TipoProyecto(Id)
);

CREATE TABLE Estado_Proyecto (
    IdProyecto INT PRIMARY KEY,
    IdEstado INT NOT NULL,
    CONSTRAINT FK_EstadoProyecto_Proyecto FOREIGN KEY (IdProyecto) REFERENCES Proyecto(Id) ON DELETE CASCADE,
    CONSTRAINT FK_EstadoProyecto_Estado FOREIGN KEY (IdEstado) REFERENCES Estado(Id)
);

-- ===========================================
-- MÓDULO PRODUCTOS
-- ===========================================

CREATE TABLE TipoProducto (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(150) NOT NULL,
    Descripcion NVARCHAR(255) NOT NULL,
    CONSTRAINT UQ_TipoProducto_Nombre UNIQUE (Nombre)
);

CREATE TABLE Producto (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IdTipoProducto INT NOT NULL,
    Codigo NVARCHAR(50) NULL,
    Titulo NVARCHAR(255) NOT NULL,
    Descripcion NVARCHAR(MAX) NULL,
    FechaInicio DATE NULL,
    FechaFinPrevista DATE NULL,
    FechaModificacion DATE NULL,
    FechaFinalizacion DATE NULL,
    RutaLogo NVARCHAR(MAX) NULL,
    CONSTRAINT FK_Producto_TipoProducto FOREIGN KEY (IdTipoProducto) REFERENCES TipoProducto(Id)
);

CREATE TABLE Proyecto_Producto (
    IdProyecto INT NOT NULL,
    IdProducto INT NOT NULL,
    FechaAsociacion DATE NULL,
    PRIMARY KEY (IdProyecto, IdProducto),
    CONSTRAINT FK_ProyectoProducto_Proyecto FOREIGN KEY (IdProyecto) REFERENCES Proyecto(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ProyectoProducto_Producto FOREIGN KEY (IdProducto) REFERENCES Producto(Id) ON DELETE CASCADE
);

-- ===========================================
-- MÓDULO ENTREGABLES
-- ===========================================

CREATE TABLE Entregable (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Codigo NVARCHAR(50) NULL,
    Titulo NVARCHAR(255) NOT NULL,
    Descripcion NVARCHAR(MAX) NULL,
    FechaInicio DATE NULL,
    FechaFinPrevista DATE NULL,
    FechaModificacion DATE NULL,
    FechaFinalizacion DATE NULL
);

CREATE TABLE Producto_Entregable (
    IdProducto INT NOT NULL,
    IdEntregable INT NOT NULL,
    FechaAsociacion DATE NULL,
    PRIMARY KEY (IdProducto, IdEntregable),
    CONSTRAINT FK_ProductoEntregable_Producto FOREIGN KEY (IdProducto) REFERENCES Producto(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ProductoEntregable_Entregable FOREIGN KEY (IdEntregable) REFERENCES Entregable(Id) ON DELETE CASCADE
);

CREATE TABLE Responsable_Entregable (
    IdResponsable INT NOT NULL,
    IdEntregable INT NOT NULL,
    FechaAsociacion DATE NULL,
    PRIMARY KEY (IdResponsable, IdEntregable),
    CONSTRAINT FK_ResponsableEntregable_Responsable FOREIGN KEY (IdResponsable) REFERENCES Responsable(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ResponsableEntregable_Entregable FOREIGN KEY (IdEntregable) REFERENCES Entregable(Id) ON DELETE CASCADE
);

-- ===========================================
-- MÓDULO ARCHIVOS
-- ===========================================

CREATE TABLE Archivo (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NOT NULL,
    Ruta NVARCHAR(MAX) NOT NULL,
    Nombre NVARCHAR(255) NOT NULL,
    Tipo NVARCHAR(50) NULL,
    Fecha DATE NULL,
    CONSTRAINT FK_Archivo_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario(Id) ON DELETE CASCADE
);

CREATE TABLE Archivo_Entregable (
    IdArchivo INT NOT NULL,
    IdEntregable INT NOT NULL,
    PRIMARY KEY (IdArchivo, IdEntregable),
    CONSTRAINT FK_ArchivoEntregable_Archivo FOREIGN KEY (IdArchivo) REFERENCES Archivo(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ArchivoEntregable_Entregable FOREIGN KEY (IdEntregable) REFERENCES Entregable(Id) ON DELETE CASCADE
);

-- ===========================================
-- MÓDULO ACTIVIDADES
-- ===========================================

CREATE TABLE Actividad (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IdEntregable INT NOT NULL,
    Titulo NVARCHAR(255) NOT NULL,
    Descripcion NVARCHAR(MAX) NULL,
    FechaInicio DATE NULL,
    FechaFinPrevista DATE NULL,
    FechaModificacion DATE NULL,
    FechaFinalizacion DATE NULL,
    Prioridad INT NULL,
    PorcentajeAvance INT CHECK (PorcentajeAvance BETWEEN 0 AND 100),
    CONSTRAINT FK_Actividad_Entregable FOREIGN KEY (IdEntregable) REFERENCES Entregable(Id) ON DELETE CASCADE
);

-- ===========================================
-- MÓDULO PRESUPUESTOS
-- ===========================================

CREATE TABLE Presupuesto (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IdProyecto INT NOT NULL,
    MontoSolicitado DECIMAL(15,2) NOT NULL,
    Estado NVARCHAR(20) NOT NULL DEFAULT 'Pendiente' CHECK (Estado IN ('Pendiente','Aprobado','Rechazado')),
    MontoAprobado DECIMAL(15,2) NULL,
    PeriodoAnio INT NULL,
    FechaSolicitud DATE NULL,
    FechaAprobacion DATE NULL,
    Observaciones NVARCHAR(MAX) NULL,
    CONSTRAINT FK_Presupuesto_Proyecto FOREIGN KEY (IdProyecto) REFERENCES Proyecto(Id) ON DELETE CASCADE
);

CREATE TABLE DistribucionPresupuesto (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IdPresupuestoPadre INT NOT NULL,
    IdProyectoHijo INT NOT NULL,
    MontoAsignado DECIMAL(15,2) NOT NULL,
    CONSTRAINT FK_Distribucion_Presupuesto FOREIGN KEY (IdPresupuestoPadre) REFERENCES Presupuesto(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Distribucion_Proyecto FOREIGN KEY (IdProyectoHijo) REFERENCES Proyecto(Id) ON DELETE NO ACTION
);

CREATE TABLE EjecucionPresupuesto (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IdPresupuesto INT NOT NULL,
    Anio INT NOT NULL,
    MontoPlaneado DECIMAL(15,2) NULL,
    MontoEjecutado DECIMAL(15,2) NULL,
    Observaciones NVARCHAR(MAX) NULL,
    CONSTRAINT FK_Ejecucion_Presupuesto FOREIGN KEY (IdPresupuesto) REFERENCES Presupuesto(Id) ON DELETE CASCADE
);

-- ===========================================
-- MÓDULO ESTRATEGIA
-- ===========================================

CREATE TABLE VariableEstrategica (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Titulo NVARCHAR(255) NOT NULL,
    Descripcion NVARCHAR(MAX) NULL
);

CREATE TABLE ObjetivoEstrategico (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IdVariable INT NOT NULL,
    Titulo NVARCHAR(255) NOT NULL,
    Descripcion NVARCHAR(MAX) NULL,
    CONSTRAINT FK_ObjetivoEstrategico_Variable FOREIGN KEY (IdVariable) REFERENCES VariableEstrategica(Id) ON DELETE CASCADE
);

CREATE TABLE MetaEstrategica (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IdObjetivo INT NOT NULL,
    Titulo NVARCHAR(255) NOT NULL,
    Descripcion NVARCHAR(MAX) NULL,
    CONSTRAINT FK_MetaEstrategica_Objetivo FOREIGN KEY (IdObjetivo) REFERENCES ObjetivoEstrategico(Id) ON DELETE CASCADE
);

CREATE TABLE Meta_Proyecto (
    IdMeta INT NOT NULL,
    IdProyecto INT NOT NULL,
    FechaAsociacion DATE NULL,
    PRIMARY KEY (IdMeta, IdProyecto),
    CONSTRAINT FK_MetaProyecto_Meta FOREIGN KEY (IdMeta) REFERENCES MetaEstrategica(Id) ON DELETE CASCADE,
    CONSTRAINT FK_MetaProyecto_Proyecto FOREIGN KEY (IdProyecto) REFERENCES Proyecto(Id) ON DELETE CASCADE
);

-- ===========================================
-- DATOS INICIALES - SEGURIDAD
-- ===========================================

INSERT INTO rol (nombre) VALUES ('Administrador'),('Vendedor'),('Cajero'),('Contador'),('Cliente');

INSERT INTO ruta (ruta, descripcion) VALUES
('/home', 'Página principal - Dashboard'),
('/usuarios', 'Gestión de usuarios'),
('/roles', 'Gestión de roles'),
('/permisos', 'Gestión de permisos (asignación rol-ruta)'),
('/permisos/crear', 'Crear permiso (POST)'),
('/permisos/eliminar', 'Eliminar permiso (POST)'),
('/rutas', 'Gestión de rutas del sistema'),
('/rutas/crear', 'Crear ruta (POST)'),
('/rutas/eliminar', 'Eliminar ruta (POST)'),
('/entregables', 'Gestión de entregables'),
('/ejecucionpresupuestos', 'Gestión de ejecución de presupuestos'),
('/auditorias', 'Consulta de auditorías del sistema');

INSERT INTO rutarol (ruta, rol) VALUES 
('/home', 'Administrador'),
('/usuarios', 'Administrador'),
('/roles', 'Administrador'),
('/permisos', 'Administrador'),
('/permisos/crear', 'Administrador'),
('/permisos/eliminar', 'Administrador'),
('/rutas', 'Administrador'),
('/rutas/crear', 'Administrador'),
('/rutas/eliminar', 'Administrador'),
('/entregables', 'Administrador'),
('/ejecucionpresupuestos', 'Administrador'),
('/auditorias', 'Administrador');

-- ===========================================
-- STORED PROCEDURES - SEGURIDAD
-- ===========================================

CREATE OR ALTER PROCEDURE crear_usuario_con_roles
    @p_email VARCHAR(100),
    @p_contrasena VARCHAR(100),
    @p_rutaavatar NVARCHAR(MAX) = NULL,
    @p_activo BIT = 1,
    @p_roles NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ErrorMessage NVARCHAR(4000), @ErrorSeverity INT, @ErrorState INT;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        IF EXISTS (SELECT 1 FROM usuario WHERE email = @p_email)
            THROW 50005, 'El usuario ya existe', 1;
        
        INSERT INTO usuario (email, contrasena, RutaAvatar, Activo) 
        VALUES (@p_email, @p_contrasena, @p_rutaavatar, @p_activo);
        
        INSERT INTO rol_usuario (fkemail, fkidrol)
        SELECT @p_email, fkidrol
        FROM OPENJSON(@p_roles) WITH (fkidrol INT '$.fkidrol');
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT @ErrorMessage = ERROR_MESSAGE(), @ErrorSeverity = ERROR_SEVERITY(), @ErrorState = ERROR_STATE();
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE actualizar_usuario_con_roles
    @p_email VARCHAR(100),
    @p_contrasena VARCHAR(100) = NULL,
    @p_rutaavatar NVARCHAR(MAX) = NULL,
    @p_activo BIT = NULL,
    @p_roles NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ErrorMessage NVARCHAR(4000), @ErrorSeverity INT, @ErrorState INT;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        IF NOT EXISTS (SELECT 1 FROM usuario WHERE email = @p_email)
            THROW 50006, 'El usuario no existe', 1;
        
        UPDATE usuario 
        SET 
            contrasena = CASE WHEN @p_contrasena IS NOT NULL THEN @p_contrasena ELSE contrasena END,
            RutaAvatar = CASE WHEN @p_rutaavatar IS NOT NULL THEN @p_rutaavatar ELSE RutaAvatar END,
            Activo = CASE WHEN @p_activo IS NOT NULL THEN @p_activo ELSE Activo END
        WHERE email = @p_email;
        
        DELETE FROM rol_usuario WHERE fkemail = @p_email;
        
        INSERT INTO rol_usuario (fkemail, fkidrol)
        SELECT @p_email, fkidrol
        FROM OPENJSON(@p_roles) WITH (fkidrol INT '$.fkidrol');
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT @ErrorMessage = ERROR_MESSAGE(), @ErrorSeverity = ERROR_SEVERITY(), @ErrorState = ERROR_STATE();
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE eliminar_usuario_con_roles
    @p_email VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ErrorMessage NVARCHAR(4000), @ErrorSeverity INT, @ErrorState INT;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        IF NOT EXISTS (SELECT 1 FROM usuario WHERE email = @p_email)
            THROW 50006, 'El usuario no existe', 1;
        
        DELETE FROM rol_usuario WHERE fkemail = @p_email;
        DELETE FROM usuario WHERE email = @p_email;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT @ErrorMessage = ERROR_MESSAGE(), @ErrorSeverity = ERROR_SEVERITY(), @ErrorState = ERROR_STATE();
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

-- ===========================================
-- STORED PROCEDURES - ROLES Y PERMISOS
-- ===========================================

CREATE OR ALTER PROCEDURE crear_rol
    @p_nombre NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ErrorMessage NVARCHAR(4000), @ErrorSeverity INT, @ErrorState INT;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        IF EXISTS (SELECT 1 FROM rol WHERE nombre = @p_nombre)
            THROW 50007, 'El rol ya existe', 1;
        
        INSERT INTO rol (nombre) VALUES (@p_nombre);
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT @ErrorMessage = ERROR_MESSAGE(), @ErrorSeverity = ERROR_SEVERITY(), @ErrorState = ERROR_STATE();
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE actualizar_rol
    @p_id INT,
    @p_nombre NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ErrorMessage NVARCHAR(4000), @ErrorSeverity INT, @ErrorState INT;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        IF NOT EXISTS (SELECT 1 FROM rol WHERE id = @p_id)
            THROW 50008, 'El rol no existe', 1;
            
        IF EXISTS (SELECT 1 FROM rol WHERE nombre = @p_nombre AND id != @p_id)
            THROW 50009, 'Ya existe otro rol con ese nombre', 1;
        
        UPDATE rol SET nombre = @p_nombre WHERE id = @p_id;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT @ErrorMessage = ERROR_MESSAGE(), @ErrorSeverity = ERROR_SEVERITY(), @ErrorState = ERROR_STATE();
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE eliminar_rol
    @p_id INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ErrorMessage NVARCHAR(4000), @ErrorSeverity INT, @ErrorState INT;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        IF NOT EXISTS (SELECT 1 FROM rol WHERE id = @p_id)
            THROW 50008, 'El rol no existe', 1;
            
        IF EXISTS (SELECT 1 FROM rol_usuario WHERE fkidrol = @p_id)
            THROW 50010, 'No se puede eliminar el rol porque está asignado a usuarios', 1;
        
        DELETE FROM rol WHERE id = @p_id;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT @ErrorMessage = ERROR_MESSAGE(), @ErrorSeverity = ERROR_SEVERITY(), @ErrorState = ERROR_STATE();
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE listar_rutarol
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ruta, rol FROM rutarol ORDER BY ruta, rol;
END;
GO

CREATE OR ALTER PROCEDURE crear_rutarol
    @p_ruta VARCHAR(100),
    @p_rol VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM rol WHERE nombre = @p_rol)
    BEGIN
        SELECT 0 AS success, 'El rol especificado no existe' AS message FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
        RETURN;
    END
    
    IF EXISTS (SELECT 1 FROM rutarol WHERE ruta = @p_ruta AND rol = @p_rol)
    BEGIN
        SELECT 0 AS success, 'El permiso ya existe' AS message FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
        RETURN;
    END
    
    INSERT INTO rutarol (ruta, rol) VALUES (@p_ruta, @p_rol);
    SELECT 1 AS success, 'Permiso creado exitosamente' AS message FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
END;
GO

CREATE OR ALTER PROCEDURE eliminar_rutarol
    @p_ruta VARCHAR(100),
    @p_rol VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM rutarol WHERE ruta = @p_ruta AND rol = @p_rol)
    BEGIN
        SELECT 0 AS success, 'El permiso no existe' AS message FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
        RETURN;
    END
    
    DELETE FROM rutarol WHERE ruta = @p_ruta AND rol = @p_rol;
    SELECT 1 AS success, 'Permiso eliminado exitosamente' AS message FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
END;
GO

-- ===========================================
-- STORED PROCEDURES PARA LISTAR USUARIOS
-- ===========================================

CREATE OR ALTER FUNCTION listar_usuarios_con_roles()
RETURNS NVARCHAR(MAX)
AS
BEGIN
    DECLARE @resultado NVARCHAR(MAX);
    
    -- FORMA CORRECTA: Usar SELECT con INTO en una subconsulta
    SELECT @resultado = (
        SELECT 
            u.email,
            u.RutaAvatar,
            u.Activo,
            (
                SELECT r.id AS idrol, r.nombre
                FROM rol_usuario ru
                INNER JOIN rol r ON r.id = ru.fkidrol
                WHERE ru.fkemail = u.email
                FOR JSON PATH
            ) AS roles
        FROM usuario u
        FOR JSON PATH
    );
    
    RETURN ISNULL(@resultado, '[]');
END;
GO
CREATE PROCEDURE sp_listar_usuarios_con_roles
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @jsonResult NVARCHAR(MAX);
    
    SELECT @jsonResult = (
        SELECT 
            u.Id,
            u.email AS Email,
            u.RutaAvatar,
            u.Activo,
            (
                SELECT r.id AS IdRol, r.nombre AS Nombre
                FROM rol_usuario ru
                INNER JOIN rol r ON r.id = ru.fkidrol
                WHERE ru.fkemail = u.email
                FOR JSON PATH
            ) AS Roles
        FROM usuario u
        FOR JSON PATH
    );
    
    SELECT ISNULL(@jsonResult, '[]') AS Resultado;
END;
GO
CREATE OR ALTER PROCEDURE sp_listar_usuarios_con_roles
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @jsonResult NVARCHAR(MAX);
    
    SELECT @jsonResult = (
        SELECT 
            u.Id,
            u.email AS Email,
            u.RutaAvatar,
            u.Activo,
            (
                SELECT r.id AS IdRol, r.nombre AS Nombre
                FROM rol_usuario ru
                INNER JOIN rol r ON r.id = ru.fkidrol
                WHERE ru.fkemail = u.email
                FOR JSON PATH
            ) AS Roles
        FROM usuario u
        FOR JSON PATH  -- ✅ Esta línea estaba causando el error
    );  -- ✅ Este paréntesis cierra el SELECT principal
    
    SELECT ISNULL(@jsonResult, '[]') AS Resultado;
END;
GO
    
    SELECT ISNULL(@jsonResult, '[]') AS Resultado;
END;
GO
    
    -- Retornar con el nombre de columna que tu aplicación espera
    SELECT ISNULL(@jsonResult, '[]') AS Resultado;
END;
GO


CREATE OR ALTER FUNCTION verificar_acceso_ruta(
    @p_email VARCHAR(100),
    @p_ruta VARCHAR(100)
)
RETURNS NVARCHAR(MAX)
AS
BEGIN
    DECLARE @v_tiene_acceso BIT = 0;
    DECLARE @resultado NVARCHAR(MAX);
    
    IF EXISTS (
        SELECT 1
        FROM usuario u
        INNER JOIN rol_usuario ur ON u.email = ur.fkemail
        INNER JOIN rol r ON ur.fkidrol = r.id
        INNER JOIN rutarol rr ON r.nombre = rr.rol
        WHERE u.email = @p_email AND rr.ruta = @p_ruta
    )
        SET @v_tiene_acceso = 1;
    
    SELECT @resultado = (
        SELECT @v_tiene_acceso AS tiene_acceso, @p_email AS email, @p_ruta AS ruta
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );
    
    RETURN @resultado;
END;
GO

-- ===========================================
-- USUARIO ADMIN POR DEFECTO
-- ===========================================

EXEC crear_usuario_con_roles 
    @p_email = 'admin@correo.com', 
    @p_contrasena = 'admin123', 
    @p_roles = '[{"fkidrol":1}]';

-- ===========================================
-- PRUEBA FINAL
-- ===========================================

-- Probar que todo funciona
EXEC sp_listar_usuarios_con_roles;
