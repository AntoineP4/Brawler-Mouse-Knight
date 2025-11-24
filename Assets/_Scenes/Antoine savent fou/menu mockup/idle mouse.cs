using UnityEngine;

public class ForceIdle : MonoBehaviour
{
    public string anim = "Idle";
    Animator animr;

    void Awake()
    {
        animr = GetComponent<Animator>();
    }

    void Update()
    {
        animr.Play(anim, 0, 0);
    }
}
