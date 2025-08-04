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
    private float currentTime;

    public TMP_Text timerText;
    private bool hasPlayerLockedIn, hasEnemyLockedIn;
    private bool hasPlayerChosenRelic, hasEnemyChosenRelic;
    private bool hasTimerEnded = false;

    private string timerString;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            hasEnemyChosenRelic = true;
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

 
}
