using UnityEngine;
using UnityEngine.InputSystem;


public class randomFace : MonoBehaviour
{
    //Array that hold all of the Card assets
    public Texture2D[] cardFace;
    
    public GameObject cardPrefab;

    //Object on where the cards where spawn
    public GameObject cardSpawn;

    public int pScore;//Player score
    public int dScore;//Dealer score

    // How far apart each new card spawns along the X axis
    public float cardSpacing = 0.7f;

    // Counts how many cards have been spawned, used to offset each new one
    private int pCardsSpawned = 0;
    private int dCardsSpawned = 0;


    // Update is called once per frame
    void Update()
    {
        //Spawns the player card when the key "space" is pressed
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            
            Vector3 spawnPosition = cardSpawn.transform.position + new Vector3(pCardsSpawned * cardSpacing, 0, 0);
            SpawnCard(spawnPosition);

        }

        //Spawns a dealer card when the key "d" is pressed
        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            Vector3 dSpawnPosition = cardSpawn.transform.position + new Vector3(dCardsSpawned * cardSpacing, 0, 0);
            spawnDealer(dSpawnPosition);
        }  
        
    }


    // Spawns a card at the given position and gives it a random face
        void SpawnCard(Vector3 position)
        {
            // Create a copy of the prefab in the scene
            GameObject newCard = Instantiate(cardPrefab, position, Quaternion.identity);

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
                pScore = 0;
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

            
            
         }



    //This section spawns the dealer cards
    void spawnDealer(Vector3 position)
    {
        
            GameObject newCard = Instantiate(cardPrefab, position, Quaternion.identity);

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


            //Basic blackjack check (will probably be removed in the future)
            if (dScore > 21)
            {   
                dScore = 0;
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



}
