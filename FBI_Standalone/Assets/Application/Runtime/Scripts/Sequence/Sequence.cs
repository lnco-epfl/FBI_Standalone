using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Sequence", menuName = "Application Data/Sequence")]
public class Sequence : ScriptableObject
{
    [SerializeField]
    public List<SequenceStepWrapper> steps = new List<SequenceStepWrapper>();

    public SequenceStepWrapper AddStep(SequenceStepWrapper.StepType type)
    {
        var wrapper = new SequenceStepWrapper();
        wrapper.stepType = type;
        wrapper.UpdateStepType();
        steps.Add(wrapper);
        return wrapper;
    }

    public void RemoveStep(int index)
    {
        if (index >= 0 && index < steps.Count)
            steps.RemoveAt(index);
    }

    public void MoveStep(int from, int to)
    {
        if (from >= 0 && from < steps.Count && to >= 0 && to < steps.Count)
        {
            var step = steps[from];
            steps.RemoveAt(from);
            steps.Insert(to, step);
        }
    }
}
