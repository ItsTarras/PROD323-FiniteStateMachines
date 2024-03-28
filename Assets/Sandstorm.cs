using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Sandstorm : MonoBehaviour
{
    public HarvesterAgentControl agent;
    public TextMeshProUGUI sandWarning;
    public GameObject sandstormParticle;
    public bool sandActive = false;
    public Text alertText;
    // Start is called before the first frame update


    void Start()
    {
        StartCoroutine("sandPrepare");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator sandPrepare()
    {
        while (true)
        {
            //Run this every second.
            int randomNumber = Random.Range(0, 15);
            Debug.Log(randomNumber);
            if (!sandActive)
            {
                //5% every second to activate sandstorm.
                if (randomNumber == 1)
                {
                    //Begin warning.
                    sandWarning.text = "Sandstorm approaching!";
                    alertText.text = "Alert Level: RED!";
                    alertText.color = Color.red;
                    sandActive = true;
                    //activate agent behaviour. 
                    agent.redAlert = true;

                    //Warning has completed.
                    yield return new WaitForSeconds(Random.Range(3f, 5f));


                    //Sandstorm has started.
                    sandWarning.text = "Darude - Sandstorm";
                    sandstormParticle.SetActive(true);
                    StartCoroutine("damageAgent");
                    yield return new WaitForSeconds(Random.Range(7f, 12f));

                    //Sandstorm has stopped.
                    sandActive = false;
                    sandWarning.text = string.Empty;
                    StopCoroutine("damageAgent");
                    sandstormParticle.SetActive(false);
                    agent.redAlert = false;

                    alertText.text = "Alert Level: Safe";
                    alertText.color = Color.white;
                }
            }
            yield return new WaitForSeconds(1f); ;
        }
    }

    public IEnumerator damageAgent()
    {
        while (true)
        {
            if (agent.CheckStationIsReached() == false)
            {
                agent.healthbar.value -= Time.deltaTime * 2;
            }

            yield return null;
        }
        
    }
}
