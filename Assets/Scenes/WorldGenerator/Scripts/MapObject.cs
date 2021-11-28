using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //objects
public class MapObject
{
    private int maxWidth;
    private int maxHeight;
    private const int propertiesCount = 2;

    //properties
    public int[,] elevationMap;
    public int[,] temperatureMap;
    //public int[,] rainfallMap;
    //public int[,] floraMap;

    //percentiles
    private int[,] deciles = new int[propertiesCount, 10];
    public MapObject(int x, int y)
    {
        maxWidth = x;
        maxHeight = y;
        elevationMap = new int[x, y];
        temperatureMap = new int[x, y];
    }
    public int GetSeaLevel()
    {
        return deciles[0, 5]; //sea level is at 60% of the elevation set
    }

    public int GetTemperate()
    {
        return deciles[1, 6]; //temperature from 70%
    }

    public Color GetColor(int x, int y)
    {
        Color tileColour;
        tileColour = new Color(GetRed(x, y), GetGreen(x, y), GetBlue(x, y));

        return tileColour; //sea level is at 60% of the elevation set
    }
    public float GetRed(int x, int y)
    {
        float red = 0;
        if (elevationMap[x, y] >= GetSeaLevel() && temperatureMap[x,y] >= GetTemperate())
        {
            red = (float)temperatureMap[x, y] / 255;
        }
        return red;
    }

    public float GetGreen(int x, int y)
    {
        float green = 0;
        if (elevationMap[x, y] >= GetSeaLevel())
        {
            green += (float)elevationMap[x, y] / 255;
        }

        return green;
    }
    public float GetBlue(int x, int y)
    {
        float blue = 0;
        if (elevationMap[x, y] < GetSeaLevel())
        {
            blue = 255;
        }
        //else if (rainfallMap[x, y] >= GetRain())
        //{
        //    blue = (float)rainfallMap[x, y] / 50;
        //}
        return blue;
    }

    public void SetDecile() //stores the 10 deciles for each property 
    {
        List<int> valueList = new List<int>(); //this is used to get the medians of the set, as a reference for where the sea level should be

        for (int x = 0; x < maxWidth; x++)
        {
            for (int y = 0; y < maxHeight; y++)
            {
                valueList.Add(elevationMap[x, y]);
                valueList.Add(temperatureMap[x, y]);
                //valueList.Add(rainfallMap[x, y]);
                //valueList.Add(floraMap[x, y]);
            }
        }


        for (int a = 0; a < propertiesCount; a++)
        {
            valueList.Sort();

            for (int b = 1; b <= 10; b++) //gets the value at each decile (0.1,0.2 etc.) This allows use of any of the deciles in the set
            {
                deciles[a, b - 1] = valueList[(int)Math.Floor((float)valueList.Count * ((float)b / 10)) - 1];
            }
        }

    }
}