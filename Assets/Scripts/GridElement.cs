// GridElement.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GridElement : MonoBehaviour
{
    public enum State { Back, Front, Empty }

    public string ID;
    public MemoryGame memoryGame;

    [Header("UI Referansları")]
    public Button button;
    public GameObject Front;
    public GameObject Back;
    public GameObject Empty;

    public bool isReveal = false;
    public State CurrentState = State.Back;

    void Start()
    {
        button.onClick.AddListener(OnClick);
        ChangeStatusAsBack();
    }

    void OnClick()
    {
        memoryGame.RegisterReveal(this);
    }

    public void ChangeStatusAsFront()
    {
        Front.SetActive(true);
        Back.SetActive(false);
        Empty.SetActive(false);
        CurrentState = State.Front;
        
    }


    public void ChangeStatusAsBack()
    {
        Front.SetActive(false);
        Back.SetActive(true);
        Empty.SetActive(false);
        CurrentState = State.Back;
    }

    public void ChangeStatusAsEmpty()
    {
        Front.SetActive(false);
        Back.SetActive(false);
        Empty.SetActive(true);
        CurrentState = State.Empty;
    }
}
