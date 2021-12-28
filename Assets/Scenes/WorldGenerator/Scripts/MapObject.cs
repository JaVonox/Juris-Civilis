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
    public List<Province> worldProvinces = new List<Province>();

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

    public Chunk AppendRandomAdjacentChunk(Province target, ref System.Random rnd) //finds all chunks in the set which contain two or more connecting 
    {
        List<(int x, int y)> provinceVertices = new List<(int x, int y)>(); //stores the current vertices of the province
        provinceVertices = target.GetVertices();
        int min = 2;
        List<Chunk> adjacents = worldChunks.FindAll(cObject => AdjacentPredicate(cObject,ref provinceVertices, ref target, ref min)); //Finds all adjacent chunks indexes

        if(adjacents.Count == 0)
        {
            return null;
        }

        int randomIndex = rnd.Next(0, adjacents.Count);

        Chunk tmpCH = adjacents[randomIndex];
        worldChunks.Remove(adjacents[randomIndex]);

        return tmpCH;
    }

    private bool AdjacentPredicate(Chunk cObject, ref List<(int x, int y)> provinceVertices, ref Province target, ref int min)
    {

        int hits = 0; //number of true values. must be 2 or higher to return
        bool anyTwo = false; //Checks that any two vertices are connected via a chunk

        //province vertices exist in sets of three per chunk, therefore we can use the subset of x -> x+3 to represent each chunk
        for (int i = 0; i < provinceVertices.Count; i+=3) //check if any chunk in a province has two connections
        {
            int tmpHits = 0;
            List<(int x, int y)> subSet = new List<(int x, int y)>(); //set of the three verts of one chunk
            subSet.Add(provinceVertices[i]);
            subSet.Add(provinceVertices[i + 1]);
            subSet.Add(provinceVertices[i + 2]);

            if (subSet.Contains(cObject.vertices[0])) { tmpHits++; }
            if (subSet.Contains(cObject.vertices[1])) { tmpHits++; }
            if (subSet.Contains(cObject.vertices[2])) { tmpHits++; }

            if(tmpHits >= 2)
            {
                hits += tmpHits;
                anyTwo = true;
            }
            subSet = null; //remove subset from memory 
        }

        if (target._startBiome._name == "Ocean" && cObject.chunkBiome._name != "Ocean") //Ocean biomes can only connect to other ocean biomes
        {
            return false;
        }

        if(hits > min) //Set a new minimum number of connections. This gives priority to connections with more adjacent chunks.
        {
            min = hits;
        }

        return (hits >= min && anyTwo);

    }


    public void SplitIntoChunks(ref System.Random rnd) //Splits the map into chunks (Triangles)
    {
        //should always be integer divisible by num of chunks.
        int chunkWidth = maxWidth / 100;
        int chunkHeight = maxHeight / 100;
        long moduloVal = rnd.Next(1, 20000); //this defines the start for the modulo randomiser
        //The random values for this procedure use a linear congruential function as the system random uses system clock data
        //As this procedure needs constant new random values, this is not preferrable
        //By using a random before the start of the procedure however, we can generate a random seed for the modulo operation to work from

        float grad = (float)chunkHeight / (float)chunkWidth; //rough dy/dx value for drawing chunk boundaries
        int iteration = 0; //chunk number

        for(int x=0;x<maxWidth;x+=chunkWidth) //Iterate through each chunk
        {
            for (int y=0;y<maxHeight;y+=chunkHeight)
            {
                moduloVal = (1103515245 * moduloVal + 12345) % 2147483648; //uses glib c parameters

                bool topLeftUsed = moduloVal < (2147483648 / 2) ? true : false; //if the value is less than half, use the top left vertex as a split. If not, use top right.

                //Add two chunks to the set, one for each side of the split triangle
                worldChunks.Insert(iteration, new Chunk()); 
                worldChunks.Insert(iteration + 1, new Chunk()); 

                for(int sX=0;sX<chunkWidth;sX++) //Iterate through all child tiles within the set
                {
                    for(int sY=0;sY<chunkHeight;sY++)
                    {
                        if(topLeftUsed)
                        {
                            if((float)sY < (-grad*(float)sX) + (float)chunkHeight)
                            {
                                worldChunks[iteration].AddTile(x + sX, y + sY, ref tiles[x + sX, y + sY]); //append tile that is below the boundary for this relative position
                            }
                            else
                            {
                                worldChunks[iteration + 1].AddTile(x + sX, y + sY, ref tiles[x + sX, y + sY]); //append tile above of the boundary to set
                            }
                        }
                        else
                        {
                            if ((float)sY < (grad * (float)sX))
                            {
                                worldChunks[iteration].AddTile(x + sX, y + sY, ref tiles[x + sX, y + sY]); //append tile that is below the boundary for this relative position
                            }
                            else
                            {
                                worldChunks[iteration + 1].AddTile(x + sX, y + sY, ref tiles[x + sX, y + sY]); //append tile above of the boundary to set
                            }
                        }

                    }
                }

                worldChunks[iteration].RegisterChunk(chunkWidth,chunkHeight);
                worldChunks[iteration+1].RegisterChunk(chunkWidth,chunkHeight);

                iteration +=2;

            }
        }

        ProvinceSplit(ref rnd); //Splits the worldchunks into provinces

    }

    public void ProvinceSplit(ref System.Random rnd) //randomly selects chunks and makes provinces from them and connecting chunks
    {

        for (int i = 0; i < 10;i++)
        {
            int targetChunk = rnd.Next(0, worldChunks.Count);
            //Add chunk to province and then remove from chunks set to stop duplicate memory
            worldProvinces.Insert(0, new Province(worldChunks[targetChunk]));
            worldChunks.Remove(worldChunks[targetChunk]);
            int iterations = worldProvinces[0]._componentChunks[0].chunkBiome._provinceSpread;

            for (int a = 0; a < iterations; a++)
            {
                Chunk tmpChunk = AppendRandomAdjacentChunk(worldProvinces[0], ref rnd);

                if (tmpChunk == null)
                {
                    break;
                }

                worldProvinces[0]._componentChunks.Insert(0, tmpChunk);
            }
        }


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