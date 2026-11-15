const step = 2;
const samples = 32 / step;

function ownCells(rect) {
  const cells = new Set();
  for (let y = rect.y; y < rect.y + rect.height; y++)
    for (let x = rect.x; x < rect.x + rect.width; x++)
      cells.add(`${x},${y}`);
  return cells;
}

function outlines(rect, neighbors, cornerCut = 2) {
  const width = rect.width * samples;
  const height = rect.height * samples;
  const filled = new Uint8Array(width * height);
  const has = (x, y) => neighbors.has(`${x},${y}`);
  for (let cy = rect.y; cy < rect.y + rect.height; cy++)
    for (let cx = rect.x; cx < rect.x + rect.width; cx++) {
      const up = has(cx, cy + 1);
      const right = has(cx + 1, cy);
      const down = has(cx, cy - 1);
      const left = has(cx - 1, cy);
      const ne = has(cx + 1, cy + 1);
      const se = has(cx + 1, cy - 1);
      const sw = has(cx - 1, cy - 1);
      const nw = has(cx - 1, cy + 1);
      const diagonals = Number(ne) + Number(se) + Number(sw) + Number(nw);
      for (let y = 0; y < samples; y++)
        for (let x = 0; x < samples; x++) {
          let trim = false;
          if (!up && !left && x + samples - 1 - y < cornerCut) trim = true;
          if (!up && !right && samples - 1 - x + samples - 1 - y < cornerCut) trim = true;
          if (!down && !left && x + y < cornerCut) trim = true;
          if (!down && !right && samples - 1 - x + y < cornerCut) trim = true;
          if (up && right && down && left && diagonals === 3) {
            if (!ne && samples - 1 - x + samples - 1 - y < 1) trim = true;
            if (!se && samples - 1 - x + y < 1) trim = true;
            if (!sw && x + y < 1) trim = true;
            if (!nw && x + samples - 1 - y < 1) trim = true;
          }
          const px = (cx - rect.x) * samples + x;
          const py = (cy - rect.y) * samples + y;
          filled[py * width + px] = Number(!trim);
        }
    }

  const next = new Map();
  const stride = width + 1;
  const add = (a, b) => {
    if (next.has(a)) throw new Error('Ambiguous tile collider contour');
    next.set(a, b);
  };
  for (let y = 0; y < height; y++)
    for (let x = 0; x < width; x++) {
      if (!filled[y * width + x]) continue;
      if (y === 0 || !filled[(y - 1) * width + x]) add(y * stride + x, y * stride + x + 1);
      if (x === width - 1 || !filled[y * width + x + 1]) add(y * stride + x + 1, (y + 1) * stride + x + 1);
      if (y === height - 1 || !filled[(y + 1) * width + x]) add((y + 1) * stride + x + 1, (y + 1) * stride + x);
      if (x === 0 || !filled[y * width + x - 1]) add((y + 1) * stride + x, y * stride + x);
    }

  const result = [];
  while (next.size) {
    const first = next.keys().next().value;
    let at = first;
    const points = [];
    do {
      points.push({ x: at % stride, y: Math.floor(at / stride) });
      if (!next.has(at)) throw new Error('Open tile collider contour');
      const end = next.get(at);
      next.delete(at);
      at = end;
    } while (at !== first);
    for (let q = points.length - 1; q >= 0; q--) {
      const a = points[(q + points.length - 1) % points.length];
      const b = points[q];
      const c = points[(q + 1) % points.length];
      if ((b.x - a.x) * (c.y - b.y) === (b.y - a.y) * (c.x - b.x))
        points.splice(q, 1);
    }
    if (points.length < 3) throw new Error('Degenerate tile collider contour');
    result.push(points);
  }
  return result;
}

function shapePrefabNodes(prefab) {
  const isChildOf = (node, ancestor) => {
    for (let p = node.parent; p; p = p.parent)
      if (p === ancestor) return true;
    return false;
  };
  const gridRect = node => {
    let x = node.x;
    let y = node.y;
    let sx = node.sx;
    let sy = node.sy;
    for (let p = node.parent; p && p !== prefab.root; p = p.parent) {
      x = p.x + x * p.sx;
      y = p.y + y * p.sy;
      sx *= p.sx;
      sy *= p.sy;
    }
    const left = x - sx / 2;
    const right = x + sx / 2;
    const bottom = y - sy / 2;
    const top = y + sy / 2;
    const edges = [left, right, bottom, top];
    if (edges.some(value => Math.abs(value - Math.round(value)) > .001))
      throw new Error(`Tile collider is off the 32-unit grid: ${node.name}`);
    return { rect: { x: Math.round(left), y: Math.round(bottom),
      width: Math.round(right - left), height: Math.round(top - bottom) }, x, y, sx, sy };
  };
  const surfaces = prefab.nodes.filter(n => n.components.some(c =>
    c.type === 'MonoBehaviour' && c.data.includes('Sleepwalking.Levels.SleepwalkingTileSurface')))
    .map(node => ({ node, box: node.components.find(c => c.type === 'BoxCollider2D'),
      ...gridRect(node), static: isChildOf(node, prefab.geometry) }));
  const staticCells = new Set();
  for (const surface of surfaces)
    if (surface.static)
      for (const cell of ownCells(surface.rect)) staticCells.add(cell);

  const number = value => String(+value.toFixed(8));
  for (const surface of surfaces) {
    if (!surface.box) continue;
    const paths = outlines(surface.rect,
      surface.static ? staticCells : ownCells(surface.rect), surface.static ? 2 : 0);
    const points = paths.map(path =>
      `    - - ${path.map((point, q) => {
        const x = (surface.rect.x + point.x / samples - surface.x) / surface.sx;
        const y = (surface.rect.y + point.y / samples - surface.y) / surface.sy;
        return `${q ? '      - ' : ''}{x: ${number(x)}, y: ${number(y)}}`;
      }).join('\n')}`);
    surface.box.classId = 60;
    surface.box.type = 'PolygonCollider2D';
    surface.box.data = surface.box.data.replace(/^  m_Size: .+$/m,
      `  m_Points:\n    m_Paths:\n${points.join('\n')}`);
  }
}

module.exports = { step, ownCells, outlines, shapePrefabNodes };
