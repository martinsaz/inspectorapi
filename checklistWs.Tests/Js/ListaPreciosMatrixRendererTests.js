"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const sourcePath = path.resolve(__dirname, "../../../inspector/checklist/wwwroot/js/ListaPrecios/ListaPrecios.js");
let source = fs.readFileSync(sourcePath, "utf8");
source = source.replace(
    /\}\)\(\);\s*$/,
    "globalThis.__lpMatrixTests = { buildMatrixColumns, formatMatrixPrice, formatMatrixDiscount, matrixExportValue }; })();"
);

const context = {
    console,
    Intl,
    $: function () { return {}; }
};
vm.createContext(context);
vm.runInContext(source, context, { filename: sourcePath });

const helpers = context.__lpMatrixTests;
assert.ok(helpers, "No fue posible exponer los helpers locales para la prueba.");

const columns = helpers.buildMatrixColumns();
assert.equal(columns.length, 20);

const price = columns[0];
const discount = columns[1];
const row = { __lpIndex: 0 };

for (const value of [null, undefined, "", "   "]) {
    assert.match(price.render(value, row), />\$0\.00<\/button>$/);
    assert.match(discount.render(value, row), />0\.00<\/button>$/);
    assert.equal(price.exportValue(value), "");
    assert.equal(discount.exportValue(value), "");
}

for (const value of [0, "0", 0.0, "0.00"]) {
    assert.match(price.render(value, row), />\$0\.00<\/button>$/);
    assert.match(discount.render(value, row), />0\.00<\/button>$/);
    assert.equal(price.exportValue(value), 0);
    assert.equal(discount.exportValue(value), 0);
}

assert.match(price.render(1234.5, row), />\$1,234\.50<\/button>$/);
assert.match(discount.render("12.5", row), />12\.50<\/button>$/);
assert.equal(price.exportValue(1234.5), 1234.5);
assert.equal(discount.exportValue("12.5"), 12.5);

for (const value of ["invalid", "12oops", Number.NaN, Number.POSITIVE_INFINITY]) {
    assert.match(price.render(value, row), />\$0\.00<\/button>$/);
    assert.match(discount.render(value, row), />0\.00<\/button>$/);
    assert.equal(price.exportValue(value), "");
    assert.equal(discount.exportValue(value), "");
}

for (let level = 0; level < 10; level += 1) {
    const priceColumn = columns[level * 2];
    const discountColumn = columns[(level * 2) + 1];
    assert.equal(priceColumn.key, `p${level + 1}`);
    assert.equal(discountColumn.key, `d${level + 1}`);
    assert.match(priceColumn.render("", row), />\$0\.00<\/button>$/);
    assert.match(discountColumn.render("", row), />0\.00<\/button>$/);
}

console.log("ListaPrecios matrix renderer tests: PASS");
