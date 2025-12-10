-- schema_seed.sql
-- Script de creación y datos de ejemplo para FacturaApp (SQLite)
BEGIN TRANSACTION;

CREATE TABLE IF NOT EXISTS Empresas (
    Id TEXT PRIMARY KEY,
    Nombre TEXT NOT NULL,
    Rnc TEXT,
    Direccion TEXT
);

CREATE TABLE IF NOT EXISTS Sucursales (
    Id TEXT PRIMARY KEY,
    Nombre TEXT NOT NULL,
    Direccion TEXT,
    EmpresaId TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Usuarios (
    Id TEXT PRIMARY KEY,
    Nombre TEXT NOT NULL,
    Username TEXT NOT NULL,
    Email TEXT
);

CREATE TABLE IF NOT EXISTS Categorias (
    Id TEXT PRIMARY KEY,
    Nombre TEXT NOT NULL,
    Descripcion TEXT
);

CREATE TABLE IF NOT EXISTS Productos (
    Id TEXT PRIMARY KEY,
    Nombre TEXT NOT NULL,
    Codigo TEXT,
    Precio REAL NOT NULL,
    Stock INTEGER,
    CategoriaId TEXT
);

CREATE TABLE IF NOT EXISTS Clientes (
    Id TEXT PRIMARY KEY,
    Nombre TEXT NOT NULL,
    Telefono TEXT,
    Direccion TEXT
);

CREATE TABLE IF NOT EXISTS FormasPago (
    Id TEXT PRIMARY KEY,
    Nombre TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Proyecto (
            Id TEXT PRIMARY KEY NOT NULL,
            RowVersion INTEGER,
            Nombre TEXT NOT NULL,
            Descripcion TEXT,
            FechaInicio TEXT NOT NULL,
            FechaFin TEXT
        );
		
CREATE TABLE IF NOT EXISTS ProyectoUsuario (
            Id TEXT PRIMARY KEY NOT NULL,
            RowVersion INTEGER,
            ProyectoId TEXT NOT NULL,
            UsuarioId TEXT NOT NULL,
            FechaAsignacion TEXT NOT NULL,
            FOREIGN KEY (ProyectoId) REFERENCES Proyecto(Id) ON DELETE CASCADE,
            FOREIGN KEY (UsuarioId) REFERENCES Usuario(Id) ON DELETE CASCADE
        );

CREATE TABLE IF NOT EXISTS TareaProyecto (
            Id TEXT PRIMARY KEY NOT NULL,
            RowVersion INTEGER,
            ProyectoId TEXT NOT NULL,
            Titulo TEXT NOT NULL,
            Descripcion TEXT,
            UsuarioId TEXT NOT NULL,
            Estado TEXT NOT NULL,
            Prioridad TEXT NOT NULL,
            FechaCreacion TEXT NOT NULL,
            FechaCierre TEXT,
            FOREIGN KEY (ProyectoId) REFERENCES Proyecto(Id) ON DELETE CASCADE,
            FOREIGN KEY (UsuarioId) REFERENCES Usuario(Id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS TareaComentario (
            Id TEXT PRIMARY KEY NOT NULL,
            RowVersion INTEGER,
            TareaId TEXT NOT NULL,
            UsuarioId TEXT NOT NULL,
            Texto TEXT NOT NULL,
            Fecha TEXT NOT NULL,
            FOREIGN KEY (TareaId) REFERENCES TareaProyecto(Id) ON DELETE CASCADE,
            FOREIGN KEY (UsuarioId) REFERENCES Usuario(Id) ON DELETE CASCADE
        );

-- Seed data

INSERT OR IGNORE INTO Empresas (Id, Nombre, Rnc, Direccion) VALUES ('11111111-1111-1111-1111-111111111111', 'Tienda Demo', '131313131', 'Calle Principal 123');
INSERT OR IGNORE INTO Sucursales (Id, Nombre, Direccion, EmpresaId) VALUES ('22222222-2222-2222-2222-222222222222', 'Sucursal Principal', 'C. Principal', '11111111-1111-1111-1111-111111111111');
INSERT OR IGNORE INTO Categorias (Id, Nombre, Descripcion) VALUES ('33333333-3333-3333-3333-333333333333', 'General', 'Productos generales');
INSERT OR IGNORE INTO Productos (Id, Nombre, Codigo, Precio, Stock, CategoriaId) VALUES ('44444444-4444-4444-4444-444444444444', 'Camiseta', 'CAM001', 15.50, 100, '33333333-3333-3333-3333-333333333333');
INSERT OR IGNORE INTO Productos (Id, Nombre, Codigo, Precio, Stock, CategoriaId) VALUES ('55555555-5555-5555-5555-555555555555', 'Pantalon', 'PAN001', 30.00, 50, '33333333-3333-3333-3333-333333333333');
INSERT OR IGNORE INTO Clientes (Id, Nombre) VALUES ('66666666-6666-6666-6666-666666666666', 'Consumidor Final');
INSERT OR IGNORE INTO FormasPago (Id, Nombre) VALUES ('77777777-7777-7777-7777-777777777777', 'Efectivo');
INSERT OR IGNORE INTO FormasPago (Id, Nombre) VALUES ('88888888-8888-8888-8888-888888888888', 'Tarjeta');

COMMIT;
