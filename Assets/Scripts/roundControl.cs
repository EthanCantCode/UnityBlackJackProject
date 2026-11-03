using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// Round flow for the Deal, Hit and Stand buttons, on top of randomFace's dealing.
//   Deal   places the bet picked with the arrows, clears the table and deals the opening hand
//   Hit    deals the player another card (space bar works too)
//   Stand  flips the dealer's face-down card, plays out the dealer's hand (draws until 17)
//          and pays the round out through moneyScore
// When a round ends the result is shown for a moment, then the table clears
// and the bet can be changed before Deal starts the next one.
public class roundControl : MonoBehaviour
{
    public enum Phase
    {
        Betting,
        Dealing,
        PlayerTurn,
        DealerTurn,
        Result
    }

    public randomFace cards;
    public moneyScore money;

    public Button dealButton;
    public Button hitButton;
    public Button standButton;

    // The dealer keeps drawing while under this
    public int dealerStandsOn = 17;

    // Box in the middle of the table that pops in with the round result.
    // resultBox is the background (it sizes itself to the text), resultText sits inside it
    public CanvasGroup resultBox;
    public TMP_Text resultText;

    // How long the result and the cards stay up before the table clears
    public float resultShowTime = 1.5f;

    // Left hand, with Ethan's NewRound clip: it reaches down and sweeps across the table between rounds.
    // The cards are cleared at sweepClearTime, when the sweep reaches the far side
    public Animation leftHand;
    public float sweepClearTime = 0.67f;

    // How much the DEAL button grows and shrinks while it's waiting to be pressed
    public float dealPulseSize = 0.06f;
    public float dealPulseSpeed = 4f;

    public Phase phase = Phase.Betting;

    Vector3 dealButtonScale = Vector3.one;

    void Start()
    {
        if (dealButton != null)
        {
            dealButtonScale = dealButton.transform.localScale;
        }

        HideMessage();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Hit();
        }

        // Catches a bust from a hit or from the opening deal
        if (phase == Phase.PlayerTurn && !cards.isDealing && cards.pScore > 21)
        {
            EndRound(moneyScore.RoundResult.Lose);
        }

        // Hitting to 21 can't be improved on, so stand for the player automatically
        if (phase == Phase.PlayerTurn && !cards.isDealing && cards.pScore == 21)
        {
            StartCoroutine(DealerTurn());
        }

        // Buttons only work in their phase, and not while a card is moving
        bool canAct = phase == Phase.PlayerTurn && !cards.isDealing;
        bool canDeal = phase == Phase.Betting && !money.gameOver;
        SetInteractable(dealButton, canDeal);
        SetInteractable(hitButton, canAct);
        SetInteractable(standButton, canAct);

        // Pulse DEAL while it's ready, so the player knows what to press without a prompt
        if (dealButton != null)
        {
            float pulse = canDeal ? 1f + Mathf.Sin(Time.time * dealPulseSpeed) * dealPulseSize : 1f;
            dealButton.transform.localScale = dealButtonScale * pulse;
        }
    }

    // Deal button
    public void Deal()
    {
        if (phase != Phase.Betting || cards.isDealing)
        {
            return;
        }

        // PlaceBet logs why if the bet can't be placed (game over, not enough money)
        if (!money.PlaceBet())
        {
            return;
        }

        StartCoroutine(OpeningDeal());
    }

    IEnumerator OpeningDeal()
    {
        phase = Phase.Dealing;
        HideMessage();
        cards.ResetHands();
        yield return cards.StartCoroutine(cards.startOfRound());

        // Natural blackjack: no hitting or standing, the round ends right away.
        // Flip the dealer's card first - if the dealer has blackjack too it's a push
        if (cards.pScore == 21)
        {
            cards.RevealHiddenCard();
            EndRound(cards.dScore == 21 ? moneyScore.RoundResult.Push : moneyScore.RoundResult.Blackjack);
            yield break;
        }

        phase = Phase.PlayerTurn;
    }

    // Hit button
    public void Hit()
    {
        if (phase != Phase.PlayerTurn || cards.isDealing)
        {
            return;
        }

        cards.StartCoroutine(cards.DealCard(true));
    }

    // Stand button
    public void Stand()
    {
        if (phase != Phase.PlayerTurn || cards.isDealing)
        {
            return;
        }

        StartCoroutine(DealerTurn());
    }

    IEnumerator DealerTurn()
    {
        phase = Phase.DealerTurn;
        cards.RevealHiddenCard();

        while (cards.dScore < dealerStandsOn)
        {
            yield return cards.StartCoroutine(cards.DealCard(false));
        }

        EndRound(Result());
    }

    // Who won, once the player has stood and the dealer is done drawing
    // (a natural blackjack never gets here - OpeningDeal ends the round right away)
    moneyScore.RoundResult Result()
    {
        int player = cards.pScore;
        int dealer = cards.dScore;

        if (dealer > 21 || player > dealer)
        {
            return moneyScore.RoundResult.Win;
        }
        if (player == dealer)
        {
            return moneyScore.RoundResult.Push;
        }
        return moneyScore.RoundResult.Lose;
    }

    void EndRound(moneyScore.RoundResult result)
    {
        // Show the dealer's whole hand at the end, even when the player busted
        cards.RevealHiddenCard();
        Debug.Log("Round over: " + result + " (you " + cards.pScore + ", dealer " + cards.dScore + ")");
        money.ResolveRound(result);
        StartCoroutine(ShowResult(result));
    }

    // Pops the result in, leaves the cards up for a moment, fades it out and clears the table
    IEnumerator ShowResult(moneyScore.RoundResult result)
    {
        phase = Phase.Result;

        Color red = new Color(1f, 0.35f, 0.3f);
        switch (result)
        {
            case moneyScore.RoundResult.Blackjack:
                ShowMessage("BLACKJACK! +$" + money.lastWinAmount, new Color(1f, 0.85f, 0.2f));
                break;
            case moneyScore.RoundResult.Win:
                ShowMessage("WIN +$" + money.lastWinAmount, new Color(0.3f, 0.9f, 0.4f));
                break;
            case moneyScore.RoundResult.Push:
                ShowMessage("PUSH", Color.white);
                break;
            case moneyScore.RoundResult.Lose:
                ShowMessage(cards.pScore > 21 ? "BUST" : "DEALER WINS", red);
                break;
        }

        // Pop in: start big and settle to normal size
        float popTime = 0.15f;
        for (float t = 0; t < popTime; t += Time.deltaTime)
        {
            SetMessageLook(Mathf.Lerp(1.4f, 1f, t / popTime), 1f);
            yield return null;
        }
        SetMessageLook(1f, 1f);

        float fadeTime = 0.4f;
        yield return new WaitForSeconds(Mathf.Max(0f, resultShowTime - popTime - fadeTime));

        // Fade out
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            SetMessageLook(1f, 1f - t / fadeTime);
            yield return null;
        }
        HideMessage();

        yield return StartCoroutine(SweepTable());
        phase = Phase.Betting;

        // Game over stays on screen, since there's nothing left to deal
        if (money.gameOver)
        {
            ShowMessage("GAME OVER", red);
        }
    }

    // Plays the left hand's NewRound sweep and clears the cards as it passes over them.
    // Without the hand (or the clip), the cards are just cleared
    IEnumerator SweepTable()
    {
        AnimationState sweep = leftHand != null ? leftHand["NewRound"] : null;
        if (sweep == null)
        {
            cards.ResetHands();
            yield break;
        }

        // The clip is set to loop, so play it once
        sweep.wrapMode = WrapMode.Once;
        leftHand.Stop();
        leftHand.Play("NewRound");

        yield return new WaitForSeconds(sweepClearTime);
        cards.ResetHands();
        yield return new WaitForSeconds(Mathf.Max(0f, sweep.length - sweepClearTime));

        leftHand["Idle"].wrapMode = WrapMode.Loop;
        leftHand.Play("Idle");
    }

    void ShowMessage(string text, Color color)
    {
        if (resultBox == null)
        {
            return;
        }

        resultText.text = text;
        resultText.color = color;
        SetMessageLook(1f, 1f);
        resultBox.gameObject.SetActive(true);
    }

    // Scales and fades the box and its text together
    void SetMessageLook(float scale, float alpha)
    {
        if (resultBox != null)
        {
            resultBox.transform.localScale = Vector3.one * scale;
            resultBox.alpha = alpha;
        }
    }

    void HideMessage()
    {
        if (resultBox != null)
        {
            resultBox.gameObject.SetActive(false);
        }
    }

    void SetInteractable(Button button, bool on)
    {
        if (button != null)
        {
            button.interactable = on;
        }
    }
}
