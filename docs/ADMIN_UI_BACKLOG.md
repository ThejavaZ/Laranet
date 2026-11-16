# Admin UI Backlog

This file tracks the remaining UI and functionality improvements for the Laranet
admin panel. Each item should be implemented and verified independently.

## Componentización

- [x] **Shell dividido en layouts.** `Layouts/_MainLayout.cshtml` y `Layouts/_AuthLayout.cshtml`.
- [x] **Componentes con convenio `_Nombre.cshtml`.** En `Pages/Components`.
- [x] **Topbar, sidebar, toast y dialog como partials.** Navegación dinámica, estado activo y `aria-current="page"`.
- [x] **Badge de entorno desde `IWebHostEnvironment`.** Sin texto hardcodeado.
- [x] **Login/logout opcional por descubrimiento de endpoints.** Usa Identity si está registrado; no lo impone.
- [x] **Formularios con `_Field` y `_Select`.** Tipos HTML, `step`, `required`, `min/max` y checkbox reflejando el valor actual.
- [x] **Tabla y paginación como componentes.** `data-label` en las celdas para móvil y estado de lista preservado.
- [x] **Empty state y botones reutilizables.** `_EmptyState` y `_Button`.

## Tablas

- [x] **Responsive table cards with visible labels.** En móvil (≤760px), cada fila es una tarjeta con etiquetas visibles. En tablet (761–1023px), tabla con scroll horizontal dentro de su contenedor. En desktop (≥1024px), tabla normal. Usar un solo mecanismo. Eliminar reglas duplicadas de ≤767px y ≤760px. El thead oculto usa `.sr-only`.
- [x] **Zebra striping y hover visible.** Filas alternas con color sutil. Hover con transición.
- [x] **Indicador de ordenación actual.** Flecha ↑/↓ visible y `aria-sort` en el header.
- [x] **Badges de estado para booleanos.** Active/Inactive con color.
- [x] **Acciones con iconos.** Edit/Delete con iconos SVG y tooltip, no texto.
- [x] **Checkbox "seleccionar todo"** en el header.
- [x] **Total no repetido.** Eliminar "results" duplicado.
- [x] **Loading skeletons.** Para tablas y cards.
- [x] **Empty states con CTA prominente.** Ilustración SVG + botón.

## Formularios

- [x] **Validación en servidor por tipo.** `decimal.TryParse`, `int.TryParse`, `DateTime.TryParse` con `ModelState` y mensajes de error. Nunca convertir a 0 en silencio.
- [x] **Validación en cliente.** `type="number"` con `step`, `required`, `min`, `max`, `maxlength`. Estados de error visibles (borde rojo y mensaje).
- [x] **Checkbox booleano refleja valor actual.** `checked` attribute en Edit.
- [x] **Estados de error visibles.** Borde rojo y mensaje debajo del campo.
- [x] **Secciones/fieldsets** en formularios largos.
- [x] **Indicador de campo requerido** (`*`).

## Paginación y navegación

- [x] **Paginación real.** Botones First/Prev/Next/Last + números de página.
- [x] **Preservar estado en sort/filtros/page size.** No perder filtros al ordenar o cambiar tamaño.
- [x] **Breadcrumbs.** Mostrar ruta actual.
- [x] **Estado activo en sidebar.** `aria-current="page"`.

## Feedback visual

- [x] **Toasts con tipos.** Success, error, warning, info con colores distintos.
- [x] **Modal con Escape y foco atrapado.** Cerrar con Escape, trap de foco.
- [x] **Loading skeletons.** Para tablas y cards.

## Seguridad

- [x] **Validar `SortBy` contra lista blanca.** Prevenir inyección.
- [x] **Limitar `PageSize` y `PageNumber`.** Clamped a rangos válidos.
- [x] **Challenge/Forbid en vez de 404.** 401 para anónimos, 403 para no autorizados.
- [x] **Export CSV con BOM.** UTF-8 con BOM para Excel.
- [x] **Bulk delete sin selección.** Validar y mostrar mensaje correcto.
- [x] **Bulk solo con soft-delete real.** Mostrar selección y formulario bulk únicamente cuando el modelo tiene `IsDeleted`; el handler responde con toast si se invoca sin soporte.

### Autenticación propia

- [x] **Login propio del Admin sin Identity.** Página, modelo, validación y mensajes de error.
- [x] **Logout propio del Admin.** Endpoint POST con antiforgery y sin depender de `/Identity/Account/Logout`.
- [x] **Cookie de autenticación propia.** Nombre, expiración, `HttpOnly`, `Secure`, `SameSite` y renovación configurables.
- [x] **Usuarios administrativos.** Contrato de usuario/store, estado activo/inactivo y claims mínimos.
- [x] **Hash seguro de contraseñas.** Usar PBKDF2-SHA256 sin incorporar el sistema completo de Identity.
- [x] **Comando `admin:user` independiente de Identity.** Crear o actualizar el administrador inicial desde configuración o variables de entorno.
- [x] **Cambio de contraseña.** Requerir la contraseña actual y aplicar las mismas reglas de hashing.
- [x] **Bloqueo y rate limiting del login.** Limitar intentos y aplicar bloqueo temporal sin revelar si el usuario existe.
- [x] **Expiración y revocación de sesiones.** Cookie con expiración deslizante, logout efectivo y `SecurityStamp` validado para invalidar sesiones tras cambio de contraseña.

### Autorización granular

- [x] **Proteger el dashboard.** No exponer recursos ni metadatos a usuarios anónimos.
- [x] **Filtrar recursos por autorización.** Mostrar únicamente los recursos cuyo permiso `List` esté autorizado.
- [x] **Usar `AdminOperation` en la autorización real.** Diferenciar `List`, `Read`, `Create`, `Update`, `Delete` y `BulkAction`.
- [x] **Permisos por recurso y operación.** Permitir políticas o claims como `Admin.Products.Update`.
- [x] **Política segura por defecto.** Denegar si no existe autenticación o autorización explícita.
- [x] **Eliminar el bypass anónimo del sample.** El sample exige autenticación y registra políticas por operación.

### Protección web y auditoría

- [ ] **Validar `returnUrl`.** Aceptar únicamente rutas locales y evitar open redirects.
- [ ] **Revisar cookies y encabezados de seguridad.** Configurar HSTS, CSP, `X-Content-Type-Options`, `Referrer-Policy` y políticas relacionadas.
- [ ] **Auditoría de acciones administrativas.** Registrar login, logout, fallos de login y operaciones CRUD/bulk.
- [ ] **Extensión de auditoría desacoplada.** Exponer un `IAdminAuditSink` opcional sin imponer almacenamiento.
- [ ] **Pruebas de seguridad del panel.** Cubrir acceso anónimo, `Challenge`, `Forbid`, CSRF, permisos por operación y aislamiento de recursos.

## Código y mantenimiento

- [x] **Mover `FormatValue` y `L(...)` a helper compartido.** (Ya existe `AdminPageModelHelper`).
- [x] **Propiedad `Singular` en `AdminResource`.** En lugar de `TrimEnd('s')`.
- [x] **Idioma consistente.** La UI base usa inglés de forma consistente.
- [x] **Logout sin dependencia de Identity.** Revisar si el README lo documenta.
- [x] **Badge "Development" desde `IWebHostEnvironment`.**
- [x] **Icono de dashboard por recurso.** No usar el mismo para todos.

## Lo que ya está bien ✅

- Layout con sidebar + topbar + content.
- Hamburger menu funcional.
- Dark mode completo.
- SVG icons consistentes.
- Modal de confirmación.
- Responsive con breakpoints Tailwind-style.
- Empty states básicos.
- Toasts de éxito.
- Loading states en botones.
- ARIA labels básicos.
- CSV export.
- Configurable page size.
- Detail view.
- Field types nativos.
- Validation attributes.
- Localization.
- RTL support.
- `laranet admin:*` commands.
- Auto-discovery.
- Provider compatibility tests.
