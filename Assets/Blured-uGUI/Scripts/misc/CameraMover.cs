using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Example.uGUI
{
    public class CameraMover : MonoBehaviour
    {
        [SerializeField]
        private Transform _target = null;

        [SerializeField]
        private float _distance = 3f;

        [SerializeField]
        private float _speed = 0.5f;

        private void Update()
        {
            float t = Time.time * _speed;
            float x = Mathf.Sin(t) * _distance;
            float y = Mathf.Sin(t) * Mathf.Sin(t) * _distance + 1f;
            float z = Mathf.Cos(t) * _distance;

            transform.position = new Vector3(x, y, z);

            transform.LookAt(_target);
        }
    }
}
