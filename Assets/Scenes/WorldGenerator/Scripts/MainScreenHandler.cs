using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //objects
using BiomeData;
using WorldProperties;
using SaveLoad;
using PropertiesGenerator;
using Perlin;
using UnityEngine.SceneManagement; //Scene switching data


using System.Threading;
public class MainScreenHandler : MonoBehaviour
{
    //UI
    private Texture2D newTexture;
    private GameObject startScreen;
    private GameObject panelScreen;
    private GameObject provinceDetailsScreen;
    public GameObject Camera;
    public Text genStateText;
    public Button exitBtn;
    int mapWidth = 6000;
    int mapHeight = 4000;

    public GameObject startScreenPrefab;
    public GameObject panelPrefab;
    public GameObject provinceDetailsPrefab;
    public GameObject loadMapPrefab;

    private GameObject loadMap;

    private MapObject currentMap;
    private System.Random rnd = new System.Random();
    public enum State //Generation States
    {
        Inactive,
        Terrain,
        Temperature,
        Rainfall,
        Flora,
        Biomes,
        Dividing,
        Displaying,
    }
    int currentState = 0;

    private Thread threadProcess;
    private volatile bool threadRunning = false;
    private List<Action> queuedFunctions = new List<Action>(); //Stores functions to be ran - this allows for better use of multithreading.

    void Start()
    {
        exitBtn.onClick.AddListener(ExitScene);
        exitBtn.gameObject.SetActive(false);

        currentMap = new MapObject(mapWidth, mapHeight);

        loadMap = Instantiate(loadMapPrefab,null); //Create new start screen instance
        loadMap.name = "Map";

        startScreen = Instantiate(startScreenPrefab, Camera.transform, false); //Create new start screen instance
        startScreen.GetComponent<MenuComponents>().startGen.onClick.AddListener(StartGeneration);

        provinceDetailsScreen = Instantiate(provinceDetailsPrefab, Camera.transform, false); //Add panel to show province details. This automatically sets itself to invisible

        newTexture = new Texture2D(mapWidth,mapHeight);
        UpdateLabel();
    }

    void Update()
    {
        if (queuedFunctions.Count > 0)
        {
            foreach (Action item in queuedFunctions.ToArray()) //using an array instead of a list here allows the function to use values rather than references - meaning modifications can be done to the list without breaking this loop 
            {
                item();
                queuedFunctions.Remove(item);
            }
        }
    }
    void ExitScene()
    {
        SceneManager.LoadScene("Main Menu", LoadSceneMode.Single); //Opens the world generator scene in place of this scene
    }

    void StartGeneration()
    {
        Camera.GetComponent<CameraScript>().enabled = true; //Allows camera movement
        Destroy(startScreen);

        if(threadRunning == false)
        {
            threadProcess = new Thread(ImageProcedure); //Begin running of the generation thread
            threadProcess.Start();
            threadRunning = true;
        }

    }
    void SaveFile() //To be called after generation has ended
    {
        string filePath = SaveLoad.SavingScript.CreateFile(mapWidth, mapHeight); //Saving procedure

        Byte[] imageBytes = newTexture.EncodeToPNG();
        SaveLoad.SavingScript.CreateMap(filePath, ref imageBytes);
        SaveLoad.SavingScript.CreateProvinceMapping(filePath, ref currentMap.provinceSaveables);
        SaveLoad.SavingScript.CreateCultures(filePath, ref currentMap.cultures);
    }

    void UpdateLabel() //using this in a function called by the queuedFunctions array stops there from being unnecessary comparitors
    {
        genStateText.text = "State: " + (State)currentState;
    }

    void ImageProcedure()
    {
        currentState++;
        queuedFunctions.Add(UpdateLabel);

        WorldGenerator _PerlinObject = new WorldGenerator(mapWidth, mapHeight); //set up worldGenerator

        currentMap.SetProperty(_PerlinObject.Generate(3.5, 0.05f, 0.1f, 0.5f, true), 0); //Elevation fractal
        currentState++;
        queuedFunctions.Add(UpdateLabel);

        //Used to kill tmpTemp after usage
        {
            int[,] tmpTemp = new int[mapWidth, mapHeight];
            tmpTemp = _PerlinObject.Generate(2, 0, 0.3f, 0.6f, false); //no fractal for temperature
            _PerlinObject.Generate1DAddition(5, mapHeight / 10, mapHeight / 2, mapHeight / 8, mapHeight / 3, 50, ref tmpTemp); //add equator
            currentMap.SetProperty(tmpTemp, 1);
            tmpTemp = null; //designate as cleanable
        }

        currentState++;
        queuedFunctions.Add(UpdateLabel);

        currentMap.SetProperty(_PerlinObject.Generate(3, 0, 0.1f, 0.1f, false), 2); //no fractal for rain. Maybe change this
        currentState++;
        queuedFunctions.Add(UpdateLabel);

        currentMap.SetProperty(_PerlinObject.Generate(6, 0, 0, 0, false),3); //no fractal for flora
        currentState++;
        queuedFunctions.Add(UpdateLabel);

        _PerlinObject = null; //Kill perlin for memory space

        currentMap.SetBiomes();
        currentState++;
        queuedFunctions.Add(UpdateLabel);

        currentMap.SplitIntoChunks(ref rnd); //Splitting into map chunks
        currentState++;
        queuedFunctions.Add(UpdateLabel);

        queuedFunctions.Add(CreateChunkMap); //Create new map

        currentState = 0;
        queuedFunctions.Add(UpdateLabel);

        //Use garbage collector manually to clear up data due to the heavy memory usage impacts of this program
        GC.Collect();
        GC.WaitForPendingFinalizers();

        threadRunning = false;
        threadProcess.Join(); //Join the thread
    }

    void CreateChunkMap()
    {
        Color[] pixSet = new Color[mapWidth * mapHeight]; //1D set of pixels
        currentMap.IterateProvinces(ref pixSet, mapWidth, mapHeight, ref rnd); //Set all pixel values
        newTexture.SetPixels(pixSet, 0); //sets all pixels from the chunk values

        currentMap.SetProvinceSaveables(ref rnd); //Create saveable properties now the map has been generated, as tiledata is no longer needed
        SaveFile(); //Save data to new file
        panelScreen = Instantiate(panelPrefab, Camera.transform, false); //Add control panel
        loadMap.GetComponent<LoadMap>().ApplyProperties(mapWidth, mapHeight,ref currentMap.provinceSaveables, ref currentMap.cultures, ref panelScreen, ref provinceDetailsScreen, ref newTexture);
        loadMap.GetComponent<LoadMap>().StartMap();
        exitBtn.gameObject.SetActive(true); //reenable exit button
    }

}
