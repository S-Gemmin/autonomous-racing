using UnityEngine;

public class Kart : MonoBehaviour
{
    public Transform centerOfMass;
    public float topSpeed = 10f;
    public float accel = 5f;
    public float accelCurve = 4f;
    public float reverseSpeed = 5f;
    public float steer = 5f;

    private IInput[] inputs;
    private Rigidbody rb;
    private InputData input;

    public float speed => rb.velocity.magnitude;
    public float localSpeed => LocalSpeed();
    public float turnInput => input.turnInput;

    public Vector3 velocity
    {
        get => rb.velocity;
        set => rb.velocity = value; 
    }

    private float LocalSpeed()
    {
        float dot = Vector3.Dot(transform.forward, velocity);

        if (Mathf.Abs(dot) <= 0.1f)
            return 0f;

        return dot < 0f ? -speed / reverseSpeed : speed / topSpeed;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputs = GetComponents<IInput>();

        rb.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            input = inputs[i].GenerateInput();
        }

        float accelInput = (input.accelerate ? 1f : 0f) - (input.brake ? 1f : 0f);
        float normalizedSpeedSquared = Mathf.Pow(speed / topSpeed, 2);
        float accelRamp = Mathf.Lerp(accelCurve * 5f, 1f, normalizedSpeedSquared);
        Vector3 dir = Quaternion.AngleAxis(input.turnInput * steer, transform.up) * transform.forward;
        float mag = accelInput * accel * accelRamp;
        Vector3 vel = velocity + dir * mag * Time.fixedDeltaTime;
        velocity = Vector3.ClampMagnitude(vel, topSpeed);
    }
}
