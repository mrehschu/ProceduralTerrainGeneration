using System.Collections;
using UnityEngine;

public static class Noise {
    
    // static class to provide float maps using perlin noise

    public static float[,] GenerateNoiseMap(int mapSize, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset) {
        float[,] noiseMap = new float[mapSize, mapSize];
        System.Random random = new System.Random(seed);
        float halfMapSize = mapSize / 2f;
        
        float maxValue = 1;
        float amplitude = 1;
        float frequency = 1;

        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++) {
            octaveOffsets[i] = new Vector2(random.Next(-100000, 100000) + offset.x, random.Next(-10000, 10000) + offset.y);
            
            maxValue += amplitude;
            amplitude *= persistance;
        }

        if (scale <= 0) scale = 0.0001f;        
        
        for (int y = 0; y < mapSize; y++) {
            for (int x = 0; x < mapSize; x++) {

                amplitude = 1;
                frequency = 1;
                float noiseValue = 0;

                for (int i = 0; i < octaves; i++) {
                    float sampleX = (x - halfMapSize + octaveOffsets[i].x) * frequency / scale;
                    float sampleY = (y - halfMapSize + octaveOffsets[i].y) * frequency / scale;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseValue += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                noiseMap[x, y] = noiseValue;
            }
        }

        for (int y = 0; y < mapSize; y++) {
            for (int x = 0; x < mapSize; x++) {
                noiseMap[x, y] = noiseMap[x, y] / maxValue * 1.75f;
                noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y]);
            }
        }

        return noiseMap;
    }

    public static int GenerateBiomeIndex(Vector2 chunkCoords, ref BiomeData[] biomes, int seed, float scale) {

        System.Random random = new System.Random(seed);
        Vector2 mapOffset = new Vector2(random.Next(-100000, 100000) + chunkCoords.x, random.Next(-10000, 10000) + chunkCoords.y);
        scale /= 8;

        float noiseValue = Mathf.PerlinNoise(mapOffset.x / scale, mapOffset.y / scale);
        float biomeSum = 0;
        for (int i = 0; i < biomes.Length; i++) {
            biomeSum += biomes[i].commonness;
            if (biomeSum >= noiseValue) return i;
        }

        return 0;
    }

}
