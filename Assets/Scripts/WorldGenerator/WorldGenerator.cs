using System;
using System.Collections.Generic;
using System.Linq;

public class PolygonSphereTopology: ITopology
{
    PolygonSphere sphere;

    public PolygonSphereTopology(PolygonSphere sphere)
    {
        this.sphere = sphere;
    }

    public int EdgeCount() => sphere.EdgeCount;

    public UnityEngine.Vector3 GetCenter(int polygon_index) => sphere.GetCenter(polygon_index);

    public int PolygonCount() => sphere.PolygonCount;
}

public class WorldGenerator
{
    public enum HeightGeneratorType { Random, Perlin }

    //int sphereLevels = 6;

    PolygonSphere[] spheres;
    Action<string> logger = s => Console.WriteLine(s);

    PolygonData[][] polygon_data;
    EdgeData[][] edge_data;

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
        polygon_data = new PolygonData[sphereLevels][];
        edge_data = new EdgeData[sphereLevels][];

        for (int i = 0; i < sphereLevels; i++)
        {
            spheres[i] = new PolygonSphere((int)MathF.Pow(2, i) - 1);
            polygon_data[i] = new PolygonData[spheres[i].PolygonCount];
            edge_data[i] = new EdgeData[spheres[i].EdgeCount];
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
        return polygon_data[sphere_level][polygon_index].terrain == Terrain.Sea;
    }

    public bool EdgeHasRidge(int sphere_level, int edge_index)
    {
        return get_ridge(sphere_level, edge_index) < ridge_density;
    }

    public int SphereCount => spheres.Length;

    public void Regenerate()
    {
        for (int layer_index = 0; layer_index < spheres.Length; layer_index++)
        {
            regenerate_data(layer_index);
        }
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

    public void SetTerrain(int layer_index, int polygon_index, Terrain terrain)
    {
        polygon_data[layer_index][polygon_index].terrain = terrain;
    }

    public void SetRidge(int layer_index, int edge_index, float ridge)
    {
        edge_data[layer_index][edge_index].ridge = ridge;
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

    int get_region(int sphere_index, int polygon_index)
    {
        return polygon_data[sphere_index][polygon_index].region;
    }

    float get_ridge(int sphere_index, int edge_index)
    {
        return edge_data[sphere_index][edge_index].ridge;
    }

    void regenerate_data(int layer_index)
    {
        generate_regions(layer_index);

        ITopology topology = new PolygonSphereTopology(spheres[layer_index]);
        if (height_generator_type == HeightGeneratorType.Random)
        {
            TerrainGenerator.GenerateRandomTerrain(this, topology, layer_index, sea_percentage, seed);
        }
        else if (height_generator_type == HeightGeneratorType.Perlin)
        {
            TerrainGenerator.GeneratePerlinTerrain(this, topology, height_perlin_settings, layer_index, sea_percentage);
        }

        TerrainGenerator.GenerateRandomRidges(this, topology, layer_index, seed + 1);
    }

    void generate_regions(int layer_index)
    {
        PolygonSphere layer = spheres[layer_index];
        SmallXXHash hash = new SmallXXHash((uint)seed);

        for (int i = 0; i < layer.PolygonCount; i++)
        {
            int parent = layer.GetParent(i);
            IEnumerable<int> nParents = layer.GetNeighbors(i).Select(layer.GetParent).Where(pi => pi > -1);
            polygon_data[layer_index][i].region = parent > -1 ?
                parent :
                nParents.ToArray()[hash.Eat(i).Float01B < 0.5f ? 0 : 1];
        }
    }
}
