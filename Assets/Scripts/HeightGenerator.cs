using System.Collections;
using System.Collections.Generic;

public interface IHeightGenerator
{
    public float GenerateHeight(UnityEngine.Vector3 vector);
    public void Regenerate();
}

public class HeightGenerator : IHeightGenerator
{
    public float GenerateHeight(UnityEngine.Vector3 vector)
    {
        return UnityEngine.Random.Range(0f, 1f);
    }

    public void Regenerate()
    {
    }
}

public class PerlinHeightGenerator : IHeightGenerator
{
    bool simplex = false;

    public Noise.Settings settings { get; set; }

    public PerlinHeightGenerator()
    {
        Noise.Settings newSettings = Noise.Settings.Default;
        newSettings.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        settings = newSettings;
    }

    public PerlinHeightGenerator(Noise.Settings settings)
    {
        settings.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        this.settings = settings;
    }

    public float GenerateHeight(UnityEngine.Vector3 vector)
    {
        float height;

        if (simplex)
        {
            height = Noise.Evaluate<Noise.Simplex3D<Noise.Perlin>>(vector, settings);
        }
        else
        {
            height = Noise.Evaluate<Noise.Lattice3D<Noise.LatticeNormal, Noise.Perlin>>(vector, settings);
        }      
      
        return height + 0.5f;
    }

    public void Regenerate()
    {
        Noise.Settings newSettings = settings;
        newSettings.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        settings = newSettings;
    }

    public void SetNoiseParameters(int frequency, int ocatves, int lacunarity, float persistence)
    {
        Noise.Settings newSettings = settings;
        newSettings.frequency = frequency;
        newSettings.octaves = ocatves;
        newSettings.lacunarity = lacunarity;
        newSettings.persistence = persistence;
        settings = newSettings;
    }
}
