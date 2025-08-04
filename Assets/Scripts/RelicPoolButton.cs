using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelicPoolButton : MonoBehaviour
{
    bool isObjectActive = false;
    public GameObject relicPool;
    public void ToggleButton()
    {
        isObjectActive = !isObjectActive;
        relicPool.SetActive(isObjectActive);
    }
}
