using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BasicPlaneMovement : MonoBehaviour {

    [SerializeField] float pitchSensitivity = 1;
    [SerializeField] float yawSensitivity = 0.3f;
    [SerializeField] float rollSensitivity = 2;
    [SerializeField] float throttleSensitivity = 0.5f;
    [SerializeField] float maxVelocity = 40;

    Rigidbody playerRigidbody;
    float currentSpeed = 0;

    void Start() {
        playerRigidbody = GetComponent<Rigidbody>();
        playerRigidbody.useGravity = false;
    }

    void FixedUpdate() {
        // pitch
        if (Input.GetKey(KeyCode.W)) {
            transform.Rotate(new Vector3(pitchSensitivity, 0, 0));
        } else if (Input.GetKey(KeyCode.S)) {
            transform.Rotate(new Vector3(-pitchSensitivity, 0, 0));
        }

        // yaw
        if (Input.GetKey(KeyCode.A)) {
            transform.Rotate(new Vector3(0, -yawSensitivity, 0));
        } else if (Input.GetKey(KeyCode.D)) {
            transform.Rotate(new Vector3(0, yawSensitivity, 0));
        }

        // roll
        if (Input.GetKey(KeyCode.Q)) {
            transform.Rotate(new Vector3(0, 0, rollSensitivity));
        } else if (Input.GetKey(KeyCode.E)) {
            transform.Rotate(new Vector3(0, 0, -rollSensitivity));
        }

        // throttle
        if (Input.GetKey(KeyCode.LeftShift)) {
            currentSpeed += throttleSensitivity;
            if (currentSpeed > maxVelocity) currentSpeed = maxVelocity;
        } else if (Input.GetKey(KeyCode.LeftControl)) {
            currentSpeed -= throttleSensitivity;
            if (currentSpeed < 0) currentSpeed = 0;
        }        

        // full stop/speed
        if (Input.GetKey(KeyCode.X)) {
            currentSpeed = 0;
        } else if (Input.GetKey(KeyCode.Y)) {
            currentSpeed = maxVelocity;
        }

        playerRigidbody.velocity = transform.forward * currentSpeed;
    }
}
