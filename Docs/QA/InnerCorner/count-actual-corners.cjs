const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..', '..', '..');
const prefabDir = path.join(root, 'Assets', 'SleepwalkingLevels', 'Prefabs');
const tileSurfaceGuid = '4bfe2fba776b456c9dd9fa1feea558c7';
const diagonals = [
  ['NW', -1, 1], ['NE', 1, 1], ['SE', 1, -1], ['SW', -1, -1],
];

for (let level = 1; level <= 7; level++) {
  const name = `L${String(level).padStart(2, '0')}`;
  const file = path.join(prefabDir, `${name}_DreamGeometry.prefab`);
  const sections = fs.readFileSync(file, 'utf8').split(/^--- !u!/m).slice(1);
  const names = new Map();
  const transforms = new Map();
  const owners = new Set();
  for (const section of sections) {
    const gameObject = section.match(/^1 &(\d+)\r?\nGameObject:/)?.[1];
    if (gameObject) names.set(gameObject, section.match(/^  m_Name: (.+)$/m)?.[1]);
    const type = section.match(/^\d+ &\d+\r?\n(\w+):/)?.[1];
    const owner = section.match(/^  m_GameObject: \{fileID: (\d+)\}/m)?.[1];
    if (type === 'Transform' && owner) {
      const position = section.match(/^  m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}/m);
      const scale = section.match(/^  m_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}/m);
      if (position && scale)
        transforms.set(owner, { x: +position[1], y: +position[2], w: +scale[1], h: +scale[2] });
    }
    if (section.includes(`guid: ${tileSurfaceGuid}`) && owner) owners.add(owner);
  }

  const cells = new Set();
  for (const owner of owners) {
    const block = names.get(owner);
    if (block?.startsWith('L05_Timed') || block?.startsWith('L06_')) continue;
    const t = transforms.get(owner);
    if (!t) throw new Error(`${name}: missing tile transform ${block}`);
    const left = t.x - t.w / 2;
    const right = t.x + t.w / 2;
    const bottom = t.y - t.h / 2;
    const top = t.y + t.h / 2;
    for (let x = left; x < right; x++)
    for (let y = bottom; y < top; y++) cells.add(`${x},${y}`);
  }

  const count = { NW: 0, NE: 0, SE: 0, SW: 0 };
  const doubleCount = { 'SW+SE': 0, 'NW+SW': 0, 'NW+NE': 0, 'NE+SE': 0 };
  const hits = [];
  const doubleHits = [];
  for (const key of cells) {
    const [x, y] = key.split(',').map(Number);
    if (![[0, 1], [1, 0], [0, -1], [-1, 0]].every(([dx, dy]) => cells.has(`${x + dx},${y + dy}`)))
      continue;
    const missing = diagonals.filter(([, dx, dy]) => !cells.has(`${x + dx},${y + dy}`));
    if (missing.length === 1) {
      const corner = missing[0][0];
      count[corner]++;
      hits.push(`${key}:${corner}`);
    }
    else if (missing.length === 2) {
      const pair = missing.map(([corner]) => corner);
      const corner = Object.keys(doubleCount).find(item =>
        item.split('+').every(part => pair.includes(part)));
      if (!corner) continue;
      doubleCount[corner]++;
      doubleHits.push(`${key}:${corner}`);
    }
  }
  console.log(`${name}: ${cells.size} static cells; tile17 NW=${count.NW} NE=${count.NE} SE=${count.SE} SW=${count.SW}; ${hits.join(' ')}`);
  console.log(`${name}: tile18 SW+SE=${doubleCount['SW+SE']} NW+SW=${doubleCount['NW+SW']} NW+NE=${doubleCount['NW+NE']} NE+SE=${doubleCount['NE+SE']}; ${doubleHits.join(' ')}`);
}
