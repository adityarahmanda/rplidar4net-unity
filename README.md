# RPLidar4Net For Unity

[RPLidar4Net](https://github.com/aberteau/RPLidar4Net) for Unity. It allows you to connect to and process data from the RPLidar sensor.

## Installation

### Install via Clone

Clone this project using this command line

```sh
git clone https://github.com/adityarahmanda/rplidar4net-unity.git
```

### Install via Git Url

1. Open Unity Package Manager

2. Click `+`

3. Select `Add package from git URL...`

4. Paste this URL

```
https://github.com/adityarahmanda/rplidar4net-unity.git?path=/Packages/com.adityarahmanda.rplidar4net
```

### Install via Tarball Releases

1. Go to [Releases](https://github.com/adityarahmanda/rplidar4net-unity/releases) page

2. Download `.tgz` from the latest release

3. In Unity, open Window/Package Manager

    - Click `+`
    - Select `Add package by tarball...`
    - Select the downloaded file to import it

### Install via Scoped Registry

1. In Unity, Open `Edit -> Project Settings -> Package Manager`

2. Add a new Scoped Registry (or edit the existing OpenUPM entry)

    |Label|Value|
    |----|----|
    |Name|Aditya Rahmanda|
    |URL|https://package.openupm.com|
    |Scope(s)|com.adityarahmanda|

3. Click `Save` or `Apply`

4. Open Window/Package Manager

5. There are several ways to add the package

    - Go to My Registries section, 
    - Find `Aditya Rahmanda`'s `RPLidar4Net` package 
    - Click `Install` Button
  
    or

    - Click `+`
    - Select `Add package by name...`
    - Paste `com.adityarahmanda.rplidar4net` into name
    - Click Add

### Use in a Unity script

Add a `RpLidar` component to a GameObject and configure its serial port and baud rate in the Inspector matched with the RPLidar sensor specs. It connects on `Awake` by default and raises `onScanFrame` when a scan arrives:

```csharp
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

```
