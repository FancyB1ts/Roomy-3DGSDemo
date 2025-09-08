using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField]
    private Button _newButton, _saveButton;

    [SerializeField]
    private SaveManager _saveManager;

    [SerializeField]
    private CameraMovement _cameraMovement;

    private void Start()
    {
        _saveButton.interactable = _saveManager.IsDataAvailable();
        _cameraMovement.gameObject.SetActive(false);
        _newButton.onClick.AddListener(ResetUI);
        if (_saveButton.interactable)
        {
            _saveButton.onClick.AddListener(HandleLoadGame);
        }
    }

    private void HandleLoadGame()
    {
        _saveManager.LoadData();
        ResetUI();
    }

    private void ResetUI()
    {
        _cameraMovement.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }
}

