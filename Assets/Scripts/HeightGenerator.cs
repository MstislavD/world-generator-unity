using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System;


public interface IHeightGenerator
{
    public float GenerateHeight(Vector3 vector);

    public float GenerateRidge();
}

public class HeightGenerator : IHeightGenerator
{
    SmallXXHash hash;

    public HeightGenerator(int seed)
    {
        hash = new SmallXXHash((uint)seed);
    }

    public float GenerateHeight(Vector3 vector)
    {
        hash = hash.Eat(1);
        return hash.Float01A;
    }

    public float GenerateRidge()
    {
        hash = hash.Eat(1);
        return hash.Float01A;
    }
}

public class PerlinHeightGenerator : IHeightGenerator
{
    bool simplex = false;

    public Noise.Settings settings { get; set; }

    public PerlinHeightGenerator(Noise.Settings settings)
    {
        this.settings = settings;
    }

    public float GenerateHeight(Vector3 vector)
    {
        Vector3 normalized_vector = Vector3.Normalize(vector);
        float height;

        if (simplex)
        {
            height = Noise.Evaluate<Noise.Simplex3D<Noise.Perlin>>(normalized_vector, settings);
        }
        else
        {
            height = Noise.Evaluate<Noise.Lattice3D<Noise.LatticeNormal, Noise.Perlin>>(normalized_vector, settings);
        }      
      
        return height + 0.5f;
    }

    public float GenerateRidge()
    {
        return 1f;
    }
}
