    using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PlayButton : MonoBehaviour
{
    public HorizontalCardHolder horizontalCardHolder;
    public PlayerHealth playerHealth;
    int value = 0;
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
        ValueLoop();

        if (value > 21)
        {
            Debug.Log("Failed! " + value);
            playerHealth.TakeDamage();
        }
        else
        {
            Debug.Log("Won! " + value);
        }
        value = 0;
    }


    void ValueLoop()
    {
        for (int i = 0; i < horizontalCardHolder.cards.Count; i++)
        {
            Card card = horizontalCardHolder.cards[i];
            if (card.selected)
            {
                value += card.cardValue;
                card.Deselect();
                ValueLoop();
                
            }
            else
            {
                Debug.Log(" not selected!");
            }
            card.transform.localPosition += card.cardVisual.transform.up * 150;
        }
    }
}
