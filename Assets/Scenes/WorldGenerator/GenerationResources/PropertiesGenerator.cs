using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //objects

//For quick querying
using System.Linq;

namespace PropertiesGenerator
{
    public static class GenerateNames //Generates a list of names to be stored, most notably the province names
    {
        public static List<string> GenerateProvinceName(ref System.Random rnd, int numToGenerate)
        {
            //Loads city names to be used to generate new city names
            TextAsset citiesFile = (TextAsset)Resources.Load("Cities");
            string[] citiesSet = citiesFile.text.Split('\n');
            citiesFile = null;

            List<string> newGeneratedCities = new List<string>();


            //This algorithm works to generate a list of new city names for the world generator
            //It does this by taking a number of characters from a random city name, then searching for a city with a letter in the same position, taking a random number of items from that set and so on.
            //This often generates psuedo-realistic city names, as well as sometimes generating the names of real cities
            for(int i=0;i<numToGenerate;i++)
            {
                int nextID = rnd.Next(0, citiesSet.Length);
                List<char> newCityName = new List<char>();
                int currentLength = 0;
                int maxLength = citiesSet[nextID].Length;

                List<string> applicableSet = citiesSet.ToList();

                while (currentLength < maxLength)
                {
                    int amountToAppend = 1 + ((applicableSet[nextID].Length - currentLength) % 5); //Gets a random value between 1 and the remaining length of a string or 5

                    if(applicableSet[nextID].Length - currentLength < amountToAppend) //If there are too few possible characters left
                    {
                        break; //Instantly end the generation procedure
                    }
                    
                    newCityName.AddRange(applicableSet[nextID].Substring(currentLength, amountToAppend));
                    currentLength += amountToAppend;
                    applicableSet = FindMatchAtIndex(ref citiesSet, currentLength, newCityName[currentLength - 1]); //Search for matching character
                    nextID = rnd.Next(0, applicableSet.Count); //Find next ID
                }

                string newCityString = new string(newCityName.ToArray());

                if(newCityString.Length <= 3) //Redo attempts with too few characters
                {
                    i--;
                }
                else //Add to set of generated cities names
                {
                    newGeneratedCities.Add(newCityString);
                }
            }

            return newGeneratedCities;
        }

        public static List<string> GenerateCultureName(ref System.Random rnd, int numToGenerate)
        {
            //This uses a variant of the previous algorithm
            TextAsset cultureFile = (TextAsset)Resources.Load("Cultures");
            string[] cultureSet = cultureFile.text.Split('\n');

            for(int i=0;i<cultureSet.Count();i++) //As the final characters are the most important, the cultures names are reversed, then reversed when appended to the set. This prioritises the last characters
            {
                char[] tmpArray = cultureSet[i].ToCharArray();
                Array.Reverse(tmpArray);
                cultureSet[i] = new string(tmpArray);
                cultureSet[i].ToLower(); //Set all characters to lower, the first character will become upper at the end
            }
            cultureFile = null;

            List<string> newGeneratedCultures = new List<string>();

            for (int i = 0; i < numToGenerate; i++)
            {
                int nextID = rnd.Next(0, cultureSet.Length);
                List<char> newCultureName = new List<char>();
                int currentLength = 0;
                int maxLength = cultureSet[nextID].Length < 5 ? rnd.Next(5,8) : cultureSet[nextID].Length;

                List<string> applicableSet = cultureSet.ToList();

                while (currentLength < maxLength)
                {
                    int amountToAppend = 1 + ((applicableSet[nextID].Length - currentLength) % 5); //Gets a random value between 1 and the remaining length of a string or 5

                    if (applicableSet[nextID].Length - currentLength < amountToAppend) //If there are too few possible characters left
                    {
                        break; //Instantly end the generation procedure
                    }

                    newCultureName.AddRange(applicableSet[nextID].Substring(currentLength, amountToAppend));
                    currentLength += amountToAppend;
                    applicableSet = FindMatchAtIndex(ref cultureSet, currentLength, newCultureName[currentLength - 1]); //Search for matching character
                    nextID = rnd.Next(0, applicableSet.Count); //Find next ID
                }

                List<char> newCityName = newCultureName.ToList();

                if (newCityName.Count <= 6) //Redo attempts with too few characters
                {
                    i--;
                }
                else //Add to set of generated cities names
                {
                    char[] tmpArray = newCityName.ToArray();
                    Array.Reverse(tmpArray);
                    tmpArray[0] = char.ToUpper(tmpArray[0]);
                    string newCity = new string(tmpArray);
                    if (newGeneratedCultures.Contains(newCity.Substring(0,newCity.Length-1)))
                    {
                        i--; //If this name is a duplicate, try again.
                    }
                    else
                    {
                        newGeneratedCultures.Add(newCity.Substring(0, newCity.Length - 1)); //The last character is a newline character and is therefore removed
                    }
                }
            }

            return newGeneratedCultures;
        }

        public static List<string> GenerateOceanNames(ref System.Random rnd, int numToGenerate)
        {
            //This uses a variant of the previous algorithm
            TextAsset oceanFile = (TextAsset)Resources.Load("Descriptor");
            string[] oceanSet = oceanFile.text.Split('\n');

            for (int i = 0; i < oceanSet.Count(); i++) //As the final characters are the most important, the oceans names are reversed, then reversed when appended to the set. This prioritises the last characters
            {
                char[] tmpArray = oceanSet[i].ToCharArray();
                Array.Reverse(tmpArray);
                oceanSet[i] = new string(tmpArray);
                oceanSet[i].ToLower(); //Set all characters to lower, the first character will become upper at the end
            }

            oceanFile = null;

            List<string> newGeneratedOceans = new List<string>();

            for (int i = 0; i < numToGenerate; i++)
            {
                int nextID = rnd.Next(0, oceanSet.Length);
                List<char> newOceanName = new List<char>();
                int currentLength = 0;
                int maxLength = oceanSet[nextID].Length < 5 ? rnd.Next(5, 8) : oceanSet[nextID].Length;

                List<string> applicableSet = oceanSet.ToList();

                while (currentLength < maxLength)
                {
                    int amountToAppend = 1 + ((applicableSet[nextID].Length - currentLength) % 5); //Gets a random value between 1 and the remaining length of a string or 5

                    if (applicableSet[nextID].Length - currentLength < amountToAppend) //If there are too few possible characters left
                    {
                        break; //Instantly end the generation procedure
                    }

                    newOceanName.AddRange(applicableSet[nextID].Substring(currentLength, amountToAppend));
                    currentLength += amountToAppend;
                    applicableSet = FindMatchAtIndex(ref oceanSet, currentLength, newOceanName[currentLength - 1]); //Search for matching character
                    nextID = rnd.Next(0, applicableSet.Count); //Find next ID
                }

                List<char> newWaterNames = newOceanName.ToList();

                if (newWaterNames.Count <= 6) //Redo attempts with too few characters
                {
                    i--;
                }
                else //Add to set of generated cities names
                {
                    char[] tmpArray = newWaterNames.ToArray();
                    Array.Reverse(tmpArray);
                    tmpArray[0] = char.ToUpper(tmpArray[0]);
                    string newCity = new string(tmpArray);
                    if (newGeneratedOceans.Contains(newCity.Substring(0, newCity.Length - 1)))
                    {
                        i--; //If this name is a duplicate, try again.
                    }
                    else
                    {
                        newGeneratedOceans.Add(newCity.Substring(0, newCity.Length - 1)); //The last character is a newline character and is therefore removed
                    }
                }
            }

            return newGeneratedOceans;
        }

        private static List<string> FindMatchAtIndex(ref string[] set, int ind, char match) //Returns a list of all matches for a query
        {
            //This finds parts of the set inwhich a character matches at the same point.
            //The ternary is included to ensure no errors occur.
            return set.Where(p => p.Length >= ind && p[(p.Length < ind ? 0 : ind - 1)] == match).ToList();
        }


    }

}