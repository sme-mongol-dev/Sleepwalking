const fs = require('fs');
const path = require('path');
const { shapePrefabNodes } = require('./tile-collider-shape.cjs');

const root = path.resolve(__dirname, '..');
const prefabsOnly = process.argv.includes('--prefabs-only');
const prefabDir = path.join(root, 'Prefabs');
const materialDir = path.join(root, 'Materials');
fs.mkdirSync(prefabDir, { recursive: true });
fs.mkdirSync(materialDir, { recursive: true });

const definitionGuid = '44f6156829004951a3db50a75ebf213b';
const featureGuid = '7e6aae69be1d492faedd534c79fd2b07';
const visualStateGuid = 'f2566cb00eac41bd906db9249e6ebb86';
const tileSurfaceGuid = '4bfe2fba776b456c9dd9fa1feea558c7';
const meshGuid = 'c9d01e94b9ca4d87a2d12692cc8a0213';
const materialGuids = {
  Floor: '29a9024f91e149b1a536749020705201',
  Hazard: '9fe261a8a79d4e2f8eb02453a16760ad',
  Mechanism: 'a2808d8c22634bc88597821c15ef526f',
  Exit: 'd46c146ebae84b9ea11a89ae30004168',
  SafeFace: 'ed863a484b484705a574eab353438e42',
  SafeEdge: '652872eff77a4ec39bd9909cd99c6499',
  Button: '0df3af42442e44b7bc42d670bf3e274f',
};

function save(file, content) { fs.writeFileSync(file, content.replace(/\r\n/g, '\n'), 'utf8'); }
function levelEnd(index) { return index === 1 ? 27 : index === 2 ? 32 : index === 3 ? 29 :
  index === 4 ? 25 : index === 5 ? 41 : index === 6 ? 42 : 44; }
function meta(file, guid, importer = 'DefaultImporter') {
  save(file + '.meta', `fileFormatVersion: 2\nguid: ${guid}\n${importer}:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n`);
}
function folderMeta(folder, guid) {
  save(folder + '.meta', `fileFormatVersion: 2\nguid: ${guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n`);
}
if (!prefabsOnly) {
folderMeta(prefabDir, 'a68406f74b1a44a1b746f071cd0d8b11');
folderMeta(materialDir, '39703ac3bce54efb9a040b25ff8d2862');

const colors = {
  Floor: [34, 32, 54], Hazard: [224, 64, 32],
  Mechanism: [140, 80, 80], Exit: [248, 212, 184],
  SafeFace: [22, 19, 24], SafeEdge: [180, 184, 212], Button: [34, 158, 222],
};
for (const [name, rgb] of Object.entries(colors)) {
  const c = rgb.map(x => +(x / 255).toFixed(7));
  save(path.join(materialDir, name + '.mat'), `%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: ${name}
  m_Shader: {fileID: 4800000, guid: 650dd9526735d5b46b79224bc6e94025, type: 3}
  m_Parent: {fileID: 0}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {}
  disabledShaderPasses: []
  m_LockedProperties:
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs: []
    m_Ints: []
    m_Floats: []
    m_Colors:
    - _BaseColor: {r: ${c[0]}, g: ${c[1]}, b: ${c[2]}, a: 1}
    - _Color: {r: ${c[0]}, g: ${c[1]}, b: ${c[2]}, a: 1}
  m_BuildTextureStacks: []
`);
  meta(path.join(materialDir, name + '.mat'), materialGuids[name], 'NativeFormatImporter');
}

const obj = `o TrianglePrism
v -0.5 -0.5 -0.5
v 0.5 -0.5 -0.5
v 0 0.5 -0.5
v -0.5 -0.5 0.5
v 0.5 -0.5 0.5
v 0 0.5 0.5
f 1 3 2
f 4 5 6
f 1 2 5 4
f 2 3 6 5
f 3 1 4 6
`;
save(path.join(root, 'TrianglePrism.obj'), obj);
meta(path.join(root, 'TrianglePrism.obj'), meshGuid, 'ModelImporter');
}

function num(x, precision = 5) { return Number.isInteger(x) ? String(x) : String(+x.toFixed(precision)); }
function vec2(x, y) { return `{x: ${num(x)}, y: ${num(y)}}`; }
function vec3(x, y, z, precision = 5) { return `{x: ${num(x, precision)}, y: ${num(y, precision)}, z: ${num(z, precision)}}`; }
function ref(id) { return `{fileID: ${id}}`; }

class Prefab {
  constructor(index) {
    this.index = index;
    this.nextId = index * 100000000000 + 1000;
    this.nodes = [];
    this.blocks = [];
    this.root = this.node(`L${String(index).padStart(2, '0')}_DreamGeometry`);
    this.root.sx = this.root.sy = 32;
    this.geometry = this.node('Geometry', this.root);
    this.hazards = this.node('Hazards', this.root);
    this.mechanisms = this.node('Mechanisms', this.root);
    this.markers = this.node('Markers', this.root);
    this.readability = this.node('Readability', this.root);
  }
  id() { return ++this.nextId; }
  node(name, parent = null, x = 0, y = 0, z = 0, sx = 1, sy = 1, sz = 1) {
    const n = { name, parent, x, y, z, sx, sy, sz, go: this.id(), transform: this.id(), components: [], children: [] };
    if (parent) parent.children.push(n);
    this.nodes.push(n);
    return n;
  }
  component(n, classId, type, data) {
    const c = { id: this.id(), classId, type, data };
    n.components.push(c);
    return c;
  }
  cube(parent, name, x, y, width, height, material, trigger = false, feature = null) {
    const n = this.node(name, parent, x, y, 0, width, height, 0.8);
    this.component(n, 33, 'MeshFilter', `  m_Mesh: {fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}`);
    this.renderer(n, material);
    this.boxColliders(n, trigger);
    if (feature) this.feature(n, feature);
    return n;
  }
  visualCube(parent, name, x, y, z, width, height, depth, material) {
    const n = this.node(name, parent, x, y, z, width, height, depth);
    this.component(n, 33, 'MeshFilter', `  m_Mesh: {fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}`);
    this.renderer(n, material);
    return n;
  }
  visualState(n) {
    this.component(n, 114, 'MonoBehaviour', `  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${visualStateGuid}, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Sleepwalking.Levels.SleepwalkingLevelVisualState`);
  }
  tileSurface(n, shell = false) {
    this.component(n, 114, 'MonoBehaviour', `  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${tileSurfaceGuid}, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Sleepwalking.Levels.SleepwalkingTileSurface
  IsShell: ${shell ? 1 : 0}`);
  }
  triangle(parent, name, x, bottom, width, height, feature) {
    const n = this.node(name, parent, x, bottom + height / 2, 0, width, height, 0.8);
    this.component(n, 33, 'MeshFilter', `  m_Mesh: {fileID: 4300000, guid: ${meshGuid}, type: 3}`);
    this.renderer(n, 'Hazard');
    const collision3D = this.node('Collision3D', n);
    this.component(collision3D, 64, 'MeshCollider', `  m_Material: {fileID: 0}
  m_IsTrigger: 1
  m_Enabled: 1
  m_Convex: 1
  m_CookingOptions: 30
  m_Mesh: {fileID: 4300000, guid: ${meshGuid}, type: 3}`);
    this.component(n, 60, 'PolygonCollider2D', this.polygonCollider());
    this.feature(n, feature);
    this.visualState(n);
    return n;
  }
  saw(parent, name, x, y, diameter = 0.75) {
    const n = this.node(name, parent, x, y, 0, diameter, diameter, 0.8);
    this.component(n, 33, 'MeshFilter', `  m_Mesh: {fileID: 10207, guid: 0000000000000000e000000000000000, type: 0}`);
    this.renderer(n, 'Hazard');
    const collision3D = this.node('Collision3D', n);
    this.component(collision3D, 135, 'SphereCollider', `  m_Material: {fileID: 0}
  m_IsTrigger: 1
  m_Enabled: 1
  serializedVersion: 3
  m_Radius: 0.5
  m_Center: {x: 0, y: 0, z: 0}`);
    this.component(n, 58, 'CircleCollider2D', this.collider2d(true, '  m_Radius: 0.5'));
    this.feature(n, { kind: 1, id: name });
    this.visualCube(n, 'Saw_Hub', 0, 0, -0.7, 0.25, 0.25, 0.06, 'SafeFace');
    this.visualState(n);
  }
  renderer(n, material) {
    this.component(n, 23, 'MeshRenderer', `  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 4294967295
  m_RendererPriority: 0
  m_Materials:
  - {fileID: 2100000, guid: ${materialGuids[material]}, type: 2}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {fileID: 0}
  m_ProbeAnchor: {fileID: 0}
  m_LightProbeVolumeOverride: {fileID: 0}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 1
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 0
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {fileID: 0}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_AdditionalVertexStreams: {fileID: 0}`);
  }
  collider3d(trigger) {
    return `  m_Material: {fileID: 0}
  m_IsTrigger: ${trigger ? 1 : 0}
  m_Enabled: 1
  serializedVersion: 3
  m_Size: {x: 1, y: 1, z: 1}
  m_Center: {x: 0, y: 0, z: 0}`;
  }
  collider2d(trigger, extra = '  m_Size: {x: 1, y: 1}') {
    return `  m_Enabled: 1
  m_Density: 1
  m_Material: {fileID: 0}
  m_IsTrigger: ${trigger ? 1 : 0}
  m_UsedByEffector: 0
  m_Offset: {x: 0, y: 0}
${extra}`;
  }
  polygonCollider() {
    return `  m_Enabled: 1
  m_Density: 1
  m_Material: {fileID: 0}
  m_IsTrigger: 1
  m_UsedByEffector: 0
  m_Offset: {x: 0, y: 0}
  m_Points:
    m_Paths:
    - - {x: -0.5, y: -0.5}
      - {x: 0.5, y: -0.5}
      - {x: 0, y: 0.5}`;
  }
  boxColliders(n, trigger) {
    this.component(n, 61, 'BoxCollider2D', this.collider2d(trigger));
    const collision3D = this.node('Collision3D', n);
    this.component(collision3D, 65, 'BoxCollider', this.collider3d(trigger));
  }
  feature(n, f) {
    this.component(n, 114, 'MonoBehaviour', `  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${featureGuid}, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Sleepwalking.Levels.SleepwalkingLevelFeature
  kind: ${f.kind}
  featureId: ${f.id}
  linkedFeatureId:${f.link ? ' ' + f.link : ''}
  activeSeconds: ${num(f.duration || 0)}
  moveOffset: ${vec2(...(f.move || [0, 0]))}
  cycleSeconds: ${num(f.cycle || 0)}
  phaseSeconds: ${num(f.phase || 0)}
  beamPixelWidth: ${num(f.beamPixels || 0)}`);
  }
  platform(name, left, right, top, height = 1) {
    const block = this.cube(this.geometry, name, (left + right) / 2, top - height / 2, right - left, height, 'Floor');
    this.tileSurface(block);
  }
  caveBlock(name, left, right, bottom, top) {
    const block = this.cube(this.cave, name, (left + right) / 2, (bottom + top) / 2,
      right - left, top - bottom, 'Floor');
    this.tileSurface(block, true);
  }
  visiblePit(id, left, right, top) {
    this.caveBlock(`Cave_${id}Foundation`, left, right, -4, top);
    this.spikes(id, left, right, top, Math.round((right - left) * 2));
  }
  caveShell() {
    const floor = -4;
    const roof = 28;
    this.cave = this.node('CaveShell', this.geometry);
    const end = levelEnd(this.index);
    let edges = this.index === 4 ? [0, end] : [0, 13, end];
    let heights = this.index === 4 ? [5] : [5, 7];
    if (this.index === 5) {
      edges = [0, 3, 6, 9, 13, 18, 23, 26, 29, end];
      heights = [5, 7, 9, 7, 7, 5, 7, 9, 8];
    } else if (this.index === 6) {
      edges = [0, 3, 16, 20, 23, end];
      heights = [3, 5, 6, 8, 9];
    } else if (this.index === 7) {
      edges = [0, 3, 22, 32, end];
      heights = [5, 8, 10, 10];
    }

    this.caveBlock('Cave_Bedrock', -8, end + 8, -20, floor);
    this.caveBlock('Cave_LeftWall', -8, 0, floor, heights[0]);
    this.caveBlock('Cave_RightWall', end, end + 8, floor, heights.at(-1));
    this.caveBlock('Cave_LeftRoof', -8, 0, heights[0], roof);
    for (let q = 0; q < heights.length; q++)
      this.caveBlock(`Cave_Roof_${q}`, edges[q], edges[q + 1], heights[q], roof);
    this.caveBlock('Cave_RightRoof', end, end + 8, heights.at(-1), roof);

    if (this.index === 1) {
      this.caveBlock('Cave_StartFoundation', 0, 4, floor, -1);
      this.caveBlock('Cave_LandingFoundation', 5, 9, floor, -1);
      this.caveBlock('Cave_ExitFoundation', 10, 13, floor, 0);
      this.caveBlock('Cave_RiseFoundation', 14, 17, floor, 1);
      this.caveBlock('Cave_ShelfFoundation', 19, 21, floor, 0);
      this.caveBlock('Cave_FinalFoundation', 23, 27, floor, 1);
      this.visiblePit('L01_GapA', 4, 5, -1);
      this.visiblePit('L01_GapB', 9, 10, -1);
      this.visiblePit('L01_GapC', 13, 14, -1);
      this.visiblePit('L01_GapD', 17, 19, -1);
      this.visiblePit('L01_GapE', 21, 23, -1);
    } else if (this.index === 2) {
      this.caveBlock('Cave_StartFoundation', 0, 4, floor, -1);
      this.caveBlock('Cave_PitSpikeFoundation', 4, 5, floor, -1);
      this.caveBlock('Cave_LandingFoundation', 5, 9, floor, -1);
      this.caveBlock('Cave_ExitFoundation', 10, 13, floor, 0);
      this.caveBlock('Cave_SecondPitFoundation', 13, 14, floor, 0);
      this.caveBlock('Cave_BridgeFoundation', 14, 21, floor, 0);
      this.caveBlock('Cave_RecoveryFoundation', 23, 26, floor, 1);
      this.caveBlock('Cave_FinalFoundation', 28, 32, floor, 1);
      this.visiblePit('L02_GapA', 9, 10, -1);
      this.visiblePit('L02_GapB', 21, 23, -1);
      this.visiblePit('L02_GapC', 26, 28, -1);
    } else if (this.index === 3) {
      this.caveBlock('Cave_StartFoundation', 0, 4, floor, -1);
      this.caveBlock('Cave_LandingFoundation', 5, 9, floor, -1);
      this.caveBlock('Cave_ExitFoundation', 10, 13, floor, 0);
      this.caveBlock('Cave_SecondSawFoundation', 14, 19, floor, 0);
      this.caveBlock('Cave_UpperFoundation', 21, 24, floor, 1);
      this.caveBlock('Cave_FinalFoundation', 26, 29, floor, 1);
      this.visiblePit('L03_GapA', 4, 5, -1);
      this.visiblePit('L03_GapB', 9, 10, -1);
      this.visiblePit('L03_GapC', 13, 14, -1);
      this.visiblePit('L03_GapD', 19, 21, -1);
      this.visiblePit('L03_GapE', 24, 26, -1);
    } else if (this.index === 4) {
      this.caveBlock('Cave_FloorFoundation', 0, 13, floor, -1);
      this.caveBlock('Cave_SecondFloorFoundation', 14, 25, floor, 0);
      this.visiblePit('L04_GapA', 13, 14, -1);
    } else if (this.index === 5) {
      this.caveBlock('Cave_StartFoundation', 0, 3, floor, -1);
      this.caveBlock('Cave_PitSpikeFoundation', 3, 19, floor, -1);
      this.caveBlock('Cave_ExitFoundation', 19, 23, floor, -1);
      this.caveBlock('Cave_SecondPitFoundation', 23, 35, floor, -1);
      this.caveBlock('Cave_FinalFoundation', 35, 41, floor, 1);
    } else if (this.index === 6) {
      this.caveBlock('Cave_StartFoundation', 0, 3, floor, -2);
      this.caveBlock('Cave_FinalFoundation', 37, 42, floor, 4);
      this.visiblePit('L06_FloorSpikes', 3, 37, -2);
    } else {
      this.caveBlock('Cave_StartFoundation', 0, 3, floor, -1);
      this.caveBlock('Cave_WallFoundation', 4, 6, floor, 0);
      this.caveBlock('Cave_RunFoundation', 6, 11, floor, 2);
      this.caveBlock('Cave_PitSpikeFoundation', 11, 17, floor, -1);
      this.caveBlock('Cave_ExitFoundation', 17, 21, floor, 2);
      this.caveBlock('Cave_SecondLaserFoundation', 22, 31, floor, 4);
      this.caveBlock('Cave_SecondPitFoundation', 31, 37, floor, -1);
      this.caveBlock('Cave_FinalFoundation', 37, 44, floor, 4);
      this.visiblePit('L07_GapA', 3, 4, -1);
      this.visiblePit('L07_GapB', 21, 22, -1);
    }
  }
  spikes(id, left, right, top, count) {
    const width = Math.fround((right - left) / count);
    for (let q = 0; q < count; q++) {
      const x = Math.fround(left + Math.fround((q + 0.5) * width));
      const spike = this.triangle(this.hazards, `${id}_${q + 1}`, x, top, width, 0.27, { kind: 0, id });
      spike.precisePosition = true;
      spike.preciseScale = true;
    }
  }
  laser(id, x, bottom, top) {
    this.triangle(this.hazards, `${id}_Base`, x, bottom, 0.8, 0.65, { kind: 2, id });
    const beamBottom = bottom + 0.65;
    const beam = this.cube(this.hazards, `${id}_Beam`, x, (beamBottom + top) / 2, 0.2, top - beamBottom,
      'Hazard', true, { kind: 3, id, beamPixels: 3.5 });
    this.visualState(beam);
  }
  mechanismCues(parent, timed) {
    if (timed) {
      for (let q = -1; q <= 1; q++)
        this.visualCube(parent, `Timed_Pulse_${q}`, q * 0.3, 0.32, -0.7, 0.18, 0.38, 0.06, 'Button');
      return;
    }
    this.visualCube(parent, 'Moving_BlueEdge', 0, 0.32, -0.7, 1, 0.38, 0.06, 'Button');
    this.visualCube(parent, 'Moving_DirectionLeft', -0.32, -0.08, -0.72, 0.12, 0.4, 0.06, 'SafeEdge');
    this.visualCube(parent, 'Moving_DirectionRight', 0.32, -0.08, -0.72, 0.12, 0.4, 0.06, 'SafeEdge');
  }
  moverWaitCue() {
    for (let q = 0; q < 3; q++)
      this.visualCube(this.readability, `L06_MoverWait_Dot_${q}`, 2.04 + q * 0.21, -0.55,
        -0.8, 0.13, 0.13, 0.06, 'Button');
    this.visualCube(this.readability, 'L06_MoverWait_ChevronTopOuter', 2.60, -0.31,
      -0.8, 0.14, 0.14, 0.06, 'SafeEdge');
    this.visualCube(this.readability, 'L06_MoverWait_ChevronTopInner', 2.75, -0.43,
      -0.8, 0.14, 0.14, 0.06, 'SafeEdge');
    this.visualCube(this.readability, 'L06_MoverWait_ChevronTip', 2.90, -0.55,
      -0.8, 0.14, 0.14, 0.06, 'SafeEdge');
    this.visualCube(this.readability, 'L06_MoverWait_ChevronBottomInner', 2.75, -0.67,
      -0.8, 0.14, 0.14, 0.06, 'SafeEdge');
    this.visualCube(this.readability, 'L06_MoverWait_ChevronBottomOuter', 2.60, -0.79,
      -0.8, 0.14, 0.14, 0.06, 'SafeEdge');
  }
  timed(id, x, top, phase) {
    const platform = this.cube(this.mechanisms, id, x, top - 0.5, 2, 1, 'Mechanism', false,
      { kind: 4, id, duration: 5.5, cycle: 7, phase });
    this.tileSurface(platform);
    this.mechanismCues(platform, true);
    this.visualState(platform);
  }
  moving(id, x, top, move, cycle) {
    const platform = this.cube(this.mechanisms, id, x, top - 0.5, 2, 1, 'Mechanism', false,
      { kind: 5, id, move, cycle });
    this.tileSurface(platform);
    this.mechanismCues(platform, false);
  }
  button(id, target, x, top, duration) {
    const button = this.cube(this.mechanisms, id, x, top + 0.12, 0.55, 0.24, 'Button', true,
      { kind: 6, id, link: target, duration });
    this.visualCube(button, 'Button_Light', 0, 0, -0.7, 0.32, 0.48, 0.06, 'SafeEdge');
  }
  marker(name, x, y) { return this.node(name, this.markers, x, y); }
  bed(x, top, visualOffset = 0) {
    const bed = this.node('BedExit', this.markers, x, top);
    const collision3D = this.node('Collision3D', bed);
    this.component(collision3D, 65, 'BoxCollider', `  m_Material: {fileID: 0}
  m_IsTrigger: 1
  m_Enabled: 1
  serializedVersion: 3
  m_Size: {x: 1.4, y: 1.5, z: 0.8}
  m_Center: {x: 0, y: 0.75, z: 0}`);
    this.component(bed, 61, 'BoxCollider2D', `  m_Enabled: 1
  m_Density: 1
  m_Material: {fileID: 0}
  m_IsTrigger: 1
  m_UsedByEffector: 0
  m_Offset: {x: 0, y: 0.75}
  m_Size: {x: 1.4, y: 1.5}`);
    this.feature(bed, { kind: 7, id: 'BedExit' });
    this.visualCube(bed, 'Bed_Silhouette', 0, 0.5 + visualOffset, 0.2, 1.5, 0.9, 0.06, 'SafeFace');
    const frame = this.cube(bed, 'Frame', 0, 0.2, 1.25, 0.35, 'Exit', true);
    const pillow = this.cube(bed, 'Pillow', -0.42, 0.46, 0.28, 0.22, 'SafeEdge', true);
    this.visualCube(bed, 'Bed_Headboard', -0.58, 0.56 + visualOffset, -0.56, 0.16, 0.96, 0.07, 'Exit');
    this.visualCube(bed, 'Bed_Footboard', 0.59, 0.39 + visualOffset, -0.56, 0.12, 0.62, 0.07, 'Exit');
    this.visualCube(bed, 'Bed_Blanket', 0.17, 0.39 + visualOffset, -0.58, 0.68, 0.23, 0.07, 'SafeEdge');
    if (visualOffset !== 0) {
      for (const item of [frame, pillow]) {
        const renderer = item.components.find(x => x.type === 'MeshRenderer');
        renderer.data = renderer.data.replace('m_Enabled: 1', 'm_Enabled: 0');
      }
      this.visualCube(bed, 'Frame_SeatedVisual', 0, 0.2 + visualOffset, -0.54, 1.25, 0.35, 0.06, 'Exit');
      this.visualCube(bed, 'Pillow_SeatedVisual', -0.42, 0.46 + visualOffset, -0.55, 0.28, 0.22, 0.06, 'SafeEdge');
    }
    return bed;
  }
  definition(start, exit) {
    this.component(this.root, 114, 'MonoBehaviour', `  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${definitionGuid}, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Sleepwalking.Levels.SleepwalkingLevelDefinition
  levelIndex: ${this.index}
  firstCycleStyle: 0
  playerStart: ${ref(start.transform)}
  bedExit: ${ref(exit.transform)}`);
  }
  write() {
    const lines = ['%YAML 1.1', '%TAG !u! tag:unity3d.com,2011:'];
    for (const n of this.nodes) {
      lines.push(`--- !u!1 &${n.go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: ${ref(n.transform)}${n.components.map(c => `\n  - component: ${ref(c.id)}`).join('')}
  m_Layer: 0
  m_Name: ${n.name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1`);
      lines.push(`--- !u!4 &${n.transform}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: ${ref(n.go)}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: ${vec3(n.x, n.y, n.z, n.precisePosition ? 7 : 5)}
  m_LocalScale: ${vec3(n.sx, n.sy, n.sz, n.preciseScale ? 8 : 5)}
  m_ConstrainProportionsScale: 0
  m_Children: [${n.children.map(x => ref(x.transform)).join(', ')}]
  m_Father: ${ref(n.parent ? n.parent.transform : 0)}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}`);
      for (const c of n.components) {
        lines.push(`--- !u!${c.classId} &${c.id}
${c.type}:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: ${ref(n.go)}
${c.data}`);
      }
    }
    const name = `L${String(this.index).padStart(2, '0')}_DreamGeometry.prefab`;
    const file = path.join(prefabDir, name);
    save(file, lines.join('\n') + '\n');
    meta(file, `a817f8341d864a068aa741000000000${this.index}`, 'PrefabImporter');
  }
}

function build(index) {
  const p = new Prefab(index);
  if (index === 1) {
    p.platform('Start', 0, 4, 0); p.platform('JumpLanding', 5, 9, 0); p.platform('ExitRun', 10, 13, 1);
    p.platform('RiseA', 14, 17, 2); p.platform('GapShelf', 19, 21, 1); p.platform('FinalRun', 23, 27, 2);
  } else if (index === 2) {
    p.platform('Start', 0, 4, 0); p.platform('SpikeLanding', 5, 9, 0); p.platform('ExitRun', 10, 13, 1);
    p.spikes('L02_PitSpikes', 4, 5, -1, 2); p.spikes('L02_FloorSpikes', 6.5, 7.4, 0, 2);
    p.platform('SpikeBridge', 14, 21, 1); p.platform('HighRecovery', 23, 26, 2); p.platform('FinalRun', 28, 32, 2);
    p.spikes('L02_SecondPitSpikes', 13, 14, 0, 2); p.spikes('L02_BridgeSpikes', 18, 19, 1, 2);
  } else if (index === 3) {
    p.platform('Start', 0, 4, 0); p.platform('SawRun', 5, 9, 0); p.platform('ExitRun', 10, 13, 1);
    p.saw(p.hazards, 'L03_Saw', 7, 0);
    p.platform('SecondSawRun', 14, 19, 1); p.platform('UpperLanding', 21, 24, 2); p.platform('FinalRun', 26, 29, 2);
    p.saw(p.hazards, 'L03_SecondSaw', 16.5, 1);
  } else if (index === 4) {
    p.platform('LaserRun', 0, 13, 0); p.platform('LaserCeiling', 7, 8, 4);
    p.button('L04_ButtonLaser', 'L04_Laser', 3, 0, 7); p.laser('L04_Laser', 7.5, 0, 3);
    p.platform('SecondLaserRun', 14, 25, 1); p.platform('SecondLaserCeiling', 20, 22, 5);
    p.button('L04_ButtonSecond', 'L04_SecondLaser', 15.5, 1, 7); p.laser('L04_SecondLaser', 21, 1, 4);
  } else if (index === 5) {
    p.platform('Start', 0, 3, 0); p.timed('L05_TimedA', 5, 2, 0); p.timed('L05_TimedB', 8, 4, 0.3);
    p.timed('L05_TimedC', 16, 2, 0); p.platform('ExitRun', 19, 23, 0); p.spikes('L05_PitSpikes', 3, 19, -1, 32);
    p.timed('L05_TimedD', 25, 2, 0.3); p.timed('L05_TimedE', 28, 4, 0);
    p.platform('FinalRun', 35, 41, 2); p.spikes('L05_SecondPitSpikes', 23, 35, -1, 24);
  } else if (index === 6) {
    p.platform('Start', 0, 3, -1); p.moverWaitCue(); p.moving('L06_Horizontal', 5, 1, [9, 0], 8);
    p.platform('LiftLanding', 16, 18, 0); p.moving('L06_Vertical', 19, 0, [0, 2], 4);
    p.platform('HighExit', 21, 23, 4);
    p.moving('L06_FinalBridge', 25, 3, [10, 0], 6); p.platform('FinalRun', 37, 42, 5);
  } else {
    p.platform('Start', 0, 3, 0); p.platform('WallGrabLedge', 4, 6, 3, 3);
    p.platform('LaserRun', 6, 11, 3); p.platform('DashLanding', 17, 21, 3);
    p.platform('SawRoof', 18, 20, 6, 1);
    p.platform('LaserCeiling', 8, 9, 6);
    p.button('L07_ButtonA', 'L07_Laser', 5, 3, 8); p.laser('L07_Laser', 8.5, 3, 5);
    p.button('L07_ButtonB', 'L07_Saw', 10, 3, 8); p.saw(p.hazards, 'L07_Saw', 19, 3, 2);
    p.spikes('L07_PitSpikes', 11, 17, -1, 12);
    p.platform('SecondLaserRun', 22, 31, 5); p.platform('SecondLaserCeiling', 27, 29, 10);
    p.button('L07_ButtonC', 'L07_SecondLaser', 23.5, 5, 8); p.laser('L07_SecondLaser', 28, 5, 9);
    p.platform('FinalRun', 37, 44, 5); p.spikes('L07_SecondPitSpikes', 31, 37, -1, 12);
  }
  p.caveShell();
  const start = p.marker('PlayerStart', 1, index === 6 ? -0.9 : 0.1);
  const exitX = index === 1 ? 25.5 : index === 2 ? 30.5 : index === 3 ? 27.5 :
    index === 4 ? 23.5 : index === 5 ? 38.5 : index === 6 ? 40 : 42;
  const exitY = index === 1 || index === 2 || index === 3 ? 2 :
    index === 4 ? 1 : index === 5 ? 2 : index === 6 ? 5 : 5;
  p.definition(start, p.bed(exitX, exitY));
  shapePrefabNodes(p);
  p.write();
}

for (let q = 1; q <= 7; q++) build(q);
console.log('Wrote seven editable prefabs.');
