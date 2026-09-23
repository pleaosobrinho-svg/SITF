using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SITFMap { Warehouse, Office, Yard, Hangar, Metro, Block }

public sealed class SITFGame : MonoBehaviour
{
    public static SITFGame Instance { get; private set; }

    public readonly string[] MapNames={"WAREHOUSE","OFFICE","YARD","HANGAR","METRO","BLOCK"};
    public readonly string[] MapSubtitles={"Loading bays / close cover","Cubicles / narrow sightlines","Containers / open lanes","Aircraft bay / long sightlines","Platform / pillars / rails","Backstreet / storefronts"};

    public MaterialFactory Materials{get;private set;}
    public Transform WorldRoot{get;private set;}
    public FPSPlayer Player{get;private set;}
    public SITFMap CurrentMap{get;private set;}
    public int Score{get;private set;}
    public int Kills{get;private set;}
    public int EnemiesTotal{get;private set;}

    readonly List<EnemyAI> enemies=new List<EnemyAI>();
    List<Vector3> enemySpawns=new List<Vector3>();
    GameUI ui;
    bool playing,gameOver;
    static readonly Color BackgroundColor=new Color(.022f,.032f,.043f,1f);

    void Awake()
    {
        if(Instance!=null && Instance!=this){Destroy(gameObject);return;}
        Instance=this;DontDestroyOnLoad(gameObject);
        Application.targetFrameRate=60;QualitySettings.vSyncCount=0;QualitySettings.antiAliasing=0;QualitySettings.shadowDistance=55f;
        Screen.sleepTimeout=SleepTimeout.NeverSleep;
        Screen.orientation=ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait=false;Screen.autorotateToPortraitUpsideDown=false;
        Screen.autorotateToLandscapeLeft=true;Screen.autorotateToLandscapeRight=false;

        Materials=new MaterialFactory();
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=38f;RenderSettings.fogEndDistance=100f;RenderSettings.fogColor=BackgroundColor;
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.48f,.52f,.58f);

        GameObject u=new GameObject("SITF_UI");ui=u.AddComponent<GameUI>();ui.Initialize(this);
    }

    void Start()=>ui.ShowMenu();

    public void StartMatch(int mapIndex)
    {
        StopCurrentWorld();
        CurrentMap=(SITFMap)Mathf.Clamp(mapIndex,0,MapNames.Length-1);
        Score=0;Kills=0;gameOver=false;playing=true;

        GameObject world=new GameObject("WORLD_"+MapNames[mapIndex]);
        WorldRoot=world.transform;
        enemySpawns=MapFactory.Build(CurrentMap,WorldRoot,Materials);
        SetupLighting(CurrentMap);
        SpawnPlayer();SpawnEnemies();ui.ShowHUD();
    }

    void SetupLighting(SITFMap map)
    {
        GameObject o=new GameObject("SUN");o.transform.SetParent(WorldRoot,false);
        Light sun=o.AddComponent<Light>();sun.type=LightType.Directional;
        sun.intensity=(map==SITFMap.Yard||map==SITFMap.Block)?1.15f:.90f;sun.shadows=LightShadows.Soft;
        o.transform.rotation=Quaternion.Euler(48,-32,0);
        RenderSettings.fogColor=(map==SITFMap.Warehouse||map==SITFMap.Office||map==SITFMap.Metro||map==SITFMap.Hangar)?new Color(.055f,.068f,.078f):new Color(.12f,.15f,.18f);
        RenderSettings.fogEndDistance=(map==SITFMap.Warehouse||map==SITFMap.Office||map==SITFMap.Metro||map==SITFMap.Hangar)?72f:100f;
    }

    void SpawnPlayer()
    {
        GameObject o=new GameObject("PLAYER");o.transform.SetParent(WorldRoot,false);o.transform.position=new Vector3(0,.12f,14f);
        Player=o.AddComponent<FPSPlayer>();Player.Initialize();
    }

    void SpawnEnemies()
    {
        enemies.Clear();int count=CurrentMap==SITFMap.Metro?14:12;EnemiesTotal=count;
        for(int i=0;i<count;i++)
        {
            GameObject o=new GameObject("HOSTILE_"+i.ToString("00"));o.transform.SetParent(WorldRoot,false);
            o.transform.position=enemySpawns[i%enemySpawns.Count];
            EnemyAI e=o.AddComponent<EnemyAI>();e.Initialize(Player,i%4);enemies.Add(e);
        }
    }

    public void RegisterKill(EnemyAI enemy,int points)
    {
        if(gameOver)return;Kills++;Score+=points;ui?.ShowKillFeed("HOSTILE ELIMINATED",points);
    }

    public void PlayerDied()
    {
        if(gameOver)return;gameOver=true;playing=false;ui.ShowResult(false,Score,Kills);
    }

    void Update()
    {
        if(!playing||gameOver||Player==null)return;
        int alive=0;for(int i=0;i<enemies.Count;i++)if(enemies[i]!=null&&!enemies[i].IsDead)alive++;
        ui?.UpdateHUD(Player,alive,EnemiesTotal,Score,Kills);
        if(alive==0){gameOver=true;playing=false;StartCoroutine(FinishMatch());}
    }

    IEnumerator FinishMatch(){yield return new WaitForSeconds(.8f);ui.ShowResult(true,Score,Kills);}

    public void SpawnProjectileTrace(Vector3 from,Vector3 to,Color color,float width=.018f)
    {
        GameObject t=GameObject.CreatePrimitive(PrimitiveType.Cube);t.name="TRACER";t.transform.SetParent(WorldRoot,true);
        Vector3 d=to-from;t.transform.position=(from+to)*.5f;t.transform.rotation=Quaternion.LookRotation(d);
        t.transform.localScale=new Vector3(width,width,Mathf.Max(d.magnitude,.1f));
        Destroy(t.GetComponent<Collider>());t.GetComponent<Renderer>().sharedMaterial=Materials.GetEmissive(color,2f);Destroy(t,.055f);
    }

    void StopCurrentWorld()
    {
        if(WorldRoot!=null)Destroy(WorldRoot.gameObject);
        Player=null;enemies.Clear();
    }
}
