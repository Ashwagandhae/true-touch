using UnityEngine;

public class IncreaseRange : MonoBehaviour
{

    [SerializeField] BluetoothClient client;
    public void OnButtonClicked()
    {
        Debug.Log("Increasing range!");
        client.HandCommand = 1;
        client.SendHandCommand();

    }
}