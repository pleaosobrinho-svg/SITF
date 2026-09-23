extends Control

var value := Vector2.ZERO
var finger_id := -1
var radius := 74.0
var knob_radius := 28.0

func _ready():
    mouse_filter = Control.MOUSE_FILTER_STOP
    queue_redraw()

func _gui_input(event):
    if event is InputEventScreenTouch:
        if event.pressed and finger_id == -1:
            finger_id = event.index
            _update_value(event.position)
            accept_event()
        elif not event.pressed and event.index == finger_id:
            finger_id = -1
            value = Vector2.ZERO
            queue_redraw()
            accept_event()

    elif event is InputEventScreenDrag and event.index == finger_id:
        _update_value(event.position)
        accept_event()

func _update_value(pos: Vector2):
    var center := size * 0.5
    var delta := pos - center
    if delta.length() > radius:
        delta = delta.normalized() * radius
    value = delta / radius
    queue_redraw()

func _draw():
    var center := size * 0.5
    draw_circle(center, radius + 8.0, Color(0.04,0.06,0.08,0.72))
    draw_circle(center, radius, Color(0.12,0.16,0.20,0.76))
    draw_circle(center + value * radius, knob_radius, Color(0.28,0.38,0.48,0.95))
