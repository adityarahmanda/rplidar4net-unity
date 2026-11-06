using Rahmanda.RpLidar4Net;
using RPLidar4Net.Core;
using UnityEngine;

public class SampleScript : MonoBehaviour
{
    [SerializeField] private RpLidar lidar;

    private void Awake()
    {
        lidar.onScanFrame.AddListener(onNewScan);
    }

    private void onNewScan(Point[] points)
    {
        if (points != null && points.Length > 0)
        {
            var lastPoint = points[points.Length - 1];
            Debug.Log($"Received scan frame with {points.Length} points. Last scan point: {lastPoint.Angle}, {lastPoint.Distance}");
        }
        else
        {
            Debug.Log("Received scan frame with no points.");
        }
    }
}
