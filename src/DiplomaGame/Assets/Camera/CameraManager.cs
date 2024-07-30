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

    /// <summary>
    /// Camera position for edge movement
    /// </summary>
    private Vector3 _targetPosition;


    private bool _isMiddleMousePressed;
    private Vector3 _lastMousePosition;

    /// <summary>
    /// If camera moving to the set point of the map
    /// </summary>
    private bool _isCameraMovingToPosition = false;

    /// <summary>
    /// New mouse position to move for middle mouse movement
    /// </summary>
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
            SetCameraPositionToMove(new Vector3(2845, 2845, 0));
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

    /// <summary>
    /// Handles camera movement when the mouse is near the edges of the screen.
    /// </summary>
    /// <remarks>
    /// This method moves the camera in the direction relative to the screen center when the mouse pointer is close to the screen edges.
    /// The camera movement speed is adjusted based on the current zoom level and normalized to stay within map boundaries.
    /// It only activates if the middle mouse button is not pressed.
    /// </remarks>
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

    /// <summary>
    /// Handles camera movement using the middle mouse button.
    /// </summary>
    /// <remarks>
    /// This method allows the user to move the camera by holding down the middle mouse button and dragging the mouse.
    /// It calculates the movement direction and distance based on the change in mouse position and updates the camera's 
    /// target position accordingly. The camera's position is then normalized to ensure it stays within map boundaries.
    /// </remarks>
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

    /// <summary>
    /// Handles the zooming functionality of the camera based on mouse scroll input.
    /// </summary>
    /// <remarks>
    /// This method adjusts the camera's zoom level and updates its position accordingly. If the new zoom level causes the camera 
    /// to collide with the map borders, it moves the camera away from the borders and normalizes its position. 
    /// If the zoom level changes and the mouse is not near the center of the screen, the camera also moves towards or away 
    /// from the mouse position.
    /// </remarks>
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

    /// <summary>
    /// Normalizes the camera's position to ensure it stays within the bounds of the map.
    /// </summary>
    /// <remarks>
    /// This method calculates the aspect ratio of the screen, determines the camera's dimensions based on the current zoom level,
    /// and then adjusts the camera's target position to ensure it doesn't go outside the map boundaries defined in CameraManager.
    /// Must be used after each camera movement method
    /// </remarks>
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

    /// <summary>
    /// Checks if the camera's new position and zoom level cause it to collide with the map borders.
    /// </summary>
    /// <param name="newZoom">The new zoom level of the camera.</param>
    /// <returns>True if the camera collides with the map borders; otherwise, false.</returns>
    /// <remarks>
    /// This method calculates the aspect ratio of the screen, determines the camera's dimensions based on the provided zoom level,
    /// and then checks if any part of the camera's view extends beyond the map boundaries defined in CameraManager.
    /// </remarks>
    private bool CheckCameraMapBordersCollision(float newZoom)
    {
        var aspectRatio = Screen.width / (float)Screen.height;
        var cameraWidth = newZoom * 2 * aspectRatio;
        var cameraHeight = newZoom * 2;

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

    /// <summary>
    /// Calculates the vector required to move the camera away from the map borders.
    /// </summary>
    /// <param name="newZoom">The new zoom level of the camera.</param>
    /// <returns>A vector indicating the direction and distance to move the camera to stay within the map borders.</returns>
    /// <remarks>
    /// This method determines the camera's dimensions based on the new zoom level and calculates the necessary adjustments 
    /// to the camera's target position to ensure it stays within the map boundaries defined in CameraManager.
    /// </remarks>
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

    /// <summary>
    /// Moves the camera to the target position and normalizes its position to stay within map boundaries.
    /// </summary>
    /// <remarks>
    /// This method sets the camera's target position to the specified position and adjusts it to ensure it remains 
    /// within the defined map borders by calling `NormalizeCameraMapPosition`.
    /// </remarks>
    private void MoveCameraToPosition()
    {
        _targetPosition.x = _positionToMove.x;
        _targetPosition.y = _positionToMove.y;
        NormalizeCameraMapPosition();
    }

    /// <summary>
    /// Sets the target position for the camera to move to and starts the movement process.
    /// </summary>
    /// <param name="finalPosition">The position to move the camera to.</param>
    /// <remarks>
    /// This method updates the camera's target position and flags that the camera is moving to this new position.
    /// </remarks>
    public void SetCameraPositionToMove(Vector3 finalPosition)
    {
        _positionToMove = finalPosition;
        _isCameraMovingToPosition = true;
    }

    /// <summary>
    /// Loads all configuration variables for the camera and map from their respective configuration classes.
    /// </summary>
    /// <remarks>
    /// This method initializes local variables for managing the camera and map parameters using the values defined in CameraConfig and MapConfig.
    /// </remarks>
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

        _mapHeight = MapConfig.MapHeight * MapConfig.CellSize;
        _mapWidth = MapConfig.MapWidth * MapConfig.CellSize;
        _mapStart = new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY);
    }
}
