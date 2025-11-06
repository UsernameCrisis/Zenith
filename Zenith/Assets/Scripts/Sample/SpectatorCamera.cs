using System.Numerics;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorCamera : MonoBehaviour
{
    public Camera camera;

    void Update()
    {
        if (camera == null) return;

        UnityEngine.Vector2 input = InputSystem.actions.FindAction("move").ReadValue<UnityEngine.Vector2>() / 12f;
        float vertical = InputSystem.actions.FindAction("MoveVertical").ReadValue<float>();

        UnityEngine.Vector3 camPos = camera.transform.position;
        camPos.x += input.x;
        camPos.z += input.y;

        camPos.y += vertical / 15f;

        camera.transform.position = camPos;
    }
}
