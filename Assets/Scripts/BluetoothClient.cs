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

  // joint storage: [finger, jointIndex]
  private Vector3[,] jointPos = new Vector3[5, 3];
  private Quaternion[,] jointAngle = new Quaternion[5, 3];

  private float[] lastBentness = new float[5];

  public byte HandCommand = 0;

  private Quaternion?[] initialFingerAngle = new Quaternion?[5];

  // Per-finger start offsets and optional rotation overrides
  private Vector3[] fingerOffsets = new Vector3[5];
  private Quaternion[] fingerAngles = new Quaternion[5];
  private bool[] fingerAngleManual = new bool[5];

  // Per-finger attachment direction (base segment direction) and bend direction
  private Vector3[] fingerAttach = new Vector3[5];
  private Vector3[] fingerBend = new Vector3[5];

  // Per-finger joint lengths for the three segments
  private float[] jointLength1PerFinger = new float[5];
  private float[] jointLength2PerFinger = new float[5];
  private float[] jointLength3PerFinger = new float[5];

  // per-finger base bend amount (degrees) applied to joint 0 at full bentness
  private float[] baseBendDegreesPerFinger = new float[5];



  void Start()
  {
    ConnectToServer("127.0.0.1", 8080); // Change IP and port if necessary
    // UpdateJointsBentness(new float[]{0.0F});
    for (int i = 0; i < 5; i++)
    {
      fingerOffsets[i] = Vector3.zero;
      fingerAngles[i] = Quaternion.identity;
      fingerAngleManual[i] = false;
      // default joint lengths (matching previous global defaults)
      jointLength1PerFinger[i] = 2.0f;
      jointLength2PerFinger[i] = 1.6f;
      jointLength3PerFinger[i] = 1.2f;
      // default attach = up, bend = right
      fingerAttach[i] = Vector3.up;
      fingerBend[i] = Vector3.right;
      // default base bend amount
      baseBendDegreesPerFinger[i] = 30f;
    }
    fingerOffsets[0] = new Vector3(1.2f, -3.0f, 0.8f);
    fingerOffsets[1] = new Vector3(0, -0.3f, 1.2f);
    fingerOffsets[2] = new Vector3(-1, 0, 2.3f);
    fingerOffsets[3] = new Vector3(-1, -0.3f, 3.4f);
    fingerOffsets[4] = new Vector3(-0.8f, -0.5f, 4.5f);

    jointLength1PerFinger[0] = 1.2f;
    jointLength2PerFinger[0] = 1.2f;
    jointLength3PerFinger[0] = 1.8f;

    jointLength1PerFinger[1] = 2.0f;
    jointLength2PerFinger[1] = 1.6f;
    jointLength3PerFinger[1] = 1.2f;

    jointLength1PerFinger[2] = 2.0f;
    jointLength2PerFinger[2] = 1.6f;
    jointLength3PerFinger[2] = 1.2f;

    jointLength1PerFinger[3] = 2.0f;
    jointLength2PerFinger[3] = 1.6f;
    jointLength3PerFinger[3] = 1.2f;

    jointLength1PerFinger[4] = 1.8f;
    jointLength2PerFinger[4] = 1.4f;
    jointLength3PerFinger[4] = 1.0f;


    fingerAttach[0] = new Vector3(0.3f, 1.2f, -1f);
    fingerBend[0] = new Vector3(1f, 1f, 0f);

    // thumb base bend smaller
    baseBendDegreesPerFinger[0] = 5f;

    UpdateJointsBentness(new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f });


    // StartCoroutine(WriteToStreamPeriodically());
  }

  public float[] GetArr()
  {
    return arr;
  }

  // Returns the (position, rotation) tuple for a finger joint
  public (Vector3, Quaternion) GetJointPos(int finger, int joint)
  {
    if (finger < 0 || finger >= 5 || joint < 0 || joint >= 3)
      return (Vector3.zero, Quaternion.identity);
    return (jointPos[finger, joint], jointAngle[finger, joint]);
  }

  // // Backwards-compatible single-parameter overload: finger 0
  // public (Vector3, Quaternion) GetJointPos(int n)
  // {
  //   return GetJointPos(0, n);
  // }

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
    if (stream != null)
    {

      Debug.Log("sending hand command!!");
      byte[] data = { HandCommand }; // Example data
      stream.Write(data, 0, data.Length);
    }
    else
    {

      Debug.Log("stream is null!!");
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
    if (arr == null || arr.Length == 0)
      return;

    // If we have at least 5 values, treat them as per-finger bentness.
    float[] bentnesses = new float[5];
    if (arr.Length >= 5)
    {
      for (int i = 0; i < 5; i++)
        bentnesses[i] = arr[i];
    }
    else
    {
      // Fallback: use first value for all fingers
      for (int i = 0; i < 5; i++)
        bentnesses[i] = arr[0];
    }

    UpdateJointsBentness(bentnesses);
  }


  private void UpdateJointsBentness(float[] bentnesses)
  {
    if (bentnesses == null)
      return;

    Quaternion defaultRootAngle = Quaternion.Euler(0, 0, 30);

    for (int f = 0; f < 5; f++)
    {
      float raw = bentnesses[f];
      lastBentness[f] = raw;
      float b = Mathf.Clamp(raw, -1f, 1f);

      float angle12 = b * 90f;
      float angle23 = b * 90f;

      // base start position comes from `fingerOffsets` (absolute)
      Vector3 basePos = fingerOffsets[f];

      // choose root angle: per-finger manual or computed from attach/bend
      Quaternion rootAngle = fingerAngleManual[f] ? fingerAngles[f] : defaultRootAngle;

      // compute orthonormal basis from attach (y) and bend (guide) vectors
      Vector3 y = fingerAttach[f].normalized;
      if (y.sqrMagnitude < 1e-6f) y = Vector3.up;
      Vector3 bendGuide = fingerBend[f].normalized;
      if (bendGuide.sqrMagnitude < 1e-6f) bendGuide = Vector3.right;

      // ensure bendGuide is not parallel to y
      Vector3 x = Vector3.Cross(bendGuide, y);
      if (x.sqrMagnitude < 1e-6f)
      {
        // pick an arbitrary vector that's not parallel
        bendGuide = Vector3.Cross(y, Vector3.forward);
        if (bendGuide.sqrMagnitude < 1e-6f) bendGuide = Vector3.Cross(y, Vector3.right);
        x = Vector3.Cross(bendGuide, y);
      }
      x.Normalize();
      Vector3 z = Vector3.Cross(y, x).normalized;

      // build root rotation so that local up (Vector3.up) -> y, local right -> x, local forward -> z
      if (!fingerAngleManual[f])
        rootAngle = Quaternion.LookRotation(z, y);

      // per-finger joint lengths
      float jointLength1 = jointLength1PerFinger[f];
      float jointLength2 = jointLength2PerFinger[f];
      float jointLength3 = jointLength3PerFinger[f];

      // joint 0 (base) — give it a share of the bend so base can flex
      float baseBendDegrees = baseBendDegreesPerFinger[f]; // per-finger configured max
      float angle0 = b * baseBendDegrees;
      jointAngle[f, 0] = rootAngle * Quaternion.AngleAxis(angle0, Vector3.right);
      jointPos[f, 0] = basePos + jointAngle[f, 0] * Vector3.up * (jointLength1 * 0.5F);

      // joint 1: rotate around local right (x) by angle12
      jointAngle[f, 1] = jointAngle[f, 0] * Quaternion.AngleAxis(angle12, Vector3.right);
      jointPos[f, 1] =
        jointPos[f, 0] +
        jointAngle[f, 0] * Vector3.up * (jointLength1 * 0.5F) +
        jointAngle[f, 1] * Vector3.up * (jointLength2 * 0.5F);

      // joint 2: continue rotating around local right by angle23
      jointAngle[f, 2] = jointAngle[f, 1] * Quaternion.AngleAxis(angle23, Vector3.right);
      jointPos[f, 2] =
        jointPos[f, 1] +
        jointAngle[f, 1] * Vector3.up * (jointLength2 * 0.5F) +
        jointAngle[f, 2] * Vector3.up * (jointLength3 * 0.5F);
    }
  }

  private void OnApplicationQuit()
  {
    // Clean up
    stream?.Close();
    client?.Close();
  }

  // Public API: set per-finger start offset
  public void SetFingerOffset(int finger, Vector3 offset)
  {
    if (finger < 0 || finger >= 5) return;
    fingerOffsets[finger] = offset;
  }

  // Set a manual root angle for a finger (optional)
  public void SetFingerAngle(int finger, Quaternion angle, bool manual = true)
  {
    if (finger < 0 || finger >= 5) return;
    fingerAngles[finger] = angle;
    fingerAngleManual[finger] = manual;
  }

  public void ClearFingerOffset(int finger)
  {
    if (finger < 0 || finger >= 5) return;
    fingerOffsets[finger] = Vector3.zero;
  }

  public void ClearFingerAngle(int finger)
  {
    if (finger < 0 || finger >= 5) return;
    fingerAngleManual[finger] = false;
    fingerAngles[finger] = Quaternion.identity;
  }

  public void ClearAllFingerOverrides()
  {
    for (int i = 0; i < 5; i++)
    {
      fingerAngleManual[i] = false;
      fingerOffsets[i] = Vector3.zero;
      fingerAngles[i] = Quaternion.identity;
    }
  }

  // Set attach and bend guide vectors for a finger
  public void SetFingerAttach(int finger, Vector3 attachDirection)
  {
    if (finger < 0 || finger >= 5) return;
    fingerAttach[finger] = attachDirection;
  }

  public void SetFingerBendGuide(int finger, Vector3 bendGuide)
  {
    if (finger < 0 || finger >= 5) return;
    fingerBend[finger] = bendGuide;
  }

  public void ClearFingerAttach(int finger)
  {
    if (finger < 0 || finger >= 5) return;
    fingerAttach[finger] = Vector3.up;
  }

  public void ClearFingerBendGuide(int finger)
  {
    if (finger < 0 || finger >= 5) return;
    fingerBend[finger] = Vector3.right;
  }

  // Public API: set per-finger joint lengths
  public void SetJointLengths(int finger, float len1, float len2, float len3)
  {
    if (finger < 0 || finger >= 5) return;
    jointLength1PerFinger[finger] = len1;
    jointLength2PerFinger[finger] = len2;
    jointLength3PerFinger[finger] = len3;
  }

  public void SetJointLength1(int finger, float len)
  {
    if (finger < 0 || finger >= 5) return;
    jointLength1PerFinger[finger] = len;
  }

  public void SetJointLength2(int finger, float len)
  {
    if (finger < 0 || finger >= 5) return;
    jointLength2PerFinger[finger] = len;
  }

  public void SetJointLength3(int finger, float len)
  {
    if (finger < 0 || finger >= 5) return;
    jointLength3PerFinger[finger] = len;
  }

  public void ResetJointLengthsToDefault()
  {
    for (int i = 0; i < 5; i++)
    {
      jointLength1PerFinger[i] = 2.0f;
      jointLength2PerFinger[i] = 1.6f;
      jointLength3PerFinger[i] = 1.2f;
    }
  }

  // Set/Get base bend degrees for joint 0 per finger
  public void SetBaseBendDegrees(int finger, float degrees)
  {
    if (finger < 0 || finger >= 5) return;
    baseBendDegreesPerFinger[finger] = degrees;
  }

  public float GetBaseBendDegrees(int finger)
  {
    if (finger < 0 || finger >= 5) return 0f;
    return baseBendDegreesPerFinger[finger];
  }

}
