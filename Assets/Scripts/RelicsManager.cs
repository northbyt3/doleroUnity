using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelicsManager : MonoBehaviour
{
    public HorizontalCardHolder relicsDeck, playerDeck;
    public EnemyDeck enemyDeck;
    public void PickButton()
    {
        int i = 0;
        foreach (Card card in relicsDeck.cards)
        {
            if (card.selected)
            {
                i++;
            }
        }

        if (i > 1 || i <= 0) return;
        relicsDeck.EraseDeck();
        playerDeck.SetupDeck();
        enemyDeck.SetupEnemyDeck();
        this.gameObject.SetActive(false);

    }

    public void SkipButton()
    {
        relicsDeck.EraseDeck();
        playerDeck.SetupDeck();
        enemyDeck.SetupEnemyDeck();
        this.gameObject.SetActive(false);
    }
}
