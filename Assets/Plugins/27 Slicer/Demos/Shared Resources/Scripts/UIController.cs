// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Slicer.Demo
{
    /// <summary>
    /// The controller for the Demos UI.
    /// </summary>
    [AddComponentMenu("Slicer/Demo/UI Controller")]
    public class UIController : MonoBehaviour
    {
        public Text DecriptionText;
        private string descriptionString;

        public Text ToggleSlicingButtonText;
        private string toggleSlicingString;

        private bool toggleSlicingState = true;

        public Text ToggleSlicingHintText;
        private string toggleSlicingHintString;

        private List<TrackedPhysicsObject> trackedPhysicsObjects;

        public Button NextViewButton;
        private int currentCameraPosition = 0;
        public List<CameraPositions> CameraPositionList;

        private Camera mainCamera;
        private const float scrollScale = 0.5f;

        // Start is called before the first frame update
        private void Awake()
        {
            mainCamera = Camera.main;

            toggleSlicingString = ToggleSlicingButtonText.text;

            if (ToggleSlicingHintText != null)
            {
                toggleSlicingHintString = ToggleSlicingHintText.text;
            }

            NextViewButton.gameObject.SetActive(CameraPositionList.Any());

            descriptionString = DecriptionText.text;
            SetDescription(CameraPositionList.FirstOrDefault());

            trackedPhysicsObjects = FindObjectsOfType<Rigidbody>()
                .Select(e => {
                    return new TrackedPhysicsObject
                    {
                        Rigidbody = e,
                        Position = e.position,
                        Rotation = e.rotation
                    };
                }).ToList();

            SetSlicingString();
        }

        private void Update()
        {
            var scroll = Input.mouseScrollDelta.y;
            var transform = mainCamera.transform;

            var cameraPos = transform.localPosition;
            var translation = new Vector3(0, 0, scroll * scrollScale);

            transform.localPosition = cameraPos + (transform.localRotation * translation);
        }

        private void SetDescription(CameraPositions cameraPosition)
        {
            if (cameraPosition != null)
            {
                DecriptionText.text = $"{descriptionString}\n\n{cameraPosition.Text}";
            }
            else
            {
                DecriptionText.text = descriptionString;
            }

            HighlightText(DecriptionText);

            // Force refresh the vertical layout group
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }

        public void NextViewButton_Click()
        {
            StartCoroutine(NextView());
        }

        public void ToggleSlicingButton_Click()
        {
            ToggleSlicing();
        }

        public void PrevSceneButton_Click(string sceneToload)
        {
            LoadSceneByName(sceneToload);
        }

        public void NextSceneButton_Click(string sceneToload)
        {
            LoadSceneByName(sceneToload);
        }

        private IEnumerator NextView()
        {
            NextViewButton.enabled = false;
            currentCameraPosition = (currentCameraPosition + 1) % CameraPositionList.Count;
            var cameraTransform = mainCamera.transform;
            var cameraPosition = CameraPositionList[currentCameraPosition];
            var endPositionTransform = cameraPosition.Transform;

            var startPosition = cameraTransform.position;
            var startRotation = cameraTransform.rotation;

            SetDescription(cameraPosition);

            float elapsed = 0;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var elapsedFraction = elapsed / duration;

                cameraTransform.position = Vector3.Lerp(startPosition, endPositionTransform.position, elapsedFraction);
                cameraTransform.rotation = Quaternion.Lerp(startRotation, endPositionTransform.rotation, elapsedFraction);

                yield return null;
            }

            cameraTransform.position = endPositionTransform.position;
            cameraTransform.rotation = endPositionTransform.rotation;


            NextViewButton.enabled = true;
        }

        private void ToggleSlicing()
        {
            toggleSlicingState = !toggleSlicingState;

            var allSlicers = FindObjectsOfType<SlicerController>();
            foreach (var slicer in allSlicers)
            {
                if (toggleSlicingState)
                {
                    slicer.EnableSlicing();
                }
                else
                {
                    slicer.DisableSlicing();
                }

                slicer.RefreshSlice();
            }

            SetSlicingString();

            ResetPhysicsObjects();
        }

        private void HighlightText(Text text)
        {
            var s = text.text;

            var classes = AppDomain.CurrentDomain.GetAssemblies()
                .First(e => e.FullName.StartsWith("27Slicer,"))
                .GetTypes();

            foreach (var c in classes)
            {
                s = Regex.Replace(s, $@"({c.Name}s?)", m =>
                {
                    var name = Regex.Replace(m.Groups[1].Value, @"(\S)([A-Z])", m2 => $"{m2.Groups[1].Value} {m2.Groups[2].Value}");
                    return $"<color=navy>{name}</color>";
                });
            }

            text.text = s;
        }

        private void SetSlicingString()
        {
            string stateText;
            string hintText;
            if (toggleSlicingState)
            {
                stateText = "Disable";
                hintText = "When the slicer is enabled you can see the effects the slicer has on Game Objects.";
            }
            else
            {
                stateText = "Enable";
                hintText = "When the slicer is disabled you can see the Game Objects in their unsliced state.";
            }

            ToggleSlicingButtonText.text = string.Format(toggleSlicingString, stateText);

            if (ToggleSlicingHintText)
            {
                ToggleSlicingHintText.text = string.Format(toggleSlicingHintString, ToggleSlicingButtonText.text, hintText);
            }
        }

        private void LoadSceneByName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError($"{nameof(sceneName)} not supplied");
                return;
            }

            var loadSceneParameters = new LoadSceneParameters(LoadSceneMode.Single);

#if UNITY_EDITOR
            // Do this the long way to ensure that the correct scene is loaded
            var foundAssetsGuids = UnityEditor.AssetDatabase.FindAssets($"t:scene {sceneName}");
            var selectedAssetPath = foundAssetsGuids
                .Select(e => UnityEditor.AssetDatabase.GUIDToAssetPath(e))
                .FirstOrDefault(e => e.Contains("/27 Slicer/Demos/"));

            if (string.IsNullOrEmpty(selectedAssetPath))
            {
                Debug.LogError($"Could not find scene '{sceneName}'");
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(selectedAssetPath, loadSceneParameters);
#else
            SceneManager.LoadScene(sceneName, loadSceneParameters);
#endif
        }
        private void ResetPhysicsObjects()
        {
            foreach (var trackedPhysicsObject in trackedPhysicsObjects)
            {
                if (trackedPhysicsObject.Rigidbody == null)
                {
                    continue;
                }

                trackedPhysicsObject.Reset();
            }
        }

        private class TrackedPhysicsObject
        {
            public Rigidbody Rigidbody;
            public Vector3 Position;
            public Quaternion Rotation;

            public void Reset()
            {
                Rigidbody.linearVelocity = Vector3.zero;
                Rigidbody.angularVelocity = Vector3.zero;

                Rigidbody.position = Position;
                Rigidbody.rotation = Rotation;
            }
        }

        [Serializable]
        public class CameraPositions
        {
            public Transform Transform;
            [TextArea(2, 10)]
            public string Text;
        }
    }
}