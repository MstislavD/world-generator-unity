using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class Edge
{
    public int Polygon1 { get; }

    public int Polygon2 { get; }

    public Edge(int p1, int p2)
    {
        Polygon1 = p1;
        Polygon2 = p2;
    }
}

public class Polygon
{
    Vector3[] vertices;
    int[] edges;
    public int Parent { get; set; }

    public Polygon(int sides)
    {
        vertices = new Vector3[sides];
        edges = (-1).Populate(sides);
    }

    public Vector3 Center
    {
        get
        {
            Vector3 center = Vector3.zero;
            for (int i = 0; i < Sides; i++)
            {
                center += vertices[i];
            }
            return center / Sides;
        }
    }

    public void SetVertex(int index, Vector3 vector)
    {
        vertices[index] = normalize(vector);
    }

    public void SetEdge(int direction, int edgeIndex) => edges[direction] = edgeIndex;

    public Vector3 GetVertex(int direction) => vertices[direction];

    public IEnumerable<Vector3> Vertices => vertices;

    public int GetEdge(int direction) => edges[direction];

    public int GetDirectionByEdge(int edge)
    {
        for (int i = 0; i < Sides; i++)
        {
            if (edges[i] == edge)
            {
                return i;
            }
        }
        return -1;
    }

    public int Sides => vertices.Length;
}
