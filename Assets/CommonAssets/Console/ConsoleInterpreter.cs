using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Act;

namespace ConsoleInterpret
{ 
    public class ConsoleInterpreter
    {
        public string InterpretCommand(string comm, GameObject provViewer)
        {
            switch(comm)
            {
                case "DEBUG":
                    Act.Actions.ToggleDebug(provViewer.GetComponent<ProvinceViewerBehaviour>().infoModeBtns[1].gameObject);
                    return "Toggled debug mode";
                    break;

                default:
                    return "Invalid command";
                    break;
            }
        }
    }
}