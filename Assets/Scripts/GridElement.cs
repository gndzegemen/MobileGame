using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridElement : MonoBehaviour
{
    public string ID;
    public Button button;
    public int waitingTime;

    void Start()
    {
        ID = Guid.NewGuid().ToString();
    }


    
}
