using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboarding : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.forward = Camera.main.transform.position - gameObject.transform.position;
    }
}
