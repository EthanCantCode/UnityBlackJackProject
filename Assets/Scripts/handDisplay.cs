using UnityEngine;
using TMPro;

// Shows the player's and dealer's hand values, read from randomFace's scores.
// Whatever deals the cards changes the scores, so this just watches them and redraws when they change.
// The dealer's face-down card isn't counted until it's flipped.
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

        if (cards.dealerShownScore != shownDealer)
        {
            shownDealer = cards.dealerShownScore;
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
