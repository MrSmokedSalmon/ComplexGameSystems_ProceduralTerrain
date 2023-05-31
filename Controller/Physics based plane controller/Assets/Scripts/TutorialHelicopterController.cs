
using System;

using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialHelicopterController : MonoBehaviour
{
	[SerializeField] private InputActionReference pitchRollAction;
	[SerializeField] private InputActionReference yawAction;
	[SerializeField] private InputActionReference throttleAction;

	[SerializeField] private UI ui;
	
	private Rigidbody rigidbody;

	[SerializeField] private float responsiveness = 500f;
	
	[SerializeField] private float throttleMax = 500f;
	[SerializeField] private float throttleAmount = 25f;
	[SerializeField] private float throttle;

	private float roll;
	private float pitch;
	private float yaw;

	private void OnEnable()
	{
		pitchRollAction.action.Enable();
		yawAction.action.Enable();
		throttleAction.action.Enable();
	}

	private void OnDisable()
	{
		pitchRollAction.action.Disable();
		yawAction.action.Disable();
		throttleAction.action.Disable();
	}

	private void Awake()
	{
		rigidbody = GetComponent<Rigidbody>();
	}

	private void Update()
	{
		HandleInputs();
		
		ui.UpdateIndicators(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.z, 
			transform.localRotation.eulerAngles.y, throttle / throttleMax, transform.position.y);
	}

	private void FixedUpdate()
	{
		rigidbody.AddForce(transform.up * throttle, ForceMode.Impulse);
		
		rigidbody.AddTorque(transform.right * pitch * responsiveness);
		rigidbody.AddTorque(transform.forward * roll * responsiveness);
		rigidbody.AddTorque(transform.up * yaw * responsiveness);
	}

	private void HandleInputs()
	{
		Vector2 pitchNRoll = pitchRollAction.action.ReadValue<Vector2>();

		roll = -pitchNRoll.x;
		pitch = pitchNRoll.y;
		yaw = yawAction.action.ReadValue<float>();
		
		throttle += throttleAmount * Time.deltaTime * throttleAction.action.ReadValue<float>();;
		
		throttle = Mathf.Clamp(throttle, 0, throttleMax);
	}
}