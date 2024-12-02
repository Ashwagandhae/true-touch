using UnityEngine;

public class CameraController : MonoBehaviour
{
  public float mouseSensitivity = 100f; // Sensitivity of mouse movement
  public float moveSpeed = 5f;         // Speed of movement

  private float xRotation = 0f;       // Keep track of vertical rotation

  void Start()
  {
    // Lock the cursor to the center of the screen and make it invisible
    Cursor.lockState = CursorLockMode.Locked;
  }

  void Update()
  {
    // Mouse look
    float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
    float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

    xRotation -= mouseY;
    xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Prevent over-rotation

    transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    transform.parent.Rotate(Vector3.up * mouseX);

    // Movement
    float moveX = Input.GetAxis("Horizontal"); // A/D keys
    float moveZ = Input.GetAxis("Vertical");   // W/S keys

    Vector3 move = transform.parent.right * moveX + transform.parent.forward * moveZ;
    transform.parent.position += move * moveSpeed * Time.deltaTime;
  }
}
