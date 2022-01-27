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
    public GameObject Camera;

    int mapWidth = 6000;
    int mapHeight = 4000;
    
    public GameObject startPrefab;
    public GameObject panelPrefab;
    public GameObject provinceDetailsPrefab;
    public GameObject loadMapPrefab;

    private GameObject loadMap;
    private string filePath;
    private List<ProvinceObject> provinces;
    private List<Culture> cultures;

    private System.Random rnd = new System.Random();
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

            loadMap.GetComponent<LoadMap>().ApplyProperties(mapWidth, mapHeight, ref provinces, ref cultures, ref panelScreen, ref provinceDetailsScreen, ref mapTexture);
            loadMap.GetComponent<LoadMap>().StartMap();

            Camera.GetComponent<CameraScript>().enabled = true; //Allows camera movement

        }
        catch(Exception ex)
        {
            Debug.Log("CRASH! " + ex);
            //Add error handling here TODO
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    void ExitScene()
    {
        //Add Saving
        SceneManager.LoadScene("Main Menu", LoadSceneMode.Single); //Opens the world generator scene in place of this scene
    }
}
