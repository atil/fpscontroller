using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class Ui : MonoBehaviour
{
    public Text Timer;

    public void UpdateTimerWith(float secs)
    {
        Timer.gameObject.SetActive(true);
        Timer.text = secs.ToString("N");
    }

    public void ShowResultWith(float secs)
    {
        Timer.text = secs.ToString("N");
        StartCoroutine(WaitAndHideText());
    }

    private IEnumerator WaitAndHideText()
    {
        yield return new WaitForSeconds(5f);
        Timer.gameObject.SetActive(false);
    }

    public void HideTimer()
    {
        Timer.gameObject.SetActive(false);
    }
}
