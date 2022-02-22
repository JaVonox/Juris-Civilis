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
    int year = 0;
    int month = 0;
    int day = 0;
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
                year = Convert.ToInt32(prop["Year"]);
                month = Convert.ToInt32(prop["Month"]);
                day = Convert.ToInt32(prop["Day"]);
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
            consoleObject.GetComponent<ConsoleScript>().LoadConsole(ref provinceDetailsScreen, ref provinces, ref cultures, ref empires, ref loadMap, ref religions);
            ToggleConsole();

            loadMap.GetComponent<LoadMap>().ApplyProperties(mapWidth, mapHeight, ref provinces, ref cultures, ref panelScreen, ref provinceDetailsScreen, ref mapTexture, ref maskTexture, ref religions, ref empires);
            loadMap.GetComponent<LoadMap>().StartMap(ref Camera);

            pause.onClick.AddListener(delegate { Calendar.Calendar.PauseTime(ref simSpeed, ref pause, ref normal, ref fast, ref veryFast); });
            normal.onClick.AddListener(delegate { Calendar.Calendar.NormalTime(ref simSpeed, ref pause, ref normal, ref fast, ref veryFast); });
            fast.onClick.AddListener(delegate { Calendar.Calendar.FastTime(ref simSpeed, ref pause, ref normal, ref fast, ref veryFast); });
            veryFast.onClick.AddListener(delegate { Calendar.Calendar.VeryFastTime(ref simSpeed, ref pause, ref normal, ref fast, ref veryFast); });

            Calendar.Calendar.PauseTime(ref simSpeed, ref pause, ref normal, ref fast, ref veryFast); //auto pause
            currentDate.text = Calendar.Calendar.SetDate(0, ref year, ref month, ref day);

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
        if(simSpeed != Calendar.Calendar.timeSettings.Pause && processing == false && timePassed > Calendar.Calendar.runSpeed[simSpeed])
        {
            timePassed = 0;
            currentDate.text = Calendar.Calendar.SetDate(1, ref year, ref month, ref day);

            processing = true;
            
            //time events
            if(day == 2)
            {
                Act.Actions.UpdateCultures(ref cultures, ref provinces, ref empires);

                if (month % 3 == 0)
                {
                    Act.Actions.UpdateMilitary(ref cultures, ref empires, ref provinces);
                }
            }

            SpawnEmpire(); 

            foreach (Empire tEmp in empires) //attempts to get an action for each empire 
            {
                tEmp.PollForAction((day,month,year), ref cultures, ref empires);
            }

            processing = false;
            
        }
    }
    private void SpawnEmpire()
    {
        int mMax = 100 + Math.Min(year, 500); //Spawning slows down over time
        int rnTick = rnd.Next(0, mMax);

        if (rnTick == rnd.Next(0,mMax))  //1/((100+y)*2) chance
        {
            int empCount = empires.Count(x => x._exists); //Number of empires in existance

            if (empCount == 0) //If no empires exist
            {
                SpawnCase("ANYHIGHPOP"); //Spawn on a highpop location
            }
            else if(rnd.Next(0,Math.Min(15,empCount+2)) == 1)
            {
                Debug.Log("SECONDTICK");

                if (rnTick > (int)Math.Floor((float)(mMax) / 3)) //2/3 chance
                {
                    SpawnCase("ANYPOPCULTURE"); //Spawn in any location in a populated culture group
                }
                else //1/3 chance
                {
                    SpawnCase("ANYPOPREGION"); //Spawn in any location in a populated culture group or high 
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
                        Act.Actions.SpawnEmpire(ref provinces, tID, ref empires, ref cultures);
                    }
                    else
                    {
                        SpawnCase("ANYPOPCULTURE"); 
                    }
                }
                break;
            case "ANYPOPREGION": //Any high population region or medium population region where there is a high pop nation in the area
                {
                    List<int> popAreas = provinces.Where(x => x._ownerEmpire != null).Select(x => x._cultureID).Distinct().ToList(); //Get all culture IDs with empires within the
                    List<ProvinceObject> applicableProvs = provinces.Where(x => (x._population == Property.High || (x._population == Property.Medium && popAreas.Contains(x._cultureID))) && x._ownerEmpire == null && x._biome != 0).ToList();
                    if (applicableProvs.Count(x => x._isCoastal) > 0) { applicableProvs = applicableProvs.Where(x => x._isCoastal).ToList(); } //Coastal regions get priority

                    if (applicableProvs.Count > 0)
                    {
                        int tID = applicableProvs[rnd.Next(0, applicableProvs.Count)]._id;
                        Act.Actions.SpawnEmpire(ref provinces, tID, ref empires, ref cultures);
                    }
                    else
                    {
                        SpawnCase("DEFAULT");
                    }
                }
                break;
            case "ANYPOPCULTURE":
                {
                    List<int> popAreas = provinces.Where(x => x._ownerEmpire != null).Select(x => x._cultureID).Distinct().ToList(); //Get all culture IDs with empires within the
                    List<ProvinceObject> applicableProvs = provinces.Where(x => (x._population == Property.High || x._population == Property.Medium) && popAreas.Contains(x._cultureID) && x._ownerEmpire == null && x._biome != 0).ToList();
                    if (applicableProvs.Count(x => x._isCoastal) > 0) { applicableProvs = applicableProvs.Where(x => x._isCoastal).ToList(); } //Coastal regions get priority

                    if (applicableProvs.Count > 0)
                    {
                        int tID = applicableProvs[rnd.Next(0, applicableProvs.Count)]._id;
                        Act.Actions.SpawnEmpire(ref provinces, tID, ref empires, ref cultures);
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
                    if (applicableProvs.Count(x => x._isCoastal) > 0) { applicableProvs = applicableProvs.Where(x => x._isCoastal).ToList(); }

                    if (applicableProvs.Count > 0)
                    {
                        int tID = applicableProvs[rnd.Next(0, applicableProvs.Count)]._id;
                        Act.Actions.SpawnEmpire(ref provinces, tID, ref empires, ref cultures);
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
            SaveLoad.SavingScript.CreateFile(mapWidth, mapHeight, true, filePath,day,month,year,worldName);
        }
        SceneManager.LoadScene("Main Menu", LoadSceneMode.Single); //Opens the world generator scene in place of this scene
    }

}
