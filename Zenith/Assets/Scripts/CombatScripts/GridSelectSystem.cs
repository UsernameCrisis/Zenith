using System;
using UnityEngine;

public class GridSelectSystem : MonoBehaviour
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

    private Vector3 mousePos;
    private GridData objectsData;
    private Renderer cellIndicatorRenderer;
    private GameObject selectedChar;
    private Color defaultColor;
    private bool isInActionMode = false;
    private string currentAction = null;
    

    void OnEnable()
    {
        // inputManager.OnHoverEnter += ShowHover;
        // inputManager.OnHoverExit += HideHover;
        inputManager.OnColliderClicked += ColliderClicked;
        inputManager.OnExit += HandleEscapePressed;
        actionMenu.OnActionSelected += HandleMenuAction;
    }

    void OnDisable()
    {
        // inputManager.OnHoverEnter -= ShowHover;
        // inputManager.OnHoverExit -= HideHover;
        inputManager.OnColliderClicked -= ColliderClicked;
        inputManager.OnExit -= HandleEscapePressed;
        actionMenu.OnActionSelected -= HandleMenuAction;
    }

    void Start()
    {
        // ExitCharacter(); // hanya untuk menghilangkan grid sementara (karena dalam scene view dinyalakan)
        objectsData = populateMap.GetComponent<PopulateMap>().objectsData;
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

    private void HandleMenuAction(string action)
    {
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

    private void HandleEscapePressed()
    {
        if (selectedChar != null)
        {
            // If a character is currently selected, just exit selection mode
            ExitCharacter();
        }
        else
        {
            // Otherwise, open pause menu
            OpenPauseMenu();
        }
    }
    
    private void OpenPauseMenu()
    {
        // You can call your UI or game manager here
        Debug.Log("Pause Menu Opened");
        // Example if you have a PauseMenuManager:
        // PauseMenuManager.Instance.TogglePause();
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

        int distanceMoved = Mathf.Abs(targetPos.x - currentPos.x) + Mathf.Abs(targetPos.y - currentPos.y);

        if (distanceMoved > charObj.RemainingMoveRange)
        {
            Debug.Log("Not enough movement points!");
            return;
        }

        if (objectsData.CanPlaceObjectAt(targetPos))
        {
            objectsData.MoveObject(currentPos, targetPos);
            selectedChar.transform.position = grid.CellToWorld(targetPos);
            charObj.UseMovement(distanceMoved);

        }
        movePreview.ClearAll();
        EndAction();
    }

    private void SelectCharacter(Collider collider)
    {
        selectedChar = collider.transform.parent.gameObject;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(selectedChar.transform.position);
        actionMenu.Show(screenPos);

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
    private void EndTurn()
    {
        Vector3Int currentPos = grid.WorldToCell(selectedChar.transform.position);
        CharacterObject charObj = objectsData.GetTileAt(currentPos)?.PlacedObject as CharacterObject;
        charObj.ResetMovement();
        charObj.EnableAttack();
        ExitCharacter();
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
