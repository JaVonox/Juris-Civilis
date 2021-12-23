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
        static System.Random rnd = new System.Random();
        public static List<Biome> activeBiomes = new List<Biome>(){
        new Biome("Ocean",Property.Low,Property.NA,Property.NA,Property.NA,new Color(0.04f,0.08f,0.58f)),
        new Biome("Temperate Forest",Property.Medium,Property.Medium,Property.NA,Property.High,new Color(0.01f,0.39f,0)),
        new Biome("Tropical Forest", Property.Medium, Property.High, Property.High, Property.High, new Color(0.06f,0.34f,0.05f)),
        new Biome("Taiga", Property.Medium, Property.Low, Property.NA, Property.High, new Color(0.06f,0.22f,0)),
        new Biome("Grasslands", Property.Medium, Property.Medium, Property.NA, Property.Low, new Color(0.02f,0.54f,0)),
        new Biome("Savannah", Property.Medium, Property.High, Property.High, Property.Low, new Color(0.78f,0.55f,0.15f)),
        new Biome("Tundra", Property.Medium, Property.Low, Property.NA, Property.Low, new Color(1,0.78f,0.78f)),
        new Biome("Desert", Property.Medium, Property.High, Property.Low, Property.Low, new Color(0.77f,0.61f,0.23f)),
        new Biome("Mountain", Property.High, Property.Low, Property.NA, Property.Low, new Color(0.5f,0.5f,0.5f)),
        new Biome("Forested Plateau", Property.High, Property.NA, Property.NA, Property.High, new Color(0.5f,0.5f,0.5f)),
        new Biome("Shrubland Plateau", Property.High, Property.High, Property.NA, Property.Low, new Color(0.5f,0.5f,0.5f)),
        };

        public static Biome SortTile(TileData target, ref int[,] deciles)
        {

            if(target._heightProp == Property.Low) //Oceans
            {
                return activeBiomes[0];
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

            return activeBiomes[maxIndex]; //returns the appropriate biome for this tile
        }

    }

    public class Chunk //Stores a set of tiles
    {
        public int xTop, yTop; //stores the coordinates of the top left
        public int height, width; //The height and width of the chunk
        TileData[,] chunkTiles;

        public Chunk(int x,int y,int w,int h)
        {
            xTop = x;
            yTop = y;
            height = h;
            width = w;
            chunkTiles = new TileData[width, height]; //defines new tiles set

        }

        public void AddTile(int relX,int relY, ref TileData tile)
        {
            chunkTiles[relX, relY] = tile; //adds referenced tile to set of tiles in chunk
        }

        public void ReturnTiles(ref Color[] dataSet, int maxWidth, int maxHeight) //Appends its chunk data into the pixels dataset
        {

            for (int x=0;x<width;x++)
            {
                for(int y=0;y<height;y++)
                {
                    int realX = xTop + x;
                    int realY = yTop + y;

                    dataSet[(realY * maxWidth) + realX] = chunkTiles[x, y].ReturnBiomeColour();
                }
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
        public Biome _biomeType;

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
            return _biomeType.GetColour();
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

        public Biome(string name, Property elT, Property tempT, Property rnT, Property flrT, Color colour)
        {
            _name = name;
            _desireElevation = elT;
            _desireTemperature = tempT;
            _desireRainfall = rnT;
            _desireFlora = flrT;
            _colour = colour;
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