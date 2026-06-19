using UnityEngine;

public class OutletManager : MonoBehaviour
{

    public OutletEvent Event { get => outletEvent; }
    private OutletEvent outletEvent;

    private static OutletManager instance;


    public static OutletManager Instance { get { return instance; } }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); } else { instance = this; }

        outletEvent = gameObject.GetComponent<OutletEvent>();
    }


    private void Start()
    {
        outletEvent.StartOutlet();
    }

    /*private float elapsed_time;
    private void Update()
    {
        elapsed_time += Time.deltaTime;
        if (elapsed_time >= 2.0f && outletEvent.Started)
        {
            outletEvent.SendEvent("TestEvent");
            elapsed_time = 0.0f;
        }
    }*/
}
