using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //objects
using BiomeData;
using Empires;
using WorldProperties;
using SaveLoad;
using Calendar;
using System.Linq;
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
    public Text rulerPersona;
    public Text stateReligion;

    //Relations
    public Text positiveOpinions;
    public Text feared;
    public Text rivals;
    public Text knownEmpires;

    public float updateCounter;

    private Empire lastEmpire;
    private List<Culture> lastCults;
    private List<ProvinceObject> lastProvs;
    private List<Empire> lastEmpires;
    private bool isActive = false;

    private enum Suffix
    {
        th=0,
        st=1,
        nd=2,
        rd=3,
    }
    public void UpdateData(Empire target, ref List<Culture> cults, ref List<ProvinceObject> provs, List<Empire> empires)
    {
        updateCounter = 0;
        lastEmpire = target;
        lastCults = cults;
        lastProvs = provs;
        lastEmpires = empires;

        if(isActive != true)
        {
            exitButton.onClick.AddListener(KillSelf);
            isActive = true;
        }
        empireName.text = target._empireName;
        empireFlag.color = target._empireCol;
        curMilScore.text = "Military Power: " + target.curMil.ToString() + "/" + target.maxMil.ToString();

        if (target.leftoverMil >= 0)
        {
            projectedMilScore.text = "Projected Growth:" + target.ExpectedMilIncrease(ref provs).ToString();
        }
        else
        {
            projectedMilScore.text = "Projected Growth:" + target.ExpectedMilIncrease(ref provs).ToString() + " (Debt: " + Math.Round(Math.Abs(target.leftoverMil),2) + ")";
        }

        milTech.text = "Military Tech: " + target.milTech;
        ecoTech.text = "Economic Tech: " + target.ecoTech;
        dipTech.text = "Diplomatic Tech: " + target.dipTech;
        logTech.text = "Logistics Tech: " + target.logTech;
        culTech.text = "Culture Tech: " + target.culTech;

        culName.text = cults[target._cultureID]._name;
        culEco.text = "Economy: " + Math.Round(cults[target._cultureID]._economyScore,2).ToString() + " units";
        cut.text = "Contribution: " + (target.percentageEco*100 >= 1 ? Math.Round(target.percentageEco * 100, 2).ToString() : "<1") + "%";

        Ruler tRuler = target.curRuler;

        string lanType = cults[target._cultureID]._nameType;
        switch (lanType) //Switch naming scheme for different languages
        {
            case "Asian":
                rulerName.text = "Ruler: " + tRuler.lName + " " + tRuler.fName;
                break;
            case "Pacific":
                rulerName.text = "Ruler: " + tRuler.fName;
                break;
            default:
                rulerName.text = "Ruler: " + tRuler.fName + " " + tRuler.lName;
                break;
        }

        char[] splitBDay = tRuler.birthday.day.ToString().ToCharArray();
        int suffixID = Convert.ToInt32(splitBDay[splitBDay.Length - 1].ToString());
        string suffix = suffixID < 4  && (splitBDay.Length == 0 || (splitBDay.Length == 2 && splitBDay[0] == 2)) ? ((Suffix)suffixID).ToString() : "th";
        rulerAge.text = "Age: " + tRuler.age + " (Birthday " + ((Calendar.Calendar.Months)tRuler.birthday.month).ToString() + " " + tRuler.birthday.day + suffix + ")";
        stateReligion.text = "State Religion: " + (target.stateReligion == null ? "No Religion" : target.stateReligion._name);
        rulerPersona.text = "Personality: " + tRuler.GetRulerPersonality();

        //Opinion texts
        {
            List<Opinion> posOps = target.opinions.Where(x => x.lastOpinion > x.ReturnMaxOpinion(target,empires[x.targetEmpireID]) && !x._fear && !x._rival).ToList();
            if (posOps.Count == 0) { positiveOpinions.text = "No Friends"; }
            else
            {
                positiveOpinions.text = "Friends: " + posOps.Count;
            }
        }

        {
            List<Opinion> fearOps = target.opinions.Where(x => x._fear).ToList();
            if (fearOps.Count == 0) { feared.text = "No Feared Nations"; }
            else
            {
                feared.text = "Feared: " + fearOps.Count;
            }
        }

        {
            List<Opinion> rivalOps = target.opinions.Where(x => x._rival).ToList();
            if (rivalOps.Count == 0) { rivals.text = "No Rivals"; }
            else
            {
                rivals.text = "Rivals: " + rivalOps.Count;
            }
        }

        {
            if(target.opinions.Count == 0) { knownEmpires.text = "No Known Empires"; }
            else if(target.opinions.Count == 1) { knownEmpires.text = "1 Known Empire"; }
            else
            {
                knownEmpires.text = target.opinions.Count + " Known Empires";
            }
        }
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
            if (Input.GetKeyDown(KeyCode.Escape)) { KillViewer(); Destroy(this.gameObject); }
            else
            {
                updateCounter += Time.deltaTime;

                if (updateCounter >= 0.5f)
                {
                    UpdateData(lastEmpire, ref lastCults, ref lastProvs, lastEmpires);
                    updateCounter = 0;
                }
            }
        }
    }
    public void KillSelf()
    {
        KillViewer();
        Destroy(this.gameObject);
    }
}
