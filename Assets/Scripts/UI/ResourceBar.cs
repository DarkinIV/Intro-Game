using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One of the player's bars (health, stamina or mana). Goes on the bar itself.
// The bar gets wider when the max goes up. When something is lost, the yellow part
// stays at the old value for a moment and then drains down.
public class ResourceBar : MonoBehaviour
{
    [SerializeField] Image fill;
    [SerializeField] Image lostFill;            // the yellow part behind the fill
    [SerializeField] TMP_Text valueText;        // optional
    [SerializeField] float pixelsPerPoint = 2f;

    [Header("Feedback")]
    [SerializeField] float lostHoldTime = 0.2f;
    [SerializeField] float lostDrainSpeed = 0.8f;
    [SerializeField] float gainFillSpeed = 0f;  // 0 jumps straight up, anything higher fills up slowly (used for healing)

    float target;
    float drainAfter;

    public void Set(float current, float max)
    {
        RectTransform rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(max * pixelsPerPoint, rect.sizeDelta.y);

        target = current / max;

        if (gainFillSpeed <= 0f || target < fill.fillAmount)
            fill.fillAmount = target;

        if (valueText != null)
            valueText.text = Mathf.FloorToInt(current) + " / " + Mathf.RoundToInt(max);
    }

    // previous is what the value was before it went down
    public void ShowLoss(float previous, float max)
    {
        lostFill.fillAmount = Mathf.Max(lostFill.fillAmount, previous / max);
        drainAfter = Time.unscaledTime + lostHoldTime;
    }

    void Update()
    {
        if (fill.fillAmount < target)
            fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, target, gainFillSpeed * Time.unscaledDeltaTime);

        if (Time.unscaledTime >= drainAfter)
            lostFill.fillAmount = Mathf.MoveTowards(lostFill.fillAmount, fill.fillAmount, lostDrainSpeed * Time.unscaledDeltaTime);
    }
}
