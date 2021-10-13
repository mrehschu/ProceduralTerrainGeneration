using UnityEngine;

// this file just contains a bunch of data structures used at different points

[CreateAssetMenu]
public class BiomeData : ScriptableObject {
    public string biomeName;
    [Tooltip("Note: This value will be converted to a range between 0 and 1 on start.")]
    public float commonness;
    public AltitudeRegion[] regions;
    public float meshHeightMultiplier = 1f;
    public AnimationCurve meshHeightCurve;


#if UNITY_EDITOR

    private void OnValidate() {
        FindObjectOfType<MapGenerator>()?.GenerateMapInEditor();
    }

#endif

}

[System.Serializable]
public struct AltitudeRegion {
    public string name;
    public float maxHeight;
    public Color color;
}

public struct TerrainChunk {
    public readonly GameObject meshObject;
    public readonly Vector3[] vertices;
    public readonly int levelOfDetail;
    public readonly int biomeIndex;

    public TerrainChunk(GameObject meshObject, int levelOfDetail, int biomeIndex) {
        this.meshObject = meshObject;
        this.vertices = meshObject.GetComponent<MeshFilter>().mesh.vertices;
        this.levelOfDetail = levelOfDetail;
        this.biomeIndex = biomeIndex;
    }
}

public class ChunkData {
    public readonly Vector2 chunkCoords;
    public readonly int levelOfDetail;
    public readonly int biomeIndex;
    public readonly Vector2 chunkLocation;
    public readonly float[,] heightMap;
    public readonly MeshData meshData;
    public readonly Color[] colorMap;

    public ChunkData(Vector2 chunkCoords, Vector2 chunkLocation, int levelOfDetail, int biomeIndex, float[,] heightMap, MeshData meshData, Color[] colorMap) {
        this.chunkCoords = chunkCoords;
        this.chunkLocation = chunkLocation;
        this.levelOfDetail = levelOfDetail;
        this.biomeIndex = biomeIndex;
        this.heightMap = heightMap;
        this.meshData = meshData;
        this.colorMap = colorMap;
    }

    public GameObject CreateChunk(Material mapMaterial) {

        GameObject meshObject = new GameObject("Terrain Chunk");
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter.mesh = meshData.CreateMesh();
        meshRenderer.material = mapMaterial;
        meshRenderer.material.mainTexture = TextureGenerator.TextureFromColorMap(colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
        meshObject.transform.position = new Vector3(chunkLocation.x, 0, chunkLocation.y);

        return meshObject;
    }
}