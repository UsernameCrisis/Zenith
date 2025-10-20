using System;
using UnityEngine;

public class GridSelectSystem : MonoBehaviour
{
    [SerializeField] GameObject mouseIndicator, cellIndicator;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Grid grid;
    [SerializeField] private ObjectDatabaseSO database;
    [SerializeField] private GameObject gridVisualization;

    private Vector3 mousePos;

    void OnEnable()
    {
        // inputManager.OnHoverEnter += ShowHover;
        // inputManager.OnHoverExit += HideHover;
        inputManager.OnColliderClicked += SelectCharacter;
        inputManager.OnExit += ExitCharacter;
    }

    void OnDisable()
    {
        // inputManager.OnHoverEnter -= ShowHover;
        // inputManager.OnHoverExit -= HideHover;
        inputManager.OnColliderClicked -= SelectCharacter;
        inputManager.OnExit -= ExitCharacter;
    }

    void Start()
    {
        ExitCharacter(); // hanya untuk menghilangkan grid sementara (karena dalam scene view dinyalakan)
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
        Vector3Int gridPosition = grid.WorldToCell(mousePos);
        cellIndicator.transform.position = grid.CellToWorld(gridPosition);
    }

    private void SelectCharacter(Collider collider)
    {
        if (collider.CompareTag("Sprite"))
        {
            gridVisualization.SetActive(true);
            cellIndicator.SetActive(true);
        }
        
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
