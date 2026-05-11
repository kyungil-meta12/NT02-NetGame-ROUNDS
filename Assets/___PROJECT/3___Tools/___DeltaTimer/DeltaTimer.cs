using System.Threading;
using UnityEngine;

public enum CheckOption
{
    Resume,
    Stop,
    Reset,
    StopReset,
    Interpolate,
}

public class DeltaTimer
{
    public float time{ get; private set; } = 0f;
    public bool isRunning{ get; private set; } = true;

    public void Update()
    {
        if(isRunning) 
        {
            time += Time.deltaTime;
        }
    }

    public void SetTime(float val)
    {
        time = val;
    }

    public void SetRunningState(bool flag)
    {
        isRunning = flag;
    }

    public void Reset()
    {
        time = 0f;
    }

    public void Interpolate(float val)
    {
        time -= val;
    }

    public bool CheckTime(float destTime, CheckOption opt)
    {
        if(time >= destTime)
        {
            switch(opt)
            {
                case CheckOption.Resume:
                    break;
                
                case CheckOption.Reset:
                    time = 0f;
                    break;
                
                case CheckOption.Stop:
                    isRunning = false;
                    break;

                case CheckOption.StopReset:
                    time = 0f;
                    isRunning = false;
                    break;

                case CheckOption.Interpolate:
                    time -= destTime;
                    break;
            }

            return true;
        }

        return false;
    }
}