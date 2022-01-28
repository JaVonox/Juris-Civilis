using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ConsoleScript : MonoBehaviour
{
    public InputField textInput;
    public Text consoleLog;

    private string LoggedText;

    // Start is called before the first frame update
    void Start()
    {
        consoleLog.text = "";
        ResetInput();
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
            //Submit, add to log and then remove the text
            LoggedText += textInput.text + "\n";
            consoleLog.text = LoggedText;
            ResetInput();   
        }
    }
}
