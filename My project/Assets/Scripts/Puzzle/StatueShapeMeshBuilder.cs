using System.Collections.Generic;
using UnityEngine;

namespace ProjectRac.Puzzle
{
    public static class StatueShapeMeshBuilder
    {
        private static readonly Dictionary<GeometricShape3D, Mesh> Shape3DCache = new Dictionary<GeometricShape3D, Mesh>();
        private static readonly Dictionary<ShapeComponent2D, Mesh> Symbol2DCache = new Dictionary<ShapeComponent2D, Mesh>();

        public static Mesh GetShape3DMesh(GeometricShape3D shape)
        {
            if (Shape3DCache.TryGetValue(shape, out Mesh cached) && cached != null) return cached;

            Mesh mesh = shape switch
            {
                GeometricShape3D.Sphere => BuildSphereMesh(0.38f, 16, 12),
                GeometricShape3D.Cube => BuildCubeMesh(0.62f),
                GeometricShape3D.Pyramid => BuildPyramidMesh(0.72f, 0.72f),
                GeometricShape3D.Cylinder => BuildCylinderMesh(0.34f, 0.72f, 20),
                GeometricShape3D.Cone => BuildConeMesh(0.38f, 0.76f, 20),
                GeometricShape3D.Prism => BuildTriangularPrismMesh(0.68f, 0.72f),
                _ => BuildCubeMesh(0.60f)
            };

            Shape3DCache[shape] = mesh;
            return mesh;
        }

        public static Mesh GetSymbol2DMesh(ShapeComponent2D comp)
        {
            if (Symbol2DCache.TryGetValue(comp, out Mesh cached) && cached != null) return cached;

            Mesh mesh = comp switch
            {
                ShapeComponent2D.Circle => BuildDiscMesh(0.11f, 0.045f, 20),
                ShapeComponent2D.Square => BuildFlatBoxMesh(0.19f, 0.19f, 0.045f),
                ShapeComponent2D.Triangle => BuildFlatTriangleMesh(0.22f, 0.20f, 0.045f),
                _ => BuildFlatBoxMesh(0.18f, 0.18f, 0.045f)
            };

            Symbol2DCache[comp] = mesh;
            return mesh;
        }

        private static Mesh BuildSphereMesh(float radius, int lonSegs, int latSegs)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();

            for (int lat = 0; lat <= latSegs; lat++)
            {
                float theta = Mathf.PI * lat / latSegs;
                float sinT = Mathf.Sin(theta);
                float cosT = Mathf.Cos(theta);

                for (int lon = 0; lon <= lonSegs; lon++)
                {
                    float phi = 2f * Mathf.PI * lon / lonSegs;
                    verts.Add(new Vector3(radius * sinT * Mathf.Cos(phi), radius * cosT, radius * sinT * Mathf.Sin(phi)));
                }
            }

            int stride = lonSegs + 1;
            for (int lat = 0; lat < latSegs; lat++)
            {
                for (int lon = 0; lon < lonSegs; lon++)
                {
                    int i0 = lat * stride + lon;
                    int i1 = i0 + stride;
                    tris.Add(i0); tris.Add(i0 + 1); tris.Add(i1);
                    tris.Add(i1); tris.Add(i0 + 1); tris.Add(i1 + 1);
                }
            }

            return FinalizeMesh("Shape_Sphere", verts, tris);
        }

        private static Mesh BuildCubeMesh(float size) => BuildFlatBoxMesh(size, size, size, "Shape_Cube");

        private static Mesh BuildFlatBoxMesh(float w, float h, float d, string name = "Symbol_Square")
        {
            float hx = w * 0.5f, hy = h * 0.5f, hz = d * 0.5f;
            var verts = new List<Vector3>();
            var tris = new List<int>();

            AddQuad(verts, tris, new Vector3(-hx, -hy, hz), new Vector3(hx, -hy, hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz));
            AddQuad(verts, tris, new Vector3(hx, -hy, -hz), new Vector3(-hx, -hy, -hz), new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz));
            AddQuad(verts, tris, new Vector3(-hx, hy, hz), new Vector3(hx, hy, hz), new Vector3(hx, hy, -hz), new Vector3(-hx, hy, -hz));
            AddQuad(verts, tris, new Vector3(-hx, -hy, -hz), new Vector3(hx, -hy, -hz), new Vector3(hx, -hy, hz), new Vector3(-hx, -hy, hz));
            AddQuad(verts, tris, new Vector3(hx, -hy, hz), new Vector3(hx, -hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz));
            AddQuad(verts, tris, new Vector3(-hx, -hy, -hz), new Vector3(-hx, -hy, hz), new Vector3(-hx, hy, hz), new Vector3(-hx, hy, -hz));

            return FinalizeMesh(name, verts, tris);
        }

        private static Mesh BuildPyramidMesh(float baseSize, float height)
        {
            float hb = baseSize * 0.5f, hh = height * 0.5f;
            Vector3 apex = new Vector3(0f, hh, 0f);
            Vector3 fl = new Vector3(-hb, -hh, hb), fr = new Vector3(hb, -hh, hb);
            Vector3 br = new Vector3(hb, -hh, -hb), bl = new Vector3(-hb, -hh, -hb);

            var verts = new List<Vector3>();
            var tris = new List<int>();
            AddTri(verts, tris, fl, fr, apex);
            AddTri(verts, tris, fr, br, apex);
            AddTri(verts, tris, br, bl, apex);
            AddTri(verts, tris, bl, fl, apex);
            AddQuad(verts, tris, bl, br, fr, fl);

            return FinalizeMesh("Shape_Pyramid", verts, tris);
        }

        private static Mesh BuildCylinderMesh(float radius, float height, int segs)
        {
            float hh = height * 0.5f;
            var verts = new List<Vector3>();
            var tris = new List<int>();

            Vector3 topCenter = new Vector3(0f, hh, 0f);
            Vector3 botCenter = new Vector3(0f, -hh, 0f);

            for (int i = 0; i < segs; i++)
            {
                float a0 = 2f * Mathf.PI * i / segs;
                float a1 = 2f * Mathf.PI * (i + 1) / segs;
                Vector3 t0 = new Vector3(Mathf.Cos(a0) * radius, hh, Mathf.Sin(a0) * radius);
                Vector3 t1 = new Vector3(Mathf.Cos(a1) * radius, hh, Mathf.Sin(a1) * radius);
                Vector3 b0 = new Vector3(t0.x, -hh, t0.z);
                Vector3 b1 = new Vector3(t1.x, -hh, t1.z);

                AddQuad(verts, tris, b0, b1, t1, t0);
                AddTri(verts, tris, topCenter, t1, t0);
                AddTri(verts, tris, botCenter, b0, b1);
            }

            return FinalizeMesh("Shape_Cylinder", verts, tris);
        }

        private static Mesh BuildConeMesh(float radius, float height, int segs)
        {
            float hh = height * 0.5f;
            var verts = new List<Vector3>();
            var tris = new List<int>();

            Vector3 apex = new Vector3(0f, hh, 0f);
            Vector3 botCenter = new Vector3(0f, -hh, 0f);

            for (int i = 0; i < segs; i++)
            {
                float a0 = 2f * Mathf.PI * i / segs;
                float a1 = 2f * Mathf.PI * (i + 1) / segs;
                Vector3 b0 = new Vector3(Mathf.Cos(a0) * radius, -hh, Mathf.Sin(a0) * radius);
                Vector3 b1 = new Vector3(Mathf.Cos(a1) * radius, -hh, Mathf.Sin(a1) * radius);

                AddTri(verts, tris, b0, b1, apex);
                AddTri(verts, tris, botCenter, b0, b1);
            }

            return FinalizeMesh("Shape_Cone", verts, tris);
        }

        private static Mesh BuildTriangularPrismMesh(float width, float height)
        {
            float r = width * 0.58f;
            float hh = height * 0.5f;
            Vector3[] top = new Vector3[3];
            Vector3[] bot = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                float angle = Mathf.Deg2Rad * (90f + i * 120f);
                float x = Mathf.Cos(angle) * r;
                float z = Mathf.Sin(angle) * r;
                top[i] = new Vector3(x, hh, z);
                bot[i] = new Vector3(x, -hh, z);
            }

            var verts = new List<Vector3>();
            var tris = new List<int>();
            AddTri(verts, tris, top[0], top[2], top[1]);
            AddTri(verts, tris, bot[0], bot[1], bot[2]);
            for (int i = 0; i < 3; i++)
            {
                int next = (i + 1) % 3;
                AddQuad(verts, tris, bot[i], bot[next], top[next], top[i]);
            }

            return FinalizeMesh("Shape_Prism", verts, tris);
        }

        private static Mesh BuildDiscMesh(float radius, float depth, int segs)
        {
            float hz = depth * 0.5f;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            Vector3 fc = new Vector3(0f, 0f, hz);
            Vector3 bc = new Vector3(0f, 0f, -hz);

            for (int i = 0; i < segs; i++)
            {
                float a0 = 2f * Mathf.PI * i / segs;
                float a1 = 2f * Mathf.PI * (i + 1) / segs;
                Vector3 f0 = new Vector3(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius, hz);
                Vector3 f1 = new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, hz);
                Vector3 b0 = new Vector3(f0.x, f0.y, -hz);
                Vector3 b1 = new Vector3(f1.x, f1.y, -hz);

                AddTri(verts, tris, fc, f0, f1);
                AddTri(verts, tris, bc, b1, b0);
                AddQuad(verts, tris, b0, b1, f1, f0);
            }

            return FinalizeMesh("Symbol_Circle", verts, tris);
        }

        private static Mesh BuildFlatTriangleMesh(float width, float height, float depth)
        {
            float hw = width * 0.5f, hh = height * 0.5f, hz = depth * 0.5f;
            Vector3 ft = new Vector3(0f, hh, hz), fl = new Vector3(-hw, -hh, hz), fr = new Vector3(hw, -hh, hz);
            Vector3 bt = new Vector3(0f, hh, -hz), bl = new Vector3(-hw, -hh, -hz), br = new Vector3(hw, -hh, -hz);

            var verts = new List<Vector3>();
            var tris = new List<int>();
            AddTri(verts, tris, fl, fr, ft);
            AddTri(verts, tris, br, bl, bt);
            AddQuad(verts, tris, bl, br, fr, fl);
            AddQuad(verts, tris, br, bt, ft, fr);
            AddQuad(verts, tris, bt, bl, fl, ft);

            return FinalizeMesh("Symbol_Triangle", verts, tris);
        }

        private static void AddTri(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c)
        {
            int idx = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
        }

        private static void AddQuad(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int idx = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
            tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 3);
        }

        private static Mesh FinalizeMesh(string name, List<Vector3> verts, List<int> tris)
        {
            Mesh mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
