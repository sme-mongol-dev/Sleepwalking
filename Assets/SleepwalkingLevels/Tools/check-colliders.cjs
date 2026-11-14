const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const root = path.resolve(__dirname, '..');
const baselineFile = path.join(__dirname, 'collider-baseline.json');
const kinds = new Set(['BoxCollider2D', 'PolygonCollider2D', 'CircleCollider2D']);

function field(text, name) {
  return text.match(new RegExp(`^  ${name}: (.+)$`, 'm'))?.[1] || '';
}
function rounded(value) {
  return value.replace(/-?\d+(?:\.\d+)?(?:e[+-]?\d+)?/gi, x => String(+Number(x).toFixed(4)));
}

function signature(file) {
  const text = fs.readFileSync(file, 'utf8');
  const headers = [...text.matchAll(/^--- !u!(\d+) &(\d+)\r?\n/gm)];
  const blocks = headers.map((header, index) => ({
    id: header[2],
    body: text.slice(header.index + header[0].length, headers[index + 1]?.index || text.length),
  }));
  const names = new Map();
  const transforms = new Map();
  const transformByGameObject = new Map();
  for (const block of blocks) {
    if (block.body.startsWith('GameObject:')) {
      names.set(block.id, field(block.body, 'm_Name'));
    }
    if (block.body.startsWith('Transform:')) {
      const go = field(block.body, 'm_GameObject').match(/fileID: (\d+)/)?.[1];
      transforms.set(block.id, {
        parent: field(block.body, 'm_Father').match(/fileID: (\d+)/)?.[1],
        position: field(block.body, 'm_LocalPosition'),
        scale: field(block.body, 'm_LocalScale'),
        rotation: field(block.body, 'm_LocalRotation'),
      });
      if (go) transformByGameObject.set(go, block.id);
    }
  }
  function ancestry(go) {
    const chain = [];
    let id = transformByGameObject.get(go);
    while (id && id !== '0') {
      const transform = transforms.get(id);
      if (!transform) throw new Error(`${file}: missing ancestor transform ${id}`);
      chain.push([rounded(transform.position), rounded(transform.scale), rounded(transform.rotation)]);
      id = transform.parent;
    }
    return chain;
  }
  const colliders = [];
  for (const block of blocks) {
    const type = block.body.match(/^(\w+):/)?.[1];
    if (!kinds.has(type)) continue;
    const go = field(block.body, 'm_GameObject').match(/fileID: (\d+)/)?.[1];
    colliders.push({
      name: names.get(go), ancestry: ancestry(go), type,
      enabled: field(block.body, 'm_Enabled'), trigger: field(block.body, 'm_IsTrigger'),
      size: rounded(field(block.body, 'm_Size')), center: rounded(field(block.body, 'm_Center')),
      offset: rounded(field(block.body, 'm_Offset')), radius: rounded(field(block.body, 'm_Radius')),
      points: rounded(block.body.match(/  m_Points:\r?\n(?: {4,}.*\r?\n)*/)?.[0] || ''),
    });
  }
  colliders.sort((a, b) => JSON.stringify(a).localeCompare(JSON.stringify(b)));
  return { count: colliders.length, sha256: crypto.createHash('sha256').update(JSON.stringify(colliders)).digest('hex'), colliders };
}

const values = {};
const details = {};
for (let q = 1; q <= 7; q++) {
  const level = `L${String(q).padStart(2, '0')}`;
  const result = signature(path.join(root, 'Prefabs', `${level}_DreamGeometry.prefab`));
  values[level] = { count: result.count, sha256: result.sha256 };
  details[level] = result.colliders;
}

const dump = process.argv.find(x => x.startsWith('--dump='));
if (dump) fs.writeFileSync(dump.slice(7), JSON.stringify(details, null, 2) + '\n');

if (process.argv.includes('--write-baseline')) {
  fs.writeFileSync(baselineFile, JSON.stringify(values, null, 2) + '\n');
  console.log('Collider baseline written.');
} else {
  const baseline = JSON.parse(fs.readFileSync(baselineFile, 'utf8'));
  for (const [level, value] of Object.entries(values)) {
    if (value.count !== baseline[level]?.count || value.sha256 !== baseline[level]?.sha256)
      throw new Error(`${level}: collider geometry changed`);
    console.log(`${level}: ${value.count} colliders unchanged`);
  }
}
