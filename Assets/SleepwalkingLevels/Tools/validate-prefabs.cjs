const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const prefabDir = path.join(root, 'Prefabs');
const expected = [
  { buttons: 0, lasers: 0, timed: 0, moving: 0, platforms: 3 },
  { buttons: 0, lasers: 0, timed: 0, moving: 0, platforms: 3 },
  { buttons: 0, lasers: 0, timed: 0, moving: 0, platforms: 3 },
  { buttons: 1, lasers: 2, timed: 0, moving: 0, platforms: 2 },
  { buttons: 0, lasers: 0, timed: 3, moving: 0, platforms: 2, oneWay: 4 },
  { buttons: 0, lasers: 0, timed: 0, moving: 2, platforms: 3, oneWay: 4 },
  { buttons: 2, lasers: 2, timed: 0, moving: 0, platforms: 5 },
];

function check(ok, message) { if (!ok) throw new Error(message); }
function count(text, pattern) { return [...text.matchAll(pattern)].length; }

for (let q = 1; q <= 7; q++) {
  const file = path.join(prefabDir, `L${String(q).padStart(2, '0')}_DreamGeometry.prefab`);
  const text = fs.readFileSync(file, 'utf8');
  const sections = text.split(/^--- !u!/m).slice(1);
  const ids = sections.map(x => x.match(/^\d+ &(\d+)/)?.[1]);
  const idSet = new Set(ids);
  check(idSet.size === ids.length && !ids.includes(undefined), `${file}: duplicate or missing fileID`);
  for (const match of text.matchAll(/(?:m_Father|component|playerStart|bedExit): \{fileID: (\d+)\}/g))
    check(match[1] === '0' || idSet.has(match[1]), `${file}: dangling reference ${match[1]}`);

  check(count(text, /^  m_Name: PlayerStart$/gm) === 1, `${file}: PlayerStart`);
  check(count(text, /^  m_Name: BedExit$/gm) === 1, `${file}: BedExit`);
  check(count(text, /^  m_Name: FallReset$/gm) === 1, `${file}: FallReset`);
  check(count(text, /^  levelIndex: /gm) === 1, `${file}: level definition`);
  check(text.includes(`levelIndex: ${q}\n`), `${file}: incorrect index`);
  check(count(text, /^  kind: 8$/gm) === 1, `${file}: fall reset feature`);
  check(count(text, /^  kind: 6$/gm) === expected[q - 1].buttons, `${file}: buttons`);
  check(count(text, /^  kind: [23]$/gm) === expected[q - 1].lasers, `${file}: laser components`);
  check(count(text, /^  kind: 4$/gm) === expected[q - 1].timed, `${file}: timed platforms`);
  check(count(text, /^  kind: 5$/gm) === expected[q - 1].moving, `${file}: moving platforms`);
  const oneWay = expected[q - 1].oneWay || 0;
  check(count(text, /^PlatformEffector2D:$/gm) === oneWay, `${file}: one-way effectors`);
  check(count(text, /^  m_UsedByEffector: 1$/gm) === oneWay, `${file}: one-way collider flags`);
  check(count(text, /^  m_UseOneWay: 1$/gm) === oneWay, `${file}: one-way behavior`);
  check(count(text, /^  m_SurfaceArc: 90$/gm) === oneWay, `${file}: one-way surface arc`);
  check(count(text, /guid: 4bfe2fba776b456c9dd9fa1feea558c7/g) ===
    expected[q - 1].platforms + expected[q - 1].timed + expected[q - 1].moving,
    `${file}: tile surface markers`);
  check(count(text, /^  m_Name: .*_SafeFace$/gm) === 0, `${file}: legacy solid face overlays`);
  check(count(text, /^  m_Name: Mechanism_DarkFace$/gm) === 0, `${file}: legacy mechanism overlays`);
  check(count(text, /^  m_Name: .*_SafeEdge$/gm) === expected[q - 1].platforms, `${file}: safe edges`);
  check(count(text, /^  m_Name: Bed_(?:Silhouette|Headboard|Footboard|Blanket)$/gm) === 4, `${file}: bed silhouette`);
  check(count(text, /^  m_Name: (?:Frame|Pillow)_SeatedVisual$/gm) === (q === 4 ? 2 : 0),
    `${file}: seated L04 bed visuals`);
  check(count(text, /^  m_Name: Timed_Pulse_[\d-]+$/gm) === expected[q - 1].timed * 3, `${file}: timed pulse tabs`);
  check(count(text, /^  m_Name: Moving_Direction(?:Left|Right)$/gm) === expected[q - 1].moving * 2, `${file}: moving direction marks`);
  check(count(text, /^  m_Name: L06_MoverWait_(?:Dot_[0-2]|Chevron(?:TopOuter|TopInner|Tip|BottomInner|BottomOuter))$/gm) === (q === 6 ? 8 : 0),
    `${file}: stationary mover wait cue`);

  const featureIds = new Set([...text.matchAll(/^  featureId: (.+)$/gm)].map(x => x[1]));
  for (const link of text.matchAll(/^  linkedFeatureId: (.+)$/gm))
    check(featureIds.has(link[1]), `${file}: unresolved button link ${link[1]}`);
  for (const section of sections.filter(x => x.startsWith('61 &')))
    check(count(section, /^  m_Offset:/gm) === 1, `${file}: duplicate or missing 2D collider offset`);
  const names = new Map();
  const components = new Map();
  for (const section of sections) {
    const gameObject = section.match(/^1 &(\d+)\r?\nGameObject:/)?.[1];
    if (gameObject) names.set(gameObject, section.match(/^  m_Name: (.+)$/m)?.[1]);
    const type = section.match(/^\d+ &\d+\r?\n(\w+):/)?.[1];
    const owner = section.match(/^  m_GameObject: \{fileID: (\d+)\}/m)?.[1];
    if (type && owner) {
      if (!components.has(owner)) components.set(owner, new Set());
      components.get(owner).add(type);
    }
  }
  for (const [owner, types] of components) {
    const has2D = [...types].some(x => x.endsWith('Collider2D'));
    const has3D = [...types].some(x => ['BoxCollider', 'SphereCollider', 'MeshCollider'].includes(x));
    check(!has2D || !has3D, `${file}: mixed physics colliders on ${names.get(owner)}`);
    if (names.get(owner) === 'FallReset')
      check(!types.has('MeshRenderer') && !types.has('MeshFilter'), `${file}: visible fall reset`);
  }
  if (q === 4 || q === 7) {
    check(text.includes('beamPixelWidth: 3.5'), `${file}: beam pixel setting`);
    check(text.includes('m_LocalScale: {x: 0.2,'), `${file}: beam preview width`);
    check(text.includes('m_Convex: 1'), `${file}: triangular base collider`);
  }
  console.log(`L${String(q).padStart(2, '0')}: ${sections.length} serialized objects, ${featureIds.size} feature IDs, links valid`);
}
console.log('All seven prefab structures passed.');
