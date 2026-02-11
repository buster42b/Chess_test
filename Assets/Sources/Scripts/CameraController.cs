using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class CameraController : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 2f;
    
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 20f;
    
    [SerializeField] private Transform cameraTransform;
    
    private InputAction _rotateAction;
    private InputAction _mousePositionAction;
    private InputAction _scrollAction;
    
    private bool _isRotating = false;
    private Vector2 _lastMousePosition;
    
    [Inject]
    public void Construct()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>().transform;
        
        SetupInputActions();
    }
    
    private void SetupInputActions()
    {
        _rotateAction = new InputAction("Rotate");
        _rotateAction.AddBinding("<Mouse>/rightButton");
        _rotateAction.started += OnRotateStarted;
        _rotateAction.canceled += OnRotateCanceled;
        _rotateAction.Enable();
        
        _mousePositionAction = new InputAction("MousePosition");
        _mousePositionAction.AddBinding("<Mouse>/position");
        _mousePositionAction.Enable();
        
        _scrollAction = new InputAction("Scroll");
        _scrollAction.AddBinding("<Mouse>/scroll");
        _scrollAction.performed += OnScroll;
        _scrollAction.Enable();
    }
    
    private void OnRotateStarted(InputAction.CallbackContext context)
    {
        _isRotating = true;
        _lastMousePosition = _mousePositionAction.ReadValue<Vector2>();
    }
    
    private void OnRotateCanceled(InputAction.CallbackContext context)
    {
        _isRotating = false;
    }
    
    private void OnScroll(InputAction.CallbackContext context)
    {
        Vector2 scrollValue = context.ReadValue<Vector2>();
        float zoomAmount = scrollValue.y * zoomSpeed * 0.1f;
        cameraTransform.Translate(Vector3.forward * zoomAmount);
        
        float currentDistance = Vector3.Distance(transform.position, cameraTransform.position);
        if (currentDistance < minDistance)
        {
            cameraTransform.position = transform.position - cameraTransform.forward * minDistance;
        }
        else if (currentDistance > maxDistance)
        {
            cameraTransform.position = transform.position - cameraTransform.forward * maxDistance;
        }
    }
    
    private void Update()
    {
        if (_isRotating)
        {
            Vector2 currentMousePosition = _mousePositionAction.ReadValue<Vector2>();
            Vector2 mouseDelta = currentMousePosition - _lastMousePosition;
            
            float horizontalRotation = mouseDelta.x * rotationSpeed;
            
            transform.Rotate(Vector3.up, horizontalRotation, Space.World);
            
            _lastMousePosition = currentMousePosition;
        }
    }
    
    
    private void OnDestroy()
    {
        _rotateAction?.Dispose();
        _mousePositionAction?.Dispose();
        _scrollAction?.Dispose();
    }
}
