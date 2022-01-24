using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //contains buttons
using UnityEngine.SceneManagement; //Scene switching data

public class UIBehaviour : MonoBehaviour
{
    public Button worldGenButton; //the world gen button
    private AssetBundle sceneAssets;
    private string[] paths;
    void Start() //Initialisation
    {
        worldGenButton.GetComponent<Button>().onClick.AddListener(WorldGenOnClick); //attach the script to the button
    }

    void Update()
    {
        
    }
    void WorldGenOnClick()
    {
        SceneManager.LoadScene("WorldGenerator", LoadSceneMode.Single); //Opens the world generator scene in place of this scene
    }
}
