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

    //List of chunks
    public List<Chunk> worldChunks = new List<Chunk>();

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
        //should always be integer divisible by num of chunks.
        int chunkWidth = maxWidth / 100;
        int chunkHeight = maxHeight / 100;

        int iteration = 0; //chunk number

        for(int x=0;x<maxWidth;x+=chunkWidth) //Iterate through each chunk
        {
            for(int y=0;y<maxHeight;y+=chunkHeight)
            {
                worldChunks.Insert(iteration, new Chunk(x, y, chunkWidth, chunkHeight)); //add new chunk to the set

                for(int sX=0;sX<chunkWidth;sX++) //Iterate through all child tiles within the set
                {
                    for(int sY=0;sY<chunkHeight;sY++)
                    {
                        worldChunks[iteration].AddTile(sX, sY, ref tiles[x+sX, y+sY]); //append all tiles in the set at their relative locations
                    }
                }

                iteration++;

            }
        }

        Debug.Log("Exited tile addition");

    }

    public void IterateChunks(ref Color[] PixelsSet, int maxWidth, int maxHeight) //Iterate through all chunks and append to the pixels set array
    {
        foreach (Chunk worldChunk in worldChunks)
        {
            worldChunk.ReturnTiles(ref PixelsSet, maxWidth, maxHeight);
        }
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