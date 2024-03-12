using System;
using System.Collections.Generic;
using System.Linq;

public class WorldGenerator<TTopology> : IWorldData, IWorldDataSetter
    where TTopology: ITopology
{
    public enum HeightGeneratorType { Random, Perlin }

    //int sphereLevels = 6;

    TTopology[] layers;
    Action<string> logger = s => Console.WriteLine(s);

    PolygonData[][] polygon_data;
    EdgeData[][] edge_data;

    HeightGeneratorType height_generator_type;
    Noise.Settings height_perlin_settings;
    float sea_percentage;
    float ridge_density;
    int seed;

    public WorldGenerator(ITopologyFactory<TTopology> topology_factory, int sphereLevels, Action<string> logger)
    {
        this.logger = logger;
        //this.sphereLevels = sphereLevels;
        layers = new TTopology[sphereLevels];
        polygon_data = new PolygonData[sphereLevels][];
        edge_data = new EdgeData[sphereLevels][];

        for (int i = 0; i < sphereLevels; i++)
        {
            layers[i] = topology_factory.Create(i);
            polygon_data[i] = new PolygonData[layers[i].PolygonCount];
            edge_data[i] = new EdgeData[layers[i].EdgeCount];
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

    public TTopology GetSphere(int level) => layers[level];

    public bool RegionIsSea(int level, int polygon_index)
    {
        return polygon_data[level][polygon_index].terrain == Terrain.Sea;
    }

    public bool RegionIsLand(int level, int polygon_index)
    {
        return polygon_data[level][polygon_index].terrain == Terrain.Land;
    }

    public bool EdgeHasRidge(int level, int edge_index) => edge_data[level][edge_index].ridge;

    public int LevelCount => layers.Length;

    public void Regenerate()
    {
        for (int layer_index = 0; layer_index < layers.Length; layer_index++)
        {
            regenerate_data(layer_index);
        }
    }

    public int GetPolygonIndex(int polygonIndex, int layer_level, int data_level)
    {
        for (int level = layer_level; level > data_level; level--)
        {
            polygonIndex = get_region(level, polygonIndex);
        }
        return polygonIndex;
    }

    public int GetEdgeIndex(int edgeIndex, int layer_level, int data_level)
    {
        for (int level = layer_level; level > data_level && edgeIndex > -1; level--)
        {
            edgeIndex = getRegionEdgeIndex(edgeIndex, level);
        }
        return edgeIndex;
    }

    public void SetTerrain(int layer_index, int polygon_index, Terrain terrain)
    {
        polygon_data[layer_index][polygon_index].terrain = terrain;
    }

    public void SetRidge(int layer_index, int edge_index, bool ridge)
    {
        edge_data[layer_index][edge_index].ridge = ridge;
    }

    int getRegionEdgeIndex(int edge_index, int level)
    {
        TTopology layer = layers[level];
        TTopology parent_layer = layers[level - 1];
        int region1 = get_region(level, layer.GetEdgePolygon1(edge_index));
        int region2 = get_region(level, layer.GetEdgePolygon2(edge_index));

        if (region1 != region2)
        {
            return parent_layer.GetEdge(region1, region2);
        }
        return -1;
    }

    int get_region(int level, int polygon_index)
    {
        return polygon_data[level][polygon_index].region;
    }

    void regenerate_data(int level)
    {
        generate_regions(level);

        ITopology topology = layers[level];
        if (height_generator_type == HeightGeneratorType.Random)
        {
            TerrainGenerator.GenerateRandomTerrain(this, topology, level, sea_percentage, seed);
        }
        else if (height_generator_type == HeightGeneratorType.Perlin)
        {
            TerrainGenerator.GeneratePerlinTerrain(this, topology, height_perlin_settings, level, sea_percentage);
        }

        TerrainGenerator.GenerateRandomRidges(this, this, topology, level, ridge_density, seed + 1);
    }

    void generate_regions(int level)
    {
        TTopology layer = layers[level];
        SmallXXHash hash = new SmallXXHash((uint)seed);

        for (int i = 0; i < layer.PolygonCount; i++)
        {
            int parent = layer.GetParent(i);
            IEnumerable<int> nParents = layer.GetNeighbors(i).Select(layer.GetParent).Where(pi => pi > -1);
            polygon_data[level][i].region = parent > -1 ?
                parent :
                nParents.ToArray()[hash.Eat(i).Float01B < 0.5f ? 0 : 1];
        }
    }
}
