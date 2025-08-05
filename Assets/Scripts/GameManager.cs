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

    public HorizontalLayoutGroup playedCardsGroup;
    public GameObject cardObject;
    public GameObject raiseBetScreen;

    void Awake()
    {
        Instance = this;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            hasEnemyChosenRelic = true;
            Test();
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

    public void FillScoreCardButton(int value)
    {
        StartCoroutine(FillScoreCardCoroutine(value));
    }

    IEnumerator FillScoreCardCoroutine(int scoreToFill)
    {
        //int i = 0;
        for (int i = 0; i <= scoreToFill; i++)
        {
            yield return new WaitForSeconds(0.05f);
            Debug.Log("Chupe");
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

    void Test()
    {
        Instantiate(cardObject, playedCardsGroup.transform);
    }

    public void OpenRaiseBetScreen()
    {
        raiseBetScreen.SetActive(true);
    }
    public void CloseRaiseBetScreen()
    {
        raiseBetScreen.SetActive(false);
    }
}
