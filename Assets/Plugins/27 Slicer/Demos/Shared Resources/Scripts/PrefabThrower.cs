// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using UnityEngine;
using UnityEngine.EventSystems;

namespace Slicer.Demo
{
    /// <summary>
    /// A simple script that can be used to throw a prefab, normally a ball.
    /// </summary>
    [AddComponentMenu("Slicer/Demo/Prefab Thrower")]
    public class PrefabThrower : MonoBehaviour
    {
        /// <summary>
        /// The prefab to throw
        /// </summary>
        public GameObject PrefabToThrow;
        private Camera mainCamera;

        /// <summary>
        /// The speed to throw the prefab at
        /// </summary>
        public float Speed = 10f;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

                var pos = ray.origin + (ray.direction * 0.25f);
                var objectThrown = Instantiate(PrefabToThrow, pos, Quaternion.identity, transform);

                var rigidBody = objectThrown.GetComponent<Rigidbody>();

                rigidBody.linearVelocity = ray.direction * (Speed);

                Destroy(objectThrown, 10f);
            }
        }
    }
}