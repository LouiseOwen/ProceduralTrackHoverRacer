//This script manages the timing and flow of the game. It is also responsible for telling
//the UI when and how to update

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

//seperate these
public enum AIType { Advanced, Middle, Back, Close, Not }

public class Ship
{
    private int aiNumber;
    private AIType aiType;
    private int currentLap;
    private int currentWaypoint;
    private Vector3 currentPosition;
    private float counter;

    public Ship(int aiNum, AIType type, int currLap, int currWaypoint, Vector3 currPos)
    {
        aiNumber = aiNum;
        aiType = type;
        currentLap = currLap;
        currentWaypoint = currWaypoint;
        currentPosition = currPos;
    }

    // Getters
    public int GetAINumber()
    {
        return aiNumber;
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
}

public class GameManager : MonoBehaviour
{
	//The game manager holds a public static reference to itself. This is often referred to
	//as being a "singleton" and allows it to be access from all other objects in the scene.
	//This should be used carefully and is generally reserved for "manager" type objects
	public static GameManager instance;		

	[Header("Race Settings")]
	public int numberOfLaps = 3;			//The number of laps to complete
	public VehicleMovement vehicleMovement;	//A reference to the ship's VehicleMovement script

	[Header("UI References")]
	public ShipUI shipUI;					//A reference to the ship's ShipUI script
	public LapTimeUI lapTimeUI;				//A reference to the LapTimeUI script in the scene
	public GameObject gameOverUI;			//A reference to the UI objects that appears when the game is complete

	float[] lapTimes;						//An array containing the player's lap times
	bool isGameOver;						//A flag to determine if the game is over
	bool raceHasBegun;                      //A flag to determine if the race has begun
    [SerializeField] GameObject playerShipObj;
    Ship playerShip = new Ship(999, AIType.Not, 0, 0, Vector3.zero); // to store player ship details
    
    // AI
    [SerializeField] GameObject[] aiVehicles; // needed for correct count and reference to the car's attributes
    Ship[] aiShips; // could probably just merge this into the aiVehicles array but easier like this for now

    // Race management
    [SerializeField] GameObject waypointsObj; // to find the exact position of a waypoint
    Vector3[] waypoints;
    List<Ship> racePositions; // ordered list of car positions in race


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
		raceHasBegun = true;

        //Initialise Player
        playerShip.SetCurrPos(playerShipObj.transform.position);

        //Initialise AI stuff
        aiShips = new Ship[aiVehicles.Length];
        for (int i = 0; i < aiShips.Length; i++)
        {
            aiShips[i] = new Ship(i, AIType.Not, 0, 0, aiVehicles[i].transform.position);
        }
        // FIND WAY TO SORT THIS OUT? - or just hardcode it
        aiShips[0].SetAIType(AIType.Advanced);
        aiShips[1].SetAIType(AIType.Middle);
        aiShips[2].SetAIType(AIType.Back);
        aiShips[3].SetAIType(AIType.Close);



        //Initialise Waypoints
        waypoints = new Vector3[waypointsObj.transform.childCount];
        for (int i = 0; i < waypoints.Length; i++)
        {
            waypoints[i] = waypointsObj.transform.GetChild(i).transform.position;
        }
	}

	void Update()
	{
		//Update the speed on the ship UI
		UpdateUI_Speed ();

		//If we have an active game...
		if (IsActiveGame())
		{


            // clears it each frame, there will be a more efficient way of doing this, just leaving it here for now
            racePositions = new List<Ship>(aiVehicles.Length + 1); // + 1 for player car
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
                racePositions[i].SetCounter(racePositions[i].GetCurrLap() * 1000.0f + racePositions[i].GetCurrWaypoint() * 100.0f + distFromPrevWaypoint); // MAGIC
            }
            racePositions.Sort(delegate (Ship s1, Ship s2) { return s2.GetCounter().CompareTo(s1.GetCounter()); });


            // DYNAMIC DIFFICULTY ADJUST HERE!!!
            Debug.Log("player ship current counter: " + playerShip.GetCounter());
            for (int i = 0; i < aiShips.Length; i++)
            {
                // REDUCE THE CALLS TO GETCOUNTER(), ALREADY A TAXING SOLUTION HERE
                if (aiShips[i].GetAIType() == AIType.Advanced)
                {
                    // get the checkpoint (counter value) 4 in front of players checkpoint
                    float targetPos = playerShip.GetCounter() + 400.0f; // MAGIC
                    Debug.Log(targetPos + " advanced AI target.");

                    // if they are at that value (range) then keep steady
                    if (aiShips[i].GetCounter() > targetPos - 100.0f && aiShips[i].GetCounter() < targetPos + 100.0f) // MAGIC
                    {
                        Debug.Log(aiShips[i].GetAINumber() + " I'm just right!");
                    }
                    // if they are behind that value then gradually increase their skills
                    else if (aiShips[i].GetCounter() < targetPos - 100.0f) // MAGIC
                    {
                        Debug.Log(aiShips[i].GetAINumber() + " I gotta increase my skill!");
                    }
                    // if they are in front of that value then gradually reduce their skills
                    else if (aiShips[i].GetCounter() > targetPos + 100.0f) // MAGIC
                    {
                        Debug.Log(aiShips[i].GetAINumber() + " I'm too good, gotta decrease skill.");
                    }

                }
                else if (aiShips[i].GetAIType() == AIType.Middle)
                {
                    // get the checkpoint (counter value) 2 in front of the players checkpoint
                    float targetPos = playerShip.GetCounter() + 200.0f; // MAGIC
                    Debug.Log(targetPos + " middle AI target.");

                    // if they are at that value (range) then keep steady
                    if (aiShips[i].GetCounter() > targetPos - 100.0f && aiShips[i].GetCounter() < targetPos + 100.0f) // MAGIC
                    {
                        Debug.Log(aiShips[i].GetAINumber() + " I'm just right!");
                    }
                    // if they are behind that value then gradually increase their skills
                    else if (aiShips[i].GetCounter() < targetPos - 100.0f) // MAGIC
                    {
                        Debug.Log(aiShips[i].GetAINumber() + " I gotta increase my skill!");
                    }
                    // if they are in front of that value then gradually reduce their skills
                    else if (aiShips[i].GetCounter() > targetPos + 100.0f) // MAGIC
                    {
                        Debug.Log(aiShips[i].GetAINumber() + " I'm too good, gotta decrease skill.");
                    }
                }
                else if (aiShips[i].GetAIType() == AIType.Back)
                {
                    // get the checkpoint (counter value) 4 BEHIND the players checkpoint
                    float targetPos = playerShip.GetCounter() - 400.0f; // MAGIC
                    Debug.Log(targetPos + " back AI target.");

                    // if they are at that value (range) then keep steady
                    if (aiShips[i].GetCounter() > targetPos - 100.0f && aiShips[i].GetCounter() < targetPos + 100.0f) // MAGIC
                    {
                        Debug.Log(aiShips[i].GetAINumber() + " I'm just right!");
                    }
                    // if they are behind that value then gradually increase their skills
                    else if (aiShips[i].GetCounter() < targetPos - 100.0f) // MAGIC
                    {
                        Debug.Log(aiShips[i].GetAINumber() + " I gotta increase my skill!");
                    }
                    // if they are in front of that value then gradually reduce their skills
                    else if (aiShips[i].GetCounter() > targetPos + 100.0f) // MAGIC
                    {
                        Debug.Log(aiShips[i].GetAINumber() + " I'm too good, gotta decrease skill.");
                    }
                }
                else if (aiShips[i].GetAIType() == AIType.Close)
                {
                    // get the checkpoint (counter value) of the players checkpoint
                    float targetPos = playerShip.GetCounter();
                    Debug.Log(targetPos + " close AI target.");

                    // if they are at that value (range) then keep steady
                    if (aiShips[i].GetCounter() > targetPos - 100.0f && aiShips[i].GetCounter() < targetPos + 100.0f) // MAGIC
                    {
                        Debug.Log(aiShips[i].GetAINumber() + " I'm just right!");
                    }
                    // if they are behind that value then gradually increase their skills
                    else if (aiShips[i].GetCounter() < targetPos - 100.0f) // MAGIC
                    {
                        Debug.Log(aiShips[i].GetAINumber() + " I gotta increase my skill!");
                    }
                    // if they are in front of that value then gradually reduce their skills
                    else if (aiShips[i].GetCounter() > targetPos + 100.0f) // MAGIC
                    {
                        Debug.Log(aiShips[i].GetAINumber() + " I'm too good, gotta decrease skill.");
                    }
                }
            }



            //...calculate the time for the lap and update the UI MAKE THIS WORK AGAIN
            lapTimes[playerShip.GetCurrLap()] += Time.deltaTime;
			UpdateUI_LapTime();
		}
	}

    public float GetFractionOfPathCovered(Vector3 position, Vector3 currWaypoint, Vector3 nextWaypoint)
    {
        Vector3 distFromCurrWaypoint = position - currWaypoint;
        Vector3 currSegmentVector = nextWaypoint - currWaypoint;
        float fraction = Vector3.Dot(distFromCurrWaypoint, currSegmentVector) / currSegmentVector.sqrMagnitude;

        return fraction;
    }

	//Called by the FinishLine script
	public void PlayerCompletedLap()
	{
		//If the game is already over exit this method 
		if (isGameOver)
			return;

		//Incrememebt the current lap
		playerShip.IncrementCurrLap();

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
        if (isGameOver)
            return;

        aiShips[aiNum].IncrementCurrLap();

        if (aiShips[aiNum].GetCurrLap() >= numberOfLaps)
        {
            Debug.Log("AI " + aiNum + " HAS FINISHED RACE");
            // it's ai? probs just keep going
        }
    }

    public void PlayerPassedWaypoint(int waypoint)
    {
        playerShip.SetCurrWaypoint(waypoint);
        Debug.Log("player passed waypoint: " + waypoint);
    }

    public void AIPassedWaypoint(int aiNum, int waypoint)
    {
        aiShips[aiNum].SetCurrWaypoint(waypoint);
        Debug.Log("AI: " + aiNum + " has passed: " + waypoint);
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
}
