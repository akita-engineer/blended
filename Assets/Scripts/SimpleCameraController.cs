using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SimpleCameraController : MonoBehaviour
{
    private Camera mCamera;

    private Vector3 prevMouseDirection;

    private Vector3 prevMousePosition;
        
    [SerializeField]
    private bool allowMovement = default;
    
    [SerializeField]
    private float movementSpeed = default;
    
    [SerializeField]
    private float xAxisTurnSpeed = default;
    
    [SerializeField]
    private float yAxisTurnSpeed = default;

    private bool firstUpdate = true;

    private void Awake()
    {
        mCamera = GetComponent<Camera>();
    }

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {   
        Vector3 mousePositionDelta = new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"), 0f);
        float yAxisTurn = mousePositionDelta.x;
        float xAxisTurn = -mousePositionDelta.y;
        Vector3 angle = mCamera.transform.rotation.eulerAngles + new Vector3(xAxisTurn*xAxisTurnSpeed, yAxisTurn*yAxisTurnSpeed, 0) * Time.deltaTime;
        angle.z = 0f;
        mCamera.transform.rotation = Quaternion.Euler(angle);

        if (allowMovement)
        {
            if (Input.GetKey(KeyCode.W))
            {
                mCamera.transform.Translate(mCamera.transform.forward*movementSpeed*Time.deltaTime, Space.World);
            }

            if (Input.GetKey(KeyCode.S))
            {
                mCamera.transform.Translate(-mCamera.transform.forward*movementSpeed*Time.deltaTime, Space.World);
            }

            if (Input.GetKey(KeyCode.A))
            {
                mCamera.transform.Translate(-mCamera.transform.right*movementSpeed*Time.deltaTime, Space.World);
            }

            if (Input.GetKey(KeyCode.D))
            {
                mCamera.transform.Translate(mCamera.transform.right*movementSpeed*Time.deltaTime, Space.World);
            }
        }
    }
}