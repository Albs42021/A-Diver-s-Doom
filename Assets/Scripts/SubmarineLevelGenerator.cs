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

    private bool escapePlaced = false;

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

                int correctBranch = Random.Range(0, 2); // 0 for forward, 1 for side

                if (correctBranch == 0)
                {
                    currentPoint = splitConnector.forwardExit;
                    GenerateBranch(splitConnector.sideExit, Random.Range(branchDepthMin, branchDepthMax + 1));
                }
                else
                {
                    GenerateBranch(splitConnector.forwardExit, Random.Range(branchDepthMin, branchDepthMax + 1));
                    currentPoint = splitConnector.sideExit;
                }
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

                int correctBranch = Random.Range(0, 3); // 0 = forward, 1 = left, 2 = right

                Transform forward = branchingConnector.forwardExit;
                Transform left = branchingConnector.leftExit;
                Transform right = branchingConnector.rightExit;

                if (correctBranch == 0)
                {
                    currentPoint = forward;
                    GenerateBranch(left, Random.Range(branchDepthMin, branchDepthMax + 1));
                    GenerateBranch(right, Random.Range(branchDepthMin, branchDepthMax + 1));
                }
                else if (correctBranch == 1 && left != null)
                {
                    GenerateBranch(forward, Random.Range(branchDepthMin, branchDepthMax + 1));
                    currentPoint = left;
                    GenerateBranch(right, Random.Range(branchDepthMin, branchDepthMax + 1));
                }
                else if (correctBranch == 2 && right != null)
                {
                    GenerateBranch(forward, Random.Range(branchDepthMin, branchDepthMax + 1));
                    GenerateBranch(left, Random.Range(branchDepthMin, branchDepthMax + 1));
                    currentPoint = right;
                }
                else
                {
                    currentPoint = forward;
                    GenerateBranch(left, Random.Range(branchDepthMin, branchDepthMax + 1));
                    GenerateBranch(right, Random.Range(branchDepthMin, branchDepthMax + 1));
                }
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
                int correctBranch = Random.Range(0, 2);

                if (correctBranch == 0 && splitConnector.forwardExit != null)
                {
                    GenerateBranch(splitConnector.forwardExit, depth - 1);
                    GenerateBranch(splitConnector.sideExit, 0);
                }
                else if (splitConnector.sideExit != null)
                {
                    GenerateBranch(splitConnector.sideExit, depth - 1);
                    GenerateBranch(splitConnector.forwardExit, 0);
                }
            }
        }
        else
        {
            GameObject branching = Instantiate(branchingHallwayPrefab, current.position, current.rotation);
            BranchingHallwayConnector branchingConnector = branching.GetComponent<BranchingHallwayConnector>();

            if (branchingConnector != null)
            {
                int correctBranch = Random.Range(0, 3);

                if (correctBranch == 0 && branchingConnector.forwardExit != null)
                {
                    GenerateBranch(branchingConnector.forwardExit, depth - 1);
                    GenerateBranch(branchingConnector.leftExit, 0);
                    GenerateBranch(branchingConnector.rightExit, 0);
                }
                else if (correctBranch == 1 && branchingConnector.leftExit != null)
                {
                    GenerateBranch(branchingConnector.leftExit, depth - 1);
                    GenerateBranch(branchingConnector.forwardExit, 0);
                    GenerateBranch(branchingConnector.rightExit, 0);
                }
                else if (correctBranch == 2 && branchingConnector.rightExit != null)
                {
                    GenerateBranch(branchingConnector.rightExit, depth - 1);
                    GenerateBranch(branchingConnector.forwardExit, 0);
                    GenerateBranch(branchingConnector.leftExit, 0);
                }
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
