using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; //Scene switching data
using WorldProperties;
using Empires;
using System.Linq;
using Calendar;
using Act;
public class SimulatorMainHandler : MonoBehaviour
{
    public Button exitButton;
    //UI
    private Texture2D mapTexture;
    private Texture2D maskTexture;
    private GameObject panelScreen;
    private GameObject provinceDetailsScreen;
    private GameObject startScreen;
    private GameObject consoleObject;
    public GameObject Camera;

    int mapWidth = 6000;
    int mapHeight = 4000;
    Date _date = new Date();
    string worldName = "";

    public GameObject startPrefab;
    public GameObject panelPrefab;
    public GameObject provinceDetailsPrefab;
    public GameObject loadMapPrefab;
    public GameObject consolePrefab;

    //Time controls
    public Button pause;
    public Button normal;
    public Button fast;
    public Button veryFast;
    public Text currentDate;
    private Calendar.Calendar.timeSettings simSpeed;
    private bool processing = false;
    private float timePassed = 0;

    private GameObject loadMap;
    private string filePath;
    private List<ProvinceObject> provinces;
    private List<Culture> cultures;
    private List<Empire> empires;
    private List<Religion> religions;

    private System.Random rnd = new System.Random();
    private bool consoleIsActive = true;
    void Start()
    {
        provinces = new List<ProvinceObject>();
        cultures = new List<Culture>();
        empires = new List<Empire>();
        religions = new List<Religion>();

        exitButton.GetComponent<Button>().onClick.AddListener(ExitScene);
        Camera.GetComponent<CameraScript>().enabled = false; //Allows camera movement

        startScreen = Instantiate(startPrefab, Camera.transform, false); //Create new start screen instance
        startScreen.GetComponent<FileBrowserBehaviour>().startBtn.onClick.AddListener(delegate { StartLoad(); });

        pause.interactable = false;
        normal.interactable = false;
        fast.interactable = false;
        veryFast.interactable = false;
        simSpeed = Calendar.Calendar.timeSettings.Pause;
    }
    void StartLoad()
    {
        try
        {
            filePath = startScreen.GetComponent<FileBrowserBehaviour>().ReturnPath(); //append filepath to dataset

            {
                Dictionary<string, string> prop = SaveLoad.SavingScript.LoadBaseData(filePath);
                mapWidth = Convert.ToInt32(prop["Width"]);
                mapHeight = Convert.ToInt32(prop["Height"]);
                _date.year = Convert.ToInt32(prop["Year"]);
                _date.month = Convert.ToInt32(prop["Month"]);
                _date.day = Convert.ToInt32(prop["Day"]);
                worldName = prop["Name"];

                mapTexture = new Texture2D(mapWidth, mapHeight);
                (byte[],byte[]) mapBytes = SaveLoad.SavingScript.LoadMap(filePath, mapWidth, mapHeight);
                mapTexture.LoadImage(mapBytes.Item1);
                maskTexture = new Texture2D(mapWidth, mapHeight);
                maskTexture.LoadImage(mapBytes.Item2);

                SaveLoad.SavingScript.LoadReligions(filePath, ref religions);
                SaveLoad.SavingScript.LoadEmpires(filePath, ref empires, ref religions);
                SaveLoad.SavingScript.LoadProvinces(filePath, ref provinces, ref empires, ref religions);
                SaveLoad.SavingScript.LoadCultures(filePath, ref cultures);
            }

            Destroy(startScreen); //remove screen from memory after getting applicable data

            loadMap = Instantiate(loadMapPrefab, null); //Create new map instance
            loadMap.name = "Map";

            panelScreen = Instantiate(panelPrefab, Camera.transform, false); //Add control panel
            provinceDetailsScreen = Instantiate(provinceDetailsPrefab, Camera.transform, false); //Add provViewer
            consoleObject = Instantiate(consolePrefab, Camera.transform, false); //Add console
            consoleObject.GetComponent<ConsoleScript>().LoadConsole(ref provinceDetailsScreen, ref provinces, ref cultures, ref empires, ref loadMap, ref religions, ref _date, ref rnd);
            ToggleConsole();

            loadMap.GetComponent<LoadMap>().ApplyProperties(mapWidth, mapHeight, ref provinces, ref cultures, ref panelScreen, ref provinceDetailsScreen, ref mapTexture, ref maskTexture, ref religions, ref empires);
            loadMap.GetComponent<LoadMap>().StartMap(ref Camera);

            pause.onClick.AddListener(delegate { Calendar.Calendar.PauseTime(ref simSpeed, ref pause, ref normal, ref fast, ref veryFast); });
            normal.onClick.AddListener(delegate { Calendar.Calendar.NormalTime(ref simSpeed, ref pause, ref normal, ref fast, ref veryFast); });
            fast.onClick.AddListener(delegate { Calendar.Calendar.FastTime(ref simSpeed, ref pause, ref normal, ref fast, ref veryFast); });
            veryFast.onClick.AddListener(delegate { Calendar.Calendar.VeryFastTime(ref simSpeed, ref pause, ref normal, ref fast, ref veryFast); });

            Calendar.Calendar.PauseTime(ref simSpeed, ref pause, ref normal, ref fast, ref veryFast); //auto pause
            currentDate.text = Calendar.Calendar.SetDate(0, ref _date);

            foreach (Empire tEmp in empires) //Reset opinions
            {
                tEmp.PollOpinions(ref _date, ref empires, ref provinces, ref cultures);
            }

        }
        catch (Exception ex)
        {
            Debug.Log("CRASH! " + ex);
            //Add error handling here TODO
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote) && consoleObject != null) { ToggleConsole(); } //on ` key press open console

        timePassed += Time.deltaTime;
        if (simSpeed != Calendar.Calendar.timeSettings.Pause && processing == false && timePassed > Calendar.Calendar.runSpeed[simSpeed])
        {
            timePassed = 0;
            currentDate.text = Calendar.Calendar.SetDate(1, ref _date);

            processing = true;

            //time events
            if(_date.day == 1)
            {
                bool isYearStart = _date.month == 1;
                foreach(Empire tEmp in empires)
                {
                    tEmp.PollOpinions(ref _date, ref empires, ref provinces, ref cultures);
                    if (isYearStart && tEmp._exists) { tEmp.ReduceUnrest(provinces, ref rnd); } //Reduce the unrest of the nation
                }

            }
            else if (_date.day == 2)
            {
                Act.Actions.UpdateCultures(ref cultures, ref provinces, ref empires);

                if (_date.month % 3 == 0)
                {
                    Act.Actions.UpdateMilitary(ref cultures, ref empires, ref provinces, ref rnd);
                }
            }

            if (!SpawnEmpire()) //Attempt to spawn a new empire
            {
                SpawnReligion(); //If spawning a new empire fails, attempt to spawn a new religion
            }

            SpreadReligionNaturally(); //Spread religions if they exist

            foreach (Empire tEmp in empires) //attempts to get an action for each empire 
            {
                tEmp.PollForAction(ref _date, ref cultures, ref empires, ref provinces, ref religions, ref rnd);
            }

            processing = false;

        }
    }
    private bool SpawnEmpire()
    {
        int mMax = 200 + Math.Min(_date.year, 500); //Spawning slows down over time
        int rnTick = rnd.Next(0, mMax);

        if (rnTick == rnd.Next(0,mMax))  //1/((100+y)*2) chance
        {
            int empCount = empires.Count(x => x._exists); //Number of empires in existance

            if (empCount == 0) //If no empires exist
            {
                SpawnCase("ANYHIGHPOP"); //Spawn on a highpop location
                return true;
            }
            else if(rnd.Next(0,4) == 1)
            {
                if (rnTick < (int)Math.Floor((float)(mMax) / 7)) //1/7 chance
                {
                    SpawnCase("ANYUNPOPULATED"); //Spawn in any location with no population in the culture group and a medium
                    return true;
                }
                else if (rnTick > (int)Math.Floor((float)(mMax) / 3)) //2/3 chance
                {
                    SpawnCase("ANYPOPCULTURE"); //Spawn in any location in a populated culture group
                    return true;
                }
                else //1/3 chance
                {
                    SpawnCase("ANYPOPREGION"); //Spawn in any location in a populated culture group or high pop area
                    return true;
                }
            }
        }

        return false;
    }
    private void SpawnReligion()
    { 
        if (empires.Count() > 5)
        {
            int mMax = 15000 + Math.Min(_date.year, 500); 
            int rnTick = rnd.Next(0, mMax);

            if (rnTick == rnd.Next(0, mMax))  
            {
                List<Religion> existingRels = provinces.Where(x => x._localReligion != null).Select(t => t._localReligion).Distinct().ToList();
                if(existingRels.Count() > 20) { return; }

                List<ProvinceObject> applicableProvs = provinces.Where(x => (x._population == Property.Medium || x._population == Property.High) && x._localReligion == null && x._biome != 0).ToList();

                if (applicableProvs.Count > 0)
                {
                    int tID = applicableProvs[rnd.Next(0, applicableProvs.Count)]._id;
                    if(Act.Actions.NewReligion(ref provinces, ref religions, tID))
                    {
                        provinces[tID].updateText = provinces[tID]._localReligion._name + " Founded";
                    }
                    
                }
            }
        }
    }
    private void SpreadReligionNaturally() //Non-empire spread religion
    {
        if (rnd.Next(0, 2000) == 55)
        {
            List<Religion> appReligions = provinces.Where(x=>x._localReligion!=null).Select(t => t._localReligion).Distinct().ToList();

            if (appReligions.Count > 0)
            {

                int selCount = rnd.Next(0, appReligions.Count);
                if(selCount == 0) { return; }

                List<int> targetRels = new List<int>() { };
                for (int i = 0; i < selCount; i++)
                {
                    int newR = rnd.Next(0, appReligions.Count);
                    if(targetRels.Contains(newR))
                    {
                        i--;
                    }
                    else
                    {
                        targetRels.Add(newR);
                    }
                }

                if(targetRels.Count <= 0) { return; }

                foreach (Religion rel in appReligions)
                {
                    if (targetRels.Contains(appReligions.IndexOf(rel)))
                    {
                        List<ProvinceObject> applicableProvs = provinces.Where(x => x._localReligion == rel && x._adjacentProvIDs.Any(y=> provinces[y]._biome != 0 && provinces[y]._localReligion != rel)).ToList();

                        if (applicableProvs.Count > 0)
                        {
                            int spreadCount;
                            if (applicableProvs.Count > 9) { spreadCount = rnd.Next(1, Convert.ToInt32(Math.Floor((float)(applicableProvs.Count)/3.0f))); }
                            else if(applicableProvs.Count > 3) { spreadCount = rnd.Next(1, applicableProvs.Count); }
                            else { spreadCount = 1; }


                            for (int i = 0; i < spreadCount; i++)
                            {
                                Debug.Log("SPREADING " + rel._id + ":" + rel._name);
                                ProvinceObject tProv = applicableProvs[rnd.Next(0, applicableProvs.Count())];
                                List<ProvinceObject> adjProvs = tProv._adjacentProvIDs.Where(y => provinces[y]._biome != 0 && provinces[y]._localReligion != rel).Select(x => provinces[x]).ToList();

                                foreach (ProvinceObject aProv in adjProvs)
                                {
                                    if (rnd.Next(0, 2) == 1) //Last random chance
                                    {
                                        aProv._localReligion = rel; //Set new religion
                                        aProv.updateText = aProv._localReligion._name + " adopted";
                                    }
                                }
                                applicableProvs.Remove(tProv);


                            }
                        }
                    }
                }
            }
        }
    }
    private void SpawnCase(string spawnCase) //Handles each empire spawning type
    {
        switch(spawnCase)
        {
            case "ANYHIGHPOP": //Any high population region
                {
                    List<ProvinceObject> applicableProvs = provinces.Where(x => x._population == Property.High && x._ownerEmpire == null && x._biome != 0).ToList();
                    if (applicableProvs.Count(x => x._isCoastal) > 0) { applicableProvs = applicableProvs.Where(x => x._isCoastal).ToList(); }

                    if (applicableProvs.Count > 0)
                    {
                        int tID = applicableProvs[rnd.Next(0, applicableProvs.Count)]._id;
                        Act.Actions.SpawnEmpire(ref provinces, tID, ref empires, ref cultures, ref rnd);
                    }
                    else
                    {
                        SpawnCase("ANYPOPCULTURE"); 
                    }
                }
                break;
            case "ANYPOPREGION": //(Any high population region) or (medium population region where there is a high pop nation in the area)
                {
                    List<int> popAreas = provinces.Where(x => x._ownerEmpire != null).Select(x => x._cultureID).Distinct().ToList(); //Cultures with nations within
                    List<ProvinceObject> applicableProvs = provinces.Where(x => (x._population == Property.High || (x._population == Property.Medium && popAreas.Contains(x._cultureID))) && x._ownerEmpire == null && x._biome != 0).ToList();
                    if (applicableProvs.Count(x => x._isCoastal) > 0) {
                        if (rnd.Next(0, 3) == 1)
                        {
                            applicableProvs = applicableProvs.Where(x => x._isCoastal).ToList();
                        }
                    } //Coastal regions get priority

                    if (applicableProvs.Count > 0)
                    {
                        int tID = applicableProvs[rnd.Next(0, applicableProvs.Count)]._id;
                        Act.Actions.SpawnEmpire(ref provinces, tID, ref empires, ref cultures, ref rnd);
                    }
                    else
                    {
                        SpawnCase("ANYUNPOPULATED"); //Default to an unpopulated region
                    }
                }
                break;
            case "ANYPOPCULTURE": //Anywhere med or high in a populated region
                {
                    List<int> popAreas = provinces.Where(x => x._ownerEmpire != null).Select(x => x._cultureID).Distinct().ToList(); //Cultures with nations within
                    List<ProvinceObject> applicableProvs = provinces.Where(x => (x._population == Property.High || x._population == Property.Medium) && popAreas.Contains(x._cultureID) && x._ownerEmpire == null && x._biome != 0).ToList();
                    if (applicableProvs.Count(x => x._isCoastal) > 0)
                    {
                        if (rnd.Next(0, 3) == 1)
                        {
                            applicableProvs = applicableProvs.Where(x => x._isCoastal).ToList();
                        }
                    } //Coastal regions get priority

                    if (applicableProvs.Count > 0)
                    {
                        int tID = applicableProvs[rnd.Next(0, applicableProvs.Count)]._id;
                        Act.Actions.SpawnEmpire(ref provinces, tID, ref empires, ref cultures, ref rnd);
                    }
                    else
                    {
                        SpawnCase("ANYUNPOPULATED"); //Default to an unpopulated region
                    }
                }
                break;
            case "ANYUNPOPULATED": //Any unpopulated region med or high
                {
                    List<int> unPopAreas = provinces.Where(x => x._ownerEmpire == null).Select(x => x._cultureID).Distinct().ToList(); //All unpopulated areas
                    List<ProvinceObject> applicableProvs = provinces.Where(x => (x._population == Property.High || x._population == Property.Medium) && unPopAreas.Contains(x._cultureID) && x._ownerEmpire == null && x._biome != 0).ToList();
                    if(applicableProvs.Any(x=>x._population == Property.High)) { applicableProvs = applicableProvs.Where(x => x._population == Property.High).ToList(); } //If there are high pop regions in the zone, these take priority
                    
                    if (applicableProvs.Count(x => x._isCoastal) > 0)
                    {
                        if (rnd.Next(0, 3) == 1)
                        {
                            applicableProvs = applicableProvs.Where(x => x._isCoastal).ToList();
                        }
                    } //Coastal regions get priority


                    if (applicableProvs.Count > 0)
                    {
                        int tID = applicableProvs[rnd.Next(0, applicableProvs.Count)]._id;
                        Act.Actions.SpawnEmpire(ref provinces, tID, ref empires, ref cultures, ref rnd);
                    }
                    else
                    {
                        SpawnCase("DEFAULT");
                    }
                }
                break;
            default:
                {
                    List<ProvinceObject> applicableProvs = provinces.Where(x => x._ownerEmpire == null).ToList();
                    if (applicableProvs.Count(x => x._isCoastal) > 0)
                    {
                        if (rnd.Next(0, 3) == 1)
                        {
                            applicableProvs = applicableProvs.Where(x => x._isCoastal).ToList();
                        }
                    } //Coastal regions get priority


                    if (applicableProvs.Count > 0)
                    {
                        int tID = applicableProvs[rnd.Next(0, applicableProvs.Count)]._id;
                        Act.Actions.SpawnEmpire(ref provinces, tID, ref empires, ref cultures, ref rnd);
                    }
                    else
                    { } //Since is the default, if there are no possibilities, then it must do nothing
                }
                break;
        }
    }
    void ToggleConsole()
    {
        consoleIsActive = !consoleIsActive;

        //Toggle camera mode and console mode with tilde

        consoleObject.gameObject.SetActive(consoleIsActive);
        Camera.GetComponent<CameraScript>().enabled = !consoleIsActive;

        if(consoleIsActive)
        {
            consoleObject.GetComponent<ConsoleScript>().ResetInput();
            consoleObject.GetComponent<ConsoleScript>().textInput.ActivateInputField(); //Automatically selects the input field
        }
    }
    void ExitScene()
    {
        if(simSpeed != Calendar.Calendar.timeSettings.Pause)
        {
            Calendar.Calendar.PauseTime(ref simSpeed, ref pause, ref normal, ref fast, ref veryFast); //auto pause
        }
        if (provinces.Count > 0) //Check if the data has been loaded before saving
        {
            SaveLoad.SavingScript.SaveEmpires(filePath, ref empires, ref provinces); //Save empire data
            SaveLoad.SavingScript.CreateCultures(filePath, ref cultures, true);
            SaveLoad.SavingScript.CreateFile(mapWidth, mapHeight, true, filePath,_date.day,_date.month,_date.year,worldName);
        }
        SceneManager.LoadScene("Main Menu", LoadSceneMode.Single); //Opens the world generator scene in place of this scene
    }

}
