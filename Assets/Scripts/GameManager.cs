//This script manages the timing and flow of the game. It is also responsible for telling
//the UI when and how to update

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class Ship // GETTERS
{
    public int currentLap;
    public int currentWaypoint;

    public Ship(int currLap, int currWaypoint)
    {
        currentLap = currLap;
        currentWaypoint = currWaypoint;
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
	//int currentLap = 0;						//The current lap the player is on
	bool isGameOver;						//A flag to determine if the game is over
	bool raceHasBegun;                      //A flag to determine if the race has begun

    //int currentWaypoint = 0; // for player
    Ship playerShip = new Ship(0, 0);
    
    



    [SerializeField] GameObject[] aiVehicles; // just for a count really, probs a better way of doing this
    Ship[] aiShips;
    //int[] aiCurrLap;
    //int[] aiCurrWaypoint;


    Ship[] racePositions; // probs gonna be ship class too


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

        //Initialise AI stuff
        aiVehicles = new GameObject[aiVehicles.Length];
        aiShips = new Ship[aiVehicles.Length];
        for (int i = 0; i < aiShips.Length; i++)
        {
            aiShips[i] = new Ship(0, 0);
        }
        //aiCurrLap = new int[aiVehicles.Length];
        //aiCurrWaypoint = new int[aiVehicles.Length];

        racePositions = new Ship[aiShips.Length + 1]; // + 1 for player car

	}

	void Update()
	{
		//Update the speed on the ship UI
		UpdateUI_Speed ();

		//If we have an active game...
		if (IsActiveGame())
		{
            // place all ships into array to then be sorted (probs shouldnt do this every update)
            for (int i = 0; i < racePositions.Length; i++)
            {
                if (i == racePositions.Length - 1) // to put the player in the last array slot?
                {
                    racePositions[i] = playerShip;
                }
                else
                {
                    racePositions[i] = aiShips[i]; // does this continually update the racePos array?
                }
            }
            // !!!!!!!!!!!!!!!!!!!!!
            Array.Sort(racePositions, delegate (Ship x, Ship y) { return x.currentLap.CompareTo(y.currentWaypoint); }); // do the sort manually



            //...calculate the time for the lap and update the UI MAKE THIS WORK AGAIN
            lapTimes[/*currentLap*/playerShip.currentLap] += Time.deltaTime;
			UpdateUI_LapTime();
		}
	}

	//Called by the FinishLine script
	public void PlayerCompletedLap()
	{
		//If the game is already over exit this method 
		if (isGameOver)
			return;

		//Incrememebt the current lap
		playerShip.currentLap++;

		//Update the lap number UI on the ship
		UpdateUI_LapNumber ();

		//If the player has completed the required amount of laps...
		if (playerShip.currentLap >= numberOfLaps)
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

        aiShips[aiNum].currentLap++;

        if (aiShips[aiNum].currentLap >= numberOfLaps)
        {
            Debug.Log("AI HAS FINISHED RACE");
            // it's ai? probs just keep going
        }
    }

    public void PlayerPassedWaypoint(int waypoint)
    {
        playerShip.currentWaypoint = waypoint;
        Debug.Log("player passed waypoint: " + waypoint);
    }

    public void AIPassedWaypoint(int aiNum, int waypoint)
    {
        aiShips[aiNum].currentWaypoint = waypoint;
        Debug.Log("AI: " + aiNum + " has passed: " + waypoint);
    }

	void UpdateUI_LapTime()
	{
		//If we have a LapTimeUI reference, update it
		if (lapTimeUI != null)
			lapTimeUI.SetLapTime(playerShip.currentLap, lapTimes[playerShip.currentLap]);
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
			shipUI.SetLapDisplay (playerShip.currentLap + 1, numberOfLaps);
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
