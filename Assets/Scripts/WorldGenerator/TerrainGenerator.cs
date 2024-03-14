using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Terrain { Deep, Shallow, Land }

public interface IWorldDataSetter
{
    void SetTerrain(int layer_index, int polygon_index,  Terrain terrain);

    void SetRidge(int layer_index, int edge_index, bool value);
}

public interface IWorldData
{
    Terrain GetTerrain(int layer, int polygon_index);

    bool RegionIsLand(int layer, int polygon_index);

    bool RegionIsSea(int level, int polygon);

    int GetParentRegion(int layer, int polygon_index);

    int GetParentEdge(int level, int edge);

    bool HasRidge(int level, int edge);
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

        int shelf_num = sea_region_num / 2;
        while (region_num > shelf_num)
        {
            int index = hash.Integer(0, region_num);
            generator.SetTerrain(layer_index, regions[index], Terrain.Shallow);
            region_num -= 1;
            hash = hash.Eat(1);
            regions[index] = regions[region_num];
        }

        while (region_num > 0)
        {
            region_num -= 1;
            generator.SetTerrain(layer_index, regions[region_num], Terrain.Deep);
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
            Terrain terrain = height[i] < sea_level ? Terrain.Deep : Terrain.Land;
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
        IWorldData data,
        IWorldDataSetter data_setter,
        ITopology topology,
        int level)
    {
        int parent_level = level - 1;
        foreach(int polygon_index in topology.GetPolygons())
        {
            int parent = data.GetParentRegion(level, polygon_index);
            Terrain terrain = data.GetTerrain(parent_level, parent);
            data_setter.SetTerrain(level, polygon_index, terrain);
        }

        foreach(int edge in topology.GetEdges())
        {
            int parent = data.GetParentEdge(level, edge);
            if (parent > -1)
            {
                bool ridge = data.HasRidge(parent_level, parent);
                data_setter.SetRidge(level, edge, ridge);
            }
            else
            {
                data_setter.SetRidge(level, edge, false);
            }
        }
    }

    public static void ModifyTerrain(
        IWorldData data,
        IWorldDataSetter data_setter,
        ITopology topology,
        int level,
        float sea_percentage,
        float mod_percentage,
        int seed)
    {
        SmallXXHash hash = new SmallXXHash((uint)seed);
        int region_num = topology.PolygonCount;
        int target_land_num = (int)(region_num * (1 - sea_percentage));
        int target_mod_num = (int)(region_num * mod_percentage);
        int land_num = 0;       

        WeightedTree<int> land_tree = new WeightedTree<int>();
        WeightedTree<int> sea_tree = new WeightedTree<int>();
        Func<int, bool> isSea = p => data.RegionIsSea(level, p);
        Func<int, bool> isLand = p => data.RegionIsLand(level, p);
        Func<int, bool> not_neck = p => !topology.IsNeck(p, isSea);

        Action make_land = () =>
        {
            int polygon = sea_tree.Extract(hash.Float01B);
            if (topology.GetNeighbors(polygon).Any(isLand) && not_neck(polygon))
            {
                land_num += 1;
                target_mod_num -= 1;
                data_setter.SetTerrain(level, polygon, Terrain.Land);
                land_tree.Add(polygon, 1f);
                foreach (int neighbor in topology.GetNeighbors(polygon).Where(isSea).Where(not_neck))
                {
                    sea_tree.Add(neighbor, 1f);
                }
            }
        };

        Action make_sea = () =>
        {
            hash = hash.Eat(1);
            int polygon = land_tree.Extract(hash.Float01B);
            if (topology.GetNeighbors(polygon).Any(isSea) && not_neck(polygon))
            {
                land_num -= 1;
                target_mod_num -= 1;
                data_setter.SetTerrain(level, polygon, Terrain.Deep);
                sea_tree.Add(polygon, 1f);
                foreach (int neighbor in topology.GetNeighbors(polygon).Where(isLand).Where(not_neck))
                {
                    land_tree.Add(neighbor, 1f);
                }
            }
        };

        foreach (int polygon in topology.GetPolygons())
        {
            if (isLand(polygon))
            {
                land_num += 1;
                if (topology.GetNeighbors(polygon).Any(isSea) && not_neck(polygon))
                {
                    land_tree.Add(polygon, 1f);
                }
            }
            else if (topology.GetNeighbors(polygon).Any(isLand) && not_neck(polygon))
            {
                sea_tree.Add(polygon, 1f);
            }
        }

        while(land_num < target_land_num)
        {
            make_land();
        }

        while (land_num > target_land_num)
        {
           make_sea();
        }

        while (target_mod_num > 0 && sea_tree.Count > 0 && land_tree.Count > 0)
        {
            if (land_num == target_land_num)
            {
                make_land();
            }            
            if (land_num == target_land_num + 1)
            {
                make_sea();
            }
        }


    }
}
