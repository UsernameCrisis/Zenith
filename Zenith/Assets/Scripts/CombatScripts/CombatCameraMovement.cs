using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CombatCameraMovement : MonoBehaviour
{
    private InputAction cameraMoveAction;
    private InputAction shiftInput;
    private Vector3 moveValue;
    private float isFast;
    private float moveSpeed;
    private Rigidbody rb;
    private ObjectFader fader;
    private List<GameObject> characters = new();
    private RaycastHit[] hits = new RaycastHit[5];
    private HashSet<ObjectFader> prevFaded = new();

    [SerializeField] private float defaultMoveSpeed = 70f;
    [SerializeField] private float speedMultiplier = 2f;
    [SerializeField] private LayerMask obstacleLayerMask;
    void Awake()
    {
        moveSpeed = defaultMoveSpeed;
        cameraMoveAction = InputSystem.actions.FindAction("Move");
        shiftInput = InputSystem.actions.FindAction("Sprint"); // action "Sprint" karena pakai default InputSystem
        rb = gameObject.GetComponent<Rigidbody>();
    }

    // Ini hanya berlaku jika menggunakan populate map secara synchronous (load di start function)
    // Jika menggunakan asynchronoous (jika mau loading screen ada load barnya), maka tidak bisa menggunakan function start ini
    // Perlu call RefreshCharacterList() setelah menunggu population selesai dalam PopulateMap script yang menggunakan async agar bisa smooth loading
    IEnumerator Start()
    {
        yield return null; // Menggunakan ini untuk skip 1 frame karena belum selesai populasi map
        RefreshCharacterList();
    }

    void Update()
    {
        moveValue = cameraMoveAction.ReadValue<Vector2>();
        isFast = shiftInput.ReadValue<float>();

        if (isFast == 1)
            moveSpeed = defaultMoveSpeed * speedMultiplier;
        else
            moveSpeed = defaultMoveSpeed;

        MakeTransparent();
    }

    void FixedUpdate()
    {
        rb.AddForce(new Vector3(moveValue.x * moveSpeed, 0, moveValue.y * moveSpeed));
    }

    private void MakeTransparent()
    {
        Vector3 cameraPos = transform.position;

        HashSet<ObjectFader> shouldFade = new();

        foreach (GameObject character in characters)
        {
            if (character == null) continue;

            Vector3 direction = character.transform.position - cameraPos;
            Ray ray = new(cameraPos, direction);
            int hitCount = Physics.RaycastNonAlloc(ray, hits, 100f);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null) continue;

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
