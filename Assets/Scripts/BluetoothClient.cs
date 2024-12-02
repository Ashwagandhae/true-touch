using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class BluetoothClient : MonoBehaviour
{
  private TcpClient client;
  private NetworkStream stream;

  private float[] arr;

  void Start()
  {
    ConnectToServer("127.0.0.1", 8080); // Change IP and port if necessary
  }

  public float[] GetArr()
  {
    return arr;
  }

  private void ConnectToServer(string ipAddress, int port)
  {
    try
    {
      client = new TcpClient(ipAddress, port);
      stream = client.GetStream();
      Debug.Log("Connected to server.");
    }
    catch (Exception e)
    {
      Debug.LogError($"Could not connect to server: {e.Message}");
    }
  }

  private void Update()
  {
    if (stream != null && stream.DataAvailable)
    {
      // Read incoming data
      byte[] byteArray = new byte[1024];
      int bytesRead = stream.Read(byteArray, 0, byteArray.Length);
      float[] floatArray = new float[bytesRead / 4];
      Buffer.BlockCopy(byteArray, 0, floatArray, 0, bytesRead);
      Debug.Log("Received:");
      Debug.LogFormat($"{floatArray[0]}, {floatArray[1]}, {floatArray[2]}");

      arr = floatArray;
    }
  }

  private void OnApplicationQuit()
  {
    // Clean up
    stream?.Close();
    client?.Close();
  }
}
