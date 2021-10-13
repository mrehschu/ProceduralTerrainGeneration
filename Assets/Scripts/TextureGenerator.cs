using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator {
    
    // static class to create textures form different source data


    // visualizes a heightmap in grayscales
    public static Texture2D TextureFromHeightMap(float[,] heightMap) {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);        

        Color[] colorMap = new Color[width * height];
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return TextureFromColorMap(colorMap, width, height);
    }

    // visualizes chunks according to their biome in as many gray steps as there are biomes
    public static Texture2D TextureFromBiomeMap(int[,] biomeMap, int numberOfBiomes, float scaleDivider) {
        int customChunkSize = (int)(MapGenerator.mapChunkSize / scaleDivider);
        int width = biomeMap.GetLength(0) * customChunkSize;
        int height = biomeMap.GetLength(1) * customChunkSize;
        int bx = 0, by = 0;

        Color[] colorMap = new Color[width * height];
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float colorValue = 1f / (numberOfBiomes - 1) * biomeMap[bx, by];
                colorMap[y * width + x] = new Color(colorValue, colorValue, colorValue);
                if (x > 0 && x % customChunkSize == 0) bx++;
            }

            bx = 0;
            if (y > 0 && y % customChunkSize == 0) by++;
        }

        return TextureFromColorMap(colorMap, width, height);
    }

    // draws a color map as given
    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height) {
        Texture2D texture = new Texture2D(width, height);

        texture.SetPixels(colorMap);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();

        return texture;
    }

}
