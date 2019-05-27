using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Cinemachine;

public class GameManager : MonoBehaviour
{
	[HideInInspector] public static GameManager instance; // a singleton class
    [HideInInspector] public static bool gameIsPaused = false; // says if game is currently paused or not

    // Constants
    const int NUM_LAPS = 3; // number of laps a race has
    const int NUM_AI_TYPES = 4; // number of AI types there are (Back, Middle, Advanced and Close)
    const float WAYPOINT_MULTIPLIER = 100.0f; // to multiply by a vehicles current waypoint index so that vehicle ranking Counter can be calculated
    const float LAP_MULTIPLIER = 1000.0f; // to multiply by a vehicles current number of laps completed so that vehicle ranking Counter can be calculated
    const float AI_CUTOFF_PERCENT = 0.8f; // the percentage of race to be completed before DCB becomes less (gives player chance of winning) 0.0f -> 1.0f
    const float DYNAMIC_DIFF_BEGIN = 5.0f; // number of seconds into the race before DCB is turned on
    const float ADV_AI_TARGET_OFFSET = 400.0f; // to add to player's Counter as target Counter value for Advanced group AI
    const float MID_AI_TAREGT_OFFSET = 200.0f; // to add to player's Counter as target Counter value for Middle group AI
    const float BACK_AI_TARGET_OFFSET = -400.0f; // to add to player's Counter as target Counter value for Back group AI
    const float AI_GROUP_RANGE = 100.0f; // to create boundary values for range AI's Counter value can be in around it's target Counter value
    const int CAM_HIGH_PRIORITY = 10; // Cinemachine will prioritise camera with higher value
    const int CAM_LOW_PRIORITY = 5; // Cinemachine will ignore camera with lower value
    const KeyCode TOGGLE_HUMAN_AI_KEY = KeyCode.H;

	[Header("UI References")]
	[SerializeField] private ShipUI shipUI;
	[SerializeField] private LapTimeUI lapTimeUI;
	[SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject pausedUI;
    [SerializeField] private AIThoughtsUI aiThoughtsUI;
    [SerializeField] private PlayerPositionUI playerPosUI;
    [SerializeField] private RankingUI rankingUI;

    // Race Management
	private bool isGameOver = false; // flag to say if the race has finished
	private bool raceHasBegun = false; // flag to say if the race has started
    private int numberOfLaps = NUM_LAPS; // number of laps player must complete
    [Header("Race Managament References")] // will only show up in inspector if variable below it is serialised or public... ugh
    [SerializeField] private GameObject waypointsObj; // to find the positions of each waypoint (the parent object of the waypoints)
    private Vector3[] waypoints; // array of all waypoint world positions, to calculate path covered between waypoints
    private List<Ship> racePositions; // list of vehicle rankings in race (to be sorted)
    private float timer = 0.0f; // to count from start of race to when AI should start using DCB
    private float oneLapCountVal; // the Counter value of one lap (used as bonus addition to rectify Counter bug and to calculate entireRaceCountVal)
    private float entireRaceCountVal; // the Counter value of an entire race, to calculate the cut off point for DCB
    private float aiCutOffCountVal; // the Counter value that, once an AI has passed, it will reduce skill to give player chance

    [Header("Vehicle References")]
    [SerializeField] private GameObject playerShipObj; // reference to player ship gameobject, mainly for positional information
    [SerializeField] private VehicleMovement vehicleMovement; // to know how fast the player vehicle is going for UI
    private Ship playerShip = new Ship(999, "Player", AIType.Not, 0, 0, Vector3.zero); // to store player ship information such as ID and ranking Counter
    private float[] lapTimes; // array of player's lap times (for UI purposes)
    [SerializeField] private GameObject[] aiVehicles; // reference to AI ships for correct count of AI and positional information
    private Ship[] aiShips; // to store AI ship information such as ID and ranking Counter

    [Header("Camera References")]
    [SerializeField] private GameObject followCameraObj; // for viewing ship from behind
    [SerializeField] private GameObject hoverCameraObj; // for viewing ship from above
    private CinemachineVirtualCamera followCam;
    private CinemachineVirtualCamera hoverCam;


	void Awake()
	{
        // Singleton, if there's no GameManager instance already, make this one it
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this) // if there already is a GameManager instance then delete this one
        {
            Destroy(gameObject);
        }
	}

	void OnEnable()
	{
        // Coroutine to allow initalisation of race data while cutscene countdown plays
		StartCoroutine(Init());
	}

	IEnumerator Init()
	{
        // Initialise UI
		UpdateUI_LapNumber();
		lapTimes = new float[numberOfLaps];

        // Initialise Player
        playerShip.SetCurrPos(playerShipObj.transform.position);
 
        // Initialise AI version of Player
        playerShip.SetShipObject(playerShipObj);
        playerShip.SetAIMidSkill();

        // Initialise AI
        string[] aiNames = new string[8] { "Mario", "Luigi", "Peach", "Toad", "Yoshi", "Bowser", "Daisy", "Wario" }; // MAGIC
        aiShips = new Ship[aiVehicles.Length];
        for (int i = 0; i < aiShips.Length; i++)
        {
            aiShips[i] = new Ship(i, aiNames[i], (AIType)(i % NUM_AI_TYPES), 0, 0, aiVehicles[i].transform.position, aiVehicles[i]);
            aiShips[i].SetAIBestSkill(); // for strong start (helps form groups)
        }
        //for (int i = 0; i < aiShips.Length; i++)
        //{
        //    Debug.Log(aiShips[i].GetAINumber() + " has type " + aiShips[i].GetAIType().ToString());
        //}

        // Initialise Race Management
        racePositions = new List<Ship>(aiVehicles.Length + 1); // + 1 for Player vehicle
        waypoints = new Vector3[waypointsObj.transform.childCount];
        for (int i = 0; i < waypoints.Length; i++)
        {
            waypoints[i] = waypointsObj.transform.GetChild(i).transform.position;
        }

        // Calculate race Counter values
        oneLapCountVal = waypoints.Length * WAYPOINT_MULTIPLIER;
        entireRaceCountVal = (oneLapCountVal * numberOfLaps) + /* >>> LAP BONUS TO AVOID COUNTER ERROR >>> */ ((numberOfLaps - 1) * LAP_MULTIPLIER); // - 1 because 1st lap does not require bonus
        //Debug.Log(entireRaceCountVal);
        aiCutOffCountVal = entireRaceCountVal * AI_CUTOFF_PERCENT;

        // Initialise Cameras
        followCam = followCameraObj.GetComponent<CinemachineVirtualCamera>();
        hoverCam = hoverCameraObj.GetComponent<CinemachineVirtualCamera>();

        yield return new WaitForSeconds(.1f);
    }

    void Update()
	{
        UpdateUI_Speed();

		if (IsActiveGame())
		{
            UpdateRacePositions(); // to update race rankings

            // DEBUG Output Player's Counter value
            //for (int i = 0; i < racePositions.Capacity; i++)
            //{
            //    if (racePositions[i].GetAINumber() == 999)
            //    {
            //        Debug.Log(racePositions[i].GetCounter());
            //    }
            //}

            if (rankingUI != null)
            {
                rankingUI.EnableRankingList();
            }

            timer += Time.deltaTime; // increment timer for DCB start
            if (timer > DYNAMIC_DIFF_BEGIN)
            {
                // Dynamic Difficulty Adjust
                float playerShipCounter = playerShip.GetCounter(); // so that we know where the Player ship is
                for (int i = 0; i < aiShips.Length; i++)
                {
                    float aiShipCounter = aiShips[i].GetCounter(); // so that we know where the AI ship is

                    // based on what AI type it is (and what position it is in relative to player) update the AI skill
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

                if (aiThoughtsUI != null)
                {
                    aiThoughtsUI.EnableAIThoughtsList();
                }
            }

            lapTimes[playerShip.GetCurrLap()] += Time.deltaTime; // calculate time for Player's lap
			UpdateUI_LapTime();

            // Toggle Human or AI Driver
            if (Input.GetKeyDown(TOGGLE_HUMAN_AI_KEY))
            {
                playerShipObj.GetComponent<PlayerInput>().ToggleIsHuman();

                // change camera priority based on Player's human/AI status
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
                //Debug.Log("Human/AI has best skill");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                playerShip.SetAIMidSkill();
                //Debug.Log("Human/AI has mid skill");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                playerShip.SetAIWorstSkill();
                //Debug.Log("Human/AI has worst skill");
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

    public float GetFractionOfPathCovered(Vector3 position, Vector3 currWaypoint, Vector3 nextWaypoint) // calculates distance covered between 2 waypoint positions
    {
        Vector3 distFromCurrWaypoint = position - currWaypoint;
        Vector3 currSegmentVector = nextWaypoint - currWaypoint;
        float fraction = Vector3.Dot(distFromCurrWaypoint, currSegmentVector) / currSegmentVector.sqrMagnitude;

        return fraction;
    }

    public void UpdateRacePositions()
    {
        // clear and then refill race ranking array with updated information
        racePositions.Clear();
        playerShip.SetCurrPos(playerShipObj.transform.position);
        racePositions.Add(playerShip);
        for (int i = 0; i < aiShips.Length; i++)
        {
            aiShips[i].SetCurrPos(aiVehicles[i].transform.position);
            racePositions.Add(aiShips[i]);
        }

        // calculate the Counter value for each vehicle, then sort it based on this value to determine who is what rank
        for (int i = 0; i < racePositions.Capacity; i++)
        {
            float distFromPrevWaypoint = GetFractionOfPathCovered(racePositions[i].GetCurrPos(), waypoints[racePositions[i].GetCurrWaypoint() % waypoints.Length], waypoints[(racePositions[i].GetCurrWaypoint() + 1) % waypoints.Length]);
            racePositions[i].SetCounter(racePositions[i].GetCurrLap() * LAP_MULTIPLIER + racePositions[i].GetCurrWaypoint() * WAYPOINT_MULTIPLIER + distFromPrevWaypoint + /* LAP BONUS TO AVOID COUNTER ERRORS */ (oneLapCountVal * racePositions[i].GetCurrLap()));
        }
        racePositions.Sort(delegate (Ship s1, Ship s2) { return s2.GetCounter().CompareTo(s1.GetCounter()); });

        // update information for race ranking UI
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

        float targetPos = playerCounter + targetOffset; // calculate the target position for the AI ship

        // if they are in the last section of the race
        if (aiCounter >= aiCutOffCountVal)
        {
            if (aiShip.GetAIType() != AIType.Back) // stops Back group increasing skill
            {
                if (aiCounter > playerCounter) // only if AI is ahead of the player
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

    public void PlayerCompletedLap() // called by FinishLine
	{
		if (isGameOver)
			return;

		// increment current lap and clear waypoint count
		playerShip.IncrementCurrLap();
        playerShip.SetCurrWaypoint(0);

		UpdateUI_LapNumber();

		// If Player has completed the number of laps the game is over
		if (playerShip.GetCurrLap() >= numberOfLaps)
        { 
			isGameOver = true;
			UpdateUI_FinalTime();
			gameOverUI.SetActive(true);
		}
	}

    public void AICompletedLap(int aiNum)
    {
        if (isGameOver || !aiShips[aiNum].LapReady()) // or AI has not passed LapChecker (to avoid race start errors where FinishLine is always active for AI)
            return;

        // increment current lap, clear waypoint count and reset LapChecker bool
        aiShips[aiNum].IncrementCurrLap();
        aiShips[aiNum].SetCurrWaypoint(0);
        aiShips[aiNum].SetLapReady(false);
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
        if (lapTimeUI != null)
        {
            lapTimeUI.SetLapTime(playerShip.GetCurrLap(), lapTimes[playerShip.GetCurrLap()]);
        }
	}

	void UpdateUI_FinalTime()
	{
		if (lapTimeUI != null)
		{
			float total = 0.0f;

            // loop through all lapTimes and calculate total amount
            for (int i = 0; i < lapTimes.Length; i++)
            {
                total += lapTimes[i];
            }

			lapTimeUI.SetFinalTime(total);
		}
	}

	void UpdateUI_LapNumber()
	{
        if (shipUI != null)
        {
            shipUI.SetLapDisplay(playerShip.GetCurrLap() + 1, numberOfLaps);
        }
	}

	void UpdateUI_Speed()
	{
        if (vehicleMovement != null && shipUI != null)
        {
            shipUI.SetSpeedDisplay(Mathf.Abs(vehicleMovement.speed));
        }
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
		// if the race has begun and the game is not over, it's an active game
		return raceHasBegun && !isGameOver;
	}

	public void Restart()
	{
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
