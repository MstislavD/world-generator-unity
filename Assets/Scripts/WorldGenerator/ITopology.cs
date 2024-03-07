using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface ITopology
{
    int PolygonCount { get; }

    int EdgeCount { get; }

    IEnumerable<int> GetPolygons() => Enumerable.Range(0, PolygonCount);

    IEnumerable<int> GetEdges() => Enumerable.Range(0, EdgeCount);

    Vector3 GetCenter(int polygon_index);

    int GetEdgePolygon1(int edge_index);

    int GetEdgePolygon2(int edge_index);

    int GetEdge(int polygon_index1, int polygon_index2);

    int GetParent(int polygon_index);

    IEnumerable<int> GetNeighbors(int polygon_index);
}

public interface ITopologyFactory<T> 
{
    T Create(int level);
}
