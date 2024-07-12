using GameUtilities.MonoBehaviours;
using System;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private CameraFollow cameraFollow;

    private float _edgeMoveSpeed;
    private float _edgeSize;

    private float _middleMouseSpeed;

    private float _minZoom;
    private float _maxZoom;
    private float _stepZoom;
    private float _currentZoom;
    private float _zoomSpeed;

    public static float _mapHeight;
    public static float _mapWidth;
    public static Vector3 _mapStart;

    private Vector3 _targetPosition;
    private bool _isMiddleMousePressed;
    private Vector3 _lastMousePosition;

    private bool _isCameraMovingToPosition = false;
    private Vector3 _positionToMove;

    void Start()
    {
        LoadAllVariables();

        if (cameraFollow == null)
        {
            cameraFollow = CameraFollow.Instance;
        }

        _targetPosition = cameraFollow.transform.position;
        cameraFollow.SetCameraZoom(_currentZoom);

        
    }

    void Update()
    {

        //For testing
        if (Input.GetKeyDown(KeyCode.T))
        {
            SetCameraPositionToMove(new Vector3(100, 100, 0));
        }
        //Debug

        if (_isCameraMovingToPosition)
        {
            MoveCameraToPosition();
            _isCameraMovingToPosition = false;
        }
        else
        {
            HandleEdgeMovement();
            HandleMiddleMouseMovement();
            HandleZoom();
        }

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

        if (CheckCameraMapBordersCollision(newZoom))
        {
            _targetPosition += CalculateVectorToMoveCameraFromBorder(newZoom);
            NormalizeCameraMapPosition();
            _currentZoom = newZoom;
        }
        else if (Math.Abs(_currentZoom - newZoom) > 10e-15 && Math.Abs(newZoom - _maxZoom) > 10e-15 && Math.Abs(newZoom - _minZoom) > 10e-15)
        {
            var screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
            var mousePositionRelativeToCenter = Input.mousePosition - screenCenter;
            if (mousePositionRelativeToCenter.magnitude >= 10f)
            {
                var moveDirection = mousePositionRelativeToCenter.normalized;

                if (zoomChange > 0)
                {
                    _targetPosition += _zoomSpeed * Time.deltaTime * -moveDirection;

                    NormalizeCameraMapPosition();
                }
                else if (zoomChange < 0)
                {
                    _targetPosition += _zoomSpeed * Time.deltaTime * moveDirection;

                    NormalizeCameraMapPosition();
                }
            }
            _currentZoom = newZoom;
        }

        
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

    private Vector3 CalculateVectorToMoveCameraFromBorder(float newZoom)
    {
        var aspectRatio = Screen.width / (float)Screen.height;
        var cameraWidth = newZoom * 2 * aspectRatio;
        var cameraHeight = newZoom * 2;

        var cameraTopLeft = _targetPosition + new Vector3(-cameraWidth / 2, cameraHeight / 2, 0);
        var cameraBottomRight = _targetPosition + new Vector3(cameraWidth / 2, -cameraHeight / 2, 0);

        var moveVector = new Vector3();

        if (cameraTopLeft.x < _mapStart.x)
        {
            moveVector.x = _mapStart.x - cameraTopLeft.x;
        }
        if (cameraTopLeft.y > _mapStart.y + _mapHeight)
        {
            moveVector.y = (_mapStart.y + _mapHeight) - cameraTopLeft.y;
        }
        if (cameraBottomRight.x > _mapStart.x + _mapWidth)
        {
            moveVector.x = (_mapStart.x + _mapWidth) - cameraBottomRight.x;
        }
        if (cameraBottomRight.y < _mapStart.y)
        {
            moveVector.y = _mapStart.y - cameraBottomRight.y;
        }

        return moveVector;
    }

    private void MoveCameraToPosition()
    {
        _targetPosition.x = _positionToMove.x;
        _targetPosition.y = _positionToMove.y;
        NormalizeCameraMapPosition();
    }

    public void SetCameraPositionToMove(Vector3 finalPosition)
    {
        _positionToMove = finalPosition;
        _isCameraMovingToPosition = true;
    }

    private void LoadAllVariables()
    {
        _edgeMoveSpeed = CameraConfig.EdgeMoveSpeed;
        _edgeSize = CameraConfig.EdgeSize;

        _middleMouseSpeed = CameraConfig.MiddleMouseSpeed;

        _minZoom = CameraConfig.MinZoom;
        _maxZoom = CameraConfig.MaxZoom;
        _stepZoom = CameraConfig.StepZoom;
        _currentZoom = CameraConfig.CurrentZoom;
        _zoomSpeed = CameraConfig.ZoomSpeed;

        _mapHeight = MapConfig.MapHeight;
        _mapWidth = MapConfig.MapWidth;
        _mapStart = new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY);
    }
}
