﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;


public class SeatCalibration : MonoBehaviour {
    public enum SearCalibrationState {
        NONE,
        STARTCALIBRATING,
        CALIBRATING,
        FINISHED,
        READY,
        ERROR,
        DEFAULT,
        FAILED
    }

    public OVRCustomSkeleton HandModelL;
    public OVRCustomSkeleton HandModelR;

    public Transform steeringWheelCenter;

    SearCalibrationState callibrationState = SearCalibrationState.NONE;

    private Vector3 OriginalPosition;


    void Start() { OriginalPosition = transform.position; }

#if UNITY_EDITOR
    void OnGUI() {
        GUIStyle gs = new GUIStyle();
        gs.fontSize = 30;
        gs.normal.textColor = Color.white;
        string displayString = "";
        switch (callibrationState) {
            case SearCalibrationState.STARTCALIBRATING:
                displayString = "Starting Callibration!";
                gs.normal.textColor = Color.green;
                break;
            case SearCalibrationState.CALIBRATING:
                displayString = "Callibrating! HOLD STILL!" + (callibrationTimer).ToString("F1");
                gs.normal.textColor = Color.red;
                break;
            case SearCalibrationState.FINISHED:
                displayString = "Finished!";
                gs.normal.textColor = Color.green;
                break;
            case SearCalibrationState.READY:
                displayString = "ready";
                gs.normal.textColor = Color.green;
                break;
            case SearCalibrationState.ERROR: break;
            case SearCalibrationState.DEFAULT:
                displayString = "Not Done Yet!";
                gs.normal.textColor = Color.red;
                break;
            default:
                break;
        }

        GUI.Label(new Rect(610, 10, 600, 300), displayString, gs);
    }
#endif


    private Transform cam;
    private ParticipantInputCapture myPic;

    public void StartCalibration(Transform SteeringWheel, Transform camera, ParticipantInputCapture pic) {
        if (callibrationState != SearCalibrationState.CALIBRATING ||
            callibrationState != SearCalibrationState.STARTCALIBRATING) {
            steeringWheelCenter = SteeringWheel;
            cam = camera;
            myPic = pic;
            if (HandModelL == null || HandModelR == null) {
                foreach (var h in transform.GetComponentsInChildren<OVRCustomSkeleton>()) {
                    if (h.GetSkeletonType() == OVRSkeleton.SkeletonType.HandLeft) { HandModelL = h; }
                    else if (h.GetSkeletonType() == OVRSkeleton.SkeletonType.HandRight) { HandModelR = h; }
                }
            }

            callibrationState = SearCalibrationState.STARTCALIBRATING;
        }
    }

    private float callibrationTimer = 0;
    int ReTryCount = 0;

    void Update() {
        if (cam == null ||
            steeringWheelCenter == null ||
            HandModelL == null ||
            HandModelR == null ||
            myPic == null) {
            if (callibrationState == SearCalibrationState.STARTCALIBRATING) {
                Debug.Log("calibrationScript is not Running something is missing");
            }

            return;
        }


        switch (callibrationState) {
            case SearCalibrationState.NONE: break;
          
            case SearCalibrationState.STARTCALIBRATING:
                OVRPlugin.RecenterTrackingOrigin(OVRPlugin.RecenterFlags.Default);
                Quaternion rotation = Quaternion.FromToRotation(cam.forward, steeringWheelCenter.parent.forward);
                Debug.Log("rotation.eulerAngles.y" + Quaternion.Euler(0, rotation.eulerAngles.y, 0));

                myPic.SetNewRotationOffset(Quaternion.Euler(0, rotation.eulerAngles.y, 0));
                callibrationState = SearCalibrationState.CALIBRATING;
                callibrationTimer = 5f;
                break;
            case SearCalibrationState.CALIBRATING:

                if (HandModelL.IsDataHighConfidence && HandModelR.IsDataHighConfidence) {
                    // if this does not work we might need to look further for getting the right bone
                    Vector3 A = HandModelL.Bones[9].Transform.position; //HandModelL.transform.position;
                    Vector3 B = HandModelR.Bones[9].Transform.position; //HandModelR.transform.position;
                    Vector3 AtoB = B - A;

                    Vector3 transformDifference = (A + (AtoB * 0.5f)) - steeringWheelCenter.position;
                    if (transformDifference.magnitude > 100) {
                        Debug.Log(transformDifference.magnitude);
                        callibrationState = SearCalibrationState.ERROR;
                    }

                    myPic.SetNewPositionOffset(-transformDifference);
                    Debug.Log("transformDifference" + (-transformDifference).ToString());
                }

                if (callibrationTimer > 0) { callibrationTimer -= Time.deltaTime; }
                else { callibrationState = SearCalibrationState.FINISHED; }

                break;
            case SearCalibrationState.FINISHED:
                myPic.FinishedCalibration();
                callibrationState = SearCalibrationState.READY;
                break;
            case SearCalibrationState.READY: break;
            case SearCalibrationState.ERROR:
                if (ReTryCount > 10) {
                    if (!myPic.DeleteCallibrationFile()) {
                        Debug.LogWarning("Could not delete calibration file. The data in that file is probably corrupt. Please consider removing the file manually.");
                    }
                    Debug.LogError("Had 10 retries calibrating the play. Did not work. Quitting.");
                    Application.Quit();
                }
                else {
                    Debug.Log("Encountered a Calibration Error. Resetting Offsets and trying again try: " +
                              ReTryCount.ToString());

                    myPic.SetNewRotationOffset(Quaternion.identity);
                    myPic.SetNewPositionOffset(Vector3.zero);
                    ReTryCount++;
                    callibrationState = SearCalibrationState.STARTCALIBRATING;
                }

                break;
            case SearCalibrationState.DEFAULT: break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
}