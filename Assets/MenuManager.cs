using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public GameObject searchingMatch, loadingIcon;
    public TMP_Text timerText;

    private bool isSearching = false;

    float timer = 0;
    void Update()
    {
        if (!isSearching) return;
        timer += Time.deltaTime;
        timerText.text = timer.ToString("00:00");
    }
    public void SearchMatch()
    {
        searchingMatch.SetActive(true);
        loadingIcon.SetActive(true);
        isSearching = true;
    }
    public void CloseButton()
    {
        SceneManager.LoadScene("Menu");
    }
}
