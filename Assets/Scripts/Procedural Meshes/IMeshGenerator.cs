using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralMeshes
{
    public interface IMeshGenerator
    {
        int VertexCount { get; }
        int IndexCount { get; }
        int JobLength { get; }
        Bounds Bounds { get; }

        int Resolution { get; set; }
        void Execute<S>(int index, S stream) where S : struct, IMeshStreams;
    }
}