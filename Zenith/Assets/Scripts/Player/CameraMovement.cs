using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    private float yPosOffset;
    [SerializeField] private GameObject player;

    [Header("Offset")]
    [SerializeField] private float xOffset = 0f;
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private float zOffset = 0f;

    [Header("Rotation")]
    [SerializeField] private float xRotation = 0f;
    [SerializeField] private float yRotation = 0f;
    [SerializeField] private float zRotation = 0f;

    void Awake()
    {
        /*
        IMPORTANT!!!

        The camera Y position and offset is called once because of a bug if it is called every frame.
        The bug causes the camera to be in a state of falling and snapping back to the offset position.
        This will cause problems in the future, if height difference for the floor is added.
        */
        yPosOffset = player.transform.position.y + yOffset;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(player.transform.position.x + xOffset, player.transform.position.y + yOffset,  player.transform.position.z + zOffset);
    }
}
