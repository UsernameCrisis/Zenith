using System.Collections;
using UnityEngine;

public class PlayerSystem : MonoBehaviour, ITurnActor
{
    [Header("References")]
    [SerializeField] GameObject mouseIndicator, cellIndicator;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private CombatAgent2 combatAgent;
    [SerializeField] private CombatExecutor combatExecutor;
    [SerializeField] private Grid grid;
    [SerializeField] private GameObject gridVisualization;
    [SerializeField] private MovementPreview movePreview;
    [SerializeField] private CombatCameraMovement cameraMovement;
    [SerializeField] private CharacterInfoUI characterInfoUI;
    [SerializeField] private bool isForHeuristicAgent = false;

    private PlayerCombatMenuController menuController;
    private Renderer cellIndicatorRenderer;
    private Material cellIndicatorMaterial;
    private Color defaultColor;

    private Vector3 mousePos;
    private GridData objectsData;
    private GameObject selectedChar;
    private CharacterObject currentTurnUnit;
    private TurnManager turnManager;
    private bool isInActionMode = false;
    private string currentAction = null;
    private bool isTurnComplete = false;
    private bool IsTryToAttack = false;
    private bool IsTryToMove = false;
    private int controlledTeam = 1;

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
        turnManager = GetComponentInParent<TurnManager>();
        cellIndicatorRenderer = cellIndicator.GetComponentInChildren<Renderer>();
        cellIndicatorMaterial = cellIndicatorRenderer.material;
        defaultColor = cellIndicatorMaterial.color;
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
            cellIndicatorMaterial.SetColor("_EmissionColor", color);
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
        inputManager.OnColliderClicked -= ColliderClicked;
        inputManager.OnColliderClicked += ColliderClicked;
    }
    public void SetControlledTeam(int team) => controlledTeam = team;

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
        if (inputManager.GetSelectMode())
        {
            GameObject candidateGO = collider.transform.parent.gameObject;
            Vector3Int candidatePos = grid.WorldToCell(candidateGO.transform.position);
            CharacterObject candidateChar = objectsData.GetTileAt(candidatePos)?.PlacedObject as CharacterObject;
            if (candidateChar == null) return;

            bool isActiveUnit = candidateChar.Team == controlledTeam
                                && candidateChar == currentTurnUnit;

            if (isActiveUnit)
                SelectCharacter(collider);
            else if (!isForHeuristicAgent)
            {
                if (selectedChar != null)
                {
                    selectedChar = null;
                    movePreview.ClearAll();
                    gridVisualization.SetActive(false);
                    cellIndicator.SetActive(false);
                    menuController.IsCharacterSelected = false;
                }

                GameObject candidateGO2 = objectsData.GetObjectAt(candidatePos);
                if (candidateGO2 != null)
                    cameraMovement.FocusOnCharacter(candidateGO2.transform);

                characterInfoUI.OnActionSelected -= HandleActionMenu;
                characterInfoUI?.Show(candidateChar, isControllable: false);
                menuController.IsCharacterSelected = true;
            } else
                print("Not this unit's turn!");
        }

        if (isInActionMode)
            HandleActionClick(collider);
    }

    private void HandleActionClick(Collider collider)
    {
        if (selectedChar == null || currentAction == null) return;
        if (combatExecutor.IsMoving || combatExecutor.IsAttacking) return;

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
                CharacterObject targetChar = objectsData.GetTileAt(clickedGrid)?.PlacedObject as CharacterObject;
                bool isValidTarget = targetChar != null && targetChar.Team != controlledTeam;
                if (isValidTarget && movePreview.IsTileAttackable(clickedGrid) && charObj.CanStillAttack())
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
                Debug.LogWarning($"Unhandled action: {currentAction}");
                break;
        }
    }

    private void HandleActionMenu(string action)
    {
        if (combatExecutor.IsMoving || combatExecutor.IsAttacking) return;
        inputManager.SetSelectMode(false);
        currentAction = action;
        isInActionMode = true;

        Vector3Int startPos = grid.WorldToCell(selectedChar.transform.position);
        CharacterObject charObj = objectsData.GetTileAt(startPos)?.PlacedObject as CharacterObject;

        switch (action)
        {
            case "Move":
                movePreview.ShowMovementRange(startPos, charObj.RemainingMoveRange);
                IsTryToMove = true;
                ShowGrid();
                characterInfoUI?.HideActionButtons();
                break;

            case "Attack":
                if (charObj.CanStillAttack())
                    movePreview.ShowAttackableTiles(startPos, charObj.AtkRange);
                IsTryToAttack = true;
                ShowGrid();
                characterInfoUI?.HideActionButtons();
                break;

            case "EndTurn":
                if (isForHeuristicAgent)
                    combatAgent.SetManualAction(2, startPos);
                else
                    EndTurn();
                break;
        }
    }

    // Character selection

    private void SelectCharacter(Collider collider)
    {
        selectedChar = collider.transform.parent.gameObject;
        Vector3Int selectedPos = grid.WorldToCell(selectedChar.transform.position);
        CharacterObject selectedCharObj = objectsData.GetTileAt(selectedPos)?.PlacedObject as CharacterObject;

        bool isControllable = selectedCharObj != null
            && selectedCharObj.Team == controlledTeam
            && selectedCharObj == currentTurnUnit;

        if (characterInfoUI != null && selectedCharObj != null)
        {
            characterInfoUI.OnActionSelected -= HandleActionMenu;
            characterInfoUI.OnActionSelected += HandleActionMenu;
            characterInfoUI.Show(selectedCharObj, isControllable);
        }

        cameraMovement.FocusOnCharacter(selectedChar.transform); 

        menuController.IsCharacterSelected = true;
    }

    public void ExitCharacter()
    {
        gridVisualization.SetActive(false);
        cellIndicator.SetActive(false);
        movePreview.ClearAll();

        if (characterInfoUI != null)
        {
            characterInfoUI.OnActionSelected -= HandleActionMenu;
            characterInfoUI.Hide();
        }

        selectedChar = null;
        isInActionMode = false;
        IsTryToMove = false;
        IsTryToAttack = false;
        currentAction = null;

        inputManager.SetSelectMode(true);
        inputManager.OnColliderClicked -= ColliderClicked;

        if (cellIndicatorRenderer != null)
            cellIndicatorMaterial.color = defaultColor;
        menuController.IsCharacterSelected = false;
    }

    private void HandleDeselectRequest()
    {
        
        if (IsTryToAttack || IsTryToMove)
        {
            gridVisualization.SetActive(false);
            cellIndicator.SetActive(false);
            movePreview.ClearAll();
            inputManager.SetSelectMode(true);
            characterInfoUI?.ShowActionButtons();
            currentAction = null;
            IsTryToMove = false;
            IsTryToAttack = false;
        }
        else
        {
            ExitCharacter();
            inputManager.OnColliderClicked += ColliderClicked;
        }
    }

    private void HandleAgentTurnEnded()
    {
        ExitCharacter();
        isTurnComplete = true;
    }

    private void EndAction()
    {
        isInActionMode = false;
        IsTryToMove = false;
        IsTryToAttack = false;
        currentAction = null;
        
        gridVisualization.SetActive(false);
        cellIndicator.SetActive(false);

        if (turnManager.State != CombatState.Playing) return;

        if (combatExecutor.IsAttacking || combatExecutor.IsMoving)
            StartCoroutine(ShowMenuAfterAnimation());
        else
        {
            characterInfoUI?.ShowActionButtons();
            inputManager.SetSelectMode(true);
        }
            
    }

    private IEnumerator ShowMenuAfterAnimation()
    {
        yield return new WaitUntil(() => !combatExecutor.IsAttacking && !combatExecutor.IsMoving);

        if (turnManager.State != CombatState.Playing) yield break;

        if (selectedChar == null) yield break;

        characterInfoUI?.ShowActionButtons();
        inputManager.SetSelectMode(true);
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
