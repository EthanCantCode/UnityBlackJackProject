using UnityEngine;
using TMPro;

// Shows the player's and dealer's hand values, read from randomFace's scores.
// Whatever deals the cards (the hit script, or randomFace's test keys) changes the scores,
// so this just watches them and redraws when they change.
public class handDisplay : MonoBehaviour
{
    public randomFace cards;

    public TMP_Text playerValueText;
    public TMP_Text dealerValueText;

    // Last values drawn, so the text is only rebuilt when a score changes
    int shownPlayer = -1;
    int shownDealer = -1;

    void Update()
    {
        if (cards.pScore != shownPlayer)
        {
            shownPlayer = cards.pScore;
            SetText(playerValueText, "You: " + shownPlayer);
        }

        if (cards.dScore != shownDealer)
        {
            shownDealer = cards.dScore;
            SetText(dealerValueText, "Dealer: " + shownDealer);
        }
    }

    void SetText(TMP_Text box, string text)
    {
        if (box != null)
        {
            box.text = text;
        }
    }
}
