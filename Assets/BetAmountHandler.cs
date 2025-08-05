using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BetAmountHandler : MonoBehaviour
{
    public List<Sprite> possibleNumbers;
    public List<Image> myNumbers;

    public int currentNumber;

    void TranslateNumber()
    {
        string formatted = currentNumber.ToString("0000");
        string cleaned = formatted.Replace(".", ""); // "0103"
        string[] digits = new string[4];
        for (int i = 0; i < digits.Length; i++)
        {
            digits[i] = cleaned[i].ToString();
        }

        switch (digits[0])
        {
            case "0":
                myNumbers[0].sprite = possibleNumbers[0];
                break;
            case "1":
                myNumbers[0].sprite = possibleNumbers[1];
                break;
            case "2":
                myNumbers[0].sprite = possibleNumbers[2];
                break;
            case "3":
                myNumbers[0].sprite = possibleNumbers[3];
                break;
            case "4":
                myNumbers[0].sprite = possibleNumbers[4];
                break;
            case "5":
                myNumbers[0].sprite = possibleNumbers[5];
                break;
            case "6":
                myNumbers[0].sprite = possibleNumbers[6];
                break;
            case "7":
                myNumbers[0].sprite = possibleNumbers[7];
                break;
            case "8":
                myNumbers[0].sprite = possibleNumbers[8];
                break;
            case "9":
                myNumbers[0].sprite = possibleNumbers[9];
                break;
        }

        switch (digits[1])
        {
            case "0":
                myNumbers[1].sprite = possibleNumbers[0];
                break;
            case "1":
                myNumbers[1].sprite = possibleNumbers[1];
                break;
            case "2":
                myNumbers[1].sprite = possibleNumbers[2];
                break;
            case "3":
                myNumbers[1].sprite = possibleNumbers[3];
                break;
            case "4":
                myNumbers[1].sprite = possibleNumbers[4];
                break;
            case "5":
                myNumbers[1].sprite = possibleNumbers[5];
                break;
            case "6":
                myNumbers[1].sprite = possibleNumbers[6];
                break;
            case "7":
                myNumbers[1].sprite = possibleNumbers[7];
                break;
            case "8":
                myNumbers[1].sprite = possibleNumbers[8];
                break;
            case "9":
                myNumbers[1].sprite = possibleNumbers[9];
                break;
        }

        switch (digits[2])
        {
            case "0":
                myNumbers[2].sprite = possibleNumbers[0];
                break;
            case "1":
                myNumbers[2].sprite = possibleNumbers[1];
                break;
            case "2":
                myNumbers[2].sprite = possibleNumbers[2];
                break;
            case "3":
                myNumbers[2].sprite = possibleNumbers[3];
                break;
            case "4":
                myNumbers[2].sprite = possibleNumbers[4];
                break;
            case "5":
                myNumbers[2].sprite = possibleNumbers[5];
                break;
            case "6":
                myNumbers[2].sprite = possibleNumbers[6];
                break;
            case "7":
                myNumbers[2].sprite = possibleNumbers[7];
                break;
            case "8":
                myNumbers[2].sprite = possibleNumbers[8];
                break;
            case "9":
                myNumbers[2].sprite = possibleNumbers[9];
                break;
        }

        switch (digits[3])
        {
            case "0":
                myNumbers[3].sprite = possibleNumbers[0];
                break;
            case "1":
                myNumbers[3].sprite = possibleNumbers[1];
                break;
            case "2":
                myNumbers[3].sprite = possibleNumbers[2];
                break;
            case "3":
                myNumbers[3].sprite = possibleNumbers[3];
                break;
            case "4":
                myNumbers[3].sprite = possibleNumbers[4];
                break;
            case "5":
                myNumbers[3].sprite = possibleNumbers[5];
                break;
            case "6":
                myNumbers[3].sprite = possibleNumbers[6];
                break;
            case "7":
                myNumbers[3].sprite = possibleNumbers[7];
                break;
            case "8":
                myNumbers[3].sprite = possibleNumbers[8];
                break;
            case "9":
                myNumbers[3].sprite = possibleNumbers[9];
                break;
        }

    }

    void Update()
    {
        TranslateNumber();
    }

    public void ChangeNumber(int number)
    {
        currentNumber = number;
    }
}
