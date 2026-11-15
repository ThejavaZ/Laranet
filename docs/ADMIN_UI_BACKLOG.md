# Admin UI Backlog

This file tracks the remaining UI and functionality improvements for the Laranet
admin panel. Each item should be implemented and verified independently.

## Tablas

- [ ] **Responsive table cards with visible labels.** En móvil (≤760px), cada fila es una tarjeta con etiquetas visibles. En tablet (761–1023px), tabla con scroll horizontal dentro de su contenedor. En desktop (≥1024px), tabla normal. Usar un solo mecanismo. Eliminar reglas duplicadas de ≤767px y ≤760px. El thead oculto usa `.sr-only`.
- [ ] **Zebra striping y hover visible.** Filas alternas con color sutil. Hover con transición.
- [ ] **Indicador de ordenación actual.** Flecha ↑/↓ visible y `aria-sort` en el header.
- [ ] **Badges de estado para booleanos.** Active/Inactive con color.
- [ ] **Acciones con iconos.** Edit/Delete con iconos SVG y tooltip, no texto.
- [ ] **Checkbox "seleccionar todo"** en el header.
- [ ] **Total no repetido.** Eliminar "results" duplicado.
- [ ] **Loading skeletons.** Para tablas y cards.
- [ ] **Empty states con CTA prominente.** Ilustración SVG + botón.

## Formularios

- [ ] **Validación en servidor por tipo.** `decimal.TryParse`, `int.TryParse`, `DateTime.TryParse` con `ModelState` y mensajes de error. Nunca convertir a 0 en silencio.
- [ ] **Validación en cliente.** `type="number"` con `step`, `required`, `min`, `max`, `maxlength`. Estados de error visibles (borde rojo y mensaje).
- [ ] **Checkbox booleano refleja valor actual.** `checked` attribute en Edit.
- [ ] **Estados de error visibles.** Borde rojo y mensaje debajo del campo.
- [ ] **Secciones/fieldsets** en formularios largos.
- [ ] **Indicador de campo requerido** (`*`).

## Paginación y navegación

- [ ] **Paginación real.** Botones First/Prev/Next/Last + números de página.
- [ ] **Preservar estado en sort/filtros/page size.** No perder filtros al ordenar o cambiar tamaño.
- [ ] **Breadcrumbs.** Mostrar ruta actual.
- [ ] **Estado activo en sidebar.** `aria-current="page"`.

## Feedback visual

- [ ] **Toasts con tipos.** Success, error, warning, info con colores distintos.
- [ ] **Modal con Escape y foco atrapado.** Cerrar con Escape, trap de foco.
- [ ] **Loading skeletons.** Para tablas y cards.

## Seguridad

- [ ] **Validar `SortBy` contra lista blanca.** Prevenir inyección.
- [ ] **Limitar `PageSize` y `PageNumber`.** Clamped a rangos válidos.
- [ ] **Challenge/Forbid en vez de 404.** 401 para anónimos, 403 para no autorizados.
- [ ] **Export CSV con BOM.** UTF-8 con BOM para Excel.
- [ ] **Bulk delete sin selección.** Validar y mostrar mensaje correcto.

## Código y mantenimiento

- [ ] **Mover `FormatValue` y `L(...)` a helper compartido.** (Ya existe `AdminPageModelHelper`).
- [ ] **Propiedad `Singular` en `AdminResource`.** En lugar de `TrimEnd('s')`.
- [ ] **Idioma consistente.** Decidir inglés o español para toda la UI.
- [ ] **Logout sin dependencia de Identity.** Revisar si el README lo documenta.
- [ ] **Badge "Development" desde `IWebHostEnvironment`.**
- [ ] **Icono de dashboard por recurso.** No usar el mismo para todos.

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
