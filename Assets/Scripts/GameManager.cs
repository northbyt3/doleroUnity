using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [HideInInspector] public UnityEvent StandEvent;
    [HideInInspector] public UnityEvent RelicEvent;

    public float timer = 40f;
    [SerializeField] private float maxTime = 40f;
    [SerializeField] private Slider slider;
    [SerializeField] private int currentScore;
    [SerializeField] private Image scoreCard;
    [SerializeField] private List<Sprite> scoreCards;
    private float currentTime;

    public TMP_Text timerText;
    private bool hasPlayerLockedIn, hasEnemyLockedIn;
    private bool hasPlayerChosenRelic, hasEnemyChosenRelic;
    private bool hasTimerEnded = false;

    public GameObject victoryScreen, defeatScreen;

    private string timerString;

    public static GameManager Instance;
    public TMP_Text currentScoreText;

    public HorizontalCardHolder playerDeck;
    public GameObject cardObject;
    public GameObject raiseBetScreen;

    public TMP_Text playerName, enemyName;

    void Awake()
    {
        Instance = this;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            hasEnemyChosenRelic = true;
            //();
            //FillScoreCardButton();
        }
        if (hasEnemyChosenRelic && !hasTimerEnded)
        {
            timer -= Time.deltaTime;
            if (timer > 0)
            {
                timerString = timer.ToString("00.00");
                timerText.text = timerString + " sec";
                currentTime = Mathf.Clamp(timer, 0, maxTime);
                slider.value = currentTime / maxTime;
            }
            else
            {
                timerText.text = "0.0 sec";
                hasTimerEnded = true;
                if (!hasPlayerLockedIn)
                {
                    StandButton();
                }
            }
        }
    }
    public void RelicButton()
    {
        RelicEvent.Invoke();
    }
    public void StandButton()
    {
        StandEvent.Invoke();
    }

    public void VictoryScreen()
    {

    }

    public void FillScoreCardButton()
    {
        int value = 0;
        foreach (Card card in playerDeck.cards)
        {
            if (card.isPlayed)
            {
                value += card.cardValue;
            }
        }
        StartCoroutine(FillScoreCardCoroutine(value));
    }

    IEnumerator FillScoreCardCoroutine(int scoreToFill)
    {
        //int i = 0;
        for (int i = 0; i <= scoreToFill; i++)
        {
            yield return new WaitForSeconds(0.05f);
            FillScoreCard(i, scoreToFill);
        }
    }

    void FillScoreCard(int currentScore, int scoreToFill)
    {
        if (scoreToFill != 0)
        {
            scoreCard.sprite = scoreCards[currentScore];
            currentScoreText.text = currentScore.ToString();
            currentScore++;
            scoreToFill--;
        }
    }

    public void EmptyScoreCardButton(int value)
    {
        StartCoroutine(EmptyScoreCardCoroutine(value));
    }

    IEnumerator EmptyScoreCardCoroutine(int scoreToFill)
    {
        //int i = 0;
        for (int i = 0; i <= scoreToFill; i++)
        {
            yield return new WaitForSeconds(0.05f);
            EmptyScoreCard(i, scoreToFill);
        }
    }

    public void EmptyScoreCard(int currentScore, int scoreToFill)
    {
        currentScore = 0;
        scoreCard.sprite = scoreCards[currentScore];
        currentScoreText.text = currentScore.ToString();
    }

public void OpenRaiseBetScreen()
{
    raiseBetScreen.SetActive(true);
}
public void CloseRaiseBetScreen()
{
    raiseBetScreen.SetActive(false);
}


public void ChangeNames(string playerNameStr, string enemyNameStr)
{
    playerName.text = playerNameStr;
    enemyName.text = enemyNameStr;
}
}
