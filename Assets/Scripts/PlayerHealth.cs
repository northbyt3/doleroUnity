using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int currentHealth;
    public List<Image> hearts;
    public GameObject heartPrefab;
    public Transform container;
    // Start is called before the first frame update
    void Start()
    {
        currentHealth = 3;
        for (int i = 0; i < currentHealth; i++)
        {
            GameObject go = Instantiate(heartPrefab, container.position, Quaternion.identity, container);
            hearts.Add(go.GetComponent<Image>());
        }
    }

    public void TakeDamage()
    {
        Destroy(hearts[0].gameObject);
        hearts.RemoveAt(0);
        if (hearts.Count <= 0)
        {
            //Trigger End Match
        }
    }
}
