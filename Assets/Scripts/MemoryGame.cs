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
    public RectTransform deckTransform;
    public RectTransform dealCard;
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

    private Coroutine firstRevealTimeoutCoroutine;
    private Coroutine dealingCoroutine;

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

        if (dealingCoroutine != null)
        {
            StopCoroutine(dealingCoroutine);
            dealingCoroutine = null;
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

        // Animasyonu başlat
        dealingCoroutine = StartCoroutine(DealCardsSequentially());
    }

    IEnumerator DealCardsSequentially()
    {
        // Tüm kartları sırasıyla dağıt
        for (int i = 0; i < cards.Count; i++)
        {
            yield return StartCoroutine(DealSingleCard(cards[i]));
            yield return new WaitForSeconds(dealStagger);
        }

        // Tüm kartlar dağıtıldıktan sonra biraz bekle, sonra back'e çevir
        yield return new WaitForSeconds(1f);

        // Tüm kartları back durumuna çevir
        foreach (var card in cards)
        {
            card.ChangeStatusAsBack();
            card.isReveal = false;
            card.CurrentState = GridElement.State.Back;
        }

        dealingCoroutine = null;
    }

    IEnumerator DealSingleCard(GridElement targetCard)
    {
        // Hedef pozisyonu al
        var targetTransform = targetCard.GetComponent<RectTransform>();
        var targetPos = targetTransform.localPosition;

        // DealCard'ı deste pozisyonuna getir ve aktif et
        dealCard.gameObject.SetActive(true);
        dealCard.localPosition = deckTransform.localPosition;

        // Animasyon tamamlandığını beklemek için bir flag
        bool animationComplete = false;

        // Animasyonu başlat
        dealCard
            .DOLocalMove(targetPos, dealDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                // Animasyon bittiğinde dealCard'ı gizle
                dealCard.gameObject.SetActive(false);

                // Hedef kartı front durumuna getir
                targetCard.ChangeStatusAsFront();
                targetCard.isReveal = true;
                targetCard.CurrentState = GridElement.State.Front;

                animationComplete = true;
            });

        // Animasyonun bitmesini bekle
        yield return new WaitUntil(() => animationComplete);
    }

    public void RegisterReveal(GridElement card)
    {
        if (!canReveal || card.isReveal || dealingCoroutine != null) return;

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
        // Çalışan coroutine'leri durdur
        if (dealingCoroutine != null)
        {
            StopCoroutine(dealingCoroutine);
            dealingCoroutine = null;
        }

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