using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WorldProperties;
using ProvViewEnums;
using Empires;
public class DebugHandler : MonoBehaviour
{
    public Text provinceID;
    public Text cultureID;
    public Text ownerEmpireID;
    public Text religionID;
    public Text techPoints;

    private ProvinceObject newSelec;
    private List<Culture> cultSet;
    private List<Empire> empSet;
    public float updateCounter;
    public void DebugInfo(ProvinceObject newSelection, List<Culture> culturesSet, List<Empire> emps)
    {
        newSelec = newSelection;
        cultSet = culturesSet;
        empSet = emps;
        updateCounter = 0;

        provinceID.text = "ID: " + newSelection._id.ToString();
        cultureID.text = "Cult ID: " + newSelection._cultureID.ToString();

        if(newSelection._ownerEmpire != null)
        {
            ownerEmpireID.text = "OwnerID : " + newSelection._ownerEmpire._id.ToString();
            techPoints.text = "Tech: " + emps[newSelection._ownerEmpire._id].techPoints + "/250";
        }
        else
        {
            ownerEmpireID.text = "Owner : No Owner";
            techPoints.text = "";
        }

        if (newSelection._localReligion != null)
        {
            religionID.text = "Religion : " + newSelection._localReligion._id.ToString();
        }
        else
        {
            religionID.text = "Religion : No Faith";
        }
    }

    void Update()
    {
        updateCounter += Time.deltaTime;

        if (updateCounter >= 0.5f)
        {
            DebugInfo(newSelec, cultSet,empSet);
            updateCounter = 0;
        }
    }
}
