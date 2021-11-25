using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //objects
public class MainScreenHandler : MonoBehaviour
{
    public Button perlinButton;
    public GameObject imageRef;
    int mapWidth = 6000;
    int mapHeight = 4000;
    MapObject currentMap;
    System.Random rnd = new System.Random();

    public WorldGenerator _PerlinObject;
    void Start()
    {
        currentMap = new MapObject(mapWidth, mapHeight);

        currentMap.elevationMap = new int[mapWidth, mapHeight]; //set up the map
        _PerlinObject = new WorldGenerator(mapWidth, mapHeight); //set up worldGenerator
        perlinButton.GetComponent<Button>().onClick.AddListener(GenerateImageOnClick); //attach the script to the button

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void GenerateImageOnClick()
    {
        currentMap.elevationMap = _PerlinObject.Generate(3.5, 0.05f, 0.1f, 0.5f, true); //fractal for elevation
        currentMap.temperatureMap = _PerlinObject.Generate(2, 0, 0.3f, 0.6f, false); //no fractal for temperature
        //_PerlinObject.Generate1DAddition(5, mapHeight / 10, mapHeight / 2, rnd.Next(mapHeight / 16, mapHeight / 14), rnd.Next(mapHeight / 10, mapHeight / 9), 50, ref currentMap.temperatureMap); //add equator
        //currentMap.rainfallMap = _PerlinObject.Generate(3, 0, 0.1f, 0.1f, false); //no fractal for rainfall
        //currentMap.floraMap = _PerlinObject.Generate(6, 0, 0, 0, false); //no fractal for flora
        currentMap.SetDecile();

        DisplayMap();
    }

    //Maybe consider moving these values to another location
    void DisplayMap()
    {
        Texture2D imageTexture = new Texture2D(mapWidth, mapHeight);
        Sprite sprite = Sprite.Create(imageTexture, new Rect(0, 0, mapWidth,mapHeight), Vector2.zero);
        imageRef.GetComponent<SpriteRenderer>().sprite = sprite;

        for (int x = 0; x < imageTexture.width; x++)
        {
            for (int y = 0; y < imageTexture.height; y++)
            {
                Color color = currentMap.GetColor(x, y);
                imageTexture.SetPixel(x, y, color);
            }
        }

        imageTexture.Apply();
    }

}
