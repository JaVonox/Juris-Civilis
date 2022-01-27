using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //contains buttons
using UnityEngine.SceneManagement; //Scene switching data

public class UIBehaviour : MonoBehaviour
{
    public Button worldGenButton; //the world gen button
    public Button playButton;
    public Button exitButton;

    private AssetBundle sceneAssets;
    private string[] paths;
    void Start() //Initialisation
    {
        //Button starts
        worldGenButton.GetComponent<Button>().onClick.AddListener(WorldGenOnClick);
        playButton.GetComponent<Button>().onClick.AddListener(SimulateOnClick);
        exitButton.GetComponent<Button>().onClick.AddListener(ExitOnClick);
    }
    void WorldGenOnClick()
    {
        SceneManager.LoadScene("WorldGenerator", LoadSceneMode.Single); //Opens the world generator scene in place of this scene
    }
    void SimulateOnClick()
    {
        SceneManager.LoadScene("Simulator", LoadSceneMode.Single); //Opens the simulator scene
    }
    void ExitOnClick()
    {
        Application.Quit(); //Exits the app
    }
}
