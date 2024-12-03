using System;
using System.Net.Sockets;
using System.Text;
using Unity.Mathematics;
using UnityEngine;

public class BluetoothClient : MonoBehaviour
{
  private TcpClient client;
  private NetworkStream stream;

  private float[] arr;

  private Vector3 joint1Pos;
  private Quaternion joint1Angle;

  private Vector3 joint2Pos;
  private Quaternion joint2Angle;

  private Vector3 joint3Pos;
  private Quaternion joint3Angle;

  private float lastBentness = 0;

  public byte HandCommand = 0;



  void Start()
  {
    ConnectToServer("127.0.0.1", 8080); // Change IP and port if necessary
    UpdateJointsBentness(0.0F);
    // StartCoroutine(WriteToStreamPeriodically());
  }

  public float[] GetArr()
  {
    return arr;
  }

  public (Vector3, Quaternion) GetJointPos(int n)
  {
    if (n == 0)
    {
      return (joint1Pos, joint1Angle);
    }
    if (n == 1)
    {
      return (joint2Pos, joint2Angle);
    }
    if (n == 2)
    {
      return (joint3Pos, joint3Angle);
    }
    return (Vector3.zero, Quaternion.identity);
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

      arr = floatArray;
      UpdateJoints();
      if (Input.GetKey(KeyCode.P))
      {
        Debug.Log("sending data!!");
        byte[] data = { 21 }; // Example data
        stream.Write(data, 0, data.Length);
      }
    }

  }

  public void SendHandCommand()
  {
    if (stream != null && stream.DataAvailable)
    {

      Debug.Log("sending hand command!!");
      byte[] data = { HandCommand }; // Example data
      stream.Write(data, 0, data.Length);
    }
  }

  private System.Collections.IEnumerator WriteToStreamPeriodically()
  {
    while (true)
    {
      if (stream != null)
      {
        try
        {
          Debug.Log("Sending data every 100ms...");
          byte[] data = { 1, 2, 3, 4 }; // Example data
          stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
          Debug.LogError($"Failed to send data: {e.Message}");
        }
      }

      // Wait for 100 milliseconds
      yield return new WaitForSeconds(0.05f);
    }
  }

  private void UpdateJoints()
  {
    Quaternion handAngle = new(arr[0], arr[1], arr[2], arr[3]);
    Quaternion fingerAngle = new(arr[4], arr[5], arr[6], arr[7]);
    Debug.Log("handAngle");
    Debug.Log(handAngle);

    Debug.Log("fingerAngle1");
    Debug.Log(fingerAngle * Quaternion.Euler(new Vector3(90, 0, 0)));
    Debug.Log("fingerAngle2");
    Debug.Log(fingerAngle * Quaternion.Euler(new Vector3(-90, 0, 0)));
    Debug.Log("fingerAngle3");
    Debug.Log(fingerAngle * Quaternion.Euler(new Vector3(0, 90, 0)));
    Debug.Log("fingerAngle4");
    Debug.Log(fingerAngle * Quaternion.Euler(new Vector3(0, -90, 0)));
    Debug.Log("fingerAngle5");
    Debug.Log(fingerAngle * Quaternion.Euler(new Vector3(0, 0, 90)));
    Debug.Log("fingerAngle6");
    Debug.Log(fingerAngle * Quaternion.Euler(new Vector3(0, 0, -90)));




    // fingerAngle *= Quaternion.Euler(new Vector3(0, -90, 0));
    // Quaternion fingerNormalized = fingerAngle * Quaternion.Inverse(handAngle);
    // Vector3 fingerEuler = fingerNormalized.eulerAngles;

    // Debug.Log(fingerEuler);
    float angle = Quaternion.Angle(handAngle, fingerAngle);

    Debug.Log("anglebetween");
    Debug.Log(angle);

    float bentness;
    float e = ((angle % 180) + 180) % 180;

    float range = 70.0F;
    float rangeStart = 10.0F;

    if (0 < e && e < rangeStart)
    {
      bentness = 0;
    }
    else if (e < rangeStart + range)
    {
      bentness = (e - rangeStart) / range;
    }
    else
    {
      bentness = 1;
    }
    UpdateJointsBentness(bentness);
  }

  private void UpdateJointsBentness(float bentness)
  {
    lastBentness = bentness;

    Quaternion rootAngle = Quaternion.Euler(0, 0, 30);
    // bentness between 0 and 1

    float angle12 = bentness * 90;
    float angle23 = bentness * 90;

    float jointLength1 = 2.0F;
    float jointLength2 = 1.6F;
    float jointLength3 = 1.2F;

    joint1Angle = rootAngle;
    joint1Pos =
      Vector3.zero +
      rootAngle * Vector3.up * (jointLength1 * 0.5F);

    joint2Angle = rootAngle * Quaternion.Euler(0, 0, angle12);
    joint2Pos =
      joint1Pos +
      joint1Angle * Vector3.up * (jointLength1 * 0.5F) +
      joint2Angle * Vector3.up * (jointLength2 * 0.5F);

    joint3Angle = rootAngle * Quaternion.Euler(0, 0, angle12 + angle23);
    joint3Pos =
      joint2Pos +
      joint2Angle * Vector3.up * (jointLength2 * 0.5F) +
      joint3Angle * Vector3.up * (jointLength3 * 0.5F);
  }

  private void OnApplicationQuit()
  {
    // Clean up
    stream?.Close();
    client?.Close();
  }
}
