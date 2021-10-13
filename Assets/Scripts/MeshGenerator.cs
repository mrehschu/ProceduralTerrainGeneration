using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator {

    // static class to create a 3D mesh from a height map
    
    public static MeshData GenerateTerrainMesh(float[,] heightMap, int levelOfDetail, float heightMultiplier, AnimationCurve heightCurve) {
        int size = heightMap.GetLength(0);

        AnimationCurve heightCurveCopy;
        lock (heightCurve) {
            heightCurveCopy = new AnimationCurve(heightCurve.keys);
        }

        int nextRow = size / levelOfDetail;
        if (size % levelOfDetail != 0) nextRow++;

        MeshData meshData = new MeshData(size, size);
        for (int x = 0; x < size; x += levelOfDetail) {
            for (int y = 0; y < size; y += levelOfDetail) {

                int vertexIndex = meshData.AddVertex(new Vector3(x, heightCurveCopy.Evaluate(heightMap[x, y]) * heightMultiplier, y), new Vector2(x / (float)size, y / (float)size));

                if (x < size-levelOfDetail && y < size-levelOfDetail) {
                    meshData.AddTriangle(vertexIndex, vertexIndex + nextRow + 1, vertexIndex + nextRow);
                    meshData.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + nextRow + 1);
                }
            }
        }

        return meshData;
    }

}

public class MeshData {
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    int vertexIndex;
    int triangleIndex;

    public MeshData(int width, int height) {
        vertices = new Vector3[width * height];
        triangles = new int[(width - 1) * (height - 1) * 6];
        uvs = new Vector2[width * height];
    }

    public int AddVertex(Vector3 vertex, Vector2 uv) {
        if (vertexIndex >= vertices.Length) return -1;
        vertices[vertexIndex] = vertex;
        uvs[vertexIndex] = uv;
        return vertexIndex++;
    }

    public void AddTriangle(int a, int b, int c) {
        if (triangleIndex >= triangles.Length) return;

        triangles[triangleIndex++] = a;
        triangles[triangleIndex++] = b;
        triangles[triangleIndex++] = c;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        return mesh;
    }
}
