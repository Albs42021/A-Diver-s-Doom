using UnityEngine;
using System.Collections.Generic;

public class SubmarineLevelGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject straightHallwayPrefab;
    public GameObject branchingHallwayPrefab;
    public GameObject splitHallwayPrefab;
    public GameObject[] puzzleRoomPrefabs;
    public GameObject escapeRoomPrefab;

    [Header("Settings")]
    public Transform startPoint;
    public int mainPathLength = 4;
    public int branchDepthMin = 1;
    public int branchDepthMax = 2;

    void Start()
    {
        GenerateLevel();
    }

    void GenerateLevel()
    {
        Transform currentPoint = startPoint;

        for (int i = 0; i < mainPathLength; i++)
        {
            currentPoint = SpawnPuzzle(currentPoint);
            currentPoint = SpawnStraightHallway(currentPoint);

            bool useSplit = Random.value < 0.5f;

            if (useSplit)
            {
                GameObject split = Instantiate(splitHallwayPrefab, currentPoint.position, currentPoint.rotation);
                SplitHallwayConnector splitConnector = split.GetComponent<SplitHallwayConnector>();
                if (splitConnector == null || splitConnector.forwardExit == null || splitConnector.sideExit == null)
                {
                    Debug.LogError("SplitHallwayConnector missing or misconfigured.");
                    return;
                }

                currentPoint = splitConnector.forwardExit;

                int depth = Random.Range(branchDepthMin, branchDepthMax + 1);
                GenerateBranch(splitConnector.sideExit, depth);
            }
            else
            {
                GameObject branching = Instantiate(branchingHallwayPrefab, currentPoint.position, currentPoint.rotation);
                BranchingHallwayConnector branchingConnector = branching.GetComponent<BranchingHallwayConnector>();
                if (branchingConnector == null || branchingConnector.forwardExit == null)
                {
                    Debug.LogError("BranchingHallwayConnector missing or misconfigured.");
                    return;
                }

                currentPoint = branchingConnector.forwardExit;

                int leftDepth = Random.Range(branchDepthMin, branchDepthMax + 1);
                int rightDepth = Random.Range(branchDepthMin, branchDepthMax + 1);

                if (branchingConnector.leftExit != null)
                    GenerateBranch(branchingConnector.leftExit, leftDepth);

                if (branchingConnector.rightExit != null)
                    GenerateBranch(branchingConnector.rightExit, rightDepth);
            }
        }

        // Final room
        Instantiate(escapeRoomPrefab, currentPoint.position, currentPoint.rotation);
    }

    // 💡 Recursive branch generator
    void GenerateBranch(Transform fromPoint, int depth)
    {
        if (depth <= 0) return;

        Transform current = fromPoint;

        current = SpawnPuzzle(current);
        current = SpawnStraightHallway(current);

        if (depth == 1) return;

        bool useSplit = Random.value < 0.5f;

        if (useSplit)
        {
            GameObject split = Instantiate(splitHallwayPrefab, current.position, current.rotation);
            SplitHallwayConnector splitConnector = split.GetComponent<SplitHallwayConnector>();
            if (splitConnector == null) return;

            // Recurse into side exit
            GenerateBranch(splitConnector.sideExit, depth - 1);
            current = splitConnector.forwardExit;
        }
        else
        {
            GameObject branching = Instantiate(branchingHallwayPrefab, current.position, current.rotation);
            BranchingHallwayConnector branchingConnector = branching.GetComponent<BranchingHallwayConnector>();
            if (branchingConnector == null) return;

            // Recurse into both sides
            if (branchingConnector.leftExit != null)
                GenerateBranch(branchingConnector.leftExit, depth - 1);
            if (branchingConnector.rightExit != null)
                GenerateBranch(branchingConnector.rightExit, depth - 1);

            current = branchingConnector.forwardExit;
        }
    }

    // Common straight hallway spawner
    Transform SpawnStraightHallway(Transform atPoint)
    {
        GameObject hallway = Instantiate(straightHallwayPrefab, atPoint.position, atPoint.rotation);
        RoomConnector hallwayConnector = hallway.GetComponent<RoomConnector>();
        if (hallwayConnector == null || hallwayConnector.exitPoint == null)
        {
            Debug.LogError("StraightHallway missing RoomConnector or ExitPoint.");
            return atPoint;
        }
        return hallwayConnector.exitPoint;
    }

    Transform SpawnPuzzle(Transform atPoint)
    {
        GameObject room = Instantiate(
            puzzleRoomPrefabs[Random.Range(0, puzzleRoomPrefabs.Length)],
            atPoint.position,
            atPoint.rotation
        );

        RoomConnector connector = room.GetComponent<RoomConnector>();
        return connector != null && connector.exitPoint != null
            ? connector.exitPoint
            : atPoint;
    }
}
