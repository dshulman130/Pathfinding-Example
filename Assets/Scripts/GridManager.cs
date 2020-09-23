using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    [SerializeField]
    public Tile blankTile = null;

    [SerializeField]
    public Tile greenTile = null;

    [SerializeField]
    public Tile[] fillTiles = null;

    [SerializeField]
    private Tilemap obstacleMap = null;

    [SerializeField]
    private Tilemap groundMap = null;

    [SerializeField]
    public Tilemap moveGridMap = null;

    [SerializeField]
    private Tilemap tacticalAreaMap = null;

    [SerializeField]
    private Pathfinding pathfinding = null;

    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private Camera mainCamera = null;

    [SerializeField]
    public GridMovement gridControls = null;

    public Dictionary<Vector3Int, NodeTileData> moveTileDataDict;
    public Dictionary<Vector3Int, Tile> tacticalAreaDict;


    private Vector2 currentMousePosition;

    public enum MapName
    {
        Obstacle,
        Ground,
        MoveGrid,
        TacticalArea
    }

    private void Awake()
    {
        // Allocate collections
        moveTileDataDict = new Dictionary<Vector3Int, NodeTileData>();
        tacticalAreaDict = new Dictionary<Vector3Int, Tile>();
        gridControls = new GridMovement();
    }

    private void OnEnable()
    {
        gridControls.Enable();
    }

    private void OnDisable()
    {
        gridControls.Disable();
    }

    private void Start()
    {
        // no implicit conversion exists here to a bool, so we need to add the "!= null"
        if (gridControls != null)
        {
            gridControls.UI.Point.performed += context => HandleMousePositionUpdate(context.ReadValue<Vector2>());
            gridControls.UI.Click.performed += context => HandleGridClick(context.ReadValue<float>());
        }

        ConstructTileDataDict();
    }

    private void HandleMousePositionUpdate(Vector2 mousePosition)
    {
        currentMousePosition = mousePosition;
        //Debug.Log("Pointed at : " + currentMousePosition);
        var mouseWorldPos = new Vector3(currentMousePosition.x, currentMousePosition.y, 0);
        var mouseScreenPos = mainCamera.ScreenToWorldPoint(mouseWorldPos);
        var screenToGridPos = moveGridMap.WorldToCell(mouseScreenPos);

        //Debug.LogError("CLICKED: " + screenToGridPos);

        if (moveGridMap.localBounds.Contains(screenToGridPos) && !playerController.isMoving)
        {
            //Debug.Log("hovering");
            pathfinding.FindPath(playerController.currentPosition, screenToGridPos);
            //var path = pathfinding.FindPath(playerController.currentPosition, screenToGridPos);
        }
    }

    private void HandleGridClick(float worldClickLocation)
    {
        if (worldClickLocation == 1)
        {
            // Get player and grid position from clicked position
            var mouseWorldPos = new Vector3(currentMousePosition.x, currentMousePosition.y, 0);
            var mouseScreenPos = mainCamera.ScreenToWorldPoint(mouseWorldPos);
            var clickGridPos = moveGridMap.WorldToCell(mouseScreenPos);
            var playerGridPos = moveGridMap.WorldToCell(playerController.currentPosition);

            // Get tile data for clicked position
            NodeTileData clickedTileData = null;

            try
            {
                clickedTileData = pathfinding.previousPath.Single(x => x.Position == clickGridPos); // Single is safe here since you can't add the same tile to the path twice
            }
            catch(Exception e)
            {
                Debug.LogError(e);
            }

            if(clickedTileData != null)
            {
                var isClickedTileWithinPath = pathfinding.previousPath.Contains(clickedTileData);
                var isTileWithinMovement = clickedTileData.cumulativeCost <= playerController.movesLeft;
                
                // if tile is within movement of player and on the path, move the player
                if (isClickedTileWithinPath && isTileWithinMovement)
                {
                    var pathPositionList = pathfinding.previousPath.Select(x => x.Position).ToList();
                    playerController.Move(clickGridPos, pathPositionList);
                }
            }

        }
    }

    private void ConstructTileDataDict()
    {
        // Iterate over all the tiles in the movement tilemap and create a NodeTileData object for them
        // This will store various data to be used in the pathfinding algorithm.
        // Store this relationship of (tile position) => (tile data) in a dictionary
        foreach (var pos in moveGridMap.cellBounds.allPositionsWithin)
        {
            var nodePosition = moveGridMap.WorldToCell(pos);
            var tile = moveGridMap.GetTile<Tile>(nodePosition);
            var nodeTileData = new NodeTileData();
            var isWalkable = !obstacleMap.HasTile(nodePosition);
            //Debug.LogError("Init node data to 0");
            nodeTileData.Init(nodePosition, 0, 0, isWalkable);

            if (tile)
            {
                moveTileDataDict[nodePosition] = nodeTileData;
            }
        }

        // Do the same for the available move area map, but save the tile instead
        foreach (var pos in tacticalAreaMap.cellBounds.allPositionsWithin)
        {
            var nodePosition = tacticalAreaMap.WorldToCell(pos);
            var tile = tacticalAreaMap.GetTile<Tile>(nodePosition);
            var isWalkable = !obstacleMap.HasTile(nodePosition);

            if (tile)
            {
                tacticalAreaDict[nodePosition] = tile;
            }
        }
    }

    public Vector3Int ClickToGridPosition(Vector2 mousePosition)
    {
        currentMousePosition = mousePosition;
        //Debug.Log("Pointed at : " + currentMousePosition);
        var mouseWorldPos = new Vector3(currentMousePosition.x, currentMousePosition.y, 0);
        //var mouseScreenPos = mainCamera.ScreenToWorldPoint(mouseWorldPos);
        var screenToGridPos = tacticalAreaMap.WorldToCell(mouseWorldPos);

        return screenToGridPos;
    }

    /// <summary>
    /// Find all the nearest neighbors for the given @node in @tilemap
    /// </summary>
    /// <param name="node">The node to find neighbors for</param>
    /// <returns>
    ///     Returns a List of NodeTileData objects which represents all the immediate neigbors of @node.
    ///     This does not include diagonal neighbors. Note: This currently only works on the moveGridMap as no
    ///     other TileMap is being edited at runtime.
    /// </returns>
    public List<NodeTileData> GetNeighbors(NodeTileData node)
    {
        // Chose not to use a loop here for readability since we only ever have 4 valid neighbors
        List<NodeTileData> neighbors = new List<NodeTileData>();
        var northPosition = new Vector3Int(node.Position.x, node.Position.y + 1, 0);
        var eastPosition = new Vector3Int(node.Position.x + 1, node.Position.y, 0);
        var southPosition = new Vector3Int(node.Position.x, node.Position.y - 1, 0);
        var westPosition = new Vector3Int(node.Position.x - 1, node.Position.y, 0);

        NodeTileData tempNode = null;

        // TryGetValue is faster than ContainsKey here if we have large tileMaps, as it avoids a second lookup
        if (moveGridMap.HasTile(northPosition) && moveTileDataDict.TryGetValue(northPosition, out tempNode))
        {
            neighbors.Add(tempNode);
        }

        if (moveGridMap.HasTile(eastPosition) && moveTileDataDict.TryGetValue(eastPosition, out tempNode))
        {
            neighbors.Add(tempNode);
        }

        if (moveGridMap.HasTile(southPosition) && moveTileDataDict.TryGetValue(southPosition, out tempNode))
        {
            neighbors.Add(tempNode);
        }

        if (moveGridMap.HasTile(westPosition) && moveTileDataDict.TryGetValue(westPosition, out tempNode))
        {
            neighbors.Add(tempNode);
        }

        return neighbors;
    }

    public int GetNodeDistance(Vector3Int source, Vector3Int target)
    {
        var targetX = target.x;
        var targetY = target.y;

        var sourceX = source.x;
        var sourceY = source.y;

        var yDistance = Math.Abs(sourceY - targetY);
        var xDistance = Math.Abs(sourceX - targetX);

        if (xDistance > yDistance)
        {
            // Diagonal distance + remaining horizontal distance
            /*
             * 2 [ ] [ ] [^] [>] [>] [B]
             * 1 [ ] [^] [ ] [ ] [ ] [ ]
             * y [A] [ ] [ ] [ ] [ ] [ ]
             *    x   1   2   3   4   5
             */
            return yDistance + (xDistance - yDistance);
        }

        return xDistance + (yDistance - xDistance);
    }

    public int GetNodeDistance(NodeTileData source, NodeTileData target)
    {
        var targetX = target.Position.x;
        var targetY = target.Position.y;

        var sourceX = source.Position.x;
        var sourceY = source.Position.y;

        var yDistance = Math.Abs(sourceY - targetY);
        var xDistance = Math.Abs(sourceX - targetX);

        if (xDistance > yDistance)
        {
            // Diagonal distance + remaining horizontal distance
            /*
             * 2 [ ] [ ] [^] [>] [>] [B]
             * 1 [ ] [^] [ ] [ ] [ ] [ ]
             * y [A] [ ] [ ] [ ] [ ] [ ]
             *    x   1   2   3   4   5
             */
            return yDistance + (xDistance - yDistance);
        }

        return xDistance + (yDistance - xDistance);
    }

    public Vector3Int WorldToCell(MapName mapName, Vector3 worldPoint)
    {
        Tilemap tilemap = NametoTilemap(mapName);

        if (tilemap)
        {
            return tilemap.WorldToCell(worldPoint);
        }
        else
        {
            return default;
        }
    }

    public Vector3 CellToWorld(MapName mapName, Vector3Int gridPoint)
    {
        Tilemap tilemap = NametoTilemap(mapName);

        if (tilemap)
        {
            return tilemap.CellToWorld(gridPoint);
        }
        else
        {
            return default;
        }
    }

    public void ClearAllTiles(MapName mapName)
    {
        Tilemap tilemap = NametoTilemap(mapName);

        if(tilemap == null)
        {
            return;
        }

        foreach (var pos in tilemap.cellBounds.allPositionsWithin)
        {
            if(tilemap.GetTile(pos) != blankTile)
            {
                tilemap.SetTile(pos, blankTile);
            }
        }
    }

    public void SetTile(MapName mapName, Vector3Int position, Tile tile)
    {
        var tileMap = NametoTilemap(mapName);
        tileMap.SetTile(position, tile);
    }

    public Tile GetTile(MapName mapName, Vector3Int position)
    {
        Tile tile = blankTile;
        var tileMap = NametoTilemap(mapName);

        return (Tile)tileMap.GetTile(position);
    }

    public bool HasTile(MapName mapName, Vector3Int position)
    {
        var tileMap = NametoTilemap(mapName);
        return tileMap.HasTile(position);
    }

    public Tilemap NametoTilemap(MapName mapName)
    {
        Tilemap tilemap = null;
        switch (mapName)
        {
            case MapName.Ground:
                tilemap = groundMap;
                break;
            case MapName.Obstacle:
                tilemap = obstacleMap;
                break;
            case MapName.MoveGrid:
                tilemap = moveGridMap;
                break;
            case MapName.TacticalArea:
                tilemap = tacticalAreaMap;
                break;
            default:
                break;
        }

        return tilemap;
    }
}
