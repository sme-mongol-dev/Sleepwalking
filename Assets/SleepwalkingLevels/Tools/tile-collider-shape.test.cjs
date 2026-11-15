const assert = require('node:assert/strict');
const { ownCells, outlines } = require('./tile-collider-shape.cjs');

const cell = { x: 0, y: 0, width: 1, height: 1 };
const directions = [[0, 1], [1, 0], [0, -1], [-1, 0]];
for (let mask = 0; mask < 16; mask++) {
  const neighbors = ownCells(cell);
  directions.forEach(([x, y], bit) => {
    if (mask & (1 << bit)) neighbors.add(`${x},${y}`);
  });
  const paths = outlines(cell, neighbors);
  assert.equal(paths.length, 1, `neighbor mask ${mask}`);
  assert.ok(paths[0].length >= 4, `neighbor mask ${mask}`);
}

for (let missing = 0; missing < 4; missing++) {
  const neighbors = new Set(['0,0', '0,1', '1,0', '0,-1', '-1,0']);
  [[1, 1], [1, -1], [-1, -1], [-1, 1]].forEach(([x, y], index) => {
    if (index !== missing) neighbors.add(`${x},${y}`);
  });
  assert.equal(outlines(cell, neighbors).length, 1, `inner-corner rotation ${missing}`);
}

const bridge = { x: 0, y: 0, width: 2, height: 1 };
for (const cut of [0, 1, 2]) {
  const paths = outlines(bridge, ownCells(bridge), cut);
  assert.equal(paths.length, 1, 'adjacent cells must have no physical seam');
  const top = paths[0].filter(point => point.y === 16).map(point => point.x);
  assert.equal(Math.min(...top), cut, 'top remains flat after the first corner');
  assert.equal(Math.max(...top), 32 - cut, 'top remains flat up to the last corner');
}

const bedrock = { x: -8, y: -20, width: 60, height: 16 };
assert.equal(outlines(bedrock, ownCells(bedrock)).length, 1,
  'large shell region must keep one continuous collider');
console.log('Tile collider contours: 16 neighbor masks, 4 inner corners, shared seam and shell passed.');
