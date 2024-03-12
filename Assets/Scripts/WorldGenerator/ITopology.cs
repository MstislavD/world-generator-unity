using System;
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

    bool IsNeck(int polygon, Func<int, bool> predicate)
    {
        bool current = true;
        int transitions = -1;
        foreach (int neighbor in GetNeighbors(polygon).Where(n => n > -1))
        {
            if (transitions < 0)
            {
                current = predicate(neighbor);
                transitions = 0;
            }
            else if (current != predicate(neighbor))
            {
                current = predicate(neighbor);
                transitions += 1;
                if (transitions == 3)
                {
                    return true;
                }
            }
        }
        return false;
    }
}

public interface ITopologyFactory<T> 
{
    T Create(int level);
}
