using GameUtilities.MonoBehaviours;
using System;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private CameraFollow cameraFollow;

    [SerializeField] private float _edgeMoveSpeed = 10f;
    [SerializeField] private float _edgeSize = 20f;

    [SerializeField] private float _middleMouseSpeed = 10f;

    [SerializeField] private float _minZoom = 5f;
    [SerializeField] private float _maxZoom = 50f;
    [SerializeField] private float _stepZoom = 5f;
    [SerializeField] private float _currentZoom = 5f;
    [SerializeField] private float _zoomSpeed = 200f;

    public static float _mapHeight = 100f;
    public static float _mapWidth = 100f;
    public static Vector3 _mapStart = new(-50, -50);

    private Vector3 _targetPosition;
    private bool _isMiddleMousePressed;
    private Vector3 _lastMousePosition;

    void Start()
    {
        if (cameraFollow == null)
        {
            cameraFollow = CameraFollow.Instance;
        }

        _targetPosition = cameraFollow.transform.position;
        cameraFollow.SetCameraZoom(_currentZoom);
    }

    void Update()
    {
        HandleEdgeMovement();
        HandleMiddleMouseMovement();
        HandleZoom();
        cameraFollow.SetCameraFollowPosition(_targetPosition);
        cameraFollow.SetCameraZoom(_currentZoom);
    }

    private void HandleEdgeMovement()
    {
        if (!_isMiddleMousePressed)
        {
            var screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
            var mousePositionRelativeToCenter = Input.mousePosition - screenCenter;
            var moveDirection = mousePositionRelativeToCenter.normalized;

            if (Input.mousePosition.x >= Screen.width - _edgeSize ||
                Input.mousePosition.x <= _edgeSize ||
                Input.mousePosition.y >= Screen.height - _edgeSize ||
                Input.mousePosition.y <= _edgeSize)
            {
                _targetPosition += moveDirection * _edgeMoveSpeed * Time.deltaTime * _currentZoom / _minZoom;
                NormalizeCameraMapPosition();
            }

        }
    }

    private void HandleMiddleMouseMovement()
    {
        if (Input.GetMouseButtonDown(2))
        {
            _isMiddleMousePressed = true;
            _lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(2))
        {
            _isMiddleMousePressed = false;
        }

        if (_isMiddleMousePressed)
        {
            var currentMousePosition = Input.mousePosition;
            var mouseDelta = currentMousePosition - _lastMousePosition;

            var moveDirection = new Vector3(mouseDelta.x, mouseDelta.y, 0).normalized;
            var distance = Vector3.Distance(currentMousePosition, _lastMousePosition) / _middleMouseSpeed / 2;


            var toAddPosition = moveDirection * _middleMouseSpeed * Time.deltaTime * distance;

            _targetPosition += toAddPosition;

            NormalizeCameraMapPosition();
        }
    }

    private void HandleZoom()
    {
        var zoomChange = -Input.GetAxis("Mouse ScrollWheel") * _stepZoom;
        var newZoom = Mathf.Clamp(_currentZoom + zoomChange, _minZoom, _maxZoom);

        if(CheckCameraMapBordersCollision(newZoom))
        {
            return;
        }

        if (Math.Abs(_currentZoom - newZoom) > 10e-15 && Math.Abs(newZoom - _maxZoom) > 10e-15 && Math.Abs(newZoom - _minZoom) > 10e-15)
        {
            var screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
            var mousePositionRelativeToCenter = Input.mousePosition - screenCenter;
            if (mousePositionRelativeToCenter.magnitude >= 10f)
            {
                var moveDirection = mousePositionRelativeToCenter.normalized;

                if (zoomChange > 0)
                {
                    _targetPosition += -moveDirection * _zoomSpeed * Time.deltaTime;

                    NormalizeCameraMapPosition();
                }
                else if (zoomChange < 0)
                {
                    _targetPosition += _zoomSpeed * Time.deltaTime * moveDirection;

                    NormalizeCameraMapPosition();
                }
            }
        }

        _currentZoom = newZoom;
    }

    private void NormalizeCameraMapPosition()
    {
        var aspectRatio = Screen.width / (float)Screen.height;
        var cameraWidth = _currentZoom * 2 * aspectRatio;
        var cameraHeight = _currentZoom * 2;

        var cameraTopLeft = _targetPosition + new Vector3(-cameraWidth / 2, cameraHeight / 2, 0);
        var cameraBottomRight = _targetPosition + new Vector3(cameraWidth / 2, -cameraHeight / 2, 0);
        
        var _mapStart = CameraManager._mapStart;
        var _mapHeight = CameraManager._mapHeight;
        var _mapWidth = CameraManager._mapWidth;

        if (cameraTopLeft.x < _mapStart.x)
        {
            _targetPosition.x += (_mapStart.x - cameraTopLeft.x);
        }
        if (cameraTopLeft.y > _mapStart.y + _mapHeight)
        {
            _targetPosition.y -= (cameraTopLeft.y - (_mapStart.y + _mapHeight));
        }
        if (cameraBottomRight.x > _mapStart.x + _mapWidth)
        {
            _targetPosition.x -= (cameraBottomRight.x - (_mapStart.x + _mapWidth));
        }
        if (cameraBottomRight.y < _mapStart.y)
        {
            _targetPosition.y += (_mapStart.y - cameraBottomRight.y);
        }
    }

    private bool CheckCameraMapBordersCollision(float currentZoom)
    {
        var aspectRatio = Screen.width / (float)Screen.height;
        var cameraWidth = currentZoom * 2 * aspectRatio;
        var cameraHeight = currentZoom * 2;

        var cameraTopLeft = _targetPosition + new Vector3(-cameraWidth / 2, cameraHeight / 2, 0);
        var cameraBottomRight = _targetPosition + new Vector3(cameraWidth / 2, -cameraHeight / 2, 0);

        var _mapStart = CameraManager._mapStart;
        var _mapHeight = CameraManager._mapHeight;
        var _mapWidth = CameraManager._mapWidth;

        return cameraTopLeft.x < _mapStart.x ||
               cameraTopLeft.y > _mapStart.y + _mapHeight ||
               cameraBottomRight.x > _mapStart.x + _mapWidth ||
               cameraBottomRight.y < _mapStart.y;
    }
}
