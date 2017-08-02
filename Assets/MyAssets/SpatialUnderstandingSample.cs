using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using HoloToolkit.Unity;
using HoloToolkit.Unity.SpatialMapping;

public class SpatialUnderstandingSample : MonoBehaviour
{
    public GameObject toPlaceObj;
    // Consts
    public float kMinAreaForStats = 5.0f;
    public float kMinAreaForComplete = 10.0f;
    public float kMinHorizAreaForComplete = 10.0f;
    public float kMinWallAreaForComplete = 10.0f;

    private bool _triggered = false;
    private Vector3 boxFullDims = new Vector3(1f, 1f, 1f);

    public bool DoesScanMeetMinBarForCompletion
    {
        get
        {
            // Only allow this when we are actually scanning
            if ((SpatialUnderstanding.Instance.ScanState != SpatialUnderstanding.ScanStates.Scanning) ||
                (!SpatialUnderstanding.Instance.AllowSpatialUnderstanding))
            {
                return false;
            }

            // Query the current playspace stats
            IntPtr statsPtr = SpatialUnderstanding.Instance.UnderstandingDLL.GetStaticPlayspaceStatsPtr();
            if (SpatialUnderstandingDll.Imports.QueryPlayspaceStats(statsPtr) == 0)
            {
                return false;
            }
            SpatialUnderstandingDll.Imports.PlayspaceStats stats = SpatialUnderstanding.Instance.UnderstandingDLL.GetStaticPlayspaceStats();

            // Check our preset requirements
            if ((stats.TotalSurfaceArea > kMinAreaForComplete) ||
                (stats.HorizSurfaceArea > kMinHorizAreaForComplete) ||
                (stats.WallSurfaceArea > kMinWallAreaForComplete))
            {
                return true;
            }
            return false;
        }
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

        print(SpatialUnderstanding.Instance.ScanState.ToString());

        if (DoesScanMeetMinBarForCompletion && (SpatialUnderstanding.Instance.ScanState == SpatialUnderstanding.ScanStates.Scanning) &&
    !SpatialUnderstanding.Instance.ScanStatsReportStillWorking)
        {
            print("FinishScan Requested");
            SpatialUnderstanding.Instance.RequestFinishScan();
        }

        if (!_triggered && SpatialUnderstanding.Instance.ScanState == SpatialUnderstanding.ScanStates.Done)
        {
            print("Triggerd");

            _triggered = true;
            CreateScene();
        }
    }

    void CreateScene()
    {

        // DLLの初期化
        SpatialUnderstandingDllObjectPlacement.Solver_Init();

        var halfBoxDims = boxFullDims * .5f;
        // 他のオブジェクトから離す距離
        var disctanceFromOtherObjects = halfBoxDims.x > halfBoxDims.z ? halfBoxDims.x * 3f : halfBoxDims.z * 3f;
        // 作成したいオブジェクトの数
        var desiredLocationCount = 3;

        for (int i = 0; i < desiredLocationCount; ++i)
        {
            // ルールの作成（複数追加可能）
            var placementRules = new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule>();
            placementRules.Add(SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule.Create_AwayFromOtherObjects(disctanceFromOtherObjects));

            // 制約の作成（複数追加可能）
            var placementConstraints = new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementConstraint>();
            placementConstraints.Add(SpatialUnderstandingDllObjectPlacement.ObjectPlacementConstraint.Create_AwayFromOtherObjects());

            // 定義の作成（１つだけ）
            SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition placementDefinition = SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition.Create_OnFloor(halfBoxDims);

            int ret = SpatialUnderstandingDllObjectPlacement.Solver_PlaceObject(
                "my placement",
                SpatialUnderstanding.Instance.UnderstandingDLL.PinObject(placementDefinition),
                placementRules.Count,
                SpatialUnderstanding.Instance.UnderstandingDLL.PinObject(placementRules.ToArray()),
                placementConstraints.Count,
                SpatialUnderstanding.Instance.UnderstandingDLL.PinObject(placementConstraints.ToArray()),
                SpatialUnderstanding.Instance.UnderstandingDLL.GetStaticObjectPlacementResultPtr()
            );

            if (ret > 0)
            {
                SpatialUnderstandingDllObjectPlacement.ObjectPlacementResult placementResult = SpatialUnderstanding.Instance.UnderstandingDLL.GetStaticObjectPlacementResult();

                var rotation = Quaternion.LookRotation(placementResult.Forward, Vector3.up);
                var obj = Instantiate(toPlaceObj, placementResult.Position, rotation);

                print("Placed:" + obj.transform.position);
            }
        }
    }
}
