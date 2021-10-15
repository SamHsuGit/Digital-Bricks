using UnityEngine;

public class AnimEvents : MonoBehaviour
{
    public AudioSource footStep;
    public AudioSource motor;
    public GameObject rearWheelLeft;
    public GameObject rearWheelRight;
    public GameObject stud;
    public bool rebuilding = true;

    GameObject[] spawnedObjectsLeft;
    GameObject[] spawnedObjectsRight;

    private void Step()
    {
        //footStep.Play(); // buggy sounds clipping
    }

    private void FinishedRebuild()
    {
        //rebuilding = false;
    }

    private void Drive()
    {
        //motor.Play();


        // would be better to use a particle system...
        //for(int i=0; i < 5; i++)
        //{
        //    spawnedObjectsLeft[i] = Instantiate(stud, rearWheelLeft.transform);
        //    spawnedObjectsRight[i] = Instantiate(stud, rearWheelRight.transform);
        //}

        //for(int i = 0; i < 5; i++)
        //{
        //    Destroy(spawnedObjectsLeft[i], 1f);
        //    Destroy(spawnedObjectsRight[i], 1f);
        //}
        
    }
}
