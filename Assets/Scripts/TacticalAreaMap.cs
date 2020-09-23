using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

public class TacticalAreaMap : MonoBehaviour
{
    [SerializeField]
    GridManager gridManager = null;

    [SerializeField]
    Pathfinding pathfinding = null;

    [SerializeField]
    private Tilemap tacticalTileMap = null;

    [SerializeField]
    private UIController uiController = null;

    [SerializeField]
    private PlayerController playerController = null;

    private void Start()
    {
        uiController.ShowTacticalArea += OnShowTacticalArea;
        playerController.PlayerPositionUpdated += UpdateTacticalArea;
    }

    private void OnDestroy()
    {
        uiController.ShowTacticalArea -= OnShowTacticalArea;
        playerController.PlayerPositionUpdated -= UpdateTacticalArea;
    }

    private void OnShowTacticalArea()
    {
        Debug.LogError("running floodfill");
        Floodfill(playerController.currentGridPosition, gridManager.blankTile, gridManager.greenTile, tacticalTileMap);
    }

    private void UpdateTacticalArea(Vector3Int playerPosition)
    {
        if(uiController.showingMoveArea)
        {
            Floodfill(playerPosition, gridManager.blankTile, gridManager.greenTile, tacticalTileMap);
        }
    }

    public void Floodfill(Vector3Int playerPos, Tile defaultTile, Tile fillTile, Tilemap moveGrid = null)
    {
        gridManager.ClearAllTiles(GridManager.MapName.TacticalArea);

        if(!moveGrid)
        {
            moveGrid = tacticalTileMap;
        }

        moveGrid.CompressBounds();
        int minY = moveGrid.cellBounds.xMin;
        int minX = moveGrid.cellBounds.yMin;
        int maxX = moveGrid.cellBounds.xMax;
        int maxY = moveGrid.cellBounds.yMax;

        if (playerPos.y < minY || playerPos.y > maxY - 1 || playerPos.x < minX || playerPos.x > maxX - 1)
        {
            return;
        }

        Stack<Vector3Int> stack = new Stack<Vector3Int>();
        stack.Push(playerPos);

        // Count the tiles so that if we try to draw a blank tileMap, we turn the map off instead
        int tilesFilled = 0;
        while (stack.Count > 0)
        {
            Vector3Int currentPoint = stack.Pop();
            int x = currentPoint.x;
            int y = currentPoint.y;

            // skip all tiles that are out of bounds
            if (y < minY || y > maxY - 1 || x < minX || x > maxX - 1)
            {
                continue;
            }
           
            // skip all tiles that contain obstacles or if the position is the player position
            if (gridManager.HasTile(GridManager.MapName.Obstacle, currentPoint))
            {
                continue;
            }

            var currentTile = moveGrid.GetTile(currentPoint);
            if (currentTile == defaultTile)
            {
                stack.Push(new Vector3Int(x + 1, y, 0));
                stack.Push(new Vector3Int(x - 1, y, 0));
                stack.Push(new Vector3Int(x, y + 1, 0));
                stack.Push(new Vector3Int(x, y - 1, 0));


                var pathExists = pathfinding.FindPath(playerPos, currentPoint, GridManager.MapName.TacticalArea);

                var nodeIndexPair = FindNodeAndIndex(pathExists.path, currentPoint);
                if (nodeIndexPair.index >= 0 && nodeIndexPair.node != null && nodeIndexPair.index < playerController.movesLeft)
                {
                    tilesFilled++;
                    moveGrid.SetTile(currentPoint, fillTile);
                }
                else
                {
                    moveGrid.SetTile(currentPoint, null);
                }

            }
            else
            {
                //Debug.LogError("we didn't insert the tile");
            }
        }

        if(tilesFilled == 0)
        {
            gridManager.ClearAllTiles(GridManager.MapName.TacticalArea);
            uiController.showingMoveArea = false;
        }
    }

    private (int index, NodeTileData node) FindNodeAndIndex(List<NodeTileData> path, Vector3Int nodePosition)
    {
        for(int i = 0; i < path.Count(); i++)
        {
            if(path[i].Position == nodePosition)
            {
                return (i, path[i]);
            }
        }

        return (-1, null);
    }
}