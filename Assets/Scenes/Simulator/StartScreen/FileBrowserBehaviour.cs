using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SaveLoad;
public class FileBrowserBehaviour : MonoBehaviour
{
    public Button startBtn;
    public Dropdown filesApplicable;

    private string worldPath;
    private Dictionary<string, string> files = new Dictionary<string, string>();
    void Start()
    {
        startBtn.gameObject.SetActive(false); //Hide start button
        filesApplicable.onValueChanged.AddListener(delegate { NewSelectionMade(); });
        PopulateDropdown();
    }
    void PopulateDropdown()
    {
        files = SaveLoad.SavingScript.FindLoadables();

        filesApplicable.ClearOptions();
        filesApplicable.AddOptions(new List<string>{"None"});
        if (files != null) { filesApplicable.AddOptions(new List<string>(files.Keys)); }
    }
    void NewSelectionMade()
    {
        worldPath = null;
        if (filesApplicable.options[filesApplicable.value].text != "None")
        {
            worldPath = files[filesApplicable.options[filesApplicable.value].text]; //Set the selected path from the dictionary
            startBtn.gameObject.SetActive(true); //Show start button
        }
        else
        {
            startBtn.gameObject.SetActive(false); //Show start button
        }

    }
    public string ReturnPath()
    {
        return worldPath;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
