using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //objects
using System.Threading;

public class MainScreenHandler : MonoBehaviour
{
    public Button perlinButton;
    public GameObject imageRef;
    public Text genStateText;
    int mapWidth = 6000;
    int mapHeight = 4000;
    MapObject currentMap;
    System.Random rnd = new System.Random();

    public enum State
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

    private Thread generatorThread;
    private volatile bool generatorThreadRunning = false;
    private List<Action> queuedFunctions = new List<Action>(); //Stores functions to be ran - this allows for better use of multithreading.

    void Start()
    {
        currentMap = new MapObject(mapWidth, mapHeight);

        perlinButton.GetComponent<Button>().onClick.AddListener(GenerateImageOnClick); //attach the script to the button
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

    void GenerateImageOnClick()
    {
        if(generatorThreadRunning == false)
        {
            generatorThread = new Thread(ImageProcedure); //Begin running of the generation thread
            generatorThread.Start();
            generatorThreadRunning = true;
        }
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

        queuedFunctions.Add(DisplayChunkMap); //adds function display map to queued items

        currentState = 0;
        queuedFunctions.Add(UpdateLabel);

        //Use garbage collector manually to clear up data due to the heavy memory usage impacts of this program
        GC.Collect();
        GC.WaitForPendingFinalizers();

        generatorThreadRunning = false;
        generatorThread.Join(); //Join the thread
    }

    void DisplayChunkMap()
    {
        Texture2D imageTexture = new Texture2D(mapWidth, mapHeight);
        Sprite sprite = Sprite.Create(imageTexture, new Rect(0, 0, mapWidth, mapHeight), Vector2.zero);
        imageRef.GetComponent<SpriteRenderer>().sprite = sprite;

        Color[] PixelsSet = new Color[mapWidth * mapHeight]; //1D set of pixels
        currentMap.IterateProvinces(ref PixelsSet, mapWidth, mapHeight, ref rnd); //Set all pixel values
        imageTexture.SetPixels(PixelsSet, 0); //sets all pixels from the chunk values

        imageTexture.Apply();
    }

}
