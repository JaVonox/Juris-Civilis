using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //objects
using BiomeData;
using SaveLoad;

using System.Threading;
public class MainScreenHandler : MonoBehaviour
{
    //UI
    private GameObject startScreen;
    public GameObject Camera;
    public Button provinceButton;
    public Text genStateText;

    //Mapping elements
    int mapWidth = 6000;
    int mapHeight = 4000;
    public GameObject backMap;
    public GameObject loadedObjectsLayer;
    Texture2D backTexture;
    Sprite mapSprite;

    //Gets the unit length of the sprite
    float spriteWidth;
    float spriteHeight;

    //Script elements
    public GameObject provincePrefab;
    public GameObject startScreenPrefab;

    MapObject currentMap;
    System.Random rnd = new System.Random();

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
    public enum MapStates
    {
        Invalid = 0,
        Geography = 1,
        Province = 2,
    }

    int currentState = 0;
    MapStates curMapState = (MapStates)0;

    private Thread threadProcess;
    private volatile bool threadRunning = false;
    private List<Action> queuedFunctions = new List<Action>(); //Stores functions to be ran - this allows for better use of multithreading.

    void Start()
    {
        currentMap = new MapObject(mapWidth, mapHeight);

        startScreen = Instantiate(startScreenPrefab, Camera.transform, false); //Create new start screen instance
        startScreen.GetComponent<MenuComponents>().startGen.onClick.AddListener(StartGeneration);

        provinceButton.GetComponent<Button>().onClick.AddListener(ShowProvinces); //attach the script to the button
        InitialiseMaps(); //Sets default map elements
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

    void InitialiseMaps()
    {
        backTexture = new Texture2D(mapWidth, mapHeight);

        mapSprite = Sprite.Create(backTexture, new Rect(0, 0, mapWidth, mapHeight), Vector2.zero);
        backMap.GetComponent<SpriteRenderer>().sprite = mapSprite;

        Color[] pixSet = new Color[mapWidth * mapHeight]; //1D set of pixels
        
        for(int i=0; i< mapWidth * mapHeight;i++)
        {
            pixSet[i] = new Color(0.61f, 0.4f, 0.23f); //Default colour
        }

        backTexture.SetPixels(pixSet, 0); //sets all the pixels
        backTexture.Apply();

        Bounds spriteBounds = mapSprite.bounds; //Get boundaries of the sprite in units
        spriteWidth = spriteBounds.size.x;
        spriteHeight = spriteBounds.size.y;


    }

    void SaveFile() //To be called after generation has ended
    {
        string filePath = SaveLoad.SavingScript.CreateFile(mapWidth, mapHeight); //Saving procedure

        Byte[] imageBytes = backTexture.EncodeToPNG();
        SaveLoad.SavingScript.CreateMap(filePath, ref imageBytes);
        SaveLoad.SavingScript.CreateProvinceMapping(filePath, ref currentMap.provinceSaveables);
    }

    void KillProvinces()
    {
        foreach (Transform provinceChild in loadedObjectsLayer.transform) //remove all loaded provinces from the dataset
        {
            GameObject.Destroy(provinceChild.gameObject);
        }
    }
    void ShowProvinces()
    {
        if ((int)curMapState != 2) //Toggle
        {
            KillProvinces();
            foreach (ProvinceObject tProv in currentMap.provinceSaveables) //Loop through and display all provinces
            {
                GameObject newProvinceObject = Instantiate(provincePrefab, loadedObjectsLayer.transform, false); //Instantiate in local space of parent
                newProvinceObject.GetComponent<ProvinceRenderer>().RenderProvinceFromObject(tProv, spriteWidth, spriteHeight, mapWidth, mapHeight);

                Vector3 newCentre = newProvinceObject.GetComponent<ProvinceRenderer>().ReturnCentreUnitSpace(spriteWidth, spriteHeight, mapWidth, mapHeight);
                newProvinceObject.transform.Translate(newCentre.x, newCentre.y, newCentre.z); //set the unitspace based provinces
                newProvinceObject.transform.Rotate(180, 180, 0); //Flip to correct orientation
            }

            curMapState = (MapStates)2;
        }
        else if((int)curMapState == 2)
        {
            KillProvinces();
            curMapState = (MapStates)1;
        }
    }

    void UpdateLabel() //using this in a function called by the queuedFunctions array stops there from being unnecessary comparitors
    {
        genStateText.text = "State: " + (State)currentState;
    }

    void ImageProcedure()
    {
        if((int)curMapState != 1 && (int)curMapState != 0) { KillProvinces(); }
        curMapState = (MapStates)0;

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

        queuedFunctions.Add(CreateChunkMap); //Blocking operation

        currentState = 0;
        queuedFunctions.Add(UpdateLabel);
        curMapState = (MapStates)1;

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
        backTexture.SetPixels(pixSet, 0); //sets all pixels from the chunk values
        backTexture.Apply();

        currentMap.SetProvinceSaveables(); //Create saveable properties now the map has been generated, as tiledata is no longer needed
        SaveFile(); //Save data to new file
    }

}
