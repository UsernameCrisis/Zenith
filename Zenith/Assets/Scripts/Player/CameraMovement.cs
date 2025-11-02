using System.Collections.Generic;
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

    private List<GameObject> characters = new();
    private RaycastHit[] hits = new RaycastHit[5];
    private HashSet<ObjectFader> prevFaded = new();
    private ObjectFader fader;

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

    void Start()
    {
        RefreshCharacterList();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(player.transform.position.x + xOffset, player.transform.position.y + yOffset, player.transform.position.z + zOffset);
        MakeTransparent();
    }
    
    private void MakeTransparent()
    {
        Vector3 cameraPos = transform.position;

        HashSet<ObjectFader> shouldFade = new();

        foreach (GameObject character in characters)
        {
            if (character == null) continue;

            Vector3 direction = character.transform.position - cameraPos;
            float distanceToCharacter = direction.magnitude;
            Ray ray = new(cameraPos, direction);

            Debug.DrawRay(cameraPos, direction.normalized * 100f, Color.green, 0.05f);
            int hitCount = Physics.RaycastNonAlloc(ray, hits, distanceToCharacter);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null) continue;

                Debug.DrawLine(cameraPos, hit.point, Color.red, 0.05f);

                if (hit.collider.gameObject == character)
                {
                    if (fader != null)
                    {
                        fader.DoFade = false;
                        print("character is visible");
                    }

                }
                else
                {
                    fader = hit.collider.gameObject.GetComponent<ObjectFader>();
                    if (fader != null)
                    {
                        shouldFade.Add(fader);
                    }
                }
            }
            for (int i = 0; i < hitCount; i++) hits[i] = default; // Gak harus pakai ini, cuma reset saja
        }

        foreach (var f in shouldFade)
            if (!f.DoFade)
                f.DoFade = true;

        foreach (var f in prevFaded)
            if (!shouldFade.Contains(f))
                if (f != null && f.DoFade)
                    f.DoFade = false;

        prevFaded = shouldFade;
    }
    
    private void RefreshCharacterList()
    {
        characters.Clear();
        characters.AddRange(GameObject.FindGameObjectsWithTag("Character"));
    }
}
