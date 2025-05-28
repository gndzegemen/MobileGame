// MemoryGame.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class MemoryGame : MonoBehaviour
{
    [Header("Grid Ayarları")]
    public int ElementNumber = 18;
    public GameObject GridElementPrefab;
    public RectTransform Parent;

    [Header("Animasyon Ayarları")]
    [Tooltip("Deste nesnesinin RectTransform'u")]

    public RectTransform dealCardTransform;
    public RectTransform dealCardTransformInit;
    public RectTransform deckTransform;
    [Tooltip("Bir karta gitmesi gereken süre")]
    public float dealDuration = 0.5f;
    [Tooltip("Kartlar arası gecikme (s)")]
    public float dealStagger = 0.1f;

    [Header("Kontrol Ayarları")]
    [Tooltip("Kartlar eşleşmezse kaç saniye sonra kapanacak?")]
    public float checkDelay = 1f;
    public float firstRevealTimeout = 3f;

    private List<string> idList = new List<string>();
    public List<GridElement> cards = new List<GridElement>();
    public Stack<GridElement> revealCards = new Stack<GridElement>();
    public bool canReveal = true;

    public GameObject MainPage;
    public GameObject FinishPage;

    public GameObject ResetButton;

    private Coroutine firstRevealTimeoutCoroutine;

    private void Start()
    {
        dealCardTransformInit = dealCardTransform;
    }

    public void SetupGame()
    {
        idList.Clear();
        cards.Clear();
        revealCards.Clear();
        canReveal = true;

        if (firstRevealTimeoutCoroutine != null)
        {
            StopCoroutine(firstRevealTimeoutCoroutine);
            firstRevealTimeoutCoroutine = null;
        }

        

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
            element.memoryGame = this;
            element.ChangeStatusAsEmpty(); // Başlangıçta empty (görünmez)
            element.isReveal = false;
            element.CurrentState = GridElement.State.Empty;
            cards.Add(element);
        }

        //deckTransform.gameObject.SetActive(true);
        //ResetButton.SetActive(false);
        //dealCardTransform.gameObject.SetActive(true);

        DealAnimation();
    }

    void DealAnimation()
    {
        Sequence seq = DOTween.Sequence();

        for (int i = 0; i < cards.Count; i++)
        {
            int index = i; // Closure için gerekli
            RectTransform cardRect = cards[i].GetComponent<RectTransform>();

            seq.AppendCallback(() => {
                // Deste pozisyonundan kart pozisyonuna git
                dealCardTransform.position = deckTransform.position;
                dealCardTransform.DOMove(cardRect.position, dealDuration)
                    .OnComplete(() => {
                        // Kart görünür hale getir
                        cards[index].ChangeStatusAsBack();
                    });
            });

            seq.AppendInterval(dealDuration + dealStagger);
        }

        seq.OnComplete(() => {
            deckTransform.gameObject.SetActive(false);
            ResetButton.SetActive(true);
            dealCardTransform = dealCardTransformInit;
            dealCardTransform.gameObject.SetActive(false);
        });
    }




    public void RegisterReveal(GridElement card)
    {
        if (!canReveal || card.isReveal) return;

        card.ChangeStatusAsFront();
        card.isReveal = true;
        revealCards.Push(card);

        if (revealCards.Count == 1)
        {
            firstRevealTimeoutCoroutine = StartCoroutine(FirstRevealTimeout());
        }
        else if (revealCards.Count == 2)
        {
            if (firstRevealTimeoutCoroutine != null)
            {
                StopCoroutine(firstRevealTimeoutCoroutine);
                firstRevealTimeoutCoroutine = null;
            }
            StartCoroutine(CheckMatch());
        }
    }

    IEnumerator FirstRevealTimeout()
    {
        yield return new WaitForSeconds(firstRevealTimeout);

        // Eğer hâlâ tek kart açıksa, kapat
        if (revealCards.Count == 1)
        {
            var first = revealCards.Pop();
            first.ChangeStatusAsBack();
            first.isReveal = false;
            // Buton etkileşimini geri aç (eğer devre dışı kaldıysa)
            if (first.button != null)
                first.button.interactable = true;
        }

        firstRevealTimeoutCoroutine = null;
    }

    private IEnumerator CheckMatch()
    {
        canReveal = false;

        // Tüm butonları etkileşime kapat
        foreach (var c in cards)
            c.button.interactable = false;

        // Bekle (animasyon için zaman)
        yield return new WaitForSeconds(checkDelay);

        // İki kartı al
        var first = revealCards.Pop();
        var second = revealCards.Pop();

        if (first.ID == second.ID)
        {
            // Eşleşme: boş slot yap
            first.ChangeStatusAsEmpty();
            second.ChangeStatusAsEmpty();

            cards.Remove(first);
            cards.Remove(second);

            if (cards.Count == 0)
            {
                MainPage.gameObject.SetActive(false);
                FinishPage.gameObject.SetActive(true);
                ResetGame();
            }
        }
        else
        {
            // Eşleşmez: geri kapat
            first.ChangeStatusAsBack();
            second.ChangeStatusAsBack();
            first.isReveal = false;
            second.isReveal = false;
        }

        // Sadece hâlâ kapalı (empty olmayan) kartlara izin ver
        foreach (var c in cards)
            if (!c.isReveal && c.CurrentState != GridElement.State.Empty)
                c.button.interactable = true;

        canReveal = true;
    }

    public void ResetGame()
    {
        if (firstRevealTimeoutCoroutine != null)
        {
            StopCoroutine(firstRevealTimeoutCoroutine);
            firstRevealTimeoutCoroutine = null;
        }

        // Sahnedeki eski kartları temizle
        foreach (Transform child in Parent)
            Destroy(child.gameObject);

        // Yeniden kurulum
        SetupGame();
    }
}