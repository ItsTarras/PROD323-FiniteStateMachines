using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using FSM;
using System.Collections;

// This script uses the StateMachine script by LavaAfterBurner to implement
// state machines. You can find this under Scripts > HFSM.
public class HarvesterAgentControl : MonoBehaviour
{
    [SerializeField] GameObject dropOffPoint;
    [SerializeField] GameObject repairPoint;
    [SerializeField] GameObject allCrystals;
    [SerializeField] int miningSpeed = 20;
    [SerializeField] ParticleSystem beam;
    [SerializeField] public Slider healthbar;
    [SerializeField] float maxHealth = 20;
    [SerializeField] Slider resourcebar;
    [SerializeField] int maxResource;
    [SerializeField] Text resourceText;
    [SerializeField] Text alertText;
    [SerializeField] ParticleSystem sandstorm;
    public bool redAlert = false;

    private GameObject currentCrystal; // Stores a reference to the target crystal to harvest
    private float elapsedTime = 0;
    private NavMeshAgent agent;
    private StateMachine harvesting_fsm;
    private StateMachine repairing_fsm;
    private StateMachine sheltering_fsm;
    private int resourceLevel = 0;
    private int stationResourceLevel = 0;
    private bool enRouteResource = false;
    private bool enRouteStation = false;
    private bool enRouteRepair = false;
    private string prevState;
    private float repairTimer = 0;
    private float prepTimer = 0;
    private float sandstormTimer = 0;

    void Start()
    {
        // Set default values
        agent = gameObject.GetComponent<NavMeshAgent>();
        healthbar.maxValue = maxHealth;
        healthbar.value = maxHealth;
        resourcebar.maxValue = maxResource;
        resourcebar.value = 0;

        #region RESOURCE HARVESTING STATE MACHINE
        // ----- A. Create a new state machine. -----

        // This is the innermost state machine as indicated in the image in the lab material.
        // This includes 3 states: Searching for resource, Harvesting / Gathering resource and Dropping off resource.
        harvesting_fsm = new StateMachine(this, needsExitTime: false);

        // ----- B. Add states to the state machine -----

        // Add Searching state
        harvesting_fsm.AddState("Searching", new State(onLogic: (state) => SearchingForResource()));
        // Add Gathering/harvesting state
        harvesting_fsm.AddState("Gathering", new State(onLogic: (state) => GatheringResource()));
        // Add Dropping off state
        harvesting_fsm.AddState("DroppingOff", new State(onLogic: (state) => DroppingOffResource()));

        // ----- C. Define transitions between states -----

        // Transition from Searching state to Gathering state.
        // Transition happens when a crystal has been identified.
        harvesting_fsm.AddTransition(new Transition(
            "Searching", // from state
            "Gathering", // to state
            (transition) => currentCrystal != null && resourceLevel < maxResource // condition that has to be met before transition happens
        ));

        harvesting_fsm.AddTransition(new Transition(
            "Searching",
            "DroppingOff",
            (transition) => currentCrystal == null && resourceLevel > 0));

        // TODO: Add transition from Gathering state to Searching state.
        // Transition happens when harvester finished gathering and harvester still has capacity to harvest resource.
        harvesting_fsm.AddTransition(new Transition(
            "Gathering", //from state
            "Searching",
            (transition) => currentCrystal == null && resourceLevel < maxResource
            ));


        // TODO: Add transition from Gathering state to DroppingOff state.
        // Transition happens when harvester finished gathering and harvester's capacity is full.
        harvesting_fsm.AddTransition(new Transition(
            "Gathering",
            "DroppingOff",
            (transition) => resourceLevel >= maxResource && currentCrystal == null
            ));

        // TODO: Add transition from DroppingOff state to Searching.
        // Transition happens when harvester unloaded all resource. Harvester goes back to find next resource.
        harvesting_fsm.AddTransition(new Transition(
            "DroppingOff", // from
            "Searching", // to
            (transition) => resourceLevel <= 0));

        #endregion

        #region HARVESTER REPAIRING STATE MACHINE
        // ----- A. Create a new state machine -----
        // This state machine have 2 states: Harvester repairing and the Resource Harvesting state machine.
        repairing_fsm = new StateMachine(this);

        // ----- B. Add states to the state machine -----

        // Repairing state machine has a higher priority than the Resource Harvesting state machine.
        // Therefore, add harvesting state machine as a state of repairing state machine.
        repairing_fsm.AddState("Harvesting", harvesting_fsm);
        // Add Repairing state
        repairing_fsm.AddState("Repairing", new State(onLogic: (state) => {RepairingHarvester();}, needsExitTime: false));

        // ----- C. Define transitions between states -----

        // TODO: Add transition from Harvesting state to Repairing state.
        // Transition happens when harvester's health is less than 30% of its max health
        repairing_fsm.AddTransition(new Transition(
            "Harvesting",
            "Repairing",
            (transition) => healthbar.value < ((maxHealth / 100) * 30)));

        // TODO: Add transition from Repairing state to Harvesting state.
        // Transition happens when harvester's health has been completely refilled.
        repairing_fsm.AddTransition(new Transition(
            "Repairing",
            "Harvesting",
            (transition) => healthbar.value >= maxHealth - 5));
        #endregion

        #region RED ALERT STATE MACHINE
        // ----- A. Create a new state machine -----
        // This state machine have 2 states: Sheltering state and the Harvester Repairing state machine.
        sheltering_fsm = new StateMachine(this);

        // ----- B. Add states to the state machine -----

        // Red Alert state machine has a higher priority than the Harvester Repairing state machine.
        // Therefore, add Harvester Repairing state machine as a state of Red Alert state machine.
        sheltering_fsm.AddState("NeedRepair", repairing_fsm);
        // Add Sheltering state
        sheltering_fsm.AddState("Sheltering", new State(onLogic: (state) => {ShelteringHarvester();},  needsExitTime: false));

        // ----- C. Define transitions between states -----

        // TODO: Add transition from NeedRepair state to Sheltering state.
        // Transition happens when there is a red alert
        sheltering_fsm.AddTransition(new Transition(
            "NeedRepair",
            "Sheltering",
            (transition) => redAlert
        ));

        // TODO: Add transition from Sheltering state to NeedRepair state.
        // Transition happens when there is no red alert
        sheltering_fsm.AddTransition(new Transition(
            "Sheltering",
            "NeedRepair",
            (transition) => !redAlert
            ));

        #endregion

        #region STATE MACHINE INITIALIZATION
        // Set entry points (states) of each state machine
        harvesting_fsm.SetStartState("Searching");
        repairing_fsm.SetStartState("Harvesting");
        sheltering_fsm.SetStartState("NeedRepair");

        // HFSM starts at Red Alert state machine since it has the highest priority
        sheltering_fsm.OnEnter();

        #endregion
    }

    void Update()
    {
        // Start logic from Red Alert state machine
        sheltering_fsm.OnLogic();
        ReduceHealthOverTime();
        //Debug.Log("Current Harvesting State:" + harvesting_fsm.activeState.name);
        //if (currentCrystal)
        
        /*
        {
            Debug.Log("Current Crystal: True");
        }
        */
        //Debug.Log("Current Repairing State:" + repairing_fsm.activeState.name);

    }

    private void ReduceHealthOverTime()
    {
        healthbar.value -= Time.deltaTime;
    }

    // Check if the harvester has reached the resource
    private bool CheckResourceInRange()
    {
        return Vector3.Distance(currentCrystal.transform.position, transform.position) <= agent.stoppingDistance;
    }

    // Check if the harvester has reached the station
    public bool CheckStationIsReached()
    {
        return Vector3.Distance(dropOffPoint.transform.position, transform.position) <= agent.stoppingDistance;
    }

    // Check if the harvester has reached the repairing station
    private bool CheckRepairerIsReached()
    {
        return Vector3.Distance(repairPoint.transform.position, transform.position) <= agent.stoppingDistance;
    }

    // Searching for the next resource and send the harvester on its way
    private void SearchingForResource()
    {
        beam.Stop();
        currentCrystal = GetNearestCrystal();
    }

    // Search for the nearest resource to the harvester
    GameObject GetNearestCrystal()
    {
        GameObject nearestCrystal = null;
        float nearest = 9999f;
        foreach (Transform child in allCrystals.transform)
        {
            float dist = Vector3.Distance(child.position, transform.position);
            if (dist < nearest)
            {
                nearest = dist;
                nearestCrystal = child.gameObject;
            }
        }

        return nearestCrystal;
    }

    #region STATE IMPLEMENTATIONS
    // This is the Gathering state implementation.
    private void GatheringResource()
    {
        // Check if harvester is already enroute to the target crystal, if not set the harvester's destination to the target crystal
        if (!enRouteResource)
        {
            agent.SetDestination(currentCrystal.transform.position);
            enRouteResource = true;
            enRouteRepair = false;
            enRouteStation = false;
        }

        // Check if there is a crystal nearby, if so mine the crystal
        if (CheckResourceInRange())
        {
            if (elapsedTime >= 1f) // 1 second delay before mining
            {
                currentCrystal.GetComponent<CrystalMine>().MineCrystal(miningSpeed);
                resourcebar.value += miningSpeed; // Fill up resource bar depending on mining speed
                resourceLevel += miningSpeed;
                resourceText.text = "Resource Level: " + resourcebar.value;
                Debug.Log("Resources" + resourceLevel + "/" + maxResource);
                elapsedTime = 0;
                enRouteResource = false;
                beam.Play(); // Shining particle system
            }
            else
            {
                elapsedTime += Time.deltaTime;
            }         
        }

    }

    // This is the DroppingOff state implementation.
    private void DroppingOffResource()
    {
        // TODO: Check if harvester is enroute to the station, if not set the harvester's destination to the station
        if (!enRouteStation)
        {
            agent.SetDestination(dropOffPoint.transform.position);
            enRouteStation = true;
            enRouteResource = false;
            enRouteRepair = false;
        }

        // TODO: Check if harvester arrived at the station, if so unload the crystals
        if (CheckStationIsReached())
        {
            if (resourceLevel > 0)
            {
                if (elapsedTime >= 1f) // 1 second delay before mining
                {
                    resourceLevel -= miningSpeed;
                    stationResourceLevel += miningSpeed;
                    resourcebar.value -= miningSpeed;
                    Debug.Log("Resources:" + resourceLevel + "/" + maxResource);
                    elapsedTime = 0;
                    enRouteStation = false;
                    beam.Play(); // Shining particle system
                }
                else
                {
                    elapsedTime += Time.deltaTime;
                }
            }
        }
    }

    // This is the Repairing state implementation
    private void RepairingHarvester()
    {
        // TODO: Check if harvester is enroute to the repair station, if not set the harvester's destination to the repair station
        if (!enRouteRepair)
        {
            agent.SetDestination(repairPoint.transform.position);
            enRouteRepair = true;
            enRouteStation = false;
            enRouteResource = false;
        }

        // TODO: Check if harvester arrived at the repair station, if so repair the harvester.
        // Repairing the harvester should take at least 3 seconds before it is back to full health.
        if (CheckRepairerIsReached())
        {
            if (healthbar.value < maxHealth)
            {
                if (elapsedTime >= 1f) // 1 second delay before mining
                {
                    healthbar.value += maxHealth/5;
                    elapsedTime = 0;
                    //We probably full, bol.
                    enRouteRepair = false;
                    beam.Play(); // Shining particle system
                }
                else
                {
                    elapsedTime += Time.deltaTime;
                }
            }
        }  
    }

    // This is the Sheltering state implementation
    private void ShelteringHarvester()
    {
        enRouteRepair = false;
        enRouteResource = false;
        enRouteStation = false;
        agent.SetDestination(dropOffPoint.transform.position);
        beam.Stop();

        repairing_fsm.SetStartState("Harvesting");
    }

    #endregion
}
