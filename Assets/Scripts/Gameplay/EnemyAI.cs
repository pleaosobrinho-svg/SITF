using System.Collections;
using UnityEngine;

public sealed class EnemyAI : MonoBehaviour
{
    private enum State { Patrol, Chase, Strafe, Attack, Search, Retreat }
    private enum Archetype { Rusher, Rifleman, Heavy, Marksman }

    private State state = State.Patrol;
    private Archetype archetype;
    private FPSPlayer target;
    private CharacterController controller;
    private Transform visualRoot;
    private Transform torso, head, leftArm, rightArm, leftLeg, rightLeg, gun;
    private float health, speed, fireInterval, accuracy, aggroRange, fireTimer, thinkTimer, strafeTimer, patrolTimer, verticalVelocity, hitPulse;
    private int damage, strafeSign;
    private Vector3 lastSeen, patrolPoint;
    private bool dead;

    public bool IsDead => dead;

    public void Initialize(FPSPlayer player, int kind)
    {
        target = player;
        archetype = (Archetype)(kind % 4);
        ConfigureType();
        BuildBody();
        PickPatrolPoint();
    }

    private void ConfigureType()
    {
        switch (archetype)
        {
            case Archetype.Rusher:   health=82f;  speed=4.2f;  fireInterval=0.65f; damage=7;  accuracy=0.55f; aggroRange=55f; break;
            case Archetype.Rifleman: health=110f; speed=3.1f;  fireInterval=0.72f; damage=9;  accuracy=0.68f; aggroRange=68f; break;
            case Archetype.Heavy:    health=180f; speed=2.25f; fireInterval=0.55f; damage=11; accuracy=0.60f; aggroRange=62f; break;
            default:                 health=92f;  speed=2.65f; fireInterval=1.15f; damage=18; accuracy=0.88f; aggroRange=88f; break;
        }
        fireTimer=Random.Range(0.25f,0.9f);
        strafeSign=Random.value>0.5f?1:-1;
    }

    private void BuildBody()
    {
        controller=gameObject.AddComponent<CharacterController>();
        controller.height=1.82f; controller.radius=0.31f; controller.center=new Vector3(0,0.91f,0);
        controller.stepOffset=0.30f; controller.slopeLimit=48f; controller.skinWidth=0.03f;

        visualRoot=new GameObject("VISUAL").transform;
        visualRoot.SetParent(transform,false);

        Color bodyColor=archetype==Archetype.Rusher?new Color(0.72f,0.17f,0.18f):
                         archetype==Archetype.Rifleman?new Color(0.18f,0.36f,0.47f):
                         archetype==Archetype.Heavy?new Color(0.29f,0.31f,0.34f):
                         new Color(0.22f,0.45f,0.31f);
        Color skin=new Color(0.81f,0.57f,0.43f), dark=new Color(0.10f,0.12f,0.13f);

        torso=Part("TORSO",new Vector3(0,0.98f,0),new Vector3(0.76f,0.92f,0.50f),bodyColor);
        head=Part("HEAD",new Vector3(0,1.70f,-0.01f),new Vector3(0.48f,0.48f,0.48f),skin);
        leftArm=Part("L_ARM",new Vector3(-0.51f,0.93f,0),new Vector3(0.18f,0.72f,0.18f),bodyColor);
        rightArm=Part("R_ARM",new Vector3(0.51f,0.93f,0),new Vector3(0.18f,0.72f,0.18f),bodyColor);
        leftLeg=Part("L_LEG",new Vector3(-0.20f,0.30f,0),new Vector3(0.22f,0.72f,0.22f),dark);
        rightLeg=Part("R_LEG",new Vector3(0.20f,0.30f,0),new Vector3(0.22f,0.72f,0.22f),dark);
        gun=Part("GUN",new Vector3(0.34f,1.00f,-0.36f),new Vector3(0.10f,0.12f,0.60f),new Color(0.055f,0.065f,0.07f));
        gun.localRotation=Quaternion.Euler(-4f,0f,0f);
    }

    private Transform Part(string name,Vector3 localPos,Vector3 size,Color color)
    {
        GameObject o=GameObject.CreatePrimitive(PrimitiveType.Cube);
        o.name=name; o.transform.SetParent(visualRoot,false); o.transform.localPosition=localPos; o.transform.localScale=size;
        o.GetComponent<Renderer>().sharedMaterial=SITFGame.Instance.Materials.Get(color,0.05f,0.14f);
        Collider c=o.GetComponent<Collider>(); if(c!=null) Object.Destroy(c);
        return o.transform;
    }

    private void Update()
    {
        if(dead || target==null || !target.IsAlive) return;
        thinkTimer-=Time.deltaTime; fireTimer-=Time.deltaTime; strafeTimer-=Time.deltaTime; patrolTimer-=Time.deltaTime;
        if(thinkTimer<=0f){ thinkTimer=Random.Range(0.11f,0.19f); Think(); }
        Animate();
    }

    private void FixedUpdate()
    {
        if(dead || target==null || !target.IsAlive) return;
        Vector3 flatTarget=new Vector3(target.transform.position.x,transform.position.y,target.transform.position.z);
        float distance=Vector3.Distance(transform.position,flatTarget);

        if(controller.isGrounded && verticalVelocity<0f) verticalVelocity=-1.2f;
        verticalVelocity+=Physics.gravity.y*Time.fixedDeltaTime;

        Vector3 velocity=Vector3.zero;
        if(state==State.Chase) velocity=DesiredDirection(flatTarget)*speed;
        else if(state==State.Strafe)
        {
            Vector3 to=(flatTarget-transform.position).normalized;
            Vector3 side=Vector3.Cross(Vector3.up,to).normalized*strafeSign;
            velocity=DesiredDirection(transform.position+(side+to*0.12f)*3f)*speed;
            if(strafeTimer<=0f){ strafeSign*=-1; strafeTimer=Random.Range(0.45f,0.9f); }
        }
        else if(state==State.Search)
        {
            velocity=DesiredDirection(lastSeen)*speed*0.78f;
            if(Vector3.Distance(transform.position,lastSeen)<1.1f){ state=State.Patrol; PickPatrolPoint(); }
        }
        else if(state==State.Patrol)
        {
            velocity=DesiredDirection(patrolPoint)*speed*0.62f;
            if(patrolTimer<=0f || Vector3.Distance(transform.position,patrolPoint)<1f) PickPatrolPoint();
        }
        else if(state==State.Retreat)
        {
            Vector3 away=(transform.position-flatTarget).normalized;
            velocity=DesiredDirection(transform.position+away*4f)*speed;
            if(distance>15f) state=State.Strafe;
        }

        velocity.y=verticalVelocity;
        controller.Move(velocity*Time.fixedDeltaTime);

        Vector3 look=flatTarget-transform.position; look.y=0f;
        if(look.sqrMagnitude>0.01f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(look),Time.fixedDeltaTime*8f);
        if(state==State.Attack) FireAtPlayer(distance);
    }

    private void Think()
    {
        float distance=Vector3.Distance(transform.position,target.transform.position);
        bool sees=HasLineOfSight();

        if(sees)
        {
            lastSeen=target.transform.position;
            if(distance<=aggroRange)
            {
                if(distance<8.5f && archetype==Archetype.Rusher) state=State.Strafe;
                else if(distance<11f && Random.value<0.45f) state=State.Strafe;
                else if(distance<=AttackRange()) state=State.Attack;
                else state=State.Chase;
            }
            else state=State.Patrol;
        }
        else if(distance<=aggroRange*1.15f && (state==State.Attack || state==State.Chase || state==State.Strafe))
            state=State.Search;

        if(health<MaxHealth()*0.22f && distance<10f && archetype!=Archetype.Heavy) state=State.Retreat;
    }

    private float MaxHealth()=>archetype==Archetype.Heavy?180f:archetype==Archetype.Rifleman?110f:archetype==Archetype.Rusher?82f:92f;
    private float AttackRange()=>archetype==Archetype.Marksman?58f:archetype==Archetype.Rusher?27f:42f;

    private bool HasLineOfSight()
    {
        Vector3 from=transform.position+Vector3.up*1.35f;
        Vector3 to=target.transform.position+Vector3.up*1.25f;
        Vector3 delta=to-from;
        if(delta.sqrMagnitude>aggroRange*aggroRange) return false;
        if(Physics.Raycast(from,delta.normalized,out RaycastHit hit,delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            return hit.collider.GetComponentInParent<FPSPlayer>()==target;
        return true;
    }

    private void FireAtPlayer(float distance)
    {
        if(fireTimer>0f || !HasLineOfSight()) return;
        fireTimer=fireInterval*Random.Range(0.82f,1.20f);
        float distancePenalty=Mathf.Clamp01(distance/AttackRange());
        float hitChance=Mathf.Clamp(accuracy-distancePenalty*0.20f,0.22f,0.96f);
        Vector3 from=transform.position+Vector3.up*1.15f+transform.forward*0.25f;
        Vector3 to=target.transform.position+Vector3.up*1.25f;
        Vector3 aim=(to-from).normalized;
        Vector2 spread=Random.insideUnitCircle*(1f-hitChance)*0.035f;
        aim=(aim+transform.right*spread.x+transform.up*spread.y).normalized;

        if(Physics.Raycast(from,aim,out RaycastHit hit,AttackRange(),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
        {
            FPSPlayer player=hit.collider.GetComponentInParent<FPSPlayer>();
            if(player==target && Random.value<hitChance) player.TakeDamage(damage);
            SITFGame.Instance.SpawnProjectileTrace(from,hit.point,new Color(1f,0.42f,0.18f),0.014f);
        }
        else SITFGame.Instance.SpawnProjectileTrace(from,from+aim*AttackRange(),new Color(1f,0.42f,0.18f),0.008f);
        StartCoroutine(EnemyMuzzleFlash());
    }

    private IEnumerator EnemyMuzzleFlash()
    {
        if(gun==null) yield break;
        GameObject flash=GameObject.CreatePrimitive(PrimitiveType.Cube);
        flash.name="AI_MUZZLE"; flash.transform.SetParent(gun,false); flash.transform.localPosition=new Vector3(0,0,-0.34f); flash.transform.localScale=new Vector3(0.10f,0.10f,0.18f);
        flash.GetComponent<Renderer>().sharedMaterial=SITFGame.Instance.Materials.GetEmissive(new Color(1f,0.55f,0.20f),2.6f);
        Collider c=flash.GetComponent<Collider>(); if(c!=null) Object.Destroy(c);
        Light l=flash.AddComponent<Light>(); l.type=LightType.Point; l.range=1.6f; l.intensity=3.2f; l.color=new Color(1f,0.45f,0.15f);
        yield return new WaitForSeconds(0.04f);
        if(flash!=null) Object.Destroy(flash);
    }

    private Vector3 DesiredDirection(Vector3 point)
    {
        Vector3 dir=point-transform.position; dir.y=0f;
        if(dir.sqrMagnitude<0.04f) return Vector3.zero;
        dir.Normalize();

        Vector3[] checks={transform.forward,(transform.forward+transform.right).normalized,(transform.forward-transform.right).normalized};
        for(int i=0;i<checks.Length;i++)
        {
            if(Physics.SphereCast(transform.position+Vector3.up*0.65f,0.28f,checks[i],out RaycastHit hit,1.4f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                if(hit.collider.GetComponentInParent<FPSPlayer>()==target) continue;
                Vector3 side=Vector3.Cross(Vector3.up,checks[i]).normalized*(i==1?1f:-1f);
                dir=(dir+side*0.85f).normalized; break;
            }
        }
        return dir;
    }

    private void PickPatrolPoint()
    {
        Vector2 ring=Random.insideUnitCircle.normalized*Random.Range(6f,17f);
        patrolPoint=transform.position+new Vector3(ring.x,0,ring.y);
        patrolPoint.x=Mathf.Clamp(patrolPoint.x,-39f,39f);
        patrolPoint.z=Mathf.Clamp(patrolPoint.z,-25f,25f);
        patrolTimer=Random.Range(2.8f,5.4f);
    }

    private void Animate()
    {
        float speed01=Mathf.Clamp01(controller.velocity.magnitude/5f);
        float swing=Mathf.Sin(Time.time*8.5f)*28f*speed01;
        leftLeg.localRotation=Quaternion.Euler(swing,0,0);
        rightLeg.localRotation=Quaternion.Euler(-swing,0,0);
        leftArm.localRotation=Quaternion.Euler(-swing*0.55f-12f,0,-5f);
        rightArm.localRotation=Quaternion.Euler(swing*0.55f-18f,0,5f);
        if(hitPulse>0f){ hitPulse-=Time.deltaTime; visualRoot.localScale=Vector3.one*1.035f; } else visualRoot.localScale=Vector3.one;
    }

    public void TakeDamage(int amount,Vector3 hitPoint,Vector3 shotDirection)
    {
        if(dead) return;
        health-=Mathf.Max(0,amount); hitPulse=0.08f;
        SITFFX.Blood(hitPoint,shotDirection,SITFGame.Instance.Materials);
        if(health<=0f) StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        if(dead) yield break;
        dead=true;
        if(controller!=null) controller.enabled=false;
        SITFGame.Instance.RegisterKill(this,archetype==Archetype.Heavy?160:archetype==Archetype.Marksman?130:100);

        float duration=0.36f; Quaternion start=visualRoot.localRotation;
        Quaternion end=Quaternion.Euler(Random.Range(-90f,-70f),0f,Random.Range(-10f,10f));
        Vector3 startPos=visualRoot.localPosition, endPos=startPos+new Vector3(0,-0.42f,0.16f);
        float t=0f;
        while(t<duration)
        {
            t+=Time.deltaTime; float p=Mathf.SmoothStep(0f,1f,t/duration);
            visualRoot.localRotation=Quaternion.Slerp(start,end,p);
            visualRoot.localPosition=Vector3.Lerp(startPos,endPos,p);
            yield return null;
        }
        yield return new WaitForSeconds(6f);
        Destroy(gameObject);
    }
}
