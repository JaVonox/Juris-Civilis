using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WorldProperties;
using ProvViewEnums;

public class DebugHandler : MonoBehaviour
{
    public Text provinceID;
    public Text cultureID;
    public Text ownerEmpireID;
    public Text religionID;

    private ProvinceObject newSelec;
    private List<Culture> cultSet;
    public float updateCounter;
    public void DebugInfo(ProvinceObject newSelection, List<Culture> culturesSet)
    {
        newSelec = newSelection;
        cultSet = culturesSet;
        updateCounter = 0;

        provinceID.text = "ID: " + newSelection._id.ToString();
        cultureID.text = "Cult ID: " + newSelection._cultureID.ToString();

        if(newSelection._ownerEmpire != null)
        {
            ownerEmpireID.text = "OwnerID : " + newSelection._ownerEmpire._id.ToString();
        }
        else
        {
            ownerEmpireID.text = "Owner : No Owner";
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
            DebugInfo(newSelec, cultSet);
            updateCounter = 0;
        }
    }
}
