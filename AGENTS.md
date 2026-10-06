# PATRON CHECKAPP OFICIAL - PRODUCTOSSERVICIOS - 2026-09-16

- 2026-10-06 #MOKA OC-CUR-05R1: `POST api/CurvasSiembra/Cerrar` y `CurvasScopeService.CerrarSiembraAsync` cierran solo la vigente mediante `Estado=3` + `FechaVigenciaFin/FechaActualizacion/idUsuarioActualizacion`, con `UPDLOCK/HOLDLOCK`, transaccion serializable, sucursal tenant-safe, HMAC proxy oficial, permiso `05005002` WRITE y respuesta controlada si ya no existe; historico preservado, hard delete/schema/migracion 0. Runtime UMBRELLA 163 PASS: 0 -> sembrar -> F5 -> reemplazar -> una vigente -> cerrar -> F5 -> 0; cleanup neto 0 y responsive `1440/820/390` PASS. Preview Huecos/Copetes runtime queda BLOQUEADO porque `CurvasSugerencias/Preview` no tiene consumidor MVC/contexto proxy autenticado; motor intacto. Regresion focal `364/364`, full `845/846` con fallo ListaPrecios preexistente, builds/node/diff/secret scan PASS. No aprobado PO/FROZEN; OC-CUR-06 no iniciado.

- 2026-10-06 #MOKA OC-CUR-05R QA RUNTIME: login QA autenticado en UMBRELLA 163; ruta separada, permiso `05005002` WRITE, sucursal Sede Central, curva `QA DENISSE`, detalle objetivo y responsive exacto `1440/820/390` PASS, sin overflow. STOP REAL antes de sembrar: el tenant parte con 0 siembras activas y el flujo no ofrece cierre/reversion oficial de la ultima siembra; ejecutar H dejaria datos legitimos imposibles de restaurar sin SQL/hard delete. No se crearon siembras/fixtures ni se modificaron datos, roles o usuarios. H-N, F5, reemplazo y Preview post-siembra quedan NO EJECUTADOS; no declarar QA completa, aprobado PO ni FROZEN. Siguiente paso: definir/autorizar cierre oficial reversible o proporcionar tenant desechable/baseline reversible; OC-CUR-06 NO ejecutado.

- 2026-10-06 #MOKA OC-CUR-05 IMPLEMENTACION LOCAL: API `api/CurvasSiembra` expone sucursales, curvas activas, detalle, vigentes y sembrar/reemplazar con permiso `05005002`, Gate Curvas y tenant exclusivamente desde contexto firmado/claims. Reutiliza `CurvasScopeService.SembrarAsync`; el servicio valida sucursal activa, curva, producto no servicio, variante y pertenencia exacta a `CurvasDetalle`, cierra vigente y preserva auditoria. Sin schema/migracion ni cambios al motor Huecos/Copetes o ListaPrecios. Build PASS, focal Curvas `29/29`, full `845/846` con unico fallo preexistente permitido `ListaPreciosMatrixSqlIntegrationTests`. Estado: `IMPLEMENTADO / QA RUNTIME AUTENTICADA BLOQUEADA POR SESION CHROME NO AUTENTICADA`; no declarar PASS A-N, responsive, aprobado PO ni FROZEN.

- 2026-10-06 #MOKA OC-CUR-04R RECUPERACION BL-03: auditoria read-only de MVC/API/contratos confirma OC-03/OC-03R implementado tecnicamente pero pendiente QA PO; REC-01 conserva schema/API/servicio e Inventario V1 sin UI operativa; OC-CUR-04S conserva Catalogo de Curvas completo y requiere QA manual Denisse tablet/mobile, sin correccion tecnica nueva identificada en Catalogo. OC-CUR-05 queda PARCIAL: `CurvasSiembra` y `CurvasScopeService.SembrarAsync` existen, pero no hay endpoint, permiso `05005002`, ruta MVC, menu ni UI. OC-CUR-03 conserva motor Preview read-only de Huecos/Copetes, sin UI ni persistencia desde Preview. Defecto comprobado pendiente: Recepcion puede llevar OC a estados 4/5, mientras `OrdenesCompraController.GetEstadoNombre*` solo mapea 1/2/3 y puede mostrar `Desconocido`. Focal BL-03 `73/73` PASS; suite global `838/839`, unico fallo ajeno en integracion ListaPrecios por datos no nulos; no corregir dentro de BL-03. No se implemento OC-CUR-05, no hubo schema/DDL/datos/puertos ni cambios funcionales. Siguiente desarrollo nuevo recomendado, sujeto a revision/autorizacion PO Denisse: `OC-CUR-05 - Siembra de Curvas`.

- 2026-10-06 #MOKA FREEZE-RECOVERY-01: Lista de Precios queda `PAUSADA / CONGELADA POR DECISION PO` en `LP-QA10R`. QA tecnica Codex realizada; QA manual Denisse INCOMPLETA; no declarar QA Denisse PASS, aprobacion PO final ni FROZEN funcional definitivo. Preservar exactamente el estado actual: no continuar tickets LP, corregir, agregar features, refactorizar, reinterpretar Legacy ni tocar UI/API/schema. La cronologia MVC/API coincide: el frente inmediatamente anterior fue Productos y Servicios; `PS-ACT-01S-R5` quedo `APROBADO PO` y FROZEN como Golden Master SIMPLE/COMPACTO de Categorias, Marcas, Unidades de medida, Colecciones y Etiquetas, aplicable a quick-create, Alta y Edicion. Retomar desde `PS-ACT-01 PARTE 2`, que permanece NO EJECUTADO y requiere ticket/handoff PO especifico. No regresionar Parte 1, `PS-ACT-01S-R5`, Patron CheckApp Catalogos V1, permisos `05001003`..`05001007`, AuthZ/Gate/idEmpresa, DynamicGrid, baja logica/reactivacion ni contratos reales de Colecciones/Etiquetas. BL-03, Ordenes de compra, Recepcion, Curvas, T25, Reporte Lider y Legacy continúan FROZEN.

- REGLA PO PERMANENTE (LP-QA05): cuando Denisse ordene auditar un sistema hermano, el comportamiento encontrado es la referencia. Codex no agrega pasos, complementos ni "mejoras" por iniciativa propia; cualquier desviación requiere autorización PO. En Editar Precios no se muestran Preview, Historial, Calcular precios finales ni Vigencia. El flujo sí conserva precios/descuentos, redondeo, alcance y "Aplicar redondeo a todas" según Legacy, además de los bloques solicitados de Datos adicionales y Promociones hasta donde exista persistencia real autorizada.

- 2026-10-05 #MOKA LP-QA05S3: hotfix V3 certificado y activado. Se reemplazaron precondiciones descriptivas no ejecutables por SQL real, se separó la unicidad activa Producto/Servicio en índices filtrados compatibles y el hash final quedó `0ce8a1e391f89577ce20a0e29f5a048b48109a93951346a395b5618073b794e5`; el hash `086c8e7fe0aced219dda9e3937ac0ddc2299c27b202ec2827c5eeda3916b732d` queda `SUPERSEDED`. CHECKAPPERP y UMBRELLA fueron migradas por `SchemaMigrationRunner`, con drift 0, datos legítimos sin cambios, extensión vacía, segunda corrida no-op y Gate `COMPATIBLE`. `LatestVersion`, versión conocida y package runtime quedan en V3. Persistencia del modal Datos adicionales/Promociones sigue fuera de alcance; no declarar aprobación PO ni FROZEN.

- 2026-10-05 #MOKA LP-QA05: paridad estricta del modal Editar Precios implementada y QA técnica PASS. Se retiraron Preview, Historial, Calcular precios finales y Vigencia; matriz 10 listas y redondeo Legacy certificados; Datos adicionales y Promociones permanecen visibles pero no editables hasta autorización de persistencia. Focales `189/189`, específicos `6/6`, full `787/787`, builds/node/diff/secret scan PASS y responsive `1440/820/390` PASS. Estado exclusivo: `CORRECCIONES UX IMPLEMENTADAS / STOP PO SCHEMA / PENDIENTE AUTORIZACIÓN`; no aprobado PO ni FROZEN.
- 2026-10-05 #MOKA LP-QA05S: propuesta ProductosServicios V2→V3 modelada sin DDL ni registro ejecutable. Agrega sólo el contrato de `ProductosServiciosIdentidadComercial` y su historial, con cuatro identidades, FK tenant-safe, baja lógica, auditoría y banderas booleanas independientes de lista/sucursal/vigencia. El manifest oficial y Compatibility Gate permanecen en V2. Pruebas LP-QA05S `21/21`, migration engine `36/36`, ListaPreciosService `138/138`, full `808/808` y builds API/MVC PASS. Pendiente revisión PO; no migrar ni conectar runtime.
- 2026-10-05 #MOKA LP-QA05S1: PO aprobó conceptualmente ProductosServicios V3 y se materializó el package local `PS-M20261005-V2-V3-IDENTIDAD-COMERCIAL` con hash final `086c8e7fe0aced219dda9e3937ac0ddc2299c27b202ec2827c5eeda3916b732d`. La unicidad activa usa tres índices filtrados y no semántica nullable compuesta. El método preparado reutiliza package/resolver/runner oficial, pero `LatestVersion`, Gate y `GetPackage(ProductosServicios)` permanecen V2 para impedir ejecución accidental. SQL real y runtime no autorizados.

- 2026-10-02 #MOKA LP-QA02: corrección visual local MVC ListaPrecios tras rechazo manual PO; API, schema, motor LP-08, Auth, Inventario y Legacy sin cambios. QA real `1440/820/390`, focal `179/179`, full `777/777`, Build MVC/node PASS. Estado exclusivo `CORRECCIÓN VISUAL IMPLEMENTADA / QA TÉCNICA PASS / PENDIENTE QA MANUAL DENISSE`; no aprobado PO ni FROZEN definitivo. Liberar `5127/5200` al finalizar QA local.

- 2026-10-02 #MOKA LP-QA01: API matricial tenant-safe sin N+1, descuento triestado, sucursales múltiples y preview/guardado matricial transaccional implementados. Estado exclusivo: `IMPLEMENTADO / QA TÉCNICA PASS / PENDIENTE QA MANUAL PO`; no declarar aprobado/frozen. Documento canónico en `../inspector/docs/lista-precios/LP_QA01_CORRECCION_QA_MANUAL_PO_20261002.md`.

- 2026-10-01 #MOKA LP-20C4 CERRADO: baseline historico Cotizaciones V1/V2 reconciliado con evidencia commit `83a2b21`; `IX_Cotizaciones_Empresa_Cliente` y `FK_CotizacionesPartidas_Cotizaciones` son objetos historicos obligatorios y se preservan en V2 junto con FK tenant-safe. Hashes vigentes: V1 `ae905dfc622135c5858fdf2c551287b38193fe3f92d4ccbe61eec374a1deb9b8`, V2 `5310c00e5991ed0e1c06bc84560a7d94b861b8ab1e310ad395a0303e9426e765`; anteriores superseded. Forward-fix oficial `COT-M20261001-V2-RECONCILE-HISTORICAL-OBJECTS` mantiene version comercial 2 y acepta exclusivamente el hash V2 superseded. CHECKAPPERP reconciliado y UMBRELLA 163 adoptada/migrada: SchemaOk/0, gates COMPATIBLE, 19 cotizaciones/70 partidas y fingerprint preservados, PRE_LP08 NULL, backfill 0, segunda corrida NO_PENDING_MIGRATIONS. Regresion 29/29, 38/38, 9/9, 36/36, 129/129, full 740/740 y builds PASS. Runtime Cotizaciones aun NO integrado; no ejecutar LP-21/LP-22. Archivos protegidos FROZEN.

- 2026-10-01 #MOKA LP-20B CERRADO local: scope oficial `Cotizaciones`, V1 historico exacto y V2 aditivo LP-08 implementados en contract provider, inventory, version provider, adopter, runner y gate oficiales. Hash V1 `b12ba03b8757a01a3922ca6bc8f75d451233336dc5d96309e4ca10233244aaa7`; V2 `e936afda626b83bafbc3c09091f721c94bffaa0957ef96f00fad9c0a38c1c8da`; package `COT-M20260930-V1-V2-LP08-SNAPSHOT`, transaccional/idempotente, sin DML comercial. V2 modela lista, cuatro identidades, snapshot, override auditado, clon, baja logica reutilizando `Activo` y `CotizacionesHistorial` propio. PRE_LP08 queda nullable y sin backfill. Focal 32/32, runner 35/35, ListaPrecios 129/129, full 725/725, builds PASS. SQL/DDL real NO; Controller/Program.cs/DI/tenant resolver runtime sin cambios; LP-21/LP-22 NO.

- 2026-09-30 #MOKA LP-20A contrato Cotizaciones consumidor LP-08: `CONTRATO EN REVISION PO / SIN IMPLEMENTACION`. Documento MVC `inspector/docs/lista-precios/LP_20A_CONTRATO_COTIZACIONES_CONSUMIDOR_LP08_20260930.md`. Diseno propone scope oficial `Cotizaciones`, baseline historico V1 validado y futura migracion aditiva `COT-M20260930-V1-V2-LP08-SNAPSHOT`; resolver LP-08 server-side al agregar mediante snapshot opaco firmado, conservar cantidad sobre snapshot y re-resolver al cambiar identidad. Los 19 documentos/70 partidas quedan `PRE_LP08`, sin backfill/recalculo. Lista encabezado + snapshot partida es recomendacion; override, descuento adicional, cambio lista, clonado y borradores son decisiones PO pendientes. No se agrego scope, contrato, paquete, DI ni gate; no DDL/migracion/codigo/datos. Cotizaciones no es consumidor certificado; LP-20 STOP PO, LP-19 bloqueado, LP-21/LP-22 no ejecutados.

- 2026-09-30 #MOKA LP-20 consumidores reales: `LP-20 = STOP PO / SIN CONSUMIDOR REAL CERTIFICABLE`. Auditoria codigo/SQL real UMBRELLA 163 identifico Cotizaciones como unico dominio comercial funcional, con 19 documentos y 70 partidas activas, pero NO certificable para LP-08: recibe precio/descuento del cliente, recalcula reglas propias, rechaza `0.00`, solo soporta Producto/Servicio, usa `SqlConnectionFactory` fijo sin resolver/gate ListaPrecios y carece de lista, Variante, PresentacionVenta y los 13 campos de snapshot LP-08 auditados. Integrarlo correctamente exige decisiones PO de schema y tenant; no se modifico API/Cotizaciones ni se ejecuto DDL. Ventas/Facturacion son placeholders, Pedidos no existe y OC/Recepcion/Inventario no consumen precio de venta. Documento MVC `inspector/docs/lista-precios/LP_20_CONSUMIDORES_PRECIOS_20260930.md`. LP-19 sigue BLOQUEADO; codigo productivo/SQL/schema/fixtures/datos modificados 0; LP-21/LP-22 NO ejecutados; FROZEN preservado.

- 2026-09-30 #MOKA LP-19D Ventas como contexto: `LP-19 = BLOQUEADO / DEPENDENCIA EXTERNA DOMINIO VENTAS CHECKAPP / SIN IMPLEMENTACION / SIN REGRESION SOBRE LP-01..LP-18`. Auditoria read-only UMBRELLA 163 sobre DatabaseIdentity saneada `DB_A883C3_CHECKLIST`: 162 objetos SQL inspeccionados y ninguna fuente canonica certificable de Ventas, tickets, devoluciones, pedidos o documentos de venta. No existe API, servicio, persistencia ni contrato canonico CheckApp de Ventas; los endpoints visuales MVC existentes son placeholders. Legacy `fma`, `detnotas` y `detdev` permanece referencia funcional read-only y queda prohibido como runtime. Inventario/OC/Recepcion no sustituyen Ventas. LP-19 es consumidor y solo puede reabrirse cuando el dominio Ventas certificado exponga tenant, identidades, sucursal, fecha, cantidad, devoluciones, permisos y API/servicio. Documento MVC `inspector/docs/lista-precios/LP_19_VENTAS_COMO_CONTEXTO_20260930.md`. LP-01..LP-18 permanecen CERRADOS/FROZEN. LP-20 NO ejecutado. Codigo productivo, SQL, DDL, schema y fixtures: 0; Auth, Inventario, ProductosServicios, Legacy y Ventas actual sin cambios.

- 2026-09-30 #MOKA LP-15R2 Copiar Lista CERRADO: hotfix backend minimo en `ListaPreciosService.PrepareCopyAsync` elimina la exigencia de fila activa previa para el destino canonico `1..10` y conserva origen real/tenant fail-closed; la ejecucion sigue creando el registro interno mediante `EnsureListaAsync` dentro de la transaccion `Serializable`. UMBRELLA 163 PASS para MERGE crear/omitir y SOBRESCRIBIR actualizar/omitir/reactivar con cuatro identidades, precio `0.00`, preview, confirmacion, `COPIA_LISTA`, correlacion, rollback y cross-tenant. Cleanup oficial 8 bajas `Activo 1 -> 0`, activos/residuos/datos legitimos/hard delete `0`, historial append-only. Focal `94/94`, full suite `660/660`, builds/node/diff/secret scan PASS. Documento MVC `inspector/docs/lista-precios/LP_15_COPIAR_LISTA_20260930.md`. Dictamen LP-15 CERRADO y listo handoff LP-16; LP-16 NO ejecutado. UI, `Utilerias.js`, `_Layout.cshtml`, `checkapp-ui.js`, Schema/migraciones, Auth, Inventario, ProductosServicios y Legacy permanecen FROZEN/sin cambios LP-15R2.

- 2026-09-30 #MOKA LP-14R2 Ajuste masivo de precios CERRADO: hotfix acotado en el repositorio compartido registra reactivacion `Activo 0 -> 1` con operacion `REACTIVACION`, origen `MASIVO` y correlacion comun solo cuando no cambia ningun valor comercial; evita duplicados y preserva `INDIVIDUAL`. QA autenticada UMBRELLA 163 PASS con lote Producto/Servicio, precio exacto `0.00`, preview `2/0`, ejecucion unica, historial y transaccion `Serializable`; rollback automatizado PASS y persistencia parcial `0`. Cleanup autorizado por PO mediante baja logica oficial: ambos fixtures `1 -> 0`, historial append-only, activos `0`, residuos `0`, datos legitimos/usuarios/roles modificados `0`. Focal `75/75`, full suite `640/640`, builds/node/diff/secret scan PASS. Dictamen: `LP-14 = CERRADO / AJUSTE MASIVO CERTIFICADO / HISTORIAL MASIVO CERTIFICADO / QA RUNTIME AUTENTICADA PASS / LISTO HANDOFF LP-15`. LP-15 NO ejecutado. `Utilerias.js`, `_Layout.cshtml`, `checkapp-ui.js`, Auth, Schema, ProductosServicios, Inventario y Legacy permanecen FROZEN/sin cambios.

- 2026-09-30 #MOKA LP-13R Existencias y movimientos integrados CERRADO: QA visual autenticada UMBRELLA 163 PASS sobre la API Inventory V1 existente; 10 identidades, 5 sucursales, 9 inventariables con existencia real `0`, servicio `N/A`, movimientos reales `0`, detalle y filtros runtime PASS. F5 y responsive exacto `1440/820/390` PASS; mobile `innerWidth=390`, `clientWidth=390`, `bodyScrollWidth=390`, modales `378` sin overflow. Focal `89/89`, full suite `619/619`, build API/MVC, node, diff y secret scan PASS. Sin SQL/DDL/fixtures, permisos/roles/usuarios ni datos legitimos modificados; codigo productivo LP-13R: 0 archivos. Dictamen: `LP-13 = CERRADO / EXISTENCIAS Y MOVIMIENTOS INTEGRADOS CERTIFICADOS / QA VISUAL AUTENTICADA PASS / LISTO HANDOFF LP-14`. LP-14 es el siguiente ticket y NO fue ejecutado. `Utilerias.js`, `_Layout.cshtml`, `checkapp-ui.js`, Auth, Schema, ProductosServicios y Legacy permanecen FROZEN/sin cambios.

- 2026-09-30 #MOKA LP-13 Existencias y movimientos integrados: API read-only Inventory V1 implementada y certificada contra UMBRELLA 163 por resolver normal; 10 identidades, 5 sucursales, 9 inventariables con existencia real 0, servicio N/A, movimientos 0 y cross-tenant 403. Consulta principal usa una sola agregacion de `InventarioSaldos`; detalle usa `InventarioMovimientos`; permiso existente `05001008`, sin schema/DDL/fixtures/roles/usuarios. Focal 89/89, full suite 619/619, builds/node/diff PASS. Cierre BLOQUEADO por QA visual autenticada `1440/820/390` al expirar sesion Chrome; LP-14 NO ejecutado. Documento MVC `inspector/docs/lista-precios/LP_13_EXISTENCIAS_MOVIMIENTOS_INTEGRADOS_20260930.md`.

- 2026-09-30 #MOKA LP-12 DynamicGrid operativo ampliado ListaPrecios CERRADO: documento `inspector/docs/lista-precios/LP_12_DYNAMICGRID_OPERATIVO_AMPLIADO_20260930.md`. No se modifico API productiva: el DTO LP-08 existente ya entrega `DescuentoPct` y `PrecioFinal`, ahora presentados por MVC sin formula cliente. QA autenticada UMBRELLA 163 PASS con 10 identidades, grid/columnas/sorting/paginacion/busqueda/Excel/F5/re-render y responsive `1440/820/390`. Focales `62/62`, `ListaPreciosServiceTests` `50/50`, full suite `613/613`, builds/node/diff/secret scan PASS. Test fuente LP-12 protege titulo mobile por `nombre`, columnas comerciales y ausencia de acciones LP-13/14/15/16/21. Sin SQL/DDL/schema/Auth/ProductosServicios/Legacy; LP-13 NO ejecutado.

- 2026-09-30 #MOKA LP-09R Edicion individual operativa ListaPrecios CERRADO: documento MVC `inspector/docs/lista-precios/LP_09_EDICION_INDIVIDUAL_OPERATIVA_20260930.md`. Runtime autenticado UMBRELLA 163 PASS: listas 1..10, modal Producto/Servicio/Variante/PresentacionVenta, preview sin persistencia, GuardarPrecio, Precio Lista `0.00` sin fallback, historial `INDIVIDUAL`, baja logica y cleanup UI activos `1 -> 0`. API mantiene motor LP-08, `IdDetallePrecio` para baja logica, `05001008` READ y `05001009` WRITE; sin endpoints LP-09 nuevos. Regresion PASS: pruebas focales `48/48`, full suite `599/599`, build API/MVC PASS, node/diff PASS, secret scan acotado PASS. SQL/DDL `NO`, credencial persistida `NO`, datos legitimos/usuarios/roles modificados `0`. Dictamen: `LP-09 = CERRADO / EDICION INDIVIDUAL OPERATIVA CERTIFICADA / QA VISUAL AUTENTICADA PASS / LISTO HANDOFF LP-10`. NO ejecutar LP-10 sin revision PO. `Utilerias.js`, `_Layout.cshtml` y `checkapp-ui.js` FROZEN sin diff.

- 2026-09-30 #MOKA LP-08C consolidacion documental pre-handoff LP-09: LP-08 queda CERRADO con dictamen `MOTOR COMERCIAL V2 CERTIFICADO / PASS TECNICO SQL REAL / LISTO HANDOFF LP-09`. Documento actualizado `inspector/docs/lista-precios/LP_08_API_MOTOR_V2_ADMINISTRACION_COMERCIAL_20260930.md`: CheckAppErp laboratorio tecnico V2 `SchemaOk/0`, Gate `COMPATIBLE`, sin residuos; UMBRELLA 163 tenant funcional real por Firebase/tenant resolver normal, DatabaseIdentity `SQL5111/DB_A883C3_CHECKLIST/E398ABAB-6416-4E68-8084-7FF7CB232EF5`, V2 hash `e7a388ec985a19fb2b3beb73e8c2cf28d5dda17d3f3bb2f0363683c166092882`, producto/servicio/variante/PresentacionVenta, precio 0, fallback, descuento, redondeo, vigencia, preview, guardar, historial, baja logica y cross-tenant fail-closed PASS; cleanup PASS; regresion `37/37` y `598/598`, builds/diff/secret scan PASS. LP-09 es el siguiente ticket (`EDICION INDIVIDUAL OPERATIVA`) y NO fue ejecutado. `checklist/wwwroot/js/Utilerias.js` y `checklist/Views/Shared/_Layout.cshtml` permanecen FROZEN / PROHIBIDO SIN AUTORIZACION PO; preservar ademas todos los FROZEN existentes.

- 2026-09-29 #MOKA LP-07R4 Lista de Precios hotfix migracion V1->V2 / SQL REAL CHECKAPPERP CERTIFICADO: documento `inspector/docs/lista-precios/LP_07_SCHEMA_VERSIONADO_COMERCIAL_20260929.md`. Defecto LP-07R3 corregido en package `LP-M20260929-V1-V2-COMERCIAL` con SQL dinamico para constraints/indices/FKs dependientes, sin modificar runner ni contrato V2. CheckAppErp `VPS3348900/CHECKAPPERP/A0E05B05-F509-44D3-8BE4-218F875720CA` queda V2 hash `e7a388ec985a19fb2b3beb73e8c2cf28d5dda17d3f3bb2f0363683c166092882`, drift `SchemaOk/0`, Gate `COMPATIBLE`, segunda corrida `NO_PENDING_MIGRATIONS`, fixtures reversibles PASS y cleanup 0. UMBRELLA 163 no migrado; no LP-08; sin secretos persistidos.

- 2026-09-29 #MOKA LP-BACKLOG-R1 Lista de Precios backlog corregido post LP-05R7: documentos `inspector/docs/lista-precios/LP_BACKLOG_CORREGIDO_POST_R7_20260929.md` y `inspector/docs/lista-precios/LP_DECISIONES_PO_POST_R7_20260929.md`. ListaPrecios evoluciona a CONSOLA OPERATIVA; descuentos, redondeos y promociones son obligatorios; LP-05 queda como base/fachada read-only y NO como modulo completo. No ejecutar LP-06 sin revision PO.

- 2026-09-29 #MOKA LP-06 Lista de Precios contrato funcional extendido comercial ejecutado / LISTO REVISION PO: documento `inspector/docs/lista-precios/LP_06_CONTRATO_FUNCIONAL_EXTENDIDO_COMERCIAL_20260929.md`. Cierra contrato canonico de precio base/original, Precio Lista, descuento, precio final, fuente de verdad, NULL/fila ausente, `0.00`, fallback, vigencia, redondeo y consumidores/snapshot. Legacy Raramuri/sazapi auditado read-only; redondeo Legacy identificado como Sin redondeo, A 4/9 y Solo a 9, aplicado despues del descuento, con clamp a precio base y modo no persistido. Promociones 2x1/3x2/descuento segundo/monedero quedan obligatorias como dominio, con decisiones PO pendientes para semantica transaccional, acumulabilidad, prioridad y alcance monedero. LP-07 NO ejecutado; sin schema/API/UI/BD/DDL/Legacy.

- 2026-09-29 #MOKA LP-06R cierre documental decisiones PO incorporado: documentos `inspector/docs/lista-precios/LP_06_CONTRATO_FUNCIONAL_EXTENDIDO_COMERCIAL_20260929.md` y `inspector/docs/lista-precios/LP_DECISIONES_PO_POST_R7_20260929.md`. PO cierra regla base 2x1 = compras 2 productos participantes y pagas 1; 3x2 = compras 3 productos participantes y el menor precio queda gratis; descuento segundo = porcentaje configurable por usuario, rango 0..100. Descuentos <0 o >100 se rechazan, no clamp. Monedero queda PENDIENTE DEFINICION FUNCIONAL; no schema/motor/UI operativa ni algoritmo. Promociones no sobrescriben Precio Lista. LP-07 NO ejecutado; sin codigo/BD/DDL/API/UI/Legacy.

- 2026-09-28 #MOKA LP-05 Lista de Precios pantalla base / fachada read-only: documento MVC `inspector/docs/lista-precios/LP_05_PANTALLA_BASE_LISTA_PRECIOS_20260928.md`. API agrega `GET api/ListaPrecios/Combos` y `GET api/ListaPrecios/Consulta` bajo `05001008` READ y Gate `ListaPrecios`. La consulta usa catalogos reales ProductosServicios y llama el motor LP-03 `ResolverPrecioAsync` por identidad para precio efectivo/origen; `PRECIO_LISTA` con precio `0.00` sigue configurado, no fallback. Sin modificar schema/DDL/V2, sin consumidores, sin Auth/Login/Firebase/Session/Cookies/Claims/Program.cs, sin Legacy, sin LP-06/LP-07.

- 2026-09-28 #MOKA LP-04R3 Lista de Precios Roles/Permisos certificado final / LISTO QA PO: documento actualizado `inspector/docs/lista-precios/LP_04_ROLES_PERMISOS_MENU_20260928.md`. Corrige LP-04R2: CheckAppErp es base QA tecnica para schema/gate/drift, no fuente de Usuarios/Roles/Permisos. Fuente real AuthZ: `db_a883c3_checklist` (`dbo.Usuarios`, `dbo.Roles`, `Roles.Permisos`, `Usuarios.idRol -> Roles.id`). Mecanismo QA historico reutilizado con fixtures SQL temporales `MOKA-LP04R3-*`, cleanup 0. NOACCESS, PARENTONLY, READONLY, WRITE y ADMINONLY certificados; `api/ListaPrecios` exige `05001008` READ antes de `05001009` WRITE para Guardar/Baja, de modo que admin sin access no salta navegacion/acceso. CheckAppErp `ListaPrecios` V1 `SchemaOk/0`, Gate `COMPATIBLE`. Suite 575/575 PASS, builds MVC/API PASS, node/diff PASS. Sin Firebase/Login/Auth/session/Program.cs/DDL/menu/Legacy/LP-05.

- 2026-09-28 #MOKA LP-04 Lista de Precios Roles/Permisos/Menu CERRADO / LISTO QA PO: documento `inspector/docs/lista-precios/LP_04_ROLES_PERMISOS_MENU_20260928.md`. Codigos oficiales agregados bajo `Proveeduria > Productos y Servicios`: `05001008` Lista de Precios acceso solo y `05001009` Administrar precios acceso+escritura. Padres no conceden hijos; roles existentes no reciben permisos automaticamente; SuperAdmin se resuelve por fusion aditiva oficial sin editar JSON. API `api/ListaPrecios` usa `05001008` para lectura/listas/precios/resolver y `05001009` WRITE para guardar/baja. El motor/servicio queda desacoplado para consumidores internos futuros con contrato PO. No DDL, no schema, no login/Auth flow, no Legacy, no consumidores, no backlog ni LP-05.

- 2026-09-28 #MOKA LP-03 Lista de Precios cerrado / PASS TECNICO API + SQL REAL CHECKAPPERP: documento `inspector/docs/lista-precios/LP_03_API_MOTOR_RESOLUCION_PRECIOS_20260928.md`. API tecnica `api/ListaPrecios` implementa lectura de listas/precios, guardar/baja logica y resolver precio efectivo. Motor V1 cubre producto, servicio, variante y PresentacionVenta con tenant server-side, Gate `ListaPrecios`, validacion exacta Variante->Producto y PresentacionVenta->Producto, precio `0.00` como configurado valido, fallback a PrecioPublico/Precio de presentacion y fail-closed para inactivos/cross-tenant/Variante+Presentacion. SQL real CheckAppErp `VPS3348900/CHECKAPPERP/A0E05B05-F509-44D3-8BE4-218F875720CA`: `SchemaOk/0`, Gate `COMPATIBLE`, fixtures reversibles y cleanup 0. No UI, no menu, no permisos/Auth nuevos, no consumidores, no Legacy, no backlog ni LP-04 sin autorizacion PO.

- 2026-09-28 #MOKA LP-02 Lista de Precios implementacion tecnica local completa / QA SQL REAL BLOQUEADO: documento `inspector/docs/lista-precios/LP_02_SCHEMA_V1_VERSIONADO_GATE_20260928.md`. Scope API `ListaPrecios` V1 hash `d4f0bbdc05f56c96026d6364f9c333799ccb0ddc9acdf8993fe040b92b48ce7e`; tablas `ListaPreciosListas` y `ListaPreciosDetalle`; versionado, inventory, gate, bootstrap/migracion V1 vacia y baseline habilitados. Decision PO: `Precio Lista = 0.00` es valido y no fallback; NULL/fila inexistente es no configurado. No UI, no LP-03, no consumidores, no permisos/menu/Auth, no Legacy. Tests contrato LP PASS 8/8. SQL real CheckAppErp no ejecutado porque `MOKA_CHECKAPPERP_QA_CONNECTION` no estuvo disponible; no declarar PASS SQL hasta credencial QA.

- 2026-09-25 #MOKA Patron CheckApp Catalogos V1 formalizado / APROBADO PO: documento MVC compartido `inspector/docs/pattern/PATRON_CHECKAPP_CATALOGOS_V1_20260925.md`. Golden Master SIMPLE/COMPACTO: PS-ACT-01S-R5 sobre Categorias, Marcas, Unidades de medida, Colecciones y Etiquetas. El Patron de Catalogos NO define ancho unico universal; cada nuevo catalogo debe clasificarse como SIMPLE/COMPACTO o AMPLIADO segun formulario real. API conserva AuthZ/Gate/idEmpresa/sanitizacion; no crear DDL, tablas, campos o endpoints por esta formalizacion. Sucursales es ejemplo conceptual ampliado, NO Golden Master ampliado ni implementacion autorizada. BL-03/OC/Recepcion/Curvas/T25/Reporte Lider/Legacy/Parte 2 permanecen FROZEN.

- 2026-09-24 #MOKA PS-ACT-01 PARTE 1 Catalogos ProductosServicios: documento `inspector/docs/productos-servicios/PS_ACT_01_PARTE1_CATALOGOS_COLECCIONES_ETIQUETAS_20260924.md`. API protege Catalogos con hijos granulares: Categorias `05001003`, Marcas `05001004`, Unidades de medida `05001005`, Colecciones `05001006`, Etiquetas `05001007`. Colecciones usa `ProductosServiciosColecciones` y `ProductosServicios.idColeccion`; Etiquetas usa `ProductosServiciosTags` + `ProductosServiciosProductoTags` many-to-many. Padres no conceden hijos; SuperAdmin sigue aditivo/protegido. BL-03/OC/Recepcion/Curvas/T25/Legacy permanecen FROZEN.

- 2026-09-23 #MOKA BL-03 FASE D OC-CUR-04 implementado tecnico / SQL REAL PASS / BLOQUEADO QA VISUAL AUTENTICADA LOCAL: documento `inspector/docs/compras/BL03_FASE_D_OC_CUR_04_CATALOGO_CURVAS_PATRON_CHECKAPP_20260923.md`. API `api/CurvasCatalogo` agrega listado/detalle/guardar/baja/reactivar/productos elegibles/variantes con AuthZ `05005001`, Gate `Curvas`, tenant server-side, transaccion, producto/variante validados y servicios rechazados. SQL real CheckAppErp PASS con Curvas V1 hash vigente, drift `SchemaOk/0`, Gate `COMPATIBLE`, CRUD fixture reversible, sin/con variante, servicio manipulado rechazado, baja/reactivacion, cross-tenant fail closed y cleanup 0. Suite/build PASS. No se declara LISTO QA PO porque no hubo sesion autenticada local para validar runtime visual. No Siembra UI, no Huecos UI, no Nueva OC, no Legacy; T25 y reporte lider FROZEN.

- 2026-09-23 #MOKA BL-03 FASE C OC-CUR-03 cerrado / PASS TECNICO SQL REAL CHECKAPPERP: documento `inspector/docs/compras/BL03_FASE_C_OC_CUR_03_MOTOR_HUECOS_COPETES_SUGERENCIAS_20260923.md`. API agrega preview read-only `api/CurvasSugerencias/Preview` con `ICurvasSugerenciasMotorService`; fuente de existencia `InventarioSaldos`, fuente de transito OC/Recepcion desde partidas OC activas/generadas/parciales con pendiente, excluyendo canceladas/servicios/otra sucursal/otro tenant. Formula canonica: `Cobertura=Existencia+Transito`, `Hueco=max(CurvaObjetivo-Cobertura,0)`, `Copete=max(Cobertura-CurvaObjetivo,0)`. Estados `SIN_CURVA/HUECO/COPETE/COMPLETA`; modos `Manual/Pedido inicial/Rellenar curva/No pedir`; producto con/sin variante; multisucursal aislada; cross-tenant fail closed. `PresentacionCompra.PermiteCantidadBase=0` redondea hacia arriba, `=1` conserva exacta. SQL real CheckAppErp PASS con fixtures reversibles y cleanup 0. No persistir snapshots desde preview; no UI final, no OCs hijas, no movimiento inventario, no Legacy, no OC-CUR-04; T25 y reporte lider FROZEN.

- 2026-09-23 #MOKA BL-03 FASE C OC-CUR-02 cerrado / PASS TECNICO SQL REAL CHECKAPPERP: scope `Curvas` V1 hash `85167e40a617c4535514563c03cfd3c49ec5c0f915f122b0d0c533779e88d4f5` con Catalogo, Detalle producto/variante nullable, Siembra, operacion agrupadora multisucursal, relacion operacion->OCs hijas y snapshots minimos. `OrdenesCompra` vigente es V2 hash `0977353cc806ec35d21c95c4149e16cdf41b3480d52b929b13ec82185957e802`; migracion aprobada `OC-M20260923-V1-V2-PRESENTACIONCOMPRA-CANTIDAD-BASE` agrega `OrdenesCompraPresentacionesCompra.PermiteCantidadBase` default 0. SQL real CheckAppErp saneado `VPS3348900/CHECKAPPERP/A0E05B05-F509-44D3-8BE4-218F875720CA`: State/History/Attempts presentes, segunda corrida idempotente, drift `SchemaOk/0`, Gate `COMPATIBLE`, fixtures reversibles, cross-tenant fail closed, OperationKey y cleanup 0. Redondeo PO resuelto: cerrada redondea hacia arriba, libre conserva exacta, usuario decide final. Sin UI, sin OC-CUR-03, sin Legacy, sin PresentacionVenta, sin permisos navegables definitivos; T25 y reporte lider FROZEN.
- 2026-09-21 #MOKA BL-03 SEC-01R incidente menu SuperAdmin restaurado / PASS TECNICO: documento `inspector/docs/compras/BL03_SEC01R_INCIDENTE_RESTAURACION_MENU_SUPERADMIN_20260921.md`. Causa: SEC-01R sustituyo el menu global SuperAdmin por `OfficialSuperAdminPermissions()` parcial de Proveeduria. Regla permanente: las extensiones de permisos/menu para SuperAdmin son estrictamente ADITIVAS; ningun resolver parcial de modulo puede sustituir el menu global ni eliminar opciones preexistentes; toda modificacion de permisos exige snapshot BEFORE/AFTER y prueba automatica de no perdida del menu completo. Fix: fusionar en memoria permisos oficiales de Proveeduria sobre `Roles.Permisos` real completo, sin modificar JSON ni quitar proteccion. Runtime: Checklists, Inspecciones, Ventas, Facturacion, Cotizaciones, Clientes, Activos, Proveeduria, Reportes y Ajustes restaurados; OC Nueva/Reporte visibles; Recepcion permisos ALLOW sin links UI.
- 2026-09-21 #MOKA BL-03 SEC-01R SuperAdmin corregido / PASS TECNICO: documento `inspector/docs/compras/BL03_REAPERTURA_SEC01R_ROLES_PERMISOS_OC_RECEPCION_20260921.md`. SuperAdmin es protegido/no editable y no debe requerir switches manuales del PO. Las nuevas opciones registradas oficialmente deben resolverse automaticamente mediante el mecanismo oficial (`ProveeduriaMenuBuilder.OfficialSuperAdminPermissions` / resolver oficial API). Denisse QA SuperAdmin resuelve `05003000/05003001/05003002/05004000/05004001/05004002` con `ALLOW`; escritura `05003001` y `05004001` con `ALLOW`; `Roles.Permisos` no se modifico. Menu SuperAdmin muestra ProductosServicios, Proveedores y Ordenes de compra > Nueva/Reporte; Recepcion tiene permiso efectivo, pero NO links hasta UI funcional. Roles normales siguen sin herencia de padre a hijos. OC-03 intacto.
- 2026-09-21 #MOKA BL-03 FASE B OC-03 cerrado / PASS TECNICO: documento `inspector/docs/compras/BL03_FASE_B_OC03_NUEVA_OC_PATRON_CHECKAPP_20260921.md`. Nueva OC existente `/Activos/OrdenesCompra/Nueva` conserva wizard 4 pasos y queda conectada a OC V1: productos, servicios, variantes, `OrdenesCompraPresentacionesCompra`, cantidad compra/base, factor snapshot, costo y OC mixta. API recalcula/valida snapshots server-side; crear/generar OC no mueve `InventarioSaldos`, `InventarioMovimientos` ni `InventarioSeries`. No usar `PresentacionesVenta`, `ProductosServiciosExistencias` ni `ProductosServiciosMovimientosInventario` como fuente operativa. No Recepcion UI, no Legacy, T25 y reporte lider FROZEN.
- 2026-09-21 #MOKA BL-03 FASE A REC-01 cerrado / PASS TECNICO: documento `inspector/docs/compras/BL03_FASE_A_REC01_MODELO_SCHEMA_RECEPCION_20260921.md`. Scope `Recepcion` V1 creado/certificado con hash `c26551d2eb625dda1138a2b5aad2ad83a3b074ea026082feb7714c97c229fd5f`; tablas `RecepcionFolios`, `Recepciones`, `RecepcionPartidas`, `RecepcionSeries`; API tecnica `api/Recepcion`; servicio `RecepcionScopeService`. SQL real tenant `163`: BEFORE/AFTER preserva OC=43, partidas=88, ProductosServicios=8, proveedores=3, sucursales=5; drift `SchemaOk` items 0; gate compatible; QA reversible: recepcion parcial + retry idempotente + segunda recepcion + servicio + variante + series + Inventario V1 con 3 movimientos y 2 saldos; cleanup PASS. No usar `ProductosServiciosExistencias` ni `ProductosServiciosMovimientosInventario` como fuente de verdad. No UI final, no Legacy, T25 y reporte lider FROZEN.
- 2026-09-21 #MOKA BL-03 FASE A INV-02 cerrado / PASS TECNICO: documento `inspector/docs/compras/BL03_FASE_A_INV02_LIMPIEZA_HISTORICO_INVENTARIO_20260921.md`. Decision PO aplicada: eliminar 10 unidades historicas de `Aceite Motor Sintetico`, sin `UNKNOWN`, sin sucursal ficticia y sin movimientos inventados. SQL real tenant `163`: `ProductosServiciosExistencias` 1 -> 0, `ProductosServiciosMovimientosInventario` 0 -> 0, segunda ejecucion 0, maestros/OC/partidas preservados. `ProductosServiciosController` ya no referencia tablas legacy de inventario; lee stock desde `InventarioSaldos`, movimientos desde `InventarioMovimientos`, bloquea endpoints legacy de movimiento por falta de sucursal/origen/idempotencia y no recrea existencias legacy al guardar. INV-01 queda desbloqueado y `CERRADO / PASS TECNICO`; Inventario V1 es fuente operativa. REC-01 no ejecutado; T25 y REPORTE LIDER siguen FROZEN.
- 2026-09-21 #MOKA BL-03 FASE A ARQ-01 cerrado como recomendacion tecnica, no decision PO: documento `inspector/docs/compras/BL03_FASE_A_ARQ01_INVENTARIO_VARIANTE_SUCURSAL_20260921.md`. Auditoria SQL real read-only: ProductosServicios=8, Existencias=1 por `idEmpresa+idProductoServicio`, MovimientosInventario=0, Variantes=4, PresentacionesVenta=57, Sucursales=136, OrdenesCompra=43, OrdenesCompraDetalle=88. Brecha confirmada: existencia/movimiento actual no tiene SucursalId ni VarianteId. `RECOMENDACION_ARQ01`: scope `Inventario` separado con ledger + saldo materializado por `idEmpresa+SucursalId+ProductoServicioId+VarianteId nullable`, cantidades en unidad base y PresentacionCompra como snapshot. `REQUIERE_DECISION_PO_INVENTARIO_VARIANTE_SUCURSAL` sigue pendiente de PO; no DDL, no migraciones, no UI, no Legacy, no OC-02/REC-01; T25 y REPORTE LIDER siguen FROZEN.
- 2026-09-21 #MOKA BL-03 FASE A SEC-01 cerrado: documento `inspector/docs/compras/BL03_FASE_A_SEC01_PERMISOS_CODIGOS_OC_RECEPCION_20260921.md`. Codigos finales: `05003000` Ordenes de compra agrupador solo Acceso, `05003001` Nueva OC Acceso+Escritura, `05003002` Reporte OC Acceso+Escritura para acciones mutables; `05004000` Recepcion agrupador solo Acceso, `05004001` Nueva Recepcion Acceso+Escritura, `05004002` Reporte Recepcion Acceso+Escritura futura. Preflight SQL real read-only: 124 roles, codigos nuevos en 0 roles/0 empresas; sin colisiones persistidas. Padres NO conceden hijos. Menu, RolesPermisos, MVC y API OC usan permiso funcional especifico. Sin DDL, sin Legacy, sin normalizacion masiva, sin ARQ-01/OC-02/REC-01; T25 y REPORTE LIDER siguen FROZEN.
- 2026-09-21 #MOKA BL-03 FASE A OC-01 cerrado como contrato funcional canonico: documento `inspector/docs/compras/BL03_FASE_A_OC01_CONTRATO_FUNCIONAL_OC_RECEPCION_20260921.md`. Decisiones PO congeladas: `PresentacionCompra` separada de `PresentacionVenta` = APROBADA (Opcion C); Recepcion independiente dentro de Proveeduria = APROBADA; OC admite Producto + Servicio + Mixta + Variantes; Series vigentes solo en Recepcion para producto/variante con control serial; Recepcion parcial = SI; sobre-recepcion = NO por ahora; crear/generar OC no mueve inventario; confirmar recepcion de producto inventariable si mueve inventario. `REQUIERE_DECISION_PO_INVENTARIO_VARIANTE_SUCURSAL` queda pendiente tecnico para ARQ-01. API futura debe mapear este contrato por scope/versionamiento, AuthZ y transaccion idempotente; OC-01 no autoriza UI/API/DDL, no inventa codigos de permisos y no ejecuta SEC-01/ARQ-01/OC-02/REC-01.
- 2026-09-17 #MOKA: los PASS visuales previos de Sucursales/RazonesSociales/Regiones quedaron invalidados por QA manual PO. La homologacion debe partir del Golden Master literal `/ProductosServicios/Index` y de `inspector/docs/pattern/PATRON_CHECKAPP_GOLDEN_MASTER_COMPONENT_MATRIX_20260917.md`. API no se modifica para correcciones visuales salvo requerimiento funcional expreso; sin Firebase, Hosting, Conexiones ni T25.
- 2026-09-17 #MOKA catalogos simples: cuando la UI sea equivalente a catalogo administrativo simple, el Golden Master especifico es `ProductosServicios -> quick-add -> Nueva categoria`; no introducir segunda card interna, copy tecnico decorativo ni huecos artificiales. Backend conserva AuthZ/Gate/idEmpresa.
- 2026-09-18 #MOKA DynamicGrid catalogos: Golden Master literal `/ProductosServicios/Categorias`. Sucursales/Razones/Regiones deben exponer baja logica/reactivacion por API, filtro `estatus`, conteos activos default 5/1/5, AuthZ especifico, Scope Sucursales, Gate compatible, `DatabaseIdentity` y `idEmpresa` server-side. No hard delete ni desactivar AuthZ/Gate.
- 2026-09-18 #MOKA acciones DynamicGrid: mejora visual frontend-only. `Editar`, `Dar de baja` y `Reactivar` deben usar el action-group oficial `.ps-catalog-actions`; API no debe reinterpretar esta regla ni convertir baja logica en hard delete.
- Antes de tocar API/contratos relacionados con el patron CheckApp, leer `inspector/docs/pattern/PATRON_CHECKAPP_OFICIAL_20260916.md`.
- ProductosServicios es la pantalla base oficial; backend conserva autoridad final de permisos, sanitizacion y schema gate.
- T25 permanece `FROZEN`; no iniciar T25, Hosting, Firebase, Conexiones, bases QA ni bootstrap.
- En trabajo local con servidores MVC/API, liberar y verificar puertos `5200` y `5127` al terminar solo si Codex los inicio; no cerrar procesos preexistentes del Product Owner.
- Excepcion PO vigente para MOKA DynamicGrid 2026-09-18: si el ticket usa `5200`/`5127`, liberarlos al final y verificar `lsof` sin listeners.

# PRODUCTOSSERVICIOS_SCHEMA_V2_DESCRIPCIONES_HTML - 2026-09-16

- PO resolvio `REQUIERE_DECISION_PO_TIPO_DESCRIPCION`: usar `NVARCHAR(MAX)` para descripciones HTML de `ProductosServiciosCategorias`, `ProductosServiciosMarcas` y `ProductosServiciosColecciones`.
- V1 historico queda inmutable con hash `4d51ce43a30ec3d583ce4252c8324f1053a52fdd89a7d80b3e01a6e8b780fb06`; V2 vigente hash `1b5c75e4b44fcfcb3af4219660095ddb2a99419db8da300aff6e4a38c731a705`.
- Migracion aprobada: `PS-M20260916-V1-V2-DESCRIPCIONES-NVARCHAR-MAX`, solo por T17/T19 con History/Attempts/State y validacion T18. No usar ALTER aislado.
- Base real 163 migrada: `CurrentVersion=2`, T18 `SchemaOk`, `DriftCount=0`, segunda corrida `NO_PENDING_MIGRATIONS`, datos preservados. T25 sigue `FROZEN`.

# POST-T24 CONSOLIDADO / T25 FROZEN - 2026-09-16

- PRODUCTOSSERVICIOS_SCHEMA_UNKNOWN_RUNTIME_REGRESSION, 2026-09-16: tenant real Denisse/empresa `163` resuelto desde Firebase `Conexiones/163` a `SQL5111/DB_A883C3_CHECKLIST/E398ABAB-6416-4E68-8084-7FF7CB232EF5`, `idEmpresa=b17aaece-2b78-4e35-b554-9e694eeb15a7`. BEFORE: T14 tables existian, pero `CheckAppSchemaState` no tenia fila para `DatabaseIdentity+ProductosServicios`; T13/T20 bloqueaban como `SCHEMA_UNKNOWN`, `CurrentVersion=NULL`, `SchemaResult=N/A`. T18 read-only confirmo V1 exacto: 20 tablas, 255 columnas, 50 indices, 24 FK, 14 CHECK, hash `4d51ce43a30ec3d583ce4252c8324f1053a52fdd89a7d80b3e01a6e8b780fb06`, drift 0.
- Correccion controlada: se implemento `IProductosServiciosHistoricalBaselineAdopter` como operacion administrativa trazada. Solo adopta bases historicas `Unknown/VERSION_EVIDENCE_MISSING` con scope completo y T18 `SchemaOk`; registra `Attempt ADOPT_BASELINE`, `History ADOPTED` y `State CurrentVersion=1` con baseline `PRODUCTOSSERVICIOS_V1_HISTORICAL_BASELINE`. No ejecuta DDL ni reconstruye schema.
- AFTER real 163: `CurrentVersion=1`, manifest hash V1, T13 `Current/VERSION_EVIDENCE_CURRENT`, T20 `COMPATIBLE`, T18 `SchemaOk`, drift 0; endpoints `ObtenerProductosServicios` y `ObtenerCombosProductosServicios` respondieron 200 sin `SCHEMA_UNKNOWN` ni `SCHEMA_SERVICE_UNAVAILABLE`.
- AuthZ corregido: ProductosServicios ya no usa fallback fijo `ConnectionStrings:CadenaConexionSQLServer`; lee Roles/Permisos usando `TenantDatabaseDescriptor` resuelto server-side. Agrupadores `05000000`, `05001000`, `05001002` son acceso-unicamente: aunque exista `Escritura=1` legacy, no conceden WRITE. ABC WRITE depende de `05001001.Acceso=1` + `05001001.Escritura=1`.
- Denisse QA: `denisse@checkapp.com.mx`, SQL id `aaf77600-e70c-44df-8257-ba2cca7098ba`, Firebase UID `JqXJBMdaZMYnZ88QaGLYodyinKf2`, rol `SuperAdmin`. Protección SuperAdmin en UI/backend se conserva. No Firebase, no Hosting, no Conexiones, no DDL, no schema, no `nxt_*`; T25 sigue `FROZEN`.

- Documento consolidado: `inspector/docs/database/POST_T24_CORRECCIONES_PERMISOS_Y_CONGELAMIENTO_T25_20260916.md`.
- T24: IMPLEMENTADO Y CERTIFICADO. T25: `FROZEN / NO INICIADO FORMALMENTE`; reanudar solo con autorizacion explicita del Product Owner.
- PRODUCTOSSERVICIOS - PERMISOS: cada opcion navegable independiente requiere permiso propio. Agrupadores: Acceso unicamente. Pantallas funcionales: Acceso + Escritura cuando corresponda. Padres NO conceden hijos.
- Arbol definitivo: `05000000` Proveeduria agrupador; `05001000` Productos y Servicios agrupador; `05001001` ABC; `05001002` Catalogos agrupador; `05001003` Categorias; `05001004` Marcas; `05001005` Unidades de medida.
- Backend es autoridad final: `05001000.Escritura` legacy se ignora; ABC WRITE usa `05001001.Acceso=1` + `05001001.Escritura=1`; aplicar equivalente a Categorias/Marcas/Unidades.
- SuperAdmin permanece protegido contra edicion manual. No usar otro rol como workaround para SuperAdmin. Denisse QA autorizada debe resolver SuperAdmin mientras esa sea la regla vigente del entorno.
- T25 freeze: NO ejecutar, NO preparar, NO modificar Hosting, NO modificar bases QA, NO provisionar, NO iniciar bootstrap, NO iniciar QA manual. No cambiar codigos `05000000`-`05001005` sin autorizacion PO.

# PRODUCTOSSERVICIOS_PERMISSION_GRANULARITY_REGRESSION - 2026-09-16

- ProductosServicios usa permisos granulares: `05000000` Proveeduria, `05001000` Productos y Servicios, `05001001` ABC Productos y Servicios, `05001002` Catalogos, `05001003` Categorias, `05001004` Marcas, `05001005` Unidades de medida.
- El padre `05001000` es agrupador de solo Acceso y no autoriza hijos automaticamente. Menu/Home, MVC directo, proxy y API deben exigir el codigo especifico; ausencia de permiso granular falla cerrado. Si `05001000.Escritura` existe en JSON legacy, se ignora y no concede WRITE.
- API centraliza la decision en `IProductosServiciosAuthorizationService` recibiendo el `PermissionCode` especifico por accion; READ exige `Acceso=1` y WRITE exige `Acceso=1` + `Escritura=1`.
- RolesPermisos conserva la proteccion SuperAdmin: no permitir edicion manual ni eliminar el mensaje "No se pueden cambiar los permisos del SuperAdmin".
- Control: no DDL, no Firebase, no Hosting, no Conexiones tenant, no `nxt_*`, no asignacion masiva. Al terminar cualquier QA/trabajo local, liberar y verificar puertos 5200 y 5127.
- QA vigente de esta regresion: tests API/MVC `403/403` PASS, builds API/MVC PASS, `git diff --check` PASS.

# Estado T24 - QA integral T11-T23 ProductosServicios #MOKA, 2026-09-14

- T24 certificado: T11-T23 PASS en conjunto, fuente real Roles/Permisos `db_a883c3_checklist`, codigos `05000000`/`05001000` libres, NOACCESS/READONLY/WRITE reales PASS y fixtures restaurados (6 filas eliminadas, 0 remanentes).
- CheckAppErp final PASS: CurrentVersion 1, hash V1 `4d51ce43a30ec3d583ce4252c8324f1053a52fdd89a7d80b3e01a6e8b780fb06`, T18 SchemaOk, DriftCount 0, T20 COMPATIBLE, conteos 20/255/50/24/14, objetos temporales MOKA 0.
- Correcciones cerradas: AuthZ ya no trata `05001000` como pendiente y lee Roles/Permisos desde `ConnectionStrings:CadenaConexionSQLServer` legacy server-side, con fallback sólo para pruebas.
- Regresion final PASS: tests 388/388, build API PASS, build MVC PASS, diff check PASS, secret scan `SECRET_HITS=0`. Runtime PO 5200/5127 respetado; MVC PS sin sesion redirige a Login y API sin contexto devuelve 401.
- No DDL, no schema changes, no Firebase, no Hosting, no Conexiones tenant, no `nxt_*`, no asignacion masiva, no roles productivos ajenos, no T25. Documento: `inspector/docs/database/TICKET24_QA_INTEGRAL_MULTITENANT_PRODUCTOSSERVICIOS_20260914.md`.

# Estado T23 - certificacion AuthZ final fuente real Roles/Permisos #MOKA, 2026-09-14

- Fuente real Roles/Permisos: conexion legacy configurada `ConnectionStrings:CadenaConexionSQLServer`, base `db_a883c3_checklist`; contiene `dbo.Usuarios`, `dbo.Roles`, `Roles.Permisos`, 124 roles y joins `Usuarios.idRol` -> `Roles.id`.
- Codigos `05000000` y `05001000` libres en SQL real. QA con tres roles/usuarios temporales: NOACCESS deny READ/WRITE, READONLY allow READ deny WRITE, WRITE allow READ/WRITE, otra empresa no autoriza. Cleanup: 6 filas eliminadas, 0 remanentes.
- AuthZ service corregido para leer Roles/Permisos desde la fuente legacy configurada; `05001000` ya no se trata como pendiente.
- CheckAppErp final: V1, hash V1, SchemaOk, DriftCount 0, T20 COMPATIBLE; no contiene ni recibe Roles/Usuarios.
- Regresion final PASS: tests 388/388, build API PASS, build MVC PASS, diff check PASS, secret scan `SECRET_HITS=0`. No DDL, schema, Firebase, Hosting, Conexiones tenant, asignacion masiva ni T24.

# Estado T23 - certificacion SQL real con credencial PO #MOKA, 2026-09-14

- Credencial QA PO usada solo en memoria para SQL real CheckAppErp; no persistida. Secret scan repo: `SECRET_HITS=0`.
- CheckAppErp no contiene `dbo.Roles`, `dbo.Usuarios` ni columnas `Permisos`; `05000000`/`05001000` no tienen uso funcional en esa base, pero no existe ahi el mecanismo `Roles.Permisos` para certificar fixtures AuthZ reales. No crear DDL/schema.
- CheckAppErp final: CurrentVersion 1, ManifestHash V1 `4d51ce43a30ec3d583ce4252c8324f1053a52fdd89a7d80b3e01a6e8b780fb06`, T18 SchemaOk, DriftCount 0, T20 COMPATIBLE.
- Correccion aplicada: `ProductosServiciosAuthorizationService` ya no trata `05001000` como codigo pendiente.
- Regresion final: tests 388/388 PASS, build API PASS, build MVC PASS, git diff --check PASS. No Firebase, Hosting, Conexiones tenant, roles productivos, asignacion masiva, DDL, schema, nxt_* ni T24.

# Estado T23 - certificacion SQL final solicitada #MOKA, 2026-09-14

- Intento de cierre SQL final ejecutado sin imprimir secretos. `MOKA_CHECKAPPERP_QA_CONNECTION` no estuvo disponible en proceso, zsh login ni launchctl; no usar connection string fija.
- SQL real no ejecutado: no se validaron `05000000`/`05001000` en `Roles.Permisos`, no se persistieron fixtures QA y no se confirmo CheckAppErp final en esta ejecucion.
- Regresion local final PASS: tests 388/388, build API PASS, build MVC PASS, `git diff --check` PASS en ambos repos.
- Control preservado: no DDL, no schema, no Firebase, no Hosting, no Conexiones tenant, no `nxt_*`, no asignacion masiva, no T24.

# Estado Ticket 23 - AuthZ ProductosServicios #MOKA cierre final, 2026-09-14

- Decision PO aplicada: `05000000` Proveeduria / `05001000` Productos y Servicios. API, MVC, menu y tests usan `ProductosServicios:PermissionCode=05001000`; `02000000` queda excluido como Inspecciones y no autoriza PS.
- AuthZ server-side implementada con `IProductosServiciosAuthorizationService` / `ProductosServiciosAuthorizationService`: lee `Usuarios.idRol` a `Roles.Permisos` JSON por empresa autorizada, evalua `Acceso`/`Escritura` de forma recursiva y fail-closed; API bloquea antes de T20/T22/SQL de negocio. MVC y Home/menu bloquean/ocultan por `05001000.Acceso`, sin reemplazar al backend.
- Matriz vigente: 51 endpoints identificados; 24 READ y 27 WRITE. READ requiere Acceso; WRITE requiere Acceso+Escritura. NOACCESS/MISSING/02000000 bloquean, READONLY bloquea WRITE antes de gate/SQL.
- Preflight local: sin colision funcional exacta de `05000000`/`05001000`; unico hallazgo relacionado `m05001000` en breadcrumb/menu legacy de configuracion, no codigo JSON exacto. SQL preflight y persistencia real no ejecutados porque `MOKA_CHECKAPPERP_QA_CONNECTION` no esta disponible; no usar connection string fija ni modificar roles productivos sin conexion QA autorizada.
- QA local: `dotnet test inspectorapi/checklistWs.sln --no-restore --verbosity minimal` PASS 388/388; builds API/MVC PASS; `git diff --check` PASS en `inspectorapi` e `inspector`. No T24.

# Estado Ticket 23 — Seguridad y contexto multitenant, 2026-09-14

- T23 NO CERRADO: implementación independiente terminada; REQUIERE_DECISION_PO para identificar permiso/política funcional de ProductosServicios. MVC sólo tenía Authorize y API no aplica AuthZ funcional; el menú directo no define un permiso. No inventar códigos ni equivalencia autenticado=autorizado. La mención histórica de «autorización» en T22 no certifica AuthZ funcional.
- API exige autenticidad, sujeto/empresa/GUID consistentes, firma y ventana existente; rechaza claims sin autenticación y contradicciones. MVC valida claims/sesión y conserva UID Firebase string. Contexto canónico/init-only; query/form/JSON no sustituyen tenant; conexión/identity/scope siguen server-side. No nuevo esquema crypto/nonce/Login.
- Logs de controller/resolver/gate saneados; identidad textual de T20/T22 sustituida por digest de correlación. T11–T22 conservan comportamiento, incluido NO_REQUIRED_COMPANY_SEEDS. Fixtures T22 añaden sujeto requerido; no se crean semillas.
- Tests 384/384 PASS (105 T23 nuevos, 279 previos); builds API/MVC PASS, 0 errores; warnings de dependencias existentes. Los tests no sustituyen el requisito AuthZ pendiente. 51 acciones HTTP auditadas y probadas sin identidad antes de SQL.
- QA SQL real CheckAppErp con firma sintética/lector T11 fixture A/B: 7 filas propias creadas/eliminadas; hashes originales restaurados; listado/ficha/PDF propios 200, ajenos 404, payloads discrepantes bloqueados. T22 15 llamadas NO-OP. T20 BLOCK 503 con gate fixture, sin DDL. V1/hash intacto, SchemaOk, drift0, COMPATIBLE, 20/255/50/24/14.
- Sin Firebase/Hosting/Conexiones data/schema/otros verticales/T24/T25; sin commit/push ni reinicio de puertos existentes. Login sólo perímetro 302 y archivos intactos; sin E2E Firebase. Multimedia certificada en guards/ownership API, no acceso directo a Storage.
- Informe y checklist completo 98: `inspector/docs/database/TICKET23_SEGURIDAD_CONTEXTO_MULTITENANT_20260914.md`; runner y evidencia en `inspector/docs/database/t23-qa/`. Resolver regla AuthZ, implementar y repetir QA antes de cerrar T23 o iniciar T24. Este bloque es el estado actual; las entradas siguientes son históricas.

# Estado Ticket 22 — Bootstrap empresarial separado del schema, 2026-09-14

- T22 IMPLEMENTADO Y CERTIFICADO en la rama NO-OP expresamente aprobada: PS no requiere semillas predefinidas al incorporar empresa. Categoría/unidad son obligatorias al guardar artículo y se crean/seleccionan por CRUD; no inventar valores por alta.
- IProductosServiciosCompanyBootstrapper / ProductosServiciosCompanyBootstrapper exige descriptor server-side y T20 compatible, retorna NO_CHANGES/NO_REQUIRED_COMPANY_SEEDS con cero items. Se integra después de autorización/resolución/T20 en el controller API, antes del CRUD; sin endpoint nuevo ni autoridad cliente. Conserva gate del controller y verifica otra vez en servicio, con coste adicional de metadata conocido.
- Sin escritores, estado persistido, transacción empresarial ni locks en NO-OP; no escribe State/History/Attempts. Parciales/rollback/creación por seed son N/A para conjunto obligatorio vacío, no pruebas de operaciones ficticias. T16 EMPTY sigue separado.
- QA automática 279/279 PASS, incluyendo 32 T22 y 247 anteriores. Builds API/MVC PASS; git diff --check en los dos repositorios PASS. PIDs preexistentes 49365/49371 y puertos 5127/5200 respetados.
- QA SQL real CheckAppErp PASS: 15 llamadas C/D incluyendo 10 repeticiones y concurrencia C/C,C/D; T22 creó 0 filas y ejecutó 0 DDL. Dos categorías fixture A/B preservadas durante T22 y eliminadas exclusivamente por sus IDs; hashes originales restaurados, sin productos demo ni tenants Firebase nuevos.
- V1/hash inalterados; snapshot físico y datos/control idénticos. Final 20 tablas/255 columnas/50 índices/24 FK/14 CHECK, SchemaOk, drift 0, T20 COMPATIBLE. Perímetro HTTP 302 Login/401 API; controller actual SQL: listado/catálogos 200 vacíos, ficha ausente 404, discrepancia tenant 403. No se realizó login Firebase interactivo ni ficha/PDF poblada sin productos QA.
- No se modificaron Firebase, Hosting, Conexiones, Auth, schema V1, servicios T11–T21, otros verticales ni MVC. T23+ sin iniciar; sin commit/push. Pendiente QA/cierre del PO, no implementación T22.
- Informe y checklist 85: `/Users/denissemendiola/dev/Inspecciones/inspector/docs/database/TICKET22_BOOTSTRAP_EMPRESARIAL_SEPARADO_SCHEMA_20260914.md`; evidencia y runner saneados en `inspector/docs/database/t22-qa/`. Esta entrada actualiza el estado T22 sin borrar bitácoras previas.

# Estado Ticket 21 — Integridad multitenant por idEmpresa ProductosServicios, 2026-09-14

- TICKET 21 IMPLEMENTADO Y CERTIFICADO: el CRUD de ProductosServicios quedó auditado/reforzado para que dos empresas en la misma `DatabaseIdentity` no puedan leer ni modificar datos entre sí.
- La autoridad de empresa sigue en `context.IdEmpresa` resuelto server-side desde T11/T12; el `idEmpresa` cliente no puede cambiar la empresa efectiva y se rechaza si contradice el contexto.
- Se reforzaron actualizaciones sensibles de inventario/existencias con `WHERE idEmpresa = @IdEmpresa AND id = @Id`, y se validaron IDs anidados de multimedia, atributos, opciones, valores y variantes contra `idEmpresa + producto padre` antes de sincronizar.
- Auditoría de 49 endpoints PASS: 45 resuelven contexto directamente antes de SQL y 4 exportaciones delegan a endpoints ya protegidos. SELECT, INSERT, UPDATE, bajas, activaciones, exportaciones, inventario y relaciones quedan cubiertos.
- QA automatizado API PASS: suite completa 247/247; pruebas T21 específicas 23/23. Build API PASS; build MVC PASS. Este checkout no contiene `.git`, por lo que se sustituyó `git diff --check` por revisión de whitespace en archivos tocados, sin hallazgos.
- QA SQL real CheckAppErp PASS: empresas QA `3bdad8ea-c040-443e-8aed-7e542abcbc1a` y `39eb2199-f030-4c17-b655-1e75bbf2d16a` compartieron `VPS3348900/CHECKAPPERP/A0E05B05-F509-44D3-8BE4-218F875720CA`; A no pudo listar, consultar, actualizar, dar de baja ni resolver relaciones de B; B permaneció intacta; fixtures limpiadas.
- T20 preservado: gate `True/COMPATIBLE`, `CurrentVersion=1`, hash V1 `4d51ce43a30ec3d583ce4252c8324f1053a52fdd89a7d80b3e01a6e8b780fb06`, `SchemaOk`, drift 0. Auth runtime preservado: MVC 5200 redirige a Login y API 5127 responde 401 sin credenciales.
- No se ejecutó DDL, no se crearon migraciones, no se creó V2, no se adelantó T22, no se modificó Firebase, Hosting, Login/Auth, permisos, sesión, datos reales de negocio ni bases históricas. No se persistieron secretos.
- Dictamen: `TICKET 21 IMPLEMENTADO Y CERTIFICADO — AISLAMIENTO MULTITENANT DE PRODUCTOSSERVICIOS POR idEmpresa OPERATIVO — SELECT/INSERT/UPDATE/BAJAS Y RELACIONES CERTIFICADOS — DOS EMPRESAS EN LA MISMA DATABASEIDENTITY NO PUEDEN LEER NI MODIFICAR DATOS ENTRE SÍ — idEmpresa DEL CLIENTE NO ES AUTORIDAD — T20 PRESERVADO — SIN DDL, MIGRACIONES NI ADELANTO DE T22 — LISTO PARA QA/Cierre DEL PRODUCT OWNER.`
- Documento: `/Users/denissemendiola/dev/Inspecciones/inspector/docs/database/TICKET21_INTEGRIDAD_MULTITENANT_IDEMPRESA_20260914.md`.

# Estado Ticket 20 — Gate server-side de compatibilidad ProductosServicios, 2026-09-14

- TICKET 20 IMPLEMENTADO Y CERTIFICADO: se agregó `IProductosServiciosCompatibilityGate` / `ProductosServiciosCompatibilityGate` para decidir en API si el CRUD de ProductosServicios puede operar sobre la base SQL resuelta.
- La decisión exige `DatabaseIdentity + Scope` válidos, `CurrentVersion=1`, hash T15 V1 `4d51ce43a30ec3d583ce4252c8324f1053a52fdd89a7d80b3e01a6e8b780fb06`, ausencia de attempts activos T16/T17/T19 y T18 `SchemaOk` sin drift.
- Estados bloqueados de forma controlada: `EMPTY`, `PARTIAL`, `OUTDATED`, `FUTURE`, `UNKNOWN`, `UNAVAILABLE`, evidencia de versión faltante, versión incompatible, hash distinto, bootstrap/preparing, migración en progreso, drift, drift crítico, validación inconclusa y requiere revisión.
- El controlador API de ProductosServicios invoca el gate antes de ejecutar SQL de negocio; ante incompatibilidad responde 503 saneado con `code`, `message` y `referenceId`. Auth 401/403 existente se preserva antes del flujo tenant/base.
- El MVC no decide compatibilidad ni puede simular ALLOW; conserva el proxy y muestra/propaga el mensaje controlado de API. No se movió lógica de negocio al frontend.
- El gate no ejecuta DDL, no provisiona, no migra, no repara, no actualiza `CheckAppSchemaState`, no escribe `History` ni crea `Attempts`, y no usa cache permisiva.
- QA automatizado API PASS: suite completa 224/224; tests T20 específicos 29/29. Build API PASS, build MVC PASS, `git diff --check` PASS, secret scan T20 PASS; único match fue `ApiKey` como nombre de propiedad/configuración existente, sin valor secreto literal.
- QA SQL real CheckAppErp PASS: BEFORE `IsAllowed=True/COMPATIBLE`, `CurrentVersion=1`, hash V1, `SchemaOk`, drift 0. Drift fixture real sobre `IX_ProductosServiciosMovimientos_Empresa_FechaMovimiento` bloqueó `IsAllowed=False/SCHEMA_DRIFT`; índice restaurado a contrato; AFTER `IsAllowed=True/COMPATIBLE`, `SchemaOk`, drift 0.
- No se creó V2, no se adelantó T21, no se modificó Firebase, Hosting, Login/Auth, permisos, sesión, datos de negocio ni bases históricas. No se persistieron secretos.
- Dictamen: `TICKET 20 IMPLEMENTADO Y CERTIFICADO — GATE SERVER-SIDE DE COMPATIBILIDAD DE PRODUCTOSSERVICIOS OPERATIVO — CRUD PERMITIDO ÚNICAMENTE CON DATABASEIDENTITY + SCOPE COMPATIBLES — ESTADOS EMPTY/PARTIAL/OUTDATED/FUTURE/UNKNOWN/DRIFT/PREPARING/MIGRATING BLOQUEADOS DE FORMA CONTROLADA — CHECKAPPERP V1 / SCHEMA_OK CONFIRMADA COMO ALLOW — RESPUESTAS HTTP/MVC SANEADAS — SIN DDL, MIGRACIÓN NI REPARACIÓN DESDE EL GATE — SIN ADELANTAR T21 — LISTO PARA QA/Cierre DEL PRODUCT OWNER.`
- Documento: `/Users/denissemendiola/dev/Inspecciones/inspector/docs/database/TICKET20_GATE_COMPATIBILIDAD_PRODUCTOSSERVICIOS_20260914.md`.

# Estado Ticket 19 — Locking, transacciones e idempotencia ProductosServicios, 2026-09-14

- TICKET 19 IMPLEMENTADO Y CERTIFICADO: se agregó `ISchemaOperationLock` / `SqlSchemaOperationLock` con autoridad SQL Server (`sp_getapplock`), timeout finito, validación explícita de retorno y clasificación `LOCK_TIMEOUT`, `LOCK_CANCELLED`, `LOCK_DEADLOCK`, `LOCK_FAILED`.
- La unidad de lock es `DatabaseIdentity + Scope`, no `idEmpresa`, tenant, usuario, PID, instancia API ni connection string textual. Recursos canónicos: `CheckApp.Schema.Control:{fingerprint}` y `CheckApp.Schema.ProductosServicios:{fingerprint}`; el orden obligatorio es Control→Scope y el release queda en `finally`/`IAsyncDisposable`.
- T16/T17 consumen el lock canónico mediante adaptador `ISchemaProvisionLock`. Bootstrap mueve infraestructura/Attempt/relectura efectiva dentro del lock; después del lock relee attempts y clasificación T13 antes de DDL. Migración mantiene relectura de State/History dentro del lock y ahora clasifica fallos de lock sin ejecutar DDL.
- Transacción/idempotencia: provisionamiento y migración conservan `SqlTransaction`; T17 mantiene `SET XACT_ABORT ON`; T18 post-validación usa misma conexión/transacción. Rollback no avanza State ni escribe History SUCCESS; 10 reintentos tras éxito no duplican DDL, State ni History.
- QA automatizado API PASS: 195 pruebas, 0 fallas. T19 agrega cobertura de recursos, aliases, orden Control→Scope, release, timeout/cancel/deadlock/error fail-closed, relectura post-lock, dos empresas misma DB, dos DB distintas e idempotencia de 10 reintentos.
- QA SQL real CheckAppErp PASS: BEFORE `SchemaOk/DriftCount=0`, `CurrentVersion=1`, hash T15 V1, conteos 20/255/50/24/14; A adquirió lock de scope, B quedó bloqueado/timeout mientras A retenía, B adquirió después del release, otro scope y otro recurso/base no se bloquearon indebidamente; fixture QA `dbo.__MOKA_T19_Idempotencia` ejecutó DDL efectivo 1 vez en 10 intentos y fue limpiada; AFTER `SchemaOk/DriftCount=0`, conteos 20/255/50/24/14.
- No se creó V2, no se implementó T20/gate CRUD, no se modificó Firebase, Hosting, Conexiones, Login/Auth, UI/CRUD, datos de negocio ni base histórica 163. No se persistieron secretos.
- Dictamen: `TICKET 19 IMPLEMENTADO Y CERTIFICADO — LOCKING SQL POR DATABASEIDENTITY + SCOPE OPERATIVO — DOBLE CREATE/MIGRACIÓN EVITADO ENTRE EMPRESAS E INSTANCIAS — RELECTURA POST-LOCK, TRANSACCIONES, ROLLBACK E IDEMPOTENCIA CONFIRMADOS — CONCURRENCIA SQL REAL CERTIFICADA — CHECKAPPERP PERMANECE V1 / SCHEMA_OK — SIN IMPACTO A FIREBASE, TENANTS, DATOS NI BASE HISTÓRICA — LISTO PARA QA/Cierre DEL PRODUCT OWNER.`
- Documento: `/Users/denissemendiola/dev/Inspecciones/inspector/docs/database/TICKET19_LOCKING_TRANSACCIONES_IDEMPOTENCIA_20260914.md`.

# Estado Ticket 18 — Certificación real de drift físico CheckAppErp, 2026-09-14

- TICKET 18 CERTIFICADO EN SQL SERVER REAL: PO autorizó provocar un drift estructural controlado y reversible exclusivamente en QA `CheckAppErp`, con restauración obligatoria. Se ejecutó preflight read-only y la base estaba sana antes del DDL temporal.
- BEFORE: `DB_NAME=CheckAppErp`, identidad saneada `VPS3348900/CHECKAPPERP/A0E05B05-F509-44D3-8BE4-218F875720CA`, `CurrentVersion=1`, hash T15 V1 `4d51ce43a30ec3d583ce4252c8324f1053a52fdd89a7d80b3e01a6e8b780fb06`, `GlobalResult=SchemaOk`, `DriftCount=0`, conteos 20 tablas / 255 columnas / 50 índices / 24 FK / 14 CHECK.
- Fixture seguro: índice no PK, no UNIQUE y no requerido por FK `IX_ProductosServiciosMovimientos_Empresa_FechaMovimiento` en `dbo.ProductosServiciosMovimientosInventario`; restauración preparada desde contrato T15 antes del DROP con `CREATE NONCLUSTERED INDEX` sobre `idEmpresa ASC, FechaMovimiento ASC`.
- DRIFT: se ejecutó únicamente `DROP INDEX` temporal sobre ese índice en `CheckAppErp`; T18 reportó `GlobalResult=SchemaDrift`, `DriftCount=1`, 49 índices, `INDEX_MISSING=True`, `ObjectName=dbo.ProductosServiciosMovimientosInventario.IX_ProductosServiciosMovimientos_Empresa_FechaMovimiento`, `Actual=<ABSENT>`, `ChangedState=False`, `ExecutedDdl=False`; el índice siguió ausente después del validador, certificando que T18 no repara.
- RESTORE/AFTER: se recreó exactamente el índice; T18 final volvió a `GlobalResult=SchemaOk`, `DriftCount=0`, conteos 20/255/50/24/14, `CurrentVersion=1`, hash T15 V1 intacto. No se creó V2, no se ejecutó T17, no hubo `History MIGRATED`, no se tocaron Firebase, Hosting, Conexiones, Login/Auth, UI/CRUD, datos de negocio ni base histórica 163.
- Dictamen: `TICKET 18 CERTIFICADO EN SQL SERVER REAL — CHECKAPPERP V1 SANA VALIDADA — DRIFT FÍSICO REAL PROVOCADO Y DETECTADO — T18 IDENTIFICÓ INDEX_MISSING SIN REPARACIÓN AUTOMÁTICA — ESTRUCTURA RESTAURADA EXACTAMENTE A V1 — SCHEMA_OK FINAL CONFIRMADO — SIN IMPACTO A DATOS, FIREBASE, TENANTS NI BASE HISTÓRICA — LISTO PARA CIERRE DEL PRODUCT OWNER.`
- Documento: `/Users/denissemendiola/dev/Inspecciones/inspector/docs/database/TICKET18_VALIDADOR_FISICO_DRIFT_20260911.md`.

# Estado Ticket 16 — Bootstrap autosuficiente ProductosServicios V1, 2026-09-10

- TICKET 16 IMPLEMENTADO: `IProductosServiciosSchemaBootstrapper` / `ProductosServiciosSchemaBootstrapper` provisiona el scope `ProductosServicios` sólo cuando T13 clasifica `Empty`.
- El bootstrap usa contrato T15 V1 y hash `4d51ce43a30ec3d583ce4252c8324f1053a52fdd89a7d80b3e01a6e8b780fb06`; genera DDL desde `SchemaContract`, valida físicamente y confirma T14 sólo en PASS.
- Estados no permitidos: `Partial`, `Current`, `Outdated`, `Future`, `Unknown`, `Unavailable`; devuelven `NoProvision` con reason code específico. `Partial` nunca se completa automáticamente.
- Control T14: crea `Attempt PROVISION`, registra `History PROVISIONED` sólo tras validación y confirma `State CurrentVersion=1 + ManifestHash` sólo tras PASS. No inventa V0, Ticket09/10 ni eventos `MIGRATED`.
- Concurrencia mínima: lock SQL `sp_getapplock` por `DatabaseIdentity + Scope` y re-clasificación dentro del lock; no se bloquea por `idEmpresa`.
- QA 163 read-only: `SQL5111/DB_A883C3_CHECKLIST/E398ABAB-6416-4E68-8084-7FF7CB232EF5` sigue `Unknown/VERSION_EVIDENCE_MISSING` con 20/20 tablas; T16 rechazó `PROVISION_NOT_ALLOWED_UNKNOWN` sin State/History/Attempts nuevos.
- Documento: `/Users/denissemendiola/dev/Inspecciones/inspector/docs/database/TICKET16_BOOTSTRAP_BASE_NUEVA_PRODUCTOSSERVICIOS_20260910.md`.

# Estado Ticket 15 — Contrato versionado ProductosServicios V1, 2026-09-10

- TICKET 15 IMPLEMENTADO: el API define `SchemaContract` + `SchemaManifest` para `ProductosServicios` con `ContractVersion = 1` y hash canónico determinista `4d51ce43a30ec3d583ce4252c8324f1053a52fdd89a7d80b3e01a6e8b780fb06`.
- Archivos de contrato: `/Users/denissemendiola/dev/Inspecciones/inspectorapi/checklistWs/Services/Tenant/SchemaContractModels.cs` y `/Users/denissemendiola/dev/Inspecciones/inspectorapi/checklistWs/Services/Tenant/ProductosServiciosSchemaContractProvider.cs`.
- El contrato cubre las 20 tablas del scope, 255 columnas, 50 índices adicionales a PK, 24 FK y 14 CHECK. No contiene datos de negocio ni secretos.
- T15 no adopta ni modifica bases históricas: no escribir `CurrentVersion`, `ManifestHash`, `ADOPTED`, history ni attempts por la mera existencia del contrato. T13 debe permanecer `Unknown/VERSION_EVIDENCE_MISSING` cuando T14 State no tenga evidencia.
- QA real read-only en tenant 163: Firebase resolvió la base, DatabaseIdentity `SQL5111/DB_A883C3_CHECKLIST/E398ABAB-6416-4E68-8084-7FF7CB232EF5`, 20/20 tablas, discrepancias contrato vs metadata `0`, `CheckAppSchemaState` sin filas para ProductosServicios.
- Documento: `/Users/denissemendiola/dev/Inspecciones/inspector/docs/database/TICKET15_CONTRATO_VERSIONADO_PRODUCTOSSERVICIOS_20260910.md`.

# AGENTS

## Auditoría Firebase → tenant → database → bootstrap, 2026-09-09 — vigente

- Reglas YA APROBADAS: N empresas por base; versión por DatabaseIdentity + Scope; filas por idEmpresa; DDL una vez/base/scope; bases distintas pueden tener versiones diferentes y sus miembros comparten versión; validar versión + física y datos por empresa; destructivos no automáticos; ProductosServicios primero. Firebase es fuente de configuración tenant: no crear catálogo paralelo.
- Evidencia estática: Login enlaza UID → Usuarios.empresa → Conexiones por clave → IdEmpresa/Cadena. Hosting se lee en Registrare sólo para nueva empresa y se copia a Conexiones; los históricos conservan su copia. Esto no certifica el destino efectivo de PS.
- Destinos actuales mixtos: Registrare usa Servidor MVC para gran parte del negocio; InsertarPrimerZona usa factory fija API; login/usuario SQL consumen Conexiones mediante cadena en request; PS usa CadenaConexionSQLServer de factory. No atribuir a esa configuración fija el significado Hosting/legacy/fallback sin evidencia. Resolver backend unificado NO implementado.
- BootstrapCompleto/BootstrapIds reflejan alta empresarial, no schema. El flujo inserta rol, razón social, zona, sucursal, departamento/puestos y luego usuario; presupone tablas. No se localizó inserción Empresa SQL en esa ruta. Las 20 tablas PS no bastan para plataforma/login/menú sobre base vacía; dependencias físicas y contrato mínimo pendientes.
- Base vacía legítima no es tenant roto: diseño pendiente propone instalación directa del contrato vigente y evento Provisioned; estructura previa coincidente usa Adopted; histórica usa Migrated secuencial; reparación explícita usa Repaired. No replay automático Ticket09/10, no GUID empresarial nuevo si Firebase ya lo asignó.
- Hallazgo de Auth que matiza bitácoras anteriores: ResolveAdministrativeAccessAsync condiciona requireFirebaseStatus al propio status; status=false puede seguir por usuario SQL activo y reparación escribir status=true. No afirmar bloqueo absoluto por status Firebase. No se ejecutó ni corrigió; requiere revisión PO. Persiste fallback query MVC y cadena legacy vía request.
- Auditoría de código concluida; certificación física y aprobación de implementación del bootstrap pendientes. Recomendación: coordinador administrativo backend automático por alta/destino, locks y gate por base/scope, separando estructura y negocio. No hay servicio, tablas de control ni versionador implementados.
- Informe: `/Users/denissemendiola/dev/Inspecciones/inspector/docs/database/AUDITORIA_FIREBASE_TENANT_DATABASE_BOOTSTRAP_20260909.md`; planeación consolidada en `PLANEACION_VERSIONAMIENTO_SCHEMA_MULTITENANT_20260909.md`, capítulo 31. Esta entrada prevalece sobre inferencias históricas incompatibles, sin reabrir Ticket10 cerrado por PO.
- Sólo documentación. Sin cambios funcionales, Firebase/Auth/sesión/roles/permisos/configuración ni SQL. Sin secretos/datos personales nuevos, sin commit/push. Esperar revisión PO antes de implementar.

## Decisión posterior PO/Líder — Planeación 01 corregida, 2026-09-09

- Una misma base física puede contener varios tenants/empresas; compartir base es válido y no exige bases exclusivas.
- El schema se versiona por **DatabaseIdentity + Scope**, con una sola versión física compartida por sus empresas; la versión no pertenece al tenant individual.
- El aislamiento de datos corresponde a **idEmpresa**; base compartida no autoriza consultar o modificar filas de otra empresa.
- El DDL se ejecuta una sola vez por base/scope, agrupando previamente sus tenants; éstos figuran como contexto del impacto, no como migraciones independientes.
- Esta regla arquitectónica está confirmada por PO/Líder. La implementación continúa **NO aprobada / NO iniciada**; esperar nueva aprobación expresa. Documento consolidado: `/Users/denissemendiola/dev/Inspecciones/inspector/docs/database/PLANEACION_VERSIONAMIENTO_SCHEMA_MULTITENANT_20260909.md`.

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
- TICKET 11 implementado el 2026-09-10 dentro de alcance: ProductosServicios deja de abrir la base fija mediante `SqlConnectionFactory` y resuelve SQL server-side desde `Conexiones/{EmpresaKey}` en Firebase, validando HMAC MVC/API, `Status == 1`, `empresa` e `idEmpresa`. No se creo versionamiento, bootstrap, migraciones, tablas schema ni catalogo tenant paralelo.
- Componentes API T11: `checklistWs/Services/Tenant/*`, DI scoped en `Program.cs`, `ProductosServiciosController.CreateConnection(context)` con `TenantSqlConnectionFactory`. `SqlConnectionFactory` legacy queda intacta para otros modulos. Errores de tenant fallan cerrado sin exponer connection string, password, token, base ni servidor.
- QA T11 API: `dotnet test inspectorapi/checklistWs.sln --verbosity minimal` PASS 10/10; `dotnet build inspectorapi/checklistWs/checklistWs.csproj` PASS con warnings legacy. No se ejecuto DDL/DML ni se modifico Firebase/SQL/Hosting.

- TICKET 12 implementado el 2026-09-10 dentro de alcance: se agregó `DatabaseIdentity` como value object inmutable y saneado, resolución canónica mediante metadata SQL read-only sobre la conexión autorizada por T11, y agrupación de tenants activos por identidad física. Una base puede contener N tenants; schema futuro será por DatabaseIdentity+Scope y datos por idEmpresa.
- Componentes API T12: `IDatabaseMetadataReader`/`SqlDatabaseMetadataReader`, `IDatabaseIdentityResolver`/`DatabaseIdentityResolver`, `ITenantCatalogReader` en Firebase read-only, `IDatabaseGroupingService`/`DatabaseGroupingService`, `DatabaseGroup`, `TenantDescriptor` y errores `DatabaseIdentityUnavailable`, `DatabaseIdentityUnverifiable`, `DatabaseIdentityAmbiguous`. Servicios registrados scoped; sin cache ni singleton mutable.
- Fuera de alcance T12: no se crearon `CheckAppSchemaState`, `CheckAppSchemaHistory` ni `CheckAppSchemaAttempts`; no hay versionamiento, baseline, bootstrap, migraciones, locking, gate, schema validator completo ni DDL/DML. No modificar Firebase/SQL/Hosting/Login/Auth ni UI por este ticket.
- QA T12 API: `dotnet test inspectorapi/checklistWs.sln --verbosity minimal` PASS 25/25; lectura real read-only de empresa 163 resolvió DatabaseIdentity saneada sin exponer ConnectionString/password/token. Warnings legacy se documentan, no se corrigen.

- TICKET 13 implementado el 2026-09-10 dentro de alcance: clasificador read-only por `DatabaseIdentity + Scope` para `ProductosServicios`, con estados `Empty`, `Partial`, `Current`, `Outdated`, `Future`, `Unknown` y error operativo `UNAVAILABLE` separado de estado estructural.
- Componentes API T13: `DatabaseClassificationModels`, `ProductScopeInventory`, `SqlDatabaseSchemaProbe`, `DatabaseStateClassifier`, `NullDatabaseVersionEvidenceReader`; DI scoped en `Program.cs`. El probe consulta sólo `sys.tables`/`sys.schemas`; no consulta datos de negocio ni usa excepciones como algoritmo de clasificación.
- Regla T13: 0 tablas scope = Empty; 1-19/20 = Partial; 20/20 sin evidencia formal T14/T15 = Unknown, no Current. Current/Outdated/Future requieren evidencia explícita consumida por abstracción, sin persistir ni crear historia.
- QA T13 API: `dotnet test inspectorapi/checklistWs.sln --verbosity minimal` PASS 40/40. QA real read-only empresa 163: T11 PASS, T12 PASS, T13 `ProductosServicios` = Unknown por `VERSION_EVIDENCE_MISSING` con 20/20 tablas; correcto porque T14/T15 no existen. Sin DDL/DML, sin writes Firebase/SQL/Hosting y sin secretos expuestos.

- TICKET 14 implementado el 2026-09-10 dentro de alcance: control persistente de versión/trazabilidad por `DatabaseIdentity + Scope` mediante `CheckAppSchemaState`, `CheckAppSchemaHistory` y `CheckAppSchemaAttempts` en la base física que describen. No hay State por idEmpresa.
- Componentes API T14: `SchemaVersionControlModels`, `SchemaVersionRepository`, `DatabaseVersionEvidenceReader`, `KnownSchemaVersionProvider`; DI scoped en `Program.cs`. `EnsureSchemaControlInfrastructureAsync` crea sólo objetos T14 faltantes y valida columnas mínimas; si detecta incompatibilidad devuelve conflicto/falla cerrado sin drop/recreate destructivo.
- Regla T14: Attempts no avanzan State; History sólo eventos estructurales confirmados; `SetConfirmedStateAsync` es la única operación de cambio de State. No se inventa baseline, historia ni versión para bases históricas; adopción queda para T15/T18. T13 consume evidencia real T14 y sin State mantiene `Unknown/VERSION_EVIDENCE_MISSING`.
- QA T14: `dotnet test inspectorapi/checklistWs.sln --no-restore --verbosity minimal` PASS 61/61. QA real empresa 163 creó/verificó las tres tablas T14; segunda inicialización idempotente creó 0/verificó 3; State ProductosServicios inexistente/null; T13 posterior `Unknown` con 20/20 tablas. Sin cambios a ProductosServicios/Firebase/Auth/Hosting ni datos de negocio; sin secretos expuestos.
# MOKA Sucursales/Razones/Regiones - 2026-09-17

- Jerarquia vigente consumida desde MVC: `04000000` Ajustes y `04003000` Sucursales son agrupadores solo Acceso; `04003100` ABC Sucursales, `04004000` Razones Sociales y `04005000` Regiones son pantallas funcionales con Acceso + Escritura.
- Los endpoints API legacy de estos catalogos no reciben identidad/rol de usuario; no agregar un bloqueo directo que rompa consumidores sin antes introducir un contrato autenticado con actor. La autorizacion efectiva de esta entrega queda en los controladores MVC autenticados.
- Scope tecnico aprobado para estos tres catalogos: `Sucursales`. T25 permanece FROZEN; no tocar Hosting, Firebase, Conexiones, bases T25, Denisse/SuperAdmin, DDL destructivo ni ProductosServicios por este patron.
- Certificacion SQL real `CheckAppErp` del scope `Sucursales` V1: PASS para bootstrap, idempotencia, drift reversible, locking, CRUD multitenant, limpieza de fixtures y gate `COMPATIBLE`.
- Correcciones API de soporte: `SqlDatabaseSchemaProbe` registra los 20 parametros del query para scopes pequenos; `SqlSchemaPhysicalSnapshotReader` usa prefijo de extras por scope y no fijo de ProductosServicios.
- Cierre final posterior: AuthZ real PASS contra `db_a883c3_checklist`; `ProductosServiciosAuthorizationService` usa fuente legacy/autorizada cuando esta configurada, no la base tenant. Endpoints API usados por Sucursales/Razones/Regiones pasan por `SucursalesScopeRequestContextResolver` antes de SQL negocio. ProductosServicios en `CheckAppErp` migrado a V2 por T17, `SchemaOk`, `DriftCount=0`, gate `COMPATIBLE`. T25 sigue `FROZEN`.

# MOKA UI/UX runtime - 2026-09-17

- Para declarar UI/UX CheckApp PASS se requiere comparacion visual autenticada en navegador contra ProductosServicios; build, CSS y markup no sustituyen runtime real.
- Si Codex levanta un puerto temporal, debe detenerlo y verificarlo libre antes de entregar.
- Si `5200`/`5127` ya estaban activos manualmente por el Product Owner antes del trabajo, no detenerlos; dejarlos intactos y reportarlo.

# MOKA BL-03 FASE A OC-02 - 2026-09-21

- OC-02 cerrado tecnicamente para scope `OrdenesCompra` V1: contrato hash `dcccb6270d0642625823ac410a273431368f26d035d21cc28e47fb8301d8af55`, State V1, drift `SchemaOk`, gate compatible por evidencia real.
- Tablas scope OC: `OrdenesCompra`, `OrdenesCompraDetalle`, `OrdenesCompraFolios`, `OrdenesCompraPresentacionesCompra`. PresentacionCompra queda separada de PresentacionVenta y sin semantica de precio de venta.
- OC historicas preservadas: 43 OC y 88 detalles. Hay 18 detalles historicos cuyo producto ya no existe en `ProductosServicios`; por eso no agregar FK fisica destructiva de detalle OC a ProductosServicios. Nuevas escrituras validan producto en API y conservan snapshots.
- Limpieza inventario historico: BLOQUEADA, 0 deletes. Existe 1 registro en `ProductosServiciosExistencias`; aunque sin FK/movimientos directos, no hay prueba segura de borrado sin regresion de ProductosServicios. No usar `UNKNOWN` ni forzar DELETE.
- No se ejecuto Recepcion, REC-01, UI final ni Legacy. T25 y reporte lider siguen FROZEN. Siguiente recomendado: `INV-01 Scope Inventario V1 por Sucursal y Variante; NO EJECUTAR`.

# MOKA BL-03 FASE A INV-01 - 2026-09-21

- Scope `Inventario` V1 creado/certificado tecnicamente: tablas `InventarioSaldos`, `InventarioMovimientos`, `InventarioSeries`, hash `146bd87ec94a7f69f9c84d61c867ee7e99ccf931138d843b0356573fe0e293b2`, State V1, drift `SchemaOk`, gate `COMPATIBLE`.
- Servicio tecnico `IInventarioScopeLedgerService` aplica ledger+saldo en una transaccion, OperationKey unico por empresa, lock por `empresa+sucursal+producto+variante`, validacion de sucursal/producto inventariable/variante y cross-tenant fail closed. No conectar todavia a Recepcion ni UI.
- Fixture real reversible PASS: entrada 0->10, repeticion idempotente, salida 10->0, cleanup dejo `InventarioSaldos=0`, `InventarioMovimientos=0`, `InventarioSeries=0`.
- INV-01 queda BLOQUEADO, no PASS: el registro legacy `ProductosServiciosExistencias` id `92A412E1-3132-4674-9825-B9A2635E1C11` de `Aceite Motor Sintetico` tiene existencia 10/minimo 5/costo 605, es leido y mutado por runtime ProductosServicios, y no hay prueba segura para DELETE. No usar `UNKNOWN`, no migrar a sucursal ficticia, no borrar.
- No se ejecuto REC-01, Recepcion, UI, Legacy, Firebase, Hosting ni Conexiones. Siguiente recomendado: `INV-02 Resolucion PO de stock historico incompatible; NO EJECUTAR`.
- 2026-09-21 #MOKA BL-03 SEC-01R cerrado / PASS TECNICO: documento `inspector/docs/compras/BL03_REAPERTURA_SEC01R_ROLES_PERMISOS_OC_RECEPCION_20260921.md`. RolesPermisos Proveeduria certifica `05003000/05003001/05003002` y `05004000/05004001/05004002`: agrupadores solo Acceso, hijos Acceso+Escritura independientes, padre no concede hijos, SuperAdmin protegido. Menu lateral OC muestra solo hijos autorizados; Recepcion tiene permisos definidos pero NO links `/Activos/Recepcion/*` hasta UI funcional. Conciliacion OC detalle: 88 fisicos, 79 activos, 9 inactivos/baja; no se perdieron partidas. OC-03 queda LISTO QA PO, no aprobado PO.
- 2026-09-21 #MOKA incidente QA real SuperAdmin/OC-03 corregido: documento `inspector/docs/compras/BL03_INCIDENTE_QA_REAL_SUPERADMIN_USUARIO_ACTIVO_OC03_20260921.md`. Regla permanente: SuperAdmin es protegido/no editable; nuevas opciones registradas oficialmente deben resolverse automaticamente por mecanismo oficial, sin exigir al PO activar switches, sin modificar JSON y sin quitar proteccion. API OrdenesCompra debe autorizar con identidad efectiva string/Firebase UID; el GUID de usuario es opcional y solo se usa cuando existe para auditoria.

# MOKA PS-ACT-01S catalogos visual aprobado - 2026-09-25

- Estado documental compartido con MVC: `PATRON CHECKAPP OFICIAL - CATALOGOS V1 — APROBADO PO`; Golden Master SIMPLE/COMPACTO `PS-ACT-01S-R5`. Ver `inspector/docs/pattern/PATRON_CHECKAPP_CATALOGOS_V1_20260925.md`.
- La refinacion es visual MVC sobre los cinco catalogos de Productos y Servicios; API no debe introducir DDL destructivo ni tablas nuevas por este ajuste.
- Etiquetas conserva contrato funcional de solo `Nombre` expuesto en UI; no agregar Descripcion/editor por este refinamiento.
- Mantener BL-03, OrdenesCompra, Recepcion, Curvas, T25, Legacy y Parte 2 FROZEN.

# MOKA LP-07R5 ListaPrecios UMBRELLA - 2026-09-30

- UMBRELLA empresa `163` / idEmpresa `b17aaece-2b78-4e35-b554-9e694eeb15a7` se resolvio por Firebase/tenant resolver normal a `SQL5111/DB_A883C3_CHECKLIST/E398ABAB-6416-4E68-8084-7FF7CB232EF5`; no usar CheckAppErp ni connection strings manuales para este tenant.
- BEFORE real ListaPrecios: V1 hash `d4f0bbdc05f56c96026d6364f9c333799ccb0ddc9acdf8993fe040b92b48ce7e`, drift `SchemaOk/0`, 2 tablas/30 columnas/11 indices/2 PK/4 FK/7 CHECK, datos `2` listas y `3` detalles preservados.
- Migracion oficial `LP-M20260929-V1-V2-COMERCIAL` PASS; segunda corrida `NO_PENDING_MIGRATIONS`. FINAL V2 hash `e7a388ec985a19fb2b3beb73e8c2cf28d5dda17d3f3bb2f0363683c166092882`, drift `SchemaOk/0`, gate `COMPATIBLE`, 4 tablas/71 columnas/19 indices/4 PK/9 FK/19 CHECK.
- QA funcional LP-03 sobre V2 PASS: producto, servicio, variante, PresentacionVenta, precio especifico, precio `0.00`, lista default/fallback, baja logica, cross-tenant fail-closed, consulta/filtros/export source data. Cleanup fixtures LP-07R5 `0`.
- UI autenticada no se declaro PASS: localhost redirigio a Login sin sesion reutilizable; no introducir credenciales ni forzar login. Puertos 5200/5127 liberados tras la prueba.

# MOKA LP-07C regla PO global - 2026-09-30

- En LP-07C no modificar schema, migraciones, DDL, tenant resolver, Auth/Login/Firebase/Session/Cookies/Claims/Program.cs ni ProductosServicios.
- La fuente de verdad del filtro `ListaPrecios.Tipo` es API/SQL; cubrir con pruebas automatizadas antes de cerrar.
- `checklist/wwwroot/js/Utilerias.js` y `checklist/Views/Shared/_Layout.cshtml` estan FROZEN del lado MVC por regla PO LP-07C; no pedir cambios alli para resolver ListaPrecios.
- No ejecutar LP-08 desde contexto LP-07/LP-07C.
- 2026-09-30 #MOKA LP-08 API + Motor V2 comercial ListaPrecios: documento MVC `inspector/docs/lista-precios/LP_08_API_MOTOR_V2_ADMINISTRACION_COMERCIAL_20260930.md`. API `api/ListaPrecios` extiende el motor LP-03 sobre V2 LP-07 con snapshot comercial (`PrecioBase`, `PrecioLista`, `OrigenPrecio`, `DescuentoPct`, `SubtotalAntesRedondeo`, `RedondeoModo`, `PrecioFinal`, vigencias, regla/correlacion), preview sin persistencia e historico minimo `INDIVIDUAL`. Guardar/Baja siguen bajo `05001009`; lectura/historico/preview bajo `05001008`; tenant y gate server-side. Lista valida `1..10`; fuera de rango `LISTA_INVALIDA`; descuento fuera de `0..100` `DESCUENTO_INVALIDO`; redondeo fuera de `0..2` `REDONDEO_INVALIDO`; vigencia invertida `VIGENCIA_INVALIDA`; duplicidad `DUPLICADO`. Promociones/monedero no aplican por contrato. QA local PASS: `ListaPreciosServiceTests 37/37`, suite API `598/598`. SQL real pendiente por falta de credencial runtime; sin secretos persistidos. No modificar schema/migraciones/runner/tenant resolver/Auth/Login/Firebase/Session/Cookies/Claims/Program.cs/ProductosServicios funcional/MVC visual/consumidores/Legacy.

# MOKA LP-10 Filtros Avanzados ListaPrecios - 2026-09-30

- LP-10 CERRADO: API `api/ListaPrecios/Consulta` acepta filtros server-side de coleccion, etiqueta, atributo, variante, presentacion venta, precio minimo/maximo y descuento. Documento MVC: `inspector/docs/lista-precios/LP_10_FILTROS_AVANZADOS_CHECKAPP_20260930.md`.
- Fuentes reales CheckApp: `ProductosServicios`, categorias, marcas, colecciones, tags, atributos, variantes y presentaciones de venta. No duplicar catalogos ni filtrar artificialmente filas ya recibidas en JS.
- Validaciones API: precio minimo/maximo no negativos, rango minimo <= maximo, descuento `0..100`; errores controlados sin persistencia parcial.
- `ListaPreciosServiceTests` queda ampliado a `45/45 PASS`; cubre catalogos CheckApp, variante, PresentacionVenta, Servicio sin combinaciones incompatibles, precio `0.00`, rango invalido y descuento.
- Existencia/Ventas quedan PENDIENTES por LP-13/Inventario y LP-19/Ventas; no inventar reglas ni tablas.
- FROZEN preservado: schema V2, migraciones/runner, tenant resolver, Auth/Login/Firebase/Session/Cookies/Claims/Program.cs, ProductosServicios funcional, Legacy.
- Siguiente ticket: LP-11. NO EJECUTAR sin handoff PO.

# MOKA LP-11 Fotografias ListaPrecios - 2026-09-30

- LP-11 implementado con bloqueo de cierre responsive MVC: API ListaPrecios expone `ImagenUrl`, `ImagenNombre` e `ImagenOrigen` reutilizando columnas oficiales de ProductosServicios, sin nuevo sistema multimedia y sin DDL.
- La consulta resuelve imagenes en el mismo CTE: Producto/Servicio desde `ProductosServicios`, Variante desde `ProductosServiciosVariantes` con fallback a producto, PresentacionVenta con fallback a producto.
- No cargar binarios, no base64, no N+1, no parametros tenant manipulables, no cambios de Auth/Login/Firebase/tenant resolver/schema/migraciones/runner/ProductosServicios funcional/Legacy.
- Regresion PASS: `ListaPreciosServiceTests 50/50`, full suite API `612/612`, build API PASS, `git diff --check` PASS. Documento MVC: `inspector/docs/lista-precios/LP_11_FOTOGRAFIAS_LISTA_PRECIOS_20260930.md`.
- Siguiente ticket: LP-12. NO EJECUTAR sin revision PO y cierre responsive LP-11.

# MOKA LP-11R3 cierre ListaPrecios - 2026-09-30

- LP-11 CERRADO: fotografias certificadas, responsive autenticado PASS y LP-09 mobile PASS. Documento MVC: `inspector/docs/lista-precios/LP_11_FOTOGRAFIAS_LISTA_PRECIOS_20260930.md`.
- LP-11R2 contiene Mobile 390 con CSS local: `390/390`, `bodyScrollWidth=390`; Desktop 1440, Tablet 820 y resize sin F5 PASS. CSS certificado hash `e255b24f0492d4d9c6b5f3e6806c5f40b81199fb7bfb240c3b06e3a2fd638958`.
- LP-11R3 mueve la delegacion Editar de `#grListaPrecios` a `#gridListaPreciosHost`; tabla y cards reutilizan el mismo flujo LP-09, sin handlers duplicados. Producto, Servicio, Variante y PresentacionVenta mobile PASS; Preview/Historial PASS sin escritura.
- Regresion PASS: focales `61/61`, ListaPreciosServiceTests `50/50`, suite `612/612`, builds API/MVC, node, diff y secret scan PASS. Sin SQL/DDL/fixtures ni cambios de datos, usuarios o roles.
- `Utilerias.js`, `_Layout.cshtml`, `checkapp-ui.js`, Auth, Schema, ProductosServicios y Legacy permanecen FROZEN/sin cambios.
- LP-12 es el siguiente ticket y NO fue ejecutado. Esperar revision PO.

# MOKA LP-14 ajuste masivo - 2026-09-30

- API LP-14 agrega preview READ y ejecucion WRITE para lotes de identidades canonicas. Reutiliza validacion/calculo LP-08 y una unica transaccion SQL `Serializable`; historial `MASIVO` conserva correlacion comun y no altera `INDIVIDUAL`.
- Casos tecnicos PASS: exacto, monto +/-, porcentaje +/-, descuento, cuatro identidades, seleccion/lista invalida, duplicado, cross-tenant y rollback. `ListaPreciosServiceTests 69/69`; suite `634/634`; build API/MVC PASS.
- Cierre BLOQUEADO por falta de sesion autenticada reutilizable para UMBRELLA 163. No ejecutar LP-15. Schema/migraciones, Auth/Login/Firebase, Inventario, ProductosServicios funcional y Legacy permanecen FROZEN.

# MOKA LP-16 descuento por marca - 2026-09-30

- LP-16 CERRADO: API/MVC de descuento por marca certificados en UMBRELLA 163. Documento: `inspector/docs/lista-precios/LP_16_DESCUENTO_POR_MARCA_20260930.md`.
- El servicio valida marca en tenant, arma universo server-side, reemplaza `DescuentoPct`, crea/reactiva/actualiza y omite no-op. Ejecucion por escritor masivo Serializable, rollback total, historial `DESCUENTO_MARCA` y CorrelationId comun.
- QA autenticada Mobil 1 Lista 10: 9 evaluados/afectados, 6 creaciones, 3 reactivaciones y precio `0.00` preservado; segundo preview 0 afectados/9 omitidos. Cleanup UI oficial 9->0, fallback restaurado y residuos 0.
- Regresion PASS: `ListaPreciosServiceTests 115/115`, suite `682/682`, builds API/MVC, node, diff y secret scan. Cross-tenant falla cerrado.
- Schema V2, migraciones/runner, Auth/Login/Firebase/Session/Cookies/Claims, Inventario, ProductosServicios funcional y Legacy siguen FROZEN. LP-17 no ejecutado.

# MOKA LP-17 Excel operativo ampliado - 2026-09-30

- LP-17 CERRADO: exportacion MVC reutiliza `ListaPrecios/Consulta`, permiso READ `05001008` y tenant server-side; no se agrego endpoint ni se modifico API productiva.
- XLSX exporta universo filtrado completo con Precio Base y Existencia; Servicio usa `N/A`; Foto/URL/Acciones/tenant/IDs tecnicos/CorrelationId quedan fuera. QA autenticada UMBRELLA 163/OpenXML/filtros/dataset vacio/responsive PASS; cleanup Excel 0.
- Regresion PASS: focales 132/132, `ListaPreciosServiceTests` 115/115, suite 683/683, builds API/MVC, node, diff y secret scan.
- `Utilerias.js`, `_Layout.cshtml`, `checkapp-ui.js`, Schema, Auth, Inventario, ProductosServicios y Legacy siguen FROZEN/sin cambios. LP-18 no ejecutado.

# MOKA LP-18 Historico y auditoria ampliada - 2026-09-30

- LP-18 CERRADO: API agrega consulta historica read-only bajo `05001008`, tenant server-side, filtros y paginacion 25/50/100 con orden `FechaUtc DESC, id DESC`.
- Una operacion SQL devuelve conteo y pagina con joins tenant-safe; sin N+1, DDL, migracion, escritura historica ni Excel. Los nombres se resuelven desde catalogo actual y no se inventan snapshots inexistentes.
- UMBRELLA 163: 144 eventos reales; INDIVIDUAL 44, MASIVO 41, COPIA_LISTA 26 y DESCUENTO_MARCA 33. CorrelationId, lotes y seis operaciones reales certificados.
- Regresion PASS: servicio 129/129, focales 154/154, suite 698/698 y build API/MVC. Schema, Auth, tenant resolver, Inventario, ProductosServicios y Legacy siguen FROZEN. LP-19 no ejecutado.

# MOKA LP-20D integracion Cotizaciones LP-08 - 2026-10-01

- LP-20D queda BLOQUEADO / NO CERRADO. Documento MVC: `inspector/docs/lista-precios/LP_20D_INTEGRACION_RUNTIME_COTIZACIONES_LP08_20261001.md`.
- API Cotizaciones usa descriptor tenant, gates Cotizaciones+ListaPrecios, resolver LP-08 server-side, snapshot V2, identidad estable, baja logica, historial append-only, override autorizado y clon re-resuelto. PRE_LP08 no se recalcula.
- QA UMBRELLA detecto cabecera Lista 1 archivada/inactiva: fallback sin `idListaPrecio`; `EnsureListaAsync` intenta INSERT y el indice unico empresa+nivel lo rechaza. ListaPrecios esta FROZEN y no se modifico.
- Persistencia parcial 0: cotizaciones QA 0, configuraciones QA activas 0, hard delete 0, SQL manual no y datos legitimos modificados 0.
- Regresion tecnica PASS: Cotizaciones LP08 10/10, schema 29/29, migration engine 36/36, ListaPrecios 129/129, suite 750/750, builds API/MVC, node y diff.
- Schema/versionado, Auth/Login/Firebase, ListaPrecios, Inventario, ProductosServicios y Legacy permanecen FROZEN. No ejecutar LP-21 ni LP-22; esperar revision PO.

# MOKA LP-20D1 hotfix cabecera ListaPrecios - 2026-10-01

- `EnsureListaAsync` usa transaccion Serializable y lectura `UPDLOCK, HOLDLOCK`: reusa activa, reactiva archivada unica sobre el mismo GUID, crea solo si no existe y falla cerrado ante incoherencia.
- Preview permanece read-only y no existe historial oficial de cabecera; no inventar eventos en `ListaPreciosHistorial`.
- UMBRELLA 163 PASS SQL real para GuardarPrecio y precio `0.00`; cleanup oficial 2/2 con `Activo 1 -> 0`, eventos `BAJA`, fallback restaurado, configuraciones activas 0 y cabecera Lista 1 activa.
- LP-20D permanece BLOQUEADO por HTTP 500 independiente del catalogo Sucursal al leer un nulo durante la carga inicial de Cotizaciones. No ampliar el hotfix.
- Regresion: cabecera 14/14, ListaPrecios 129/129, Cotizaciones 39/39, suite 764/764, builds y diff PASS. No ejecutar LP-21 ni LP-22.

# MOKA LP-20D2 hotfix Sucursales - 2026-10-01

- Hotfix API certificado: `Sucursales.Notas` admite el `NULL` valido del contrato V2 y `SucursalController` usa lectura nullable solo para ese campo. No hubo schema, DDL ni correccion de datos.
- Endpoint real `GET /api/Sucursal/ObtenerSucursales` y `/Cotizaciones/Nueva` PASS autenticado en UMBRELLA 163 con 8 sucursales. Test focal mapper nullable agregado.
- Retoma Cotizaciones quedo BLOQUEADA al guardar cambio a Lista 2: el fallback LP-08 es resuelto, pero sin cabecera materializada entrega `IdListaPrecio = null`; `BuildLp08PartidasAsync` exige el id y responde `FALLBACK_PRECIO_PUBLICO`. No ampliar el hotfix a Cotizaciones/ListaPrecios sin ticket PO.
- Cleanup oficial 3/3: `COT-000020` cancelada; dos configuraciones Lista 1 con `Activo 1 -> 0`, historial append-only, fallback restaurado, activos/residuos 0, datos legitimos 0 y cabecera Lista 1 activa.
- Regresion: focales 187/187, full 765/765, builds API/MVC, node, diff y secret scan PASS. LP-20D NO CERRADO. No ejecutar LP-21 ni LP-22.

# MOKA LP-20D3 cierre Cotizaciones LP-08 - 2026-10-01

- LP-20D CERRADO: `BuildLp08PartidasAsync` materializa/reutiliza una cabecera canonica faltante con `EnsureListaAsync`, vuelve a resolver LP-08 y persiste el snapshot con GUID real. Preview no materializa ni escribe.
- `EnsureListaAsync` conserva Serializable + `UPDLOCK/HOLDLOCK`, tenant server-side, niveles 1..10 y fail-closed cross-tenant; no crea detalles de precio.
- UMBRELLA 163 PASS runtime completo, PRE_LP08 19 cotizaciones/70 partidas preservadas y cleanup oficial 2/2; Listas 1 y 2 activas, duplicados 0, activos/residuos 0 y datos legitimos 0.
- Regresion: hotfix 7/7, focales 226/226, suite 772/772, builds API/MVC, node, diff y secret scan PASS.
- Schema/versionado, Auth/Login/Firebase, Inventario, ProductosServicios, Ventas, Facturacion y Legacy permanecen FROZEN. No ejecutar LP-21 ni LP-22; esperar revision PO.

# MOKA LP-20E consolidacion final LP-20 - 2026-10-01

- LP-20 CERRADO: Cotizaciones es el unico consumidor real certificado de ListaPrecios LP-08; tenant server-side, gates Cotizaciones+ListaPrecios, Producto/Servicio/Variante/PresentacionVenta y snapshot comercial completo PASS.
- PRE_LP08 19 cotizaciones/70 partidas preservado; UMBRELLA 163 runtime autenticado y regresion 772/772 PASS. Expedientes MVC LP-20A, LP-20B/LP-20C4 y LP-20D/LP-20D1/LP-20D2/LP-20D3 conservan la evidencia detallada.
- Ventas y Facturacion siguen PLACEHOLDER; Pedidos NO EXISTE. LP-19 permanece BLOQUEADO EXTERNO, LP-21 BLOQUEADO/NO EJECUTABLE y LP-22 PENDIENTE REVISION PO.
- LP-20E es solo documental: codigo productivo 0, SQL/DDL/fixtures 0. Schema, Auth, ListaPrecios, Cotizaciones, Inventario, ProductosServicios, Ventas, Facturacion, Legacy y archivos UI protegidos permanecen FROZEN. No ejecutar LP-21 ni LP-22.

# MOKA LP-22 certificacion final Lista de Precios - 2026-10-01

- LP-22 CERRADO: motor ListaPrecios LP-08 y Cotizaciones consumidor real certificados en UMBRELLA 163; listas 1..10, cuatro identidades, precio 0, fallback, reglas comerciales, snapshots, override, clon y cross-tenant fail-closed PASS.
- ListaPrecios y Cotizaciones permanecen V2 con hashes certificados, SchemaOk/0 y gates COMPATIBLE. PRE_LP08 19 cotizaciones/70 partidas preservado. Focales 134/134 + 53/53 + 48/48, suite 772/772, builds/node/diff/secret scan PASS.
- LP-22 no modifico codigo productivo, SQL, DDL, fixtures, usuarios, roles ni datos legitimos. Cleanup activo 0 y puertos temporales libres. Expediente final MVC: `inspector/docs/lista-precios/LP_22_CERTIFICACION_FINAL_LISTA_PRECIOS_20261001.md`.
- LP-19 permanece BLOQUEADO EXTERNO y LP-21 BLOQUEADO/NO EJECUTADO; promociones fuera del alcance funcional cerrado. Schema, Auth, ListaPrecios, Cotizaciones, dominios preservados, archivos UI protegidos y Legacy permanecen FROZEN/intactos. Siguiente paso: REVISION PO FINAL.

# MOKA LP-CLOSE cierre PO definitivo Lista de Precios - 2026-10-01

- Decision PO: LP-22 APROBADO. `LISTA DE PRECIOS CHECKAPP = CERRADA / CERTIFICADA / APROBADA PO / FROZEN`. Alcance certificado: LP-01..LP-18, LP-20 y LP-22 cerrados; consola operativa y Cotizaciones consumidor LP-08 PASS; regresion 772/772; cleanup y datos legitimos modificados 0.
- Queda FROZEN el alcance certificado: schema V2, motor LP-08, edicion, filtros, fotografias, DynamicGrid, inventario/movimientos, ajuste masivo, copiar lista, descuento por marca, Excel, historico, Cotizaciones LP-08, PRE_LP08, tenant/gates y permisos. Todo cambio futuro requiere nuevo alcance/ticket y analisis de regresion previo.
- LP-19 permanece BLOQUEADO por dominio Ventas inexistente; LP-21 permanece BLOQUEADO por dependencias comerciales/Ventas. 2x1, 3x2, descuento de segundo articulo, monedero, Ventas, Pedidos, Facturacion y sugerencias LP-21 son exclusiones documentadas, no defectos, y no impiden el cierre.
- Los archivos UI protegidos permanecen NUNCA TOCAR; Auth y Legacy preservados. LP-CLOSE es exclusivamente documental: codigo productivo, SQL, DDL y QA adicional 0.

# MOKA LP-QA03 paridad Legacy real - 2026-10-02

- API ListaPrecios agrega valores de atributo y filtro server-side tenant-scoped; combos de presentacion conservan ids y desambiguan colisiones con datos reales, sin `DISTINCT` ciego.
- Ausencia permanece nullable en la matriz/Excel y cero explícito sigue numérico; escritura LP-08 conserva permiso 05001009, transaccion, rollback e historial. Lectura usa 05001008.
- Regresion focal 180/180 y full 778/778; build API PASS. Schema, Auth, Inventario, ProductosServicios, Raramuri y sazapi sin cambios. Pendiente QA manual Denisse.

# MOKA LP-QA04R modal Editar Precios - 2026-10-02

- La consulta ListaPrecios proyecta `ProductosServicios.Descripcion` desde la fuente canónica para exposición read-only en MVC; no existe endpoint ni escritura adicional.
- LP-08 conserva dirty-only, no-op y null frente a cero; no se agregó schema, migración ni contrato de promociones.
- Regresión PASS: focales ListaPrecios 183/183, full suite 781/781 y build API/MVC sin errores. Pendiente QA manual Denisse; no aprobado PO ni FROZEN.
- Expediente MVC: `inspector/docs/lista-precios/LP_QA04_MODAL_EDITAR_PRECIOS_20261002.md`. Auth, Legacy, ProductosServicios funcional y dominios preservados intactos.
