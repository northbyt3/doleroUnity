using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelicsManager : MonoBehaviour
{
    public HorizontalCardHolder relicsDeck, playerDeck;
    public void PickButton()
    {
        relicsDeck.EraseDeck();
        playerDeck.SetupDeck();

    }
}
