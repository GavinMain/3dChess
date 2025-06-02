using System;
using UnityEngine;

///Code from Unity Nav 2 SLAM Project

/// <summary>
///     A simple free camera to be added to a Unity game object.
///     Keys:
///     wasd / arrows	- movement
///     q/e 			- down/up (local space)
///     r/f 			- up/down (world space)
///     pageup/pagedown	- up/down (world space)
///     hold shift		- enable fast movement mode
///     right mouse  	- enable free look
///     mouse			- free look / rotation
/// </summary>

public class CameraMovement : MonoBehaviour 
{
    /// <summary>
    ///     Normal speed of camera movement.
    /// </summary>
    [SerializeField]
    float m_MovementSpeed = 25f;

    /// <summary>
    ///     Speed of camera movement when shift is held down,
    /// </summary>
    [SerializeField]
    float m_FastMovementSpeed = 100f;

    /// <summary>
    ///     Sensitivity for free look.
    /// </summary>
    [SerializeField]
    float m_FreeLookSensitivity = 3f;

    /// <summary>
    ///     Amount to zoom the camera when using the mouse wheel.
    /// </summary>
    [SerializeField]
    float m_ZoomSensitivity = 25f;

    /// <summary>
    ///     Amount to zoom the camera when using the mouse wheel (fast mode).
    /// </summary>
    [SerializeField]
    float m_FastZoomSensitivity = 50f;

    /// <summary>
    ///     Set to true when free looking (on right mouse button).
    /// </summary>
    bool m_Looking;

    void Update()
    {
        var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var movementSpeed = fastMode ? m_FastMovementSpeed : m_MovementSpeed;

        if (Input.GetKey(KeyCode.A))
            transform.position = transform.position + -transform.right * movementSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.D))
            transform.position = transform.position + transform.right * movementSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.W))
            transform.position = transform.position + transform.forward * movementSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.S))
            transform.position = transform.position + -transform.forward * movementSpeed * Time.deltaTime;

        if (m_Looking)
        {
            var newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * m_FreeLookSensitivity;
            var newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * m_FreeLookSensitivity;
            transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
            StartLooking();
        else if (Input.GetKeyUp(KeyCode.Mouse1)) StopLooking();
    }

    void OnDisable()
    {
        StopLooking();
    }

    /// <summary>
    ///     Enable free looking.
    /// </summary>
    void StartLooking()
    {
        m_Looking = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    ///     Disable free looking.
    /// </summary>
    void StopLooking()
    {
        m_Looking = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}