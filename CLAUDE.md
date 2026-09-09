

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
