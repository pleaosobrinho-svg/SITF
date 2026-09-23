using System.Collections;
using UnityEngine;

public sealed class WeaponSystem : MonoBehaviour
{
    public sealed class WeaponSpec
    {
        public readonly string Name,AudioKey;
        public readonly int Damage,Magazine,Reserve,Pellets;
        public readonly float FireRate,Spread,ReloadTime,Recoil,Range,AimFov;
        public readonly bool Automatic;
        public readonly Color Accent;

        public WeaponSpec(string name,string audioKey,int damage,float fireRate,int magazine,int reserve,float spread,int pellets,bool automatic,float reloadTime,float recoil,float range,float aimFov,Color accent)
        {
            Name=name;AudioKey=audioKey;Damage=damage;FireRate=fireRate;Magazine=magazine;Reserve=reserve;Spread=spread;Pellets=pellets;Automatic=automatic;ReloadTime=reloadTime;Recoil=recoil;Range=range;AimFov=aimFov;Accent=accent;
        }
    }

    readonly WeaponSpec[] specs =
    {
        new WeaponSpec("PISTOL","pistol",32,.26f,15,90,.010f,1,false,1.05f,.075f,85f,54f,new Color(.62f,.64f,.67f)),
        new WeaponSpec("SMG","smg",20,.085f,32,128,.020f,1,true,1.35f,.045f,78f,60f,new Color(.19f,.23f,.27f)),
        new WeaponSpec("RIFLE","rifle",42,.135f,30,120,.008f,1,true,1.50f,.065f,110f,58f,new Color(.25f,.28f,.31f)),
        new WeaponSpec("SHOTGUN","shotgun",13,.82f,8,48,.060f,9,false,1.70f,.11f,46f,63f,new Color(.42f,.31f,.21f)),
        new WeaponSpec("DMR","dmr",58,.42f,14,70,.0045f,1,false,1.55f,.09f,150f,52f,new Color(.35f,.38f,.41f)),
        new WeaponSpec("SNIPER","sniper",100,1.10f,5,30,.0018f,1,false,1.90f,.16f,220f,38f,new Color(.13f,.17f,.21f))
    };

    readonly int[] ammo=new int[6],reserve=new int[6];
    int current;
    bool fireHeld,aiming,reloading;
    float nextFire;
    Transform weaponPivot;
    Camera cameraRef;
    FPSPlayer player;
    AudioSource audioSource;
    MaterialFactory materials;
    GameObject muzzleFlash;

    public int CurrentIndex=>current;
    public string CurrentName=>specs[current].Name;
    public int CurrentAmmo=>ammo[current];
    public int CurrentReserve=>reserve[current];
    public bool IsReloading=>reloading;
    public bool IsAiming=>aiming;
    public WeaponSpec CurrentSpec=>specs[current];

    public void Initialize(FPSPlayer owner,Camera cam,MaterialFactory factory)
    {
        player=owner;cameraRef=cam;materials=factory;
        audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0f;
        for(int i=0;i<specs.Length;i++){ammo[i]=specs[i].Magazine;reserve[i]=specs[i].Reserve;}
        BuildWeaponRoot();current=0;BuildModel(specs[current]);
    }

    void BuildWeaponRoot()
    {
        GameObject root=new GameObject("VIEW_WEAPON");
        root.transform.SetParent(cameraRef.transform,false);
        root.transform.localPosition=new Vector3(.34f,-.29f,.55f);
        root.transform.localRotation=Quaternion.Euler(1,0,0);
        weaponPivot=root.transform;AddHands();
    }

    void AddHands()
    {
        Hand("LEFT_HAND",new Vector3(-.18f,-.08f,-.32f));
        Hand("RIGHT_HAND",new Vector3(.18f,-.08f,-.24f));
    }

    void Hand(string name,Vector3 pos)
    {
        GameObject h=GameObject.CreatePrimitive(PrimitiveType.Cube);h.name=name;h.transform.SetParent(weaponPivot,false);h.transform.localPosition=pos;h.transform.localScale=new Vector3(.13f,.17f,.20f);
        h.GetComponent<Renderer>().sharedMaterial=materials.Get(new Color(.74f,.50f,.36f),0,.05f);
        Object.Destroy(h.GetComponent<Collider>());
    }

    void ClearModel(){for(int i=weaponPivot.childCount-1;i>=0;i--)Object.Destroy(weaponPivot.GetChild(i).gameObject);muzzleFlash=null;}

    public void SwitchWeapon(int index)
    {
        if(reloading||index<0||index>=specs.Length||index==current)return;
        StartCoroutine(SwitchRoutine(index));
    }

    IEnumerator SwitchRoutine(int index)
    {
        reloading=true;
        yield return AnimatePose(new Vector3(.15f,-.22f,.30f),new Vector3(-20,20,-7),.09f);
        current=index;ClearModel();BuildModel(specs[current]);
        yield return AnimatePose(new Vector3(.34f,-.29f,.55f),new Vector3(1,0,0),.13f);
        reloading=false;
    }

    public void SetFire(bool pressed)=>fireHeld=pressed;
    public void SetAim(bool pressed)=>aiming=pressed;

    void Update()
    {
        if(weaponPivot==null||player==null||!player.IsAlive)return;
        cameraRef.fieldOfView=Mathf.Lerp(cameraRef.fieldOfView,aiming?specs[current].AimFov:70f,Time.deltaTime*10f);
        if(fireHeld)TryFire();
        UpdateWeaponPose();
    }

    void UpdateWeaponPose()
    {
        float speed=player.HorizontalSpeed;
        float bob=Mathf.Sin(Time.time*(7f+speed))*Mathf.Clamp01(speed/5f)*(aiming?.004f:.012f);
        Vector3 basePos=new Vector3(aiming?.13f:.34f,aiming?-.22f:-.29f,aiming?.62f:.55f);
        weaponPivot.localPosition=Vector3.Lerp(weaponPivot.localPosition,basePos+new Vector3(0,bob,0),Time.deltaTime*12f);
    }

    void TryFire()
    {
        if(reloading||Time.time<nextFire||!player.IsAlive)return;
        if(ammo[current]<=0){Reload();return;}

        WeaponSpec spec=specs[current];nextFire=Time.time+spec.FireRate;ammo[current]--;player.AddWeaponKick(spec.Recoil);

        for(int pellet=0;pellet<spec.Pellets;pellet++)
        {
            Vector3 origin=cameraRef.transform.position,direction=cameraRef.transform.forward;
            Vector2 cone=Random.insideUnitCircle*spec.Spread;
            direction=(direction+cameraRef.transform.right*cone.x+cameraRef.transform.up*cone.y).normalized;

            if(Physics.Raycast(origin,direction,out RaycastHit hit,spec.Range,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                EnemyAI enemy=hit.collider.GetComponentInParent<EnemyAI>();
                if(enemy!=null){enemy.TakeDamage(spec.Damage,hit.point,direction);audioSource.PlayOneShot(ProceduralAudio.Hit(),.25f);}
                else SITFFX.Impact(hit.point,hit.normal,materials);
                SITFGame.Instance?.SpawnProjectileTrace(origin,hit.point,spec.Accent);
            }
            else SITFGame.Instance?.SpawnProjectileTrace(origin,origin+direction*spec.Range,spec.Accent,.012f);
        }

        audioSource.PlayOneShot(ProceduralAudio.Shot(spec.AudioKey),current==5?.95f:.72f);
        ShowMuzzle(spec.Accent);StartCoroutine(RecoilRoutine(spec.Recoil));
        if(!spec.Automatic)fireHeld=false;
    }

    public void Reload()
    {
        if(reloading||ammo[current]>=specs[current].Magazine||reserve[current]<=0)return;
        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        reloading=true;fireHeld=false;
        audioSource.PlayOneShot(ProceduralAudio.Reload(),.7f);
        Vector3 start=weaponPivot.localPosition;Quaternion sr=weaponPivot.localRotation;float duration=specs[current].ReloadTime,t=0;
        while(t<duration)
        {
            t+=Time.deltaTime;float p=Mathf.Clamp01(t/duration),down=Mathf.Sin(p*Mathf.PI);
            weaponPivot.localPosition=start+new Vector3(0,-.14f*down,.08f*down);
            weaponPivot.localRotation=sr*Quaternion.Euler(12f*down,0,9f*down);yield return null;
        }
        int need=specs[current].Magazine-ammo[current],given=Mathf.Min(need,reserve[current]);
        ammo[current]+=given;reserve[current]-=given;reloading=false;
    }

    IEnumerator RecoilRoutine(float amount)
    {
        if(weaponPivot==null)yield break;
        Vector3 origin=weaponPivot.localPosition;
        weaponPivot.localPosition=origin+new Vector3(0,0,-amount);
        weaponPivot.localRotation=Quaternion.Euler(amount*80f,-amount*25f,0);
        yield return new WaitForSeconds(.035f);
        weaponPivot.localPosition=origin;weaponPivot.localRotation=Quaternion.Euler(1,0,0);
    }

    IEnumerator AnimatePose(Vector3 pos,Vector3 euler,float duration)
    {
        Vector3 startPos=weaponPivot.localPosition;Quaternion startRot=weaponPivot.localRotation,target=Quaternion.Euler(euler);float t=0;
        while(t<duration){t+=Time.deltaTime;float p=Mathf.SmoothStep(0,1,t/duration);weaponPivot.localPosition=Vector3.Lerp(startPos,pos,p);weaponPivot.localRotation=Quaternion.Slerp(startRot,target,p);yield return null;}
    }

    void ShowMuzzle(Color color)
    {
        if(muzzleFlash!=null)Object.Destroy(muzzleFlash);
        muzzleFlash=GameObject.CreatePrimitive(PrimitiveType.Cube);muzzleFlash.name="MUZZLE_FLASH";muzzleFlash.transform.SetParent(weaponPivot,false);
        muzzleFlash.transform.localPosition=new Vector3(0,0,-.75f);muzzleFlash.transform.localScale=new Vector3(.16f,.16f,.35f);
        muzzleFlash.GetComponent<Renderer>().sharedMaterial=materials.GetEmissive(Color.Lerp(Color.white,color,.55f),3.2f);
        Object.Destroy(muzzleFlash.GetComponent<Collider>());
        Light light=muzzleFlash.AddComponent<Light>();light.type=LightType.Point;light.range=2.5f;light.intensity=6f;light.color=color;
        StartCoroutine(FlashOff(muzzleFlash));
    }

    IEnumerator FlashOff(GameObject go){yield return new WaitForSeconds(.045f);if(go!=null)Object.Destroy(go);}

    void BuildModel(WeaponSpec spec)
    {
        switch(spec.Name){case "PISTOL":BuildPistol(spec);break;case "SMG":BuildSMG(spec);break;case "RIFLE":BuildRifle(spec);break;case "SHOTGUN":BuildShotgun(spec);break;case "DMR":BuildDMR(spec);break;default:BuildSniper(spec);break;}
    }

    GameObject Part(string name,Vector3 pos,Vector3 scale,Color color,float metallic=.25f)
    {
        GameObject o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(weaponPivot,false);o.transform.localPosition=pos;o.transform.localScale=scale;
        o.GetComponent<Renderer>().sharedMaterial=materials.Get(color,metallic,.18f);Object.Destroy(o.GetComponent<Collider>());return o;
    }

    void Barrel(Vector3 pos,float length,Color color){GameObject b=Part("BARREL",pos,new Vector3(.055f,.055f,length),color,.55f);b.transform.localPosition+=Vector3.forward*(-length*.5f);}
    void Grip(Vector3 pos,float angle,Color color){GameObject g=Part("GRIP",pos,new Vector3(.13f,.24f,.26f),color,.05f);g.transform.localRotation=Quaternion.Euler(angle,0,0);}
    void Magazine(Vector3 pos,Vector3 scale,Color color)=>Part("MAG",pos,scale,color,.3f);
    void Scope(Vector3 pos,float width,Color color){Part("SCOPE_BASE",pos,new Vector3(width,.06f,.10f),color,.4f);GameObject lens=Part("SCOPE",pos+new Vector3(0,.10f,0),new Vector3(width*.62f,.12f,.20f),new Color(.06f,.16f,.21f),.7f);lens.GetComponent<Renderer>().sharedMaterial=materials.GetEmissive(new Color(.06f,.16f,.21f),.45f);}

    void BuildPistol(WeaponSpec s){Part("SLIDE",new Vector3(0,0,-.16f),new Vector3(.16f,.15f,.56f),new Color(.10f,.12f,.14f),.7f);Part("FRAME",new Vector3(0,-.08f,-.06f),new Vector3(.18f,.12f,.44f),s.Accent);Grip(new Vector3(0,-.22f,.12f),-12,new Color(.09f,.11f,.12f));Barrel(new Vector3(0,0,-.43f),.30f,new Color(.08f,.09f,.10f));}
    void BuildSMG(WeaponSpec s){Part("RECEIVER",new Vector3(0,0,-.12f),new Vector3(.22f,.18f,.62f),s.Accent,.45f);Part("STOCK",new Vector3(0,0,.32f),new Vector3(.20f,.14f,.25f),new Color(.07f,.09f,.10f),.5f);Barrel(new Vector3(0,0,-.40f),.52f,new Color(.06f,.07f,.08f));Grip(new Vector3(0,-.18f,.12f),-15,new Color(.07f,.08f,.09f));Magazine(new Vector3(0,-.24f,-.10f),new Vector3(.12f,.32f,.22f),new Color(.08f,.09f,.10f));}
    void BuildRifle(WeaponSpec s){Part("RECEIVER",new Vector3(0,0,-.08f),new Vector3(.24f,.22f,.72f),s.Accent,.5f);Part("HANDGUARD",new Vector3(0,0,-.44f),new Vector3(.20f,.17f,.60f),new Color(.07f,.09f,.10f),.45f);Part("STOCK",new Vector3(0,0,.38f),new Vector3(.22f,.18f,.35f),new Color(.10f,.11f,.12f),.35f);Barrel(new Vector3(0,0,-.78f),.48f,new Color(.04f,.05f,.06f));Grip(new Vector3(0,-.20f,.13f),-14,new Color(.08f,.09f,.10f));Magazine(new Vector3(0,-.23f,-.04f),new Vector3(.15f,.33f,.25f),new Color(.08f,.09f,.10f));Scope(new Vector3(0,.16f,-.03f),.30f,new Color(.11f,.13f,.15f));}
    void BuildShotgun(WeaponSpec s){Part("RECEIVER",new Vector3(0,0,0),new Vector3(.24f,.22f,.55f),s.Accent,.35f);Part("FORE",new Vector3(0,0,-.48f),new Vector3(.22f,.18f,.62f),new Color(.10f,.08f,.06f),.1f);Barrel(new Vector3(0,0,-.84f),.85f,new Color(.05f,.05f,.05f));Grip(new Vector3(0,-.20f,.18f),-12,new Color(.09f,.07f,.05f));Part("STOCK",new Vector3(0,0,.40f),new Vector3(.22f,.17f,.38f),new Color(.22f,.15f,.09f),.1f);}
    void BuildDMR(WeaponSpec s){BuildRifle(s);Scope(new Vector3(0,.18f,-.15f),.36f,new Color(.10f,.12f,.14f));}
    void BuildSniper(WeaponSpec s){Part("RECEIVER",new Vector3(0,0,.02f),new Vector3(.24f,.22f,.72f),s.Accent,.55f);Part("STOCK",new Vector3(0,0,.50f),new Vector3(.21f,.20f,.48f),new Color(.06f,.08f,.09f),.45f);Barrel(new Vector3(0,0,-.70f),1.05f,new Color(.04f,.05f,.06f));Scope(new Vector3(0,.20f,-.02f),.42f,new Color(.08f,.10f,.12f));Magazine(new Vector3(0,-.18f,-.02f),new Vector3(.14f,.24f,.18f),new Color(.05f,.06f,.07f));Grip(new Vector3(0,-.19f,.18f),-14,new Color(.06f,.07f,.08f));}
}
