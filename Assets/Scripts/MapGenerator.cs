using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class MapGenerator : MonoBehaviour {

    [HideInInspector] public static MapGenerator Instance;
    public const int mapChunkSize = 241;

    Queue<ChunkData> processedChunkData;
    Dictionary<Vector2, TerrainChunk> generatedChunks;

    [Header("Global Settings")]
    [SerializeField] Material mapMaterial;
    [SerializeField] int seed;
    [SerializeField] Vector2 offset;
    [SerializeField] BiomeData[] biomes;
    [Range(1, mapChunkSize/2)] [SerializeField] int biomeBlend = 10;

    [SerializeField] float noiseScale = 5;
    [Range(1, 10)] [SerializeField] int octaves = 3;
    [Range(0, 1)] [SerializeField] float persistance = 0.5f;
    [SerializeField] float lacunarity = 2;


#if UNITY_EDITOR
    enum DrawMode { HeightMap, ColorMap, BiomeMap, Mesh }

    [Header("Test-Edit Settings")]
    [SerializeField] DrawMode drawMode;
    [Range(1, 6)] [SerializeField] int levelOfDetail = 1;
    public bool AutoUpdate;

#endif



    public void RequestMapChunk(Vector2 chunkCoords, int requestedLOD) {
        if (biomes.Length == 0) return;
        foreach (BiomeData biome in biomes) if (biome == null) return;

        if (generatedChunks.TryGetValue(chunkCoords, out TerrainChunk currentChunk)) {            
            if (requestedLOD == -1) {
                Destroy(currentChunk.meshObject);
                Resources.UnloadUnusedAssets();                 // otherwise there is a memory leak with unitys handling of meshes
                generatedChunks.Remove(chunkCoords);
            }
            if (currentChunk.levelOfDetail <= requestedLOD) return;
        }

        if (requestedLOD == -1) return;

        ThreadStart task = delegate {
            ChunkData chunkData = GenerateMapChunk(chunkCoords, requestedLOD);
            lock (processedChunkData) {
                processedChunkData.Enqueue(chunkData);
            }
        };

        new Thread(task).Start();
    }

    public ChunkData GenerateMapChunk(Vector2 chunkCoords, int levelOfDetail) {
        Vector2 chunkLocation = chunkCoords * (mapChunkSize - 1);
        chunkLocation.x -= (mapChunkSize - 1) / 2;
        chunkLocation.y -= (mapChunkSize - 1) / 2;

        Vector2[] offsets = new Vector2[] { new Vector2(1, 0), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(0, -1) };
        int[] otherBiomes = new int[4];
        for (int i = 0; i < 4; i++) {
            otherBiomes[i] = Noise.GenerateBiomeIndex(offsets[i], ref biomes, seed, noiseScale);
        }

        int biomeIndex = Noise.GenerateBiomeIndex(chunkCoords, ref biomes, seed, noiseScale);
        float[,] heightMap = Noise.GenerateNoiseMap(mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, chunkLocation);        

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for (int x = 0; x < mapChunkSize; x++) {
            for (int y = 0; y < mapChunkSize; y++) {
                float currentHeight = heightMap[x, y];
                for (int i = 0; i < biomes[biomeIndex].regions.Length; i++) {
                    if (currentHeight <= biomes[biomeIndex].regions[i].maxHeight) {
                        colorMap[y * mapChunkSize + x] = biomes[biomeIndex].regions[i].color;
                        break;
                    }
                }
            }
        }

        MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap, levelOfDetail, biomes[biomeIndex].meshHeightMultiplier, biomes[biomeIndex].meshHeightCurve);
        ChunkData chunkData = new ChunkData(chunkCoords, chunkLocation, levelOfDetail, biomeIndex, heightMap, meshData, colorMap);
        if (levelOfDetail == 1) AdjustBiomeBorders(chunkData);
        return chunkData;
    }

    public void AdjustBiomeBorders(ChunkData currentChunk) {
        bool[] neighborCalculated = new bool[4];
        Vector2[] neighborOffsets = new Vector2[] { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        for (int i = 0; i < 4; i++) neighborCalculated[i] = (generatedChunks != null) ? generatedChunks.ContainsKey(currentChunk.chunkCoords + neighborOffsets[i]) : false;

        for (int i = 0; i < 4; i++) {
            int neighborBiomeIndex;
            Vector3[] neighborVertices;

            // fetching neighboring chunkData
            if (neighborCalculated[i] && false) {
                TerrainChunk neighborChunk = generatedChunks[currentChunk.chunkCoords + neighborOffsets[i]];
                neighborBiomeIndex = neighborChunk.biomeIndex;
                
                if (neighborBiomeIndex == currentChunk.biomeIndex) continue;

                neighborVertices = neighborChunk.vertices;
            } else {
                neighborBiomeIndex = Noise.GenerateBiomeIndex(currentChunk.chunkCoords + neighborOffsets[i], ref biomes, seed, noiseScale);

                if (neighborBiomeIndex == currentChunk.biomeIndex) continue;

                Vector2 neighborChunkLocation = (currentChunk.chunkCoords + neighborOffsets[i]) * (mapChunkSize - 1);
                neighborChunkLocation.x -= (mapChunkSize - 1) / 2;
                neighborChunkLocation.y -= (mapChunkSize - 1) / 2;
                float[,] neighborHeightMap = Noise.GenerateNoiseMap(mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, neighborChunkLocation);
                neighborVertices = MeshGenerator.GenerateTerrainMesh(neighborHeightMap, 1, biomes[neighborBiomeIndex].meshHeightMultiplier, biomes[neighborBiomeIndex].meshHeightCurve).vertices;
            }

            switch (i) {
                case 0: // up
                    for (int y = 0; y < biomeBlend; y++) {
                        for (int x = 0; x < mapChunkSize; x++) {
                            float a = currentChunk.meshData.vertices[(x * mapChunkSize) + mapChunkSize - 1 - y].y;
                            float b = neighborVertices[(x * mapChunkSize) + y].y;
                            float t = 0.5f - ((float)y / ((biomeBlend - 1) * 2));
                            float correctionValue = Mathf.Lerp(a, b, t) - currentChunk.meshData.vertices[(x * mapChunkSize) + mapChunkSize - 1 - y].y;
                            currentChunk.meshData.vertices[(x * mapChunkSize) + mapChunkSize - 1 - y].y += correctionValue;
                        }
                    }
                    break;

                case 1: // right
                    for (int x = 0; x < biomeBlend; x++) {
                        for (int y = 0; y < mapChunkSize; y++) {
                            float a = currentChunk.meshData.vertices[((mapChunkSize - 1 - x) * mapChunkSize) + y].y;
                            float b = neighborVertices[(x * mapChunkSize) + y].y;
                            float t = 0.5f - ((float)x / ((biomeBlend - 1) * 2));
                            float correctionValue = Mathf.Lerp(a, b, t) - currentChunk.meshData.vertices[((mapChunkSize - 1 - x) * mapChunkSize) + y].y;
                            currentChunk.meshData.vertices[((mapChunkSize - 1 - x) * mapChunkSize) + y].y += correctionValue;
                        }
                    }
                    break;

                case 2: // down
                    for (int y = 0; y < biomeBlend; y++) {
                        for (int x = 0; x < mapChunkSize; x++) {
                            float a = currentChunk.meshData.vertices[(x * mapChunkSize) + y].y;
                            float b = neighborVertices[(x * mapChunkSize) + mapChunkSize - 1 - y].y;
                            float t = 0.5f - ((float)y / ((biomeBlend - 1) * 2));
                            float correctionValue = Mathf.Lerp(a, b, t) - currentChunk.meshData.vertices[(x * mapChunkSize) + y].y;
                            currentChunk.meshData.vertices[(x * mapChunkSize) + y].y += correctionValue;
                        }
                    }
                    break;

                case 3: // left
                    for (int x = 0; x < biomeBlend; x++) {
                        for (int y = 0; y < mapChunkSize; y++) {
                            float a = currentChunk.meshData.vertices[(x * mapChunkSize) + y].y;
                            float b = neighborVertices[((mapChunkSize - 1 - x) * mapChunkSize) + y].y;
                            float t = 0.5f - ((float)x / ((biomeBlend - 1) * 2));
                            float correctionValue = Mathf.Lerp(a, b, t) - currentChunk.meshData.vertices[(x * mapChunkSize) + y].y;
                            currentChunk.meshData.vertices[(x * mapChunkSize) + y].y += correctionValue;
                        }
                    }
                    break;
            }
        }
    }

    private void Update() {

        // checking for processed chunk data from different threads and use it to create chunk

        if (processedChunkData.Count > 0) {
            ChunkData chunkData;

            lock (processedChunkData) {
                chunkData = processedChunkData.Dequeue();
            }

            if (generatedChunks.TryGetValue(chunkData.chunkCoords, out TerrainChunk currentChunk)) {
                Destroy(currentChunk.meshObject);
                Resources.UnloadUnusedAssets();                 // otherwise there is a memory leak with unitys handling of meshes
                generatedChunks.Remove(chunkData.chunkCoords);
            }

            GameObject meshObject = chunkData.CreateChunk(mapMaterial);
            meshObject.transform.parent = transform;
            generatedChunks.Add(chunkData.chunkCoords, new TerrainChunk(meshObject, chunkData.levelOfDetail, chunkData.biomeIndex));
        }
    }

    private void Awake() {
        if (MapGenerator.Instance != null && MapGenerator.Instance != this) {
            Destroy(gameObject);
        }

        MapGenerator.Instance = this;
        processedChunkData = new Queue<ChunkData>();
        generatedChunks = new Dictionary<Vector2, TerrainChunk>();

        // normalizing biome commonness to a range between 0 and 1
        float biomeSum = 0;
        for (int i = 0; i < biomes.Length; i++) if (biomes[i] != null) biomeSum += biomes[i].commonness;
        for (int i = 0; i < biomes.Length; i++) if (biomes[i] != null) biomes[i].commonness /= biomeSum;
    }


#if UNITY_EDITOR
    public void GenerateMapInEditor() {
        if (biomes.Length == 0) return;
        foreach (BiomeData biome in biomes) if (biome == null) return;

        ChunkData chunkData = GenerateMapChunk(offset / mapChunkSize, levelOfDetail);
        MapDisplay display = FindObjectOfType<MapDisplay>();

        switch (drawMode) {
            case DrawMode.HeightMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(chunkData.heightMap));
                break;
            case DrawMode.ColorMap:
                display.DrawTexture(TextureGenerator.TextureFromColorMap(chunkData.colorMap, mapChunkSize, mapChunkSize));
                break;
            case DrawMode.BiomeMap:
                int chunkOffsetX = (int)offset.x / mapChunkSize, chunkOffsetY = (int)offset.y / mapChunkSize;

                int[,] biomeMap = new int[20, 20];
                for (int x = 0; x < biomeMap.GetLength(0); x++) {
                    for (int y = 0; y < biomeMap.GetLength(1); y++) {
                        biomeMap[x, y] = Noise.GenerateBiomeIndex(new Vector2(x + chunkOffsetX, y + chunkOffsetY), ref biomes, seed, noiseScale);
                    }
                }

                display.DrawTexture(TextureGenerator.TextureFromBiomeMap(biomeMap, biomes.Length, 20));
                break;
            case DrawMode.Mesh:
                display.DrawMesh(chunkData.meshData, TextureGenerator.TextureFromColorMap(chunkData.colorMap, mapChunkSize, mapChunkSize));
                break;
        }
    }

    private void OnValidate() {
        if (noiseScale < 1.5f) noiseScale = 1.5f;
        if (lacunarity < 1) lacunarity = 1;
    }

#endif

}
