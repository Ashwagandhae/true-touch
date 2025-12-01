using System.Collections;
using UnityEngine;

public class ReduceRange : MonoBehaviour
{

    [SerializeField] BluetoothClient client;
    public void OnButtonClicked()
    {
        Debug.Log("Reducing range!");
        client.HandCommand = 2;
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