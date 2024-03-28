using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrystalMine : MonoBehaviour
{
    public int totalAmount = 100;

    private int currentAmount;

    // Start is called before the first frame update
    private void Start()
    {
        currentAmount = totalAmount;
    }

    public void MineCrystal(int amount)
    {
        currentAmount = currentAmount - amount;
        currentAmount = currentAmount < 0 ? 0 : currentAmount;
    }


    // Update is called once per frame
    void Update()
    {
        if (currentAmount == 0)
            Destroy(gameObject);
    }
}
