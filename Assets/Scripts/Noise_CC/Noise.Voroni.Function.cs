using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Noise
{
    public interface IVoronoiFunction
    {
        float Evaluate(Vector2 distances);
    }

    public struct F1 : IVoronoiFunction
    {
        public float Evaluate(Vector2 distances) => distances.x;
    }

    public struct F2: IVoronoiFunction
    {
        public float Evaluate(Vector2 distances) => distances.y;
    }

    public struct F2MinusF1 : IVoronoiFunction
    {
        public float Evaluate(Vector2 distances) => distances.y - distances.x;
    }
}
