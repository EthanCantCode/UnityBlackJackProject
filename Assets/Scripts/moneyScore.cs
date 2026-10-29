using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Money side of the game: wallet, bets, round multiplier, payouts, high score,
// plus the left dashboard and the bet controls on the bottom right.
//
// The round/hit script only needs two calls:
//   money.PlaceBet()             when a round starts (places the bet picked with the arrows)
//   money.ResolveRound(result)   when the round ends (Win, Blackjack, Push or Lose)
public class moneyScore : MonoBehaviour
{
    public enum RoundResult
    {
        Win,
        Blackjack,
        Push,
        Lose
    }

    [Header("Wallet")]
    public int wallet = 1000;
    public float minBet = 10f;
    public int currentBet = 0;

    [Header("Rounds")]
    public int roundNumber = 0;
    public bool gameOver = false;

    readonly float[] minBetCycle = new float[] { 1.5f, 1.5f, 2f };

    [Header("Round Multiplier")]
    public float minRandomMultiplier = 1.0f;
    public float maxRandomMultiplier = 5.0f;

    public float winBaseMultiplier = 1.0f;
    public float blackjackBaseMultiplier = 1.5f;

    public float currentMultiplier = 1.0f;

    [Header("Last Round")]
    public int lastPayout = 0;    // what went back into the wallet
    public int lastWinAmount = 0; // payout minus the bet (negative on a loss)

    [Header("High Score")]
    public int highScore = 0;     // highest wallet ever, saved between plays

    [Header("Bet Controls (bottom right)")]
    public int betAmount = 10;    // bet picked with the arrows, placed when a round starts
    // How much the up/down arrows change the bet. The step buttons pick one of these
    public int[] betSteps = new int[] { 10, 50, 100, 1000 };
    public int stepIndex = 0;

    public TMP_Text betText;   // the bet amount between the arrows
    public TMP_Text stepText;  // how much each arrow press changes the bet
    public Image[] stepButtons; // highlights the step that's picked
    public Color stepSelectedColor = new Color(1f, 0.85f, 0.2f);
    public Color stepNormalColor = new Color(0.8f, 0.8f, 0.8f);

    [Header("Left Dashboard (top to bottom)")]
    public TMP_Text walletText;
    public TMP_Text payoutText;
    public TMP_Text winAmtText;
    public TMP_Text betMinText;
    public TMP_Text multText;
    public TMP_Text roundText;
    public TMP_Text highscoreText;

    void Awake()
    {
        highScore = PlayerPrefs.GetInt("HighScore", wallet);

        // Multiplier for the first round, shown while the player picks their bet
        RollMultiplier();
    }

    void Start()
    {
        ClampBet();
        UpdateDisplay();
    }

    // Places the bet picked with the arrows. Call this when a round starts
    public bool PlaceBet()
    {
        ClampBet();
        return PlaceBet(betAmount);
    }

    public bool PlaceBet(int amount)
    {
        if (gameOver)
        {
            Debug.Log("Game over - no more bets");
            return false;
        }

        if (currentBet > 0)
        {
            Debug.Log("A bet is already placed this round");
            return false;
        }

        if (amount < minBet || amount > wallet)
        {
            Debug.Log("Invalid bet amount - must be at least " + minBet + " and no more than your wallet");
            return false;
        }

        currentBet = amount;
        wallet -= amount;
        UpdateDisplay();
        return true;
    }

    public void ResolveRound(RoundResult result)
    {
        roundNumber++;
        lastPayout = 0;

        switch (result)
        {
            case RoundResult.Win:
                Payout(winBaseMultiplier);
                break;

            case RoundResult.Blackjack:
                Payout(blackjackBaseMultiplier);
                break;

            case RoundResult.Push:
                wallet += currentBet;
                lastPayout = currentBet;
                Debug.Log("Push - bet returned");
                break;

            case RoundResult.Lose:
                Debug.Log("Loss");
                break;
        }

        lastWinAmount = lastPayout - currentBet;
        currentBet = 0;

        if (wallet > highScore)
        {
            highScore = wallet;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }

        int cycleIndex = (roundNumber - 1) % minBetCycle.Length;
        minBet *= minBetCycle[cycleIndex];
        CheckGameOver();

        // New multiplier for the next round, so the player sees it before betting
        RollMultiplier();

        // The wallet and min bet just changed, so the next bet may need adjusting
        ClampBet();
        UpdateDisplay();
    }

    void CheckGameOver()
    {
        if (!gameOver && minBet > wallet)
        {
            gameOver = true;
            Debug.Log("Game over - min bet (" + minBet.ToString("0.00") + ") exceeds wallet (" + wallet + ")");
        }
    }

    // Pays the round's multiplier, times the base (1x for a win, 1.5x for a blackjack)
    void Payout(float baseMultiplier)
    {
        float multiplier = currentMultiplier * baseMultiplier;
        int payout = Mathf.RoundToInt(currentBet * multiplier);
        wallet += payout;
        lastPayout = payout;
        Debug.Log("Win! Multiplier: " + multiplier.ToString("0.00") + "x, Payout: " + payout);
    }

    void RollMultiplier()
    {
        currentMultiplier = Random.Range(minRandomMultiplier, maxRandomMultiplier);
    }

    // Up arrow
    public void BetUp()
    {
        if (currentBet > 0)
        {
            return; // the bet is locked in once the round has started
        }

        betAmount += betSteps[stepIndex];
        ClampBet();
        UpdateDisplay();
    }

    // Down arrow
    public void BetDown()
    {
        if (currentBet > 0)
        {
            return;
        }

        betAmount -= betSteps[stepIndex];
        ClampBet();
        UpdateDisplay();
    }

    // Step buttons (0 = first step in betSteps, 1 = second, ...)
    public void SetStep(int index)
    {
        stepIndex = Mathf.Clamp(index, 0, betSteps.Length - 1);
        UpdateDisplay();
    }

    // Keeps the bet between the min bet and what's in the wallet
    // (with $1000 left, +$1000 from $500 gives $1000, not $1500)
    void ClampBet()
    {
        betAmount = Mathf.Min(betAmount, wallet);
        betAmount = Mathf.Max(betAmount, Mathf.CeilToInt(minBet));
    }

    void UpdateDisplay()
    {
        // Left dashboard, top to bottom. Each value goes in its own box on the table
        SetText(walletText, "Wallet\n$" + wallet);
        SetText(payoutText, "Payout\n$" + lastPayout);
        SetText(winAmtText, "Win\n$" + lastWinAmount);
        SetText(betMinText, "Bet Min\n$" + Mathf.CeilToInt(minBet));
        SetText(multText, "Mult\n" + currentMultiplier.ToString("0.00") + "x");
        SetText(roundText, gameOver ? "GAME OVER" : "Round\n" + roundNumber);
        SetText(highscoreText, "Highscore\n$" + highScore);

        // Bottom right bet controls. During a round, show the bet that was placed
        SetText(betText, "Bet $" + (currentBet > 0 ? currentBet : betAmount));
        SetText(stepText, "+/- $" + betSteps[stepIndex]);

        if (stepButtons != null)
        {
            for (int i = 0; i < stepButtons.Length; i++)
            {
                if (stepButtons[i] != null)
                {
                    stepButtons[i].color = i == stepIndex ? stepSelectedColor : stepNormalColor;
                }
            }
        }
    }

    void SetText(TMP_Text box, string text)
    {
        if (box != null)
        {
            box.text = text;
        }
    }

    // Testing without the hit script: in Play mode, right-click the Money Score header in the Inspector
    [ContextMenu("Test/Place Bet")]
    void TestPlaceBet() => PlaceBet();

    [ContextMenu("Test/Win")]
    void TestWin() => ResolveRound(RoundResult.Win);

    [ContextMenu("Test/Blackjack")]
    void TestBlackjack() => ResolveRound(RoundResult.Blackjack);

    [ContextMenu("Test/Push")]
    void TestPush() => ResolveRound(RoundResult.Push);

    [ContextMenu("Test/Lose")]
    void TestLose() => ResolveRound(RoundResult.Lose);
}
