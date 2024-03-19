using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public enum Terrain { Deep, Shallow, Land }

public interface IWorldDataSetter
{
    void SetTerrain(int layer_index, int polygon_index,  Terrain terrain);

    void SetRidge(int layer_index, int edge_index, bool value);

    void SetContinent(int level, int polygon, int continent);

    void AddContinentFeature(int continent, RegionFeature feature);

    void AddContinent();
}

public interface IWorldData
{
    Terrain GetTerrain(int layer, int polygon_index);

    bool RegionIsLand(int layer, int polygon_index);

    bool RegionIsSea(int level, int polygon);

    int GetParentRegion(int layer, int polygon_index);

    int GetParentEdge(int level, int edge);

    int GetContinent(int level, int polygon);

    bool HasRidge(int level, int edge);

    bool HasFeature(int level, int polygon, RegionFeature feature);
}

public static class TerrainGenerator
{
    public static void GenerateRandomTerrain(
        IWorldDataSetter data_setter,
        ITopology topolgy,
        int level,
        float sea_level,
        int seed)
    {
        float shallow_percentage = 0.5f;

        int[] regions = topolgy.GetPolygons().ToArray();
        int region_num = regions.Length;
        int sea_region_num = (int)(region_num * sea_level);
        SmallXXHash hash = new SmallXXHash((uint)seed);
        int continent = 0;

        Action<int> set_continent_feature = continent =>
        {
            hash = hash.Eat(1);
            if (hash.Float01C > 0.5f)
            {
                data_setter.AddContinentFeature(continent, RegionFeature.Snaky);
            }
            else
            {
                data_setter.AddContinentFeature(continent, RegionFeature.Round);
            }
        };

        while (region_num > sea_region_num)
        {
            int index = hash.Integer(0, region_num);
            data_setter.SetTerrain(level, regions[index], Terrain.Land);
            data_setter.AddContinent();
            data_setter.SetContinent(level, regions[index], continent);
            set_continent_feature(continent);
            continent += 1;
            region_num -= 1;
            hash = hash.Eat(1);
            regions[index] = regions[region_num];           
        }

        int shallow_num = (int)(sea_region_num * shallow_percentage);
        while (region_num > shallow_num)
        {
            int index = hash.Integer(0, region_num);
            data_setter.SetTerrain(level, regions[index], Terrain.Shallow);
            data_setter.AddContinent();
            data_setter.SetContinent(level, regions[index], continent);
            //set_continent_feature(continent);
            continent += 1;
            region_num -= 1;
            hash = hash.Eat(1);
            regions[index] = regions[region_num];           
        }

        while (region_num > 0)
        {
            region_num -= 1;
            data_setter.SetTerrain(level, regions[region_num], Terrain.Deep);
            data_setter.SetContinent(level, regions[region_num], -1);
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
        foreach(int polygon in topology.GetPolygons())
        {
            int parent = data.GetParentRegion(level, polygon);
            Terrain terrain = data.GetTerrain(parent_level, parent);
            int continent = data.GetContinent(parent_level, parent);
            data_setter.SetTerrain(level, polygon, terrain);
            data_setter.SetContinent(level, polygon, continent);
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
        float island_percentage,
        int seed)
    {
        float weight_base = 2f;

        SmallXXHash hash = new SmallXXHash((uint)seed);
        int region_num = topology.PolygonCount;
        int target_land_num = (int)(region_num * (1 - sea_percentage));
        int target_mod_num = (int)(region_num * mod_percentage);
        int land_num = 0;       

        WeightedTree<int> land_tree = new WeightedTree<int>();
        WeightedTree<int> sea_tree = new WeightedTree<int>();
        WeightedTree<int> shallow_tree = new WeightedTree<int>();

        Func<int, int> get_continent = p => data.GetContinent(level, p);
        Func<int, bool> is_sea = p => data.RegionIsSea(level, p);
        Func<int, bool> is_land = p => data.RegionIsLand(level, p);
        Func<int, bool> is_coast = p => topology.GetNeighbors(p).Any(is_land) && topology.GetNeighbors(p).Any(is_sea);
        Func<int, bool> not_terrrain_neck = p => !topology.IsNeck(p, is_sea);
        Func<int, bool> not_continent_neck = p => is_sea(p) || !topology.IsNeck(p, get_continent, is_sea);
        Func<int, bool> can_convert = p => is_coast(p) && not_terrrain_neck(p) && not_continent_neck(p);
        
        Func<int, int> sea_neighbors_num = p => topology.GetNeighbors(p).Count(is_sea);

        int island_num = (int)(Mathf.Pow(region_num, 0.5f) * island_percentage);

        Action<int> convert_continent = (int polygon) =>
        {
            int cont = data.GetContinent(level, polygon);
            List<int> n_conts = new List<int>();
            foreach (int n_cont in topology.GetNeighbors(polygon).Where(is_land).Select(get_continent))
            {
                if (n_cont == cont)
                {
                    return;
                }
                n_conts.Add(n_cont);
            }
            hash = hash.Eat(1);
            int i = hash.Integer(0, n_conts.Count);
            data_setter.SetContinent(level, polygon, n_conts[i]);
        };

        Func<int, float> get_weight_land = (polygon) =>
        {
            int sea_num = topology.GetNeighbors(polygon).Count(is_sea);
            int factor =
                data.HasFeature(level, polygon, RegionFeature.Round) ? sea_num - 3 :
                data.HasFeature(level, polygon, RegionFeature.Snaky) ? 3 - sea_num :
                0;
            return Mathf.Pow(weight_base, factor);
        };

        Func<int, float> get_weight_sea = (polygon) =>
        {
            int n = topology.GetNeighbors(polygon).First(is_land);
            int sea_num = topology.GetNeighbors(polygon).Count(is_sea);
            int factor =
               data.HasFeature(level, n, RegionFeature.Round) ? 3 - sea_num :
               data.HasFeature(level, n, RegionFeature.Snaky) ? sea_num - 3 :
               0;
            return Mathf.Pow(weight_base, factor);
        };

        Action make_island = () =>
        {
            hash = hash.Eat(1);
            int polygon = shallow_tree.Extract(hash.Float01B);
            if (topology.GetNeighbors(polygon).All(is_sea))
            {
                land_num += 1;
                target_mod_num -= 1;
                island_num -= 1;
                data_setter.SetTerrain(level, polygon, Terrain.Land);
            }
        };

        Action make_land = () =>
        {
            hash = hash.Eat(1);
            int polygon = sea_tree.Extract(hash.Float01B);
            if (can_convert(polygon))
            {
                land_num += 1;
                target_mod_num -= 1;
                data_setter.SetTerrain(level, polygon, Terrain.Land);
                convert_continent(polygon);
                land_tree.Add(polygon, get_weight_land(polygon));
                foreach (int neighbor in topology.GetNeighbors(polygon).Where(is_sea).Where(can_convert))
                {
                    sea_tree.Add(neighbor, get_weight_sea(polygon));
                }
            }
        };

        Action make_sea = () =>
        {
            hash = hash.Eat(1);
            int polygon = land_tree.Extract(hash.Float01B);
            if (can_convert(polygon))
            {
                land_num -= 1;
                target_mod_num -= 1;
                data_setter.SetTerrain(level, polygon, Terrain.Shallow);
                sea_tree.Add(polygon, get_weight_sea(polygon));
                foreach (int neighbor in topology.GetNeighbors(polygon).Where(is_land).Where(can_convert))
                {
                    land_tree.Add(neighbor, get_weight_land(polygon));
                }
            }
        };

        foreach (int polygon in topology.GetPolygons())
        {
            if (is_land(polygon))
            {
                land_num += 1;
                if (can_convert(polygon))
                {
                    land_tree.Add(polygon, get_weight_land(polygon));
                }
            }
            else if (can_convert(polygon))
            {
                sea_tree.Add(polygon, get_weight_sea(polygon));
            }
            else if (topology.GetNeighbors(polygon).All(is_sea) && data.GetTerrain(level, polygon) == Terrain.Shallow)
            {
                shallow_tree.Add(polygon, 1f);
            }
        }

        while (island_num > 0 && shallow_tree.Count > 0)
        {
            make_island();
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
