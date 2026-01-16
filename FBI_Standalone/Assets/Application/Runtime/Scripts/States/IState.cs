
using System.Collections;

public interface IState
{
    void Enter(SequenceStep sequenceStep);
    IEnumerator Execute();
    void Exit();
}
