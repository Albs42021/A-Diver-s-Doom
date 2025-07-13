using UnityEngine;
using System.Collections.Generic;

public class BranchingHallwayConnector : MonoBehaviour
{
    public Transform forwardExit;
    public Transform leftExit;
    public Transform rightExit;

    public List<Transform> GetAllExits()
    {
        List<Transform> exits = new List<Transform>();
        if (forwardExit != null) exits.Add(forwardExit);
        if (leftExit != null) exits.Add(leftExit);
        if (rightExit != null) exits.Add(rightExit);
        return exits;
    }
}
