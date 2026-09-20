# PATRON CHECKAPP OFICIAL - PRODUCTOSSERVICIOS - 2026-09-16

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
