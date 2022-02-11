using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ConsoleInterpret;
using WorldProperties;
using BiomeData;
using Empires;
public class ConsoleScript : MonoBehaviour
{
    public InputField textInput;
    public Text consoleLog;

    private GameObject refProvDetails;
    private ConsoleInterpreter interpreter;

    //Should be references
    private List<ProvinceObject> _provinces;
    private List<Culture> _cultures;
    private List<Empire> _empires;
    private List<Religion> _religions;
    private GameObject _loadedMap;

    private string LoggedText;

    // Start is called before the first frame update
    void Start()
    {
        interpreter = new ConsoleInterpreter(); //New console interpreter instance
        consoleLog.text = "";
        ResetInput();
    }
    
    public void LoadConsole(ref GameObject provDetails, ref List<ProvinceObject> provs, ref List<Culture> cultures, ref List<Empire> empires, ref GameObject map, ref List<Religion> religions)
    {
        refProvDetails = provDetails;
        _provinces = provs;
        _cultures = cultures;
        _empires = empires;
        _religions = religions;
        _loadedMap = map;
    }
    public void ResetInput()
    {
        textInput.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)) { SubmitCommand(); textInput.ActivateInputField(); }
    }
    void SubmitCommand()
    {
        if (textInput.text != "")
        {
            LoggedText = interpreter.InterpretCommand(textInput.text, refProvDetails, ref _provinces, ref _cultures, ref _empires, ref _loadedMap, ref _religions) + "\n" + LoggedText;
            //Submit, add to log and then remove the text
            consoleLog.text = LoggedText;
            ResetInput();   
        }
    }
}
