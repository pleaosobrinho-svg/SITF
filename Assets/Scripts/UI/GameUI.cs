using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class GameUI : MonoBehaviour
{
    private SITFGame game;
    private Canvas canvas;
    private Font font;
    private GameObject menuRoot, hudRoot, resultRoot, settingsRoot;
    private Text objectiveText, hpText, ammoText, weaponText, killFeedText, mapTitle;
    private int selectedMap;
    private float feedTimer;
    private MobileControls mobileControls;

    public void Initialize(SITFGame owner)
    {
        game=owner;
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildCanvas();
        BuildMenu();
    }

    private void BuildCanvas()
    {
        GameObject o=new GameObject("CANVAS");
        o.transform.SetParent(transform,false);
        canvas=o.AddComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler=o.AddComponent<CanvasScaler>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);
        scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight=0.5f;
        o.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
    }

    private void EnsureEventSystem()
    {
        if(FindFirstObjectByType<EventSystem>()!=null) return;
        GameObject o=new GameObject("EVENT_SYSTEM");
        o.AddComponent<EventSystem>();
        o.AddComponent<StandaloneInputModule>();
    }

    public void ShowMenu()
    {
        ClearRoot(ref hudRoot); ClearRoot(ref resultRoot);
        if(menuRoot!=null) Destroy(menuRoot);
        menuRoot=Panel("MAIN_MENU",canvas.transform,new Color(0.018f,0.026f,0.034f,1f));

        PanelAt(menuRoot.transform,new Vector2(0,0),new Vector2(1920,115),new Color(0.025f,0.038f,0.048f,1f),new Vector2(0,1),new Vector2(1,1),new Vector2(0,-115));
        TextAt(menuRoot.transform,"SITF",new Vector2(78,-76),new Vector2(500,74),62,FontStyle.Bold,Color.white,TextAnchor.MiddleLeft);
        TextAt(menuRoot.transform,"SILENCE IN THE FIRE",new Vector2(82,-128),new Vector2(560,40),19,FontStyle.Normal,new Color(0.58f,0.66f,0.72f),TextAnchor.MiddleLeft);
        TextAt(menuRoot.transform,"NATIVE 3D FPS  •  UNITY 6.3 LTS  •  LANDSCAPE",new Vector2(84,-164),new Vector2(680,32),13,FontStyle.Normal,new Color(0.35f,0.43f,0.48f),TextAnchor.MiddleLeft);

        mapTitle=TextAt(menuRoot.transform,"SELECT MAP",new Vector2(1075,-148),new Vector2(650,40),22,FontStyle.Bold,new Color(0.88f,0.90f,0.91f),TextAnchor.MiddleLeft);
        BuildMapCards();

        Button start=ButtonAt(menuRoot.transform,"START MISSION",new Vector2(1080,690),new Vector2(650,78),28,new Color(0.14f,0.29f,0.38f),Color.white);
        start.onClick.AddListener(()=>game.StartMatch(selectedMap));

        Button settings=ButtonAt(menuRoot.transform,"SETTINGS",new Vector2(1080,780),new Vector2(318,62),19,new Color(0.075f,0.10f,0.12f),new Color(0.80f,0.84f,0.86f));
        settings.onClick.AddListener(ShowSettings);
        Button reset=ButtonAt(menuRoot.transform,"RESET SETTINGS",new Vector2(1412,780),new Vector2(318,62),19,new Color(0.075f,0.10f,0.12f),new Color(0.80f,0.84f,0.86f));
        reset.onClick.AddListener(()=>{PlayerPrefs.DeleteKey("SITF_SENS");PlayerPrefs.Save();});

        TextAt(menuRoot.transform,"6 MAPS  •  6 WEAPONS  •  4 ENEMY ARCHETYPES",new Vector2(1078,860),new Vector2(650,28),12,FontStyle.Normal,new Color(0.37f,0.45f,0.50f),TextAnchor.MiddleLeft);
        TextAt(menuRoot.transform,"DESKTOP + MOBILE TOUCH CONTROLS",new Vector2(1078,894),new Vector2(650,28),12,FontStyle.Normal,new Color(0.37f,0.45f,0.50f),TextAnchor.MiddleLeft);
        BuildLeftPreview(menuRoot.transform);
    }

    private void BuildLeftPreview(Transform parent)
    {
        GameObject frame=PanelAt(parent,new Vector2(82,-270),new Vector2(880,760),new Color(0.03f,0.045f,0.055f,1f),new Vector2(0,1),new Vector2(0,1),new Vector2(880,760));
        PanelAt(frame.transform,new Vector2(0,0),new Vector2(880,420),new Color(0.08f,0.11f,0.13f,1f),new Vector2(0,0),new Vector2(1,0),new Vector2(880,420));
        PanelAt(frame.transform,new Vector2(0,0),new Vector2(880,130),new Color(0.08f,0.10f,0.12f,1f),new Vector2(0,0),new Vector2(1,0),new Vector2(880,130));
        for(int i=0;i<8;i++)
        {
            float x=55+i*105;
            PanelAt(frame.transform,new Vector2(x,155+(i%2)*38),new Vector2(74,180-(i%2)*35),new Color(0.18f+0.02f*(i%3),0.23f+0.01f*(i%2),0.27f,1f),new Vector2(0,0),new Vector2(0,0),new Vector2(74,180-(i%2)*35));
        }
        PanelAt(frame.transform,new Vector2(0,280),new Vector2(880,140),new Color(0.20f,0.22f,0.22f,1f),new Vector2(0,0),new Vector2(0,0),new Vector2(880,140));
        TextAt(frame.transform,"SITF",new Vector2(36,336),new Vector2(300,50),30,FontStyle.Bold,new Color(0.92f,0.94f,0.94f),TextAnchor.MiddleLeft);
        TextAt(frame.transform,"BLOCKY  /  FAST  /  TACTICAL",new Vector2(38,305),new Vector2(420,30),12,FontStyle.Normal,new Color(0.53f,0.61f,0.66f),TextAnchor.MiddleLeft);
        TextAt(frame.transform,"Six distinct combat spaces, compact controls, six weapons and reactive enemies.",new Vector2(38,430),new Vector2(800,82),19,FontStyle.Normal,new Color(0.75f,0.78f,0.80f),TextAnchor.MiddleLeft);
        TextAt(frame.transform,"Procedural geometry keeps the game light while maintaining a complete playable loop.",new Vector2(38,520),new Vector2(800,70),15,FontStyle.Normal,new Color(0.47f,0.54f,0.58f),TextAnchor.MiddleLeft);
    }

    private void BuildMapCards()
    {
        float x0=1080,y0=195,w=316,h=214;
        for(int i=0;i<6;i++)
        {
            int index=i;
            float x=x0+(i%2)*(w+18), y=y0+(i/2)*(h+17);
            Button b=ButtonAt(menuRoot.transform,game.MapNames[i],new Vector2(x,y),new Vector2(w,h),21,new Color(0.052f,0.071f,0.082f,1f),new Color(0.88f,0.90f,0.91f));
            b.onClick.AddListener(()=>SelectMap(index));
            TextAt(b.transform,game.MapSubtitles[i],new Vector2(17,-142),new Vector2(w-34,42),12,FontStyle.Normal,new Color(0.53f,0.60f,0.64f),TextAnchor.UpperLeft);
            TextAt(b.transform,(i+1).ToString("00"),new Vector2(w-55,-35),new Vector2(35,25),11,FontStyle.Bold,new Color(0.35f,0.43f,0.47f),TextAnchor.MiddleRight);
            PanelAt(b.transform,new Vector2(17,-74),new Vector2(80,38),MapAccent((SITFMap)i),new Vector2(0,1),new Vector2(0,1),new Vector2(80,38));
        }
        SelectMap(0);
    }

    private void SelectMap(int index)
    {
        selectedMap=index;
        if(mapTitle!=null) mapTitle.text="SELECT MAP  /  "+game.MapNames[index];
    }

    public void ShowHUD()
    {
        ClearRoot(ref menuRoot); ClearRoot(ref resultRoot);
        if(hudRoot!=null) Destroy(hudRoot);
        hudRoot=Panel("HUD",canvas.transform,new Color(0,0,0,0));

        PanelAt(hudRoot.transform,new Vector2(26,-24),new Vector2(625,92),new Color(0.018f,0.026f,0.034f,0.72f),new Vector2(0,1),new Vector2(0,1),new Vector2(625,92));
        TextAt(hudRoot.transform,"SITF",new Vector2(44,-54),new Vector2(100,34),20,FontStyle.Bold,Color.white,TextAnchor.MiddleLeft);
        objectiveText=TextAt(hudRoot.transform,"ELIMINATE HOSTILES",new Vector2(135,-54),new Vector2(480,34),15,FontStyle.Bold,new Color(0.73f,0.79f,0.82f),TextAnchor.MiddleLeft);
        TextAt(hudRoot.transform,"LANDSCAPE FPS",new Vector2(44,-85),new Vector2(180,24),10,FontStyle.Normal,new Color(0.42f,0.49f,0.52f),TextAnchor.MiddleLeft);

        hpText=TextAt(hudRoot.transform,"HP 100",new Vector2(42,42),new Vector2(180,42),22,FontStyle.Bold,new Color(0.87f,0.90f,0.92f),TextAnchor.MiddleLeft);
        PanelAt(hudRoot.transform,new Vector2(42,88),new Vector2(230,8),new Color(0.16f,0.18f,0.19f,0.8f),new Vector2(0,0),new Vector2(0,0),new Vector2(230,8));
        PanelAt(hudRoot.transform,new Vector2(42,88),new Vector2(230,8),new Color(0.20f,0.58f,0.44f,0.95f),new Vector2(0,0),new Vector2(0,0),new Vector2(230,8),"HPBAR");

        weaponText=TextAt(hudRoot.transform,"PISTOL",new Vector2(1425,-65),new Vector2(280,34),15,FontStyle.Bold,new Color(0.70f,0.76f,0.79f),TextAnchor.MiddleRight);
        ammoText=TextAt(hudRoot.transform,"15 / 90",new Vector2(1490,-100),new Vector2(215,44),27,FontStyle.Bold,Color.white,TextAnchor.MiddleRight);
        killFeedText=TextAt(hudRoot.transform,"",new Vector2(150,110),new Vector2(600,40),15,FontStyle.Bold,new Color(1f,0.72f,0.40f),TextAnchor.MiddleLeft);

        GameObject cross=PanelAt(hudRoot.transform,new Vector2(945,518),new Vector2(30,30),new Color(0,0,0,0),new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(30,30));
        TextAt(cross.transform,"+",new Vector2(0,0),new Vector2(30,30),27,FontStyle.Normal,new Color(1,1,1,0.92f),TextAnchor.MiddleCenter);
        BuildTouchControls();
    }

    private void BuildTouchControls()
    {
        GameObject lookZone=PanelAt(hudRoot.transform,new Vector2(960,0),new Vector2(960,1080),new Color(1,1,1,0.001f),new Vector2(0,0),new Vector2(1,1),new Vector2(960,1080));
        mobileControls=lookZone.AddComponent<MobileControls>();
        mobileControls.Initialize(lookZone.GetComponent<RectTransform>());

        GameObject stick=PanelAt(hudRoot.transform,new Vector2(72,78),new Vector2(220,220),new Color(0,0,0,0),new Vector2(0,0),new Vector2(0,0),new Vector2(220,220));
        CircleGraphic cg=stick.AddComponent<CircleGraphic>(); cg.color=new Color(0.76f,0.84f,0.88f,0.30f);
        GameObject knob=PanelAt(stick.transform,new Vector2(80,80),new Vector2(60,60),new Color(0,0,0,0),new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(60,60),"KNOB");
        CircleGraphic kg=knob.AddComponent<CircleGraphic>(); kg.color=new Color(0.78f,0.85f,0.89f,0.62f);
        VirtualJoystick joystick=stick.AddComponent<VirtualJoystick>(); joystick.Initialize(mobileControls);

        GameObject fire=CircleButton("FIRE",new Vector2(1654,108),new Vector2(160,160),31,new Color(1f,0.55f,0.37f,0.76f));
        fire.AddComponent<HoldButton>().Initialize(mobileControls,HoldButton.ActionType.Fire);
        GameObject aim=CircleButton("AIM",new Vector2(1462,92),new Vector2(112,112),19,new Color(0.70f,0.79f,0.84f,0.50f));
        aim.AddComponent<HoldButton>().Initialize(mobileControls,HoldButton.ActionType.Aim);

        Button reload=ButtonAt(hudRoot.transform,"RELOAD",new Vector2(1654,292),new Vector2(160,54),14,new Color(0.08f,0.11f,0.13f,0.80f),new Color(0.87f,0.90f,0.91f));
        reload.onClick.AddListener(()=>game.Player.Weapons.Reload());

        for(int i=0;i<6;i++)
        {
            int index=i;
            float x=1120+(i%3)*165, y=54+(i/3)*58;
            Button slot=ButtonAt(hudRoot.transform,(i+1)+"  "+WeaponShort(i),new Vector2(x,y),new Vector2(150,46),12,new Color(0.055f,0.075f,0.085f,0.84f),new Color(0.78f,0.83f,0.85f));
            slot.onClick.AddListener(()=>game.Player.Weapons.SwitchWeapon(index));
        }
    }

    private string WeaponShort(int index){string[] n={"PST","SMG","RFL","SGN","DMR","SNP"};return n[index];}

    private GameObject CircleButton(string label,Vector2 pos,Vector2 size,int fontSize,Color color)
    {
        GameObject o=PanelAt(hudRoot.transform,pos,size,new Color(0,0,0,0),new Vector2(0,0),new Vector2(0,0),size,"BUTTON_"+label);
        CircleGraphic g=o.AddComponent<CircleGraphic>(); g.color=color;
        TextAt(o.transform,label,new Vector2(0,0),size,fontSize,FontStyle.Bold,Color.white,TextAnchor.MiddleCenter);
        return o;
    }

    public void UpdateHUD(FPSPlayer p,int alive,int total,int score,int kills)
    {
        if(p==null || hudRoot==null) return;
        objectiveText.text=$"ELIMINATE HOSTILES   •   {alive} LEFT   •   SCORE {score}";
        hpText.text=$"HP  {p.Health:000}";
        ammoText.text=$"{p.Weapons.CurrentAmmo:00} / {p.Weapons.CurrentReserve:000}";
        weaponText.text=p.Weapons.CurrentName+(p.Weapons.IsAiming?"   /   ADS":"");

        Transform bar=hudRoot.transform.Find("HPBAR");
        if(bar!=null) bar.GetComponent<RectTransform>().sizeDelta=new Vector2(230f*Mathf.Clamp01(p.Health/100f),8f);

        if(feedTimer>0f)
        {
            feedTimer-=Time.deltaTime;
            if(feedTimer<=0f) killFeedText.text="";
        }
    }

    public void ShowKillFeed(string message,int points)
    {
        if(killFeedText==null) return;
        killFeedText.text=$"{message}   +{points}";
        feedTimer=1.1f;
    }

    public void ShowResult(bool victory,int score,int kills)
    {
        ClearRoot(ref hudRoot); ClearRoot(ref resultRoot);
        resultRoot=Panel("RESULT",canvas.transform,new Color(0.015f,0.020f,0.026f,0.94f));
        string title=victory?"MISSION COMPLETE":"MISSION FAILED";
        TextAt(resultRoot.transform,title,new Vector2(0,365),new Vector2(1920,96),56,FontStyle.Bold,victory?new Color(0.82f,0.94f,0.88f):new Color(0.96f,0.62f,0.60f),TextAnchor.MiddleCenter);
        TextAt(resultRoot.transform,$"SCORE  {score}\nKILLS  {kills}",new Vector2(0,465),new Vector2(1920,140),26,FontStyle.Normal,new Color(0.76f,0.80f,0.82f),TextAnchor.MiddleCenter);
        Button again=ButtonAt(resultRoot.transform,"PLAY AGAIN",new Vector2(710,650),new Vector2(240,66),20,new Color(0.12f,0.25f,0.32f),Color.white);
        again.onClick.AddListener(()=>game.StartMatch((int)game.CurrentMap));
        Button menu=ButtonAt(resultRoot.transform,"MAIN MENU",new Vector2(970,650),new Vector2(240,66),20,new Color(0.07f,0.09f,0.11f),Color.white);
        menu.onClick.AddListener(ShowMenu);
    }

    private void ShowSettings()
    {
        if(settingsRoot!=null) Destroy(settingsRoot);
        settingsRoot=Panel("SETTINGS",menuRoot.transform,new Color(0.02f,0.028f,0.036f,0.98f));
        TextAt(settingsRoot.transform,"SETTINGS",new Vector2(0,-130),new Vector2(1920,60),28,FontStyle.Bold,Color.white,TextAnchor.MiddleCenter);
        TextAt(settingsRoot.transform,"LOOK SENSITIVITY",new Vector2(620,-250),new Vector2(680,36),16,FontStyle.Bold,new Color(0.72f,0.78f,0.81f),TextAnchor.MiddleLeft);

        GameObject sliderRoot=PanelAt(settingsRoot.transform,new Vector2(620,-300),new Vector2(680,70),new Color(0,0,0,0),new Vector2(0,1),new Vector2(0,1),new Vector2(680,70),"SENS_SLIDER");
        Slider slider=sliderRoot.AddComponent<Slider>();
        GameObject bg=PanelAt(sliderRoot.transform,new Vector2(0,8),new Vector2(680,12),new Color(0.18f,0.22f,0.24f,1f),new Vector2(0,1),new Vector2(0,1),new Vector2(680,12));
        slider.targetGraphic=bg.GetComponent<Image>();
        GameObject fill=PanelAt(sliderRoot.transform,new Vector2(0,8),new Vector2(680,12),new Color(0.25f,0.50f,0.58f,1f),new Vector2(0,1),new Vector2(0,1),new Vector2(680,12));
        slider.fillRect=fill.GetComponent<RectTransform>();
        GameObject handle=PanelAt(sliderRoot.transform,new Vector2(340,14),new Vector2(30,40),Color.white,new Vector2(0,1),new Vector2(0,1),new Vector2(30,40),"HANDLE");
        slider.handleRect=handle.GetComponent<RectTransform>();
        slider.minValue=0.5f; slider.maxValue=1.8f; slider.value=PlayerPrefs.GetFloat("SITF_SENS",1f);
        slider.onValueChanged.AddListener(v=>{PlayerPrefs.SetFloat("SITF_SENS",v);PlayerPrefs.Save();});
        Button close=ButtonAt(settingsRoot.transform,"CLOSE",new Vector2(830,610),new Vector2(260,64),19,new Color(0.10f,0.13f,0.15f),Color.white);
        close.onClick.AddListener(()=>Destroy(settingsRoot));
    }

    private static Color MapAccent(SITFMap map)
    {
        switch(map)
        {
            case SITFMap.Warehouse:return new Color(0.56f,0.43f,0.23f);
            case SITFMap.Office:return new Color(0.28f,0.48f,0.55f);
            case SITFMap.Yard:return new Color(0.25f,0.44f,0.25f);
            case SITFMap.Hangar:return new Color(0.42f,0.45f,0.48f);
            case SITFMap.Metro:return new Color(0.18f,0.42f,0.44f);
            default:return new Color(0.48f,0.27f,0.23f);
        }
    }

    private void ClearRoot(ref GameObject root){if(root!=null){Destroy(root);root=null;}}

    private GameObject Panel(string name,Transform parent,Color color)
    {
        GameObject o=new GameObject(name);o.transform.SetParent(parent,false);
        RectTransform r=o.AddComponent<RectTransform>();r.anchorMin=new Vector2(0,0);r.anchorMax=new Vector2(1,1);r.pivot=new Vector2(0,1);r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;
        Image img=o.AddComponent<Image>();img.color=color;img.raycastTarget=false;return o;
    }

    private GameObject PanelAt(Transform parent,Vector2 pos,Vector2 size,Color color,Vector2 anchorMin,Vector2 anchorMax,Vector2 sizeDelta,string name="PANEL")
    {
        GameObject o=new GameObject(name);o.transform.SetParent(parent,false);
        RectTransform r=o.AddComponent<RectTransform>();r.anchorMin=anchorMin;r.anchorMax=anchorMax;r.pivot=new Vector2(0,1);r.anchoredPosition=pos;r.sizeDelta=sizeDelta==Vector2.zero?size:sizeDelta;
        Image img=o.AddComponent<Image>();img.color=color;img.raycastTarget=false;return o;
    }

    private Text TextAt(Transform parent,string value,Vector2 pos,Vector2 size,int sizePx,FontStyle style,Color color,TextAnchor anchor)
    {
        GameObject o=new GameObject("TEXT");o.transform.SetParent(parent,false);
        RectTransform r=o.AddComponent<RectTransform>();r.anchorMin=new Vector2(0,1);r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=pos;r.sizeDelta=size;
        Text t=o.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=sizePx;t.fontStyle=style;t.color=color;t.alignment=anchor;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;return t;
    }

    private Button ButtonAt(Transform parent,string label,Vector2 pos,Vector2 size,int fontSize,Color bg,Color fg)
    {
        GameObject o=new GameObject("BUTTON_"+label);o.transform.SetParent(parent,false);
        RectTransform r=o.AddComponent<RectTransform>();r.anchorMin=new Vector2(0,1);r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=pos;r.sizeDelta=size;
        Image img=o.AddComponent<Image>();img.color=bg;img.raycastTarget=true;
        Outline outline=o.AddComponent<Outline>();outline.effectColor=new Color(fg.r,fg.g,fg.b,0.18f);outline.effectDistance=new Vector2(1.2f,-1.2f);
        Button b=o.AddComponent<Button>();b.targetGraphic=img;
        ColorBlock cb=b.colors;cb.normalColor=Color.white;cb.highlightedColor=new Color(1,1,1,0.86f);cb.pressedColor=new Color(0.75f,0.82f,0.86f,1f);cb.selectedColor=Color.white;b.colors=cb;
        Text text=TextAt(o.transform,label,new Vector2(0,0),size,fontSize,FontStyle.Bold,fg,TextAnchor.MiddleCenter);text.raycastTarget=false;
        return b;
    }
}
