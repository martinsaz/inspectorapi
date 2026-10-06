"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");

const sourcePath = path.resolve(__dirname, "../../../inspector/checklist/wwwroot/js/ListaPrecios/ListaPrecios.js");
const source = fs.readFileSync(sourcePath, "utf8");

assert.match(source, /initGrid\(\);\s*bindDynamicGridActions\(\);\s*bindEvents\(\);/);
assert.match(source, /const root = document\.documentElement/);
assert.match(source, /root\.dataset\.lpGridActionsBound === "true"/);
assert.match(source, /root\.dataset\.lpGridActionsBound = "true"/);
assert.match(source, /document\.addEventListener\("click", handleDynamicGridAction, true\)/);
assert.equal((source.match(/addEventListener\("click", handleDynamicGridAction, true\)/g) || []).length, 1);

assert.match(source, /closest\("\[data-lp-edit-row\],\[data-lp-inventory-row\]"\)/);
assert.match(source, /action\.closest\("#gridListaPreciosHost"\)/);
assert.match(source, /action\.dataset\.lpEditRow/);
assert.match(source, /action\.dataset\.lpLevel \|\| row\.lista \|\| 1/);
assert.match(source, /action\.dataset\.lpInventoryRow/);

assert.doesNotMatch(source, /\$\("#gridListaPreciosHost"\)\.on\("click", "\[data-lp-edit-row\]"/);
assert.equal((source.match(/function bindDynamicGridActions\(/g) || []).length, 1);
assert.equal((source.match(/function handleDynamicGridAction\(/g) || []).length, 1);

for (const level of [1, 5, 10]) {
    const key = `data-lp-level='" + level + "'`;
    assert.ok(source.includes(key), `La matriz debe conservar el nivel dinámico para Lista ${level}.`);
}

assert.match(source, /const disabled = !state\.canWrite/);
assert.match(source, /#lpEditorMatrizRows input,#lpEditorMatrizRows select/);
assert.match(source, /\.prop\("disabled", disabled\)/);
assert.match(source, /#lpEditorMatrizRows"\)\.on\("change", "\[data-lp-matrix-price\],\[data-lp-matrix-discount-input\],\[data-lp-matrix-final-input\]"/);
assert.doesNotMatch(source, /#lpEditorMatrizRows"\)\.on\("input change"/);

assert.doesNotMatch(source, /window\.(confirm|alert|prompt)\s*\(/);
assert.match(source, /confirmCheckApp\("¿Guardar los cambios\?", "Guardar"\)/);
assert.match(source, /confirmButtonText: confirmText/);
assert.match(source, /cancelButtonText: "Cancelar"/);
assert.match(source, /closeEditorModal\(\);\s*reloadGrid\(\);\s*showCheckAppSuccess/);
assert.match(source, /function applyRoundingToCurrentFinal/);
assert.match(source, /if \(all\) applyRoundingToCurrentFinal\(\$row, mode\)/);
assert.doesNotMatch(source, /const \$row = \$\(this\)\.attr\("data-round", String\(mode\)\);\s*recalculateMatrixRow\(\$row, "discount"\)/);
assert.match(source, /const commercialSnapshot = JSON\.stringify\(commercial\);/);
assert.match(source, /commercial\.modificado = !!state\.editorCommercialInitial && commercialSnapshot !== state\.editorCommercialInitial;/);
assert.doesNotMatch(source, /commercial\.modificado = !!state\.editorCommercialInitial && JSON\.stringify\(commercial\)/);

console.log("ListaPrecios editor handler tests: PASS");
