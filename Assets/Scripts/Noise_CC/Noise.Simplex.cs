using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static partial class Noise
{
    public struct Simplex1D<G> : INoise
        where G : struct, IGradient
    {
        static float Kernel(SmallXXHash hash, float lx, Vector3 positions)
        {
            float x = positions.x - lx;
            float f = 1 - x * x;
            f = f * f * f;
            return f * default(G).Evaluate(hash, x);
        }

        public float GetNoise(Vector3 positions, SmallXXHash hash, int frequency)
        {
            positions *= frequency;
            int x0 = (int)Mathf.Floor(positions.x);
            int x1 = x0 + 1;
            return default(G).EvaluateCombined(Kernel(hash.Eat(x0), x0, positions) + Kernel(hash.Eat(x1), x1, positions));
        }
    }

    public struct Simplex2D<G> : INoise
         where G : struct, IGradient
    {
        static float Kernel(SmallXXHash hash, float lx, float lz, Vector3 positions)
        {
            float unskew = (lx + lz) * ((3f - Mathf.Sqrt(3f)) / 6f);
            float x = positions.x - lx + unskew;
            float z = positions.z - lz + unskew;
            float f = 0.5f - x * x - z * z;
            f = f * f * f * 8f;
            return Mathf.Max(0f, f) * default(G).Evaluate(hash, x, z);
        }

        public float GetNoise(Vector3 positions, SmallXXHash hash, int frequency)
        {
            positions *= frequency * (1f / Mathf.Sqrt(3f));
            float skew = (positions.x + positions.z) * ((Mathf.Sqrt(3f) - 1f) / 2f);
            float sx = positions.x + skew, sz = positions.z + skew;
            int x0 = (int)Mathf.Floor(sx), x1 = x0 + 1;
            int z0 = (int)Mathf.Floor(sz), z1 = z0 + 1;

            bool xGz = sx - x0 > sz - z0;
            int xC = xGz ? x1 : x0, zC = xGz ? z0 : z1;

            SmallXXHash h0 = hash.Eat(x0), h1 = hash.Eat(x1), hc = SmallXXHash.Select(h0, h1, xGz);

            return default(G).EvaluateCombined(
                Kernel(h0.Eat(z0), x0, z0, positions) +
                Kernel(h1.Eat(z1), x1, z1, positions) +
                Kernel(hc.Eat(zC), xC, zC, positions));
        }
    }

    public struct Simplex3D<G> : INoise
         where G : struct, IGradient
    {
        static float Kernel(SmallXXHash hash, float lx, float ly, float lz, Vector3 positions)
        {
            float unskew = (lx + ly + lz) * (1f / 6f);
            float x = positions.x - lx + unskew;
            float y = positions.y - ly + unskew;
            float z = positions.z - lz + unskew;
            float f = 0.5f - x * x - y * y - z * z;
            f = f * f * f * 8f;
            return Mathf.Max(0f, f) * default(G).Evaluate(hash, x, y, z);
        }

        public float GetNoise(Vector3 positions, SmallXXHash hash, int frequency)
        {
            positions *= frequency * 0.6f;
            float skew = (positions.x + positions.y + positions.z) * (1f / 3f);
            float sx = positions.x + skew,
                sy = positions.y + skew,
                sz = positions.z + skew;
            int x0 = (int)Mathf.Floor(sx), x1 = x0 + 1;
            int y0 = (int)Mathf.Floor(sy), y1 = y0 + 1;
            int z0 = (int)Mathf.Floor(sz), z1 = z0 + 1;

            bool 
                xGy = sx - x0 > sy - y0,
                xGz = sx - x0 > sz - z0,
                yGz = sy - y0 > sz - z0;

            bool
                xA = xGy & xGz,
                xB = xGy | (xGz & yGz),
                yA = !xGy & yGz,
                yB = !xGy | (xGz & yGz),
                zA = (xGy & !xGz) | (!xGy & !yGz),
                zB = !(xGz & yGz);

            int
                xCA = xA ? x1 : x0,
                xCB = xB ? x1 : x0,
                yCA = yA ? y1 : y0,
                yCB = yB ? y1 : y0,
                zCA = zA ? z1 : z0,
                zCB = zB ? z1 : z0;

            SmallXXHash h0 = hash.Eat(x0), h1 = hash.Eat(x1),
                hA = SmallXXHash.Select(h0, h1, xA),
                hB = SmallXXHash.Select(h0, h1, xB);

            return default(G).EvaluateCombined(
                Kernel(h0.Eat(y0).Eat(z0), x0, y0, z0, positions) +
                Kernel(h1.Eat(y1).Eat(z1), x1, y1, z1, positions) + 
                Kernel(hA.Eat(yCA).Eat(zCA), xCA, yCA, zCA, positions) + 
                Kernel(hB.Eat(yCB).Eat(zCB), xCB, yCB, zCB, positions));
        }
    }
}
