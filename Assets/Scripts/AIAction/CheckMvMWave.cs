using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class CheckMvMWave : Conditional
{
    public int wave;

    public override TaskStatus OnUpdate()
    {
        if (ManvsMachineManager.instan.wave == wave) return TaskStatus.Success;

        return TaskStatus.Failure;
    }
}
