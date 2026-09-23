using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MobileControls : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public static MobileControls Active { get; private set; }
    public Vector2 MoveInput { get; private set; }
    public bool FireHeld { get; private set; }
    public bool AimHeld { get; private set; }

    private Vector2 lookDelta;
    private Vector2 lookLast;
    private int lookPointer=-1;

    public void Initialize(RectTransform targetRect)
    {
        Active=this;
        Image image=gameObject.GetComponent<Image>();
        if(image==null) image=gameObject.AddComponent<Image>();
        image.color=new Color(1f,1f,1f,0.001f);
        image.raycastTarget=true;
    }

    public void SetMove(Vector2 value)=>MoveInput=Vector2.ClampMagnitude(value,1f);
    public void ClearMove()=>MoveInput=Vector2.zero;
    public void SetFire(bool value)=>FireHeld=value;
    public void SetAim(bool value)=>AimHeld=value;
    public void ClearCombat(){FireHeld=false;AimHeld=false;}

    public void OnPointerDown(PointerEventData eventData)
    {
        if(lookPointer!=-1) return;
        lookPointer=eventData.pointerId;
        lookLast=eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(eventData.pointerId!=lookPointer) return;
        Vector2 current=eventData.position;
        lookDelta+=current-lookLast;
        lookLast=current;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if(eventData.pointerId==lookPointer) lookPointer=-1;
    }

    public Vector2 ConsumeLookDelta()
    {
        Vector2 d=lookDelta;
        lookDelta=Vector2.zero;
        return d;
    }
}

public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ActionType { Fire,Aim }
    private ActionType action;
    private MobileControls controls;

    public void Initialize(MobileControls target,ActionType type){controls=target;action=type;}

    public void OnPointerDown(PointerEventData eventData)
    {
        if(controls==null) return;
        if(action==ActionType.Fire) controls.SetFire(true); else controls.SetAim(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if(controls==null) return;
        if(action==ActionType.Fire) controls.SetFire(false); else controls.SetAim(false);
    }
}

public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public float Radius=78f;
    private MobileControls controls;
    private RectTransform rect;
    private RectTransform knob;

    public void Initialize(MobileControls target){controls=target;rect=GetComponent<RectTransform>();knob=transform.Find("KNOB") as RectTransform;}
    public void OnPointerDown(PointerEventData eventData)=>UpdatePointer(eventData);
    public void OnDrag(PointerEventData eventData)=>UpdatePointer(eventData);
    public void OnPointerUp(PointerEventData eventData){if(knob!=null) knob.anchoredPosition=Vector2.zero; if(controls!=null) controls.ClearMove();}

    private void UpdatePointer(PointerEventData e)
    {
        if(rect==null || knob==null || controls==null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,e.position,e.pressEventCamera,out Vector2 local);
        Vector2 delta=Vector2.ClampMagnitude(local,Radius);
        knob.anchoredPosition=delta;
        controls.SetMove(delta/Radius);
    }
}
