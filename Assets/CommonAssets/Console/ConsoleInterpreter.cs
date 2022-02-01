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
        public string InterpretCommand(string comm, GameObject provViewer, ref List<ProvinceObject> provs, ref List<Culture> cultures, ref List<Empire> empires, ref GameObject loadedMap)
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
                        if (provs.Count < Convert.ToInt32(commandSplit[1])) { return "Unrecognised integer ID supplied"; }
                        if (!ValueLimiter(commandSplit[1],0,provs.Count() - 1)) { return "Invalid ID parameters"; }

                        if (Act.Actions.SpawnEmpire(ref provs, Convert.ToInt32(commandSplit[1]),ref empires)) { ForceUpdate(ref loadedMap); return "Spawned new empire"; }
                        else { return "Could not spawn an empire"; }
                    case "ADD": //ADD (PROVID) (EMPIREID)
                        if (commandSplit.Count < 3) { return "Insufficient parameters"; }
                        if (!ValueLimiter(commandSplit[1], 0, provs.Count() - 1)) { return "Invalid ID parameters"; }
                        if (!ValueLimiter(commandSplit[2], 0, empires.Count() - 1)) { return "Invalid ID parameters"; }
                        if (provs.Count < Convert.ToInt32(commandSplit[1]) || empires.Count < Convert.ToInt32(commandSplit[2])) { return "Unrecognised integer ID supplied"; }

                        if (Act.Actions.ConquerLand(provs[Convert.ToInt32(commandSplit[1])], empires[Convert.ToInt32(commandSplit[2])])) { ForceUpdate(ref loadedMap); return "Added new land"; }
                        else { return "Could not add land to empire"; }
                    default:
                        return "Invalid command";
                }

            }
            catch(Exception ex)
            {
                return "Error - " + ex.Message;
            }
        }
        private bool ValueLimiter(string target, int lowerLimit, int upperLimit)
        {
            int tInt = Convert.ToInt32(target);
            return (tInt >= lowerLimit && tInt <= upperLimit);
        }

        private void ForceUpdate(ref GameObject loadedMap)
        {
            loadedMap.GetComponent<LoadMap>().UpdateMapMode("-1"); //-1 uses last processed command
        }
    }
}