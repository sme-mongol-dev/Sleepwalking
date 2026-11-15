const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const prefabDir = path.join(root, 'Prefabs');
const expected = [
  { buttons: 0, lasers: 0, timed: 0, moving: 0, platforms: 6 },
  { buttons: 0, lasers: 0, timed: 0, moving: 0, platforms: 6 },
  { buttons: 0, lasers: 0, timed: 0, moving: 0, platforms: 6 },
  { buttons: 2, lasers: 4, timed: 0, moving: 0, platforms: 4 },
  { buttons: 0, lasers: 0, timed: 5, moving: 0, platforms: 3 },
  { buttons: 0, lasers: 0, timed: 0, moving: 3, platforms: 4 },
  { buttons: 3, lasers: 4, timed: 0, moving: 0, platforms: 9 },
  { buttons: 2, lasers: 2, timed: 0, moving: 1, platforms: 7 },
];
const caveBlocks = [18, 18, 18, 9, 19, 13, 19, 7];
const ends = [27, 32, 29, 25, 41, 42, 44, 26];
const visibleGapIds = [
  ['L01_GapA', 'L01_GapB', 'L01_GapC', 'L01_GapD', 'L01_GapE'],
  ['L02_GapA', 'L02_GapB', 'L02_GapC'],
  ['L03_GapA', 'L03_GapB', 'L03_GapC', 'L03_GapD', 'L03_GapE'],
  ['L04_GapA'], [], ['L06_FloorSpikes'], ['L07_GapA', 'L07_GapB'],
  [],
];

function check(ok, message) { if (!ok) throw new Error(message); }
function count(text, pattern) { return [...text.matchAll(pattern)].length; }

for (let q = 1; q <= expected.length; q++) {
  const file = path.join(prefabDir, `L${String(q).padStart(2, '0')}_DreamGeometry.prefab`);
  const text = fs.readFileSync(file, 'utf8').replace(/\r\n/g, '\n');
  const sections = text.split(/^--- !u!/m).slice(1);
  const ids = sections.map(x => x.match(/^\d+ &(\d+)/)?.[1]);
  const idSet = new Set(ids);
  check(idSet.size === ids.length && !ids.includes(undefined), `${file}: duplicate or missing fileID`);
  for (const match of text.matchAll(/(?:m_Father|component|playerStart|bedExit): \{fileID: (\d+)\}/g))
    check(match[1] === '0' || idSet.has(match[1]), `${file}: dangling reference ${match[1]}`);

  check(count(text, /^  m_Name: PlayerStart$/gm) === 1, `${file}: PlayerStart`);
  check(count(text, /^  m_Name: BedExit$/gm) === 1, `${file}: BedExit`);
  check(!text.includes('FallReset'), `${file}: invisible kill plane`);
  check(count(text, /^  m_Name: CaveShell$/gm) === 1, `${file}: cave shell group`);
  check(count(text, /^  m_Name: Cave_[^\r\n]+$/gm) === caveBlocks[q - 1], `${file}: cave fill blocks`);
  check(count(text, /^  m_Name: Cave_Bedrock$/gm) === 1, `${file}: bottom tile mass`);
  check(count(text, /^  m_Name: Cave_(?:Left|Right)Wall$/gm) === 2, `${file}: side walls`);
  check(count(text, /^  m_Name: Cave_(?:Left|Right)Roof$/gm) === (q === 8 ? 0 : 2), `${file}: side roofs`);
  check(count(text, /^  levelIndex: /gm) === 1, `${file}: level definition`);
  check(text.includes(`levelIndex: ${q}\n`), `${file}: incorrect index`);
  check(count(text, /^  kind: 8$/gm) === 0, `${file}: invisible fall reset feature`);
  check(count(text, /^  kind: 0$/gm) > 0, `${file}: no visible pit hazard`);
  check(count(text, /^  kind: 6$/gm) === expected[q - 1].buttons, `${file}: buttons`);
  check(count(text, /^  kind: [23]$/gm) === expected[q - 1].lasers, `${file}: laser components`);
  check(count(text, /^  kind: 4$/gm) === expected[q - 1].timed, `${file}: timed platforms`);
  check(count(text, /^  kind: 5$/gm) === expected[q - 1].moving, `${file}: moving platforms`);
  check(count(text, /^PlatformEffector2D:$/gm) === 0, `${file}: non-solid one-way surfaces`);
  check(count(text, /^  m_UsedByEffector: 1$/gm) === 0, `${file}: collider must block every side`);
  check(count(text, /guid: 4bfe2fba776b456c9dd9fa1feea558c7/g) ===
    expected[q - 1].platforms + expected[q - 1].timed + expected[q - 1].moving + caveBlocks[q - 1],
    `${file}: tile surface markers`);
  check(count(text, /^  IsShell: 1$/gm) === caveBlocks[q - 1], `${file}: shell tile markers`);
  check(count(text, /^  m_Name: .*_SafeFace$/gm) === 0, `${file}: legacy solid face overlays`);
  check(count(text, /^  m_Name: Mechanism_DarkFace$/gm) === 0, `${file}: legacy mechanism overlays`);
  check(count(text, /^  m_Name: .*_SafeEdge$/gm) === 0, `${file}: platform underlay strips`);
  check(count(text, /^  m_Name: Bed_(?:Silhouette|Headboard|Footboard|Blanket)$/gm) === 4, `${file}: bed silhouette`);
  check(count(text, /^  m_Name: (?:Frame|Pillow)_SeatedVisual$/gm) === 0,
    `${file}: old bed visuals`);
  check(count(text, /^  m_Name: Timed_Pulse_[\d-]+$/gm) === expected[q - 1].timed * 3, `${file}: timed pulse tabs`);
  check(count(text, /^  m_Name: Moving_Direction(?:Left|Right)$/gm) === expected[q - 1].moving * 2, `${file}: moving direction marks`);
  check(count(text, /^  m_Name: L06_MoverWait_(?:Dot_[0-2]|Chevron(?:TopOuter|TopInner|Tip|BottomInner|BottomOuter))$/gm) === (q === 6 ? 8 : 0),
    `${file}: stationary mover wait cue`);

  const featureIds = new Set([...text.matchAll(/^  featureId: (.+)$/gm)].map(x => x[1]));
  for (const link of text.matchAll(/^  linkedFeatureId: (.+)$/gm))
    check([...featureIds].some(id => id === link[1] || id.startsWith(`${link[1]}_`)),
      `${file}: unresolved button link ${link[1]}`);
  for (const section of sections.filter(x => x.startsWith('61 &') || x.startsWith('60 &')))
    check(count(section, /^  m_Offset:/gm) === 1, `${file}: duplicate or missing 2D collider offset`);
  const names = new Map();
  const components = new Map();
  const transforms = new Map();
  const tileOwners = new Set();
  for (const section of sections) {
    const gameObject = section.match(/^1 &(\d+)\r?\nGameObject:/)?.[1];
    if (gameObject) names.set(gameObject, section.match(/^  m_Name: (.+)$/m)?.[1]);
    const type = section.match(/^\d+ &\d+\r?\n(\w+):/)?.[1];
    const owner = section.match(/^  m_GameObject: \{fileID: (\d+)\}/m)?.[1];
    if (type === 'Transform' && owner) {
      const position = section.match(/^  m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}/m);
      const scale = section.match(/^  m_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}/m);
      check(position && scale, `${file}: transform data on ${names.get(owner)}`);
      transforms.set(owner, { x: +position[1], y: +position[2], w: +scale[1], h: +scale[2] });
    }
    if (section.includes(`guid: 4bfe2fba776b456c9dd9fa1feea558c7`) && owner)
      tileOwners.add(owner);
    if (type && owner) {
      if (!components.has(owner)) components.set(owner, new Set());
      components.get(owner).add(type);
    }
  }
  check(text.includes('m_LocalScale: {x: 32, y: 32, z: 1}'), `${file}: 32-unit cell root scale`);
  const bed = [...names].find(([, name]) => name === 'BedExit')?.[0];
  const bedPosition = transforms.get(bed);
  check(bedPosition && bedPosition.x > ends[q - 1] - 3 && bedPosition.x < ends[q - 1],
    `${file}: bed is not at the extended finish`);
  const sawSupports = q === 3 ? [['L03_Saw', 'SawRun'], ['L03_SecondSaw', 'SecondSawRun']] :
    q === 7 ? [['L07_Saw', 'DashLanding']] : [];
  for (const [sawName, supportName] of sawSupports) {
    const saw = transforms.get([...names].find(([, name]) => name === sawName)?.[0]);
    const support = transforms.get([...names].find(([, name]) => name === supportName)?.[0]);
    check(saw && support && Math.abs(saw.y - (support.y + support.h / 2)) < 0.0001,
      `${file}: ${sawName} axis must sit on ${supportName} top edge`);
    check(saw.x > support.x - support.w / 2 && saw.x < support.x + support.w / 2,
      `${file}: ${sawName} axis must remain over ${supportName}`);
  }
  if (q === 7) {
    const saw = transforms.get([...names].find(([, name]) => name === 'L07_Saw')?.[0]);
    const roof = transforms.get([...names].find(([, name]) => name === 'SawRoof')?.[0]);
    check(saw && roof && Math.abs(saw.w - 2) < 0.0001,
      `${file}: L07 saw art and collider must share two-cell diameter`);
    check(Math.abs(roof.y - roof.h / 2 - 5) < 0.0001 &&
      saw.x > roof.x - roof.w / 2 && saw.x < roof.x + roof.w / 2,
      `${file}: L07 saw roof must leave a two-cell passage above the support`);
  }
  const staticCells = new Map();
  for (const owner of tileOwners) {
    const transform = transforms.get(owner);
    const name = names.get(owner);
    check(transform && name, `${file}: missing tiled block transform`);
    const left = transform.x - transform.w / 2;
    const right = transform.x + transform.w / 2;
    const bottom = transform.y - transform.h / 2;
    const top = transform.y + transform.h / 2;
    for (const edge of [left, right, bottom, top])
      check(Math.abs(edge - Math.round(edge)) < 0.0001, `${file}: off-grid solid ${name} at ${edge}`);
    if (name.startsWith('L05_Timed') || name.startsWith('L06_')) continue;
    for (let x = left; x < right; x++)
    for (let y = bottom; y < top; y++) {
      const key = `${x},${y}`;
      check(!staticCells.has(key), `${file}: overlapping static cells ${name} and ${staticCells.get(key)}`);
      staticCells.set(key, name);
    }
  }
  check(staticCells.size > 0, `${file}: empty solid matrix`);
  for (const owner of tileOwners) {
    const types = components.get(owner) || new Set();
    check(types.has('PolygonCollider2D') && !types.has('BoxCollider2D'),
      `${file}: solid tile must have a shaped 2D collider: ${names.get(owner)}`);
  }
  for (let x = 0; x < ends[q - 1]; x++) {
    const floorRows = q === 8 ? [-4, -3, -2] : [-3];
    check(floorRows.some(y => staticCells.has(`${x},${y}`)),
      `${file}: open shaft at x=${x} reaches hidden bedrock`);
  }
  for (const id of visibleGapIds[q - 1]) {
    check(text.includes(`m_Name: Cave_${id}Foundation\n`), `${file}: missing raised support for ${id}`);
    check(text.includes(`m_Name: ${id}_1\n`), `${file}: missing visible spikes for ${id}`);
  }
  for (const [owner, types] of components) {
    const has2D = [...types].some(x => x.endsWith('Collider2D'));
    const has3D = [...types].some(x => ['BoxCollider', 'SphereCollider', 'MeshCollider'].includes(x));
    check(!has2D || !has3D, `${file}: mixed physics colliders on ${names.get(owner)}`);
    if (names.get(owner)?.startsWith('Cave_'))
      check(types.has('PolygonCollider2D') && types.has('MonoBehaviour'),
        `${file}: cave fill must have a solid 2D collider and tile marker`);
  }
  if (q === 4 || q === 7 || q === 8) {
    check(text.includes('beamPixelWidth: 3.5'), `${file}: beam pixel setting`);
    check(text.includes('m_LocalScale: {x: 0.2,'), `${file}: beam preview width`);
    check(text.includes('m_Convex: 1'), `${file}: triangular base collider`);
  }
  console.log(`L${String(q).padStart(2, '0')}: ${sections.length} serialized objects, ${featureIds.size} feature IDs, links valid`);
}
console.log(`All ${expected.length} prefab structures passed.`);
