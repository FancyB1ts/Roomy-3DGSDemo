using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Data saved when we place an Edge object on our grid
/// </summary>
public class PlacedEdgeObjectData
{
    public IEnumerable<Edge> PositionsOccupied { get; private set; }
    public int gameObjectIndex, structureID;
    public Vector3Int origin;
    public int rotation;
    public int objectRotation;
    public PlacedEdgeObjectData(int gameObjectIndex, int structureID, IEnumerable<Edge> positionsOccupied, Vector3Int origin, int rotation, int objectRotation)
    {
        this.gameObjectIndex = gameObjectIndex;
        this.PositionsOccupied = positionsOccupied;
        this.structureID = structureID;
        this.origin = origin;
        this.rotation = rotation;
        this.objectRotation = objectRotation;
    }

    /// <summary>
    /// Returns us data to save for this Edge object
    /// </summary>
    /// <returns></returns>
    public EdgeObjectSaveData GetSaveData()
    {
        EdgeObjectSaveData data = new()
        {
            edges = EdgesData.GetEdgesData(PositionsOccupied),
            gameObjectIndex = gameObjectIndex,
            structureID = structureID,
            origin = origin,
            rotation = rotation,
            objectRotation = objectRotation
        };

        return data;
    }

}

/// <summary>
/// Save data definition for edge object
/// </summary>
[Serializable]
public struct EdgeObjectSaveData
{
    public EdgesData edges;
    public int gameObjectIndex, structureID;
    public Vector3Int origin;
    public int rotation;
    public int objectRotation;

}

[Serializable]
public struct EdgesData
{
    public List<Vector3Int> smallerPoints, biggerPoints;

    public static EdgesData GetEdgesData(IEnumerable<Edge> edges)
    {
        List<Vector3Int> smallerPoints = new(), biggerPoints = new();
        foreach (var item in edges)
        {
            smallerPoints.Add(item.smallerPoint);
            biggerPoints.Add(item.biggerPoint);
        }
        return new EdgesData
        {
            smallerPoints = smallerPoints,
            biggerPoints = biggerPoints
        };
    }
}