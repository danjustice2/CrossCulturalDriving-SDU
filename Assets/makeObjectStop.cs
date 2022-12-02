using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class makeObjectStop : MonoBehaviour
{
    private GameObject cyclist;

    private bool isPlayerInZone = false;
    private bool isCyclistInZone = false;

    public void Start()
    {
        this.cyclist = GameObject.FindWithTag("Cyclist");
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Cyclist")
        {
            this.isCyclistInZone= true;
        }

        if (other.tag == "Player")
        {
            this.isPlayerInZone = true;
        }


        if (this.isPlayerInZone && this.isCyclistInZone)
        {
            Debug.Log("Stopping cyclist because both are in the zone");
            MakeCyclistMove(false);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.tag == "Cyclist")
        {
            this.isCyclistInZone = false;
        }

        if (other.CompareTag("Player"))
        {
            this.isPlayerInZone = false;
        }

        if (!this.isPlayerInZone || !this.isCyclistInZone)
        {
            Debug.Log("Making cyclist move again somehow");
            MakeCyclistMove(true);
        }
    }

    private void MakeCyclistMove(bool shouldMove)
    {
        //this.cyclist.GetComponent<BezierSolution.BezierWalkerWithTime>().shouldMove = shouldMove;
        cyclist.GetComponent<BezierSolution.BezierWalkerWithTime>().enabled = shouldMove; // I've tested this and disabling the component script seems to have the desired effect without throwing an error -Dan
        
    }
}
