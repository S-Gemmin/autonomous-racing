using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[System.Serializable]
public struct Sensor
{
    public Transform transform;
    public float rayDst;
    public float hitValDst;
}

public enum AgentMode
{
    Training,
    Inferencing
}

public class KartAgent : Agent, IInput
{
    #region Agent Config

    [Header("Agent Config")]
    public AgentMode agentMode = AgentMode.Inferencing;
    public ushort initCheckpointIndex;
    
    #endregion

    #region Sensors

    [Header("Sensors")]
    public LayerMask detectionMask;
    public LayerMask checkpointMask;
    public LayerMask outOfBoundsMask;
    public LayerMask trackMask;
    public Sensor[] sensors;
    public Collider[] checkpoints;
    public Transform agentSensorTransform;
    public float groundCastDst = 1f;
    
    #endregion

    #region Rewardss

    private const float HIT_PENALTY = -1f;
    private const float PASS_CHECKPOINT_REWARD = 1f;
    private const float TOWARDS_CHECKPOINT_REWARD = 0.03f;
    private const float SPEED_REWARD = 0.02f;
    
    #endregion

    #region Private Fields

    private Kart kart;
    private bool accel;
    private bool brake;
    private float steering;
    private int checkpointIndex;
    private bool endEpisode;
    private float lastReward;

    #endregion

    private void Awake()
    {
        kart = GetComponent<Kart>();
    }

    private void Start()
    {
        OnEpisodeBegin();

        if (agentMode == AgentMode.Inferencing)
        {
            checkpointIndex = initCheckpointIndex;
        }
    }

    private void Update()
    {
        if (endEpisode)
        {
            endEpisode = false;
            AddReward(lastReward);
            EndEpisode();
            OnEpisodeBegin();
        }
    }

    private void LateUpdate()
    {
        if (agentMode == AgentMode.Training)
            return;

        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down,
            out RaycastHit hit, groundCastDst, trackMask))
        {
            if (MaskedValue(hit.collider.gameObject, outOfBoundsMask) > 0)
            {
                ResetKartToCheckpoint(checkpoints[checkpointIndex].transform);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        int maskedVal = MaskedValue(other.gameObject, checkpointMask);
        int index = FindCheckpointIndex(other);

        if ((maskedVal > 0 && index > checkpointIndex) || 
            (index == 0 && checkpointIndex == checkpoints.Length - 1))
        {
            AddReward(PASS_CHECKPOINT_REWARD);
            checkpointIndex = index;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(kart.localSpeed);
        sensor.AddObservation(CalculateVelocityAlignment());
        Debug.Log(CalculateVelocityAlignment());

        lastReward = 0f;
        endEpisode = false;

        for (int i = 0; i < sensors.Length; i++)
        {
            bool hit = Physics.Raycast(agentSensorTransform.position, sensors[i].transform.forward,
                out RaycastHit hitInfo, sensors[i].rayDst, detectionMask, QueryTriggerInteraction.Ignore);

            if (hit && hitInfo.distance < sensors[i].hitValDst)
            {
                lastReward += HIT_PENALTY;
                endEpisode = true;
            }

            sensor.AddObservation(hit ? hitInfo.distance : sensors[i].rayDst);
        }

        sensor.AddObservation(accel);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        base.OnActionReceived(actions);

        accel = actions.DiscreteActions[1] >= 1f;
        brake = actions.DiscreteActions[1] < 1f;
        steering = actions.DiscreteActions[0] - 1f;

        AddReward(CalculateVelocityAlignment() * TOWARDS_CHECKPOINT_REWARD);
        AddReward(kart.localSpeed * SPEED_REWARD);
    }

    public override void OnEpisodeBegin()
    {
        if (agentMode == AgentMode.Inferencing)
            return;

        Collider collider = checkpoints[Random.Range(0, checkpoints.Length - 1)];
        ResetKartToCheckpoint(collider.transform);
    }

    private void ResetKartToCheckpoint(Transform checkpoint)
    {
        transform.localRotation = checkpoint.rotation;
        transform.position = checkpoint.position;
        kart.velocity = default;
        accel = brake = false;
        steering = 0f;
    }

    private int MaskedValue(GameObject gameObject, int mask)
    {
        int maskedValue = 1 << gameObject.layer;
        return maskedValue & mask;
    }

    private float CalculateVelocityAlignment()
    {
        Collider nextCollider = checkpoints[(checkpointIndex + 1) % checkpoints.Length];
        Vector3 direction = (nextCollider.transform.position - kart.transform.position).normalized;
        return Vector3.Dot(kart.velocity.normalized, direction);
    }

    private int FindCheckpointIndex(Collider checkPoint)
    {
        for (int i = 0; i < checkpoints.Length; i++)
        {
            if (checkpoints[i].GetInstanceID() == checkPoint.GetInstanceID())
            {
                return i;
            }
        }

        return -1;
    }

    public InputData GenerateInput()
    {
        return new InputData
        {
            accelerate = accel,
            brake = brake,
            turnInput = steering
        };
    }
}
