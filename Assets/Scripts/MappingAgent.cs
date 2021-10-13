using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// put this script on any player or camera that needs to render terrain around themself
public class MappingAgent : MonoBehaviour {

    public int maxViewDistance = 5;
    public float updateThreshold = 100;
    [SerializeField] AnimationCurve lodCurve;

    Vector2 lastMappedPosition;
    float sqrUpdateThreshold;
    int chunkSize;
    

    private void Start() {
        lastMappedPosition = new Vector2(transform.position.x, transform.position.z);
        sqrUpdateThreshold = updateThreshold * updateThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        UpdateVisibleChunks();
    }

    private void Update() {
        Vector2 currentPosition = new Vector2(transform.position.x, transform.position.z);

        if ((lastMappedPosition - currentPosition).sqrMagnitude > sqrUpdateThreshold) {
            lastMappedPosition = currentPosition;
            UpdateVisibleChunks();
        }
        
        Debug.DrawRay(transform.position, Vector3.down * transform.position.y, Color.blue);
    }

    private void UpdateVisibleChunks() {
        Debug.Log("Updated Chunks");
        Vector2 chunkCoordsCenter = new Vector2((int)(transform.position.x / chunkSize), (int)(transform.position.z / chunkSize));
        if (transform.position.x < 0) chunkCoordsCenter.x--;
        if (transform.position.z < 0) chunkCoordsCenter.y--;

        for (int xOffset = -maxViewDistance; xOffset <= maxViewDistance; xOffset++) {
            for (int yOffset = -maxViewDistance; yOffset <= maxViewDistance; yOffset++) {
                Vector2 currentChunkCoords = new Vector2(chunkCoordsCenter.x + xOffset, chunkCoordsCenter.y + yOffset);

                int levelOfDetail = EvaluateLOD(currentChunkCoords);
                MapGenerator.Instance.RequestMapChunk(currentChunkCoords, levelOfDetail);
            }
        }
    }

    private int EvaluateLOD(Vector2 chunkCoords) {
        float sqrDistance = (new Vector2(transform.position.x, transform.position.z) - (chunkCoords * chunkSize)).sqrMagnitude;
        sqrDistance /= maxViewDistance * chunkSize * maxViewDistance * chunkSize;
        int lodValue = (int)(lodCurve.Evaluate((sqrDistance >= 1) ? 1 : sqrDistance) * 6);
        lodValue = (++lodValue == 7) ? -1 : lodValue;
        Debug.Log(chunkCoords + " : " + sqrDistance + " -> " + lodValue);
        return lodValue;
    }
}
