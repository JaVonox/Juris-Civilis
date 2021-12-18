using System;
using System.Collections;
using System.Collections.Generic;
using BiomeData; //Biome stuff
using UnityEngine;
using UnityEngine.UI; //objects

public class MapObject
{
    private int maxWidth;
    private int maxHeight;
    private const int propertiesCount = 4;

    //properties
    public TileData[,] tiles;

    //percentiles per property
    private int[,] deciles = new int[propertiesCount, 10]; //Elevation, Temperature, Rainfall, Flora

    public MapObject(int x, int y)
    {
        maxWidth = x;
        maxHeight = y;
        tiles = new TileData[maxWidth, maxHeight];

        for (int xIter = 0; xIter < maxWidth; xIter++) //Initialise all values of list
        {
            for (int yIter = 0; yIter < maxHeight; yIter++)
            {
                tiles[xIter, yIter] = new TileData();
            }
        }

    }

    public void SetProperty(int[,] set,int property)
    {
        //TODO there must be a better way to do this. Maybe using enums?
        if(property == 0)
        {
            for(int x=0;x<maxWidth;x++)
            {
                for(int y=0;y<maxHeight;y++)
                {
                    tiles[x, y]._height = set[x,y];
                }
            }
        }
        else if (property == 1)
        {
            for (int x = 0; x < maxWidth; x++)
            {
                for (int y = 0; y < maxHeight; y++)
                {
                    tiles[x, y]._temperature = set[x,y];
                }
            }
        }
        else if (property == 2)
        {
            for (int x = 0; x < maxWidth; x++)
            {
                for (int y = 0; y < maxHeight; y++)
                {
                    tiles[x, y]._rainfall = set[x,y];
                }
            }
        }
        else if (property == 3)
        {
            for (int x = 0; x < maxWidth; x++)
            {
                for (int y = 0; y < maxHeight; y++)
                {
                    tiles[x, y]._flora = set[x,y];
                }
            }
        }
    }

    public void SetBiomes() //Set biomes for the existing tile data
    {
        for (int x = 0; x < maxWidth; x++)
        {
            for (int y = 0; y < maxHeight; y++)
            {
                tiles[x, y].InitializeBiome(ref deciles);
            }
        }
    }

    public int GetSeaLevel()
    {
        return deciles[0, 5]; //sea level is at 60% of the elevation set
    }

    public int GetTemperate()
    {
        return deciles[1, 7]; //temperature from 80%
    }

    public Color GetColor(int x, int y)
    {
        Color tileColour;
        tileColour = tiles[x, y]._biomeType.GetColour();
        return tileColour; 
    }

    public void SetDecile() //stores the 10 deciles for each property 
    {
        List<int> valueList = new List<int>(); //this is used to get the medians of the set, as a reference for where the sea level should be

        for (int x = 0; x < maxWidth; x++)
        {
            for (int y = 0; y < maxHeight; y++) //Adds decile properties
            {
                valueList.Add(tiles[x, y]._height);
                valueList.Add(tiles[x, y]._temperature);
                valueList.Add(tiles[x, y]._rainfall);
                valueList.Add(tiles[x, y]._flora);
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