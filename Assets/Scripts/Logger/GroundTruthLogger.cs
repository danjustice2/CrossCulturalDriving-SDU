using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GroundTruthLogger
{
    private Dictionary<ParticipantOrder, NetworkVehicleController> _vehicles;
    private Dictionary<ParticipantOrder, List<float>> _speeds;
    private Dictionary<ParticipantOrder, bool> _horn;

    private Dictionary<ParticipantOrder, bool> _leftSignal;
    private Dictionary<ParticipantOrder, bool> _rightSignal;
    private Dictionary<ParticipantOrder, bool> _hazardLights;
    
    private Dictionary<ParticipantOrder, bool> _breaks;



    // Start is called before the first frame update
    public void Init()
    {
    }

    public void StartScenario(ParticipantOrder[] participantstoCollectFor)
    {
        if (_vehicles == null) _vehicles = new Dictionary<ParticipantOrder, NetworkVehicleController>();
        _vehicles.Clear();

        if (_speeds == null) _speeds = new Dictionary<ParticipantOrder, List<float>>();
        _speeds.Clear();

        if (_horn == null) _horn = new Dictionary<ParticipantOrder, bool>();
        _horn.Clear();

        if (_leftSignal == null) _leftSignal = new Dictionary<ParticipantOrder, bool>();
        _leftSignal.Clear();
        if (_rightSignal == null) _rightSignal = new Dictionary<ParticipantOrder, bool>();
        _rightSignal.Clear();
        if (_hazardLights == null) _hazardLights = new Dictionary<ParticipantOrder, bool>();
        _hazardLights.Clear();
        
        if (_breaks == null) _breaks = new Dictionary<ParticipantOrder, bool>();
        _breaks.Clear();

        foreach (ParticipantOrder po in participantstoCollectFor)
        {
            if (po == ParticipantOrder.None) continue;
            _vehicles.Add(po, null);
            _speeds.Add(po, new List<float>());
            _horn.Add(po, false);
            _leftSignal.Add(po, false);
            _rightSignal.Add(po, false);
            _hazardLights.Add(po, false);
            _breaks.Add(po, false);
        }
    }


    public void GatherGroundTruth(ref ScenarioLog log)
    {
        log.facts.Add("scenario length", (log.endTime - log.startTime).ToString());
        foreach (var po in _vehicles.Keys)
        {
            if (_speeds[po].Count() > 1)
            {
                log.facts.Add(po + " Average Speed: ", _speeds[po].Average().ToString("F4"));
                log.facts.Add(po + " Standard deviation Speed: ", _speeds[po].StandardDeviation().ToString("F4"));
            }
            else
            {
                log.facts.Add(po + " Speed: ", "Failed to collect speed data.");
            }

            log.facts.Add(po + " Used Horn: ", _horn[po] ? "Yes" : "No");

            log.facts.Add(po + " Left indicator use: ", _leftSignal[po] ? "Yes" : "No");
            log.facts.Add(po + " Right indicator use: ", _rightSignal[po] ? "Yes" : "No");
            log.facts.Add(po + " Hazard indicator use: ", _hazardLights[po] ? "Yes" : "No");
            
            log.facts.Add(po + " break used: ", _breaks[po] ? "Yes" : "No");
            
        }
    }


    // Update is called once per frame
    public void Update()
    {
        foreach (var po in _vehicles.Keys)
        {
            if (_vehicles[po] == null)
            {
                _vehicles[po] = ConnectionAndSpawing.Singleton
                    .GetClientObject(po, ConnectionAndSpawing.ParticipantObjectSpawnType.CAR)
                    ?.GetComponent<NetworkVehicleController>();
            }

            if (_vehicles[po] != null)
            {
                _speeds[po].Add(_vehicles[po].CurrentSpeed.Value);
                if (SteeringWheelManager.Singleton.GetButtonInput(po))
                {
                    _horn[po] = true;
                }

                _vehicles[po].GetIndicatorState(out bool left, out bool right);
                if (left) _leftSignal[po] = true;
                if (right) _rightSignal[po] = true;
                if (left && right) _hazardLights[po] = true;

                if (SteeringWheelManager.Singleton.GetAccelInput(po) < 0)
                {
                    _breaks[po] = true;
                }
                
            }
        }
    }
}

//From https://stackoverflow.com/a/6252351
public static class Extend
{
    public static float StandardDeviation(this IEnumerable<float> values)
    {
        double avg = values.Average();
        return (float) Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
    }
}