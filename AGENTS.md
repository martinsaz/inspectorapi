# AGENTS

## Planeación 01 — schema multitenant, 2026-09-09 — PENDIENTE DE APROBACIÓN

- Única iteración de planeación documentada en `/Users/denissemendiola/dev/Inspecciones/inspector/docs/database/PLANEACION_VERSIONAMIENTO_SCHEMA_MULTITENANT_20260909.md`. Arquitectura PROPUESTA, no aprobada; implementación NO iniciada. Esperar aprobación expresa del PO / Líder de Proyecto.
- Hallazgos estáticos: catálogo real de configuración en Firebase `Conexiones`; ProductosServicios abre hoy la conexión fija de `SqlConnectionFactory`, sin asociación tenant→base certificada. MVC conserva fallback de empresa desde query; el helper `Firebase.GetCadenaConexion` no une la clave de conexión con la empresa y no tiene llamada activa localizada. No reutilizar estos comportamientos para DDL.
- No se encontró versión SQL, baseline ni runner secuencial en fuentes. Hay scripts manuales, guardas parciales y DDL bajo petición en Cotizaciones. 20 tablas del núcleo declaradas; no son inventario físico certificado. Esta iteración no consultó catálogo vivo ni SQL; el SQL 18456 corresponde a la auditoría anterior, no a todos los tenants.
- Recomendación pendiente de aprobación: ejecutor administrativo separado, catálogo/resolver backend sin fallback, contratos inmutables + SQL revisado, adopción sin historia falsa, versión/historial local, validación física por paso, lock por base y transacción por transición. Bases compartidas requieren grupo de impacto: el DDL no se aísla por idEmpresa.
- Scripts Ticket 09/10 mezclan DDL/DML y algunos cambian consumidores externos o eliminan unidades; no envolverlos como auto-reparación. Backfills, suspendidos, grupos compartidos, ventana, política de incompatibilidad y activación quedan sujetos a decisiones explícitas del PO según el documento.
- No se cambió código funcional, SQL, configuración, esquema, datos, Login/Auth, Firebase, sesión, roles o permisos. No hubo migraciones, commit, push, despliegue ni servidores iniciados. Sólo documentación; cambios previos preservados.
- Ticket 10 permanece CERRADO POR PRODUCT OWNER; PDF Producto y Servicio aprobados. PrecioPublico por UNA unidad base, base 1:1, adicionales independientes y atributos/variantes separados. No reabrir ni reinterpretar esas reglas.

## Estado oficial vigente — Product Owner, 2026-09-09

- **TICKET 10 CERRADO POR PRODUCT OWNER — PDF PRODUCTO Y SERVICIO APROBADOS.**
- El PO revisó y aprobó manualmente ambos PDF. Este cierre sustituye los estados históricos pendientes de QA de Ticket 10 conservados abajo como bitácora.
- No reabrir Ticket 10 ni modificar su diseño/código sin nueva instrucción explícita del Product Owner.
- Nueva etapa autorizada: auditoría de solo lectura del dominio Productos y Servicios, contrastando base real, API, frontend, DTO y scripts. No autoriza implementación, cambios de datos/esquema ni ejecución de scripts de escritura.
- Mantener PrecioPublico como precio de UNA unidad base; presentación base 1:1 y adicionales con precio independiente.
- Al terminar, detener únicamente procesos iniciados por Codex; respetar procesos preexistentes del PO.

## Auditoría Productos y Servicios — hallazgos consolidados, 2026-09-09

- Estado: auditoría estática documentada; auditoría SQL real pendiente por error de autenticación 18456 del destino configurado. No certificar conteos, esquema real, FK físicas, huérfanos ni drift hasta obtener SELECT reales.
- Código y scripts actuales contemplan imagen propia por variante (`ProductosServiciosVariantes.ImagenUrl/ImagenNombre`); la multimedia general es otra tabla. Este hallazgo de código sustituye el diagnóstico histórico de imagen ausente, sin afirmar comprobación física actual.
- Código vigente: PrecioPublico corresponde a UNA unidad base y sincroniza la base 1:1; adicionales independientes. PrecioUnitario* del maestro se conserva como legacy. Atributos descriptivos y opciones comerciales permanecen separados.
- Modelo versionado de inventario: empresa/producto, sin dimensión sucursal/variante. No se infiere ausencia de otras tablas reales sin consulta.
- Informe parcial: `/Users/denissemendiola/dev/Inspecciones/inspector/docs/productos-servicios/AUDITORIA_BASE_DATOS_PRODUCTOS_SERVICIOS_20260909.md`. Anexo con esquema declarado y SELECT pendientes; no confundirlo con esquema real certificado.
- No hubo cambios de datos, esquema, migraciones, SQL de escritura ni código funcional. No se iniciaron servidores. La auditoría no autoriza correcciones ni reabre Ticket 10.



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
- Cierre funcional Ticket 05 del `2026-08-24` en backend/API:
  - se conserva la regla de desconfianza hacia `idEmpresa`, `empresa`, `cadena` y `correo` enviados por cliente
  - el endurecimiento multiempresa de Activos sigue vigente con validación de empresa firmada por proxy MVC
  - se agregó script idempotente versionado:
    - `checklistWs/Scripts/activos-catalogos-unique-codigos-up.sql`
  - ejecución real certificada:
    - sin duplicados previos por `(idEmpresa, Codigo)` en `dbo.ActivosMarcas`
    - sin duplicados previos por `(idEmpresa, Codigo)` en `dbo.ActivosProveedores`
    - índices creados:
      - `UX_ActivosMarcas_IdEmpresa_Codigo`
      - `UX_ActivosProveedores_IdEmpresa_Codigo`
    - históricos eliminados solo por autorización y con `0` referencias:
      - `ORD-QA-27`
      - `VIS-QA-27`
  - build final de `checklistWs.csproj` correcto; se mantienen warnings legacy de paquetes existentes


- TICKET 10 — corrección UX por QA del Product Owner, `2026-09-07`:
  - Precios: ayudas por click/tap y ancho de Unidad base igual a la columna real de Categoría en desktop/tablet/móvil.
  - Presentaciones: sin Nombre manual ni columna redundante; cantidad + unidad, equivalencia compacta con sufijo, ayudas y modal compacto. API deriva Nombre legacy del catálogo en alta inicial/individual y edición; sin migraciones.
  - Fórmulas, factores, 53 Sistema, CRUD Unidades, Login/Auth, IVA, Ticket 08, POS e inventario operativo intactos.
  - QA acotada PASS: Sixpack 6 pz/$50, Metro 100 cm readonly, Kilogramo 2.2046 lb readonly; alta/edición sin Nombre, precio predeterminado, responsive 1890/820/390 sin overflow; builds sin errores y sintaxis JS PASS.
  - Limpieza final: 2 productos de trabajo y 55 unidades activas; 0 productos/presentaciones QA adicionales activos. Sixpack temporal dado de baja.
  - Evidencia: `inspector/docs/qa/ux-precios-presentaciones-ticket10-20260907/INFORME_MOKA.md` (67 puntos).
  - Aviso preexistente al reabrir productos con presentaciones («No puedes cambiar la unidad base») documentado fuera de alcance; no se alteró la protección.
  - Estado: listo para segundo QA visual del Product Owner; no declarar Ticket 10 cerrado.


## Instrucción permanente del Product Owner — puertos locales

- Al terminar cualquier trabajo o QA local, liberar siempre los puertos 5200 y 5127 deteniendo los servidores del proyecto y comprobar que no queden listeners. No dejarlos funcionando al entregar, salvo instrucción posterior explícita del Product Owner.


- TICKET 10 — segundo QA del Product Owner, `2026-09-07`:
  - Cinco ayudas coordinadas: una visible; cierre fuera/otra ayuda/Escape/modal/card. Labels explícitos evitan activar ayuda al pulsar selector.
  - Unidad base y venta agrupadas Peso/Volumen/Longitud/Área/Por artículo/Tiempo/Otra, nombre+abreviatura y búsqueda Select2.
  - Causa Paquete: alta rápida perdía TipoUnidad y demás metadata local. Se consulta registro completo y conserva metadata; refresco sin F5 comprobado.
  - OTHER activa admite equivalencia manual con cualquier base según instrucción PO. Ajustados sólo predicados de admisión manual en API; fórmulas/factores/precisión y motor intactos.
  - Labels Cantidad de venta / Cantidad en inventario y ayudas actualizadas. QA PASS: Paquete 6 pz/$50, Metro 100 cm readonly, Kilogramo 2.2046 lb readonly, búsqueda y responsive; builds sin errores.
  - Limpieza: 2 productos activos, 56 unidades activas (incluye Paquete del PO); unidad/producto/presentación temporales dados de baja. 53 Sistema idénticas.
  - Puertos 5200 y 5127 liberados al entregar, por instrucción permanente.
  - Informe completo: `inspector/docs/qa/segundo-qa-po-ticket10-20260907/INFORME_MOKA.md`. Listo para siguiente QA visual; no declarar cerrado.


- TICKET 10 — nueva regla aprobada Precio público / presentación base, `2026-09-07`:
  - Esta instrucción posterior del PO REEMPLAZA la regla anterior de sincronización desde una presentación predeterminada elegida: PrecioPublico es el precio de UNA unidad base.
  - Base automática única 1 unidad de venta base → 1 unidad inventario; PrecioPublico la actualiza. Adicionales con precios independientes nunca sobrescriben PrecioPublico.
  - Sin elección Predeterminada en UI. Campo legacy conservado/derivado sólo para base; no hay consumidores POS actuales. No inventar una función futura ni reactivar sincronización desde paquetes.
  - Caso del PO era borrador Nuevo sin id/nombre/código/categoría, no persistido. Se documentó antes y se guardó con identidad de prueba comunicada: QA-T10-PRECIO-BASE, Producto QA T10 Precio base, Alimentos, id 4ffbb00e-d4b3-4199-981d-5714a707b516. Conservar para continuar QA.
  - Final: Pieza1/1 $100, Paquete6 $250, Paquete12 $480; costo80, comparación120, ganancia20, margen20%. Guardar/F5/reapertura PASS; cambio público110 sólo cambia base; paquetes260/500 no cambian público; importes restaurados.
  - API bloquea alta duplicada/edición directa/baja de base. Edición de borrador conserva id. Sin migraciones/SQL estructural ni cambios en unidad base, conversiones, IVA, Ticket08, POS, inventario o Auth.
  - Builds sin errores; sintaxis/diff PASS. Puertos5200/5127 libres al entregar.
  - Informe45: inspector/docs/qa/precio-base-ticket10-20260907/INFORME_MOKA.md. Aviso preexistente de unidad base al cargar producto documentado fuera de alcance. No declarar ticket cerrado.


- TICKET 10 — validaciones y ficha técnica, 2026-09-08:
  - Orden general Por artículo/Peso/Volumen/Longitud/Área/Tiempo/Otra; unidad base excluida de venta adicional. API rechaza repetición y duplicados activos por cantidad/unidad/equivalencia a 4 decimales, también alta inicial/edición, sin precio en llave.
  - Ficha/PDF añaden ganancia/margen y presentaciones activas para Producto; Servicio conserva sólo valores aplicables. Motor, factores, catálogo, IVA y Ticket08 sin cambios.
  - API y PDF real verificados; QA visual/F5/responsive pendientes porque Chrome no tiene sesión. No declarar cerrado/aprobado.
  - Instrucción posterior explícita de esta entrega: dejar 5200/5127 activos para QA manual.
  - Nueva limpieza autorizada sustituye conservar fixture anterior: QA-T10-PRECIO-BASE y sus presentaciones archivados. Final 2 registros de trabajo, 53 Sistema + 3 personalizadas; Paquete activo. Datos de trabajo idénticos.
  - Informe: inspector/docs/qa/validaciones-ficha-ticket10-20260908/INFORME_MOKA.md.
