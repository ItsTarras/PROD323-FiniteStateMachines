using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlowerControl : MonoBehaviour
{
    [SerializeField] ParticleSystem blower;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            blower.Play();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            blower.Stop();
        }
    }
}
