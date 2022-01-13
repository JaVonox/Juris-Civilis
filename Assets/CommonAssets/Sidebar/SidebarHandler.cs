using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //objects
public class SidebarHandler : MonoBehaviour
{
    public Button expandPanel;
    public GameObject panel;
    public GameObject mapModeHandler;

    public enum mapModesEnum //correlates with position in mapmodes list
    {
        Geography = 0,
        National = 1,
    }

    //Interactables
    public mapModesEnum activeMapMode;
    public List<Button> mapModes = new List<Button>();

    //Animator
    private bool panelOut; //Stores if the panel is expanded or not
    private bool animating;

    private float movementLeft;
    private Vector3 initPos;
    void Start()
    {
        panelOut = false;
        animating = false;
        activeMapMode = (mapModesEnum)0; //set to geographic mode
    }
    void Update()
    {
        if (animating)
        {
            int dir = panelOut ? -1 : 1;
            GetMovement(dir);
        }
    }
    public void AppendListener(Action<string> updater) //gives the required updating methods when used
    {
        foreach (Button btn in mapModes)
        {
            btn.GetComponent<Button>().onClick.AddListener(delegate { ActivateMapMode(btn); updater(activeMapMode.ToString()); }); //adds mapMode events from mainscreen 
        }
        expandPanel.GetComponent<Button>().onClick.AddListener(PanelExpand);

        ActivateMapMode(mapModes[0]); //Use geography viewer to start off map procedure
    }
    void ActivateMapMode(Button sender)
    {
        activeMapMode = (mapModesEnum)(mapModes.IndexOf(sender)); //Set new mapmode

        foreach(Button mapBtn in mapModes) //Swap map images and interactability
        {
            if(mapBtn != sender) 
            {
                mapBtn.interactable = true;
                mapBtn.image.sprite = mapBtn.spriteState.highlightedSprite;
            }
            else
            {
                mapBtn.interactable = false;
                mapBtn.image.sprite = mapBtn.spriteState.pressedSprite;
            }
        }

    }

    private void GetMovement(int dir) //Moves the panel dynamically
    {
        const float expectedSeconds = 0.5f; 
        float step = (150 / expectedSeconds) * Time.deltaTime;
        
        if(step > movementLeft)
        {
            panel.transform.position = new Vector3(initPos.x + (dir * 150), initPos.y,initPos.z);

            //End of animation sequence
            animating = false;
            movementLeft = 0;
            expandPanel.GetComponent<Button>().interactable = true; //Reenables the button to switch animation

            foreach(Button btn in mapModes)
            {
                btn.GetComponent<Button>().interactable = true;
            }    
        }
        else
        {
            panel.transform.Translate(new Vector3(dir * step, 0));
            movementLeft -= step;
        }

    }
    void PanelExpand()
    {
        if (!animating) //Start of animation sequence
        {
            expandPanel.GetComponent<Button>().interactable = false;

            foreach (Button btn in mapModes)
            {
                btn.GetComponent<Button>().interactable = false;
            }

            initPos = new Vector3(panel.transform.position.x, panel.transform.position.y, panel.transform.position.z);
            movementLeft = 150;
            if (!panelOut)
            {
                animating = true;
                panelOut = !panelOut;
            }
            else
            {
                animating = true;
                panelOut = !panelOut;
            }
        }
    }

}
