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

//public class OldPathfinding : MonoBehaviour
//{
//    [SerializeField]
//    private PlayerController playerController;

//    [SerializeField]
//    private GridManager gridManager = null;

//    [NonSerialized]
//    public List<NodeTileData> previousPath;

//    private void Awake()
//    {
//        // Allocate collections
//        previousPath = new List<NodeTileData>();
//    }

//    public void FindPath(Vector3 startWorldPos, Vector3 targetWorldPos)
//    {
//        // convert world positions to node positions on the grid
//        var startNodePos = gridManager.moveGridMap.WorldToCell(startWorldPos);
//        var targetNodePos = gridManager.moveGridMap.WorldToCell(targetWorldPos);

//        // Get the NodeTileData (our Node class for this A* implementation) from the tile
//        // at the start and target positions
//        NodeTileData startNode = null;
//        NodeTileData targetNode = null;
//        gridManager.moveTileDataDict.TryGetValue(startNodePos, out startNode);
//        gridManager.moveTileDataDict.TryGetValue(targetNodePos, out targetNode);

//        // If we encounter and invalid start or target, we want to exit early
//        if (!startNode || !targetNode)
//        {
//            return;
//        }

//        // This is the set of nodes to be evaluated or re-evaluated
//        List<NodeTileData> openSet = new List<NodeTileData>();

//        // This is the set of nodes that have already been evaluated
//        // At the end, this will be our path
//        HashSet<NodeTileData> closedSet = new HashSet<NodeTileData>();

//        // Add our starting node to the open set
//        openSet.Add(startNode);

//        while (openSet.Count > 0)
//        {
//            NodeTileData currentNode = openSet[0];
//            for (int i = 0; i < openSet.Count; i++)
//            {
//                // Find the unvisited node in the open set that has the lowest score
//                // *NOTE: (lower score is better, it represents the estimated + cumulative cost, which we want to minimize)
//                bool isScoreLower = openSet[i].Score < currentNode.Score;
//                bool isScoreEqual = openSet[i].Score == currentNode.Score;
//                bool isTotalCostLower = openSet[i].estimatedCostToTarget < currentNode.estimatedCostToTarget;
//                if (isScoreLower || (isScoreEqual && isTotalCostLower))
//                {
//                    currentNode = openSet[i];
//                }

//                // remove this optimal node from the open set and add it to the closed set
//                // since we have evaluated it to be an optimal node on the path
//                openSet.Remove(currentNode);
//                closedSet.Add(currentNode);

//                // If we've found our target node, exit and return the path
//                if (currentNode == targetNode)
//                {
//                    RetracePath(startNode, targetNode);
//                    //Debug.Log("FOUND THE PATH!!");
//                    return;
//                }

//                // Iterate through neighbors and do the following:
//                // 1. Calculate their G and H costs (cumulativeCost and estimatedCostToTarget)
//                // 2. Set the parent node
//                // 3. Add it to the open set if it's not in it already
//                foreach (NodeTileData neighbor in gridManager.GetNeighbors(currentNode, gridManager.moveGridMap))
//                {
//                    if (!neighbor.IsWalkable || closedSet.Contains(neighbor))
//                    {
//                        continue;
//                    }

//                    int newMovementCostToNeighbor = currentNode.cumulativeCost + gridManager.GetNodeDistance(currentNode, neighbor);
//                    if (newMovementCostToNeighbor < neighbor.cumulativeCost || !openSet.Contains(neighbor))
//                    {
//                        neighbor.cumulativeCost = newMovementCostToNeighbor;
//                        neighbor.estimatedCostToTarget = gridManager.GetNodeDistance(neighbor, targetNode);
//                        neighbor.ParentNode = currentNode;

//                        if (!openSet.Contains(neighbor))
//                        {
//                            openSet.Add(neighbor);
//                        }
//                    }
//                }
//            }
//        }
//    }

//    private void RetracePath(NodeTileData startNode, NodeTileData endNode)
//    {
//        List<NodeTileData> path = new List<NodeTileData>();
//        NodeTileData currentNode = endNode;

//        // iterate over the path following all the parent nodes starting from the end
//        while (currentNode != startNode)
//        {
//            path.Add(currentNode);
//            currentNode = currentNode.ParentNode;
//        }

//        // Reverse the path since started at the end
//        path.Reverse();

//        // Recycle tiles from the previous path, but remove any that don't overlap
//        RemoveLastPath(previousPath, path);

//        // Visualize the path by setting the tiles equal to a their move cost
//        for (int x = 0; x < path.Count; x++)
//        {
//            // We only want to draw the path up to the max moves left for the player
//            if (x < playerController.movesLeft)
//            {
//                // check if the tile at the current position is blank
//                // we don't want to set the tile to a number if it's been
//                // recycled from the previous path
//                bool isTileBlank = gridManager.moveGridMap.GetTile(path[x].Position) == gridManager.fillTiles[0];
//                if (isTileBlank)
//                {
//                    // x+1 here because the 0th index holds the blank tile
//                    // each index holds a tile with that index as a number on it (up to 6)
//                    gridManager.moveGridMap.SetTile(path[x].Position, gridManager.fillTiles[x + 1]);
//                }

//            }
//        }

//        // Save this path so that we can delete it before we draw the next one
//        previousPath = path;
//    }

//    private void RemoveLastPath(List<NodeTileData> oldPath, List<NodeTileData> newPath)
//    {
//        for (int i = 0; i < oldPath.Count; i++)
//        {
//            // Need to reset the costs for all nodes that we may be recycling
//            // This is to prevent them keeping their costs and scores from the last run of A*
//            var currentNode = oldPath[i];
//            currentNode.Init(currentNode.Position, 0, 0, currentNode.IsWalkable);

//            // only look at tiles in the oldPath that are within the newpath's max distance
//            if (i < newPath.Count)
//            {
//                // Since the paths are ordered, compare the cumulativeCosts (the number on the tile) and the position:
//                // if either don't match we need to remove this tile so it can be redrawn, we leave it otherwise
//                if (newPath[i].cumulativeCost != oldPath[i].cumulativeCost || newPath[i].Position != oldPath[i].Position)
//                {
//                    gridManager.moveGridMap.SetTile(oldPath[i].Position, gridManager.fillTiles[0]);
//                }
//            }
//            else
//            {
//                // This case is if the newPath is shorter than the oldPath, so we need to remove all the tiles past that point
//                gridManager.moveGridMap.SetTile(oldPath[i].Position, gridManager.fillTiles[0]);
//            }
//        }
//    }
//}
