using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class Noise
{
    public interface IGradient
    {
        float Evaluate(SmallXXHash hash, float x);

        float Evaluate(SmallXXHash hash, float x, float y);

        float Evaluate(SmallXXHash hash, float x, float y, float z);

        float EvaluateCombined(float value);
    }

    public struct Turbulence<G> : IGradient
        where G : struct, IGradient
    {
        public float Evaluate(SmallXXHash hash, float x) => default(G).Evaluate(hash, x);

        public float Evaluate(SmallXXHash hash, float x, float y) => default(G).Evaluate(hash, x, y);

        public float Evaluate(SmallXXHash hash, float x, float y, float z) => default(G).Evaluate(hash, x, y, z);

        public float EvaluateCombined(float value)
        {
            return Mathf.Abs(default(G).EvaluateCombined(value));
        }
    }

    public struct Value : IGradient
    {
        public float Evaluate(SmallXXHash hash, float x)
        {
            return hash.Float01A * 2f - 1f;
        }

        public float Evaluate(SmallXXHash hash, float x, float y)
        {
            return hash.Float01A * 2f - 1f;
        }

        public float Evaluate(SmallXXHash hash, float x, float y, float z)
        {
            return hash.Float01A * 2f - 1f;
        }

        public float EvaluateCombined(float value)
        {
            return value;
        }
    }

    public struct Perlin : IGradient
    {
        public float Evaluate(SmallXXHash hash, float x) => BaseGradients.Line(hash, x);

        public float Evaluate(SmallXXHash hash, float x, float y) => BaseGradients.Square(hash, x, y) * (2f / 0.53528f);

        public float Evaluate(SmallXXHash hash, float x, float y, float z) => BaseGradients.Octahedron(hash, x, y, z) * (1f / 0.56290f);

        public float EvaluateCombined(float value)
        {
            return value;
        }
    }

    public struct Simplex: IGradient
    {
        public float Evaluate(SmallXXHash hash, float x) => BaseGradients.Line(hash, x) * (32f / 27f);

        public float Evaluate(SmallXXHash hash, float x, float y) => BaseGradients.Circle(hash, x, y) * (5.832f / Mathf.Sqrt(2f));

        public float Evaluate(SmallXXHash hash, float x, float y, float z) =>
            BaseGradients.Sphere(hash, x, y, z) * (1024f / (125f * Mathf.Sqrt(3f)));

        public float EvaluateCombined(float value) => value;
    }

    public static class BaseGradients
    {
        public static float Line(SmallXXHash hash, float x) => (1 + hash.Float01A) * (hash & 1 << 8) == 0 ? x : -x;

        public static Vector2 SquareVector(SmallXXHash hash)
        {
            Vector2 v;
            v.x = hash.Float01A * 2f - 1f;
            v.y = 0.5f - Mathf.Abs(v.x);
            v.x -= Mathf.Floor(v.x + 0.5f);
            return v;
        }

        public static Vector3 OctahedronVector(SmallXXHash hash)
        {
            Vector3 g;
            g.x = hash.Float01A * 2f - 1f;
            g.y = hash.Float01D * 2f - 1f;
            g.z = 1f - Mathf.Abs(g.x) - Mathf.Abs(g.y);
            float offset = Mathf.Max(-g.z, 0f);
            g.x += g.x < 0f ? offset : -offset;
            g.y += g.y < 0f ? offset : -offset;
            return g;
        }

        public static float Square(SmallXXHash hash, float x, float y)
        {
            Vector2 v = SquareVector(hash);
            return v.x * x + v.y * y;
        }

        public static float Circle(SmallXXHash hash, float x, float y)
        {
            Vector2 v = SquareVector(hash);
            return (v.x * x + v.y * y) / Mathf.Sqrt(v.x * v.x + v.y * v.y);
        }

        public static float Octahedron(SmallXXHash hash, float x, float y, float z)
        {
            Vector3 v = OctahedronVector(hash);
            return v.x * x + v.y * y + v.z * z;
        }

        public static float Sphere(SmallXXHash hash, float x, float y, float z)
        {
            Vector3 v = OctahedronVector(hash);
            return (v.x * x + v.y * y + v.z * z) / Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        }
    }
}
