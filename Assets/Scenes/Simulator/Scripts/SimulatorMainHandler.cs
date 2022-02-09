using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; //Scene switching data
using WorldProperties;
using Empires;
using Calendar;
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

    private System.Random rnd = new System.Random();
    private bool consoleIsActive = true;
    void Start()
    {
        provinces = new List<ProvinceObject>();
        cultures = new List<Culture>();
        empires = new List<Empire>();
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

                mapTexture = new Texture2D(mapWidth, mapHeight);
                (byte[],byte[]) mapBytes = SaveLoad.SavingScript.LoadMap(filePath, mapWidth, mapHeight);
                mapTexture.LoadImage(mapBytes.Item1);
                maskTexture = new Texture2D(mapWidth, mapHeight);
                maskTexture.LoadImage(mapBytes.Item2);

                SaveLoad.SavingScript.LoadEmpires(filePath, ref empires);
                SaveLoad.SavingScript.LoadProvinces(filePath, ref provinces, ref empires);
                SaveLoad.SavingScript.LoadCultures(filePath, ref cultures);
            }

            Destroy(startScreen); //remove screen from memory after getting applicable data

            loadMap = Instantiate(loadMapPrefab, null); //Create new map instance
            loadMap.name = "Map";

            panelScreen = Instantiate(panelPrefab, Camera.transform, false); //Add control panel
            provinceDetailsScreen = Instantiate(provinceDetailsPrefab, Camera.transform, false); //Add provViewer
            consoleObject = Instantiate(consolePrefab, Camera.transform, false); //Add console
            consoleObject.GetComponent<ConsoleScript>().LoadConsole(ref provinceDetailsScreen, ref provinces, ref cultures, ref empires, ref loadMap);
            ToggleConsole();

            loadMap.GetComponent<LoadMap>().ApplyProperties(mapWidth, mapHeight, ref provinces, ref cultures, ref panelScreen, ref provinceDetailsScreen, ref mapTexture, ref maskTexture);
            loadMap.GetComponent<LoadMap>().StartMap();

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
            if(day == 1)
            {
                Act.Actions.UpdateCultures(ref cultures, ref provinces, ref empires);

                if (month % 3 == 0)
                {
                    Act.Actions.UpdateMilitary(ref cultures, ref empires, ref provinces);
                }
            }

            processing = false;
            
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
            SaveLoad.SavingScript.CreateFile(mapWidth, mapHeight, true, filePath,day,month,year);
        }
        SceneManager.LoadScene("Main Menu", LoadSceneMode.Single); //Opens the world generator scene in place of this scene
    }

}
