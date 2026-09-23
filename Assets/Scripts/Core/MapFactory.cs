using System.Collections.Generic;
using UnityEngine;

public static class MapFactory
{
    static readonly Color C=new Color(.26f,.29f,.32f), D=new Color(.13f,.16f,.18f), S=new Color(.34f,.38f,.41f);
    static readonly Color W=new Color(.76f,.79f,.79f), B=new Color(.10f,.25f,.34f), T=new Color(.08f,.34f,.35f);
    static readonly Color Y=new Color(.72f,.56f,.16f), R=new Color(.46f,.11f,.12f);

    public static List<Vector3> Build(SITFMap map,Transform root,MaterialFactory m)
    {
        switch(map)
        {
            case SITFMap.Warehouse:return Warehouse(root,m);
            case SITFMap.Office:return Office(root,m);
            case SITFMap.Yard:return Yard(root,m);
            case SITFMap.Hangar:return Hangar(root,m);
            case SITFMap.Metro:return Metro(root,m);
            default:return Block(root,m);
        }
    }

    static List<Vector3> Warehouse(Transform r,MaterialFactory m)
    {
        Base(r,m,72,46,new Color(.70f,.72f,.70f),C);
        for(int z=-16;z<=16;z+=8){Shelf(r,m,new Vector3(-18,0,z),6.5f,S);Shelf(r,m,new Vector3(14,0,z+4),7f,S);}
        Counter(r,m,new Vector3(0,1,-16),new Vector3(15,2,2),Y); Counter(r,m,new Vector3(0,1,13),new Vector3(15,2,2),B);
        Crates(r,m,new Vector3(-29,0,-13),3,2);Crates(r,m,new Vector3(26,0,15),3,2);Lights(r,m,-28,28,0,7);
        Door(r,m,new Vector3(-35,3.4f,0),new Vector3(.5f,6.8f,10),T);
        return Spawns(new[]{new Vector3(-31,.2f,-19),new Vector3(-24,.2f,-4),new Vector3(-31,.2f,10),new Vector3(31,.2f,-17),new Vector3(26,.2f,-2),new Vector3(31,.2f,14),new Vector3(-8,.2f,-20),new Vector3(9,.2f,-20),new Vector3(-10,.2f,19),new Vector3(10,.2f,18),new Vector3(-14,.2f,7),new Vector3(14,.2f,7)});
    }

    static List<Vector3> Office(Transform r,MaterialFactory m)
    {
        Base(r,m,68,46,new Color(.53f,.56f,.57f),W);
        float[] xs={-16,-5,6,17}, zs={-12,-2,8};
        foreach(float x in xs)foreach(float z in zs)Cubicle(r,m,new Vector3(x,0,z));
        Counter(r,m,new Vector3(0,1,-18),new Vector3(14,2.1f,2.5f),T);
        Table(r,m,new Vector3(0,.85f,16),new Vector3(15,1.7f,3));
        Machine(r,m,new Vector3(-27,1,-16));Machine(r,m,new Vector3(27,1,16));Lights(r,m,-24,24,0,6);
        return Spawns(new[]{new Vector3(-30,.2f,-19),new Vector3(-30,.2f,1),new Vector3(-30,.2f,18),new Vector3(30,.2f,-18),new Vector3(30,.2f,2),new Vector3(30,.2f,18),new Vector3(-11,.2f,-19),new Vector3(1,.2f,-19),new Vector3(13,.2f,-19),new Vector3(-12,.2f,19),new Vector3(12,.2f,19),new Vector3(25,.2f,9)});
    }

    static List<Vector3> Yard(Transform r,MaterialFactory m)
    {
        Base(r,m,84,54,new Color(.27f,.29f,.28f),D);
        Container(r,m,new Vector3(-24,2,-15),B);Container(r,m,new Vector3(0,2,-15),T);Container(r,m,new Vector3(24,2,-15),R);
        Container(r,m,new Vector3(-18,2,12),Y);Container(r,m,new Vector3(15,2,11),B);Container(r,m,new Vector3(31,2,4),T);
        Barrier(r,m,new Vector3(0,.55f,4),10);Barrier(r,m,new Vector3(-10,.55f,21),7);Cones(r,m,new Vector3(-34,0,0),5);
        Pole(r,m,new Vector3(0,0,0));Pole(r,m,new Vector3(34,0,18));
        return Spawns(new[]{new Vector3(-36,.2f,-21),new Vector3(-12,.2f,-22),new Vector3(13,.2f,-22),new Vector3(36,.2f,-22),new Vector3(-36,.2f,1),new Vector3(-16,.2f,2),new Vector3(15,.2f,2),new Vector3(36,.2f,1),new Vector3(-34,.2f,21),new Vector3(-1,.2f,21),new Vector3(18,.2f,21),new Vector3(36,.2f,20)});
    }

    static List<Vector3> Hangar(Transform r,MaterialFactory m)
    {
        Base(r,m,88,52,new Color(.36f,.38f,.38f),S);
        for(int x=-32;x<=32;x+=16){Pillar(r,m,new Vector3(x,4.5f,-19),.9f,9,S);Pillar(r,m,new Vector3(x,4.5f,19),.9f,9,S);}
        Door(r,m,new Vector3(0,5.5f,-25),new Vector3(26,10,.3f),B);Crates(r,m,new Vector3(-29,0,4),3,2);Crates(r,m,new Vector3(29,0,-5),3,3);Plane(r,m,new Vector3(0,0,5));Lights(r,m,-28,28,0,8);
        return Spawns(new[]{new Vector3(-39,.2f,-20),new Vector3(-19,.2f,-20),new Vector3(0,.2f,-20),new Vector3(20,.2f,-20),new Vector3(39,.2f,-20),new Vector3(-39,.2f,20),new Vector3(-19,.2f,20),new Vector3(0,.2f,20),new Vector3(20,.2f,20),new Vector3(39,.2f,20),new Vector3(-12,.2f,8),new Vector3(13,.2f,8)});
    }

    static List<Vector3> Metro(Transform r,MaterialFactory m)
    {
        Base(r,m,96,44,new Color(.24f,.26f,.28f),C);
        Platform(r,m,new Vector3(-29,.55f,0),new Vector3(33,1.1f,14),Y);Platform(r,m,new Vector3(29,.55f,0),new Vector3(33,1.1f,14),Y);
        Rail(r,m,-47,47,-7);Rail(r,m,-47,47,7);
        for(int x=-40;x<=40;x+=16)Pillar(r,m,new Vector3(x,4,0),.75f,8,S);
        Bench(r,m,new Vector3(-28,1,-13));Bench(r,m,new Vector3(27,1,13));Machine(r,m,new Vector3(39,2,-13));Machine(r,m,new Vector3(-39,2,13));Lights(r,m,-40,40,0,7);
        return Spawns(new[]{new Vector3(-42,.2f,-13),new Vector3(-27,.2f,-13),new Vector3(-12,.2f,-13),new Vector3(12,.2f,-13),new Vector3(28,.2f,-13),new Vector3(42,.2f,-13),new Vector3(-42,.2f,13),new Vector3(-27,.2f,13),new Vector3(-12,.2f,13),new Vector3(12,.2f,13),new Vector3(28,.2f,13),new Vector3(42,.2f,13)});
    }

    static List<Vector3> Block(Transform r,MaterialFactory m)
    {
        Base(r,m,90,60,new Color(.21f,.22f,.22f),D);
        Building(r,m,new Vector3(-30,3.5f,-18),new Vector3(20,7,18),new Color(.30f,.33f,.35f),T);
        Building(r,m,new Vector3(20,4.5f,-17),new Vector3(18,9,18),new Color(.34f,.31f,.29f),Y);
        Building(r,m,new Vector3(-27,3.5f,18),new Vector3(22,7,16),new Color(.28f,.32f,.35f),B);
        Building(r,m,new Vector3(24,3.5f,17),new Vector3(22,7,17),new Color(.34f,.28f,.27f),R);
        Road(r,m,new Vector3(0,0,0),new Vector3(86,.08f,11));Road(r,m,new Vector3(0,0,0),new Vector3(11,.09f,56));
        Crosswalk(r,m,new Vector3(0,.08f,-7));Car(r,m,new Vector3(-13,.6f,6),B);Car(r,m,new Vector3(14,.6f,-6),R);Car(r,m,new Vector3(31,.6f,10),W);
        StreetLight(r,m,new Vector3(-10,0,-1));StreetLight(r,m,new Vector3(11,0,-1));StreetLight(r,m,new Vector3(-10,0,20));StreetLight(r,m,new Vector3(12,0,20));
        return Spawns(new[]{new Vector3(-40,.2f,-25),new Vector3(-7,.2f,-25),new Vector3(9,.2f,-25),new Vector3(40,.2f,-25),new Vector3(-40,.2f,25),new Vector3(-7,.2f,25),new Vector3(8,.2f,25),new Vector3(40,.2f,25),new Vector3(-39,.2f,3),new Vector3(-18,.2f,3),new Vector3(18,.2f,3),new Vector3(39,.2f,3)});
    }

    static List<Vector3> Spawns(Vector3[] a)=>new List<Vector3>(a);

    static void Base(Transform r,MaterialFactory m,float w,float d,Color floor,Color wall)
    {
        Box(r,m,new Vector3(0,-.25f,0),new Vector3(w,.5f,d),floor,"FLOOR");
        Box(r,m,new Vector3(0,3.5f,-d*.5f),new Vector3(w,7,.7f),wall,"WALL");
        Box(r,m,new Vector3(0,3.5f,d*.5f),new Vector3(w,7,.7f),wall,"WALL");
        Box(r,m,new Vector3(-w*.5f,3.5f,0),new Vector3(.7f,7,d),wall,"WALL");
        Box(r,m,new Vector3(w*.5f,3.5f,0),new Vector3(.7f,7,d),wall,"WALL");
    }

    static void Box(Transform r,MaterialFactory m,Vector3 p,Vector3 s,Color c,string n,bool col=true)
    {
        GameObject o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=n;o.transform.SetParent(r,false);o.transform.position=p;o.transform.localScale=s;o.GetComponent<Renderer>().sharedMaterial=m.Get(c,.05f,.14f);
        if(!col){Collider x=o.GetComponent<Collider>();if(x!=null)Object.Destroy(x);}
    }

    static void Counter(Transform r,MaterialFactory m,Vector3 p,Vector3 s,Color accent){Box(r,m,p,s,new Color(.30f,.26f,.22f),"COUNTER");Box(r,m,p+Vector3.up*(s.y*.54f),new Vector3(s.x*.96f,.08f,s.z*1.04f),accent,"COUNTER_TOP",false);}
    static void Shelf(Transform r,MaterialFactory m,Vector3 p,float w,Color c){Box(r,m,p+Vector3.up*1.2f,new Vector3(.25f,2.4f,.55f),c,"SHELF");Box(r,m,p+new Vector3(w,1.2f,0),new Vector3(.25f,2.4f,.55f),c,"SHELF");for(int y=0;y<3;y++)Box(r,m,p+new Vector3(w*.5f,.18f+y*.9f,0),new Vector3(w,.16f,.55f),c,"SHELF_BAR");}
    static void Cubicle(Transform r,MaterialFactory m,Vector3 p){Color c=new Color(.31f,.38f,.42f);Box(r,m,p+new Vector3(0,1.25f,-1.55f),new Vector3(6.8f,2.5f,.3f),c,"CUBICLE");Box(r,m,p+new Vector3(-3.25f,1.25f,0),new Vector3(.3f,2.5f,3.1f),c,"CUBICLE");Box(r,m,p+new Vector3(3.25f,1.25f,0),new Vector3(.3f,2.5f,3.1f),c,"CUBICLE");Box(r,m,p+new Vector3(0,.85f,.45f),new Vector3(4.5f,.18f,1.7f),new Color(.23f,.26f,.28f),"DESK");}
    static void Table(Transform r,MaterialFactory m,Vector3 p,Vector3 s){Box(r,m,p,s,new Color(.28f,.22f,.18f),"TABLE");for(int i=-1;i<=1;i+=2)Box(r,m,p+new Vector3(i*s.x*.36f,-.55f,0),new Vector3(.45f,1.1f,s.z*.78f),S,"TABLE_BASE");}
    static void Machine(Transform r,MaterialFactory m,Vector3 p){Box(r,m,p,new Vector3(2.4f,2.6f,1.4f),D,"MACHINE");Box(r,m,p+new Vector3(0,.2f,-.75f),new Vector3(1.8f,1.8f,.08f),T,"SCREEN",false);}
    static void Container(Transform r,MaterialFactory m,Vector3 p,Color c){Box(r,m,p,new Vector3(16,4,6),c,"CONTAINER");for(int x=-6;x<=6;x+=3)Box(r,m,p+new Vector3(x,0,3.1f),new Vector3(.16f,3.7f,.08f),D,"RIB",false);}
    static void Barrier(Transform r,MaterialFactory m,Vector3 p,int count){for(int i=0;i<count;i++){float x=(i-(count-1)*.5f)*2.4f;Box(r,m,p+new Vector3(x,0,0),new Vector3(1.9f,1.1f,.5f),S,"BARRIER");Box(r,m,p+new Vector3(x,1.25f,0),new Vector3(1.5f,.08f,.08f),Y,"TAPE",false);}}
    static void Cones(Transform r,MaterialFactory m,Vector3 p,int count){for(int i=0;i<count;i++){GameObject o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.transform.SetParent(r,false);o.transform.position=p+new Vector3(i*2.4f,.5f,0);o.transform.localScale=new Vector3(.5f,.8f,.5f);o.GetComponent<Renderer>().sharedMaterial=m.Get(new Color(.9f,.3f,.08f));}}
    static void Crates(Transform r,MaterialFactory m,Vector3 p,int w,int h){for(int y=0;y<h;y++)for(int x=0;x<w;x++)Box(r,m,p+new Vector3(x*1.4f,.8f+y*1.6f,0),new Vector3(1.2f,1.4f,1.2f),new Color(.45f,.33f,.18f),"CRATE");}
    static void Lights(Transform r,MaterialFactory m,float min,float max,float z,int count){for(int i=0;i<count;i++){float x=Mathf.Lerp(min,max,(float)i/Mathf.Max(1,count-1));GameObject l=BoxObj(r,m,new Vector3(x,6.2f,z),new Vector3(1.8f,.12f,.45f),new Color(.92f,.88f,.72f),"LIGHT",false);Light p=l.AddComponent<Light>();p.type=LightType.Point;p.range=11;p.intensity=5.5f;p.color=new Color(1f,.92f,.78f);}}
    static void Pole(Transform r,MaterialFactory m,Vector3 p){Box(r,m,p+Vector3.up*3.5f,new Vector3(.3f,7,.3f),S,"POLE");GameObject l=BoxObj(r,m,p+Vector3.up*7.1f,new Vector3(1,.16f,.35f),new Color(.93f,.88f,.68f),"LAMP",false);Light q=l.AddComponent<Light>();q.type=LightType.Point;q.range=16;q.intensity=7;}
    static void Pillar(Transform r,MaterialFactory m,Vector3 p,float rad,float h,Color c){GameObject o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.transform.SetParent(r,false);o.transform.position=p;o.transform.localScale=new Vector3(rad,h*.5f,rad);o.GetComponent<Renderer>().sharedMaterial=m.Get(c,.25f,.25f);}
    static void Door(Transform r,MaterialFactory m,Vector3 p,Vector3 s,Color c){Box(r,m,p,s,c,"DOOR");}
    static void Plane(Transform r,MaterialFactory m,Vector3 p){Box(r,m,p+Vector3.up*.9f,new Vector3(17,1.2f,2.4f),new Color(.54f,.56f,.57f),"PLANE");Box(r,m,p+Vector3.up*.8f,new Vector3(3,.45f,13),new Color(.49f,.51f,.52f),"WING");}
    static void Platform(Transform r,MaterialFactory m,Vector3 p,Vector3 s,Color c){Box(r,m,p,s,new Color(.45f,.47f,.47f),"PLATFORM");Box(r,m,p+new Vector3(0,.58f,-s.z*.52f),new Vector3(s.x,.08f,.4f),c,"EDGE",false);Box(r,m,p+new Vector3(0,.58f,s.z*.52f),new Vector3(s.x,.08f,.4f),c,"EDGE",false);}
    static void Rail(Transform r,MaterialFactory m,float min,float max,float z){Box(r,m,new Vector3((min+max)*.5f,.22f,z),new Vector3(max-min,.16f,.15f),S,"RAIL");}
    static void Bench(Transform r,MaterialFactory m,Vector3 p){Box(r,m,p,new Vector3(5,.25f,1.3f),D,"BENCH");Box(r,m,p+new Vector3(0,.9f,.5f),new Vector3(5,1.1f,.18f),D,"BENCH_BACK");}
    static void Building(Transform r,MaterialFactory m,Vector3 p,Vector3 s,Color c,Color accent){Box(r,m,p,s,c,"BUILDING");for(int x=-1;x<=1;x++)Box(r,m,p+new Vector3(x*s.x*.3f,-s.y*.25f,-s.z*.51f),new Vector3(2.2f,1.2f,.08f),new Color(.08f,.14f,.17f),"WINDOW",false);Box(r,m,p+Vector3.up*s.y*.15f+Vector3.back*s.z*.51f,new Vector3(s.x*.7f,.45f,.08f),accent,"SIGN",false);}
    static void Road(Transform r,MaterialFactory m,Vector3 p,Vector3 s){Box(r,m,p,s,new Color(.08f,.09f,.09f),"ROAD",false);}
    static void Crosswalk(Transform r,MaterialFactory m,Vector3 p){for(int i=-4;i<=4;i++)Box(r,m,p+new Vector3(i*1.1f,.07f,0),new Vector3(.7f,.03f,5),W,"CROSS",false);}
    static void Car(Transform r,MaterialFactory m,Vector3 p,Color c){Box(r,m,p,new Vector3(4.2f,.95f,2.1f),c,"CAR");Box(r,m,p+Vector3.up*.65f,new Vector3(2.4f,.7f,1.7f),c,"CABIN");}
    static void StreetLight(Transform r,MaterialFactory m,Vector3 p){Box(r,m,p+Vector3.up*3.2f,new Vector3(.18f,6.4f,.18f),S,"STREET");Box(r,m,p+new Vector3(1,6,0),new Vector3(2.1f,.16f,.16f),S,"ARM");}

    static GameObject BoxObj(Transform r,MaterialFactory m,Vector3 p,Vector3 s,Color c,string n,bool col)
    {
        GameObject o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=n;o.transform.SetParent(r,false);o.transform.position=p;o.transform.localScale=s;o.GetComponent<Renderer>().sharedMaterial=m.Get(c,.05f,.14f);if(!col){Collider q=o.GetComponent<Collider>();if(q!=null)Object.Destroy(q);}return o;
    }
}
