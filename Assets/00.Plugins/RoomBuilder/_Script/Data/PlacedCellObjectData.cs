using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Data saved when we place a Cell object on our grid
/// </summary>
public class PlacedCellObjectData
{
    public IEnumerable<Vector3Int> PositionsOccupied { get; private set; }
    public int gameObjectIndex, structureID;
    public Vector3Int origin;
    public int rotation, objectRotation;

    public PlacedCellObjectData(int gameObjectIndex,int structureID, IEnumerable<Vector3Int> positionsOccupied, Vector3Int origin, int rotation, int objectRotation)
    {
        this.gameObjectIndex = gameObjectIndex;
        this.PositionsOccupied = positionsOccupied;
        this.structureID = structureID;
        this.origin = origin;
        this.rotation = rotation;
        this.objectRotation = objectRotation;
    }

    /// <summary>
    /// Generates save data for this cell object
    /// </summary>
    /// <returns></returns>
    public CellObjectSaveData GetSaveData()
    {
        CellObjectSaveData data = new()
        {
            positions = new(PositionsOccupied),
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
/// Save data struct for Cell objects
/// </summary>
[Serializable]
public struct CellObjectSaveData
{
    public List<Vector3Int> positions;
    public int gameObjectIndex, structureID;
    public Vector3Int origin;
    public int rotation;
    public int objectRotation;
}