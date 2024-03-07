using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Terrain { Sea, Land }

public interface IWorldDataSetter
{
    void SetTerrain(int layer_index, int polygon_index,  Terrain terrain);

    void SetRidge(int layer_index, int polygon_index, float value);
}

public class TerrainGenerator
{
    public static void GenerateRandomTerrain(IWorldDataSetter generator, ITopology topolgy, int layer_index, float sea_level, int seed)
    {
        int[] regions = topolgy.GetPolygons().ToArray();
        int region_num = regions.Length;
        int sea_region_num = (int)(region_num * sea_level);
        SmallXXHash hash = new SmallXXHash((uint)seed);

        while (region_num > sea_region_num)
        {            
            int index = (int)(hash.Float01A * region_num);
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

    public static void GenerateRandomRidges(IWorldDataSetter generator, ITopology topolgy, int layer_index, int seed)
    {
        SmallXXHash hash = new SmallXXHash((uint)seed);
        for (int i = 0; i < topolgy.EdgeCount; i++)
        {
            hash = hash.Eat(1);
            generator.SetRidge(layer_index, i, hash.Float01A);
        }
    }
}
