using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class CombatCameraMovement : MonoBehaviour
{
    private InputAction cameraMoveAction;
    private InputAction shiftInput;
    private Vector3 moveValue;
    private float isFast;
    private float moveSpeed;
    private float minX = -5f, maxX = 5f, minZ= -10f, maxZ= 0f;
    private Rigidbody rb;
    private ObjectFader fader;
    private List<GameObject> characters = new();
    private RaycastHit[] hits = new RaycastHit[5];
    private HashSet<ObjectFader> prevFaded = new();

    [SerializeField] private float defaultMoveSpeed = 70f;
    [SerializeField] private float speedMultiplier = 2f;
    [SerializeField] private float defaultX = 0f, defaultY = 3.251f, defaultZ = -5f;
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

        Vector3 movement = new Vector3(moveValue.x, 0, moveValue.y) * moveSpeed * Time.deltaTime;
        transform.position += movement;
    
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        transform.position = pos;
        MakeTransparent();
    }

    void FixedUpdate()
    {
        // rb.AddForce(new Vector3(moveValue.x * moveSpeed, 0, moveValue.y * moveSpeed));
        // ClampPosition();
    }

    private void ClampPosition()
    {
        Vector3 pos = rb.position;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);

        rb.position = pos;
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

    public void FocusOnCharacter(Transform target)
    {
        if (target == null) return;

        Vector3 targetPos = target.position + new Vector3(defaultX+0.5f, defaultY, defaultZ+0.5f);

        transform.DOMove(targetPos, 0.7f)
            .SetEase(Ease.OutQuint);
    }

    private void RefreshCharacterList()
    {
        characters.Clear();
        characters.AddRange(GameObject.FindGameObjectsWithTag("Character"));
    }
}
