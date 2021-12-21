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
        const int numOfChunks = 1024; //Number of chunks to be generated. Always must be square number
        Chunk[] worldChunks = new Chunk[numOfChunks];

        //should always be integer divisible by num of chunks.
        int chunkWidth = maxWidth / (int)Math.Sqrt(numOfChunks);
        int chunkHeight = maxHeight / (int)Math.Sqrt(numOfChunks);

        int iteration = -1; //start at -1 because first loop will always add 1

        //Iterate through all tiles and store the tile in the appropriate tileSlot
        for(int x=0;x<maxWidth;x++)
        {
            for(int y=0;y<maxHeight;y++)
            {
                if (y % chunkHeight == 0 && x % chunkWidth == 0) //If this exists on a new chunk border (Inwhich x and y are both on the chunk boundary, hence being top left)
                {
                    iteration++;
                    Debug.Log("iter: " + iteration);
                    Debug.Log("CREATING for (" + x + "," + y + ")");
                    worldChunks[iteration] = new Chunk(x, y, chunkWidth, chunkHeight);
                }
                worldChunks[iteration].AddTile(x % chunkWidth, y % chunkHeight, tiles[x, y]); //appends the tile at x y into the chunks set
            }
        }

        Debug.Log("Number of chunks:" + iteration);
    }

    public void SetProperty(int[,] set,int property)
    {
        SetDecile(property, ref set);

        for (int x = 0; x < maxWidth; x++)
        {
            for (int y = 0; y < maxHeight; y++)
            {
                tiles[x, y].DefineProperties(ref deciles, property, set[x, y]);
            }
        }

        set = null;

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

    public void SetDecile(int property, ref int[,] set) //stores the 10 deciles for the defined property
    {
        //This originally used actual deciles but now uses range-based calculations to minimize memory usage

        //Stores min/max for each property

        int max = -1;
        int min = -1;

        for (int x = 0; x < maxWidth; x++)
        {
            for (int y = 0; y < maxHeight; y++) //Adds decile properties
            {
                if (set[x, y] > max || max == -1)
                {
                    max = set[x, y];
                }
                if (set[x, y] < min || min == -1)
                {
                    min = set[x, y];
                }
            }
        }

        for (int b = 1; b <= 10; b++) //sets deciles based on range
        {
            deciles[property, b - 1] = (min) + (int)((float)(max - min) * ((float)b/10));
        }

    }
}