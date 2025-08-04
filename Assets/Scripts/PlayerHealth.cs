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

    private int heartIndex = 2;
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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            TakeDamage();
        }
    }

    public void TakeDamage()
    {
        hearts[heartIndex].GetComponent<Animator>().Play("Heart_Damage");
        heartIndex--;
        //Destroy(hearts[0].gameObject);
        //hearts.RemoveAt(0);
        if (heartIndex <= -1)
        {
            //Trigger End Match
        }
    }
}
