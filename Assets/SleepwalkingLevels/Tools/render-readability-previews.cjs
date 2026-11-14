const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const output = path.join(root, 'Previews');
fs.mkdirSync(output, { recursive: true });

const palette = {
  '29a9024f91e149b1a536749020705201': '#222036',
  '9fe261a8a79d4e2f8eb02453a16760ad': '#E04020',
  'a2808d8c22634bc88597821c15ef526f': '#8C5050',
  'd46c146ebae84b9ea11a89ae30004168': '#F8D4B8',
  'ed863a484b484705a574eab353438e42': '#161318',
  '652872eff77a4ec39bd9909cd99c6499': '#B4B8D4',
  '0df3af42442e44b7bc42d670bf3e274f': '#229EDE',
};
const styles = [
  { name: 'Meat', bg: '#582C30', main: '#B4202A' },
  { name: 'PostApocalypse', bg: '#3C2030', main: '#8C5050' },
  { name: 'DeepWater', bg: '#222036', main: '#229EDE' },
  { name: 'Heaven', bg: '#B4B8D4', main: '#F8D4B8' },
];

function field(block, key) {
  return block.match(new RegExp(`^  ${key}: (.+)$`, 'm'))?.[1] || '';
}
function vec(block, key) {
  const value = field(block, key);
  return ['x', 'y', 'z'].map(axis => Number(value.match(new RegExp(`${axis}: (-?[0-9.eE+-]+)`))?.[1] || 0));
}
function load(level) {
  const text = fs.readFileSync(path.join(root, 'Prefabs', `L${String(level).padStart(2, '0')}_DreamGeometry.prefab`), 'utf8');
  const headers = [...text.matchAll(/^--- !u!(\d+) &(\d+)\r?\n/gm)];
  const blocks = headers.map((header, index) => ({
    id: header[2], type: header[1],
    text: text.slice(header.index + header[0].length, headers[index + 1]?.index || text.length),
  }));
  const names = new Map();
  const transforms = new Map();
  const transformByGo = new Map();
  const meshes = new Map();
  const materials = new Map();
  for (const block of blocks) {
    if (block.type === '1') names.set(block.id, field(block.text, 'm_Name'));
    const go = field(block.text, 'm_GameObject').match(/fileID: (\d+)/)?.[1];
    if (block.type === '4') {
      transforms.set(block.id, {
        go, parent: field(block.text, 'm_Father').match(/fileID: (\d+)/)?.[1],
        position: vec(block.text, 'm_LocalPosition'), scale: vec(block.text, 'm_LocalScale'),
      });
      transformByGo.set(go, block.id);
    }
    if (block.type === '33') meshes.set(go, field(block.text, 'm_Mesh'));
    if (block.type === '23' && field(block.text, 'm_Enabled') === '1')
      materials.set(go, block.text.match(/m_Materials:\r?\n  - \{fileID: \d+, guid: ([0-9a-f]{32})/)?.[1]);
  }
  function world(go) {
    let transform = transforms.get(transformByGo.get(go));
    let x = 0, y = 0, z = 0, sx = 1, sy = 1, sz = 1;
    const ancestors = [];
    while (transform) {
      ancestors.push(names.get(transform.go));
      x = transform.position[0] + x * transform.scale[0];
      y = transform.position[1] + y * transform.scale[1];
      z = transform.position[2] + z * transform.scale[2];
      sx *= transform.scale[0]; sy *= transform.scale[1]; sz *= transform.scale[2];
      transform = transforms.get(transform.parent);
    }
    return { x, y, z, sx, sy, sz, ancestors };
  }
  return [...materials].map(([go, guid]) => ({
    name: names.get(go), mesh: meshes.get(go), color: palette[guid], ...world(go),
  })).filter(x => x.color && x.mesh);
}

function render(level, style, centerX, centerY, name, player = null) {
  const shapes = load(level).sort((a, b) => b.z - a.z);
  const ppu = 24;
  const px = x => 160 + (x - centerX) * ppu;
  const py = y => 120 + (centerY - y) * ppu;
  const parts = [`<svg xmlns="http://www.w3.org/2000/svg" width="320" height="240" viewBox="0 0 320 240">`,
    `<rect width="320" height="240" fill="${style.bg}"/>`];
  for (const shape of shapes) {
    const color = shape.ancestors.includes('Geometry') ? style.main : shape.color;
    const left = px(shape.x - shape.sx / 2), right = px(shape.x + shape.sx / 2);
    const top = py(shape.y + shape.sy / 2), bottom = py(shape.y - shape.sy / 2);
    if (shape.name.includes('Spikes_') || shape.name.endsWith('_Base') || shape.mesh.includes('TrianglePrism'))
      parts.push(`<polygon points="${left},${bottom} ${right},${bottom} ${px(shape.x)},${top}" fill="${color}"/>`);
    else if (shape.mesh.includes('10207'))
      parts.push(`<ellipse cx="${px(shape.x)}" cy="${py(shape.y)}" rx="${shape.sx * ppu / 2}" ry="${shape.sy * ppu / 2}" fill="${color}"/>`);
    else
      parts.push(`<rect x="${left}" y="${top}" width="${right - left}" height="${bottom - top}" fill="${color}"/>`);
  }
  if (player)
    parts.push(`<rect x="${px(player.x - 0.275)}" y="${py(player.y + 0.5)}" width="${0.55 * ppu}" height="${ppu}" fill="#F8D4B8"/>`);
  parts.push('</svg>');
  fs.writeFileSync(path.join(output, name + '.svg'), parts.join('\n') + '\n');
}

for (const style of styles)
  render(1, style, 3, 1.8, `L01_${style.name}_route`, { x: 1, y: 0.51 });
for (const style of styles)
  render(7, style, 5.4, 3.0, `L07_${style.name}_wall`, { x: 3.25, y: 1.35 });
render(2, styles[0], 6.2, 1.8, 'L02_Meat_spikes');
render(3, styles[1], 6.4, 1.8, 'L03_PostApocalypse_saw');
render(4, styles[0], 5.4, 1.8, 'L04_Meat_button_laser');
render(5, styles[2], 5.8, 1.8, 'L05_DeepWater_timed');
render(6, styles[3], 6.5, 1.8, 'L06_Heaven_moving');
render(7, styles[0], 18.7, 3.8, 'L07_Meat_exit');
console.log('Wrote fourteen schematic 320x240 preview frames.');
