using System;
using Cinemachine;
using UnityEngine;

namespace Disc0ver
{
    [DefaultExecutionOrder(99)]
    public class CameraController : MonoBehaviour
    {
        public float minAngle = -90f;
        public float maxAngle = 90f;
        public float cameraRotateSpeed = 5f;
        public Camera camera;
        
        public Transform lookAtTf;

        public static CameraController Instance { get; private set; }
        private CharacterController _characterController;
        private Vector3 v = Vector3.zero;


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

        public void Tick()
        {
            var x = Input.GetAxisRaw("Mouse X");
            var y = Input.GetAxisRaw("Mouse Y");
            
            var eulerAngles = lookAtTf.transform.eulerAngles;
            eulerAngles += Vector3.up * x * cameraRotateSpeed;
            var tmpEulerAngles = eulerAngles;
            eulerAngles += Vector3.left * y * cameraRotateSpeed;
            
            float xRotation = eulerAngles.x;
            if (xRotation > 180)
                xRotation -= 360;
            if (xRotation < minAngle || xRotation > maxAngle)
            {
                lookAtTf.transform.eulerAngles = tmpEulerAngles;
            }
            else
            {
                lookAtTf.transform.eulerAngles = eulerAngles;
            }

            // lookAtTf.transform.eulerAngles = camera.transform.eulerAngles;
            // lookAtTf.position = _characterController.transform.position;
            lookAtTf.position = Vector3.SmoothDamp(lookAtTf.position, _characterController.transform.position, ref v, 0.3f);
        }

        public Vector3 Forward()
        {
            return Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
        }
    }
}