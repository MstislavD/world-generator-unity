using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Terrain { Sea, Land }

public interface IWorldDataSetter
{
    void SetTerrain(int layer_index, int polygon_index,  Terrain terrain);

    void SetRidge(int layer_index, int edge_index, bool value);
}

public interface IWorldData
{
    Terrain GetTerrain(int layer, int polygon_index);

    bool RegionIsLand(int layer, int polygon_index);

    int ParentRegion(int layer, int polygon_index);
}

public static class TerrainGenerator
{
    public static void GenerateRandomTerrain(
        IWorldDataSetter generator,
        ITopology topolgy,
        int layer_index,
        float sea_level,
        int seed)
    {
        int[] regions = topolgy.GetPolygons().ToArray();
        int region_num = regions.Length;
        int sea_region_num = (int)(region_num * sea_level);
        SmallXXHash hash = new SmallXXHash((uint)seed);

        while (region_num > sea_region_num)
        {            
            int index = (int)(hash.Float01A * region_num);
            if (index >= region_num)
            {
                index = region_num - 1;
            }
            generator.SetTerrain(layer_index, regions[index], Terrain.Land);
            region_num -= 1;
            hash = hash.Eat(1);
            regions[index] = regions[region_num];
        }

        while (region_num > 0)
        {
            region_num -= 1;
            generator.SetTerrain(layer_index, regions[region_num], Terrain.Sea);
        }
    }

    public static void GeneratePerlinTerrain(
        IWorldDataSetter generator,
        ITopology topolgy,
        Noise.Settings settings,
        int layer_index,
        float sea_percentage)
    {
        bool simplex = false;
        int polygon_num = topolgy.PolygonCount;
        float[] height = new float[topolgy.PolygonCount];

        for (int i = 0; i < polygon_num; i++)
        {
            Vector3 normalized_vector = Vector3.Normalize(topolgy.GetCenter(i));

            if (simplex)
            {
                height[i] = Noise.Evaluate<Noise.Simplex3D<Noise.Perlin>>(normalized_vector, settings);
            }
            else
            {
                height[i] = Noise.Evaluate<Noise.Lattice3D<Noise.LatticeNormal, Noise.Perlin>>(normalized_vector, settings);
            }
        }

        List<float> sorted_heights = height.ToList();
        sorted_heights.Sort();
        int sea_level_index = (int)(sea_percentage * (polygon_num - 1));
        float sea_level = sorted_heights[sea_level_index];

        for (int i = 0; i < polygon_num; i++)
        {
            Terrain terrain = height[i] < sea_level ? Terrain.Sea : Terrain.Land;
            generator.SetTerrain(layer_index, i, terrain);
        }
    }

    public static void GenerateRandomRidges(
        IWorldData world_data,
        IWorldDataSetter world_data_setter,
        ITopology topolgy,
        int layer,
        float ridge_density,
        int seed)
    {
        SmallXXHash hash = new SmallXXHash((uint)seed);

        List<int> land_land_edges = new List<int>();
        List<int> land_sea_edges = new List<int>();
        WeightedTree<int> tree = new WeightedTree<int>();

        foreach (int edge in topolgy.GetEdges())
        {
            world_data_setter.SetRidge(layer, edge, false);
            int p1 = topolgy.GetEdgePolygon1(edge);
            int p2 = topolgy.GetEdgePolygon2(edge);
            bool l1 = world_data.RegionIsLand(layer, p1);
            bool l2 = world_data.RegionIsLand(layer, p2);
            if (l1 && l2)
            {
                land_land_edges.Add(edge);
            }
            else if (l1 || l2)
            {
                land_sea_edges.Add(edge);
            }
        }

        float ll_weight = (float)land_sea_edges.Count / land_land_edges.Count;
        tree.AddMany(land_land_edges, ll_weight);
        tree.AddMany(land_sea_edges, 1f);

        int edge_num = land_land_edges.Count + land_sea_edges.Count;
        int ridge_num = (int)(ridge_density * edge_num);

        for (int i = 0; i < ridge_num; i++)
        {
            int edge = tree.Extract(hash.Eat(i).Float01A);
            world_data_setter.SetRidge(layer, edge, true);
        }
    }

    public static void InheritTerrain(
        IWorldData world_data,
        IWorldDataSetter world_data_setter,
        ITopology topology,
        int layer
        )
    {
        int parent_layer = layer - 1;
        foreach(int polygon_index in topology.GetPolygons())
        {
            int parent = world_data.ParentRegion(layer, polygon_index);
            Terrain terrain = world_data.GetTerrain(parent_layer, parent);
            world_data_setter.SetTerrain(layer, polygon_index, terrain);
        }

    }
}
