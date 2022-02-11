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
    public void DebugInfo(ProvinceObject newSelection, List<Culture> culturesSet)
    {
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
}
