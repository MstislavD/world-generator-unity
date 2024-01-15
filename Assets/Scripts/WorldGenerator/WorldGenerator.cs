using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator
{
    const int sphereLevels = 6;

    PolygonSphere[] spheres;

    public WorldGenerator(IHeightGenerator heightGenerator)
    {
        spheres = new PolygonSphere[sphereLevels];
        for (int i = 0; i < sphereLevels; i++)
        {
            spheres[i] = new PolygonSphere((int)Mathf.Pow(2, i) - 1, heightGenerator);
        }
    }

    public PolygonSphere GetSphere(int level) => spheres[level];

    public int SphereCount => spheres.Length;

    public void Regenerate()
    {
        for (int i = 0; i < spheres.Length; i++)
        {
            spheres[i].RegenerateData();
        }
    }

    public int GetPolygonIndex(int polygonIndex, int sphereLevel, int dataLevel)
    {
        for (int level = sphereLevel; level > dataLevel; level--)
        {
            polygonIndex = spheres[level].GetPolygonData(polygonIndex).region;
        }
        return polygonIndex;
    }

    public int GetEdgeIndex(int edgeIndex, int sphereLevel, int dataLevel)
    {
        for (int level = sphereLevel; level > dataLevel && edgeIndex > -1; level--)
        {
            edgeIndex = getRegionEdgeIndex(edgeIndex, level);
        }
        return edgeIndex;
    }

    int getRegionEdgeIndex(int edgeIndex, int sLevel)
    {
        PolygonSphere sphere = spheres[sLevel];
        PolygonSphere parentSphere = spheres[sLevel - 1];
        PolygonData pd1 = sphere.GetPolygonData(sphere.GetEdgePolygon1(edgeIndex));
        PolygonData pd2 = sphere.GetPolygonData(sphere.GetEdgePolygon2(edgeIndex));
        if (pd1.region != pd2.region)
        {
            return parentSphere.GetEdge(pd1.region, pd2.region);
        }
        return -1;
    }
}
