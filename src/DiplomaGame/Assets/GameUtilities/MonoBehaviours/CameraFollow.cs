using System;
using UnityEngine;

namespace GameUtilities.MonoBehaviours
{

    /*
     * Script to handle Camera Movement and Zoom
     * Place on Camera GameObject
     * */
    public class CameraFollow : MonoBehaviour {

        public static CameraFollow Instance { get; private set; }

        [SerializeField] private float distanceCof = 1.001f;
        [SerializeField] private float cameraMoveSpeed = 20f;
        [SerializeField] private float cameraZoomSpeed = 10f;
        
        private static Camera myCamera;
        private Func<Vector3> GetCameraFollowPositionFunc;
        private Func<float> GetCameraZoomFunc;

        public event Action<CameraMoveEventArgs> OnCameraMoved;
        private Vector3 _lastPosition;
        private float _moveThreshold = 10f;

        public void Setup(Func<Vector3> GetCameraFollowPositionFunc, Func<float> GetCameraZoomFunc, bool teleportToFollowPosition, bool instantZoom) {
            this.GetCameraFollowPositionFunc = GetCameraFollowPositionFunc;
            this.GetCameraZoomFunc = GetCameraZoomFunc;

            if (teleportToFollowPosition) {
                Vector3 cameraFollowPosition = GetCameraFollowPositionFunc();
                cameraFollowPosition.z = transform.position.z;
                transform.position = cameraFollowPosition;
            }

            if (instantZoom) {
                myCamera.orthographicSize = GetCameraZoomFunc();
            }

        }

        private void Awake() {
            Instance = this;
            myCamera = transform.GetComponent<Camera>();
        }

        public void SetCameraFollowPosition(Vector3 cameraFollowPosition) {
            SetGetCameraFollowPositionFunc(() => cameraFollowPosition);
        }

        public void SetGetCameraFollowPositionFunc(Func<Vector3> GetCameraFollowPositionFunc) {
            this.GetCameraFollowPositionFunc = GetCameraFollowPositionFunc;
        }

        public void SetCameraZoom(float cameraZoom) {
            SetGetCameraZoomFunc(() => cameraZoom);
        }

        public void SetGetCameraZoomFunc(Func<float> GetCameraZoomFunc) {
            this.GetCameraZoomFunc = GetCameraZoomFunc;
        }

        
        private void Update() {
            var moveDelta = Vector3.Distance(transform.position, _lastPosition);
            if (moveDelta > _moveThreshold)
            {
                OnCameraMoved?.Invoke(new CameraMoveEventArgs(_lastPosition, transform.position, myCamera));
                _lastPosition = transform.position;
            }

            HandleMovement();
            HandleZoom();
        }

        private void HandleMovement() {
            if (GetCameraFollowPositionFunc == null) return;
            Vector3 cameraFollowPosition = GetCameraFollowPositionFunc();
            cameraFollowPosition.z = transform.position.z;

            Vector3 cameraMoveDir = (cameraFollowPosition - transform.position).normalized;
            float distance = Vector3.Distance(cameraFollowPosition, transform.position) / distanceCof;

            if (distance > 0) {
                Vector3 newCameraPosition = transform.position + cameraMoveDir * distance * cameraMoveSpeed * Time.deltaTime;

                float distanceAfterMoving = Vector3.Distance(newCameraPosition, cameraFollowPosition);

                if (distanceAfterMoving > distance) {
                    // Overshot the target
                    newCameraPosition = cameraFollowPosition;
                }

                transform.position = newCameraPosition;
            }
        }

        private void HandleZoom() {
            if (GetCameraZoomFunc == null) return;
            float cameraZoom = GetCameraZoomFunc();

            float cameraZoomDifference = cameraZoom - myCamera.orthographicSize;

            myCamera.orthographicSize += cameraZoomDifference * cameraZoomSpeed * Time.deltaTime;

            if (cameraZoomDifference > 0) {
                if (myCamera.orthographicSize > cameraZoom) {
                    myCamera.orthographicSize = cameraZoom;
                }
            } else {
                if (myCamera.orthographicSize < cameraZoom) {
                    myCamera.orthographicSize = cameraZoom;
                }
            }
        }

        public static Vector2 GetWorldPosition(Vector2 coords)
        {
            return myCamera.ScreenToWorldPoint(coords);
        }
    }

    public class CameraMoveEventArgs : EventArgs
    {
        public Vector3 OldPosition { get; private set; }
        public Vector3 NewPosition { get; private set; }
        public float ViewportWidth { get; private set; }
        public float ViewportHeight { get; private set; }
        public Vector3 BottomLeft { get; private set; }
        public Vector3 TopRight { get; private set; }

        public CameraMoveEventArgs(Vector3 oldPosition, Vector3 newPosition, Camera camera)
        {
            OldPosition = oldPosition;
            NewPosition = newPosition;

            BottomLeft = camera.ViewportToWorldPoint(new Vector3(0, 0, 0));
            TopRight = camera.ViewportToWorldPoint(new Vector3(1, 1, 0));
            ViewportWidth = Mathf.Abs(TopRight.x - BottomLeft.x);
            ViewportHeight = Mathf.Abs(TopRight.y - BottomLeft.y);
        }
    }
}