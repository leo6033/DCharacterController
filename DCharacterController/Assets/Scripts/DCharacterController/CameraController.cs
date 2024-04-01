using System;
using UnityEngine;

namespace Disc0ver
{
    public class CameraController : MonoBehaviour
    {
        public float minAngle = -90f;
        public float maxAngle = 90f;

        public Transform lookAtTf;

        public static CameraController Instance { get; private set; }
        private CharacterController _characterController;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            Cursor.lockState = CursorLockMode.Locked;
        }

        public void FollowCharacter(CharacterController characterController)
        {
            _characterController = characterController;
            lookAtTf.position = _characterController.transform.position;
        }

        private void Update()
        {
            var x = Input.GetAxisRaw("Mouse X");
            var y = Input.GetAxisRaw("Mouse Y");
            
            lookAtTf.Rotate(new Vector3(0, x, 0), Space.World);
            lookAtTf.Rotate(new Vector3(-y, 0, 0), Space.World);

            lookAtTf.position = _characterController.transform.position;
        }

        public Vector3 Forward()
        {
            return Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        }
    }
}