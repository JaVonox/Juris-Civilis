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

    //properties
    public TileData[,] tiles;

    //percentiles per property
    private int[,] deciles = new int[4, 10]; //Elevation, Temperature, Rainfall, Flora

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

    public void SplitIntoChunks() //Splits the map into chunks (Squares/rects)
    {
        const int numOfChunks = 1000; //Number of chunks to be generated.
        Chunk[] worldChunks = new Chunk[numOfChunks];

        //should always be integer divisible by num of chunks.
        int chunkWidth = maxWidth / numOfChunks;
        int chunkHeight = maxHeight / numOfChunks;

        int iteration = -1; //start at -1 because first loop will always add 1

        //Iterate through all tiles and store the tile in the appropriate tileSlot
        for(int x=0;x<maxWidth;x++)
        {
            for(int y=0;y<maxHeight;y++)
            {
                if (y % chunkHeight == 0 && x % chunkWidth == 0) //If this exists on a new chunk border (Inwhich x and y are both on the chunk boundary, hence being top left)
                {
                    iteration++;
                    worldChunks[iteration] = new Chunk(x, y, maxWidth, maxHeight);
                }

                worldChunks[iteration].AddTile(x % chunkWidth, y % chunkHeight, ref tiles[x, y]); //appends the tile at x y into the chunks set
            }
        }

        Debug.Log("Number of chunks:" + iteration);
    }

    public void SetProperty(int[,] set,int property)
    {
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

    public Color GetColor(int x, int y)
    {
        Color tileColour;
        tileColour = tiles[x, y]._biomeType.GetColour();
        return tileColour; 
    }

    public void SetDecile() //stores the 10 deciles for each property 
    {
        //Sets of each property
        List<int> heightList = new List<int>();
        List<int> tempList = new List<int>();
        List<int> rainList = new List<int>();
        List<int> floraList = new List<int>();

        for (int x = 0; x < maxWidth; x++)
        {
            for (int y = 0; y < maxHeight; y++) //Adds decile properties
            {
                heightList.Add(tiles[x, y]._height);
                tempList.Add(tiles[x, y]._temperature);
                rainList.Add(tiles[x, y]._rainfall);
                floraList.Add(tiles[x, y]._flora);
            }
        }

        heightList.Sort();
        tempList.Sort();
        rainList.Sort();
        floraList.Sort();

        for (int b = 1; b <= 10; b++) //gets the value at each decile (0.1,0.2 etc.) This allows use of any of the deciles in the set
        {
            deciles[0, b - 1] = heightList[(int)Math.Floor((float)heightList.Count * ((float)b / 10)) - 1];
            deciles[1, b - 1] = tempList[(int)Math.Floor((float)tempList.Count * ((float)b / 10)) - 1];
            deciles[2, b - 1] = rainList[(int)Math.Floor((float)rainList.Count * ((float)b / 10)) - 1];
            deciles[3, b - 1] = floraList[(int)Math.Floor((float)floraList.Count * ((float)b / 10)) - 1];
        }

    }
}