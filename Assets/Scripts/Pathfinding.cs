using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Tilemaps;

public class Pathfinding : MonoBehaviour
{
    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private GridManager gridManager = null;

    [SerializeField]
    private GameObject[] pathTiles = null;

    [NonSerialized]
    public List<NodeTileData> previousPath = null;

    private Vector3 tileOffset;

    private void Awake()
    {
        // Allocate collections
        previousPath = new List<NodeTileData>();

        tileOffset = new Vector3(0.5f, 0.5f, 0);
    }

    
    ///TODO THIS IS VERY BROKEN
    public void CalculateMovementArea()
    {
        gridManager.ClearAllTiles(GridManager.MapName.TacticalArea);

        List<Vector3Int> tilesWithinMovement = new List<Vector3Int>();

        foreach(KeyValuePair<Vector3Int, Tile> node in gridManager.tacticalAreaDict)
        {
            var distanceToNode = gridManager.GetNodeDistance(playerController.currentGridPosition, node.Key);
            if(distanceToNode <= playerController.movesLeft)
            {
                //gridManager.SetAvailableMoveTile(node.Key, gridManager.greenTile);
                tilesWithinMovement.Add(node.Key);
            }
        }

        foreach (var node in tilesWithinMovement)
        {
            //var pathToNode = FindPath(playerController.currentGridPosition, node);
            //if (pathToNode != null && pathToNode.Count <= playerController.movesLeft)
            //{
            //    gridManager.SetTile(GridManager.MapName.TacticalArea, node, gridManager.greenTile);
            //}
        }
    }

    public (bool exists, List<NodeTileData> path) FindPath(Vector3 startWorldPos, Vector3 targetWorldPos, GridManager.MapName mapName = GridManager.MapName.MoveGrid, bool drawPath = true)
    {
        // convert world positions to node positions on the grid
        var startNodePos = gridManager.WorldToCell(mapName, startWorldPos);
        var targetNodePos = gridManager.WorldToCell(mapName, targetWorldPos);

        // Get the NodeTileData (our Node class for this A* implementation) from the tile
        // at the start and target positions
        NodeTileData startNode = null;
        NodeTileData targetNode = null;
        gridManager.moveTileDataDict.TryGetValue(startNodePos, out startNode);
        gridManager.moveTileDataDict.TryGetValue(targetNodePos, out targetNode);
        
        // If we encounter and invalid start or target, we want to exit early
        if(startNode == null || targetNode == null)
        {
            //Debug.LogError("invalid node");
            return (false, null);
        }

        // This is the set of nodes to be evaluated or re-evaluated
        List<NodeTileData> openSet = new List<NodeTileData>();

        // This is the set of nodes that have already been evaluated
        // At the end, this will be our path
        HashSet<NodeTileData> closedSet = new HashSet<NodeTileData>();

        // Add our starting node to the open set
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            NodeTileData currentNode = openSet[0];
            for (int i = 0; i < openSet.Count; i++)
            {
                // Find the unvisited node in the open set that has the lowest score
                // *NOTE: (lower score is better, it represents the estimated + cumulative cost, which we want to minimize)
                bool isScoreLower = openSet[i].Score < currentNode.Score;
                bool isScoreEqual = openSet[i].Score == currentNode.Score;
                bool isTotalCostLower = openSet[i].estimatedCostToTarget < currentNode.estimatedCostToTarget;
                if (isScoreLower || (isScoreEqual && isTotalCostLower))
                {
                    currentNode = openSet[i];
                }
            }

            // remove this optimal node from the open set and add it to the closed set
            // since we have evaluated it to be an optimal node on the path
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            // If we've found our target node, exit and return the path
            if (currentNode == targetNode)
            {
                var path = RetracePath(startNode, targetNode, drawPath);

                // Set previous path so we can delete it later
                //Debug.LogError("Saving Path");

                //Debug.Log("FOUND THE PATH!!");
                return (true, path);
            }

            // Iterate through neighbors and do the following:
            // 1. Calculate their G and H costs (cumulativeCost and estimatedCostToTarget)
            // 2. Set the parent node
            // 3. Add it to the open set if it's not in it already

            ///TODO: GetNeighbors may be inefficient since it's querying the tilemap
            /// we can make it more efficient by storing all the neighbor's data for each tile in the tile data
            foreach (NodeTileData neighbor in gridManager.GetNeighbors(currentNode))
            {
                if(!neighbor.IsWalkable || closedSet.Contains(neighbor))
                {
                    continue;
                }

                int newMovementCostToNeighbor = currentNode.cumulativeCost + gridManager.GetNodeDistance(currentNode, neighbor);
                if(newMovementCostToNeighbor < neighbor.cumulativeCost || !openSet.Contains(neighbor))
                {
                    neighbor.cumulativeCost = newMovementCostToNeighbor;
                    neighbor.estimatedCostToTarget = gridManager.GetNodeDistance(neighbor, targetNode);
                    neighbor.ParentNode = currentNode;

                    if(!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return (false, null);
    }

    private List<NodeTileData> RetracePath(NodeTileData startNode, NodeTileData endNode, bool drawPath = true)
    {
        List<NodeTileData> path = new List<NodeTileData>();
        NodeTileData currentNode = endNode;
        
        // iterate over the path following all the parent nodes starting from the end
        while(currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.ParentNode;
        }

        // Reverse the path since started at the end
        path.Reverse();

        if(drawPath)
        {
            // Recycle tiles from the previous path, but remove any that don't overlap
            RemoveLastPath(previousPath, path);

            // Visualize the path by setting the tiles equal to a their move cost
            for(int x = 0; x < path.Count; x++)
            {
                // We only want to draw the path up to the max moves left for the player
                if (x < playerController.movesLeft)
                {
                    // check if the tile at the current position is blank
                    // we don't want to set the tile to a number if it's been
                    // recycled from the previous path
                    //bool isTileBlank = gridManager.moveGridMap.GetTile(path[x].Position) == gridManager.fillTiles[0];
                    bool isTileBlank = IsTileBlank(path[x].Position);
                    if (isTileBlank)
                    {
                        // x+1 here because the 0th index holds the blank tile
                        // each index holds a tile with that index as a number on it (up to 6)
                        //gridManager.SetTile(GridManager.MapName.MoveGrid, path[x].Position, gridManager.fillTiles[x+1]);
                        //Debug.LogError("Adding "+x+" to path");
                        MoveAndSetActiveState(x, path[x].Position, true);
                    }

                }
            }

            // Save this path so that we can delete it before we draw the next one
            previousPath = path;
        }

        return path;
    }

    /// TODO: REWRITE THIS FUNCTION IT SUCKS
    /// Current bug: Making a long path, hovering back across the path to shorten it, and then lengthening it again
    /// will not show the tiles that were removed when shortened
    private void RemoveLastPath(List<NodeTileData> oldPath, List<NodeTileData> newPath)
    {
        for (int i = 0; i < oldPath.Count; i++)
        {
            // Need to reset the costs for all nodes that we may be recycling
            // This is to prevent them keeping their costs and scores from the last run of A*
            var currentNode = oldPath[i];

            // only look at tiles in the oldPath that are within the newpath's max distance
            if (i < newPath.Count)
            {
                // Since the paths are ordered, compare the cumulativeCosts (the number on the tile) and the position:
                // if either don't match we need to remove this tile so it can be redrawn, we leave it otherwise
                if (newPath[i].Position != oldPath[i].Position && i < pathTiles.Count())
                {
                    //gridManager.SetTile(GridManager.MapName.MoveGrid, oldPath[i].Position, gridManager.blankTile);
                    var tileToSet = oldPath[i];
                    MoveAndSetActiveState(i, tileToSet.Position, false);
                }
            }
            else if (i >= newPath.Count && i < pathTiles.Count())
            {
                // This case is if the newPath is shorter than the oldPath, so we need to remove all the tiles past that point
                var tileToSet = oldPath[i];
                MoveAndSetActiveState(i, tileToSet.Position, false);
            }
            currentNode.Init(currentNode.Position, 0, 0, currentNode.IsWalkable);
        }
    }

    private void MoveAndSetActiveState(int tile, Vector3Int position, bool isActive)
    {
        Vector3 worldPoint = gridManager.CellToWorld(GridManager.MapName.MoveGrid, position);
        var tileInPath = pathTiles[tile];
        var tileSpriteRenderer = tileInPath.GetComponent<SpriteRenderer>();

        // If we are moving a tile into place we want to move it before we turn it on
        // If we are removing it, we do the opposite
        if(isActive)
        {
            //Debug.LogError("Setting tile " + tile + " active");
            tileInPath.transform.position = worldPoint + tileOffset;
            //tileInPath.SetActive(isActive);
            tileSpriteRenderer.enabled = isActive;
        }
        else if(!isActive)
        {
            //Debug.LogError("Setting tile " + tile + " INACTIVE");
            //tileInPath.SetActive(isActive);
            tileSpriteRenderer.enabled = isActive;
            tileInPath.transform.position = worldPoint + tileOffset;
        }
    }

    private bool IsTileBlank(Vector3Int position)
    {
        foreach(var tile in pathTiles)
        {
            var tileCellPosition = gridManager.WorldToCell(GridManager.MapName.MoveGrid, tile.transform.position);
            if(tileCellPosition == position)
            {
                return false;
            }
        }

        return true;
    }
}
