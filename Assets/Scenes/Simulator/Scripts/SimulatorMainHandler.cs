using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; //Scene switching data
using WorldProperties;
public class SimulatorMainHandler : MonoBehaviour
{
    public Button exitButton;
    //UI
    private Texture2D mapTexture;
    private GameObject panelScreen;
    private GameObject provinceDetailsScreen;
    private GameObject startScreen;
    private GameObject consoleObject;
    public GameObject Camera;

    int mapWidth = 6000;
    int mapHeight = 4000;

    public GameObject startPrefab;
    public GameObject panelPrefab;
    public GameObject provinceDetailsPrefab;
    public GameObject loadMapPrefab;
    public GameObject consolePrefab;

    private GameObject loadMap;
    private string filePath;
    private List<ProvinceObject> provinces;
    private List<Culture> cultures;

    private System.Random rnd = new System.Random();
    private bool consoleIsActive = true;
    void Start()
    {
        provinces = new List<ProvinceObject>();
        cultures = new List<Culture>();
        exitButton.GetComponent<Button>().onClick.AddListener(ExitScene);
        Camera.GetComponent<CameraScript>().enabled = false; //Allows camera movement

        startScreen = Instantiate(startPrefab, Camera.transform, false); //Create new start screen instance
        startScreen.GetComponent<FileBrowserBehaviour>().startBtn.onClick.AddListener(delegate { StartLoad(); });
    }
    void StartLoad()
    {
        try
        {
            filePath = startScreen.GetComponent<FileBrowserBehaviour>().ReturnPath(); //append filepath to dataset

            {
                Dictionary<string, string> prop = SaveLoad.SavingScript.LoadBaseData(filePath);
                mapWidth = Convert.ToInt32(prop["Width"]);
                mapHeight = Convert.ToInt32(prop["Height"]);

                mapTexture = new Texture2D(mapWidth, mapHeight);
                byte[] mapBytes = SaveLoad.SavingScript.LoadMap(filePath, mapWidth, mapHeight);
                mapTexture.LoadImage(mapBytes);

                SaveLoad.SavingScript.LoadProvinces(filePath, ref provinces);
                SaveLoad.SavingScript.LoadCultures(filePath, ref cultures);
            }

            Destroy(startScreen); //remove screen from memory after getting applicable data

            loadMap = Instantiate(loadMapPrefab, null); //Create new map instance
            loadMap.name = "Map";

            panelScreen = Instantiate(panelPrefab, Camera.transform, false); //Add control panel
            provinceDetailsScreen = Instantiate(provinceDetailsPrefab, Camera.transform, false); //Add provViewer
            consoleObject = Instantiate(consolePrefab, Camera.transform, false); //Add console
            consoleObject.GetComponent<ConsoleScript>().LoadConsole(ref provinceDetailsScreen);
            ToggleConsole();

            loadMap.GetComponent<LoadMap>().ApplyProperties(mapWidth, mapHeight, ref provinces, ref cultures, ref panelScreen, ref provinceDetailsScreen, ref mapTexture);
            loadMap.GetComponent<LoadMap>().StartMap();

        }
        catch (Exception ex)
        {
            Debug.Log("CRASH! " + ex);
            //Add error handling here TODO
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote) && consoleObject != null) { ToggleConsole(); } //on ` key press open console
    }
    void ToggleConsole()
    {
        consoleIsActive = !consoleIsActive;

        //Toggle camera mode and console mode with tilde

        consoleObject.gameObject.SetActive(consoleIsActive);
        Camera.GetComponent<CameraScript>().enabled = !consoleIsActive;

        if(consoleIsActive)
        {
            consoleObject.GetComponent<ConsoleScript>().ResetInput();
            consoleObject.GetComponent<ConsoleScript>().textInput.ActivateInputField(); //Automatically selects the input field
        }
    }
    void ExitScene()
    {
        //Add Saving
        SceneManager.LoadScene("Main Menu", LoadSceneMode.Single); //Opens the world generator scene in place of this scene
    }
}
