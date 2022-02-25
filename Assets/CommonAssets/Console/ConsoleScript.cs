using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ConsoleInterpret;
using WorldProperties;
using BiomeData;
using Empires;
using System.Linq;
using Calendar;
public class ConsoleScript : MonoBehaviour
{
    public InputField textInput;
    public Text consoleLog;
    public GameObject console;

    private GameObject refProvDetails;
    private ConsoleInterpreter interpreter;

    //Should be references
    private List<ProvinceObject> _provinces;
    private List<Culture> _cultures;
    private List<Empire> _empires;
    private List<Religion> _religions;
    private GameObject _loadedMap;

    private Date dateRef;

    private string LoggedText;

    // Start is called before the first frame update
    void Start()
    {
        interpreter = new ConsoleInterpreter(); //New console interpreter instance
        consoleLog.text = "";
        ResetInput();
    }
    
    public void LoadConsole(ref GameObject provDetails, ref List<ProvinceObject> provs, ref List<Culture> cultures, ref List<Empire> empires, ref GameObject map, ref List<Religion> religions, ref Date date)
    {
        refProvDetails = provDetails;
        _provinces = provs;
        _cultures = cultures;
        _empires = empires;
        _religions = religions;
        _loadedMap = map;
        dateRef = date;
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
            if (textInput.text.ToUpper() == "CLEAR")
            {
                consoleLog.text = "";
                LoggedText = "";
            }
            else
            {
                LoggedText = interpreter.InterpretCommand(textInput.text, refProvDetails, ref _provinces, ref _cultures, ref _empires, ref _loadedMap, ref _religions, ref dateRef) + "\n" + LoggedText;
                //Submit, add to log and then remove the text
                List<string> textLog = LoggedText.Split('\n').ToList();
                if (textLog.Count() > 15)
                {
                    LoggedText = string.Join("\n",textLog.GetRange(0,15).ToArray());
                }
                consoleLog.text = LoggedText;
            }
            ResetInput();
            Canvas.ForceUpdateCanvases();
        }
    }
}
