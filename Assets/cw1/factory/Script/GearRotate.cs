using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GearRotate : MonoBehaviour
{
    public GameObject GearOne;
    public GameObject GearTwo;

    // Update is called once per frame
    void Update()
    {
        GearOne.transform.Rotate(0, 1, 0);
        GearTwo.transform.Rotate(0, -1, 0);
        
    }
}
