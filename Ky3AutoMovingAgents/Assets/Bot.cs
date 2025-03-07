using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

public class Bot : MonoBehaviour
{
    NavMeshAgent agent;
    public GameObject target;

    // Start is called before the first frame update
    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();
    }

    void Seek(Vector3 location)
    {
        agent.SetDestination(location);

    void Flee(Vector3 location)
    {
        Vector3 fleeVector = location - this.transform.position;
        agent.SetDestination(this.transform.position - fleeVector);
    }

    void Evade()
    {
        Vector3 targetDir = target.transform.position - this.transform.position;

        float lookAhead = targetDir.magnitude / (agent.speed + target.GetComponent<Drive>().currentSpeed);
        Flee(target.transform.position + target.transform.forward * lookAhead);
    }

    void Pursue()
    {
        Vector3 targetDir = target.transform.position - this.transform.position;

        float relativeHeading = Vector3.Angle(this.transform.forward, this.transform.TransformVector(target.transform.forward));
        float toTarget = Vector3.Angle(this.transform.forward, this.transform.TransformVector(targetDir));

        if ((toTarget > 90 && relativeHeading > 20) || target.GetComponent<Drive>().speed < 0.001f)
        {
            Seek(target.transform.position);
            return;
        }

        float lookAhead = targetDir.magnitude / (agent.speed + target.GetComponent<Drive>().currentSpeed);
        Seek(target.transform.position + target.transform.forward * lookAhead);
    }

    Vector3 wanderTarget = Vector3.zero;
    void Wander()
    {
        float wanderRad = 10;
        float wanderDis = 10;
        float wanderJit = 1;

        wanderTarget += new Vector3(Random.Range(-1.0f, 1.0f) * wanderJit, 0, Random.Range(-1.0f, 1.0f) * wanderJit);

        wanderTarget.Normalize();
        wanderTarget *= wanderRad;

        Vector3 targetLocal = wanderTarget + new Vector3(0, 0, wanderDis);
        Vector3 targetWorld = this.gameObject.transform.InverseTransformVector(targetLocal);

        Seek(targetWorld);
    }

    void Hide()
    {
        float dist = Mathf.Infinity;
        Vector3 chosenSpot = Vector3.zero;

        for (int i = 0; i < World.Instance.GetHidingSpots().Length; i++)
        {
            Vector3 hideDir = World.Instance.GetHidingSpots()[i].transform.position - target.transform.position;
            Vector3 hidePos = World.Instance.GetHidingSpots()[i].transform.position + hideDir.normalized * 3;

            if (Vector3.Distance(this.transform.position, hidePos) < dist)
            {
                chosenSpot = hidePos;
                dist = Vector3.Distance(this.transform.position, hidePos);
            }
        }

        Seek(chosenSpot);
    }

    void CleverHide()
    {
        float dist = Mathf.Infinity;
        Vector3 chosenSpot = Vector3.zero;
        Vector3 chosenDir = Vector3.zero;
        GameObject chosenGO = World.Instance.GetHidingSpots()[0];

        for (int i = 0; i < World.Instance.GetHidingSpots().Length; i++)
        {
            Vector3 hideDir = World.Instance.GetHidingSpots()[i].transform.position - target.transform.position;
            Vector3 hidePos = World.Instance.GetHidingSpots()[i].transform.position + hideDir.normalized * 3;

            if (Vector3.Distance(this.transform.position, hidePos) < dist)
            {
                chosenSpot = hidePos;
                chosenDir = hideDir;
                chosenGO = World.Instance.GetHidingSpots()[i];
                dist = Vector3.Distance(this.transform.position, hidePos);
            }
        }

        Collider hideCol = chosenGO.GetComponent<Collider>();
        Ray backRay = new Ray(chosenSpot, -chosenDir.normalized);
        RaycastHit info;
        float distance = 250.0f;
        hideCol.Raycast(backRay, out info, distance);

        Seek(info.point + chosenDir.normalized * 5);
    }

    bool CanSeeTarget()
    {
        RaycastHit raycastInfo;
        Vector3 rayToTarget = target.transform.position - this.transform.position;
        float lookAngle = Vector3.Angle(this.transform.forward, rayToTarget);
        if (Physics.Raycast(this.transform.position, rayToTarget, out raycastInfo) && lookAngle < 85)
        {
            if(raycastInfo.transform.gameObject.tag == "cop") { return true;  }
        }

        return false;
    }

    bool CanSeeMe()
    {
        Vector3 rayToMe = this.transform.position - target.transform.position;
        float lookAngle = Vector3.Angle(target.transform.forward, rayToMe);
        
        if (lookAngle < 65)
        {
            return true;
        }

        return false;
    }

    bool coolDown = false;
    void BehaviorCooldown()
    {
        coolDown = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!coolDown)
        {
            if (Vector3.Distance(transform.position, target.transform.position) < 10)
            {
                if (CanSeeTarget() && CanSeeMe())
                {
                    CleverHide();
                    coolDown = true;
                    Invoke("BehaviorCooldown", 5);
                }
                else if (!CanSeeMe())
                {
                    Pursue();
                }
            }
            else
            {
                Wander();
            }
        }
    }
}
