using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSystem : MonoBehaviour, ITurnActor
{
    [SerializeField] GameObject mouseIndicator, cellIndicator;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private CombatAgent combatAgent;
    [SerializeField] private CombatExecutor combatExecutor;
    [SerializeField] private Grid grid;
    [SerializeField] private GameObject gridVisualization;
    [SerializeField] AudioSource source; // gunakan untuk suara
    [SerializeField] private MovementPreview movePreview;
    [SerializeField] private CharacterActionMenu actionMenu;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private TutorialMenu tutorialMenu;
    [SerializeField] private AudioSettingsUI optionMenu;
    [SerializeField] private CombatCameraMovement cameraMovement;
    [SerializeField] private Animator animator;
    [SerializeField] private bool isForHeuristicAgent = false;

    private Vector3 mousePos;
    private GridData objectsData;
    private Renderer cellIndicatorRenderer;
    private GameObject selectedChar;
    private Color defaultColor;
    private bool isInActionMode = false;
    private string currentAction = null;
    private CharacterObject currentTurnUnit;
    private TurnManager turnManager;
    private bool isTurnComplete = false;
    private bool isPaused = false, isInsideOption = false, isInsideTutorial = false;
    public bool IsTurnComplete() => isTurnComplete;
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
        tutorialMenu.OnButtonSelected += HandleTutorialMenu;
        if (isForHeuristicAgent)
        {
            combatAgent.OnTurnEnded += HandleAgentTurnEnded;
        }
    }

    void OnDisable()
    {
        // inputManager.OnHoverEnter -= ShowHover;
        // inputManager.OnHoverExit -= HideHover;
        inputManager.OnExit -= HandleEscapePressed;
        actionMenu.OnActionSelected -= HandleActionMenu;
        pauseMenu.OnButtonSelected -= HandlePauseMenu;
        optionMenu.OnButtonSelected -= HandleOptionMenu;
        tutorialMenu.OnButtonSelected -= HandleTutorialMenu;
        if (isForHeuristicAgent)
        {
            combatAgent.OnTurnEnded -= HandleAgentTurnEnded;
        }
    }
    
    public void BeginTurn(GridData gridData)
    {
        objectsData = gridData;
        isTurnComplete = false;
        print("inside select");
        inputManager.OnColliderClicked += ColliderClicked;
    }

    public void BeginTurn(GridData gridData, CharacterObject unit)
    {
        objectsData = gridData;
        currentTurnUnit = unit;
        isTurnComplete = false;
        print("inside select agent");
        inputManager.OnColliderClicked += ColliderClicked;
    }

    void Start()
    {
        turnManager = GetComponentInParent<TurnManager>();
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
        if (isForHeuristicAgent)
        {
            // agak janky ini code
            if (inputManager.getSelectMode() == true)
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
        if (isMoving) return;

        Vector3Int clickedGrid = grid.WorldToCell(mousePos);
        Vector3Int startPos = grid.WorldToCell(selectedChar.transform.position);
        CharacterObject charObj = objectsData.GetTileAt(startPos)?.PlacedObject as CharacterObject;


        switch (currentAction)
        {
            case "Move":
                if (collider.CompareTag("Grid") && movePreview.IsTileReachable(clickedGrid))
                {
                    if (isForHeuristicAgent)
                    {
                        combatAgent.SetManualAction(0, clickedGrid);
                    }
                        
                    else
                        combatExecutor.ExecuteMove(charObj, startPos, clickedGrid, objectsData);
                    EndAction();
                }
                    
                break;

            case "Attack":
                if (collider.CompareTag("Enemy") && movePreview.IsTileAttackable(clickedGrid) && charObj.canStillAttack())
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
                movePreview.ShowAttackableTiles(startPos, charObj.AtkRange);
            gridVisualization.SetActive(true);
            cellIndicator.SetActive(true);
            Debug.Log("Attack mode enabled.");
        }
        else if (action == "EndTurn")
        {
            if (isForHeuristicAgent)
                combatAgent.SetManualAction(2, startPos);
            else
            {
                EndTurn();
            }
                
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
        else if (button == "Tutorial")
        {
            pauseMenu.Hide();
            tutorialMenu.Show();
            isInsideTutorial = true;
        }
        else if (button == "Exit")
        {
            Time.timeScale = 1;
            SceneManager.LoadScene("Main Menu");
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

    private void HandleTutorialMenu(string button)
    {
        if (button == "Back")
        {
            tutorialMenu.Back();
            isInsideTutorial = false;
        }
    }

    private void HandleEscapePressed()
    {
        if (isInsideTutorial)
        {
            tutorialMenu.Back();
            isInsideTutorial = false;
            return;
        }

        if (isInsideOption)
        {
            optionMenu.Back();
            isInsideOption = false;
            return;
        }

        if (isPaused)
        {
            Resume();
            return;
        }

        if (selectedChar != null)
        {
            ExitCharacter();
            inputManager.OnColliderClicked += ColliderClicked;
            return;
        }

        OpenPauseMenu();
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

    private void SelectCharacter(Collider collider)
    {
        selectedChar = collider.transform.parent.gameObject;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(selectedChar.transform.position);
        actionMenu.Show(screenPos);

        cameraMovement.FocusOnCharacter(selectedChar.transform); 

        inputManager.SetSelectMode(false);
    }

    public void ExitCharacter()
    {
        gridVisualization.SetActive(false);
        cellIndicator.SetActive(false);
        movePreview.ClearAll();
        actionMenu.Hide();

        selectedChar = null;
        isInActionMode = false;
        currentAction = null;

        inputManager.SetSelectMode(true);
        inputManager.OnColliderClicked -= ColliderClicked;

        if (cellIndicatorRenderer != null)
            cellIndicatorRenderer.material.color = defaultColor;
    }

    private void HandleAgentTurnEnded()
    {
        ExitCharacter();
        isTurnComplete = true;
    }
    public void EndTurn()
    {
        Vector3Int currentPos = grid.WorldToCell(selectedChar.transform.position);
        CharacterObject charObj = objectsData.GetTileAt(currentPos)?.PlacedObject as CharacterObject;
        charObj.ResetMovement();
        charObj.EnableAttack();
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

    public void ForceComplete()
    {
        isTurnComplete = true;
        ExitCharacter();
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
