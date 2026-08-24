# AGENTS

## Entorno activo

- Frontend local relacionado: `/Users/denissemendiola/dev/CheckList_Original/checklist`
- Backend local: `/Users/denissemendiola/dev/checklistWs-Original/checklistWs`
- URL frontend local: `http://localhost:5200`
- URL API local activa: `http://localhost:5127`

## Configuracion de API

- La configuracion publicada debe preservarse como referencia cuando exista, sin romper el ambiente local activo.
- El backend local es la fuente real para QA funcional del frontend en ambiente local.
- No habilitar origenes amplios ni bypass de seguridad solo para facilitar pruebas.

## Responsabilidades por capa

- La API conserva la responsabilidad exclusiva sobre logica de negocio, validaciones de negocio, acceso a datos, persistencia, integridad y contratos HTTP.
- El frontend no debe absorber reglas de negocio para evitar cambios funcionales ocultos.
- Los cambios del backend deben preservar contratos existentes salvo aprobacion explicita.

## Politica de base de datos

- Esta prohibido modificar esquema, tablas, columnas, relaciones, indices, stored procedures, migraciones o datos estructurales sin autorizacion expresa del Product Owner.
- Antes de proponer cambios de esquema se debe evaluar primero la reutilizacion del modelo actual.
- Cualquier necesidad de cambio debe documentar problema, reutilizacion evaluada, cambio minimo, impacto, riesgos y regresiones antes de pedir autorizacion.

## Reglas de trabajo

- Documentar cada cambio tecnico y sus regresiones verificadas.
- Proteger funcionalidades aprobadas y evitar efectos laterales fuera del alcance.
- Liberar unicamente procesos iniciados por Codex; no detener procesos previos del usuario sin instruccion.
- No dejar textos tecnicos ni mensajes de auditoria visibles para usuarios finales.
- No deshabilitar autenticacion, permisos, sesion o controles de acceso para pasar QA.
- Los endpoints multiempresa deben asumir que `idEmpresa`, `cadena`, `empresa` y `correo` pueden llegar manipulados desde el cliente; cualquier endurecimiento debe preservar contratos pero buscar que el frontend proxy envie contexto resuelto desde sesion del servidor.
- La API define qué listas son ejecutables para operación; el frontend no debe inferirlo.
- Las pantallas operativas no deben reutilizar sin análisis los catálogos generales de diseño.
- Está prohibido exponer mensajes técnicos al usuario final.
- No modificar esquema, tablas ni estados persistidos sin autorización expresa.
- Desde el 2026-07-20 quedan pausados R3 y cualquier cambio adicional de `Inspección en campo` hasta definir la arquitectura completa de `Operadores`.
- El login vigente se bloquea por `Usuarios/{uid}.status` en Firebase Realtime Database; el `Estatus` del usuario SQL no es suficiente por sí solo para negar acceso.
- El modelo actual de usuarios mantiene una sola `IdSucursal`; no asumir multisucursal real sin diseño y autorización explícitos.

## Ultima certificacion local

- Certificacion frontend -> API local validada el 2026-07-17 con frontend en `http://localhost:5200` y API en `http://localhost:5127`.

## Consumo activo desde Recolecciones BL26

- La nueva ruta frontend `http://localhost:5200/ContestarLista/RecoleccionesBL26` consume el backend local reutilizando contratos existentes.
- Este backend debe preservar esos contratos sin cambios de esquema ni cambios de firma para no romper la ruta paralela BL26 ni el flujo legacy.
- En esta fase no se autorizaron endpoints nuevos ni cambios de persistencia para recolecciones BL26.
- El frontend BL26 reutiliza el permiso legacy `02001000` de `Nueva`; cualquier bloqueo de acceso adicional debe resolverse en datos/permisos existentes y no inventando un contrato paralelo.
- Validacion local del `2026-07-17`:
  - el frontend autentico cargo listas reales en `/ContestarLista/RecoleccionesBL26`
  - el paso de sucursales quedo bloqueado porque `api/Sucursal/ObtenerSucursalesPorUsuario` respondio `[]` para la sesion de QA
  - no se autorizaron cambios de base de datos ni ajustes manuales de datos para desbloquear esa respuesta
- Auditoria del `idEmpresa` el `2026-07-17`:
  - el valor tenant auditado proviene de Firebase Realtime Database y llega a la API por parametros HTTP
  - la API de sucursales no usa una sesion propia; depende del contexto reenviado por el frontend
  - cualquier validacion adicional entre correo e `idEmpresa` debe mantener compatibilidad con los consumidores legacy existentes
- Diagnostico y correccion de catalogos globales el `2026-07-17`:
  - `ObtenerCategorias` y `ObtenerSubcategorias` fallaban con `500` por filas activas con `notas = NULL`
  - la tolerancia a `NULL` se corrigio solo en lectura del backend local
  - no hubo cambios de contrato, datos ni esquema
  - resultado validado:
    - `ObtenerCategorias` regreso `28` registros para la empresa auditada
    - `ObtenerSubcategorias` regreso `26` registros para la empresa auditada
    - los proxies frontend volvieron a poblar `CategoriasABC`, `SubcategoriasABC` y los combos de `CreadorListaBL26`
- Auditoria operativa previa a R2 del `2026-07-17`:
  - reglas reales confirmadas en API:
    - `ObtenerCategorias` y `ObtenerSubcategorias` filtran por `idEmpresa` y `borrado = 0`
    - no existe relacion contractual `categoria -> subcategoria` en los endpoints de combo actuales
    - `Evaluacion/ObtenerPreguntasXPrograma` expone `categoria` y `subcategoria` por pregunta activa (`lp.status = 1`)
    - `ObtenerComboProgramasXAlumno` expone listas cerradas (`Estado = 2`) sin restringir hoy por preguntas activas
  - riesgo documentado:
    - el flujo de recolecciones puede heredar listas cerradas no ejecutables o con `Status = false`
    - no se altero ese filtro en esta tarea porque el endpoint es compartido por experiencias legacy
  - cierre controlado de R1:
    - se autoriza crear operaciones aisladas cuando la regla operativa difiera del endpoint legacy compartido
    - la operación de listas ejecutables debe filtrar por empresa, `Estado`, `Status`, `Activo` y presencia de preguntas activas
- Auditoria de persistencia R3 del `2026-07-18`:
  - el backend legacy de respuestas inserta filas en `ListasRespuestas` y relaciona anexos por `idListaRespuesta`
  - la identidad operativa disponible hoy es `evento`, pero se recibe desde el cliente y no existe una cabecera propia de ejecucion
  - no existe estado persistente para diferenciar inspecciones abiertas, en proceso o terminadas
  - no existe garantia nativa para recuperar una ejecucion abierta de forma no ambigua ni para bloquear duplicados de ejecucion o de respuesta
  - cualquier persistencia real nueva para `RecoleccionesBL26` debe pasar primero por autorizacion expresa del Product Owner cuando implique:
    - nueva cabecera de ejecucion
    - nuevo estado persistente
    - regla de unicidad
    - actualizacion controlada de respuestas ya guardadas
  - mientras esa autorizacion no exista, no se deben implementar heuristicas de recuperacion ni persistencia simulada para cerrar la fase R3
- Propuesta tecnica R3 pendiente de autorizacion del `2026-07-18`:
  - si se autoriza persistencia real, la API debe generar y controlar la identidad de ejecucion y el `evento` de compatibilidad
  - `evento` no debe seguir siendo la identidad principal de la nueva inspeccion en campo
- Certificacion previa del cambio de esquema R3 del `2026-07-18`:
  - la arquitectura de persistencia fue aprobada en principio por el Product Owner
  - la autorizacion final de esquema sigue pendiente
  - se prepararon scripts exactos de avance y rollback solo como propuesta documental
  - no se ejecutaron scripts ni cambios sobre la base
- Auditoria de Operadores del `2026-07-20`:
  - existe autorregistro público que crea Firebase Auth, Firebase RTDB y usuario SQL en pasos separados y sin rollback explícito
  - `ListasProgramacion` existe como base de asignación, pero `ObtenerComboProgramasEjecutablesXAlumno` aún devuelve listas ejecutables por empresa y no restringe realmente por usuario
  - antes de habilitar Operadores se debe definir:
    - restricción server-side exclusiva para `Inspección en campo`
    - política de suspensión que corte acceso real y sesión activa
    - modelo de asignación de sucursales y listas por operador
- Paquete C de Operadores ejecutado el `2026-07-20`:
  - el modelo legacy real no usa tablas separadas de permisos ni menú; `Inspección en campo` queda representado en `dbo.Roles.Permisos`
  - se creó el permiso exclusivo `02005000` para `/ContestarLista/RecoleccionesBL26`
  - se creó `Operador Base` solo para empresas activas detectadas en `dbo.Empresa`
  - la transición quedó compatible con `02001000 OR 02005000`, sin retirar acceso legacy
  - evidencia actual en base:
    - respaldo `dbo.Roles_BKP_OPERADORES_C_20260720_130454`
    - `1` rol base insertado
  - Paquete D sigue pendiente
- Fase O1 de Operadores implementada el `2026-07-20`:
  - se agregaron endpoints API para listar, consultar, crear, actualizar rol, suspender y reactivar `OperadoresPerfil`
  - el CRUD opera solo sobre `dbo.OperadoresPerfil`; no crea cuentas Firebase, no modifica `dbo.Usuarios` y no cambia esquema
  - el acceso nuevo a `RecoleccionesBL26` usa `02005000` desde `idRolOperador` + perfil activo, manteniendo compatibilidad con `02001000`
  - la validación de roles operativos depende hoy de `dbo.Roles.Permisos` porque `Roles` no tiene columnas de estatus o borrado
  - QA local:
    - `UMBRELLA CORP` tiene candidato elegible pero sin rol `02005000`
    - la empresa con `Operador Base` no tiene usuarios candidatos
    - `OperadoresPerfil` cerró con `0` filas
- Certificación positiva O1 de Operadores cerrada el `2026-07-20`:
  - `UMBRELLA CORP` quedó con `Operador Base` usando `02005000`
  - la API validó alta, duplicado, edición con concurrencia, suspensión y reactivación del perfil temporal autorizado
  - al cierre no quedaron datos QA en `OperadoresPerfil` ni en `ListasOperadoresAsignaciones`
  - no hubo cambios adicionales de backend durante esta certificación final; la limitación observada fue de autenticación/sesión compartida en frontend
- TICKET 02 — Diseño y Validaciones de `Productos y Servicios` ejecutado el `2026-08-21`.
- Reglas backend/API ratificadas por esta iteración:
  - la API mantiene autoridad final sobre validaciones funcionales y mensajes no técnicos
  - no se modificó esquema SQL; se reutilizó el modelo vigente
  - `PrecioUnitarioMonto`, `PrecioUnitarioBaseCantidad` y `PrecioUnitarioUnidad` ya soportaban el concepto requerido; solo se endureció validación de completitud
- Hallazgos funcionales auditados el `2026-08-21`:
  - `Categoría`, `Marca` y `Unidad` tienen duplicado real por `Código`
  - `Colección` tiene duplicado real por `Número`
  - `Paquete` ya soportaba `EsPredeterminado` por empresa; la API limpia el predeterminado previo al marcar uno nuevo
  - `PesoKg` del producto y `PesoEmpaqueVacioKg` del paquete son conceptos distintos y deben conservarse separados
- SAT auditado en solo lectura el `2026-08-21`:
  - el legado `Raramuri.blzr` usa `_opcionesProd` y `_opcionesUnidad`
  - `sazapi` obtiene catálogo SAT desde API externa con rutas `GetClaveProdServ4` y `GetTodoClaveUnidad`
  - la API CheckApp debe consumir esa fuente de manera server-to-server y nunca exponerla directamente al navegador
  - `H87` debe preservarse como unidad base segura cuando no exista otra selección
