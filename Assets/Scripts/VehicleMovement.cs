using UnityEngine;

public class VehicleMovement : MonoBehaviour
{
	public float speed; // current speed of the ship MAKE GETTERS FOR THIS CLASS

	// Drive Settings
	private float driveForce = 17.0f; // force that the engine generates
	private float slowingVelFactor = 0.99f; // percentage of velocity the ship maintains when not thrusting i.e. ship loses 1% velocity every frame if no thruster input
	private float brakingVelFactor = 0.95f; // percentage of velocty the ship maintains when braking i.e. ship loses 5% velocity every frame when brake key is held
	private float angleOfRoll = 30.0f; // angle that the ship "banks" into a turn

	// Hover Settings
	private float hoverHeight = 1.5f; // height the ship maintains when hovering
	private float maxGroundDist = 5.0f; // distance the ship can be above the ground before it is "falling"
	private float hoverForce = 300.0f; // force of the ship's hovering
	[SerializeField] private LayerMask whatIsGround; // layer mask to determine what layer the ground is on
	public PIDController hoverPID; // PID controller to smooth the ship's hovering (needs to be public - figure out why)

	// Physics Settings
	[SerializeField] private Transform shipBody; // reference to the ship's body, this is for cosmetics (banking)
	public float terminalVelocity = 100.0f; // max speed the ship can go
	private float hoverGravity = 20.0f; // gravity applied to the ship while it is on the ground - ignore Unity gravity so that hover car can hug track better
	private float fallGravity = 80.0f; // gravity applied to the ship while it is falling - so that falls feel more exciting
    private float drag; // air resistance the ship recieves in the forward direction
    private bool isOnGround; // flag determining if the ship is currently on the ground

    private Rigidbody rigidBody; // reference to the ship's rigidbody
	private PlayerInput input; // reference to the player's input					


	void Start()
	{
		rigidBody = GetComponent<Rigidbody>();
		input = GetComponent<PlayerInput>();

		drag = driveForce / terminalVelocity; // calculate the ship's drag value
    }

	void FixedUpdate()
	{
		speed = Vector3.Dot(rigidBody.velocity, transform.forward); // how much of the ship's velocity is in the "forward" direction 

        UpdateHover();
		UpdatePropulsion();
	}

	void UpdateHover()
	{
		Vector3 groundNormal;

		Ray ray = new Ray(transform.position, -transform.up);
		RaycastHit hitInfo;
		isOnGround = Physics.Raycast(ray, out hitInfo, maxGroundDist, whatIsGround); // raycast down to determine if car is on ground layer

		if (isOnGround)
		{
			float height = hitInfo.distance;
			groundNormal = hitInfo.normal.normalized; // store which way is up according to the ground
			float forcePercent = hoverPID.Seek(hoverHeight, height); // use PID controller to determine the hover force needed

			Vector3 force = groundNormal * hoverForce * forcePercent; // calculate total amount of hover force to be applied, using groundNormal to orient the car perfectly above the ground
			Vector3 gravity = -groundNormal * hoverGravity * height; // calculate the amount of gravity to be applied, fake gravity here as car is always pulled toward ground (ground may not always be world Down)
			rigidBody.AddForce(force, ForceMode.Acceleration);
			rigidBody.AddForce(gravity, ForceMode.Acceleration);
		}
		else // falling
		{
			groundNormal = Vector3.up; // consider world Up as our up because there is no ground to reference - this will cause car to self-right if it flips over

			Vector3 gravity = -groundNormal * fallGravity; // calculate the amount of gravity to be applied, stronger gravity force here to make it more exciting
			rigidBody.AddForce(gravity, ForceMode.Acceleration);
		}

		//Calculate the amount of pitch and roll the ship needs to match its orientation
		//with that of the ground. This is done by creating a projection and then calculating
		//the rotation needed to face that projection
		Vector3 projection = Vector3.ProjectOnPlane(transform.forward, groundNormal);
		Quaternion rotation = Quaternion.LookRotation(projection, groundNormal);

		//Move the ship over time to match the desired rotation to match the ground. This is 
		//done smoothly (using Lerp) to make it feel more realistic
		rigidBody.MoveRotation(Quaternion.Lerp(rigidBody.rotation, rotation, Time.deltaTime * 10f));

		//Calculate the angle we want the ship's body to bank into a turn based on the current rudder.
		//It is worth noting that these next few steps are completetly optional and are cosmetic.
		//It just feels so darn cool
		float angle = angleOfRoll * -input.rudder;

		//Calculate the rotation needed for this new angle
		Quaternion bodyRotation = transform.rotation * Quaternion.Euler(0f, 0f, angle);
		//Finally, apply this angle to the ship's body
		shipBody.rotation = Quaternion.Lerp(shipBody.rotation, bodyRotation, Time.deltaTime * 10f);
	}

	void UpdatePropulsion()
	{
		//Calculate the yaw torque based on the rudder and current angular velocity
		float rotationTorque = input.rudder - rigidBody.angularVelocity.y;
		//Apply the torque to the ship's Y axis
		rigidBody.AddRelativeTorque(0f, rotationTorque, 0f, ForceMode.VelocityChange);

		//Calculate the current sideways speed by using the dot product. This tells us
		//how much of the ship's velocity is in the "right" or "left" direction
		float sidewaysSpeed = Vector3.Dot(rigidBody.velocity, transform.right);

		//Calculate the desired amount of friction to apply to the side of the vehicle. This
		//is what keeps the ship from drifting into the walls during turns. If you want to add
		//drifting to the game, divide Time.fixedDeltaTime by some amount
		Vector3 sideFriction = -transform.right * (sidewaysSpeed / Time.fixedDeltaTime); 

		//Finally, apply the sideways friction
		rigidBody.AddForce(sideFriction, ForceMode.Acceleration);

		//If not propelling the ship, slow the ships velocity
		if (input.thruster <= 0f)
			rigidBody.velocity *= slowingVelFactor;

		//Braking or driving requires being on the ground, so if the ship
		//isn't on the ground, exit this method
		if (!isOnGround)
			return;

		//If the ship is braking, apply the braking velocty reduction
		if (input.isBraking)
			rigidBody.velocity *= brakingVelFactor;

		//Calculate and apply the amount of propulsion force by multiplying the drive force
		//by the amount of applied thruster and subtracting the drag amount
		float propulsion = driveForce * input.thruster - drag * Mathf.Clamp(speed, 0f, terminalVelocity);
		rigidBody.AddForce(transform.forward * propulsion, ForceMode.Acceleration);
	}

	void OnCollisionStay(Collision collision)
	{
		//If the ship has collided with an object on the Wall layer...
		if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
		{
			//...calculate how much upward impulse is generated and then push the vehicle down by that amount 
			//to keep it stuck on the track (instead up popping up over the wall)
			Vector3 upwardForceFromCollision = Vector3.Dot(collision.impulse, transform.up) * transform.up;
			rigidBody.AddForce(-upwardForceFromCollision, ForceMode.Impulse);
		}
	}

	public float GetSpeedPercentage()
	{
		//Returns the total percentage of speed the ship is traveling
		return rigidBody.velocity.magnitude / terminalVelocity;
	}
}
