using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //objects
using BiomeData;
using Empires;
using WorldProperties;
using SaveLoad;

public class EmpireViewer : MonoBehaviour
{
    public Button exitButton;

    //MainData
    public Text empireName;
    public Image empireFlag;
    public Text curMilScore;
    public Text projectedMilScore;

    //TechData
    public Text milTech;
    public Text ecoTech;
    public Text dipTech;
    public Text logTech;
    public Text culTech;

    //Economics/Culture
    public Text culName;
    public Text culEco;
    public Text cut;

    //Ruler
    public Text rulerName;
    public Text rulerAge;
    public Text stateReligion;

    public float updateCounter;

    private Empire lastEmpire;
    private List<Culture> lastCults;
    private bool isActive = false;
    public void UpdateData(ref Empire target, ref List<Culture> cults)
    {
        updateCounter = 0;
        lastEmpire = target;
        lastCults = cults;
        if(isActive != true)
        {
            exitButton.onClick.AddListener(KillSelf);
            isActive = true;
        }
        empireName.text = target._empireName;
        empireFlag.color = target._empireCol;
        curMilScore.text = "Military Power: " + target.curMil.ToString() + "/" + target.maxMil.ToString();
        projectedMilScore.text = "Projected Growth:" + target.ExpectedMilIncrease(ref cults).ToString();

        milTech.text = "Military Tech: " + target.milTech;
        ecoTech.text = "Economic Tech: " + target.ecoTech;
        dipTech.text = "Diplomatic Tech: " + target.dipTech;
        logTech.text = "Logistics Tech: " + target.logTech;
        culTech.text = "Culture Tech: " + target.culTech;

        culName.text = cults[target._cultureID]._name;
        culEco.text = "Economy: " + Math.Round(cults[target._cultureID]._economyScore,2).ToString() + " units";
        cut.text = "Contribution: " + (target.percentageEco*100 >= 1 ? Math.Round(target.percentageEco * 100, 2).ToString() : "<1") + "%";

        rulerName.text = "Ruler: No Ruler";
        rulerAge.text = "Age : N/A";
        stateReligion.text = "State Religion: " + (target.stateReligion == null ? "No Religion" : target.stateReligion._name);
    }
    private void KillViewer()
    {
        updateCounter = 0;
        isActive = false;
        lastCults = null;
        lastEmpire = null;
    }
    void Update()
    {
        if (isActive)
        {
            updateCounter += Time.deltaTime;

            if (updateCounter >= 0.5f)
            {
                UpdateData(ref lastEmpire, ref lastCults);
                updateCounter = 0;
            }
        }
    }
    public void KillSelf()
    {
        KillViewer();
        Destroy(this.gameObject);
    }
}
