using System.Collections;
using System.Collections.Generic;

public enum RegionFeature { None, Snaky, Round }

public struct PolygonData
{
    //public float height;
    public int region;
    public Terrain terrain;
    public int continent;
}

public struct EdgeData
{
    public bool ridge;
}

public class ContinentData
{
    public RegionFeature feature;
}
