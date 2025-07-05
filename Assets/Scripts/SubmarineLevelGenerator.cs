using UnityEngine;

public class SubmarineLevelGenerator : MonoBehaviour
{
    public GameObject[] puzzleRooms;
    public GameObject hallwayPrefab;
    public Transform startPoint;
    public int numRooms = 5;

    void Start()
    {
        Transform currentPoint = startPoint;
        for (int i = 0; i < numRooms; i++)
        {
            GameObject room = Instantiate(
                puzzleRooms[Random.Range(0, puzzleRooms.Length)],
                currentPoint.position,
                currentPoint.rotation
            );

            RoomConnector connector = room.GetComponent<RoomConnector>();
            currentPoint = connector.exitPoint;
        }

        // Final escape room (hallway with door prefab)
        GameObject escape = Instantiate(
            hallwayPrefab,
            currentPoint.position,
            currentPoint.rotation
        );
        escape.name = "EscapeRoom";
    }
}
