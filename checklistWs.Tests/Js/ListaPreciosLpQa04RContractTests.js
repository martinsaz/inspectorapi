"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");

const root = path.resolve(__dirname, "../../..");
const view = fs.readFileSync(path.join(root, "inspector/checklist/Views/ListaPrecios/Index.cshtml"), "utf8");
const js = fs.readFileSync(path.join(root, "inspector/checklist/wwwroot/js/ListaPrecios/ListaPrecios.js"), "utf8");
const css = fs.readFileSync(path.join(root, "inspector/checklist/wwwroot/css/ListaPrecios/ListaPrecios.css"), "utf8");
const models = fs.readFileSync(path.join(root, "inspectorapi/checklistWs/Services/Tenant/ListaPreciosModels.cs"), "utf8");
const service = fs.readFileSync(path.join(root, "inspectorapi/checklistWs/Services/Tenant/ListaPreciosService.cs"), "utf8");

const editor = view.slice(view.indexOf('id="modalListaPreciosEditor"'), view.indexOf('id="modalListaPreciosMasivo"'));

assert.match(css, /#accordionFiltrosListaPrecios\.is-open[\s\S]*overflow:\s*visible/);
assert.match(css, /#accordionFiltrosListaPrecios[\s\S]*\.lp-check-dropdown-panel[\s\S]*overscroll-behavior:\s*contain/);
assert.match(view, /id="panelFiltroSucursalListaPrecios"/);
assert.match(view, /id="ckTodasSucursalesListaPrecios"[^>]*checked/);
assert.match(js, /Array\.from\(state\.selectedBranches\)\.join\(","\)/);

assert.match(editor, /id="lpEditorContext"/);
assert.match(css, /\.lp-editor-photo\s*\{[\s\S]*width:\s*88px;[\s\S]*height:\s*88px;/);
assert.match(css, /\.lp-editor-photo-frame img\s*\{[\s\S]*object-fit:\s*contain/);
assert.match(css, /\.lp-editor-modal \.lp-matrix-editor-wrap\s*\{[\s\S]*display:\s*block !important;[\s\S]*overflow-x:\s*auto/);
assert.match(js, /class='lp-editor-chip'/);
assert.doesNotMatch(js, /buildEditorContext[\s\S]{0,1400}<small>/);
assert.doesNotMatch(js, /buildEditorContext[\s\S]{0,1800}imagenNombre/);

assert.match(editor, /<th>Lista<\/th><th>Precio<\/th><th>Descuento %<\/th><th>Precio final<\/th>/);
assert.match(editor, /Sin redondeo/);
assert.match(editor, /A 4\/9/);
assert.match(editor, /Sólo a 9/);
assert.match(editor, /Sólo la lista editada/);
assert.match(editor, /Todas las listas/);
assert.match(js, /for \(let level = 1; level <= 10; level \+= 1\)/);
assert.match(js, /if \(onlyDirty\) \$rows = \$rows\.filter\("\[data-dirty='true'\]"\)/);
assert.match(js, /if \(!\$rows\.length && !commercial\.modificado\) throw new Error\("Modifica al menos un valor antes de guardar\."\)/);
assert.match(js, /updateEditorSaveState/);
assert.match(js, /matrixNumericValue\(matrixPrice\) \?\? 0/);
for (const level of [1, 5, 10]) {
    assert.ok(js.includes(`data-lp-level='" + level + "'`), `La matriz conserva el click para P${level}/D${level}.`);
}

assert.match(editor, /id="lpEditorAdditional"/);
assert.match(editor, /id="lpEditorDescription"[^>]*data-lp-commercial/);
assert.match(js, /parser\.innerHTML = source/);
assert.match(js, /parser\.textContent/);
for (const field of ["lpEditorWeb", "lpEditorLiverpool", "lpEditorMercadoLibre", "lpEditorObservaciones"]) {
    assert.match(editor, new RegExp(`id="${field}"[^>]*data-lp-commercial`));
}
for (const field of ["lpEditorDosPorUno", "lpEditorTresPorDos", "lpEditorDescuentoSegundo", "lpEditorMonedero"]) {
    assert.match(editor, new RegExp(`id="${field}"[^>]*data-lp-commercial`));
}
assert.match(models, /public string Descripcion \{ get; set; \} = string\.Empty;/);
assert.match(service, /ISNULL\(ps\.Descripcion, N''\) AS Descripcion/);
assert.match(service, /Descripcion = identity\.Descripcion/);

assert.match(editor, /class="modal-footer lp-editor-footer"/);
assert.match(editor, />Cancelar<\/button>/);
assert.match(editor, /id="btLpGuardar"[^>]*disabled/);
assert.doesNotMatch(editor, /Preview e historial/);

console.log("ListaPrecios LP-QA04R contract tests: PASS");
