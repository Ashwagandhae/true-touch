using UnityEngine;

public class ReduceRange : MonoBehaviour
{

    [SerializeField] BluetoothClient client;
    public void OnButtonClicked()
    {
        Debug.Log("Reducing range!");
        client.HandCommand = 2;
        client.SendHandCommand();

    }
}