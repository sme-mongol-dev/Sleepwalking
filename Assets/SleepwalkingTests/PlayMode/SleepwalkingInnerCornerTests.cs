using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class SleepwalkingInnerCornerTests
    {
        [Test]
        public void Tile17PaintedCornerFacesTheOnlyMissingDiagonal()
        {
            Vector2Int[] missing =
            {
                new Vector2Int(1, -1), new Vector2Int(-1, -1),
                new Vector2Int(-1, 1), new Vector2Int(1, 1)
            };
            for (int q = 0; q < missing.Length; q++)
                CheckUv(CenterUv(Matrix(missing[q])), 17, q);
        }

        [Test]
        public void FullAndAmbiguousDiagonalSurroundingsKeepTile5()
        {
            CheckUv(CenterUv(Matrix()), 5, 0);
            CheckUv(CenterUv(Matrix(new Vector2Int(-1, 1), new Vector2Int(1, -1))), 5, 0);
        }

        [Test]
        public void MissingOrthogonalNeighborKeepsTheExistingTileChoice()
        {
            CheckUv(CenterUv(Matrix(new Vector2Int(0, 1))), 2, 0);
        }

        static HashSet<Vector2Int> Matrix(params Vector2Int[] missing)
        {
            HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                cells.Add(new Vector2Int(x, y));
            foreach (Vector2Int cell in missing) cells.Remove(cell);
            return cells;
        }

        static Vector2[] CenterUv(HashSet<Vector2Int> cells)
        {
            GameObject root = new GameObject("Synthetic 3x3 tile matrix");
            Mesh mesh = null;
            try
            {
                System.Type theme = System.Type.GetType("Sleepwalking.Visual.DreamTheme, Assembly-CSharp");
                Assert.That(theme, Is.Not.Null);
                MethodInfo method = theme.GetMethod("GridMesh",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(method, Is.Not.Null);
                mesh = (Mesh)method.Invoke(null, new object[] { root.transform, cells });
                Vector3[] vertices = mesh.vertices;
                Vector2[] uv = mesh.uv;
                for (int q = 0; q < vertices.Length; q += 4)
                {
                    if (Mathf.Abs(vertices[q].x) > .001f || Mathf.Abs(vertices[q].y) > .001f) continue;
                    return new[] { uv[q], uv[q + 1], uv[q + 2], uv[q + 3] };
                }
                Assert.Fail("The synthetic matrix has no centre tile.");
                return null;
            }
            finally
            {
                if (mesh != null) Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(root);
            }
        }

        static void CheckUv(Vector2[] actual, int tileNumber, int clockwiseTurns)
        {
            int tile = tileNumber - 1;
            int column = tile % 3;
            int row = tile / 3;
            float u0 = (column * 32 + .5f) / 96f;
            float u1 = ((column + 1) * 32 - .5f) / 96f;
            float v0 = ((5 - row) * 32 + .5f) / 192f;
            float v1 = ((6 - row) * 32 - .5f) / 192f;
            Vector2[] source =
            {
                new Vector2(u0, v0), new Vector2(u1, v0),
                new Vector2(u1, v1), new Vector2(u0, v1)
            };
            for (int q = 0; q < 4; q++)
                Assert.That(Vector2.Distance(actual[q], source[(q + clockwiseTurns) & 3]),
                    Is.LessThan(.000001f), "UV corner " + q + " of tile " + tileNumber);
        }
    }
}
