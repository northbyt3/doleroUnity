using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDeck : MonoBehaviour
{
    public HorizontalCardHolder horizontalCardHolder;


    public void SetupEnemyDeck()
    {
         horizontalCardHolder.SetupDeck();
    }
}
