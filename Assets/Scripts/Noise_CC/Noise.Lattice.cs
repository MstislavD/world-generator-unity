using UnityEngine;

public static partial class Noise
{
    public interface ILattice
    {
        public LatticeSpan GetLatticeSpan(float coordinate, int frequency);

        public int ValidateSingleStep(int point, int frequency);
    }

    public struct LatticeNormal : ILattice
    {
        public LatticeSpan GetLatticeSpan(float coordinate, int frequency)
        {
            coordinate *= frequency;
            float point = Mathf.Floor(coordinate);
            LatticeSpan span;
            span.p0 = (int)point;
            span.p1 = span.p0 + 1;
            span.t = coordinate - point;
            span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);
            span.g0 = coordinate - span.p0;
            span.g1 = span.g0 - 1f;

            return span;
        }

        public int ValidateSingleStep(int point, int frequency) => point;
    }

    public struct LatticeTiling : ILattice
    {
        public LatticeSpan GetLatticeSpan(float coordinate, int frequency)
        {
            coordinate *= frequency;
            float point = Mathf.Floor(coordinate);
            LatticeSpan span;
            span.p0 = (int)point;
            span.g0 = coordinate - span.p0;
            span.g1 = span.g0 - 1f;

            span.p0 -= (int)Mathf.Ceil(point / frequency) * frequency;
            span.p0 = span.p0 < 0 ? span.p0 + frequency : span.p0;
            span.p1 = span.p0 + 1;
            span.p1 = span.p1 == frequency ? 0 : span.p1;

            span.t = coordinate - point;
            span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);

            return span;
        }

        public int ValidateSingleStep(int point, int frequency)
        {
            return point == -1 ? frequency - 1 : (point == frequency ? 0 : point);
        }
    }

    public struct Lattice1D<L, G> : INoise
        where G : struct, IGradient
        where L : struct, ILattice
    {
        public float GetNoise(Vector3 positions, SmallXXHash hash, int frequency)
        {
            LatticeSpan x = default(L).GetLatticeSpan(positions.x, frequency);
            var g = default(G);
            return g.EvaluateCombined(Mathf.Lerp(g.Evaluate(hash.Eat(x.p0), x.g0), g.Evaluate(hash.Eat(x.p1), x.g1), x.t));
        }
    }

    public struct Lattice2D<L, G> : INoise
       where G : struct, IGradient
       where L : struct, ILattice
    {
        public float GetNoise(Vector3 positions, SmallXXHash hash, int frequency)
        {
            var l = default(L);

            LatticeSpan x = l.GetLatticeSpan(positions.x, frequency);
            LatticeSpan z = l.GetLatticeSpan(positions.z, frequency);

            SmallXXHash h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1);

            var g = default(G);

            return g.EvaluateCombined(Mathf.Lerp(
                Mathf.Lerp(g.Evaluate(h0.Eat(z.p0), x.g0, z.g0), g.Evaluate(h0.Eat(z.p1), x.g0, z.g1), z.t),
                Mathf.Lerp(g.Evaluate(h1.Eat(z.p0), x.g1, z.g0), g.Evaluate(h1.Eat(z.p1), x.g1, z.g1), z.t),
                x.t));
        }
    }

    public struct Lattice3D<L, G> : INoise
        where G : struct, IGradient
        where L : struct, ILattice
    {
        public float GetNoise(Vector3 positions, SmallXXHash hash, int frequency)
        {
            var l = default(L);

            LatticeSpan
                x =  l.GetLatticeSpan(positions.x, frequency),
                y =  l.GetLatticeSpan(positions.y, frequency),
                z =  l.GetLatticeSpan(positions.z, frequency);

            SmallXXHash
                h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1),
                h00 = h0.Eat(y.p0), h01 = h0.Eat(y.p1),
                h10 = h1.Eat(y.p0), h11 = h1.Eat(y.p1);

            var g = default(G);

            return g.EvaluateCombined(Mathf.Lerp(
                Mathf.Lerp(
                    Mathf.Lerp(g.Evaluate(h00.Eat(z.p0), x.g0, y.g0, z.g0), g.Evaluate(h00.Eat(z.p1), x.g0, y.g0, z.g1), z.t),
                    Mathf.Lerp(g.Evaluate(h01.Eat(z.p0), x.g0, y.g1, z.g0), g.Evaluate(h01.Eat(z.p1), x.g0, y.g1, z.g1), z.t), y.t),
                Mathf.Lerp(
                    Mathf.Lerp(g.Evaluate(h10.Eat(z.p0), x.g1, y.g0, z.g0), g.Evaluate(h10.Eat(z.p1), x.g1, y.g0, z.g1), z.t),
                    Mathf.Lerp(g.Evaluate(h11.Eat(z.p0), x.g1, y.g1, z.g0), g.Evaluate(h11.Eat(z.p1), x.g1, y.g1, z.g1), z.t), y.t),
                x.t));
        }
    }

    public struct LatticeSpan
    {
        public int p0, p1;
        public float g0, g1;
        public float t;
    }
}
