using System.Collections;
using UnityEngine;
using System.Collections.Generic;



public class randomFace : MonoBehaviour
{
    //Array that hold all of the Card assets
    public Texture2D[] cardFace;
    
    public GameObject cardPrefab;

    //Object on where the cards where spawn
    public GameObject cardSpawn;

    public Transform playerHand;
    public Transform dealerHand;

    private GameObject hiddenCard;

    public float moveSpeed = 5f;

    // Every card currently in each hand (used to shift them over)
    public List<GameObject> playerCards = new List<GameObject>();
    public List<GameObject> dealerCards = new List<GameObject>();

    // True while a card is being dealt, so the player can't hit mid-deal
    public bool isDealing = false;

    public bool isFaceDown = true;

    //Dealing animation
    public Animation drawCard;
    public float dealTime;

    public int pScore;//Player score
    public int dScore;//Dealer score

    // How far apart each new card spawns along the X axis
    public float cardSpacing = 0.7f;

    // Counts how many cards have been spawned, used to offset each new one
    private int pCardsSpawned = 0;
    private int dCardsSpawned = 0;

    // Value of the face-down dealer card, kept out of the dealer's shown score until it's revealed
    private int hiddenValue;
    private int lastDealerValue;

    // Dealer's score without the face-down card (what the player is allowed to see)
    public int dealerShownScore
    {
        get { return hiddenCard != null ? dScore - hiddenValue : dScore; }
    }

    // The round is started by roundControl (Deal button), not when the scene loads
    public IEnumerator startOfRound()
    {

       // Opening hand: player, dealer, player, dealer (the dealer's second card stays face down)
    for (int i = 0; i < 2; i++)
    {
        yield return StartCoroutine(DealCard(true));
        //Debug.Log("drew player card");

        yield return StartCoroutine(DealCard(false, i == 1));
        //Debug.Log("Drew dealer card");
    }

    }

    // Clears the table and the scores before a new round
    public void ResetHands()
    {
        foreach (GameObject card in playerCards)
        {
            Destroy(card);
        }
        foreach (GameObject card in dealerCards)
        {
            Destroy(card);
        }

        playerCards.Clear();
        dealerCards.Clear();
        pScore = 0;
        dScore = 0;
        pCardsSpawned = 0;
        dCardsSpawned = 0;
        hiddenCard = null;
    }

    // Flips the dealer's face-down card, so its value counts in the shown score
    public void RevealHiddenCard()
    {
        if (hiddenCard != null)
        {
            hiddenCard.transform.rotation = Quaternion.identity;
            hiddenCard = null;
        }
    }


    // Spawns a card at the given position and gives it a random face
        GameObject spawnCard(Vector3 position)
        {
            // Create a copy of the prefab in the scene
            GameObject newCard = Instantiate(cardPrefab, position, Quaternion.Euler(0,180,0));

            // Pick a random index from 0 to 51 (max is excluded for ints)
            int randomIndex = Random.Range(0, cardFace.Length);

            // Find the child quad named "Face" on the new card
            Transform face = newCard.transform.Find("Face");

            // Swap that quad's texture to the randomly chosen card image
            face.GetComponent<MeshRenderer>().material.mainTexture = cardFace[randomIndex];

            pCardsSpawned++;


            //This logs the name of the randomly selected card face
            string cardName = cardFace[randomIndex].name;
            string[] cardParts = cardName.Split('_'); //Spilts the card name
            string cardValue = cardParts[0];

            int blackjackValue = cardCalc(cardValue);
            //Debug.Log(cardName + " = " + blackjackValue);
            pScore += blackjackValue;


            //Basic blackjack check (will probably be removed in the future)
            if (pScore > 21)
            {
                Debug.Log("bust");
            }
            else if(pScore == 21)
            {
                Debug.Log("BLACKJACK");
            }
            else
            {
                Debug.Log("Your score is " + pScore);
            }


            return newCard;
            
         }




    //This section spawns the dealer cards
    GameObject spawnDealer(Vector3 position)
    {
        
            GameObject newCard = Instantiate(cardPrefab, position, Quaternion.Euler(0,180,0));

            // Pick a random index from 0 to 51 (max is excluded for ints)
            int randomIndex = Random.Range(0, cardFace.Length);

            // Find the child quad named "Face" on the new card
            Transform face = newCard.transform.Find("Face");

            // Swap that quad's texture to the randomly chosen card image
            face.GetComponent<MeshRenderer>().material.mainTexture = cardFace[randomIndex];

            dCardsSpawned++;


            //This logs the name of the randomly selected card face
            string cardName = cardFace[randomIndex].name;
            string[] cardParts = cardName.Split('_'); //Spilts the card name
            string cardValue = cardParts[0];

            int blackjackValue = cardCalc(cardValue);
            dScore += blackjackValue;
            lastDealerValue = blackjackValue;


            //Basic blackjack check (will probably be removed in the future)
            if (dScore > 21)
            {
                Debug.Log("Dealer bust");
            }
            else if(dScore == 21)
            {
                Debug.Log("Dealer BLACKJACK");
            }
            else
            {
                Debug.Log("Dealer score is " + dScore);
            }
        return newCard;

    }

    int cardCalc(string cardAmt)
        {
            
        if (cardAmt == "ace")
        {
            return 11; 
        }        
        else if (cardAmt == "jack" || cardAmt == "queen" || cardAmt == "king")
        {
            return 10;
        }
        else
        {    
            return int.Parse(cardAmt);  
        }
        

        }


    // Deals one card to the player (true) or dealer (false), synced to the hand animation.
    // A faceDown card stays face down until RevealHiddenCard
public IEnumerator DealCard(bool toPlayer, bool faceDown = false)
{
    
    isDealing = true;
    float startTime = Time.time;

    // Frame 32 of the draw animation, converted to seconds
    float releaseTime = 32f / drawCard["draw"].clip.frameRate;

    // Start the hand animation from the beginning
    drawCard.Stop();
    drawCard["draw"].wrapMode = WrapMode.Once;
    drawCard.Play("draw");

    // Spawn the card on the deck and pick which hand it goes to
    GameObject newCard;
    List<GameObject> hand;
    Transform landingSpot;

    if (toPlayer)
    {
        newCard = spawnCard(cardSpawn.transform.position);
        hand = playerCards;
        landingSpot = playerHand;
    }
    else
    {
        newCard = spawnDealer(cardSpawn.transform.position);
        hand = dealerCards;
        landingSpot = dealerHand;

        if (faceDown)
        {
            hiddenCard = newCard;
            hiddenValue = lastDealerValue;
        }
    }

    // Wait until frame 32, when the hand releases the card
    yield return new WaitForSeconds(releaseTime);

    // Add the new card, then slide the whole hand into a centered row
    hand.Add(newCard);
    ArrangeHand(hand, landingSpot, !faceDown);

    // Wait long enough for the new card to reach its spot
    yield return new WaitForSeconds(0.5f);

    // Flip the card face up once it lands
    if (!faceDown)
    {
        newCard.transform.rotation = Quaternion.identity;
    }

    // Let the rest of the hand animation finish before the next deal
    float remaining = drawCard["draw"].length - (Time.time - startTime);
    if (remaining > 0)
    {
        yield return new WaitForSeconds(remaining);
    }

    drawCard.Stop();
    drawCard["RightIdle"].wrapMode = WrapMode.Loop;
    drawCard.Play("RightIdle");
    isDealing = false;
}

// Slides every card in a hand so they're evenly spaced and centered on the landing spot
void ArrangeHand(List<GameObject> hand, Transform center, bool flipNewest)
{
    for (int i = 0; i < hand.Count; i++)
    {
        float offset = (i - (hand.Count - 1) / 2f) * cardSpacing;
        Vector3 target = center.position + new Vector3(0, 0, offset);
        bool isNewest = flipNewest && (i == hand.Count - 1);
        StartCoroutine(MoveCard(hand[i], target, isNewest));
    }
}

// Slides a card toward the target a little each frame until it arrives
IEnumerator MoveCard(GameObject card, Vector3 target, bool flipWhenDone)
{
    while (Vector3.Distance(card.transform.position, target) > 0.01f)
    {
        card.transform.position = Vector3.MoveTowards(card.transform.position, target, moveSpeed * Time.deltaTime);
        yield return null; // continue next frame
    }

    // Snap exactly into place
    card.transform.position = target;

    if (flipWhenDone)
    {
        card.transform.rotation = Quaternion.identity;
    }
}
    



}
