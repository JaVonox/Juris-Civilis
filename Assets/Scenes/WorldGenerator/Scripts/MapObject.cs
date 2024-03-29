using System;
using System.Collections;
using System.Collections.Generic;
using BiomeData; //Biome stuff
using WorldProperties;
using UnityEngine;
using System.Linq;
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

    //List of saveable objects
    public List<ProvinceObject> provinceSaveables = new List<ProvinceObject>();
    public List<Culture> cultures = new List<Culture>();

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

        bool amIOcean = (target._biome == 0 ? true : false) ; //checks if self is ocean. ternary is not required but added just incase

        foreach (int id in adjacentIDs.ToArray()) //remove all chunks that are not valid
        {
            if (!worldChunks.ContainsKey(id)) //World chunks stores only the chunks that are free for taking
            {
                adjacentIDs.Remove(id);
            }
            else if (worldChunks[id].chunkBiome == 0) //Check if the adjacent is an ocean biome
            {
                if (!amIOcean) { adjacentIDs.Remove(id); } //If the target is not an ocean, remove from IDs list
            }
            else if (worldChunks[id].chunkBiome != 0 && amIOcean)
            {
                adjacentIDs.Remove(id); //If this province is an ocean, it cannot connect to non ocean provinces.
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
    public void SetProvinceSaveables(ref System.Random rnd) //Create saveable province objects
    {
        int i = 0;
        int lname = 0;
        int oname = 0;
        string[] oceanConnectedNames = { " Gulf", " Cove", " Bay", " Ocean", " Sea"};
        string[] oceanUnconnectedNames = { " Ocean", " Sea" };
        List<string> provNamesSet = PropertiesGenerator.GenerateNames.GenerateProvinceName(ref rnd, worldProvinces.Count(tProv => tProv._biome != 0));
        List<string> oceanNamesSet = PropertiesGenerator.GenerateNames.GenerateOceanNames(ref rnd, worldProvinces.Count(tProv => tProv._biome == 0));

        foreach (Province tProv in worldProvinces)
        {
            if(tProv._biome != 0)
            {
                provinceSaveables.Add(new ProvinceObject(i, provNamesSet[lname], tProv));
                bool isCoastal = worldProvinces.Any(prov => prov._biome == 0 && provinceSaveables[i]._adjacentProvIDs.Contains(worldProvinces.IndexOf(prov))); //Finds if there are any adjacent coastal provinces
                provinceSaveables[i].GenerateFinalValues(ref rnd, isCoastal); //Generates some final properties while putting the province into save mode.
                lname++;
            }
            else
            {
                provinceSaveables.Add(new ProvinceObject(i, "Ocean", tProv)); //Maybe add ocean names in the future?
                bool isCoastal = worldProvinces.Any(prov => prov._biome != 0 && provinceSaveables[i]._adjacentProvIDs.Contains(worldProvinces.IndexOf(prov))); //Finds if there are any adjacent non-coastal provinces
                string newOceanName = oceanNamesSet[oname];

                if(isCoastal){ newOceanName = newOceanName + oceanConnectedNames[rnd.Next(0, oceanConnectedNames.Count())];}
                else{newOceanName = newOceanName + oceanUnconnectedNames[rnd.Next(0, oceanUnconnectedNames.Count())];}
                provinceSaveables[i]._cityName = newOceanName;

                provinceSaveables[i].GenerateFinalValues(ref rnd, isCoastal);
                provinceSaveables[i]._provCol = new Color(0.02f, 0.25f, 0.45f);
                oname++;
            }
            i++;
        }

        //Clears unneeded data
        worldProvinces = null;
        provNamesSet = null;
        oceanNamesSet = null;

        //Use garbage collector manually to clear up data due to the heavy memory usage impacts of this program
        GC.Collect();
        GC.WaitForPendingFinalizers();

        cultures.Add(new Culture("0", ref rnd));
        cultures[0]._name = "Ocean";
        CreateCultures(ref rnd);

        List<string> cultNamesSet = PropertiesGenerator.GenerateNames.GenerateCultureName(ref rnd, cultures.Count);

        i = 0;
        foreach (Culture tCul in cultures) //Add culture names for each applicable
        {
            if (tCul._name != "Ocean")
            {
                tCul._name = cultNamesSet[i];
                i++;
            }
        }

        //Sort the number of provinces each province owns. get the minimum and maximum
        var provCultures = from provs in provinceSaveables
                      group provs._id by provs._cultureID into g
                      orderby g.Count()
                      select new { cultID = g.Key, counter = g.Count() };

        float cMax = (float)provCultures.Max(t => t.counter);
        float cMin = (float)provCultures.Min(t => t.counter);

        //Set population values for each non-ocean province

        float maxP = -1;
        float minP = -1;
        float[] provScores = new float[provinceSaveables.Count];

        foreach (ProvinceObject tProv in provinceSaveables) //Could be simplified
        {
            if (tProv._biome != 0)
            {
                float myCultCount = (float)(from cultC in provCultures where cultC.cultID == tProv._cultureID select cultC.counter).FirstOrDefault();
                float normalisedValue = (myCultCount - cMin) / (cMax - cMin); //Gets value from 0 to 1

                //y = (((T/2)*0.5) + (-(E / (x+1)) + (1 / 4)) + (6xcos(F - 1))+(R / (x + 1) - 1) / 10
                float elScore = ((int)tProv._elProp - 1);
                float teScore = ((int)tProv._tmpProp - 1);
                float rnScore = tProv._rainProp == Property.Low ? 0 : 1;
                float flScore = tProv._floraProp == Property.Low ? 0 : 1;

                float popScore =
                    ((((float)teScore / 2.0f) *0.5f) +
                    (-(elScore / (normalisedValue + 1.0f)) + (1.0f / 4.0f)) +
                    ((6 * normalisedValue) * (float)Math.Cos(flScore - 1.0f)) +
                    ((rnScore / (normalisedValue + 1.0f)) - 1.0f)) / 10.0f;

                if(popScore < -2) { popScore = -2; }
                if(popScore > 2) { popScore = 2; }

                provScores[tProv._id] = popScore;

                if(popScore > maxP || maxP == -1) { maxP = popScore; }
                if(popScore < minP || minP == -1) { minP = popScore; }
                
            }
        }

        foreach(ProvinceObject tprov in provinceSaveables)
        {
            if(tprov._biome != 0)
            {
                float relativeScore = (provScores[tprov._id] - minP) / (maxP - minP);

                if(relativeScore < 0.55f) { tprov._population = Property.Low; }
                else if(relativeScore > 0.55f && relativeScore < 0.8f) { tprov._population = Property.Medium; }
                else { tprov._population = Property.High; }
            }
        }
    }

    public void SetAdjacentChunks(ref Dictionary<int, Chunk> worldChunks) //Sets the adjacencies of all chunks in the set. worldChunks stores each ID and chunk
    {
        List<(int x, int y, int id)> verticesSet = new List<(int x, int y, int id)>(); //Stores all vertices from the set

        foreach (KeyValuePair<int, Chunk> vChunk in worldChunks) //append all vertices to the set of vertices
        {
            verticesSet.Insert(0, (vChunk.Value.vertices[0].x, vChunk.Value.vertices[0].y, vChunk.Key));
            verticesSet.Insert(0, (vChunk.Value.vertices[1].x, vChunk.Value.vertices[1].y, vChunk.Key));
            verticesSet.Insert(0, (vChunk.Value.vertices[2].x, vChunk.Value.vertices[2].y, vChunk.Key));
        }

        int currentIteration = 0; //Stores which chunk in the set is currently being accessed
        foreach (KeyValuePair<int, Chunk> vChunk in worldChunks) //iterate through all chunks in the set to find adjacencies
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

        tiles = null; //Kill the tiles data to reduce memory usage
        ProvinceSplit(ref rnd, ref worldChunks); //Splits the worldchunks into provinces
    }

    public void ProvinceSplit(ref System.Random rnd, ref Dictionary<int,Chunk> worldChunks) //randomly selects chunks and makes provinces from them and connecting chunks
    {
        Dictionary<int, int> chunkIDsandOwner = new Dictionary<int, int>(); //Store chunkID and Owner
        SetAdjacentChunks(ref worldChunks);

        while (worldChunks.Count > 0) //Iterate through world chunks until all provinces have been made
        {
            int targetChunk;

            //Temporary variable
            {
                List<int> randomChunk = new List<int>(worldChunks.Keys);
                targetChunk = randomChunk[rnd.Next(0, worldChunks.Count)];
            }

            int nextProvId = worldProvinces.Count;
            //Add chunk to province and then remove from chunks set to stop duplicate memory
            worldProvinces.Insert(nextProvId, new Province(targetChunk,worldChunks[targetChunk]));
            chunkIDsandOwner.Add(targetChunk, nextProvId);

            worldChunks.Remove(targetChunk);

            int iterations = BiomesObject.activeBiomes[worldProvinces[nextProvId]._componentChunks[targetChunk].chunkBiome]._provinceSpread;

            int connectedProvs = 1;
            for (int a = 0; a < iterations; a++)
            {
                (int id, Chunk ret) tmpChunk = AppendRandomAdjacentChunk(worldProvinces[nextProvId], ref rnd, ref worldChunks);

                if (tmpChunk.ret == null)
                {
                    break;
                }

                worldProvinces[nextProvId]._componentChunks.Add(tmpChunk.id, tmpChunk.ret);
                chunkIDsandOwner.Add(tmpChunk.id, nextProvId);
                connectedProvs++;
            }
        }

        StoreProvAdjacents(ref chunkIDsandOwner);

        foreach(Province tProv in worldProvinces) //Sets some basic properties for each province
        {
            tProv.SetGenerationProvProperties(ref rnd);
        }
    }
    private void StoreProvAdjacents(ref Dictionary<int,int> owners) //For each province, set which provinces are adjacent
    {
        int id = 0;
        foreach(Province tProv in worldProvinces)
        {
            List<int> adjacentProvinces = new List<int>();
            
            foreach (Chunk tChunk in tProv._componentChunks.Values)
            {
                foreach(int chunkID in tChunk.adjacentChunks)
                {
                    int ownerProv = owners[chunkID];

                    if(!adjacentProvinces.Contains(ownerProv) && ownerProv != id)
                    {
                        adjacentProvinces.Add(ownerProv);
                    }
                }
            }

            tProv.AppendAdjacentProvs(ref adjacentProvinces);
            id++;
        }
    }
    private void CultureLanguages(ref System.Random rnd) //Sets the language for each culture based on proximity
    {
        List<string> languages = new List<string>(){ "Asian", "Colonial", "European", "Indian", "Muslim", "Latin", "Pacific" };
        int lanCount = Math.Min(5,languages.Count);
        for (int i=0;i<languages.Count;i++) //Shuffle set
        {
            int swapNum = rnd.Next(0, languages.Count);
            string store = languages[i];
            languages[i] = languages[swapNum];
            languages[swapNum] = store;
        }

        float cultsPerLanguage = ((float)(cultures.Count - 1) / (float)(lanCount)); //Check number of cultures to assign to each language
        int id = 0;

        Dictionary<Vector3, Culture> unSetCultures = new Dictionary<Vector3, Culture>();

        foreach(Culture tCult in cultures)
        {
            if (tCult._id != "0") //Skip ocean culture
            {
                unSetCultures.Add(provinceSaveables.First(p => p._cultureID.ToString() == tCult._id).CalculateRelativeCenterPoint(), tCult); //Gets the centerpoint of a province in the culture group
            }
        }

        //Get a random cultureVector
        while (id < lanCount)
        {
            KeyValuePair<Vector3, Culture> tmp = unSetCultures.ElementAt(rnd.Next(0, unSetCultures.Count)); //Get random key value pair
            unSetCultures.Remove(tmp.Key); //Remove from set of unsetCultures
            tmp.Value._nameType = languages[id];
            Dictionary<Culture, float> distances = new Dictionary<Culture, float>();

            foreach(KeyValuePair<Vector3,Culture> tCult in unSetCultures)
            {
                distances.Add(tCult.Value, Vector3.Distance(tmp.Key, tCult.Key));
            }

            if(id == lanCount -1) { cultsPerLanguage = unSetCultures.Count - 1; } //If this is the last language, set all remaining cultures
            List<Culture> culturesToSetLang = (distances.OrderBy(x => x.Value).Take((int)(Math.Floor(cultsPerLanguage)))).Select(p=>p.Key).ToList(); //Get a proportion of the cultures based on distance to the first province in the set.

            foreach(Culture aCult in culturesToSetLang)
            {
                aCult._nameType = languages[id];
                unSetCultures.Remove(unSetCultures.FirstOrDefault(x => x.Value == aCult).Key); //Remove this culture from the set of unset languages
            }

            id++;

        }

        string smallestLan = "";
        for (int i = 0; i < lanCount; i++) //Shuffle set
        {
            int lCount = cultures.Count(x => x._nameType == languages[i] && x._id != "0");
            if(smallestLan.Length < lCount || smallestLan == "")
            {
                smallestLan = languages[i];
            }

        }

        //Any leftover (likely just ocean culture but worth setting for robustness)
        List<Culture> finalUnsets = cultures.Where(x => x._nameType == null || !(languages.Contains(x._nameType))).ToList();
        foreach (Culture tCult in finalUnsets)
        {
            tCult._nameType = smallestLan;
        }
    }
    private void FindNewCultureLocation(ref System.Random rnd, ref int cultureID, ref List<ProvinceObject> expandableProvinces, ref List<ProvinceObject> potentialTargets)
    {
        int newCultureTarget = rnd.Next(0, potentialTargets.Count);

        Culture newCult = new Culture(cultureID.ToString(), ref rnd);
        cultures.Add(newCult);
        potentialTargets[newCultureTarget]._cultureID = cultureID;
        expandableProvinces.Add(potentialTargets[newCultureTarget]);
        potentialTargets.RemoveAt(newCultureTarget); //Removes from set of potential options
        cultureID++;
    }
    private List<ProvinceObject> CultureLocationsLeft()
    {
        List<ProvinceObject> potentialTargets = provinceSaveables.Where(
        prov => prov._cultureID == 0 && prov._biome != 0).ToList(); //This returns references

        return potentialTargets;
    }
    public void CreateCultures(ref System.Random rnd) //Sets cultures across the map
    {
        int culturesToGenerate = (provinceSaveables.Where(prov => prov._biome != 0)).Count() / 20;
        List<int> culturedTiles = new List<int>();
        List<ProvinceObject> expandableProvinces = new List<ProvinceObject>();
        int cultureID = 1;

        {
            List<ProvinceObject> potentialTargets = provinceSaveables.Where(
                prov => prov._cultureID == 0
                && prov._adjacentProvIDs.Count > 3
                && prov._biome != 0).ToList(); //This returns references

            while (culturesToGenerate > 0)
            {
                if(potentialTargets.Count == 0) { break; }

                FindNewCultureLocation(ref rnd, ref cultureID, ref expandableProvinces, ref potentialTargets);
                culturesToGenerate--;
            }

            potentialTargets = null;
        }


        while(true) //expand all cultures that can be expanded. End when there are no expandable provinces left
        {
            int expansionTarget; //The province to expand

            //Randomly get culture, select province within culture group to expand
            List<string> expandableCultures = (from prov in expandableProvinces where prov._cultureID != 0 select prov._cultureID.ToString()).ToList();
            if(expandableCultures.Count <= 0) { expansionTarget = -1;}
            expandableCultures = expandableCultures.Distinct().ToList();
            string cultureTarget = expandableCultures[rnd.Next(0, expandableCultures.Count)];

            expandableCultures = null;
            List<ProvinceObject> possibleCultureProvs = (from prov in expandableProvinces where prov._cultureID.ToString() == cultureTarget select prov).ToList();
            expansionTarget = expandableProvinces.IndexOf(possibleCultureProvs[rnd.Next(0, possibleCultureProvs.Count)]);

            if (expansionTarget != -1)
            {
                if (expandableProvinces[expansionTarget]._adjacentProvIDs.Count > 0)
                {
                    foreach (int expandableID in expandableProvinces[expansionTarget]._adjacentProvIDs) //Adds to adjacent provinces
                    {
                        if (provinceSaveables[expandableID]._cultureID == 0 && provinceSaveables[expandableID]._biome != 0)
                        {
                            provinceSaveables[expandableID]._cultureID = expandableProvinces[expansionTarget]._cultureID;
                            expandableProvinces.Add(provinceSaveables[expandableID]);
                        }
                    }
                }

                expandableProvinces.RemoveAt(expansionTarget); //Remove from list as this target has been completed
            }

            if(expandableProvinces.Count == 0) //When no more expansion can be completed
            {
                List<ProvinceObject> locationsLeft = CultureLocationsLeft();

                if(locationsLeft.Count == 0) { break; } //When all provinces have a location
                else
                {
                    FindNewCultureLocation(ref rnd, ref cultureID, ref expandableProvinces, ref locationsLeft); //Append new location to expandables, ensuring all provinces will recieve a culture
                }
            }
        }

        CultureLanguages(ref rnd);

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
        //This originally used actual deciles but now uses average-based calculations to minimize memory usage
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