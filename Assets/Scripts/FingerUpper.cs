using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


public class FingerUpper : MonoBehaviour
{
    private float scale = 0.5F;
    [SerializeField] private BluetoothClient client;
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

        transform.position = new Vector3(arr[arrOffset + 0], arr[arrOffset + 1], arr[arrOffset + 2]) * scale;
    }
}
