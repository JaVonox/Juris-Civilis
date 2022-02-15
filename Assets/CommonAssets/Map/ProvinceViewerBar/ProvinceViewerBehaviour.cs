using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BiomeData;
using WorldProperties;
using ProvViewEnums;

public class ProvinceViewerBehaviour : MonoBehaviour
{

    public GameObject container;
    public List<Button> infoModeBtns = new List<Button>();
    public InfoMode activeInfoMode;

    private GameObject activeInfoScreen;
    public GameObject basicsPrefab;
    public GameObject debugPrefab;
    public GameObject nationalPrefab;
    public enum InfoMode
    {
        Basic = 0,
        Debug = 1,
        Close = 2,
        National = 3,
    }

    private Dictionary<int, Action<ProvinceObject>> actionRef = new Dictionary<int, Action<ProvinceObject>>();

    private ProvinceObject lastSelection;
    private List<Culture> culturesSet;
    private List<ProvinceObject> provsSet;
    public void Start()
    {
        container.SetActive(false);
        InitDictionary();
        AppendInfoModes();
    }

    private void InitDictionary()
    {
        actionRef.Add(0, LoadBasics);
        actionRef.Add(1, LoadDebug);
        actionRef.Add(2, CloseTab);
        actionRef.Add(3, LoadNational);
    }
    public void AppendInfoModes()
    {
        foreach (Button btn in infoModeBtns)
        {
            btn.GetComponent<Button>().onClick.AddListener(delegate { SwitchInfoMode(btn); InteriorUpdate(); }); //add info switching events
        }

        activeInfoMode = (InfoMode)(0);
        SwitchInfoMode(infoModeBtns[0]); //Start by using terrain viewer mode
    }

    void SwitchInfoMode(Button sender)
    {
        activeInfoMode = (InfoMode)(infoModeBtns.IndexOf(sender)); //Set new mapmode

        foreach (Button modeBtn in infoModeBtns) //Swap map images and interactability
        {
            if (modeBtn != sender)
            {
                modeBtn.interactable = true;
                modeBtn.image.sprite = modeBtn.spriteState.highlightedSprite;
            }
            else
            {
                modeBtn.interactable = false;
                modeBtn.image.sprite = modeBtn.spriteState.pressedSprite;
            }
        }

    }
    public void LoadBasics(ProvinceObject newSelection)
    {
        if (activeInfoScreen != null) { Destroy(activeInfoScreen.gameObject); }
        activeInfoScreen = null;
        activeInfoScreen = Instantiate(basicsPrefab, container.transform, false);
        activeInfoScreen.GetComponent<BasicsHandler>().BasicsInfo(newSelection, culturesSet);
        container.SetActive(true);
    }
    public void LoadDebug(ProvinceObject newSelection)
    {
        if (activeInfoScreen != null) { Destroy(activeInfoScreen.gameObject); }
        activeInfoScreen = null;
        activeInfoScreen = Instantiate(debugPrefab, container.transform, false);
        activeInfoScreen.GetComponent<DebugHandler>().DebugInfo(newSelection, culturesSet);
        container.SetActive(true);
    }
    public void LoadNational(ProvinceObject newSelection)
    {
        if (activeInfoScreen != null) { Destroy(activeInfoScreen.gameObject); }
        activeInfoScreen = null;
        activeInfoScreen = Instantiate(nationalPrefab, container.transform, false);
        activeInfoScreen.GetComponent<NationalHandler>().NationalInfo(ref newSelection, ref culturesSet, ref provsSet);
        container.SetActive(true);
    }
    private void InteriorUpdate() //Updates from script
    {
        actionRef[(int)activeInfoMode](lastSelection);
    }
    public void DisplayProvince(ProvinceObject newSelection, ref List<Culture> cultures, ref List<ProvinceObject> provs) //Updates the selection based on the provincial data provided
    {
        lastSelection = newSelection;
        if(culturesSet == null) { culturesSet = cultures; }
        if(provsSet == null) { provsSet = provs; }

        actionRef[(int)activeInfoMode](newSelection);
    }

    void CloseTab(ProvinceObject newSelection)
    {
        //Close container and set info mode to default
        activeInfoMode = (InfoMode)(0);
        SwitchInfoMode(infoModeBtns[0]); //Sets graphic to default info mode
        container.SetActive(false);
    }
}
