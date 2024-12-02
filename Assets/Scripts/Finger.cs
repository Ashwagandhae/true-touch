using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


public class Finger : MonoBehaviour
{
  private float scale = 0.5F;
  [SerializeField] private BluetoothClient client;
  // [SerializeField] private CapsuleCollider collider;
  [SerializeField] private int arrOffset = 0;
  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {
    if (client == null)
    {
      // Debug.Log("client is null");
      return;
    }
    // Debug.Log("client is not null");
    float[] arr = client.GetArr();

    Vector3 upperPos = new Vector3(arr[arrOffset + 0], arr[arrOffset + 1], arr[arrOffset + 2]) * scale;
    Vector3 lowerPos = new Vector3(arr[arrOffset + 3], arr[arrOffset + 4], arr[arrOffset + 5]) * scale;

    transform.position = (upperPos + lowerPos) / 2;
    transform.rotation = Quaternion.LookRotation(upperPos - lowerPos) * Quaternion.Euler(90, 0, 0);
  }
}
