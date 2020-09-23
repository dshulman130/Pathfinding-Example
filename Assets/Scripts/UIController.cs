using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField]
    Button endTurnButton = null;

    [SerializeField]
    Button showMoveAreaButton = null;

    [SerializeField]
    GridManager gridManager = null;

    [SerializeField]
    Pathfinding pathfinding = null;

    [SerializeField]
    Tilemap tacticalAreaMap = null;

    public bool showingMoveArea { get; set; }

    public UnityAction ShowTacticalArea;

    private void Awake()
    {
        showingMoveArea = false;
    }

    private void Start()
    {
        gridManager.gridControls.UI.ShowMovement.canceled += context => OnShowMoveAreaClicked();
        gridManager.gridControls.UI.EndTurn.started += context => OnEndTurnClicked();
    }

    private void OnDestroy()
    {
        endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
        showMoveAreaButton.onClick.RemoveListener(OnShowMoveAreaClicked);
    }

    public void OnEndTurnClicked()
    {
        Debug.LogError("end turn button clicked!");
    }

    public void OnShowMoveAreaClicked()
    {
        if(showingMoveArea)
        {
            Debug.LogError("clearing tiles");
            gridManager.ClearAllTiles(GridManager.MapName.TacticalArea);
            showingMoveArea = false;
        }
        else
        {
            Debug.LogError("showing tactical area");
            showingMoveArea = true;
            ShowTacticalArea?.Invoke();
        }
        Debug.LogError("show move area button clicked!");
    }
}
