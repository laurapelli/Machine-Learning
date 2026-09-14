using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;
using System.Collections;

public class Ball3DAgent : Agent
{
    [Header("Specific to Ball3D")]
    public GameObject ball;
    [Tooltip("Whether to use vector observation. This option should be checked " +
        "in 3DBall scene, and unchecked in Visual3DBall scene. ")]
    public bool useVecObs;
    Rigidbody m_BallRb;
    EnvironmentParameters m_ResetParams;

    public float headBounceHeight = 300f;
    public float headBounceSpeed = 300f;
    public float headBounceInterval = 1f;

    Vector3 initialPosition;

    public override void Initialize()
    {
        m_BallRb = ball.GetComponent<Rigidbody>();
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        initialPosition = transform.position;
        SetResetParameters();
    }


    IEnumerator HeadBounceRoutine()
    {
        while (true)
        {
            // Goes up
            float t = 0;
            Vector3 targetPos = initialPosition + Vector3.up * headBounceHeight;
            while (t < 1f)
            {
                t += Time.deltaTime * headBounceSpeed;
                transform.position = Vector3.Lerp(initialPosition, targetPos, t);
                yield return null;
            }

            // Goes down
            t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * headBounceSpeed;
                transform.position = Vector3.Lerp(targetPos, initialPosition, t);
                yield return null;
            }

            yield return new WaitForSeconds(headBounceInterval);
        }
    }

    void FixedUpdate()
    {
        // Increase the gravity
        float extraGravity = -20f; 
        m_BallRb.AddForce(new Vector3(0, extraGravity, 0), ForceMode.Acceleration);

        Vector3 ballPos = ball.transform.position;
        Vector3 myPos = transform.position;
        Vector3 ballVelocity = m_BallRb.linearVelocity;

        float verticalDistance = ballPos.y - myPos.y;

        // If the ball is falling to the head
        if (verticalDistance > 1.0f && verticalDistance < 1.8f &&
            ballVelocity.y < -0.5f &&
            Mathf.Abs(ballPos.x - myPos.x) < 0.5f &&
            Mathf.Abs(ballPos.z - myPos.z) < 0.5f)
        {
            StartCoroutine(HeadBounceRoutine());
        }

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (useVecObs)
        {
            sensor.AddObservation(gameObject.transform.rotation.z);
            sensor.AddObservation(gameObject.transform.rotation.x);
            sensor.AddObservation(ball.transform.position - gameObject.transform.position);
            sensor.AddObservation(m_BallRb.linearVelocity);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var actionZ = 2f * Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
        var actionX = 2f * Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);

        if ((gameObject.transform.rotation.z < 0.25f && actionZ > 0f) ||
            (gameObject.transform.rotation.z > -0.25f && actionZ < 0f))
        {
            gameObject.transform.Rotate(new Vector3(0, 0, 1), actionZ);
        }

        if ((gameObject.transform.rotation.x < 0.25f && actionX > 0f) ||
            (gameObject.transform.rotation.x > -0.25f && actionX < 0f))
        {
            gameObject.transform.Rotate(new Vector3(1, 0, 0), actionX);
        }
        if ((ball.transform.position.y - gameObject.transform.position.y) < -2f ||
            Mathf.Abs(ball.transform.position.x - gameObject.transform.position.x) > 3f ||
            Mathf.Abs(ball.transform.position.z - gameObject.transform.position.z) > 3f)
        {
            SetReward(-1.5f);
            EndEpisode();
        }
        else
        {
            SetReward(0.2f);
        }
    }

    public override void OnEpisodeBegin()
    {
        gameObject.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
        gameObject.transform.Rotate(new Vector3(1, 0, 0), Random.Range(-10f, 10f));
        gameObject.transform.Rotate(new Vector3(0, 0, 1), Random.Range(-10f, 10f));
        m_BallRb.linearVelocity = new Vector3(0f, 0f, 0f);
        ball.transform.position = new Vector3(Random.Range(-1.5f, 1.5f), 4f, Random.Range(-1.5f, 1.5f))
            + gameObject.transform.position;
        //Reset the parameters when the Agent is reset.
        SetResetParameters();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = -Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    public void SetBall()
    {
        //Set the attributes of the ball by fetching the information from the academy
        m_BallRb.mass = m_ResetParams.GetWithDefault("mass", 1.0f);
        var scale = m_ResetParams.GetWithDefault("scale", 1.0f);
        ball.transform.localScale = new Vector3(scale, scale, scale);
    }

    public void SetResetParameters()
    {
        SetBall();
    }


    void OnCollisionEnter(Collision collision)
    {
            // Apply the impulse after the collision
            m_BallRb.AddForce(Vector3.up * 1.5f, ForceMode.Impulse);
        
    }
}
