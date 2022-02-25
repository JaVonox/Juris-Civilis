using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Act;
using WorldProperties;
using BiomeData;
using Empires;
using System.Linq;
using Calendar;

namespace ConsoleInterpret
{ 
    public class ConsoleInterpreter
    {
        public string InterpretCommand(string comm, GameObject provViewer, ref List<ProvinceObject> provs, ref List<Culture> cultures, ref List<Empire> empires, ref GameObject loadedMap, ref List<Religion> religions, ref Date currentDate)
        {
            try
            {
                List<string> commandSplit = comm.Split(' ').ToList();
                switch (commandSplit[0].ToUpper())
                {
                    case "DEBUG": //DEBUG - enters debug mode
                        {
                            Act.Actions.ToggleDebug(provViewer.GetComponent<ProvinceViewerBehaviour>().infoModeBtns[1].gameObject);
                            return "Toggled debug mode";
                        }
                    case "ECHO": //ECHO (ARGS) - echos back args
                        {
                            return String.Join(" ", commandSplit.GetRange(1, commandSplit.Count - 1));
                        }
                    case "EMPCOUNT": //EMPCOUNT - counts living empires
                        {
                            return empires.Count(x => x._exists).ToString();
                        }
                    case "SPAWN": //SPAWN (PROVID) - spawns empire at province
                        {
                            if (commandSplit.Count < 2) { return "Insufficient parameters"; }
                            if (provs.Count < Convert.ToInt32(commandSplit[1])) { return "Unrecognised integer ID supplied"; }
                            if (!ValueLimiter(commandSplit[1], 0, provs.Count() - 1)) { return "Invalid ID parameters"; }

                            if (Act.Actions.SpawnEmpire(ref provs, Convert.ToInt32(commandSplit[1]), ref empires, ref cultures)) { ForceUpdate(ref loadedMap); return "Spawned new empire"; }
                            else { return "Could not spawn an empire"; }
                        }
                    case "ADD": //ADD (PROVID) (EMPIREID) - adds to empire without cost or restrictions
                        {
                            if (commandSplit.Count < 3) { return "Insufficient parameters"; }
                            if (!ValueLimiter(commandSplit[1], 0, provs.Count() - 1)) { return "Invalid ID parameters"; }
                            if (!ValueLimiter(commandSplit[2], 0, empires.Count() - 1)) { return "Invalid ID parameters"; }
                            if(empires[Convert.ToInt32(commandSplit[2])]._exists == false) { return "The target empire is dead"; }
                            if (provs.Count < Convert.ToInt32(commandSplit[1]) || empires.Count < Convert.ToInt32(commandSplit[2])) { return "Unrecognised integer ID supplied"; }

                            if (Act.Actions.ForceConquerLand(provs[Convert.ToInt32(commandSplit[1])], empires[Convert.ToInt32(commandSplit[2])], ref provs)) { ForceUpdate(ref loadedMap); return "Added new land"; }
                            else { return "Could not add land to empire"; }
                        }
                    case "ECOUPDATE": //ECOUPDATE - updates economics data
                        {
                            Act.Actions.UpdateCultures(ref cultures, ref provs, ref empires);
                            return "Updated economics data";
                        }
                    case "TECHUP": //TECHUP (EMPIREID) (TYPE) - Increase a tech of an empire
                        {
                            if (commandSplit.Count < 3) { return "Insufficient parameters"; }
                            if (!ValueLimiter(commandSplit[1], 0, empires.Count() - 1)) { return "Invalid ID parameters"; }
                            if (empires[Convert.ToInt32(commandSplit[1])]._exists == false) { return "The target empire is dead"; }
                            if (Act.Actions.UpdateTech(ref empires, Convert.ToInt32(commandSplit[1]), commandSplit[2])) { return "Updated Tech"; }
                            else { return "Failed to update tech"; }
                        }
                    case "SPAWNMIL": //SPAWNMIL - spawn military units
                        {
                            Act.Actions.UpdateMilitary(ref cultures, ref empires, ref provs);
                            return "Spawned new military units";
                        }
                    case "SPAWNREL": //SPAWNREL (PROVID) - spawns new religion
                        {
                            if (commandSplit.Count < 2) { return "Insufficient parameters"; }
                            if (!ValueLimiter(commandSplit[1], 0, provs.Count() - 1)) { return "Invalid ID parameters"; }
                            if (Act.Actions.NewReligion(ref provs, ref religions, Convert.ToInt32(commandSplit[1])))
                            { return "Generated new Religion"; }
                            else { return "Failed to generate religion"; }
                        }
                    case "GRANTREL": //GRANTREL (PROVID) (RELIGIONID) - grants religion to province
                        {
                            if (commandSplit.Count < 3) { return "Insufficient parameters"; }
                            if (!ValueLimiter(commandSplit[1], 0, provs.Count() - 1)) { return "Invalid ID parameters"; }
                            if (!ValueLimiter(commandSplit[2], 0, religions.Count() - 1)) { return "Invalid ID parameters"; }
                            if (Act.Actions.SetReligion(ref provs, ref religions, Convert.ToInt32(commandSplit[1]), Convert.ToInt32(commandSplit[2])))
                            { return "Set Religion"; }
                            else { return "Failed to set religion"; }
                        }
                    case "STATEREL": //STATEREL (EMPIREID) (RELIGIONID) - grants religion to an empire
                        {
                            if (commandSplit.Count < 3) { return "Insufficient parameters"; }
                            if (!ValueLimiter(commandSplit[1], 0, empires.Count() - 1)) { return "Invalid ID parameters"; }
                            if (!ValueLimiter(commandSplit[2], 0, religions.Count() - 1)) { return "Invalid ID parameters"; }
                            if (empires[Convert.ToInt32(commandSplit[1])]._exists == false) { return "The target empire is dead"; }

                            if (Act.Actions.SetStateReligion(ref provs, ref empires, ref religions, Convert.ToInt32(commandSplit[1]), Convert.ToInt32(commandSplit[2])))
                            { return "Set state Religion"; }
                            else { return "Failed to set state religion"; }
                        }
                    case "COLONY": //COLONY (PROVID) (EMPIREID)
                        {
                            if (commandSplit.Count < 3) { return "Insufficient parameters"; }
                            if (!ValueLimiter(commandSplit[1], 0, provs.Count() - 1)) { return "Invalid ID parameters"; }
                            if (!ValueLimiter(commandSplit[2], 0, empires.Count() - 1)) { return "Invalid ID parameters"; }
                            if (empires[Convert.ToInt32(commandSplit[2])]._exists == false) { return "The target empire is dead"; }
                            if (provs.Count < Convert.ToInt32(commandSplit[1]) || empires.Count < Convert.ToInt32(commandSplit[2])) { return "Unrecognised integer ID supplied"; }

                            (bool canColonize, int colonyCost) colonyApplicable = Act.Actions.CanColonize(provs[Convert.ToInt32(commandSplit[1])], empires[Convert.ToInt32(commandSplit[2])], ref provs);
                            if (colonyApplicable.canColonize)
                            {
                                if (Act.Actions.ColonizeLand(provs[Convert.ToInt32(commandSplit[1])], empires[Convert.ToInt32(commandSplit[2])], ref provs))
                                {
                                    ForceUpdate(ref loadedMap);
                                    return "Colonized new land for mil score " + colonyApplicable.colonyCost;
                                }
                                else
                                {
                                    return "Failed to Colonize";
                                }
                            }
                            else
                            {
                                if (colonyApplicable.colonyCost == 0) { return "Could not colonize"; }
                                else { return "Could not afford colony cost of " + colonyApplicable.colonyCost; }
                            }
                        }
                    case "ATTACK": //ATTACK (PROVID) (EMPIREID)
                        {
                            if (commandSplit.Count < 3) { return "Insufficient parameters"; }
                            if (!ValueLimiter(commandSplit[1], 0, provs.Count() - 1)) { return "Invalid ID parameters"; }
                            if (!ValueLimiter(commandSplit[2], 0, empires.Count() - 1)) { return "Invalid ID parameters"; }
                            if (empires[Convert.ToInt32(commandSplit[2])]._exists == false) { return "The target empire is dead"; }
                            if (provs.Count < Convert.ToInt32(commandSplit[1]) || empires.Count < Convert.ToInt32(commandSplit[2])) { return "Unrecognised integer ID supplied"; }

                            if (Act.Actions.CanConquer(provs[Convert.ToInt32(commandSplit[1])], empires[Convert.ToInt32(commandSplit[2])], ref provs))
                            {
                                if (Act.Actions.ConquerLand(provs[Convert.ToInt32(commandSplit[1])], empires[Convert.ToInt32(commandSplit[2])], ref provs))
                                {
                                    ForceUpdate(ref loadedMap);
                                    return "Successfully won battle";
                                }
                                else
                                {
                                    return "Attack Failed";
                                }
                            }
                            else { return "Could not attack"; }
                        }
                    case "ADDMOD": //ADDMOD (RECVEMPIREID) (SENDEMPIREID) (DAYS) (OPINIONMOD)
                        {
                            if (commandSplit.Count < 5) { return "Insufficient parameters"; }
                            if (!ValueLimiter(commandSplit[1], 0, empires.Count() - 1)) { return "Invalid ID parameters"; }
                            if (!ValueLimiter(commandSplit[2], 0, empires.Count() - 1)) { return "Invalid ID parameters"; }
                            if (empires[Convert.ToInt32(commandSplit[1])]._exists == false) { return "The receiver empire is dead"; }
                            if (empires[Convert.ToInt32(commandSplit[2])]._exists == false) { return "The sender empire is dead"; }
                            int days = Convert.ToInt32(commandSplit[3]);
                            int modifier = Convert.ToInt32(commandSplit[4]);

                            if (Act.Actions.AddNewModifier(empires[Convert.ToInt32(commandSplit[1])], empires[Convert.ToInt32(commandSplit[2])], days, modifier, (currentDate.day, currentDate.month, currentDate.year)))
                            {
                                return "Added new modifier";
                            }
                            else { return "Could not add modifier"; }
                        }
                    case "CHECKOP": //CHECKOP (EMPIREID) (EMPIREID)
                        {
                            if (commandSplit.Count < 3) { return "Insufficient parameters"; }
                            int empOneID = Convert.ToInt32(commandSplit[1]);
                            int empTwoID = Convert.ToInt32(commandSplit[2]);
                            if (!ValueLimiter(commandSplit[1], 0, empires.Count() - 1)) { return "Invalid ID parameters"; }
                            if (!ValueLimiter(commandSplit[2], 0, empires.Count() - 1)) { return "Invalid ID parameters"; }
                            if (empires[empOneID]._exists == false) { return "An empire is dead"; }
                            if (empires[empTwoID]._exists == false) { return "An empire is dead"; }

                            if (!empires[empOneID].opinions.Any(x=>x.targetEmpireID == empTwoID)) { return "One empire has no opinion on another"; }
                            if (!empires[empTwoID].opinions.Any(x => x.targetEmpireID == empOneID)) { return "One empire has no opinion on another"; }

                            return commandSplit[1] + "->" + commandSplit[2] + "=" + empires[empOneID].opinions.First(x => x.targetEmpireID == empTwoID).lastOpinion + " --- " +
                                commandSplit[2] + "->" + commandSplit[1] + "=" + empires[empTwoID].opinions.First(x => x.targetEmpireID == empOneID).lastOpinion;
                        }
                    default:
                        return "Invalid command";
                }

            }
            catch(Exception ex)
            {
                Debug.Log(ex);
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