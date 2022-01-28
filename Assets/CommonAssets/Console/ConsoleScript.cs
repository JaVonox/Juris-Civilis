using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ConsoleInterpret;
public class ConsoleScript : MonoBehaviour
{
    public InputField textInput;
    public Text consoleLog;

    private GameObject refProvDetails;
    private ConsoleInterpreter interpreter;
    private string LoggedText;

    // Start is called before the first frame update
    void Start()
    {
        interpreter = new ConsoleInterpreter(); //New console interpreter instance
        consoleLog.text = "";
        ResetInput();
    }
    
    public void LoadConsole(ref GameObject provDetails)
    {
        refProvDetails = provDetails;
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
            LoggedText += interpreter.InterpretCommand(textInput.text, refProvDetails) + "\n";
            //Submit, add to log and then remove the text
            consoleLog.text = LoggedText;
            ResetInput();   
        }
    }
}
