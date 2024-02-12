using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class WorldGenerator
{
    public enum HeightGeneratorType { Random, Perlin }

    //int sphereLevels = 6;

    PolygonSphere[] spheres;
    Action<string> logger = s => Console.WriteLine(s);
    float[] sea_level_by_sphere;

    HeightGeneratorType height_generator_type;
    Noise.Settings height_perlin_settings;
    float sea_percentage;
    float ridge_density;
    int seed;

    public WorldGenerator(Action<string> logger, int sphereLevels)
    {
        this.logger = logger;
        //this.sphereLevels = sphereLevels;
        spheres = new PolygonSphere[sphereLevels];
        sea_level_by_sphere = new float[sphereLevels];

        for (int i = 0; i < sphereLevels; i++)
        {
            spheres[i] = new PolygonSphere((int)MathF.Pow(2, i) - 1);
        }
    }

    public bool UpdateSettings(
        HeightGeneratorType heightGenType,
        Noise.Settings perlinSettings,
        float sea_percentage,
        float ridge_density,
        int seed)
    {
        bool updated = false;

        if (this.seed != seed)
        {
            updated = true;
            this.seed = seed;
        }

        if (height_generator_type != heightGenType)
        {
            updated = true;
            height_generator_type = heightGenType;
        }

        if (!height_perlin_settings.Equals(perlinSettings))
        {
            updated = updated || heightGenType == HeightGeneratorType.Perlin;
            height_perlin_settings = perlinSettings;
        }

        if (this.sea_percentage != sea_percentage)
        {
            updated = true;
            this.sea_percentage = sea_percentage;
        }

        if (this.ridge_density != ridge_density)
        {
            updated = true;
            this.ridge_density = ridge_density;
        }

        return updated;
    }

    public PolygonSphere GetSphere(int level) => spheres[level];

    public bool RegionIsSea(int sphere_level, int polygon_index)
    {
        return get_height(sphere_level, polygon_index) < sea_level_by_sphere[sphere_level];
    }

    public bool EdgeHasRidge(int sphere_level, int edge_index)
    {
        return get_ridge(sphere_level, edge_index) < ridge_density;
    }

    public int SphereCount => spheres.Length;

    public void Regenerate()
    {
        IHeightGenerator height_generator = null;
        if (height_generator_type == HeightGeneratorType.Random)
        {
            height_generator = new HeightGenerator(seed);
        }
        else if (height_generator_type == HeightGeneratorType.Perlin)
        {
            height_generator = new PerlinHeightGenerator(height_perlin_settings);
        }

        for (int i = 0; i < spheres.Length; i++)
        {
            spheres[i].RegenerateData(height_generator, seed);
            calculate_sea_level(i);
        }
    }

    private void calculate_sea_level(int sphere_level)
    {
        List<float> sorted_heights = 
            Enumerable.Range(0, spheres[sphere_level].PolygonCount).
            Select(i => get_height(sphere_level, i)).
            ToList();
        sorted_heights.Sort();
        int sea_level_index = (int)(sea_percentage * (spheres[sphere_level].PolygonCount-1));
        sea_level_by_sphere[sphere_level] = sorted_heights[sea_level_index];
    }

    public int GetPolygonIndex(int polygonIndex, int sphereLevel, int dataLevel)
    {
        for (int level = sphereLevel; level > dataLevel; level--)
        {
            polygonIndex = get_region(level, polygonIndex);
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

    int getRegionEdgeIndex(int edgeIndex, int sphere_level)
    {
        PolygonSphere sphere = spheres[sphere_level];
        PolygonSphere parentSphere = spheres[sphere_level - 1];
        int region1 = get_region(sphere_level, sphere.GetEdgePolygon1(edgeIndex));
        int region2 = get_region(sphere_level, sphere.GetEdgePolygon2(edgeIndex));

        if (region1 != region2)
        {
            return parentSphere.GetEdge(region1, region2);
        }
        return -1;
    }

    float get_height(int sphere_index, int polygon_index)
    {
        return spheres[sphere_index].GetPolygonData(polygon_index).height;
    }

    int get_region(int sphere_index, int polygon_index)
    {
        return spheres[sphere_index].GetPolygonData(polygon_index).region;
    }

    float get_ridge(int sphere_index, int edge_index)
    {
        return spheres[sphere_index].GetEdgeData(edge_index).ridge;
    }
}
