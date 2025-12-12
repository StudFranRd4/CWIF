### CWIF: sistema de gestión comercial

## Estructura:

- FacturaApp.Core : Modelos de datos
- FacturaApp.Data : DbContext y configuración SQLite
- FacturaApp.Services : Repositorio genérico con CRUD
- FacturaApp.WinForms : UI Windows Forms con CRUD genérico

## Requisitos:

- .NET 9 SDK instalado
- Plataforma Windows para ejecutar la aplicación de escritorio

## Instrucciones rápidas:

1) Abrir una terminal en la carpeta /FacturaApp
2) Restaurar paquetes: dotnet restore
3) Ejecutar: dotnet run --project FacturaApp.WinForms

La aplicación crea la base de datos SQLite en la carpeta %LOCALAPPDATA%/FacturaApp/facturaapp.db y la inicializa con datos de ejemplo.

## Notas:

- Se usó EnsureCreated() y seed. También incluí un script SQL de creación/seed en /sql/schema_seed.sql
- Logging: consola + archivo facturaapp.log en la carpeta donde se ejecuta la app
- Manejo global de errores: capturado y mostrado con MessageBox
