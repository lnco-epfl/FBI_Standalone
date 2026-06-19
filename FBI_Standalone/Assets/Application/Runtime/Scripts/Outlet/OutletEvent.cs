using System.Collections.Generic;
using System.Transactions;
using LSL4Unity.Utils;
using UnityEngine;

public class OutletEvent : AStringOutlet
{

    public override List<string> ChannelNames
    {
        get
        {
            List<string> chanNames = new List<string> { "Event" };
            return chanNames;
        }
    }

    public bool Started { get => started; }


    private bool started = false;

    private bool buildSample = false;

    public string CurrentEvent { get => currentEvent; }
    private string currentEvent;

    public void Reset()
    {
        StreamName = "FBI.Event";
        StreamType = "Markers";
        moment = MomentForSampling.EndOfFrame;
        IrregularRate = true;
    }

    public void StartOutlet()
    {
        base.Start();

        started = true;
        currentEvent = string.Empty;
    }

    public void SendEvent(string str)
    {

        if (currentEvent == string.Empty && str != string.Empty)
        {
            EventFileManager.Log($"[OutletEvent] Send Event {str}");

            buildSample = true;
            currentEvent = str;
        }
    }

    protected override bool BuildSample()
    {
        if(buildSample)
        {

            buildSample = false;

            sample[0] = currentEvent;

            currentEvent = string.Empty;

            return true;
        }

        return false;
    }
}
