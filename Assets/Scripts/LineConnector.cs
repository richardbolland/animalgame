using UnityEngine;

public class LineConnector : MonoBehaviour
{
    public LineRenderer myLine;
    public Transform startPoint;
    public Transform middlePoint;
    public Transform endPoint;

    void Update()
    {
        // This makes sure we don't get errors if something is missing
        if (myLine != null && startPoint != null && endPoint != null)
        {
            // Set the first point (Index 0) to the start object's position
            myLine.SetPosition(0, startPoint.position);

            // Set the first point (Index 0) to the start object's position
            myLine.SetPosition(1, middlePoint.position);
            
            // Set the third point (Index 2) to the end object's position
            myLine.SetPosition(2, endPoint.position);
        }
    }
}