extends Node3D

const PLAYER_SCRIPT = preload("res://player.gd")
const ENEMY_SCRIPT = preload("res://enemy.gd")
const JOYSTICK_SCRIPT = preload("res://touch_joystick.gd")

var player: CharacterBody3D
var enemies: Array[Node3D] = []
var level_root: Node3D
var hud: CanvasLayer
var menu: Control
var settings_panel: Panel
var selected_map := 0
var in_game := false
var sensitivity := 0.0025
var score := 0

func _ready():
    _build_environment()
    _build_menu()

func _build_environment():
    var env := WorldEnvironment.new()
    env.name = "Environment"
    var e := Environment.new()
    e.background_mode = Environment.BG_COLOR
    e.background_color = Color("#081019")
    e.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
    e.ambient_light_color = Color("#7890a8")
    e.ambient_light_energy = 0.75
    e.tonemap_mode = Environment.TONE_MAPPER_FILMIC
    env.environment = e
    add_child(env)

    var sun := DirectionalLight3D.new()
    sun.rotation_degrees = Vector3(-52, -28, 0)
    sun.light_energy = 1.15
    sun.shadow_enabled = true
    add_child(sun)

func _build_menu():
    menu = Control.new()
    menu.name = "MainMenu"
    menu.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    add_child(menu)

    var backdrop := ColorRect.new()
    backdrop.color = Color("#080d13")
    backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    menu.add_child(backdrop)

    var title := Label.new()
    title.text = "SITF"
    title.position = Vector2(88, 95)
    title.add_theme_font_size_override("font_size", 72)
    menu.add_child(title)

    var sub := Label.new()
    sub.text = "SILENCE IN THE FIRE"
    sub.position = Vector2(94, 170)
    sub.add_theme_font_size_override("font_size", 22)
    menu.add_child(sub)

    var mode := Label.new()
    mode.text = "3D MOBILE FPS  •  ORIGINAL FICTIONAL COMBAT"
    mode.position = Vector2(96, 205)
    mode.add_theme_font_size_override("font_size", 15)
    menu.add_child(mode)

    var map_label := Label.new()
    map_label.text = "MAP"
    map_label.position = Vector2(760, 105)
    map_label.add_theme_font_size_override("font_size", 18)
    menu.add_child(map_label)

    var map_a := _make_button("WAREHOUSE", Vector2(760, 145), Vector2(300, 58))
    menu.add_child(map_a)
    var map_b := _make_button("YARD", Vector2(760, 215), Vector2(300, 58))
    menu.add_child(map_b)
    map_a.pressed.connect(func(): _select_map(0, map_a, map_b))
    map_b.pressed.connect(func(): _select_map(1, map_a, map_b))
    _select_map(0, map_a, map_b)

    var start := _make_button("START MISSION", Vector2(760, 315), Vector2(300, 66))
    start.pressed.connect(_start_game)
    menu.add_child(start)

    var settings := _make_button("SETTINGS", Vector2(760, 395), Vector2(300, 52))
    settings.pressed.connect(_toggle_settings)
    menu.add_child(settings)

    settings_panel = Panel.new()
    settings_panel.position = Vector2(760, 465)
    settings_panel.size = Vector2(300, 175)
    settings_panel.visible = false
    menu.add_child(settings_panel)

    var s_title := Label.new()
    s_title.text = "LOOK SENSITIVITY"
    s_title.position = Vector2(20, 18)
    s_title.add_theme_font_size_override("font_size", 16)
    settings_panel.add_child(s_title)

    var slider := HSlider.new()
    slider.name = "Sensitivity"
    slider.position = Vector2(20, 52)
    slider.size = Vector2(260, 30)
    slider.min_value = 0.0012
    slider.max_value = 0.005
    slider.step = 0.0001
    slider.value = sensitivity
    slider.value_changed.connect(func(v): sensitivity = float(v))
    settings_panel.add_child(slider)

    var hint := Label.new()
    hint.text = "Desktop: mouse aim\nAndroid: drag the right side"
    hint.position = Vector2(20, 94)
    hint.add_theme_font_size_override("font_size", 14)
    settings_panel.add_child(hint)

func _make_button(text_value: String, pos: Vector2, size_value: Vector2) -> Button:
    var b := Button.new()
    b.text = text_value
    b.position = pos
    b.size = size_value
    b.add_theme_font_size_override("font_size", 18)

    var normal := StyleBoxFlat.new()
    normal.bg_color = Color("#18232e")
    normal.corner_radius_top_left = 8
    normal.corner_radius_top_right = 8
    normal.corner_radius_bottom_left = 8
    normal.corner_radius_bottom_right = 8
    normal.border_width_left = 1
    normal.border_width_right = 1
    normal.border_width_top = 1
    normal.border_width_bottom = 1
    normal.border_color = Color("#33485b")
    b.add_theme_stylebox_override("normal", normal)

    var hover := normal.duplicate()
    hover.bg_color = Color("#223447")
    b.add_theme_stylebox_override("hover", hover)
    return b

func _select_map(id: int, a: Button, b: Button):
    selected_map = id
    if is_instance_valid(a):
        a.text = ("✓ " if selected_map == 0 else "") + "WAREHOUSE"
    if is_instance_valid(b):
        b.text = ("✓ " if selected_map == 1 else "") + "YARD"

func _toggle_settings():
    settings_panel.visible = not settings_panel.visible

func _start_game():
    menu.visible = false
    in_game = true
    score = 0
    _build_level(selected_map)
    _spawn_player()
    _spawn_enemies()
    _build_hud()

func _build_level(map_id: int):
    level_root = Node3D.new()
    level_root.name = "Level"
    add_child(level_root)

    _box(Vector3(0,-0.5,0), Vector3(40,1,40), Color("#20272d"))
    _box(Vector3(0,2,-20), Vector3(40,4,1), Color("#394149"))
    _box(Vector3(0,2,20), Vector3(40,4,1), Color("#394149"))
    _box(Vector3(-20,2,0), Vector3(1,4,40), Color("#394149"))
    _box(Vector3(20,2,0), Vector3(1,4,40), Color("#394149"))

    if map_id == 0:
        _box(Vector3(-8,1,-6), Vector3(4,2,3), Color("#515c66"))
        _box(Vector3(8,1,-6), Vector3(4,2,3), Color("#515c66"))
        _box(Vector3(-8,1,7), Vector3(4,2,3), Color("#66717a"))
        _box(Vector3(8,1,7), Vector3(4,2,3), Color("#66717a"))
        _box(Vector3(0,1,-11), Vector3(3,2,5), Color("#444e57"))
        _box(Vector3(-14,1,0), Vector3(2,2,6), Color("#4c565e"))
        _box(Vector3(14,1,0), Vector3(2,2,6), Color("#4c565e"))
    else:
        _box(Vector3(-11,1,-8), Vector3(5,2,2), Color("#59656b"))
        _box(Vector3(-2,1,-3), Vector3(7,2,2), Color("#465159"))
        _box(Vector3(9,1,-8), Vector3(5,2,2), Color("#59656b"))
        _box(Vector3(-9,1,8), Vector3(6,2,2), Color("#465159"))
        _box(Vector3(2,1,6), Vector3(4,2,4), Color("#5e696e"))
        _box(Vector3(11,1,9), Vector3(5,2,2), Color("#465159"))

func _box(pos: Vector3, size_value: Vector3, color: Color):
    var body := StaticBody3D.new()
    body.position = pos

    var mesh := MeshInstance3D.new()
    var box_mesh := BoxMesh.new()
    box_mesh.size = size_value
    mesh.mesh = box_mesh
    var mat := StandardMaterial3D.new()
    mat.albedo_color = color
    mesh.material_override = mat
    body.add_child(mesh)

    var shape := CollisionShape3D.new()
    var box_shape := BoxShape3D.new()
    box_shape.size = size_value
    shape.shape = box_shape
    body.add_child(shape)
    level_root.add_child(body)

func _spawn_player():
    player = CharacterBody3D.new()
    player.name = "Player"
    player.set_script(PLAYER_SCRIPT)
    player.position = Vector3(0, 1.05, 14)
    add_child(player)

func _spawn_enemies():
    var spots: Array[Vector3]
    if selected_map == 0:
        spots = [Vector3(-12,1,-12), Vector3(12,1,-12), Vector3(-13,1,6), Vector3(13,1,6), Vector3(0,1,-14), Vector3(5,1,-2)]
    else:
        spots = [Vector3(-14,1,-10), Vector3(-5,1,-10), Vector3(6,1,-10), Vector3(14,1,-10), Vector3(-12,1,10), Vector3(12,1,10)]

    for i in spots.size():
        var enemy := CharacterBody3D.new()
        enemy.name = "Enemy_%02d" % i
        enemy.set_script(ENEMY_SCRIPT)
        enemy.position = spots[i]
        enemy.target = player
        add_child(enemy)
        enemies.append(enemy)
        enemy.died.connect(_on_enemy_died)

func _build_hud():
    hud = CanvasLayer.new()
    hud.name = "HUD"
    add_child(hud)

    var top := Label.new()
    top.name = "Top"
    top.text = "SITF  //  " + ("WAREHOUSE" if selected_map == 0 else "YARD")
    top.position = Vector2(24, 18)
    top.add_theme_font_size_override("font_size", 22)
    hud.add_child(top)

    var objective := Label.new()
    objective.name = "Objective"
    objective.position = Vector2(24, 50)
    objective.add_theme_font_size_override("font_size", 15)
    hud.add_child(objective)

    var hp := Label.new()
    hp.name = "HP"
    hp.position = Vector2(24, 655)
    hp.add_theme_font_size_override("font_size", 20)
    hud.add_child(hp)

    var weapon := Label.new()
    weapon.name = "Weapon"
    weapon.position = Vector2(905, 625)
    weapon.add_theme_font_size_override("font_size", 18)
    hud.add_child(weapon)

    var cross := Label.new()
    cross.text = "+"
    cross.position = Vector2(634, 338)
    cross.add_theme_font_size_override("font_size", 28)
    hud.add_child(cross)

    if OS.has_feature("android") or DisplayServer.is_touchscreen_available():
        var joystick := Control.new()
        joystick.name = "Joystick"
        joystick.set_script(JOYSTICK_SCRIPT)
        joystick.position = Vector2(38, 470)
        joystick.size = Vector2(210, 210)
        hud.add_child(joystick)
        player.setup_mobile_controls(joystick)

        var fire := Button.new()
        fire.name = "Fire"
        fire.text = "FIRE"
        fire.position = Vector2(1060, 515)
        fire.size = Vector2(155, 105)
        fire.add_theme_font_size_override("font_size", 22)
        hud.add_child(fire)
        fire.button_down.connect(player.start_fire)
        fire.button_up.connect(player.stop_fire)

        var reload := Button.new()
        reload.name = "Reload"
        reload.text = "RELOAD"
        reload.position = Vector2(900, 535)
        reload.size = Vector2(135, 55)
        reload.add_theme_font_size_override("font_size", 15)
        hud.add_child(reload)
        reload.pressed.connect(player.reload)

        var w1 := _make_hud_button("SMG", Vector2(900, 600))
        var w2 := _make_hud_button("RIFLE", Vector2(1030, 600))
        var w3 := _make_hud_button("SHOTGUN", Vector2(1160, 600))
        hud.add_child(w1)
        hud.add_child(w2)
        hud.add_child(w3)
        w1.pressed.connect(func(): player.switch_weapon(0))
        w2.pressed.connect(func(): player.switch_weapon(1))
        w3.pressed.connect(func(): player.switch_weapon(2))
    else:
        var help := Label.new()
        help.text = "WASD move  •  Mouse aim  •  LMB fire  •  R reload  •  1/2/3 weapons"
        help.position = Vector2(24, 82)
        help.add_theme_font_size_override("font_size", 14)
        hud.add_child(help)

func _make_hud_button(t: String, p: Vector2) -> Button:
    var b := Button.new()
    b.text = t
    b.position = p
    b.size = Vector2(120, 42)
    b.add_theme_font_size_override("font_size", 13)
    return b

func _process(_delta):
    if not in_game or not is_instance_valid(player):
        return

    var alive := 0
    for e in enemies:
        if is_instance_valid(e) and not e.is_queued_for_deletion():
            alive += 1

    var hp = hud.get_node_or_null("HP")
    var weapon = hud.get_node_or_null("Weapon")
    var objective = hud.get_node_or_null("Objective")
    if hp:
        hp.text = "HP  %d" % player.health
    if weapon:
        weapon.text = "%s   %d / %d" % [player.current_weapon_name(), player.current_ammo(), player.reserve_ammo()]
    if objective:
        objective.text = "ELIMINATE HOSTILES  •  ENEMIES: %d  •  SCORE: %d" % [alive, score]

    if alive == 0 and not hud.get_node_or_null("Victory"):
        _show_victory()

    if player.health <= 0:
        _show_death()

func _on_enemy_died(_enemy):
    score += 100

func _show_victory():
    var panel := ColorRect.new()
    panel.name = "Victory"
    panel.color = Color(0.02,0.04,0.035,0.92)
    panel.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    hud.add_child(panel)

    var text := Label.new()
    text.text = "MISSION COMPLETE\nSCORE %d" % score
    text.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
    text.position = Vector2(0, 245)
    text.size = Vector2(1280, 120)
    text.add_theme_font_size_override("font_size", 42)
    panel.add_child(text)

    var again := Button.new()
    again.text = "PLAY AGAIN"
    again.position = Vector2(540, 405)
    again.size = Vector2(200, 60)
    again.add_theme_font_size_override("font_size", 22)
    again.pressed.connect(func(): get_tree().reload_current_scene())
    panel.add_child(again)

func _show_death():
    if hud.get_node_or_null("Death"):
        return
    var panel := ColorRect.new()
    panel.name = "Death"
    panel.color = Color(0.04,0.015,0.02,0.92)
    panel.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    hud.add_child(panel)

    var text := Label.new()
    text.text = "YOU ARE DOWN\nSCORE %d" % score
    text.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
    text.position = Vector2(0, 245)
    text.size = Vector2(1280, 120)
    text.add_theme_font_size_override("font_size", 42)
    panel.add_child(text)

    var retry := Button.new()
    retry.text = "RESTART"
    retry.position = Vector2(540, 405)
    retry.size = Vector2(200, 60)
    retry.add_theme_font_size_override("font_size", 22)
    retry.pressed.connect(func(): get_tree().reload_current_scene())
    panel.add_child(retry)

func spawn_tracer(a: Vector3, b: Vector3, hot := false):
    var mid := (a + b) * 0.5
    var length := a.distance_to(b)
    var mesh := MeshInstance3D.new()
    var box := BoxMesh.new()
    box.size = Vector3(0.025, 0.025, length)
    mesh.mesh = box
    mesh.position = mid
    mesh.look_at(b, Vector3.UP)

    var mat := StandardMaterial3D.new()
    mat.albedo_color = Color("#ffd36b") if hot else Color("#a8c6e8")
    mat.emission_enabled = true
    mat.emission = Color("#fff0b0")
    mat.emission_energy_multiplier = 2.0
    mesh.material_override = mat
    add_child(mesh)

    var tween := create_tween()
    tween.tween_property(mesh, "scale", Vector3(0.2,0.2,1.0), 0.06)
    tween.tween_callback(mesh.queue_free)

func spawn_impact(pos: Vector3, normal := Vector3.UP):
    var flash := OmniLight3D.new()
    flash.position = pos
    flash.light_color = Color("#ffbe7a")
    flash.light_energy = 3.5
    flash.omni_range = 2.0
    add_child(flash)
    var tween := create_tween()
    tween.tween_property(flash, "light_energy", 0.0, 0.07)
    tween.tween_callback(flash.queue_free)

    var dot := MeshInstance3D.new()
    var cube := BoxMesh.new()
    cube.size = Vector3(0.07,0.07,0.07)
    dot.mesh = cube
    dot.position = pos + normal * 0.03
    var mat := StandardMaterial3D.new()
    mat.albedo_color = Color("#d6bba0")
    dot.material_override = mat
    add_child(dot)

    var t2 := create_tween()
    t2.tween_property(dot, "scale", Vector3(0.1,0.1,0.1), 0.18)
    t2.tween_callback(dot.queue_free)

func spawn_blood(pos: Vector3, normal := Vector3.UP):
    for i in 7:
        var p := MeshInstance3D.new()
        var cube := BoxMesh.new()
        cube.size = Vector3(0.055,0.055,0.055)
        p.mesh = cube
        p.position = pos

        var mat := StandardMaterial3D.new()
        mat.albedo_color = Color("#a61f2b")
        p.material_override = mat
        add_child(p)

        var velocity := Vector3(randf_range(-1.4,1.4), randf_range(0.4,1.7), randf_range(-1.4,1.4)) + normal
        var tween := create_tween()
        tween.tween_property(p, "position", pos + velocity * 0.25, 0.28)
        tween.parallel().tween_property(p, "scale", Vector3.ZERO, 0.28)
        tween.tween_callback(p.queue_free)
