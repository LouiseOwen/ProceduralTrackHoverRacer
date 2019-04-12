//This script handles reading inputs from the player and passing it on to the vehicle. We 
//separate the input code from the behaviour code so that we can easily swap controls 
//schemes or even implement and AI "controller". Works together with the VehicleMovement script

using UnityEngine;

// CAN PROBABLY SPLIT THIS INTO TWO CLASSES (Human and AI)
public class PlayerInput : MonoBehaviour
{
    public bool isHuman;

	public string verticalAxisName = "Vertical";        //The name of the thruster axis
	public string horizontalAxisName = "Horizontal";    //The name of the rudder axis
	public string brakingKey = "Brake";                 //The name of the brake button

	//We hide these in the inspector because we want 
	//them public but we don't want people trying to change them
	[HideInInspector] public float thruster;			//The current thruster value
	[HideInInspector] public float rudder;				//The current rudder value
	[HideInInspector] public bool isBraking;			//The current brake value

    // AI THINGS
    private VehicleMovement m_Vehicle; // so we can get values
    private float m_RandomPerlin; // A random value for the car to base its wander on (so that AI cars don't all wander in the same pattern)
    private Rigidbody m_Rigidbody; // to get speed values etc.
    [SerializeField] private Transform m_Target; // 'target' the target object to aim for.
    [SerializeField] private bool m_Driving = true; // whether the AI is currently actively driving or stopped.
    [SerializeField] private float m_CautiousAngularVelocityFactor = 100f; // how cautious the AI should be when considering its own current angular velocity (i.e. easing off acceleration if spinning!)
    [SerializeField] [Range(0, 180)] private float m_CautiousMaxAngle = 90f; // angle of approaching corner to treat as warranting maximum caution
    [SerializeField] [Range(0, 1)] private float m_CautiousSpeedFactor = 0.6f; // percentage of max speed to use when being maximally cautious
    private float m_AvoidOtherCarTime; // time until which to avoid the car we recently collided with
    private float m_AvoidOtherCarSlowdown; // how much to slow down due to colliding with another car, whilst avoiding
    private float m_AvoidPathOffset; // direction (-1 or 1) in which to offset path to avoid other car, whilst avoiding
    [SerializeField] private float m_LateralWanderSpeed = 0.2f; // how fast the lateral wandering will fluctuate
    [SerializeField] private float m_LateralWanderDistance = 3f; // how far the car will wander laterally towards its target
    [SerializeField] private float m_BrakeSensitivity = 1f; // How sensitively the AI uses the brake to reach the current desired speed
    [SerializeField] private float m_AccelSensitivity = 0.5f; // How sensitively the AI uses the accelerator to reach the current desired speed
    [SerializeField] [Range(0, 1)] private float m_AccelWanderAmount = 0.5f; // how much the cars acceleration will wander
    [SerializeField] private float m_AccelWanderSpeed = 0.1f; // how fast the cars acceleration wandering will fluctuate
    [SerializeField] private float m_SteerSensitivity = 0.3f; // how sensitively the AI uses steering input to turn to the desired direction

    private void Awake()
    {
        m_Vehicle = GetComponent<VehicleMovement>();
        m_RandomPerlin = Random.value * 100.0f;
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate() // for AI stuff
    {
        //If a GameManager exists and the game is not active...
        if (GameManager.instance != null && !GameManager.instance.IsActiveGame())
        {
            //...set all inputs to neutral values and exit this method
            thruster = rudder = 0f;
            isBraking = false;
            return;
        }

        if (!isHuman)
        {
            if (m_Target == null || !m_Driving) // no movement
            {
                thruster = 0.0f;
                rudder = 0.0f;
                isBraking = false; // maybe true?
            }
            else
            {
                Vector3 fwd = transform.forward;
                if (m_Rigidbody.velocity.magnitude > m_Vehicle.terminalVelocity * 0.1f) // if the current speed is more than the top speed, set to current speed (for downward hills i guess) scaled down top speed for magnitude value?
                {
                    fwd = m_Rigidbody.velocity;
                }

                float desiredSpeed = m_Vehicle.terminalVelocity;

                // decide if we should slow down
                float approachingCornerAngle = Vector3.Angle(m_Target.forward, fwd); // finds different in angle between these 2 points

                float spinningAngle = m_Rigidbody.angularVelocity.magnitude * m_CautiousAngularVelocityFactor;

                float cautiousnessRequired = Mathf.InverseLerp(0, m_CautiousMaxAngle, Mathf.Max(spinningAngle, approachingCornerAngle));

                desiredSpeed = Mathf.Lerp(m_Vehicle.terminalVelocity, m_Vehicle.terminalVelocity * m_CautiousSpeedFactor, cautiousnessRequired);
                //Debug.Log(desiredSpeed);
                // evasive cos cars
                Vector3 offsetTargetPos = m_Target.position;

                if (Time.time < m_AvoidOtherCarTime)
                {
                    desiredSpeed *= m_AvoidOtherCarSlowdown;

                    offsetTargetPos += m_Target.right * m_AvoidPathOffset;
                }
                else
                {
                    offsetTargetPos += m_Target.right * (Mathf.PerlinNoise(Time.time * m_LateralWanderSpeed, m_RandomPerlin) * 2 - 1) * m_LateralWanderDistance; // adding sideways wander to path
                }

                float accelBrakeSensitivity = (desiredSpeed < m_Vehicle.speed) ? m_BrakeSensitivity : m_AccelSensitivity;

                float accel = Mathf.Clamp((desiredSpeed - m_Vehicle.speed) * accelBrakeSensitivity, -1, 1);

                accel *= (1 - m_AccelWanderAmount) + (Mathf.PerlinNoise(Time.time * m_AccelWanderSpeed, m_RandomPerlin) * m_AccelWanderAmount);

                Vector3 localTarget = transform.InverseTransformPoint(offsetTargetPos);

                float targetAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;

                float steer = Mathf.Clamp(targetAngle * m_SteerSensitivity, -1, 1) * Mathf.Sign(m_Vehicle.speed);

                rudder = steer;
                if (desiredSpeed < m_Vehicle.speed) // unlikely, more likely when bumpy bits are in. Braking will be more visable when car avoidance is in
                {
                    thruster = 0.0f;
                    isBraking = true;
                    //Debug.Log("I braked");
                }
                else
                {
                    thruster = accel;
                    isBraking = false;
                }
            }
        }
    }

    void Update()
	{
		//If the player presses the Escape key and this is a build (not the editor), exit the game
		if (Input.GetButtonDown("Cancel") && !Application.isEditor)
			Application.Quit();

		//If a GameManager exists and the game is not active...
		if (GameManager.instance != null && !GameManager.instance.IsActiveGame())
		{
			//...set all inputs to neutral values and exit this method
			thruster = rudder = 0f;
			isBraking = false;
			return;
		}

        if (isHuman)
        {
            //Get the values of the thruster, rudder, and brake from the input class
            thruster = Mathf.Clamp(Input.GetAxis(verticalAxisName), -0.6f, 0.6f); // MAGIC
            rudder = Input.GetAxis(horizontalAxisName);
            isBraking = Input.GetButton(brakingKey);
        }
	}

    private void OnCollisionStay(Collision col)
    {
        // detect collision against other cars, so that we can take evasive action
        if (col.rigidbody != null)
        {
            var otherCar = col.rigidbody.GetComponent<PlayerInput>();

            if (isHuman)
            {
                return;
            }

            if (otherCar != null)
            {
                // we'll take evasive action for 1 second
                m_AvoidOtherCarTime = Time.time + 1;

                // but who's in front?...
                if (Vector3.Angle(transform.forward, otherCar.transform.position - transform.position) < 90)
                {
                    // the other ai is in front, so it is only good manners that we ought to brake...
                    m_AvoidOtherCarSlowdown = 0.75f;
                }
                else
                {
                    // we're in front! ain't slowing down for anybody...
                    m_AvoidOtherCarSlowdown = 1f;
                }

                // both cars should take evasive action by driving along an offset from the path centre,
                // away from the other car
                var otherCarLocalDelta = transform.InverseTransformPoint(otherCar.transform.position);
                float otherCarAngle = Mathf.Atan2(otherCarLocalDelta.x, otherCarLocalDelta.z);
                m_AvoidPathOffset = m_LateralWanderDistance * -Mathf.Sign(otherCarAngle);
            }
        }
    }

    public void SetTarget(Transform target)
    {
        m_Target = target;
        m_Driving = true;
    }

    public void BestSkill()
    {
        m_CautiousSpeedFactor = 0.6f; // MAGIC
        m_BrakeSensitivity = 0.5f;
        m_AccelSensitivity = 1.0f;
        m_SteerSensitivity = 0.7f;
    }

    public void MidSkill()
    {
        m_CautiousSpeedFactor = 0.5f; // MAGIC
        m_BrakeSensitivity = 0.75f;
        m_AccelSensitivity = 0.75f;
        m_SteerSensitivity = 0.5f;
    }

    public void WorstSkill()
    {
        m_CautiousSpeedFactor = 0.4f; // MAGIC
        m_BrakeSensitivity = 1.0f;
        m_AccelSensitivity = 0.5f;
        m_SteerSensitivity = 0.3f;
    }
}
