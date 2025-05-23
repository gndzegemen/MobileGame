using System.Collections.Generic;
using UnityEngine;

public class MemoryGame : MonoBehaviour
{
    public int ElementNumber = 18;
    public GameObject GridElementPrefab;
    public Transform Parent;

    private List<string> idList = new List<string>();
    private List<GameObject> gridElements = new List<GameObject>();

    private void Start()
    {
        GenerateIDPairs();
        Shuffle(idList);
        SpawnGridElements();
    }

    void GenerateIDPairs()
    {
        int pairCount = ElementNumber / 2;

        for (int i = 0; i < pairCount; i++)
        {
            string id = System.Guid.NewGuid().ToString();
            idList.Add(id);
            idList.Add(id);
        }
    }

    void Shuffle(List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randIndex = Random.Range(i, list.Count);
            (list[i], list[randIndex]) = (list[randIndex], list[i]);
        }
    }

    void SpawnGridElements()
    {
        for (int i = 0; i < ElementNumber; i++)
        {
            GameObject go = Instantiate(GridElementPrefab, Parent);
            var element = go.GetComponent<GridElement>();
            element.ID = idList[i];
            gridElements.Add(go);
        }
    }
}
