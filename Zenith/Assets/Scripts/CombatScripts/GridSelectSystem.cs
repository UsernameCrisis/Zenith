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

    private Vector3 mousePos;
    private GridData objectsData;
    private Renderer previewRenderer;
    private GameObject selectedChar;
    

    void OnEnable()
    {
        // inputManager.OnHoverEnter += ShowHover;
        // inputManager.OnHoverExit += HideHover;
        inputManager.OnColliderClicked += ColliderClicked;
        inputManager.OnExit += ExitCharacter;
    }

    void OnDisable()
    {
        // inputManager.OnHoverEnter -= ShowHover;
        // inputManager.OnHoverExit -= HideHover;
        inputManager.OnColliderClicked -= ColliderClicked;
        inputManager.OnExit -= ExitCharacter;
    }

    void Start()
    {
        ExitCharacter(); // hanya untuk menghilangkan grid sementara (karena dalam scene view dinyalakan)
        objectsData = populateMap.GetComponent<PopulateMap>().objectsData;
        previewRenderer = cellIndicator.GetComponentInChildren<Renderer>(); 
    }

    void Update()
    {
        mousePos = inputManager.GetHoveredMapPosition();
        mouseIndicator.transform.position = mousePos;

        if (gridVisualization.activeSelf)
            VisualizeHoveredGrid();
    }

    private void VisualizeHoveredGrid()
    {
        // Grid pos (x,y,0), world pos (x,0,z) y == z
        Vector3Int gridPosition = grid.WorldToCell(mousePos);
        cellIndicator.transform.position = grid.CellToWorld(gridPosition);
    }
    
    private void ColliderClicked(Collider collider)
    {
        if (collider.CompareTag("Sprite"))
            SelectCharacter(collider);
        else if (gridVisualization.activeSelf && collider.CompareTag("Grid"))
            MoveCharacter();
    }

    private void MoveCharacter()
    {
        Vector3Int gridPos = grid.WorldToCell(mousePos);
        if (objectsData.CanPlaceObjectAt(gridPos))
        {
            objectsData.MoveObject(grid.WorldToCell(selectedChar.transform.position), gridPos);
            selectedChar.transform.position = grid.CellToWorld(gridPos);
        }
    }

    private void SelectCharacter(Collider collider)
    {
        selectedChar = collider.transform.parent.gameObject;
        gridVisualization.SetActive(true);
        cellIndicator.SetActive(true);
    }

    private void ExitCharacter()
    {
        gridVisualization.SetActive(false);
        cellIndicator.SetActive(false);
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
