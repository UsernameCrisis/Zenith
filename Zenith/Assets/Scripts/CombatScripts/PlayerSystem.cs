using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSystem : MonoBehaviour, ITurnActor
{
    [SerializeField] GameObject mouseIndicator, cellIndicator;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Grid grid;
    [SerializeField] private ObjectDatabaseSO database;
    [SerializeField] private GameObject gridVisualization;
    [SerializeField] AudioSource source; // gunakan untuk suara
    [SerializeField] private GameObject populateMap;
    [SerializeField] private MovementPreview movePreview;
    [SerializeField] private CharacterActionMenu actionMenu;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private AudioSettingsUI optionMenu;
    [SerializeField] private CombatCameraMovement cameraMovement;

    private Vector3 mousePos;
    private GridData objectsData;
    private Renderer cellIndicatorRenderer;
    private GameObject selectedChar;
    private Color defaultColor;
    private bool isInActionMode = false;
    private string currentAction = null;
    private bool isPaused = false, isInsideOption = false;

    public bool IsPlayer => true;
    private bool isMoving = false;

    void OnEnable()
    {
        // inputManager.OnHoverEnter += ShowHover;
        // inputManager.OnHoverExit += HideHover;
        inputManager.OnExit += HandleEscapePressed;
        actionMenu.OnActionSelected += HandleActionMenu;
        pauseMenu.OnButtonSelected += HandlePauseMenu;
        optionMenu.OnButtonSelected += HandleOptionMenu;
    }

    void OnDisable()
    {
        // inputManager.OnHoverEnter -= ShowHover;
        // inputManager.OnHoverExit -= HideHover;
        inputManager.OnExit -= HandleEscapePressed;
        actionMenu.OnActionSelected -= HandleActionMenu;
        pauseMenu.OnButtonSelected -= HandlePauseMenu;
        optionMenu.OnButtonSelected -= HandleOptionMenu;
    }
    
    public void BeginTurn(Vector3Int pos, GridData gridData)
    {
        objectsData = gridData;
        print("inside select");
        inputManager.OnColliderClicked += ColliderClicked;
    }

    void Start()
    {
        // ExitCharacter(); // hanya untuk menghilangkan grid sementara (karena dalam scene view dinyalakan)
        // objectsData = populateMap.GetComponent<PopulateMap>().objectsData;
        cellIndicatorRenderer = cellIndicator.GetComponentInChildren<Renderer>();
        defaultColor = cellIndicatorRenderer.material.color;
    }

    void Update()
    {
        mousePos = inputManager.GetHoveredMapPosition();
        mouseIndicator.transform.position = mousePos;

        if (gridVisualization.activeSelf)
            VisualizeHoveredGrid();

        if (selectedChar != null && gridVisualization.activeSelf)
        {
            Vector3Int hoverTile = grid.WorldToCell(mousePos);
            bool inRange = movePreview.IsTileReachable(hoverTile);
            movePreview.ShowPathPreview(hoverTile);

            Color color = inRange ? defaultColor : Color.red;
            cellIndicatorRenderer.material.SetColor("_EmissionColor", color);
        }
    }

    private void VisualizeHoveredGrid()
    {
        // Grid pos (x,y,0), world pos (x,0,z) y == z
        Vector3Int gridPosition = grid.WorldToCell(mousePos);
        cellIndicator.transform.position = grid.CellToWorld(gridPosition);
    }

    private void ColliderClicked(Collider collider)
    {
        if (collider.CompareTag("Player"))
        {
            SelectCharacter(collider);
            return;
        }

        if (isInActionMode)
            HandleActionClick(collider);
    }

    private void HandleActionClick(Collider collider)
    {
        if (selectedChar == null || currentAction == null) return;
        if (isMoving) return;

        Vector3Int clickedGrid = grid.WorldToCell(mousePos);
        Vector3Int startPos = grid.WorldToCell(selectedChar.transform.position);
        CharacterObject charObj = objectsData.GetTileAt(startPos)?.PlacedObject as CharacterObject;


        switch (currentAction)
        {
            case "Move":
                if (collider.CompareTag("Grid") && movePreview.IsTileReachable(clickedGrid))
                    MoveCharacter(clickedGrid);
                break;

            case "Attack":
                if (collider.CompareTag("Enemy") && movePreview.IsTileAttackable(clickedGrid) && charObj.canStillAttack())
                    HandleAttack(clickedGrid);
                else
                    Debug.Log("Enemy out of range!");
                break;

            default:
                Debug.Log($"Unhandled action: {currentAction}");
                break;
        }
    }

    private void HandleActionMenu(string action)
    {
        if (isMoving) return;
        currentAction = action;
        isInActionMode = true;
        Vector3Int startPos = grid.WorldToCell(selectedChar.transform.position);
        CharacterObject charObj = objectsData.GetTileAt(startPos)?.PlacedObject as CharacterObject;
        if (action == "Move")
        {
            movePreview.ShowMovementRange(startPos, charObj.RemainingMoveRange);
            gridVisualization.SetActive(true);
            cellIndicator.SetActive(true);
        }
        else if (action == "Attack")
        {
            if (charObj.canStillAttack())
                movePreview.ShowAttackableEnemies(startPos, charObj.AtkRange);
            gridVisualization.SetActive(true);
            cellIndicator.SetActive(true);
            Debug.Log("Attack mode enabled.");
        }
        else if (action == "EndTurn")
        {
            EndTurn();
        }
        actionMenu.Hide();
    }

    private void HandlePauseMenu(string button)
    {
        if (button == "Resume")
        {
            Resume();
        }
        else if (button == "Settings")
        {
            pauseMenu.Hide();
            optionMenu.Show();
            isInsideOption = true;

        }
        else if (button == "Exit")
        {
            print("Exit pressed");
        }
    }

    private void HandleOptionMenu(string button)
    {
        if (button == "Back")
        {
            optionMenu.Back();
            isInsideOption = false;
        }
    }

    private void HandleEscapePressed()
    {
        if (isMoving)
        {
            if (!isPaused)
                OpenPauseMenu();
            else
                Resume();

            return;
        }

        if (isPaused && !isInsideOption)
        {
            Resume();
        }
        else if (isInsideOption)
        {
            optionMenu.Back();
            isInsideOption = false;
        }
        else if (selectedChar != null)
        {
            ExitCharacter();
        }
        else
        {
            OpenPauseMenu();
        }
    }
    
    private void Resume()
    {
        Time.timeScale = 1;
        isPaused = false;
        pauseMenu.Hide();
    }
    
    private void OpenPauseMenu()
    {
        pauseMenu.Show();
        isPaused = true;
        Time.timeScale = 0;
    }
    
    private void HandleAttack(Vector3Int targetPos)
    {
        if (objectsData == null) return;

        Vector3Int attackerPos = grid.WorldToCell(selectedChar.transform.position);
        CharacterObject charObj = objectsData.GetTileAt(attackerPos)?.PlacedObject as CharacterObject;

        if (attackerPos == targetPos)
            return; // cannot attack self

        objectsData.AttackObject(attackerPos, targetPos);
        charObj.DisableAttack();
        movePreview.ClearAll();
        EndAction();
    }

    private void MoveCharacter(Vector3Int targetPos)
    {
        Vector3Int currentPos = grid.WorldToCell(selectedChar.transform.position);
        CharacterObject charObj = objectsData.GetTileAt(currentPos)?.PlacedObject as CharacterObject;
        
        if (charObj == null)
            return;

        List<Vector3Int> path = movePreview.FindPathAStar(currentPos, targetPos);
        int distanceMoved = path.Count;

        if (distanceMoved > charObj.RemainingMoveRange)
        {
            Debug.Log("Not enough movement points!");
            return;
        }

        if (objectsData.CanPlaceObjectAt(targetPos))
        {
            // objectsData.MoveObject(currentPos, targetPos);
            StartCoroutine(WalkPath(path, currentPos, charObj, selectedChar.transform));
            // charObj.UseMovement(distanceMoved);
        }
        movePreview.ClearAll();
        EndAction();
    }

    private IEnumerator WalkPath(List<Vector3Int> path, Vector3Int currPos, 
                                CharacterObject charObj, Transform charTransform)
    {
        isMoving = true;
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 start = charTransform.position;
            Vector3 end = grid.CellToWorld(path[i]);
            
            float t = 0f;
            float speed = 2f;
    
            while (t < 1f)
            {
                if (charTransform == null)
                    yield break;

                t += Time.deltaTime * speed;
                charTransform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }
        }
        Vector3Int finalPos = path[path.Count - 1];
        objectsData.MoveObject(currPos, finalPos);
    
        int distanceMoved = path.Count;
        charObj.UseMovement(distanceMoved);
        isMoving = false;
    
        movePreview.ClearAll();
        // EndAction();
    }

    private void SelectCharacter(Collider collider)
    {
        selectedChar = collider.transform.parent.gameObject;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(selectedChar.transform.position);
        actionMenu.Show(screenPos);

        cameraMovement.FocusOnCharacter(selectedChar.transform); 

        inputManager.SetSelectMode(false);

    }

    private void ExitCharacter()
    {
        gridVisualization.SetActive(false);
        cellIndicator.SetActive(false);
        movePreview.ClearAll();
        actionMenu.Hide();

        selectedChar = null;
        isInActionMode = false;
        currentAction = null;

        inputManager.SetSelectMode(true);

        if (cellIndicatorRenderer != null)
            cellIndicatorRenderer.material.color = defaultColor;
    }
    public void EndTurn()
    {
        Vector3Int currentPos = grid.WorldToCell(selectedChar.transform.position);
        CharacterObject charObj = objectsData.GetTileAt(currentPos)?.PlacedObject as CharacterObject;
        charObj.ResetMovement();
        charObj.EnableAttack();
        ExitCharacter();
        inputManager.OnColliderClicked -= ColliderClicked;
        TurnManager.Instance.EndTurn();
    }

    private void EndAction()
    {
        isInActionMode = false;
        currentAction = null;
        
        gridVisualization.SetActive(false);
        cellIndicator.SetActive(false);

        Vector3 screenPos = Camera.main.WorldToScreenPoint(selectedChar.transform.position);
        actionMenu.Show(screenPos);
    }

    private void HideHover(Collider collider)
    {
        throw new NotImplementedException();
    }

    private void ShowHover(Collider collider)
    {
        throw new NotImplementedException();
    }
}
