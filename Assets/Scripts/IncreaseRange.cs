using System.Collections;
using UnityEngine;

public class IncreaseRange : MonoBehaviour
{

    [SerializeField] BluetoothClient client;
    public void OnButtonClicked()
    {
        Debug.Log("Increasing range!");
        client.HandCommand = 1;
        client.SendHandCommand();
        StartCoroutine(DoSomethingAfterDelay(0.6f));


    }

    IEnumerator DoSomethingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        client.HandCommand = 0;
        client.SendHandCommand();
        Debug.Log("Action performed 1/10th of a second later!");
    }
}