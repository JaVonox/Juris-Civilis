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

    public WorldGenerator _PerlinObject;
    void Start()
    {
        _PerlinObject = new WorldGenerator(mapWidth, mapHeight); //set up worldGenerator
        perlinButton.GetComponent<Button>().onClick.AddListener(GenerateImageOnClick); //attach the script to the button
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void GenerateImageOnClick()
    {
        int[,] newMap = _PerlinObject.Generate(3.5, 0.05f, 0.1f, 0.5f, true);
        Debug.Log("Image Set");
        DisplayMap(0.6f, "Land/Sea", ref newMap);
    }

    //Maybe consider moving these values to another location
    void DisplayMap(float modifier, string type, ref int[,] map)
    {
        Texture2D imageTexture = new Texture2D(mapWidth, mapHeight);
        Sprite sprite = Sprite.Create(imageTexture, new Rect(0, 0, mapWidth,mapHeight), Vector2.zero);
        imageRef.GetComponent<SpriteRenderer>().sprite = sprite;

        List<int> typeStorage = new List<int>();  //This is used to store integer values which has variable meaning dependent on the type

        switch (type)
        {
            case "Land/Sea":
                typeStorage.Add(GetPercentile(0.6f, ref map)); //land vs sea
                typeStorage.Add(GetPercentile(0.95f, ref map)); //mountain region
                break;
            case "None":
                break;
            default:
                break;
        }

        for (int x = 0; x < imageTexture.width; x++)
        {
            for (int y = 0; y < imageTexture.height; y++)
            {
                if (type == "Land/Sea")
                {
                    LandType(ref imageTexture, x, y, typeStorage[0], typeStorage[1], ref map); //Sets land/sea and mountains
                }
                else if (type == "None")
                {
                    Color color = new Color((float)map[x, y] / 255, (float)map[x, y] / 255, (float)map[x, y] / 255);
                    imageTexture.SetPixel(x, y, color);
                }

            }
        }

        imageTexture.Apply();
    }

    public void LandType(ref Texture2D bmp, int x, int y, int seaLevel, int mountainLevel, ref int[,] map) //handles land/sea generation
    {
        if (map[x, y] >= mountainLevel)
        {
            Color color = new Color((float)map[x, y] / 255, (float)map[x, y] / 255, (float)map[x, y] / 255);
            bmp.SetPixel(x, y, color);
        }
        else if (map[x, y] >= seaLevel)
        {
            Color color = new Color(0, (float)map[x, y] / 255, 0);
            bmp.SetPixel(x, y, color);
        }
        else
        {
            Color color = new Color(0, 0, (float)map[x, y] / 255);
            bmp.SetPixel(x, y, color);
        }
    }

    public int GetPercentile(float modifier, ref int[,] map) //returns the value at a specific percentile (0 - 100)
    {
        List<int> percList = new List<int>(); //this is used to get the median of the set, as a reference for where the sea level should be

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                percList.Add(map[x, y]);
            }
        }

        percList.Sort();

        //Get sealevel modifier value quartile
        int seaLevel = percList[(int)Math.Floor((float)percList.Count * modifier)];
        return seaLevel;
    }
}
