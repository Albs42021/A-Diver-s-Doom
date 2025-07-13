using UnityEngine;

public class SubmarineLevelGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject straightHallwayPrefab;
    public GameObject branchingHallwayPrefab;
    public GameObject splitHallwayPrefab;
    public GameObject deadEndHallwayPrefab;
    public GameObject[] puzzleRoomPrefabs;
    public GameObject escapeRoomPrefab;

    [Header("Settings")]
    public Transform startPoint;
    public int mainPathLength = 4;
    public int branchDepthMin = 1;
    public int branchDepthMax = 2;

    void Start()
    {
        ValidateHallwayPrefabs();
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

                int depth = Random.Range(branchDepthMin, branchDepthMax + 1);

                GenerateBranch(splitConnector.sideExit, depth - 1);
                currentPoint = splitConnector.forwardExit;
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

                int leftDepth = Random.Range(branchDepthMin, branchDepthMax + 1);
                int rightDepth = Random.Range(branchDepthMin, branchDepthMax + 1);

                if (branchingConnector.leftExit != null)
                    GenerateBranch(branchingConnector.leftExit, leftDepth - 1);
                else
                    SpawnDeadEnd(currentPoint);

                if (branchingConnector.rightExit != null)
                    GenerateBranch(branchingConnector.rightExit, rightDepth - 1);
                else
                    SpawnDeadEnd(currentPoint);

                currentPoint = branchingConnector.forwardExit;
            }
        }

        Instantiate(escapeRoomPrefab, currentPoint.position, currentPoint.rotation);
    }

    void GenerateBranch(Transform fromPoint, int depth)
    {
        if (depth <= 0 || fromPoint == null)
        {
            SpawnDeadEnd(fromPoint);
            return;
        }

        Transform current = fromPoint;

        current = SpawnPuzzle(current);

        if (depth == 1)
        {
            SpawnDeadEnd(current);
            return;
        }

        current = SpawnStraightHallway(current);

        bool useSplit = Random.value < 0.5f;

        if (useSplit)
        {
            GameObject split = Instantiate(splitHallwayPrefab, current.position, current.rotation);
            SplitHallwayConnector splitConnector = split.GetComponent<SplitHallwayConnector>();

            if (splitConnector != null)
            {
                if (splitConnector.sideExit != null)
                    GenerateBranch(splitConnector.sideExit, depth - 1);
                else
                    SpawnDeadEnd(current);

                if (splitConnector.forwardExit != null)
                    GenerateBranch(splitConnector.forwardExit, depth - 1);
                else
                    SpawnDeadEnd(current);
            }
        }
        else
        {
            GameObject branching = Instantiate(branchingHallwayPrefab, current.position, current.rotation);
            BranchingHallwayConnector branchingConnector = branching.GetComponent<BranchingHallwayConnector>();

            if (branchingConnector != null)
            {
                if (branchingConnector.leftExit != null)
                    GenerateBranch(branchingConnector.leftExit, depth - 1);
                else
                    SpawnDeadEnd(current);

                if (branchingConnector.rightExit != null)
                    GenerateBranch(branchingConnector.rightExit, depth - 1);
                else
                    SpawnDeadEnd(current);

                if (branchingConnector.forwardExit != null)
                    GenerateBranch(branchingConnector.forwardExit, depth - 1);
                else
                    SpawnDeadEnd(current);
            }
        }
    }

    Transform SpawnStraightHallway(Transform atPoint)
    {
        GameObject hallway = Instantiate(straightHallwayPrefab, atPoint.position, atPoint.rotation);
        RoomConnector connector = hallway.GetComponent<RoomConnector>();
        if (connector == null || connector.exitPoint == null)
        {
            Debug.LogError("StraightHallway missing RoomConnector or ExitPoint.");
            return atPoint;
        }
        return connector.exitPoint;
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

    void SpawnDeadEnd(Transform atPoint)
    {
        if (atPoint == null || deadEndHallwayPrefab == null) return;
        Instantiate(deadEndHallwayPrefab, atPoint.position, atPoint.rotation);
    }

    void ValidateHallwayPrefabs()
    {
        if (branchingHallwayPrefab != null)
        {
            var temp = Instantiate(branchingHallwayPrefab);
            var bc = temp.GetComponent<BranchingHallwayConnector>();
            if (bc == null || bc.forwardExit == null || bc.leftExit == null || bc.rightExit == null)
                Debug.LogError("BranchingHallwayPrefab missing one or more exits!");
            Destroy(temp);
        }
    }
}
