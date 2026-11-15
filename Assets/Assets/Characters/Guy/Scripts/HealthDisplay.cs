using System.Collections;
using TMPro;
using UnityEngine;

public class HealthDisplay : MonoBehaviour
{
    public GameEventSO health;
    public TMP_Text label;

    private void Start()
    {
        label.color = Color.white;
    }

    public void refresh()
    {
        label.text = "Health: " + health.currentHealth;
        StopAllCoroutines();
        StartCoroutine(flashColor());
    }

    private IEnumerator flashColor()
    {
        label.color = Color.red;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // progress from 0 to 1 is the time passed divided by the total time
            float t = elapsed / duration;

            label.color = Color.Lerp(Color.red, Color.white, t);

            // wait for the next frame
            yield return null;
        }

        label.color = Color.white;
    }
}
