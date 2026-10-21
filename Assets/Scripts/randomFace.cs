using UnityEngine;
using UnityEngine.InputSystem;


public class randomFace : MonoBehaviour
{
    //Array that hold all of the Card assets
    public Texture2D[] cardFace;
    

    public GameObject cardPrefab;

    // How far apart each new card spawns along the X axis
    public float cardSpacing = 0.7f;

    // Counts how many cards have been spawned, used to offset each new one
    private int cardsSpawned = 0;


    // Update is called once per frame
    void Update()
    {

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            
            Vector3 spawnPosition = new Vector3(cardsSpawned * cardSpacing, 0, 0);
            SpawnCard(spawnPosition);

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

            cardsSpawned++;


            //This logs the name of the randomly selected card face
            string cardName = cardFace[randomIndex].name;
            string[] cardParts = cardName.Split('_'); //Spilts the card name
            string rank = cardParts[0];

            Debug.Log(rank);

            

            
            
            
            
         }



        
    }
}
