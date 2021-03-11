using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Action_Hub : MonoBehaviour
{
    public bool Active { get; set; } //Does this action contain energy?

    public bool Selected { get; set; } //Has this action been selected for use?

    public Energy PossessedEnergy { get; set; }

    private SpriteRenderer spriteRenderer;

    [SerializeField] private Sprite inactiveSprite;
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite selectedSprite;

    [SerializeField] private GameObject conductorObject;
    [SerializeField] private GameObject elementObject;
    [SerializeField] private GameObject boosterObject;

    private SpriteRenderer conductorRenderer;
    private SpriteRenderer elementRenderer;
    private SpriteRenderer boosterRenderer;

    // Start is called before the first frame update
    void Start()
    {
        Active = false;
        Selected = false;
        spriteRenderer = GetComponent<SpriteRenderer>();
        conductorRenderer = conductorObject.GetComponent<SpriteRenderer>();
        elementRenderer = elementObject.GetComponent<SpriteRenderer>();
        boosterRenderer = boosterObject.GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Receive energy from a Node_Block and become an active action.
    public void ReceiveEnergy(Energy receivedEnergy)
    {
        //If this action already possessed energy, prevent new energy from overwriting.

        if (PossessedEnergy == null)
        {
            PossessedEnergy = receivedEnergy;
            Active = true;
            spriteRenderer.sprite = activeSprite;
            PossessedEnergy.GetComponent<SpriteRenderer>().enabled = false;

            DisplayActionData();
        }
        else
        {
            Destroy(receivedEnergy.gameObject);
        }
    }

    //Show data sprites based on action parameters obtained from energy.
    public void DisplayActionData()
    {
        //Create filepaths based on energy parameters.
        string conductorPath = "ConductorSprites/" + PossessedEnergy.Conductor.ToString() + "Icon";
        string elementPath = "ElementSprites/" + PossessedEnergy.Element.ToString() + "Icon";
        string boosterPath = "BoosterSprites/" + PossessedEnergy.Booster.ToString() + "Icon";

        Debug.Log(boosterPath);

        //Obtain and set the sprites from constructed filepaths.
        conductorRenderer.sprite = Resources.Load<Sprite>(conductorPath);
        elementRenderer.sprite = Resources.Load<Sprite>(elementPath);
        boosterRenderer.sprite = Resources.Load<Sprite>(boosterPath);
    }

    //Receive enemy selection from Battle_Manager and execute action with that target.
    public void TargetEnemy(Battle_Enemy targetedEnemy)
    {
        PossessedEnergy.currentTarget = targetedEnemy;
        PossessedEnergy.Execute();

        Battle_Manager.selectedAction = null;
        Deactivate();
    }

    //Select this action when clicked if active.
    private void OnMouseDown()
    {
        if (Active)
        {
            Select();
        }
    }

    //Set this action as selected for use.
    public void Select()
    {
        //Disable selection status on all actions.
        foreach (GameObject action in Action_Bar.actions)
        {
            action.GetComponent<Action_Hub>().Deselect();
        }

        //Then set this action as selected.
        Selected = true;
        Battle_Manager.selectedAction = this;
        spriteRenderer.sprite = selectedSprite;


        //Healing actions automatically execute on selection (for now you can't heal enemies)
        if (PossessedEnergy.Conductor == Energy.Conductors.Heal)
        {
            PossessedEnergy.currentTarget = Battle_Manager.player;
            PossessedEnergy.Execute();

            Battle_Manager.selectedAction = null;
            Deactivate();
        }    
    }

    public void Deselect()
    {
        Selected = false;

        if (Active)
        {
            spriteRenderer.sprite = activeSprite;
        }
        else
        {
            spriteRenderer.sprite = inactiveSprite;
        }
    }

    //Clear this action's PossessedEnergy and set all sprites to inactive states.
    public void Deactivate()
    {
        Active = false;
        Deselect();
        spriteRenderer.sprite = inactiveSprite;
        conductorRenderer.sprite = null;
        elementRenderer.sprite = null;
        boosterRenderer.sprite = null;

        Destroy(PossessedEnergy.gameObject);
        PossessedEnergy = null;
    }
}
