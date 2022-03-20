using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WorldProperties;
using ProvViewEnums;
using System.Linq;
using Empires;

public class BasicsHandler : MonoBehaviour
{
    public Text provName;
    public Text biomeName;
    public Text geoDetailsVal;
    public Text cultureVal;
    public Text popVal;
    public Text religion;
    public Text unrest;
    public Text rebels;
    public Text ecoOutput;
    public Image empireFlag;

    private ProvinceObject newSelec;
    private List<ProvinceObject> provsSet;
    private List<Culture> cultSet;
    private List<Religion> relSet;
    private List<Empire> empSet;

    public float updateCounter;
    public void BasicsInfo(ProvinceObject newSelection, List<Culture> culturesSet, List<Religion> religionsSet, List<Empire> empires, List<ProvinceObject> provs)
    {
        newSelec = newSelection;
        cultSet = culturesSet;
        relSet = religionsSet;
        empSet = empires;
        provsSet = provs; 

        updateCounter = 0;
        provName.text = newSelection._cityName.ToString();
        popVal.text = ((PopulationEnum)(int)newSelection._population).ToString();
        biomeName.text = BiomesObject.activeBiomes[newSelection._biome]._name.ToString();
        geoDetailsVal.text = ((CoastalEnum)(Convert.ToInt32(newSelection._isCoastal))).ToString() + "/" + ((HeightEnum)((int)newSelection._elProp)).ToString() + "/" + ((TempEnum)((int)newSelection._tmpProp)).ToString() + "/" + ((RainEnum)((int)newSelection._rainProp)).ToString() + "/" + ((FloraEnum)((int)newSelection._floraProp)).ToString();
        cultureVal.text = "Culture: " + culturesSet[newSelection._cultureID]._name;
        religion.text = "Religion: " + (newSelection._localReligion == null ? "Local Beliefs" : newSelection._localReligion._name);
        unrest.text = "Unrest: " + Math.Round(newSelection._unrest, 1).ToString();

        if(newSelection._ownerEmpire == null)
        {
            empireFlag.color = Color.white;
            rebels.text = "";
            ecoOutput.text = "";

        }
        else
        {
            empireFlag.color = newSelection._ownerEmpire._empireCol;
            ecoOutput.text = "Economic Output: " + Math.Round(newSelection._ownerEmpire.ReturnIndividualEcoScore(newSelection, provs, true),2).ToString() + "u/" + Math.Round(newSelection._ownerEmpire.ReturnIndividualEcoScore(newSelection, provs, false), 2).ToString() + "u";
            Rebellion? rebelGroup = newSelection._ownerEmpire.rebels.FirstOrDefault(x => x._provinceIDs.Contains(newSelection._id));

            if(rebelGroup != null)
            {
                switch(rebelGroup._type)
                {
                    case RebelType.Culture:
                        {
                            rebels.text = culturesSet[Convert.ToInt32(rebelGroup.targetType)]._name + " Rebels";
                            break;
                        }
                    case RebelType.Religion:
                        {
                            rebels.text = religionsSet[Convert.ToInt32(rebelGroup.targetType)]._name + " Rebels";
                            break;
                        }
                    case RebelType.Revolution:
                        {
                            rebels.text = "Revolutionary Rebels";
                            break;
                        }
                    case RebelType.Separatist:
                        {
                            rebels.text = "Separatist Rebels";
                            break;
                        }
                    default:
                        {
                            rebels.text = "";
                            break;
                        }
                }
            }
            else
            {
                rebels.text = "";
            }
        }
    }

    void Update()
    {
        updateCounter += Time.deltaTime;

        if (updateCounter >= 0.5f)
        {
            BasicsInfo(newSelec, cultSet,relSet,empSet,provsSet);
            updateCounter = 0;
        }
    }
}
