using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using DG.Tweening;
using System.Linq;
using Unity.Mathematics;

public class HorizontalCardHolder : MonoBehaviour
{

    [SerializeField] private Card selectedCard;
    [SerializeReference] private Card hoveredCard;

    [SerializeField] private GameObject slotPrefab;
    private RectTransform rect;

    [Header("Spawn Settings")]
    [SerializeField] private int cardsToSpawn = 7;
    public List<Card> cards;

    bool isCrossing = false;
    [SerializeField] private bool tweenCardReturn = true;

    [SerializeField] private int swapsAvailable = 4;
    [SerializeField] private bool deckOnStart;
    [SerializeField] private int cardValueMin, cardValueMax, cardRankMin, cardRankMax;

    void Start()
    {
        if (!deckOnStart) return;
        SetupDeck();
    }


    public void SetupDeck()
    {
        for (int i = 0; i < cardsToSpawn; i++)
        {
            Instantiate(slotPrefab, transform);
        }

        rect = GetComponent<RectTransform>();
        cards = GetComponentsInChildren<Card>().ToList();


        int cardCount = 0;

        foreach (Card card in cards)
        {
            card.cardValue = UnityEngine.Random.Range(cardValueMin, cardValueMax);
            card.cardRank = UnityEngine.Random.Range(cardRankMin, cardRankMax);
            card.PointerEnterEvent.AddListener(CardPointerEnter);
            card.PointerExitEvent.AddListener(CardPointerExit);
            card.BeginDragEvent.AddListener(BeginDrag);
            card.EndDragEvent.AddListener(EndDrag);
            card.name = cardCount.ToString();
            cardCount++;
        }

        StartCoroutine(Frame());

        IEnumerator Frame()
        {
            yield return new WaitForSecondsRealtime(.1f);
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].cardVisual != null)
                    cards[i].cardVisual.UpdateIndex(transform.childCount);
            }
        }
    }

    private void SetupCard(Card card)
    {
        card.cardValue = UnityEngine.Random.Range(1, 14);
        card.cardRank = UnityEngine.Random.Range(0, 4);
        card.PointerEnterEvent.AddListener(CardPointerEnter);
        card.PointerExitEvent.AddListener(CardPointerExit);
        card.BeginDragEvent.AddListener(BeginDrag);
        card.EndDragEvent.AddListener(EndDrag);
        int i = cards.Count - 1;
        card.name = i.ToString();
    }

    //This is a really weird loop
    public void EraseDeck()
    {
        for (int i = cards.Count - 1; i <= cards.Count; i--)
        {
            if (i == -1) return;
            Destroy(cards[i].transform.parent.gameObject);
            cards.Remove(cards[i]);
        }
    }
    public void SwapButton()
    {
        if (swapsAvailable <= 0) return;
        bool anyCardSelected = false;
        foreach (Card card in cards)
        {
            if (card.selected == true)
            {
                anyCardSelected = true;
            }
        }

        if (!anyCardSelected) return;
        swapsAvailable--;
        SwapLoop();

    }

    void SwapLoop()
    {
        for(int i = 0; i< cards.Count; i++)
        {
            Card card = cards[i];
            if (card.selected)
            {
                GameObject c = Instantiate(slotPrefab, transform);
                cards.Add(c.GetComponentInChildren<Card>());
                SetupCard(c.GetComponentInChildren<Card>());
                Destroy(cards[i].gameObject.transform.parent.gameObject);
                cards.RemoveAt(i);
                Debug.Log("selected!");
                SwapLoop();
            }
            else
            {   
                Debug.Log(" not selected!");
            }
        }
    }

    private void BeginDrag(Card card)
    {
        selectedCard = card;
    }


    void EndDrag(Card card)
    {
        if (selectedCard == null)
            return;

        selectedCard.transform.DOLocalMove(selectedCard.selected ? new Vector3(0,selectedCard.selectionOffset,0) : Vector3.zero, tweenCardReturn ? .15f : 0).SetEase(Ease.OutBack);

        rect.sizeDelta += Vector2.right;
        rect.sizeDelta -= Vector2.right;

        selectedCard = null;

    }

    void CardPointerEnter(Card card)
    {
        hoveredCard = card;
    }

    void CardPointerExit(Card card)
    {
        hoveredCard = null;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (hoveredCard != null)
            {
                Destroy(hoveredCard.transform.parent.gameObject);
                cards.Remove(hoveredCard);

            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            foreach (Card card in cards)
            {
                card.Deselect();
            }
        }

        if (selectedCard == null)
            return;

        if (isCrossing)
            return;

        for (int i = 0; i < cards.Count; i++)
        {

            if (selectedCard.transform.position.x > cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() < cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }

            if (selectedCard.transform.position.x < cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() > cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }
        }
    }

    void Swap(int index)
    {
        isCrossing = true;

        Transform focusedParent = selectedCard.transform.parent;
        Transform crossedParent = cards[index].transform.parent;

        cards[index].transform.SetParent(focusedParent);
        cards[index].transform.localPosition = cards[index].selected ? new Vector3(0, cards[index].selectionOffset, 0) : Vector3.zero;
        selectedCard.transform.SetParent(crossedParent);

        isCrossing = false;

        if (cards[index].cardVisual == null)
            return;

        bool swapIsRight = cards[index].ParentIndex() > selectedCard.ParentIndex();
        cards[index].cardVisual.Swap(swapIsRight ? -1 : 1);

        //Updated Visual Indexes
        foreach (Card card in cards)
        {
            card.cardVisual.UpdateIndex(transform.childCount);
        }
    }

}
