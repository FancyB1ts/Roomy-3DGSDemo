using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Uses PlayerPrefs to store and retrieve save data for our house
/// </summary>
public class SaveManager : MonoBehaviour
{
    //We need to reuse PlacementManager to place objects
    [SerializeField]
    private PlacementManager _placementManager;
    //We need to retrieve ItemData using the database
    [SerializeField]
    private ItemDataBaseSO _database;

    //Arbitrary key to save and load the data from PlayerPrefs
    string _saveDataKey = "saveData";

    //For UI to enable / disable 'Load' button
    public bool IsDataAvailable() => PlayerPrefs.HasKey(_saveDataKey);

    public void SaveData()
    {
        GridSaveData data = _placementManager.GetDataToSave();
        string dataToSave = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(_saveDataKey, dataToSave);
        Debug.Log(dataToSave);
        Debug.Log("Data saved");
    }

    /// <summary>
    /// Loads all the data that was saved
    /// </summary>
    public void LoadData()
    {
        string data = PlayerPrefs.GetString(_saveDataKey);
        GridSaveData loadedData = JsonUtility.FromJson<GridSaveData>(data);

        ///Order is important! We cand place inWall objects 
        ///without having the walls first
        ProcessPlacementData(loadedData.floorData);
        ProcessPlacementData(loadedData.wallData);
        ProcessPlacementData(loadedData.objectData);
        ProcessPlacementData(loadedData.inWallData);
        Debug.Log("Data LOADED");
    }

    /// <summary>
    /// Recreates Cell and Edge objects for a specified loaded PlacementGrid data
    /// </summary>
    /// <param name="placementData"></param>
    private void ProcessPlacementData(PlacementGridSaveData placementData)
    {
        //object data
        for (int i = 0; i < placementData.cellsData.Count; i++)
        {
            CellObjectSaveData objectData = placementData.cellObjectData[i];
            List<Quaternion> gridCheckRotation = new() { Quaternion.Euler(0, objectData.rotation, 0) };
            List<Quaternion> objectRotation = new() { Quaternion.Euler(0, objectData.objectRotation, 0) };
            var (selectionData, data)
                = CreateData(objectData.structureID,
                    new (),
                    new () { objectData.origin },
                    objectRotation,
                    gridCheckRotation
                );
            _placementManager.PlaceStructureAt(selectionData, data);
        }
        for (int i = 0; i < placementData.edgesData.smallerPoints.Count; i++)
        {
            EdgeObjectSaveData objectData = placementData.edgesObjectData[i];
            List<Quaternion> gridCheckRotation = new() { Quaternion.Euler(0, objectData.rotation, 0) };
            List<Quaternion> objectRotation = new() { Quaternion.Euler(0, objectData.objectRotation, 0) };
            var (selectionData, data)
                = CreateData(objectData.structureID,
                    new(),
                    new() { objectData.origin },
                    objectRotation,
                    gridCheckRotation
                );
            _placementManager.PlaceStructureAt(selectionData, data);
        }
    }

    /// <summary>
    /// Creates the SelectionResult and ItemData so we can place the objects on the map
    /// </summary>
    /// <param name="id"> Objects ID</param>
    /// <param name="loadedSelectedPositions"> Placement Positions (we need to calculate them later)</param>
    /// <param name="loadedSelectedGridPositions"> Grid Positions loaded from our SaveData</param>
    /// <param name="loadedSelectedPositionsObjectRotation">Object rotation loaded from our SaveData</param>
    /// <param name="loadedSelectedPositionGridCheckRotation">GridCheck rotation loaded from our SaveData</param>
    /// <returns></returns>
    /// <exception cref="System.Exception"></exception>
    private (SelectionResult, ItemData) CreateData(int id, List<Vector3> loadedSelectedPositions, List<Vector3Int> loadedSelectedGridPositions, List<Quaternion> loadedSelectedPositionsObjectRotation, List<Quaternion> loadedSelectedPositionGridCheckRotation)
    {
        ItemData data;
        SelectionResult selectionData;
        _placementManager.StartPlacingObject(id);
        data = _database.GetItemWithID(id);
        if (data == null)
            throw new System.Exception("No Structure data with id " + id);
        selectionData = new()
        {
            isEdgeStructure = data.objectPlacementType.IsEdgePlacement(),
            placementValidity = true,
            size = data.size,
            selectedGridPositions = loadedSelectedGridPositions,
            selectedPositions = loadedSelectedPositions,
            selectedPositionsObjectRotation = loadedSelectedPositionsObjectRotation,
            selectedPositionGridCheckRotation = loadedSelectedPositionGridCheckRotation,
        };
        return (selectionData, data);
    }
}
