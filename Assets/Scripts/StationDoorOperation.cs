using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StationDoorOperation : MonoBehaviour
{
    [SerializeField] Animation doorAnim;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            doorAnim.Play("Structure_v3_open");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            doorAnim.Play("Structure_v3_close");
        }
    }
}
