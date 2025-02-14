using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    //Variables
    private PlayerInput playerInput;
    private PlayerInput.OnFootActions onFoot;

    private PlayerMotor motor;
    private PlayerLook look;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerInput = new PlayerInput();
        onFoot = playerInput.OnFoot;

        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Tell playermotor to move using value from movement action
        motor.ProcessMove(onFoot.Movement.ReadValue<Vector2>());
    }

	private void LateUpdate()
	{
		look.ProcessLook(onFoot.Look.ReadValue<Vector2>());
	}

	private void OnEnable()
	{
		onFoot.Enable();
	}

	private void OnDisable()
	{
		onFoot.Disable();
	}
}
