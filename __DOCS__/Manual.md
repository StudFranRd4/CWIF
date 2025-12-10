# CWIF - Manual de usuario

## 1. Pantalla principal

Al iniciar la aplicación se muestra el **Dashboard de tareas** en el panel central. Desde aquí pueden verse métricas, estados de tareas y accesos rápidos. 

En la parte superior está el **menú principal**

En la parte inferior se encuentra una barra de accesos directos a **Proyectos**, **Tareas** y **Reportes**.

---

## 2. Menú principal


### 2.1 Consultas

Permite consultar datos sin modificarlos. Cada opción abre una ventana de solo lectura con filtros básicos.

* **Clientes**
* **Compras máster**
* **Ventas máster**
* **Productos**
* **Inventario**
* **Proveedores**

---

### 2.2 Mantenimientos

Opciones CRUD para administrar datos del sistema.

* **Clientes**
* **Proveedores**
* **Compras máster** (abre formulario especializado para compras)

Cada pantalla permite crear, editar, eliminar y refrescar registros.

---

### 2.3 Inventario

Herramientas para gestionar existencias.

* **Productos**
* **Categorías**
* **Control de inventario** (muestra movimientos e información detallada mediante un formulario dedicado)

---

### 2.4 Facturación

Opciones relacionadas al proceso de venta.

* **Impuestos**
* **Formas de pago**
* **Facturar** (abre el formulario completo para emitir una factura)

---

### 2.5 Usuarios

Abre el administrador general de usuarios (CRUD dinámico).

---

### 2.6 Salir

Permite cerrar la aplicación, con confirmación.

---

## 3. Barra inferior (accesos rápidos)

* **Proyectos**
  Abre el administrador de proyectos en ventana independiente.

* **Tareas**
  Muestra la lista de tareas.

* **Reportes**
  Acceso reservado para futuras funcionalidades.

---

## 4. CRUD (funcionamiento general)

* **Nuevo**: abre un editor dinámico basado en anotaciones del modelo.
* **Editar**: requiere seleccionar una fila.
* **Eliminar**: pide confirmación antes de borrar.
* **Refrescar**: recarga los datos desde la base.

---

## 5. Base de datos

Se usa **SQLite**; la base se genera automáticamente en: `%LOCALAPPDATA%/FacturaApp/facturaapp.db`

Si desea replicarla manualmente, utilice el archivo: `/sql/schema_seed.sql`

---

## 6. Logging

Se genera archivo `facturaapp.log` en el directorio de ejecución.
También se escribe información en la consola.

---

## 7. Requisitos

* Windows 10/11
* .NET 9 SDK

---

## 8. Despliegue

Opción típica de publicación:

```
dotnet publish -c Release -r win-x64 --self-contained false
```

Distribuya la carpeta **publish** resultante en la máquina destino.

Si ejecuta desde código fuente, puede compilar con Visual Studio o usando:

```
dotnet restore
dotnet run --project FacturaApp.WinForms
```

---