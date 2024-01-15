using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Noise
{
    static Vector2 UpdateVoronoiMinima(Vector2 minima, float distances)
    {
        bool newMinimum = distances < minima.x;
        minima.y = newMinimum ? minima.x : (distances < minima.y ? distances : minima.y);
        minima.x = newMinimum ? distances : minima.x;
        return minima;
    }

    public struct Voronoi1D<L, D, F> : INoise
        where L : struct, ILattice
        where D : struct, IVoronoiDistance
        where F : struct, IVoronoiFunction
    {
        public float GetNoise(Vector3 positions, SmallXXHash hash, int frequency)
        {
            var l = default(L);
            var d = default(D);
            LatticeSpan x = l.GetLatticeSpan(positions.x, frequency);

            Vector2 minima = new Vector2(2f, 2f);
            for (int u = -1; u <= 1; u++)
            {
                SmallXXHash h = hash.Eat(l.ValidateSingleStep(x.p0 + u, frequency));
                minima = UpdateVoronoiMinima(minima, d.GetDistance(h.Float01A + u - x.g0));
            }

            return default(F).Evaluate(d.Finalize1D(minima));
        }
    }

    public struct Voronoi2D<L, D, F> : INoise
        where L : struct, ILattice
        where D : struct, IVoronoiDistance
        where F : struct, IVoronoiFunction
    {
        public float GetNoise(Vector3 positions, SmallXXHash hash, int frequency)
        {
            var l = default(L);
            var d = default(D);
            LatticeSpan
                x = l.GetLatticeSpan(positions.x, frequency),
                z = l.GetLatticeSpan(positions.z, frequency);

            Vector2 minima = new Vector2(2f, 2f);
            for (int u = -1; u <= 1; u++)
            {
                float xOffset = u - x.g0;
                SmallXXHash hx = hash.Eat(l.ValidateSingleStep(x.p0 + u, frequency));
                for (int v = -1; v <= 1; v++)
                {
                    float zOffset = v - z.g0;
                    SmallXXHash h = hx.Eat(l.ValidateSingleStep(z.p0 + v, frequency));
                    minima = UpdateVoronoiMinima(minima, d.GetDistance(h.Float01A + xOffset, h.Float01B + zOffset));
                    minima = UpdateVoronoiMinima(minima, d.GetDistance(h.Float01C + xOffset, h.Float01D + zOffset));
                }
            }

            return default(F).Evaluate(d.Finalize2D(minima));
        }
    }

    public struct Voronoi3D<L, D, F> : INoise
        where L : struct, ILattice
        where D : struct, IVoronoiDistance
        where F : struct, IVoronoiFunction
    {
        public float GetNoise(Vector3 positions, SmallXXHash hash, int frequency)
        {
            var l = default(L);
            var d = default(D);
            LatticeSpan
               x = l.GetLatticeSpan(positions.x, frequency),
               y = l.GetLatticeSpan(positions.y, frequency),
               z = l.GetLatticeSpan(positions.z, frequency);

            Vector2 minima = new Vector2(2f, 2f);
            for (int u = -1; u <= 1; u++)
            {
                float xOffset = u - x.g0;
                SmallXXHash hx = hash.Eat(l.ValidateSingleStep(x.p0 + u, frequency));
                for (int v = -1; v <= 1; v++)
                {
                    float yOffset = v - y.g0;
                    SmallXXHash hy = hx.Eat(l.ValidateSingleStep(y.p0 + v, frequency));
                    for (int w = -1; w <= 1; w++)
                    {
                        float zOffset = w - z.g0;
                        SmallXXHash h = hy.Eat(l.ValidateSingleStep(z.p0 + w, frequency));
                        minima = UpdateVoronoiMinima(minima, d.GetDistance(
                            h.GetBitsAsFloat01(5, 0) + xOffset,
                            h.GetBitsAsFloat01(5, 5) + yOffset,
                            h.GetBitsAsFloat01(5, 10) + zOffset));
                        minima = UpdateVoronoiMinima(minima, d.GetDistance(
                            h.GetBitsAsFloat01(5, 15) + xOffset,
                            h.GetBitsAsFloat01(5, 20) + yOffset,
                            h.GetBitsAsFloat01(5, 25) + zOffset));
                    }
                }
            }

            return default(F).Evaluate(d.Finalize3D(minima));
        }
    }
}
