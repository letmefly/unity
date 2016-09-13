using UnityEngine;
using System.Collections;

public class TestFSM : MonoBehaviour {
    static int a = 0;
    private FSM mFSM;
	// Use this for initialization
	void Start () {
        mFSM = new FSM();
        FSM.State state = mFSM.AddState("A");
        state.SetEnterMethod(delegate() { Debug.Log("Enter A " + a); });
        state.SetTickMethod(delegate(float dt) { });
        state.SetExitMethod(delegate() { Debug.Log("Exit A " + a); });

        state = mFSM.AddState("B");
        state.SetEnterMethod(delegate() { Debug.Log("Enter B" + a); });
        state.SetTickMethod(delegate(float dt) { });
        state.SetExitMethod(delegate() { Debug.Log("Exit B " + a); });

        mFSM.addTransition("A", "B", "!(A2B1 & (A2B1 | A2B2)) & !OK");
        mFSM.addTransition("B", "A", "(B2A | B2A)");

        mFSM.Begin();

        InvokeRepeating("Test", 3.0f, 6.0f);
	}
	
	// Update is called once per frame
	void Update () {
        mFSM.Evaluate();
        mFSM.Tick(Time.deltaTime);
	}

    void Test()
    {
        Debug.Log("\n-------Test" + a++ + "-------");
        if (a % 2 == 0){
            //mFSM.SetCondition("A2B", true);
            //mFSM.PulseCondition("A2B1");
            //mFSM.PulseCondition("A2B2");
            //mFSM.PulseCondition("OK");
            mFSM.SetCondition("OK", false);
        }
        else
        {
            mFSM.PulseCondition("B2A");
        }
        
    }

}
