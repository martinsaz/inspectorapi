"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");

const root = path.resolve(__dirname, "../../..");
const view = fs.readFileSync(path.join(root, "inspector/checklist/Views/ListaPrecios/Index.cshtml"), "utf8");
const js = fs.readFileSync(path.join(root, "inspector/checklist/wwwroot/js/ListaPrecios/ListaPrecios.js"), "utf8");
const css = fs.readFileSync(path.join(root, "inspector/checklist/wwwroot/css/ListaPrecios/ListaPrecios.css"), "utf8");
const service = fs.readFileSync(path.join(root, "inspectorapi/checklistWs/Services/Tenant/ListaPreciosService.cs"), "utf8");

assert.match(view, /id="lpAtributoValoresDropdown"/);
assert.match(view, /id="panelFiltroValoresAtributoListaPrecios"/);
assert.match(js, /state\.selectedAttributeValues\.clear\(\);\s*renderAttributeValueOptions\(\);/);
assert.match(js, /appendQuery\(query, "valoresAtributo", Array\.from\(state\.selectedAttributeValues\)\.join\(","\)\)/);

assert.doesNotMatch(view, /<select id="cbFiltroSucursalListaPrecios"[^>]*multiple/);
assert.match(view, /id="panelFiltroSucursalListaPrecios"/);
assert.match(view, /id="ckTodasSucursalesListaPrecios"[^>]*checked/);
assert.match(css, /\.lp-check-dropdown-panel/);
assert.match(css, /\.lp-check-option/);

assert.match(view, /class="lp-photo-switch"/);
assert.match(view, /class="lp-photo-switch-track"/);
assert.doesNotMatch(view, /lp-photo-switch ps-switch-card/);
assert.match(js, /togglePhotoColumn\(this\.checked\)/);

assert.match(view, /<th>Lista<\/th><th>Precio<\/th><th>Descuento %<\/th><th>Precio final<\/th>/);
assert.match(view, /Sin redondeo/);
assert.match(view, /A 4\/9/);
assert.match(view, /Sólo a 9/);
assert.match(js, /for \(let level = 1; level <= 10; level \+= 1\)/);
assert.match(js, /if \(onlyDirty\) \$rows = \$rows\.filter\("\[data-dirty='true'\]"\)/);
assert.match(js, /matrixNumericValue\(matrixPrice\) \?\? 0/);
assert.match(js, /matrixNumericValue\(matrixDiscount\) \?\? 0/);
assert.doesNotMatch(js, /key: "acciones"/);

assert.match(service, /COUNT\(1\) OVER/);
assert.match(service, /CASE WHEN Coincidencias > 1/);
assert.match(service, /Precio base/);
assert.match(service, /Ref/);

console.log("ListaPrecios LP-QA03 contract tests: PASS");
