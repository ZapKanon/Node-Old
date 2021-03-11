using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Battle_Manager : MonoBehaviour, ISaveable
{
    [SerializeField] private GameObject pulseObject;
    [SerializeField] private GameObject energyObject;

    //The speed of a traveling pulse
    [SerializeField] public static float pulseTravelSpeed;
    //The rate at which new pulses are created
    [SerializeField] private float pulseCycleSpeed;
    //Current time relative to the pulse cycle
    [SerializeField] private float pulseCycleTimer;

    [SerializeField] public static float energyTravelSpeed;

    private Vector3 pulseStartPos;
    private Vector3 pulseEndPos;

    [SerializeField] public List<GameObject> activePulses;

    //List of enemies in the current battle. Should be loaded from somewhere eventually, not hardcoded in editor
    [SerializeField] public List<GameObject> enemies;

    public static Battle_Player player;
    public GameObject victoryText;
    public GameObject defeatText;

    //The node block currently following the cursor
    public static Node_Block liftedBlock;

    public static Action_Hub selectedAction;
    public static Battle_Enemy selectedEnemy;

    private Node_Block lastLiftedBlock;

    [SerializeField] private bool testSave;
    [SerializeField] private bool testLoad;

    [SerializeField] private GameObject grid;

    public static Save_Data gridSaveData;

    void Awake()
    {
        player = GetComponent<Battle_Player>();
    }

    // Start is called before the first frame update
    void Start()
    {
        pulseTravelSpeed = 1f;
        pulseCycleSpeed = 3.0f;
        pulseCycleTimer = 0f;
        energyTravelSpeed = 1f;
        pulseStartPos = new Vector3(-3.35f, -1.735f, 0);
        pulseEndPos = new Vector3(1.765f, -1.735f, 0);

        gridSaveData = new Save_Data();

        //Load grid once on startup
        testLoad = true;

        //Creating a pulse immediately for testing
        CreatePulse();
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePulseCycle();

        //End battle in victory if there are no enemies left, or defeat if the player's health reaches 0.
        //This currentyl results in infinite pulses being generated over time with nowhere to move.
        if (enemies.Count == 0)
        {
            pulseTravelSpeed = 0;
            foreach (GameObject enemy in enemies)
            {
                enemy.SetActive(false);
            }
            victoryText.SetActive(true);
        }
        else if (player.currentHealth <= 0)
        {
            pulseTravelSpeed = 0;
            foreach (GameObject enemy in enemies)
            {
                enemy.SetActive(false);
            }
            defeatText.SetActive(true);
        }

        //If an enemy is selected after an action, send the action to that enemy.
        //Otherwise, clear the selected enemy.
        if (selectedEnemy != null)
        {
            if (selectedAction != null)
            {
                Debug.Log("Enemy thas been targeted and attacked.");
                selectedAction.TargetEnemy(selectedEnemy);
            }
            else
            {
                selectedEnemy = null;
            }
        }

        //Right mouse button functions.
        if (Input.GetMouseButtonDown(1))
        {
            //Rotate the lifted block.
            if (liftedBlock != null)
            {
                if (liftedBlock.HasEnergy == false)
                {
                    liftedBlock.RotateClockwise();
                }
            }

            //Deselect any selected action.
            if (selectedAction != null)
            {
                selectedAction.Deselect();
                selectedAction = null;
            }
            
        }

        //Update the node grid whenever a block is placed or removed.
        if (lastLiftedBlock != liftedBlock)
        {
            grid.GetComponent<Grid>().PopulateNodeGrid();
        }

        if (testSave == true)
        {
            grid.GetComponent<Grid>().PopulateSaveData(gridSaveData);
            SaveJsonData(this);
            testSave = false;
        }

        if (testLoad == true)
        {
            LoadJsonData(this);
            testLoad = false;
        }

        lastLiftedBlock = liftedBlock;
    }

    //If a cycle has passed, create a new pulse object and reset cycleTimer to 0.
    public void UpdatePulseCycle()
    {
        pulseCycleTimer += Time.deltaTime;

        if (pulseCycleTimer >= pulseCycleSpeed)
        {
            CreatePulse();
            pulseCycleTimer -= pulseCycleSpeed;
        }
    }

    //Instantiate a new pulse with proper starting position and speed.
    public void CreatePulse()
    {
        GameObject newPulseObject = Instantiate(pulseObject, pulseStartPos, Quaternion.identity);
        Pulse newPulse = newPulseObject.GetComponent<Pulse>();
        newPulse.EndXPos = pulseEndPos.x;
        newPulse.activePulses = activePulses;
        newPulse.battle_manager = this;

        //Add the newly created pulse to activePulses.
        activePulses.Add(newPulseObject);
    }

    //Compare each pulse with the block positioned above it (in the bottom row of the grid)
    //If the above block is a source node, create energy at that block.
    public void CreateEnergy(Pulse pulse)
    {
        GameObject[,] currentNodeGrid = Grid.nodeGrid;

        int pulseColumn = pulse.CurrentGridColumn;

        if (currentNodeGrid[pulseColumn, 4] != null)
        {
            Node_Block currentBlock = currentNodeGrid[pulseColumn, 4].GetComponent<Node_Block>();
            //Proceed if the block is a source node and doesn't already contain energy
            if (currentBlock.IsSource && currentBlock.HasEnergy == false)
            {
                //TODO: Instantiate energy prefab, set position to correct block and set that block's hasEnergy and possessedEnergy
                GameObject newEnergyObject = Instantiate(energyObject, currentBlock.transform.position, Quaternion.identity);
                newEnergyObject.transform.position += new Vector3(0, -0.09f, 0);

                Energy newEnergy = newEnergyObject.GetComponent<Energy>();
                newEnergy.GridPosition = new Vector2(pulseColumn, 4);
                currentBlock.PossessedEnergy = newEnergy;
                currentBlock.HasEnergy = true;

                Debug.Log("Creating energy in column " + pulseColumn);
            }
        }           
        
    }


    //Remove a dead enemy from the enemies list
    public void DeadEnemy(Battle_Enemy enemy)
    {
        enemies.Remove(enemy.gameObject);
    }

    private static void SaveJsonData(Battle_Manager a_Battle_Manager)
    {
        Save_Data sd = gridSaveData;
        //a_Battle_Manager.PopulateSaveData(sd);

        if (File_Manager.WriteToFile("SaveData.dat", sd.ToJson()))
        {
            Debug.Log("Save successful!");
            Debug.Log(Application.persistentDataPath);
        }
    }

    public void PopulateSaveData(Save_Data a_SaveData)
    {
        //a_SaveData.m_Score = CurrentScore;
    }

    private static void LoadJsonData(Battle_Manager a_Battle_Manager)
    {
        if (File_Manager.LoadFromFile("SaveData.dat", out var json))
        {
            Save_Data sd = new Save_Data();
            sd.LoadFromJson(json);

            a_Battle_Manager.LoadFromSaveData(sd);
        }
    }

    public void LoadFromSaveData(Save_Data a_SaveData)
    {
        grid.GetComponent<Grid>().LoadFromSaveData(a_SaveData);
    }
}
