using System;
using UnityEngine;

public static partial class Noise
{
    [Serializable]
    public struct Settings
    {
        public int seed;

        [Min(1)]
        public int frequency;

        [Range(1, 6)]
        public int octaves;

        [Range(2, 4)]
        public int lacunarity;

        [Range(0f, 1f)]
        public float persistence;

        public static Settings Default => new Settings { frequency = 4, octaves = 1, lacunarity = 2, persistence = 0.5f };

        public Settings ChangeSeed(int new_seed) => new Settings {
            frequency = frequency, octaves = octaves, lacunarity = lacunarity, seed = new_seed, persistence = persistence };
    }

    public interface INoise
    {
        float GetNoise(Vector3 position, SmallXXHash hash, int frequency);
    }

    public static float Evaluate<N>(Vector3 point, Settings settings) where N : INoise
    {
        SmallXXHash hash = SmallXXHash.Seed(settings.seed);
        int frequency = settings.frequency;
        float sum = 0f;
        float amplitude = 1f, amplitudeSum = 0f;

        for (int o = 0; o < settings.octaves; o++)
        {
            sum += amplitude * default(N).GetNoise(point, hash + o, frequency);
            amplitudeSum += amplitude;
            frequency *= settings.lacunarity;
            amplitude *= settings.persistence;
        }

        return sum / amplitudeSum;
    }
}
