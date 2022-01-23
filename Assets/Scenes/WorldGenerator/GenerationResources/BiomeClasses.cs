using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //objects
using WorldProperties;

namespace BiomeData
{
    public class Province //Contains multiple connected chunks
    {
        public Dictionary<int,Chunk> _componentChunks = new Dictionary<int,Chunk>(); //Chunk ID and component
        public List<int> adjacentProvIDs = new List<int>();

        //Province properties
        public int _biome;
        public Color _provCol;
        public Property _elProp;
        public Property _tmpProp;
        public Property _rainProp;
        public Property _floraProp;

        public Province(int id, Chunk firstChunk) 
        {
            _componentChunks.Add(id, firstChunk);
            _biome = firstChunk.chunkBiome;
        }
        public void SetGenerationProvProperties(ref System.Random rnd) //Properties to set during world gen for a province
        {
            //Sets the province biome
            {
                if (_biome != 0) 
                {
                    Dictionary<int, int> biomeInstance = new Dictionary<int, int>(); //Stores the amount of each biome in a chunk

                    //Avgcheckers dictionaries
                    Dictionary<Property, int> elvCount = new Dictionary<Property, int>();
                    Dictionary<Property, int> tmpCount = new Dictionary<Property, int>();
                    Dictionary<Property, int> rainCount = new Dictionary<Property, int>();
                    Dictionary<Property, int> floraCount = new Dictionary<Property, int>();

                    foreach (Chunk compChunk in _componentChunks.Values)
                    {
                        if(biomeInstance.ContainsKey(compChunk.chunkBiome)) { biomeInstance[compChunk.chunkBiome] += 1; }
                        else { biomeInstance.Add(compChunk.chunkBiome, 0); }

                        if (elvCount.ContainsKey(compChunk.avgEl)) { elvCount[compChunk.avgEl] += 1; }
                        else { elvCount.Add(compChunk.avgEl, 0); }

                        if (tmpCount.ContainsKey(compChunk.avgTmp)) { tmpCount[compChunk.avgTmp] += 1; }
                        else { tmpCount.Add(compChunk.avgTmp, 0); }

                        if (rainCount.ContainsKey(compChunk.avgRain)) { rainCount[compChunk.avgRain] += 1; }
                        else { rainCount.Add(compChunk.avgRain, 0); }

                        if (floraCount.ContainsKey(compChunk.avgFlor)) { floraCount[compChunk.avgFlor] += 1; }
                        else { floraCount.Add(compChunk.avgFlor, 0); }
                    }

                    int maxBio = -1;

                    foreach(int biomeID in biomeInstance.Keys)
                    {
                        if(maxBio == -1 || biomeInstance[biomeID] > biomeInstance[maxBio] )
                        {
                            maxBio = biomeID;
                        }
                    }

                    _biome = maxBio;

                    int maxElv = -1;
                    foreach (Property elv in elvCount.Keys)
                    {
                        if (elvCount[elv] > maxElv || maxElv == -1)
                        {
                            _elProp = elv;
                            maxElv = elvCount[elv];
                        }
                    }
                    elvCount = null;

                    int maxTmp = -1;
                    foreach (Property tmp in tmpCount.Keys)
                    {
                        if (tmpCount[tmp] > maxTmp || maxTmp == -1)
                        {
                            _tmpProp = tmp;
                            maxTmp = tmpCount[tmp];
                        }
                    }
                    tmpCount = null;

                    int maxRain = -1;
                    foreach (Property rain in rainCount.Keys)
                    {
                        if (rainCount[rain] > maxRain || maxRain == -1)
                        {
                            _rainProp = rain;
                            maxRain = rainCount[rain];
                        }
                    }
                    rainCount = null;

                    int maxFlor = -1;
                    foreach (Property flor in floraCount.Keys)
                    {
                        if (floraCount[flor] > maxFlor || maxFlor == -1)
                        {
                            _floraProp = flor;
                            maxFlor = floraCount[flor];
                        }
                    }
                    floraCount = null;

                }
                else
                {
                    _elProp = Property.NA;
                    _tmpProp = Property.NA;
                    _rainProp = Property.NA;
                    _floraProp = Property.NA;
                }
            }

            if(_biome == 0) { _provCol = new Color(0, 0, 0); }
            else
            {
                float r = (float)rnd.Next(0, 256) / 255;
                float g = (float)rnd.Next(0, 256) / 255;
                float b = (float)rnd.Next(0, 256) / 255;

                _provCol = new Color(r, g, b);
            }

        }
        public void AppendAdjacentProvs(ref List<int> provAdjs)
        {
            adjacentProvIDs = provAdjs;
        }
        public Vector3 CalculateRelativeCenterPoint() //returns the relative centerpoint for the province
        {
            int lowestX = -1;
            int highestX = -1;
            int lowestY = -1;
            int highestY = -1;

            foreach(Chunk target in _componentChunks.Values)
            {
                foreach((int x, int y) vert in target.vertices)
                {
                    if(vert.x < lowestX || lowestX == -1) { lowestX = vert.x; }
                    if (vert.x > highestX || highestX == -1) { highestX = vert.x; }
                    if (vert.y < lowestY || lowestY == -1) { lowestY = vert.y; }
                    if (vert.y > highestY || highestY == -1) { highestY = vert.y; }
                }
            }

            float centerX = lowestX + (((float)highestX - (float)lowestX) / 2);
            float centerY = lowestY + (((float)highestY - (float)lowestY) / 2);

            return new Vector3(centerX, centerY, 0);
        }

        public void IterateChunks(ref Color[] PixelsSet, int maxWidth, int maxHeight, ref System.Random rnd) //Iterate through all chunks and append to the pixels set array
        {
            foreach (Chunk worldChunk in _componentChunks.Values)
            {
                worldChunk.ReturnTiles(ref PixelsSet, maxWidth, maxHeight);
            }
        }

        public List<int> ReturnAdjacents() //returns all the adjacent tiles in the set. (Including duplicates)
        {
            List<int> adjacentIds = new List<int>();

            foreach (KeyValuePair<int, Chunk> item in _componentChunks)
            {
                adjacentIds.AddRange(item.Value.adjacentChunks); //adds all adjacent chunks to set
            }

            if(adjacentIds.Count < 1)
            {
                return null;
            }

            return adjacentIds;
        }
    }

    public class Chunk //Stores a set of tiles
    {
        public List<(int x, int y)> vertices = new List<(int x, int y)>();
        public List<(int x,int y, TileData tile)> chunkTiles = new List<(int x, int y, TileData tile)>(); //List of tile tuples to represent the triangle chunk
        public int chunkBiome;
        public List<int> adjacentChunks = new List<int>(); //set of all chunks with adjacencies

        public Property avgEl;
        public Property avgTmp;
        public Property avgRain;
        public Property avgFlor;

        //A chunk consists of a number of half a rectangle worth of tiles, forming a right angled triangle bound from either the top left or top right

        public Chunk() {
        }

        public void AddTile(int x,int y, ref TileData tile)
        {
            chunkTiles.Add((x, y, tile)); //Append the tile into the set of tiles within the chunk
        }

        public bool IsAdjacent(int id)
        {
            return adjacentChunks.Contains(id); //check if the specified ID is in the set or not
        }

        public void SetAdjacencies(List<int> adjacenciesToMap) //sets the IDs of all adjacent chunks
        {
            adjacentChunks = adjacenciesToMap;
        }

        public void RegisterChunk(int chunkW, int chunkH)
        {
            //Find all vertices
            int maxX = -1;
            int minX = -1;
            int maxY = -1;
            int minY = -1;

            //biome checker dictionary
            Dictionary<int, int> biomeCounts = new Dictionary<int, int>();

            //Avgcheckers dictionaries
            Dictionary<Property, int> elvCount = new Dictionary<Property, int>();
            Dictionary<Property, int> tmpCount = new Dictionary<Property, int>();
            Dictionary<Property, int> rainCount = new Dictionary<Property, int>();
            Dictionary<Property, int> floraCount = new Dictionary<Property, int>();

            for (int i = 0; i < chunkTiles.Count; i++) //Loop through all tiles to find values
            {
                if(chunkTiles[i].x > maxX || maxX == -1) { maxX = chunkTiles[i].x; }
                if (chunkTiles[i].x < minX || minX == -1) { minX = chunkTiles[i].x; }
                if (chunkTiles[i].y > maxY || maxY == -1) { maxY = chunkTiles[i].y; }
                if (chunkTiles[i].y < minY || minY == -1) { minY = chunkTiles[i].y; }

                if(biomeCounts.ContainsKey(chunkTiles[i].tile._biomeType))
                {
                    if(chunkTiles[i].tile._biomeType != 0) //ocean will always stay at 0, ensuring it will only be selected if there is only ocean in a tile
                    {
                        biomeCounts[chunkTiles[i].tile._biomeType] += 1;
                    }
                }
                else
                {
                    biomeCounts.Add(chunkTiles[i].tile._biomeType, 0);
                }

                if (chunkTiles[i].tile._biomeType != 0) //Store modal properties for non ocean 
                {
                    //Find counts of each property
                    if (elvCount.ContainsKey(chunkTiles[i].tile._heightProp))
                    {
                        elvCount[chunkTiles[i].tile._heightProp] += 1;
                    }
                    else
                    {
                        elvCount.Add(chunkTiles[i].tile._heightProp, 0);
                    }

                    if (tmpCount.ContainsKey(chunkTiles[i].tile._tempProp))
                    {
                        tmpCount[chunkTiles[i].tile._tempProp] += 1;
                    }
                    else
                    {
                        tmpCount.Add(chunkTiles[i].tile._tempProp, 0);
                    }

                    if (rainCount.ContainsKey(chunkTiles[i].tile._rainfallProp))
                    {
                        rainCount[chunkTiles[i].tile._rainfallProp] += 1;
                    }
                    else
                    {
                        rainCount.Add(chunkTiles[i].tile._rainfallProp, 0);
                    }

                    if (floraCount.ContainsKey(chunkTiles[i].tile._floraProp))
                    {
                        floraCount[chunkTiles[i].tile._floraProp] += 1;
                    }
                    else
                    {
                        floraCount.Add(chunkTiles[i].tile._floraProp, 0);
                    }
                }
            }

            for (int i = 0; i < chunkTiles.Count; i++) 
            {
                //All chunks have three vertices which display exactly two of the characteristics listed
                //if 2 hits are made, then a vertice has been found
                int hits = 0;
                if(chunkTiles[i].x == maxX) { hits++; }
                if(chunkTiles[i].x == minX) { hits++; }
                if (chunkTiles[i].y == maxY) { hits++; }
                if (chunkTiles[i].y == minY) { hits++; }

                if(hits == 2)
                {
                    //gets closest vertex and appends to set
                    int vX = chunkTiles[i].x % chunkW == 0 ? chunkTiles[i].x : (int)Math.Round((float)chunkTiles[i].x / (float)chunkW, 0) * chunkW;
                    int vY = chunkTiles[i].y % chunkH == 0 ? chunkTiles[i].y : (int)Math.Round((float)chunkTiles[i].y / (float)chunkH, 0) * chunkH;

                    vertices.Add((vX, vY));
                }
            }

            int maxCount = -1;

            //sets the biome based on the maximum recorded biome type

            foreach(int biomeID in biomeCounts.Keys)
            {
                if(biomeCounts[biomeID] > maxCount || maxCount == -1)
                {
                    chunkBiome = biomeID;
                    maxCount = biomeCounts[biomeID];
                }
            }

            if (chunkBiome != 0) //Find modal properties
            {
                int maxElv = -1;
                foreach (Property elv in elvCount.Keys)
                {
                    if (elvCount[elv] > maxElv || maxElv == -1)
                    {
                        avgEl = elv;
                        maxElv = elvCount[elv];
                    }
                }
                elvCount = null;

                int maxTmp = -1;
                foreach (Property tmp in tmpCount.Keys)
                {
                    if (tmpCount[tmp] > maxTmp || maxTmp == -1)
                    {
                        avgTmp = tmp;
                        maxTmp = tmpCount[tmp];
                    }
                }
                tmpCount = null;

                int maxRain = -1;
                foreach (Property rain in rainCount.Keys)
                {
                    if (rainCount[rain] > maxRain || maxRain == -1)
                    {
                        avgRain = rain;
                        maxRain = rainCount[rain];
                    }
                }
                rainCount = null;

                int maxFlor = -1;
                foreach (Property flor in floraCount.Keys)
                {
                    if (floraCount[flor] > maxFlor || maxFlor == -1)
                    {
                        avgFlor = flor;
                        maxFlor = floraCount[flor];
                    }
                }
                floraCount = null;
            }

            }

        public void ReturnTiles(ref Color[] dataSet, int maxWidth, int maxHeight) //Appends its chunk data into the pixels dataset
        {
            for(int i=0;i<chunkTiles.Count;i++) //for loop through the tuple list (using a foreach makes things too complicated)
            {
                dataSet[(chunkTiles[i].y * maxWidth) + chunkTiles[i].x] = chunkTiles[i].tile.ReturnBiomeColour();
            }

        }
    }

    public class TileData //Stored in X,Y array. Stores the properties for each pixel.
    {

        //Relative properties - not exact values but usable by the biome setter to define biomes
        public Property _heightProp;
        public Property _tempProp;
        public Property _rainfallProp;
        public Property _floraProp;

        //Defined by properties
        public int _biomeType;

        public TileData() //Empty constructor - for new TileData() calls
        {

        }

        public void DefineProperties(ref int[,] deciles, int property, int value) //Select the property value for each property
        {
            if (property == 0)
            {
                if (value < deciles[0, 5]) { _heightProp = Property.Low; }
                else if (value >= deciles[0, 5] && value < deciles[0, 8]) { _heightProp = Property.Medium; }
                else { _heightProp = Property.High; }
            }
            else if (property == 1)
            {
                if (value < deciles[1, 2]) { _tempProp = Property.Low; }
                else if (value >= deciles[1, 2] && value < deciles[1, 6]) { _tempProp = Property.Medium; }
                else { _tempProp = Property.High; }
            }
            else if (property == 2)
            {
                if (value < deciles[2, 3]) { _rainfallProp = Property.Low; }
                else { _rainfallProp = Property.High; }
            }
            else if (property == 3)
            {
                if (value < deciles[3, 4]) { _floraProp = Property.Low; }
                else { _floraProp = Property.High; }
            }

        }

        public void InitializeBiome(ref int[,] deciles) //Set the biome for each tile, using the decile set as comparison
        {
            _biomeType = BiomesObject.SortTile(this, ref deciles); //attempts a self sort
        }

        public Color ReturnBiomeColour()
        {
            return BiomesObject.activeBiomes[_biomeType].GetColour();
        }

    }
}