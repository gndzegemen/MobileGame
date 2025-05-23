using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GridElement : MonoBehaviour
{
    public string ID;
    public Button button;
    public int waitingTime;
    public GameObject Front;
    public GameObject Back;
    public GameObject Empty;

    void Start()
    {
        button?.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        Front.SetActive(false);
        Back.SetActive(true);
        StartCoroutine(ReturnImageBack());
    }

    IEnumerator ReturnImageBack()
    {
        yield return new WaitForSeconds(waitingTime);
        Front.SetActive(true);
        Back.SetActive(false);
    }
}
