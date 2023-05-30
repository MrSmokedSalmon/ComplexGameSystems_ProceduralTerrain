using System;
using UnityEngine;
using UnityEngine.LowLevel;

public class HelicopterController : MonoBehaviour
{
    [SerializeField] private float throttleMax;
    [SerializeField] private float throttleRate;
    [SerializeField] private float throttle;
    
    [SerializeField] private Transform rotorMain;
    
    [SerializeField] private float rotorTiltMax;
    [SerializeField] private float rotorTiltSpeed;
    [SerializeField] private AnimationCurve rotorRotationCurve;
    
    private float rotorTiltX;
    private float rotorTiltZ;
    
    [SerializeField] private Rigidbody rigidbody;

    private void Update()
    {
        if (Input.GetKey(KeyCode.Space))
            throttle += throttleRate * (Input.GetKey(KeyCode.LeftControl) ? 3 : 1) * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftShift))
            throttle -= throttleRate * (Input.GetKey(KeyCode.LeftControl) ? 3 : 1) * Time.deltaTime;

        if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
        {
            if (Mathf.Abs(rotorTiltX) < 0.005f)
                rotorTiltX = 0f;
            else
                rotorTiltX -= rotorTiltSpeed * Mathf.Sign(rotorTiltX) * Time.deltaTime;
        }
        else
        {
            if (Input.GetKey(KeyCode.A))
            {
                rotorTiltX += rotorTiltSpeed * Time.deltaTime;

                if (rotorTiltX > 1)
                    rotorTiltX = 1;
            }
            if (Input.GetKey(KeyCode.D))
            {
                rotorTiltX -= rotorTiltSpeed * Time.deltaTime;

                if (rotorTiltX < -1)
                    rotorTiltX = -1;
            }
        }
        if (!Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.W))
        {
            if (Mathf.Abs(rotorTiltZ) < 0.005f)
                rotorTiltZ = 0f;
            else
                rotorTiltZ -= rotorTiltSpeed * Mathf.Sign(rotorTiltZ) * Time.deltaTime;
        }
        else
        {
            if (Input.GetKey(KeyCode.S))
            {
                rotorTiltZ += rotorTiltSpeed * Time.deltaTime;

                if (rotorTiltZ > 1)
                    rotorTiltZ = 1;
            }
            if (Input.GetKey(KeyCode.W))
            {
                rotorTiltZ -= rotorTiltSpeed * Time.deltaTime;

                if (rotorTiltZ < -1)
                    rotorTiltZ = -1;
            }
        }
    }

    // https://www.youtube.com/watch?v=WzNDI7g6jA4
    
    private void FixedUpdate()
    {
        float rotorX = rotorRotationCurve.Evaluate(rotorTiltX);
        float rotorZ = rotorRotationCurve.Evaluate(rotorTiltZ);
        rotorMain.localRotation = Quaternion.Euler(rotorX, 0, rotorZ);
        
        Vector3 forceDirection = rotorMain.up;
        rigidbody.AddForce(forceDirection * throttle, ForceMode.Impulse);
        
        rigidbody.AddTorque(transform.right * rotorX);
        rigidbody.AddTorque(transform.forward * rotorZ);
    }
}