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

    //List of provinces and container chunks
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

    public (int id, Chunk ret) AppendRandomAdjacentChunk(Province target, ref System.Random rnd, ref Dictionary<int,Chunk> worldChunks) //finds all chunks in the set which contain two or more connecting 
    {
        List<int> adjacentIDs = target.ReturnAdjacents(); //Finds all adjacent chunks indexes

        if (adjacentIDs == null)
        {
            return (0,null);
        }

        foreach (int id in adjacentIDs.ToArray()) //remove all chunks that are not free
        {
            if(!worldChunks.ContainsKey(id)) //World chunks stores only the chunks that are free for taking
            {
                adjacentIDs.Remove(id);
            }
        }

        if (adjacentIDs.Count == 0)
        {
            return (0,null);
        }

        int randomIndex = rnd.Next(0, adjacentIDs.Count);

        Chunk tmpCH = worldChunks[adjacentIDs[randomIndex]];
        worldChunks.Remove(adjacentIDs[randomIndex]); //remove the id from the set, as it is now a member of the province

        return (adjacentIDs[randomIndex],tmpCH);
    }


    public void SetAdjacentChunks(ref Dictionary<int, Chunk> worldChunks) //Sets the adjacencies of all chunks in the set. worldChunks stores each ID and chunk
    {
        //TODO add ocean culling?

        List<(int x, int y, int id)> verticesSet = new List<(int x, int y, int id)>(); //Stores all vertices from the set

        foreach (KeyValuePair<int, Chunk> vChunk in worldChunks) //append all vertices to the set of vertices
        {
            verticesSet.Insert(0, (vChunk.Value.vertices[0].x, vChunk.Value.vertices[0].y, vChunk.Key));
            verticesSet.Insert(0, (vChunk.Value.vertices[1].x, vChunk.Value.vertices[1].y, vChunk.Key));
            verticesSet.Insert(0, (vChunk.Value.vertices[2].x, vChunk.Value.vertices[2].y, vChunk.Key));
        }

        int currentIteration = 0; //Stores which chunk in the set is currently being accessed
        foreach (KeyValuePair<int, Chunk> vChunk in worldChunks) //append all vertices to the set of vertices
        {
            List<int> vertConnections = new List<int>(); //stores all the chunks that have a matching vertex to this chunk
            List<int> trueAdjacents = new List<int>(); //stores all tiles with more than one matching vertex
            int adjacentsCount = 0;

            foreach ((int x, int y, int id) vert in verticesSet) //compare all vertices in the world set of verts to find matches
            {
                if (adjacentsCount >= 3) { break; } //breaks when adjacency hits the maximum number of adjacent chunks (3)

                if (vert.id > currentIteration && !trueAdjacents.Contains(vert.id)) //If not already a registered value
                {
                    if (vert.id != vChunk.Key && vChunk.Value.vertices.Contains((vert.x, vert.y))) //If there is a matching vertex
                    {
                        if (!vertConnections.Contains(vert.id))
                        {
                            vertConnections.Insert(0, vert.id); //insert to list of connections - not the list of tiles notably
                        }
                        else
                        {
                            trueAdjacents.Insert(0, vert.id); //adds to set of adjacent tiles
                            adjacentsCount++;
                        }

                    }
                }
                else if (vert.id < currentIteration && !trueAdjacents.Contains(vert.id)) //If the selected vertex already has been tested
                {
                    if (worldChunks[vert.id].IsAdjacent(currentIteration)) //If the previously set chunk already considers it an adjacent
                    {
                        trueAdjacents.Insert(0, vert.id); //Append to set of adjacents
                        adjacentsCount++;
                    }
                }
            }
            currentIteration++;
            vChunk.Value.SetAdjacencies(trueAdjacents);
        }


    }

    public void SplitIntoChunks(ref System.Random rnd) //Splits the map into chunks (Triangles)
    {
        //Set of all chunks
        Dictionary<int,Chunk> worldChunks = new Dictionary<int,Chunk>();

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

                worldChunks.Add(iteration, new Chunk());
                worldChunks.Add(iteration + 1, new Chunk());

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

        ProvinceSplit(ref rnd, ref worldChunks); //Splits the worldchunks into provinces
    }

    public void ProvinceSplit(ref System.Random rnd, ref Dictionary<int,Chunk> worldChunks) //randomly selects chunks and makes provinces from them and connecting chunks
    {
        Debug.Log("Pre Adjacent");
        SetAdjacentChunks(ref worldChunks);
        Debug.Log("Post Adjacent");

        while (worldChunks.Count > 0) //Iterate through world chunks until all provinces have been made
        {
            int targetChunk;

            //Temporary variable
            {
                List<int> randomChunk = new List<int>(worldChunks.Keys);
                targetChunk = randomChunk[rnd.Next(0, worldChunks.Count)];
            }

            //Add chunk to province and then remove from chunks set to stop duplicate memory
            worldProvinces.Insert(0, new Province(targetChunk,worldChunks[targetChunk]));
            worldChunks.Remove(targetChunk);

            int iterations = BiomesObject.activeBiomes[worldProvinces[0]._componentChunks[targetChunk].chunkBiome]._provinceSpread;

            int connectedProvs = 1;
            for (int a = 0; a < iterations; a++)
            {
                (int id, Chunk ret) tmpChunk = AppendRandomAdjacentChunk(worldProvinces[0], ref rnd, ref worldChunks);

                if (tmpChunk.ret == null)
                {
                    break;
                }

                worldProvinces[0]._componentChunks.Add(tmpChunk.id, tmpChunk.ret);
                connectedProvs++;
            }
        }


    }

    public void IterateProvinces(ref Color[] PixelsSet, int maxWidth, int maxHeight, ref System.Random rn) //Iterate through all provinces and append to the pixels set array
    {
        foreach (Province prov in worldProvinces)
        {
            prov.IterateChunks(ref PixelsSet, maxWidth, maxHeight, ref rn); //return the data for each component chunk to be printed out
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
        tileColour = BiomesObject.activeBiomes[tiles[x, y]._biomeType].GetColour();
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