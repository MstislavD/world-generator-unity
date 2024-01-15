using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Noise
{
    public interface IVoronoiDistance
    {
        float GetDistance(float x);
        float GetDistance(float x, float y);
        float GetDistance(float x, float y, float z);
        Vector2 Finalize1D(Vector2 minima);
        Vector2 Finalize2D(Vector2 minima);
        Vector2 Finalize3D(Vector2 minima);
    }

    public struct Worley : IVoronoiDistance
    {
        public Vector2 Finalize1D(Vector2 minima) => minima;

        public Vector2 Finalize2D(Vector2 minima)
        {
            minima.x = Mathf.Sqrt(Mathf.Min(minima.x, 1f));
            minima.y = Mathf.Sqrt(Mathf.Min(minima.y, 1f));
            return minima;
        }

        public Vector2 Finalize3D(Vector2 minima) => Finalize2D(minima);

        public float GetDistance(float x) => Mathf.Abs(x);

        public float GetDistance(float x, float y) => x * x + y * y;

        public float GetDistance(float x, float y, float z) => x * x + y * y + z * z;
    }

    public struct Chebyshev : IVoronoiDistance
    {
        public Vector2 Finalize1D(Vector2 minima) => minima;

        public Vector2 Finalize2D(Vector2 minima) => minima;

        public Vector2 Finalize3D(Vector2 minima) => minima;

        public float GetDistance(float x) => Mathf.Abs(x);

        public float GetDistance(float x, float y) => Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));

        public float GetDistance(float x, float y, float z) => Mathf.Max(Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)), Mathf.Abs(z));
    }
}
