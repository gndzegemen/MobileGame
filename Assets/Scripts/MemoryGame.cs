using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryGame : MonoBehaviour
{
    public int ElementNumber = 21;
    public GameObject GridElement;
    public Transform Parent;
    [SerializeField]
    private List<GameObject> GridElements;


    private void Start()
    {
        for (int i = 0; i < ElementNumber; i++)
        {
            var go = Instantiate(GridElement, Parent);
            
            GridElements.Add(go);
        }
          
    }
}
