"use strict";

const FILE_NAME = "SleepwalkingMenuDecor.json";
const TARGET_PATH = "Assets/StreamingAssets/" + FILE_NAME;
const DRAFT_KEY = "sleepwalking.odradek.editor.v2";
const ASSET_ROOT = "OdradekAssets/";
const catalogue = [];
function add(id, title, image, group, min, max, assetKey) {
    catalogue.push({id:id, title:title, image:image, group:group, min:min, max:max, asset:assetKey || id});
}
add("window", "Окно", "Window.png", "props", 1, 100, "menu.window");
add("nightstand", "Тумба", "Tymba.png", "props", 1, 100, "menu.nightstand");
add("clock.big", "Большая стрелка часов", "ClockArrowBig.png", "props", 1, 100);
add("clock.small", "Малая стрелка часов", "ClockArrowSmall.png", "props", 1, 100);
add("slippers", "Тапочки", "Slippers.png", "props", 35, 100);
add("eye", "Глаз", "Eye.png", "props", 55, 100);
add("eyeball", "Глазное яблоко", "Eyeball.png", "props", 75, 100);
add("calendar.1", "Календарь 1", "Calendar.png", "calendar", 25, 49);
add("calendar.2", "Календарь 2", "Calendar2.png", "calendar", 50, 74);
add("calendar.3", "Календарь 3", "Calendar3.png", "calendar", 75, 100);
for (let q = 1; q <= 12; q++) {
    const id = "poster." + String(q).padStart(2, "0");
    add(id, "Постер " + String(q).padStart(2, "0"), "Poster" + String(q).padStart(2, "0") + ".png",
        "poster", q <= 10 ? (q - 1) * 8 + 1 : q === 11 ? 81 : 91,
        q <= 10 ? q * 8 : q === 11 ? 90 : 100);
}
add("bed.base", "Кровать / герой — исходный", "BoyAndBed.png", "bed", 1, 12);
for (let q = 1; q <= 8; q++)
    add("bed.look." + q, "Кровать / герой — фаза сна " + q, "bedframe:" + (q - 1),
        "bed", q === 1 ? 13 : q * 11 + 2, q === 8 ? 100 : (q + 1) * 11 + 1);
for (let q = 1; q <= 5; q++)
    add("crack." + q, "Трещина " + q, "Crack" + q + ".png", "cracks", 35 + (q - 1) * 15, 100);

const byId = new Map(catalogue.map(item => [item.id, item]));
const byAsset = new Map(catalogue.map(item => [item.asset, item]));
const variants = new Set(["poster", "calendar", "bed"]);
const nodes = {
    rows:document.getElementById("rows"), groupSummary:document.getElementById("groupSummary"),
    issues:document.getElementById("issues"), groupFilter:document.getElementById("groupFilter"),
    partitionGroup:document.getElementById("partitionGroup"), rowCount:document.getElementById("rowCount"),
    search:document.getElementById("search"), odradekValue:document.getElementById("odradekValue"),
    odradekNumber:document.getElementById("odradekNumber"), status:document.getElementById("saveStatus"),
    saveUnity:document.getElementById("saveUnity"),
    saveUnityTop:document.getElementById("saveUnityTop"),
    importFile:document.getElementById("importFile")
};
let doc = createDefaults();
let savedSnapshot = JSON.stringify(doc);
let exported = false;
let message = "";

function createDefaults() {
    return {version:1, decorations:catalogue.map(item => ({
        id:item.id, group:item.group, min:item.min, max:item.max, asset:item.asset
    }))};
}
function integer(value) { return Number.isInteger(value) && value >= 1 && value <= 100; }
function segments(values) {
    if (!values.length) return "нет";
    const parts = [];
    let first = values[0], last = first;
    for (let q = 1; q < values.length; q++) {
        if (values[q] === last + 1) { last = values[q]; continue; }
        parts.push(first === last ? String(first) : first + "–" + last);
        first = last = values[q];
    }
    parts.push(first === last ? String(first) : first + "–" + last);
    return parts.join(", ");
}
function changed(row, item) {
    return row.group !== item.group || row.min !== item.min || row.max !== item.max ||
        row.asset !== item.asset || Object.keys(row).some(key => !["id","group","min","max","asset"].includes(key));
}
function exportDocument(input) {
    const result = {};
    for (const key of Object.keys(input))
        if (key !== "decorations") result[key] = input[key];
    result.decorations = input.decorations.filter(row => {
        const item = byId.get(row.id);
        return !item || changed(row, item);
    });
    return result;
}
function validate(input) {
    const errors = [], warnings = [], rowErrors = new Map(), groups = new Map(), seen = new Set();
    if (!input || input.version !== 1 || !Array.isArray(input.decorations)) {
        errors.push("Нужны version: 1 и массив decorations.");
        return {errors:errors, warnings:warnings, rowErrors:rowErrors, groups:groups};
    }
    for (let q = 0; q < input.decorations.length; q++) {
        const row = input.decorations[q], problems = [];
        if (!row || typeof row !== "object" || Array.isArray(row)) {
            errors.push("Строка " + (q + 1) + ": нужен объект.");
            rowErrors.set(q, ["нужен объект"]);
            continue;
        }
        const known = byId.get(row.id);
        if (!known) problems.push("ID не зарегистрирован в Unity");
        if (seen.has(row.id)) problems.push("повтор ID");
        seen.add(row.id);
        if (known && row.group !== known.group) problems.push("группа должна быть " + known.group);
        const selectedAsset = byAsset.get(row.asset);
        if (!selectedAsset) problems.push("asset не зарегистрирован в Unity");
        else if (known && selectedAsset.group !== known.group)
            problems.push("asset должен принадлежать группе " + known.group);
        if (!integer(row.min) || !integer(row.max)) problems.push("Min и Max — целые числа 1–100");
        else if (row.min > row.max) problems.push("Min больше Max");
        if (problems.length) {
            rowErrors.set(q, problems);
            errors.push((row.id || "Строка " + (q + 1)) + ": " + problems.join("; ") + ".");
        }
        if (!known || !integer(row.min) || !integer(row.max) || row.min > row.max) continue;
        const name = known.group;
        if (!groups.has(name)) groups.set(name, {mode:variants.has(name) ? "replace" : "cumulative",
            counts:new Array(101).fill(0), gaps:[], overlaps:[]});
        for (let value = row.min; value <= row.max; value++) groups.get(name).counts[value]++;
    }
    for (const name of ["poster", "calendar", "bed", "cracks", "props"])
        if (!groups.has(name)) groups.set(name, {mode:variants.has(name) ? "replace" : "cumulative",
            counts:new Array(101).fill(0), gaps:[], overlaps:[]});
    for (const [name, group] of groups) {
        for (let value = 1; value <= 100; value++) {
            if (!group.counts[value]) group.gaps.push(value);
            if (group.counts[value] > 1) group.overlaps.push(value);
        }
        if (variants.has(name) && group.overlaps.length)
            errors.push(name + ": пересечения при " + segments(group.overlaps) + ".");
        if ((name === "poster" || name === "bed") && group.gaps.length)
            errors.push(name + ": нет варианта при " + segments(group.gaps) + ".");
        if (name === "calendar" && group.gaps.length)
            warnings.push("calendar: нет календаря при " + segments(group.gaps) + " (допустимо).");
        if (!variants.has(name) && group.gaps.length)
            warnings.push(name + ": декор отсутствует при " + segments(group.gaps) + " (допустимо).");
    }
    return {errors:errors, warnings:warnings, rowErrors:rowErrors, groups:groups};
}
function el(tag, className, content) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (content !== undefined) node.textContent = content;
    return node;
}
function preview(item) {
    const box = el("div", "preview");
    if (!item || !item.image) { box.append(el("span", "missing", "Нет превью")); return box; }
    if (item.image.startsWith("bedframe:")) {
        const frame = Number(item.image.slice(9));
        const sprite = el("span", "bed-frame");
        sprite.style.backgroundPosition = (-frame * 72) + "px 0";
        sprite.setAttribute("aria-label", "Кадр " + (frame + 1) + " из восьми");
        sprite.setAttribute("role", "img");
        box.append(sprite);
    } else {
        const image = el("img");
        image.src = ASSET_ROOT + item.image;
        image.alt = item.title;
        image.loading = "lazy";
        image.onerror = function() { image.replaceWith(el("span", "missing", "Нет изображения")); };
        box.append(image);
    }
    return box;
}
function field(type, value, label, onChange) {
    const input = el("input");
    input.type = type;
    input.value = value === null || value === undefined ? "" : String(value);
    input.setAttribute("aria-label", label);
    if (type === "number") { input.min = "1"; input.max = "100"; input.step = "1"; }
    input.addEventListener("change", function() { onChange(input.value); });
    return input;
}
function update(index, key, value) {
    const row = doc.decorations[index];
    row[key] = key === "min" || key === "max" ? (value === "" ? null : Number(value)) : value;
    message = "";
    persist();
    render();
}
function renderRows(result) {
    const query = nodes.search.value.trim().toLowerCase(), filter = nodes.groupFilter.value;
    const fragment = document.createDocumentFragment();
    let visible = 0;
    doc.decorations.forEach(function(row, index) {
        const known = byId.get(row.id), picture = byAsset.get(row.asset) || known;
        const title = known ? known.title : "Неизвестный декор";
        if (filter && row.group !== filter) return;
        if (query && !(title + " " + row.id + " " + row.group + " " + row.asset).toLowerCase().includes(query)) return;
        visible++;
        const tr = el("tr");
        if (result.rowErrors.has(index)) tr.classList.add("invalid");
        const active = integer(row.min) && integer(row.max) &&
            row.min <= Number(nodes.odradekValue.value) && row.max >= Number(nodes.odradekValue.value);
        if (active) tr.classList.add("active-now");
        const imageCell = el("td"); imageCell.append(preview(picture)); tr.append(imageCell);
        const idCell = el("td");
        idCell.append(el("span", "asset-name", title), el("span", "asset-id", row.id));
        if (!known) {
            const idInput = field("text", row.id, "ID", value => update(index, "id", value));
            idInput.className = "field-wide"; idCell.append(idInput);
        }
        tr.append(idCell);
        const groupCell = el("td");
        const group = field("text", row.group, "Группа " + row.id, value => update(index, "group", value));
        group.className = "field-wide"; groupCell.append(group); tr.append(groupCell);
        const mode = variants.has(row.group) ? "Заменяющий" : "Накопительный";
        tr.append(el("td", "mode", mode));
        const minCell = el("td"); minCell.append(field("number", row.min, "Min " + row.id,
            value => update(index, "min", value))); tr.append(minCell);
        const maxCell = el("td"); maxCell.append(field("number", row.max, "Max " + row.id,
            value => update(index, "max", value))); tr.append(maxCell);
        const assetCell = el("td");
        const assetInput = field("text", row.asset, "Asset " + row.id, value => update(index, "asset", value));
        assetInput.className = "asset-field"; assetInput.setAttribute("list", "assetKeys"); assetCell.append(assetInput);
        tr.append(assetCell);
        const stateCell = el("td"), problems = result.rowErrors.get(index);
        stateCell.append(el("span", "row-status" + (problems ? " bad" : ""),
            problems ? problems.join("; ") : active ? "Виден" : "Скрыт"));
        tr.append(stateCell);
        const actionCell = el("td"), button = el("button", "remove", known ? "↺" : "×");
        button.type = "button";
        button.setAttribute("aria-label", known ? "Сбросить " + row.id : "Удалить " + row.id);
        button.addEventListener("click", function() {
            if (known) doc.decorations[index] = {id:known.id, group:known.group,
                min:known.min, max:known.max, asset:known.asset};
            else doc.decorations.splice(index, 1);
            message = known ? "Строка сброшена к значениям префаба." : "Строка удалена.";
            persist(); render();
        });
        actionCell.append(button); tr.append(actionCell); fragment.append(tr);
    });
    nodes.rows.replaceChildren(fragment);
    nodes.rowCount.textContent = visible + " из " + doc.decorations.length + " строк";
}
function renderGroups(result) {
    const fragment = document.createDocumentFragment();
    for (const [name, group] of result.groups) {
        const bad = variants.has(name) && (group.overlaps.length > 0 ||
            (name !== "calendar" && group.gaps.length > 0));
        const card = el("div", "group-card" + (bad ? " error" : ""));
        card.append(el("strong", "", name), el("small", "", group.mode === "replace" ? "Заменяющий" : "Накопительный"),
            el("small", bad ? "bad" : "", "Пусто: " + segments(group.gaps)),
            el("small", group.overlaps.length && variants.has(name) ? "bad" : "",
                (variants.has(name) ? "Пересечения: " : "Одновременно: ") + segments(group.overlaps)));
        fragment.append(card);
    }
    nodes.groupSummary.replaceChildren(fragment);
    const oldFilter = nodes.groupFilter.value, oldPartition = nodes.partitionGroup.value;
    nodes.groupFilter.replaceChildren(new Option("Все группы", ""));
    nodes.partitionGroup.replaceChildren();
    for (const [name] of result.groups) {
        nodes.groupFilter.append(new Option(name, name));
        if (variants.has(name)) nodes.partitionGroup.append(new Option(name, name));
    }
    nodes.groupFilter.value = oldFilter;
    nodes.partitionGroup.value = oldPartition || "poster";
}
function renderIssues(result) {
    const fragment = document.createDocumentFragment();
    if (!result.errors.length && !result.warnings.length)
        fragment.append(el("li", "", "Ошибок нет. JSON готов для Unity."));
    for (const value of result.errors) fragment.append(el("li", "error", value));
    for (const value of result.warnings) fragment.append(el("li", "", value));
    nodes.issues.replaceChildren(fragment);
    nodes.saveUnity.disabled = result.errors.length > 0;
    nodes.saveUnityTop.disabled = result.errors.length > 0;
}
function renderStatus(result) {
    const dirty = JSON.stringify(doc) !== savedSnapshot;
    nodes.status.className = "status " + (dirty ? "dirty" : "good");
    nodes.status.textContent = message || (dirty ?
        "Есть несохранённые правки. Черновик в браузере Unity не читает." :
        exported ? "Экспорт сохранён. Unity читает файл из " + TARGET_PATH + "." :
            "Показаны исходные значения префаба. Экспорт ещё не сохранён.");
    if (result.errors.length) nodes.status.textContent += " Ошибок: " + result.errors.length + ".";
}
function render() {
    const result = validate(doc);
    nodes.odradekNumber.textContent = nodes.odradekValue.value;
    renderGroups(result); renderIssues(result); renderRows(result); renderStatus(result);
}
function persist() {
    try { localStorage.setItem(DRAFT_KEY, JSON.stringify(doc)); }
    catch (error) { message = "Хранилище браузера недоступно; скачайте черновик."; }
}
async function saveFile(name, allowInvalid) {
    const result = validate(doc);
    if (!allowInvalid && result.errors.length) {
        message = "Исправьте ошибки перед экспортом для Unity."; render(); return;
    }
    const data = JSON.stringify(allowInvalid ? doc : exportDocument(doc), null, 2) + "\n";
    if (window.showSaveFilePicker) {
        try {
            const handle = await window.showSaveFilePicker({suggestedName:name,
                types:[{description:"JSON", accept:{"application/json":[".json"]}}]});
            const stream = await handle.createWritable();
            await stream.write(data); await stream.close();
            if (!allowInvalid) { savedSnapshot = JSON.stringify(doc); exported = true; }
            message = allowInvalid ? "Черновик сохранён." :
                "JSON сохранён. Поместите файл в " + TARGET_PATH + " для Unity.";
            render(); return;
        } catch (error) { if (error && error.name === "AbortError") return; }
    }
    const blob = new Blob([data], {type:"application/json"}), url = URL.createObjectURL(blob);
    const link = el("a"); link.href = url; link.download = name;
    document.body.append(link); link.click(); link.remove();
    setTimeout(function() { URL.revokeObjectURL(url); }, 30000);
    if (!allowInvalid) { savedSnapshot = JSON.stringify(doc); exported = true; }
    message = allowInvalid ? "Черновик скачан." :
        "JSON скачан. Переместите его в " + TARGET_PATH + " для Unity.";
    render();
}
function partition() {
    const name = nodes.partitionGroup.value, rows = doc.decorations.filter(row => row.group === name);
    if (!rows.length) return;
    const start = name === "calendar" ? Math.min(...rows.map(row => row.min)) : 1;
    if (!integer(start) || 101 - start < rows.length) {
        message = "Для разбиения оставьте хотя бы одно значение на каждый вариант.";
        render(); return;
    }
    const span = 101 - start;
    rows.forEach(function(row, index) {
        row.min = Math.floor(index * span / rows.length) + start;
        row.max = Math.floor((index + 1) * span / rows.length) + start - 1;
    });
    message = name + " разбита без дыр и пересечений с " + start + " до 100.";
    persist(); render();
}
function loadDraft() {
    try {
        const value = localStorage.getItem(DRAFT_KEY);
        if (!value) return;
        const parsed = JSON.parse(value);
        if (parsed && parsed.version === 1 && Array.isArray(parsed.decorations)) {
            doc = parsed; exported = false;
            message = "Восстановлен локальный черновик; Unity его ещё не читает.";
        }
    } catch (error) { message = "Не удалось восстановить черновик."; }
}
function importDocument(input) {
    if (!input || input.version !== 1 || !Array.isArray(input.decorations))
        throw new Error("Нужны version: 1 и decorations: [].");
    const full = createDefaults();
    for (const key of Object.keys(input))
        if (key !== "decorations") full[key] = input[key];
    const seen = new Set();
    for (const row of input.decorations) {
        if (!row || typeof row !== "object" || Array.isArray(row))
            throw new Error("Каждая строка decorations должна быть объектом.");
        if (seen.has(row.id)) { full.decorations.push(row); continue; }
        seen.add(row.id);
        const index = full.decorations.findIndex(item => item.id === row.id);
        if (index < 0) full.decorations.push(row);
        else full.decorations[index] = row;
    }
    return full;
}
const datalist = el("datalist"); datalist.id = "assetKeys";
for (const key of byAsset.keys()) {
    const option = el("option"); option.value = key; datalist.append(option);
}
document.body.append(datalist);
document.getElementById("importButton").addEventListener("click", function() { nodes.importFile.click(); });
nodes.importFile.addEventListener("change", async function() {
    const file = nodes.importFile.files && nodes.importFile.files[0];
    if (!file) return;
    try {
        doc = importDocument(JSON.parse(await file.text())); exported = false;
        message = "Загружен " + file.name + ". Неизвестные строки и дополнительные поля сохранены.";
        persist(); render();
    } catch (error) { message = "Ошибка импорта: " + error.message; render(); }
    nodes.importFile.value = "";
});
document.getElementById("saveDraft").addEventListener("click",
    function() { saveFile("SleepwalkingMenuDecor.draft.json", true); });
function saveUnity() { saveFile(FILE_NAME, false); }
nodes.saveUnity.addEventListener("click", saveUnity);
nodes.saveUnityTop.addEventListener("click", saveUnity);
document.getElementById("resetDefaults").addEventListener("click", function() {
    if (!confirm("Сбросить правки к значениям префаба?")) return;
    doc = createDefaults(); exported = false;
    message = "Таблица сброшена к значениям префаба."; persist(); render();
});
document.getElementById("addRow").addEventListener("click", function() {
    let number = 1, id = "new.decor." + number;
    while (doc.decorations.some(row => row.id === id)) { number++; id = "new.decor." + number; }
    doc.decorations.push({id:id, group:"props", min:1, max:100, asset:"menu.window"});
    nodes.search.value = ""; nodes.groupFilter.value = "";
    message = "Новый ID требует регистрации в Unity; пока экспорт заблокирован.";
    persist(); render();
});
document.getElementById("autoPartition").addEventListener("click", partition);
nodes.search.addEventListener("input", render);
nodes.groupFilter.addEventListener("change", render);
nodes.odradekValue.addEventListener("input", render);
loadDraft(); render();
window.OdradekEditorTest = {validate:validate, createDefaults:createDefaults,
    exportDocument:exportDocument, importDocument:importDocument, segments:segments,
    getDocument:function() { return doc; }};
