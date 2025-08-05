using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PlayButton : MonoBehaviour
{
    public HorizontalCardHolder horizontalCardHolder;
    public PlayerHealth playerHealth;
   public int value = 0;
    public void PlayButtonClick()
    {
        bool anyCardSelected = false;
        foreach (Card card in horizontalCardHolder.cards)
        {
            if (card.selected == true)
            {
                anyCardSelected = true;
                //card.SelectionMaster();
            }
            else
            {

            }
        }
        if (!anyCardSelected) return;
        foreach (Card card in horizontalCardHolder.cards)
        {
            if (card.selected)
            {
                value += card.cardValue;
                card.isInteractable = false;
                card.isDraggable = false;
                card.isPlayed = true;
                ValueLoop(card);
            }
            else
            {
                //card.cardVisual.TurnCardDown();
            }
        }

        GameManager.Instance.FillScoreCardButton();
        


        //value = 0;
    }


    void ValueLoop(Card card)
    {
            if (card.selected)
            {
                value += card.cardValue;
                card.Deselect();
                //ValueLoop();
                card.transform.localPosition += card.cardVisual.transform.up * 300;
            }
            else
            {
                Debug.Log(" not selected!");
            }
            
        }
    }

