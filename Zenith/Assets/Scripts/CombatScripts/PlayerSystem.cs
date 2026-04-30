using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSystem : MonoBehaviour, ITurnActor
{
    [Header("References")]
    [SerializeField] GameObject mouseIndicator, cellIndicator;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private CombatAgent combatAgent;
    [SerializeField] private CombatExecutor combatExecutor;
    [SerializeField] private Grid grid;
    [SerializeField] private GameObject gridVisualization;
    [SerializeField] private MovementPreview movePreview;
    [SerializeField] private CharacterActionMenu actionMenu;
    [SerializeField] private CombatCameraMovement cameraMovement;
    [SerializeField] private bool isForHeuristicAgent = false;

    private PlayerCombatMenuController menuController;
    private Renderer cellIndicatorRenderer;
    private Color defaultColor;

    private Vector3 mousePos;
    private GridData objectsData;
    private GameObject selectedChar;
    private CharacterObject currentTurnUnit;
    private bool isInActionMode = false;
    private string currentAction = null;
    private bool isTurnComplete = false;

    public bool IsTurnComplete() => isTurnComplete;
    public bool IsPlayer => true;

    void Awake()
    {
        menuController = GetComponent<PlayerCombatMenuController>();
        menuController.OnRequestDeselectCharacter += HandleDeselectRequest;
    }
    void OnEnable()
    {
        inputManager.OnExit += menuController.HandleEscapePressed;
        if (isForHeuristicAgent)
            combatAgent.OnTurnEnded += HandleAgentTurnEnded;
    }

    void OnDisable()
    {
        inputManager.OnExit -= menuController.HandleEscapePressed;
        if (isForHeuristicAgent)
            combatAgent.OnTurnEnded -= HandleAgentTurnEnded;
    }

    void Start()
    {
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
            movePreview.ShowPathPreview(hoverTile);

            Color color = movePreview.IsTileReachable(hoverTile) ? defaultColor : Color.red;
            cellIndicatorRenderer.material.SetColor("_EmissionColor", color);
        }
    }

    // ITurnActor

    public void BeginTurn(GridData gridData)
    {
        objectsData = gridData;
        isTurnComplete = false;
        inputManager.OnColliderClicked += ColliderClicked;
    }

    public void BeginTurn(GridData gridData, CharacterObject unit)
    {
        objectsData = gridData;
        currentTurnUnit = unit;
        isTurnComplete = false;
        inputManager.OnColliderClicked += ColliderClicked;
    }

    public void EndTurn()
    {
        if (selectedChar == null) return;
        Vector3Int currentPos = grid.WorldToCell(selectedChar.transform.position);
        CharacterObject charObj = objectsData.GetTileAt(currentPos)?.PlacedObject as CharacterObject;
        charObj.ResetMovement();
        charObj.EnableAttack();
        ExitCharacter();
        
        isTurnComplete = true;
    }

    public void ForceComplete()
    {
        isTurnComplete = true;
        ExitCharacter();
    }

    // Input Handling

    private void ColliderClicked(Collider collider)
    {
        if (isForHeuristicAgent)
        {
            // agak janky ini code
            if (inputManager.GetSelectMode())
            {
                GameObject selectedCharTemp = collider.transform.parent.gameObject;
                Vector3Int currentPos = grid.WorldToCell(selectedCharTemp.transform.position);
                CharacterObject charObj = objectsData.GetTileAt(currentPos)?.PlacedObject as CharacterObject;

                if (charObj == currentTurnUnit)
                    SelectCharacter(collider);
                else
                    print("Not this unit's turn!");
            }
        } else
        {
            if (collider.CompareTag("Player"))
            {
                SelectCharacter(collider);
                return;
            }
        }

        if (isInActionMode)
            HandleActionClick(collider);
    }

    private void HandleActionClick(Collider collider)
    {
        if (selectedChar == null || currentAction == null) return;
        if (combatExecutor.IsMoving) return;

        Vector3Int clickedGrid = grid.WorldToCell(mousePos);
        Vector3Int startPos = grid.WorldToCell(selectedChar.transform.position);
        CharacterObject charObj = objectsData.GetTileAt(startPos)?.PlacedObject as CharacterObject;


        switch (currentAction)
        {
            case "Move":
                if (collider.CompareTag("Grid") && movePreview.IsTileReachable(clickedGrid))
                {
                    if (isForHeuristicAgent)
                        combatAgent.SetManualAction(0, clickedGrid);
                    else
                        combatExecutor.ExecuteMove(charObj, startPos, clickedGrid, objectsData);
                    EndAction();
                }
                break;

            case "Attack":
                if (collider.CompareTag("Enemy") && movePreview.IsTileAttackable(clickedGrid) && charObj.CanStillAttack())
                {
                    if (isForHeuristicAgent)
                        combatAgent.SetManualAction(1, clickedGrid);
                    else
                        combatExecutor.ExecuteAttack(charObj, startPos, clickedGrid, objectsData);
                    EndAction();
                }
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
        if (combatExecutor.IsMoving) return;

        currentAction = action;
        isInActionMode = true;

        Vector3Int startPos = grid.WorldToCell(selectedChar.transform.position);
        CharacterObject charObj = objectsData.GetTileAt(startPos)?.PlacedObject as CharacterObject;

        switch (action)
        {
            case "Move":
                movePreview.ShowMovementRange(startPos, charObj.RemainingMoveRange);
                ShowGrid();
                break;

            case "Attack":
                if (charObj.CanStillAttack())
                    movePreview.ShowAttackableTiles(startPos, charObj.AtkRange);
                ShowGrid();
                break;

            case "EndTurn":
                if (isForHeuristicAgent)
                    combatAgent.SetManualAction(2, startPos);
                else
                    EndTurn();
                break;
        }

        actionMenu.Hide();
    }

    // Character selection

    private void SelectCharacter(Collider collider)
    {
        selectedChar = collider.transform.parent.gameObject;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(selectedChar.transform.position);

        actionMenu.OnActionSelected += HandleActionMenu;
        actionMenu.Show(screenPos);

        cameraMovement.FocusOnCharacter(selectedChar.transform); 

        inputManager.SetSelectMode(false);
        menuController.IsCharacterSelected = true;
    }

    public void ExitCharacter()
    {
        gridVisualization.SetActive(false);
        cellIndicator.SetActive(false);
        movePreview.ClearAll();

        actionMenu.OnActionSelected -= HandleActionMenu;
        actionMenu.Hide();

        selectedChar = null;
        isInActionMode = false;
        currentAction = null;

        inputManager.SetSelectMode(true);
        inputManager.OnColliderClicked -= ColliderClicked;

        if (cellIndicatorRenderer != null)
            cellIndicatorRenderer.material.color = defaultColor;
        menuController.IsCharacterSelected = false;
    }

    private void HandleDeselectRequest()
    {
        ExitCharacter();
        inputManager.OnColliderClicked += ColliderClicked;
    }

    private void HandleAgentTurnEnded()
    {
        ExitCharacter();
        isTurnComplete = true;
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

    private void VisualizeHoveredGrid()
    {
        // Grid pos (x,y,0), world pos (x,0,z) y == z
        Vector3Int gridPosition = grid.WorldToCell(mousePos);
        cellIndicator.transform.position = grid.CellToWorld(gridPosition);
    }

    private void ShowGrid()
    {
        gridVisualization.SetActive(true);
        cellIndicator.SetActive(true);
    }
}
