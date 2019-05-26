//This script manages the timing and flow of the game. It is also responsible for telling
//the UI when and how to update

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Cinemachine;

//seperate these
public enum AIType { Advanced, Middle, Back, Close, Not }

public class Ship
{
    private int aiNumber;
    private string aiName; // for display purposes
    private AIType aiType;
    private int currentLap;
    private int currentWaypoint;
    private Vector3 currentPosition;
    private float counter;
    private PlayerInput aiControl;
    private bool lapReady = false;

    public Ship(int aiNum, string name, AIType type, int currLap, int currWaypoint, Vector3 currPos)
    {
        aiNumber = aiNum;
        aiName = name;
        aiType = type;
        currentLap = currLap;
        currentWaypoint = currWaypoint;
        currentPosition = currPos;
    }

    public Ship(int aiNum, string name, AIType type, int currLap, int currWaypoint, Vector3 currPos, GameObject shipObject)
    {
        aiNumber = aiNum;
        aiName = name;
        aiType = type;
        currentLap = currLap;
        currentWaypoint = currWaypoint;
        currentPosition = currPos;
        aiControl = shipObject.GetComponent<PlayerInput>();
    }

    // Getters
    public int GetAINumber()
    {
        return aiNumber;
    }

    public string GetAIName()
    {
        return aiName;
    }

    public AIType GetAIType()
    {
        return aiType;
    }

    public int GetCurrLap()
    {
        return currentLap;
    }

    public int GetCurrWaypoint()
    {
        return currentWaypoint;
    }

    public Vector3 GetCurrPos()
    {
        return currentPosition;
    }

    public float GetCounter()
    {
        return counter;
    }

    public bool LapReady()
    {
        return lapReady;
    }

    // Setters
    public void SetCurrPos(Vector3 currPos)
    {
        currentPosition = currPos;
    }

    public void SetCounter(float counterCalc)
    {
        counter = counterCalc;
    }

    public void IncrementCurrLap()
    {
        currentLap++;
    }

    public void SetCurrWaypoint(int currWaypoint)
    {
        currentWaypoint = currWaypoint;
    }

    public void SetAIType(AIType type)
    {
        aiType = type;
    }

    public void SetShipObject(GameObject shipObject)
    {
        aiControl = shipObject.GetComponent<PlayerInput>();
    }

    public void SetAIBestSkill()
    {
        aiControl.BestSkill();
    }

    public void SetAIMidSkill()
    {
        aiControl.MidSkill();
    }

    public void SetAIWorstSkill()
    {
        aiControl.WorstSkill();
    }

    public void SetLapReady(bool value)
    {
        lapReady = value;
    }
}

public class GameManager : MonoBehaviour
{
	//The game manager holds a public static reference to itself. This is often referred to
	//as being a "singleton" and allows it to be access from all other objects in the scene.
	//This should be used carefully and is generally reserved for "manager" type objects
	public static GameManager instance;

    const int NUM_AI_TYPES = 4;
    const float WAYPOINT_MULTIPLIER = 100.0f;
    const float LAP_MULTIPLIER = 1000.0f;
    const float AI_CUTOFF_PERCENT = 0.8f;
    const float DYNAMIC_DIFF_BEGIN = 5.0f;
    const float ADV_AI_TARGET_OFFSET = 400.0f;
    const float MID_AI_TAREGT_OFFSET = 200.0f;
    const float BACK_AI_TARGET_OFFSET = -400.0f;
    const float AI_GROUP_RANGE = 100.0f;
    const int CAM_HIGH_PRIORITY = 10;
    const int CAM_LOW_PRIORITY = 5;

	[Header("Race Settings")]
	public int numberOfLaps = 3;			//The number of laps to complete MAGIC OR SET BY PLAYER BEFORE RACE?
	public VehicleMovement vehicleMovement;	//A reference to the ship's VehicleMovement script

	[Header("UI References")]
	public ShipUI shipUI;					//A reference to the ship's ShipUI script
	public LapTimeUI lapTimeUI;				//A reference to the LapTimeUI script in the scene
	public GameObject gameOverUI;			//A reference to the UI objects that appears when the game is complete
    public GameObject pausedUI;
    public static bool gameIsPaused = false;

	float[] lapTimes;						//An array containing the player's lap times
	bool isGameOver = false;						//A flag to determine if the game is over
	bool raceHasBegun = false;                      //A flag to determine if the race has begun
    [SerializeField] GameObject playerShipObj;
    Ship playerShip = new Ship(999, "Player", AIType.Not, 0, 0, Vector3.zero); // to store player ship details
    
    // AI
    [SerializeField] GameObject[] aiVehicles; // needed for correct count and reference to the car's attributes i.e. the gameobjects themselves
    Ship[] aiShips; // race data about the ai
    public AIThoughtsUI aiThoughtsUI;

    // Race management
    [SerializeField] GameObject waypointsObj; // to find the exact position of a waypoint (the parent object of the waypoints)
    Vector3[] waypoints;
    List<Ship> racePositions; // ordered list of car rankings in race
    public PlayerPositionUI playerPosUI;
    public RankingUI rankingUI;
    float timer = 0.0f; // to count to when ai should start reacting at start of race (after a few seconds)
    float oneLapCountVal; // the counter value of one lap (used as bonus addition to rectify counter bug and to calculate entireRaceCountVal)
    float entireRaceCountVal; // the counter value of the entire race - not sure we actually need it as it's own variable as it's not used anywhere else
    float aiCutOffCountVal; // counter value where once ai has passed it will reduce skill to give player chance (80% of race)

    // Camera management
    [SerializeField] GameObject followCameraObj; // for viewing ship from behind
    [SerializeField] GameObject hoverCameraObj; // for viewing ship from above
    CinemachineVirtualCamera followCam;
    CinemachineVirtualCamera hoverCam;
    KeyCode toggleHumanButton = KeyCode.H;


	void Awake()
	{
		//If the variable instance has not be initialized, set it equal to this
		//GameManager script...
		if (instance == null)
			instance = this;
		//...Otherwise, if there already is a GameManager and it isn't this, destroy this
		//(there can only be one GameManager)
		else if (instance != this)
			Destroy(gameObject);
	}

	void OnEnable()
	{
		//When the GameManager is enabled, we start a coroutine to handle the setup of
		//the game. It is done this way to allow our intro cutscene to work. By slightly
		//delaying the start of the race, we give the cutscene time to take control and 
		//play out
		StartCoroutine(Init());
	}

	IEnumerator Init()
	{
		//Update the lap number on the ship
		UpdateUI_LapNumber();

		//Wait a little while to let everything initialize
		yield return new WaitForSeconds(.1f);

		//Initialize the lapTimes array and set that the race has begun
		lapTimes = new float[numberOfLaps];
		//raceHasBegun = true;

        //Initialise Player
        playerShip.SetCurrPos(playerShipObj.transform.position);

        // Set now just in case player wants to watch AI
        playerShip.SetShipObject(playerShipObj);
        playerShip.SetAIMidSkill();

        //Initialise AI
        //Vector3[] positions = new Vector3[8]; // MAGIC - although technically a const array of offsets?
        //positions[0] = new Vector3(0.0f, 0.0f, 10.0f);
        //positions[1] = new Vector3(0.0f, 0.0f, 5.0f);
        //// player car here (0.0f, 0.0f, 0.0f)
        //positions[2] = new Vector3(0.0f, 0.0f, -5.0f);
        //positions[3] = new Vector3(-7.5f, 0.0f, 12.5f);
        //positions[4] = new Vector3(-7.5f, 0.0f, 7.5f);
        //positions[5] = new Vector3(-7.5f, 0.0f, 2.5f);
        //positions[6] = new Vector3(-7.5f, 0.0f, -2.5f);
        //positions[7] = new Vector3(-7.5f, 0.0f, -7.5f);
        // MAGIC
        string[] aiNames = new string[8] { "Mario", "Luigi", "Peach", "Toad", "Yoshi", "Bowser", "Daisy", "Wario" };

        aiShips = new Ship[aiVehicles.Length];
        for (int i = 0; i < aiShips.Length; i++)
        {
            //aiVehicles[i].transform.rotation = playerShipObj.transform.rotation;
            //aiVehicles[i].transform.position = playerShipObj.transform.position + positions[i];
            aiShips[i] = new Ship(i, aiNames[i], (AIType)(i % NUM_AI_TYPES), 0, 0, aiVehicles[i].transform.position, aiVehicles[i]);
            aiShips[i].SetAIBestSkill(); // check if race has been going for number of seconds before activating dynamic difficulty
        }
        for (int i = 0; i < aiShips.Length; i++)
        {
            Debug.Log(aiShips[i].GetAINumber() + " has type " + aiShips[i].GetAIType().ToString());
        }

        //Initialise Race Ranking
        racePositions = new List<Ship>(aiVehicles.Length + 1); // + 1 for player car

        //Initialise Waypoints
        waypoints = new Vector3[waypointsObj.transform.childCount];
        for (int i = 0; i < waypoints.Length; i++)
        {
            waypoints[i] = waypointsObj.transform.GetChild(i).transform.position;
        }

        //Calculate race counter values
        oneLapCountVal = waypoints.Length * WAYPOINT_MULTIPLIER;
        entireRaceCountVal = (oneLapCountVal * numberOfLaps) + ((numberOfLaps - 1) * LAP_MULTIPLIER); // - 1 because 1st lap does not require bonus
        //Debug.Log(entireRaceCountVal);
        aiCutOffCountVal = entireRaceCountVal * AI_CUTOFF_PERCENT;

        //Initialise Cameras
        followCam = followCameraObj.GetComponent<CinemachineVirtualCamera>();
        hoverCam = hoverCameraObj.GetComponent<CinemachineVirtualCamera>();

	}

	void Update()
	{
        //Update the speed on the ship UI
        UpdateUI_Speed ();

		//If we have an active game...
		if (IsActiveGame())
		{
            if (rankingUI != null)
            {
                rankingUI.EnableRankingList();
            }

            UpdateRacePositions(); // MAKE A NICE LIST ON SCREEN FOR THIS
            
            //for (int i = 0; i < racePositions.Capacity; i++)
            //{
            //    if (racePositions[i].GetAINumber() == 999)
            //    {
            //        Debug.Log(racePositions[i].GetCounter());
            //    }
            //}

            timer += Time.deltaTime;
            if (timer > DYNAMIC_DIFF_BEGIN)
            {
                if (aiThoughtsUI != null)
                {
                    aiThoughtsUI.EnableAIThoughtsList();
                }

                // Dynamic Difficulty Adjust
                float playerShipCounter = playerShip.GetCounter(); // so that we know where the player ship is
                for (int i = 0; i < aiShips.Length; i++)
                {
                    float aiShipCounter = aiShips[i].GetCounter(); // so that we know where the ai ship is

                    // based on what ai type it is (and what position it is in relative to player) update the ai skill
                    if (aiShips[i].GetAIType() == AIType.Advanced)
                    {
                        UpdateSkillRequirements(playerShipCounter, ADV_AI_TARGET_OFFSET, aiShipCounter, ref aiShips[i]);
                    }
                    else if (aiShips[i].GetAIType() == AIType.Middle)
                    {
                        UpdateSkillRequirements(playerShipCounter, MID_AI_TAREGT_OFFSET, aiShipCounter, ref aiShips[i]);
                    }
                    else if (aiShips[i].GetAIType() == AIType.Back)
                    {
                        UpdateSkillRequirements(playerShipCounter, BACK_AI_TARGET_OFFSET, aiShipCounter, ref aiShips[i]);
                    }
                    else if (aiShips[i].GetAIType() == AIType.Close)
                    {
                        UpdateSkillRequirements(playerShipCounter, 0.0f, aiShipCounter, ref aiShips[i]);
                    }
                }
            }

            //...calculate the time for the lap and update the UI
            lapTimes[playerShip.GetCurrLap()] += Time.deltaTime;
			UpdateUI_LapTime();

            // Toggle Human or AI Driver
            if (Input.GetKeyDown(toggleHumanButton))
            {
                playerShipObj.GetComponent<PlayerInput>().ToggleIsHuman();

                if (playerShipObj.GetComponent<PlayerInput>().isHuman)
                {
                    followCam.Priority = CAM_HIGH_PRIORITY;
                    hoverCam.Priority = CAM_LOW_PRIORITY;
                }
                else
                {
                    followCam.Priority = CAM_LOW_PRIORITY;
                    hoverCam.Priority = CAM_HIGH_PRIORITY;
                }
            }

            // Human/AI Select Skill Level
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                playerShip.SetAIBestSkill();
                Debug.Log("Human/AI has best skill");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                playerShip.SetAIMidSkill();
                Debug.Log("Human/AI has mid skill");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                playerShip.SetAIWorstSkill();
                Debug.Log("Human/AI has worst skill");
            }

            // Pause and Resume()
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (gameIsPaused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }
	}

    public float GetFractionOfPathCovered(Vector3 position, Vector3 currWaypoint, Vector3 nextWaypoint)
    {
        Vector3 distFromCurrWaypoint = position - currWaypoint;
        Vector3 currSegmentVector = nextWaypoint - currWaypoint;
        float fraction = Vector3.Dot(distFromCurrWaypoint, currSegmentVector) / currSegmentVector.sqrMagnitude;

        return fraction;
    }

    public void UpdateRacePositions()
    {
        racePositions.Clear();
        playerShip.SetCurrPos(playerShipObj.transform.position);
        racePositions.Add(playerShip);
        for (int i = 0; i < aiShips.Length; i++)
        {
            aiShips[i].SetCurrPos(aiVehicles[i].transform.position);
            racePositions.Add(aiShips[i]);
        }

        // calculate the counter value for each car, then sort it based on this value to determine who is what rank
        for (int i = 0; i < racePositions.Capacity; i++)
        {
            float distFromPrevWaypoint = GetFractionOfPathCovered(racePositions[i].GetCurrPos(), waypoints[racePositions[i].GetCurrWaypoint() % waypoints.Length], waypoints[(racePositions[i].GetCurrWaypoint() + 1) % waypoints.Length]);
            racePositions[i].SetCounter(racePositions[i].GetCurrLap() * LAP_MULTIPLIER + racePositions[i].GetCurrWaypoint() * WAYPOINT_MULTIPLIER + distFromPrevWaypoint + (oneLapCountVal * racePositions[i].GetCurrLap()));
        }
        racePositions.Sort(delegate (Ship s1, Ship s2) { return s2.GetCounter().CompareTo(s1.GetCounter()); });

        for (int i = 0; i < racePositions.Capacity; i++)
        {
            string driverName = racePositions[i].GetAIName() + " (" + racePositions[i].GetAIType().ToString()[0] + ")";
            rankingUI.SetRankingPosition(i, driverName);

            if (racePositions[i].GetAINumber() == 999) // MAGIC
            {
                UpdateUI_PlayerPos(i + 1); // + 1 to take array out of zero indexed list
            }
        }
    }

    public void UpdateSkillRequirements(float playerCounter, float targetOffset, float aiCounter, ref Ship aiShip)
    {
        int aiNumber = aiShip.GetAINumber();
        string aiName = aiShip.GetAIName();

        float targetPos = playerCounter + targetOffset; // calculate the target position of the ai ship

        // if they are in the last section of the race
        if (aiCounter >= aiCutOffCountVal)
        {
            if (aiShip.GetAIType() != AIType.Back) // stops back group increasing skill
            {
                if (aiCounter > playerCounter) // only if ai is ahead of the player
                {
                    //Debug.Log("Turning off my dynamic difficulty to give player a chance!");
                    aiShip.SetAIMidSkill();
                    UpdateUI_AIThought(aiNumber, aiName + "'s DCB is OFF.");
                }
                else
                {
                    //Debug.Log("Player is ahead, increasing skill to maximum!");
                    aiShip.SetAIBestSkill();
                    UpdateUI_AIThought(aiNumber, aiName + "'s DCB is ON.");
                }
            }
        }
        // if they are at that value (range) then keep steady
        else if (aiCounter > targetPos - AI_GROUP_RANGE && aiCounter < targetPos + AI_GROUP_RANGE)
        {
            //Debug.Log(aiShip.GetAINumber() + " I'm just right!");
            aiShip.SetAIMidSkill();
            UpdateUI_AIThought(aiNumber, aiName + " is just right!");
        }
        // if they are behind that value then gradually increase their skills
        else if (aiCounter < targetPos - AI_GROUP_RANGE)
        {
            //Debug.Log(aiShip.GetAINumber() + " I gotta increase my skill!");
            aiShip.SetAIBestSkill();
            UpdateUI_AIThought(aiNumber, aiName + " is too slow. Increasing skills.");
        }
        // if they are in front of that value then gradually reduce their skills
        else if (aiCounter > targetPos + AI_GROUP_RANGE)
        {
            //Debug.Log(aiShip.GetAINumber() + " I'm too good, gotta decrease skill.");
            aiShip.SetAIWorstSkill();
            UpdateUI_AIThought(aiNumber, aiName + " is too fast. Decreasing skills.");
        }
    }

    //Called by the FinishLine script
    public void PlayerCompletedLap()
	{
		//If the game is already over exit this method 
		if (isGameOver)
			return;

		//Incrememebt the current lap and reset waypoints
		playerShip.IncrementCurrLap();
        playerShip.SetCurrWaypoint(0);

		//Update the lap number UI on the ship
		UpdateUI_LapNumber ();

		//If the player has completed the required amount of laps...
		if (playerShip.GetCurrLap() >= numberOfLaps)
		{
			//...the game is now over...
			isGameOver = true;
			//...update the laptime UI...
			UpdateUI_FinalTime();
			//...and show the Game Over UI
			gameOverUI.SetActive(true);
		}
	}

    public void AICompletedLap(int aiNum)
    {
        if (isGameOver || !aiShips[aiNum].LapReady()) // or AI not ready ADD THIS
            return;

        aiShips[aiNum].IncrementCurrLap();
        aiShips[aiNum].SetCurrWaypoint(0);
        aiShips[aiNum].SetLapReady(false);

        if (aiShips[aiNum].GetCurrLap() >= numberOfLaps)
        {
            //Debug.Log("AI " + aiNum + " HAS FINISHED RACE");
            // it's ai? probs just keep going
        }
    }

    public void AIPassedLapChecker(int aiNum)
    {
        aiShips[aiNum].SetLapReady(true);
    }

    public void PlayerPassedWaypoint(int waypoint)
    {
        playerShip.SetCurrWaypoint(waypoint);
        //Debug.Log("player passed waypoint: " + waypoint);
    }

    public void AIPassedWaypoint(int aiNum, int waypoint)
    {
        aiShips[aiNum].SetCurrWaypoint(waypoint);
        //Debug.Log("AI: " + aiNum + " has passed: " + waypoint);
    }

	void UpdateUI_LapTime()
	{
		//If we have a LapTimeUI reference, update it
		if (lapTimeUI != null)
			lapTimeUI.SetLapTime(playerShip.GetCurrLap(), lapTimes[playerShip.GetCurrLap()]);
	}

	void UpdateUI_FinalTime()
	{
		//If we have a LapTimeUI reference... 
		if (lapTimeUI != null)
		{
			float total = 0f;

			//...loop through all of the lapTimes and total up an amount...
			for (int i = 0; i < lapTimes.Length; i++)
				total += lapTimes[i];

			//... and update the final race time
			lapTimeUI.SetFinalTime(total);
		}
	}

	void UpdateUI_LapNumber()
	{
		//If we have a ShipUI reference, update it
		if (shipUI != null) 
			shipUI.SetLapDisplay (playerShip.GetCurrLap() + 1, numberOfLaps);
	}

	void UpdateUI_Speed()
	{
		//If we have a VehicleMovement and ShipUI reference, update it
		if (vehicleMovement != null && shipUI != null) 
			shipUI.SetSpeedDisplay (Mathf.Abs(vehicleMovement.speed));
	}

    void UpdateUI_PlayerPos(int playerPos)
    {
        if (playerPosUI != null)
        {
            playerPosUI.SetPlayerPos(playerPos);
        }
    }

    void UpdateUI_AIThought(int aiNum, string thought)
    {
        if (aiThoughtsUI != null)
        {
            aiThoughtsUI.SetAIThought(aiNum, thought);
        }
    }

	public bool IsActiveGame()
	{
		//If the race has begun and the game is not over, we have an active game
		return raceHasBegun && !isGameOver;
	}

	public void Restart()
	{
		//Restart the scene by loading the scene that is currently loaded
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}

    public void Quit()
    {
        Application.Quit();
    }

    public void Pause()
    {
        if (pausedUI != null)
        {
            pausedUI.SetActive(true);
            Time.timeScale = 0.0f;
            gameIsPaused = true;
        }
    }

    public void Resume()
    {
        if (pausedUI != null)
        {
            pausedUI.SetActive(false);
            Time.timeScale = 1.0f;
            gameIsPaused = false;
        }
    }

    public void SetRaceHasBegun()
    {
        raceHasBegun = true;
    }
}
