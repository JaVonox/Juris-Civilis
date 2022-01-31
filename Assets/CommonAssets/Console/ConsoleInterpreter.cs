using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Act;
using WorldProperties;
using BiomeData;
using Empires;
using System.Linq;

namespace ConsoleInterpret
{ 
    public class ConsoleInterpreter
    {
        public string InterpretCommand(string comm, GameObject provViewer, ref List<ProvinceObject> provs, ref List<Culture> cultures, ref List<Empire> empires)
        {
            try
            {
                List<string> commandSplit = comm.Split(' ').ToList();
                switch (commandSplit[0].ToUpper())
                {
                    case "DEBUG": //DEBUG - enters debug mode
                        Act.Actions.ToggleDebug(provViewer.GetComponent<ProvinceViewerBehaviour>().infoModeBtns[1].gameObject);
                        return "Toggled debug mode";
                    case "ECHO": //ECHO (ARGS) - echos back args
                        return String.Join(" ", commandSplit.GetRange(1, commandSplit.Count - 1));
                    case "SPAWN": //SPAWN (ID) - spawns empire at province
                        if (commandSplit.Count < 2) { return "Insufficient parameters"; }
                        if (Act.Actions.SpawnEmpire(ref provs, Convert.ToInt32(commandSplit[1]),ref empires)) { return "Spawned new empire"; }
                        else { return "Could not spawn an empire"; }
                    case "ADD": //ADD (PROVID) (EMPIREID)
                        if (commandSplit.Count < 3) { return "Insufficient parameters"; }
                        if (Act.Actions.ConquerLand(provs[Convert.ToInt32(commandSplit[1])], empires[Convert.ToInt32(commandSplit[2])])) { return "Added new land"; }
                        else { return "Could not spawn an empire"; }
                    default:
                        return "Invalid command";
                }
            }
            catch(Exception ex)
            {
                return "Error - " + ex.Message;
            }
        }
    }
}