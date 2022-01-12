using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //objects

namespace BiomeData
{

    public enum Property
    {
        Low,
        Medium,
        High,
        NA
    }

    public static class BiomesObject
    {
        public static List<Biome> activeBiomes = new List<Biome>(){
        new Biome("Ocean",Property.Low,Property.NA,Property.NA,Property.NA,new Color(0.04f,0.08f,0.58f),100), //ocean should always be 0th
        new Biome("Temperate Forest",Property.Medium,Property.Medium,Property.NA,Property.High,new Color(0.01f,0.39f,0),24),
        new Biome("Tropical Forest", Property.Medium, Property.High, Property.High, Property.High, new Color(0.06f,0.34f,0.05f),48),
        new Biome("Taiga", Property.Medium, Property.Low, Property.NA, Property.High, new Color(0.06f,0.22f,0),24),
        new Biome("Grasslands", Property.Medium, Property.Medium, Property.NA, Property.Low, new Color(0.02f,0.54f,0),24),
        new Biome("Savannah", Property.Medium, Property.High, Property.High, Property.Low, new Color(0.78f,0.55f,0.15f),16),
        new Biome("Tundra", Property.Medium, Property.Low, Property.NA, Property.Low, new Color(1,0.78f,0.78f),48),
        new Biome("Desert", Property.Medium, Property.High, Property.Low, Property.Low, new Color(0.77f,0.61f,0.23f),36),
        new Biome("Mountain", Property.High, Property.Low, Property.NA, Property.Low, new Color(0.5f,0.5f,0.5f),16),
        new Biome("Forested Plateau", Property.High, Property.NA, Property.NA, Property.High, new Color(0.5f,0.5f,0.5f),16),
        new Biome("Shrubland Plateau", Property.High, Property.High, Property.NA, Property.Low, new Color(0.5f,0.5f,0.5f),16),
        };

        public static int SortTile(TileData target, ref int[,] deciles)
        {

            if(target._heightProp == Property.Low) //Oceans
            {
                return 0;
            }

            float[] indexScores = new float[activeBiomes.Count]; //make temporary array the size of the activeBiomes list

            foreach (Biome tBiome in activeBiomes) //find the score of each biome against the target data
            {
                indexScores[activeBiomes.IndexOf(tBiome)] = tBiome.Score(target, ref deciles);
            }

            float maxVal = -1;
            int maxIndex = -1;

            for (int i = 1; i < activeBiomes.Count; i++) //iterate through all active biomes except ocean
            {
                if (indexScores[i] > maxVal || maxIndex == -1)
                {
                    maxVal = indexScores[i];
                    maxIndex = i;
                }
            }

            return maxIndex; //returns the appropriate biome for this tile
        }

    }

    public class ProvinceObject //Stores the data for a province after province data has been appended. This is the saved province data.
    {
        public int _id;
        public int _biome;
        public Color _provCol;
        public List<Vector3> _vertices = new List<Vector3>();
        public List<int> _adjacentProvIDs = new List<int>();
        public ProvinceObject(int id, Province tProv) //Constructor from province object
        {
            _id = id;
            _biome = tProv._biome;
            _provCol = tProv._provCol;
            _adjacentProvIDs = tProv.adjacentProvIDs;
            
            foreach(Chunk compChunk in tProv._componentChunks.Values)
            {
                for (int v = 0; v < 3; v++)
                {
                    _vertices.Add(new Vector3(compChunk.vertices[v].x, compChunk.vertices[v].y, 0));
                }
            }
        }
        public Vector3 CalculateRelativeCenterPoint() //returns the relative centerpoint for the province
        {
            float lowestX = -1;
            float highestX = -1;
            float lowestY = -1;
            float highestY = -1;

            foreach (Vector3 vert in _vertices)
            {
                if (vert.x < lowestX || lowestX == -1) { lowestX = vert.x; }
                if (vert.x > highestX || highestX == -1) { highestX = vert.x; }
                if (vert.y < lowestY || lowestY == -1) { lowestY = vert.y; }
                if (vert.y > highestY || highestY == -1) { highestY = vert.y; }
            }

            float centerX = lowestX + (((float)highestX - (float)lowestX) / 2);
            float centerY = lowestY + (((float)highestY - (float)lowestY) / 2);

            return new Vector3(centerX, centerY, 0);
        }

    }

    public class Province //Contains multiple connected chunks
    {
        public Dictionary<int,Chunk> _componentChunks = new Dictionary<int,Chunk>(); //Chunk ID and component
        public List<int> adjacentProvIDs = new List<int>();

        //Province properties
        public int _biome;
        public Color _provCol;

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
                    foreach (Chunk compChunk in _componentChunks.Values)
                    {
                        if(biomeInstance.ContainsKey(compChunk.chunkBiome)) { biomeInstance[compChunk.chunkBiome] += 1; }
                        else { biomeInstance.Add(compChunk.chunkBiome, 0); }
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

        //A chunk consists of a number of half a rectangle worth of tiles, forming a right angled triangle bound from either the top left or top right

        public Chunk() { }

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
                else if (value >= deciles[0, 5] && value < deciles[0, 8] + (int)(((float)deciles[0, 9] - (float)deciles[0, 8]) / 4)) { _heightProp = Property.Medium; }
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

    public class Biome
    {
        public string _name;
        
        //Stores what the expected properties are for a biome of this type
        Property _desireElevation;
        Property _desireTemperature;
        Property _desireRainfall;
        Property _desireFlora;
        Color _colour;
        public int _provinceSpread; //decides how many chunks a province of this type will spread

        public Biome(string name, Property elT, Property tempT, Property rnT, Property flrT, Color colour, int provSpread)
        {
            _name = name;
            _desireElevation = elT;
            _desireTemperature = tempT;
            _desireRainfall = rnT;
            _desireFlora = flrT;
            _colour = colour;
            _provinceSpread = provSpread;
        }

        public float Score(TileData targetTile, ref int[,] deciles) //returns the deviance of a tile from this biome. the set of these scores is then polled to find the biome with the most appropriate typing
        {
            float score=0;

            if(targetTile._heightProp == _desireElevation)
            {
                score += 10;
            }
            else if(_desireElevation == Property.NA)
            {
                score += 0.5f;
            }

            if (targetTile._tempProp == _desireTemperature)
            {
                score += 1;
            }
            else if (_desireTemperature == Property.NA)
            {
                score += 0.5f;
            }

            if (targetTile._rainfallProp == _desireRainfall)
            {
                score += 1;
            }
            else if (_desireRainfall == Property.NA)
            {
                score += 0.5f;
            }

            if (targetTile._floraProp == _desireFlora)
            {
                score += 1;
            }
            else if (_desireFlora == Property.NA)
            {
                score += 0.5f;
            }

            return score;
        }

        public Color GetColour()
        {
            return _colour;
        }
    }
}