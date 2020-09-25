using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public PlayerManager player;
    public float sensitivity = 100f;
    public float clampAngle = 85f;

    private float verticalRotation;
    private float horizontalRotation;

    private void Start()
    {
        verticalRotation = transform.localEulerAngles.x;
        horizontalRotation = player.transform.localEulerAngles.y;
    }

    private void Update()
    {
        Look();
        Debug.DrawRay( transform.position, transform.forward * 2, Color.red );
    }

    private void Look()
    {
        float _movementVertical = Input.GetAxis( "Mouse Y" );
        float _movementHorizontal = Input.GetAxis( "Mouse X" );

        verticalRotation += _movementVertical * sensitivity * Time.deltaTime;
        horizontalRotation += _movementHorizontal * sensitivity * Time.deltaTime;

        transform.localRotation = Quaternion.Euler( verticalRotation, 0f, 0f );
        player.transform.rotation = Quaternion.Euler( 0f, horizontalRotation, 0f );
    }
}
