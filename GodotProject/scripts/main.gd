extends Node3D
@warning_ignore_start("inferred_declaration")

const PLAYER_SCRIPT = preload("res://scripts/player.gd")
const ENEMY_SCRIPT = preload("res://scripts/enemy.gd")

var player
var camera
var world := Node3D.new()
var ui := CanvasLayer.new()
var menu_panel
var hud_panel
var map_select_panel
var settings_panel
var menu_preview_root
var map_id := 0
var match_over := false
var score := 0
var kills := 0
var wave_alive := 0
var player_hp := 100.0
var ammo := 30
var reserve := 120
var reloading := false
var reload_left := 0.0
var weapon_index := 0
var weapon_kick := 0.0
var ads := false
var fire_held := false
var fire_timer := 0.0
var match_time := 0.0
var muzzle_flash_left := 0.0

var hud_title: Label
var hud_stats: Label
var hud_ammo: Label
var hud_status: Label
var crosshair: Label
var damage_flash: ColorRect
var weapon_root: Node3D
var weapon_model: Node3D
var look_area: Control
var joystick_base: Control
var joystick_knob: Control
var joystick_touch_id := -1
var virtual_move := Vector2.ZERO
var touch_sensitivity := 0.11

var sound_players: Array[AudioStreamPlayer] = []
var sound_cursor := 0

var weapons := [
    {"name":"PISTOL", "mag":15, "reserve":105, "damage":24.0, "delay":0.22, "range":65.0, "pellets":1, "spread":0.008, "recoil":0.042, "color":Color("#d8dde4")},
    {"name":"SMG", "mag":32, "reserve":192, "damage":13.0, "delay":0.075, "range":56.0, "pellets":1, "spread":0.022, "recoil":0.025, "color":Color("#3b4c5c")},
    {"name":"RIFLE", "mag":30, "reserve":180, "damage":28.0, "delay":0.115, "range":84.0, "pellets":1, "spread":0.012, "recoil":0.033, "color":Color("#53606d")},
    {"name":"SHOTGUN", "mag":8, "reserve":64, "damage":14.0, "delay":0.82, "range":36.0, "pellets":9, "spread":0.075, "recoil":0.09, "color":Color("#725139")},
    {"name":"DMR", "mag":12, "reserve":72, "damage":56.0, "delay":0.42, "range":110.0, "pellets":1, "spread":0.005, "recoil":0.060, "color":Color("#8b96a1")},
    {"name":"SNIPER", "mag":5, "reserve":35, "damage":110.0, "delay":1.25, "range":150.0, "pellets":1, "spread":0.001, "recoil":0.14, "color":Color("#4d5661")}
]

var maps := [
    {"name":"WAREHOUSE", "size":Vector2(46,34), "theme":Color("#30363c"), "accent":Color("#d27638"), "seed":11},
    {"name":"OFFICE", "size":Vector2(42,32), "theme":Color("#34383b"), "accent":Color("#5c89b6"), "seed":23},
    {"name":"YARD", "size":Vector2(54,38), "theme":Color("#3e4642"), "accent":Color("#b69a57"), "seed":37},
    {"name":"HANGAR", "size":Vector2(58,42), "theme":Color("#313940"), "accent":Color("#8c9aa5"), "seed":49},
    {"name":"METRO", "size":Vector2(48,30), "theme":Color("#302e32"), "accent":Color("#a44767"), "seed":61},
    {"name":"BLOCK", "size":Vector2(52,36), "theme":Color("#3b3e44"), "accent":Color("#7d9b5e"), "seed":73}
]

func _ready():
    randomize()
    Engine.max_fps = 60
    add_child(world)
    add_child(ui)
    _build_audio()
    _build_menu()

func _process(delta):
    if match_over or player == null:
        return

    _update_virtual_move_actions()
    match_time += delta
    fire_timer = maxf(0.0, fire_timer - delta)
    weapon_kick = lerpf(weapon_kick, 0.0, delta * 13.0)
    muzzle_flash_left = maxf(0.0, muzzle_flash_left - delta)

    if reloading:
        reload_left -= delta
        if reload_left <= 0.0:
            reloading = false
            var w = weapons[weapon_index]
            var need = int(w.mag) - ammo
            var give = mini(need, reserve)
            ammo += give
            reserve -= give
            _play_sound("reload")

    if fire_held and not reloading and fire_timer <= 0.0:
        _try_fire()

    if Input.is_action_just_pressed("reload"):
        _start_reload()

    for i in range(6):
        var action := "weapon_%d" % (i + 1)
        if Input.is_action_just_pressed(action):
            _equip(i)

    _update_weapon_visual(delta)
    _update_hud()

func _build_menu():
    menu_panel = Panel.new()
    menu_panel.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    var bg := StyleBoxFlat.new()
    bg.bg_color = Color("#07090e")
    menu_panel.add_theme_stylebox_override("panel", bg)
    ui.add_child(menu_panel)
    _build_menu_preview()

    var logo := TextureRect.new()
    var logo_tex := load("res://icon.svg")
    if logo_tex:
        logo.texture = logo_tex
    logo.position = Vector2(92, 72)
    logo.size = Vector2(260, 260)
    logo.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
    logo.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
    menu_panel.add_child(logo)

    var title := Label.new()
    title.text = "SILENCE IN THE FIRE"
    title.position = Vector2(96, 345)
    title.add_theme_font_size_override("font_size", 30)
    title.add_theme_color_override("font_color", Color("#e9edf3"))
    menu_panel.add_child(title)

    var subtitle := Label.new()
    subtitle.text = "FIRST-PERSON COMBAT"
    subtitle.position = Vector2(98, 387)
    subtitle.add_theme_font_size_override("font_size", 15)
    subtitle.add_theme_color_override("font_color", Color("#7f8b9a"))
    menu_panel.add_child(subtitle)

    var play := Button.new()
    play.text = "PLAY"
    play.position = Vector2(96, 485)
    play.size = Vector2(390, 82)
    play.add_theme_font_size_override("font_size", 30)
    play.add_theme_stylebox_override("normal", _button_style(Color("#d34b24"), Color("#ffb06b")))
    play.add_theme_stylebox_override("hover", _button_style(Color("#ed5b2d"), Color("#ffd0a5")))
    play.add_theme_stylebox_override("pressed", _button_style(Color("#9f3218"), Color("#ffd0a5")))
    play.pressed.connect(_show_map_select)
    menu_panel.add_child(play)

    var settings := Button.new()
    settings.text = "SETTINGS"
    settings.position = Vector2(96, 580)
    settings.size = Vector2(390, 62)
    settings.add_theme_font_size_override("font_size", 21)
    settings.add_theme_stylebox_override("normal", _button_style(Color("#141a22"), Color("#526070")))
    settings.add_theme_stylebox_override("hover", _button_style(Color("#1e2732"), Color("#d27638")))
    settings.pressed.connect(_show_settings)
    menu_panel.add_child(settings)

    var hint := Label.new()
    hint.text = "MOBILE READY  •  VIRTUAL JOYSTICK + TOUCH AIM"
    hint.position = Vector2(98, 680)
    hint.add_theme_font_size_override("font_size", 14)
    hint.add_theme_color_override("font_color", Color("#667384"))
    menu_panel.add_child(hint)

    var version := Label.new()
    version.text = "SITF  //  v0.2"
    version.position = Vector2(1730, 1010)
    version.add_theme_font_size_override("font_size", 14)
    version.add_theme_color_override("font_color", Color("#586372"))
    menu_panel.add_child(version)

func _build_menu_preview():
    menu_preview_root = Node3D.new()
    menu_preview_root.name = "MenuPreview"
    world.add_child(menu_preview_root)

    var camera_preview := Camera3D.new()
    camera_preview.position = Vector3(5.8, 2.7, 7.4)
    camera_preview.fov = 42.0
    camera_preview.look_at(Vector3(1.2, 1.25, 0.0), Vector3.UP)
    menu_preview_root.add_child(camera_preview)

    var floor := MeshInstance3D.new()
    var floor_mesh := BoxMesh.new()
    floor_mesh.size = Vector3(13, 0.25, 9)
    floor.mesh = floor_mesh
    floor.position = Vector3(1.0, -0.15, 0.0)
    floor.material_override = _mat(Color("#171d25"), 0.05, 0.9)
    menu_preview_root.add_child(floor)

    var glow := OmniLight3D.new()
    glow.position = Vector3(1.5, 4.0, 2.0)
    glow.omni_range = 12.0
    glow.light_energy = 5.0
    glow.light_color = Color("#d27638")
    menu_preview_root.add_child(glow)

    var rim := OmniLight3D.new()
    rim.position = Vector3(-3.5, 2.8, -3.0)
    rim.omni_range = 10.0
    rim.light_energy = 4.0
    rim.light_color = Color("#557da6")
    menu_preview_root.add_child(rim)

    var hero := Node3D.new()
    hero.position = Vector3(1.2, 0.0, 0.0)
    menu_preview_root.add_child(hero)

    var torso := MeshInstance3D.new()
    var torso_mesh := BoxMesh.new()
    torso_mesh.size = Vector3(0.85, 1.15, 0.48)
    torso.mesh = torso_mesh
    torso.position = Vector3(0, 1.35, 0)
    torso.material_override = _mat(Color("#242d37"), 0.15, 0.65)
    hero.add_child(torso)

    var vest := MeshInstance3D.new()
    var vest_mesh := BoxMesh.new()
    vest_mesh.size = Vector3(0.94, 0.72, 0.12)
    vest.mesh = vest_mesh
    vest.position = Vector3(0, 1.42, -0.27)
    vest.material_override = _mat(Color("#3b4652"), 0.1, 0.72)
    hero.add_child(vest)

    var head := MeshInstance3D.new()
    var head_mesh := BoxMesh.new()
    head_mesh.size = Vector3(0.55, 0.55, 0.55)
    head.mesh = head_mesh
    head.position = Vector3(0, 2.25, 0)
    head.material_override = _mat(Color("#b66e52"), 0.0, 0.9)
    hero.add_child(head)

    var helmet := MeshInstance3D.new()
    var helmet_mesh := BoxMesh.new()
    helmet_mesh.size = Vector3(0.64, 0.20, 0.62)
    helmet.mesh = helmet_mesh
    helmet.position = Vector3(0, 2.53, 0)
    helmet.material_override = _mat(Color("#11171e"), 0.2, 0.5)
    hero.add_child(helmet)

    for side in [-1.0, 1.0]:
        var arm := MeshInstance3D.new()
        var arm_mesh := BoxMesh.new()
        arm_mesh.size = Vector3(0.22, 0.92, 0.25)
        arm.mesh = arm_mesh
        arm.position = Vector3(side * 0.60, 1.42, -0.02)
        arm.rotation_degrees.z = side * -9.0
        arm.material_override = _mat(Color("#2c3742"), 0.1, 0.72)
        hero.add_child(arm)

        var leg := MeshInstance3D.new()
        var leg_mesh := BoxMesh.new()
        leg_mesh.size = Vector3(0.28, 1.05, 0.30)
        leg.mesh = leg_mesh
        leg.position = Vector3(side * 0.23, 0.52, 0)
        leg.material_override = _mat(Color("#202832"), 0.1, 0.78)
        hero.add_child(leg)

    var rifle := MeshInstance3D.new()
    var rifle_mesh := BoxMesh.new()
    rifle_mesh.size = Vector3(0.14, 0.16, 1.55)
    rifle.mesh = rifle_mesh
    rifle.position = Vector3(0.42, 1.30, -0.62)
    rifle.rotation_degrees.x = -8.0
    rifle.rotation_degrees.z = -12.0
    rifle.material_override = _mat(Color("#111419"), 0.35, 0.45)
    hero.add_child(rifle)

    var light := DirectionalLight3D.new()
    light.rotation_degrees = Vector3(-35, -130, 0)
    light.light_energy = 1.1
    light.shadow_enabled = true
    menu_preview_root.add_child(light)

func _show_map_select():
    menu_panel.visible = false
    map_select_panel = Panel.new()
    map_select_panel.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    var bg := StyleBoxFlat.new()
    bg.bg_color = Color("#080b10")
    map_select_panel.add_theme_stylebox_override("panel", bg)
    ui.add_child(map_select_panel)

    var title := Label.new()
    title.text = "SELECT MAP"
    title.position = Vector2(92, 72)
    title.add_theme_font_size_override("font_size", 46)
    title.add_theme_color_override("font_color", Color("#edf1f5"))
    map_select_panel.add_child(title)

    var sub := Label.new()
    sub.text = "CHOOSE YOUR DEPLOYMENT"
    sub.position = Vector2(96, 130)
    sub.add_theme_font_size_override("font_size", 17)
    sub.add_theme_color_override("font_color", Color("#7f8b9a"))
    map_select_panel.add_child(sub)

    for i in range(maps.size()):
        var b := Button.new()
        b.text = "%02d   %s" % [i + 1, maps[i].name]
        b.position = Vector2(92 + (i / 3) * 470, 205 + (i % 3) * 150)
        b.size = Vector2(420, 112)
        b.add_theme_font_size_override("font_size", 25)
        b.add_theme_stylebox_override("normal", _button_style(Color("#141a22"), maps[i].accent))
        b.add_theme_stylebox_override("hover", _button_style(Color("#202a35"), maps[i].accent.lightened(0.2)))
        b.pressed.connect(func(i=i): _start_match(i))
        map_select_panel.add_child(b)

    var back := Button.new()
    back.text = "BACK"
    back.position = Vector2(92, 885)
    back.size = Vector2(210, 60)
    back.add_theme_font_size_override("font_size", 20)
    back.add_theme_stylebox_override("normal", _button_style(Color("#11161d"), Color("#566375")))
    back.pressed.connect(func():
        map_select_panel.queue_free()
        map_select_panel = null
        menu_panel.visible = true
    )
    map_select_panel.add_child(back)

func _show_settings():
    settings_panel = Panel.new()
    settings_panel.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    var bg := StyleBoxFlat.new()
    bg.bg_color = Color(0.02, 0.025, 0.04, 0.96)
    settings_panel.add_theme_stylebox_override("panel", bg)
    ui.add_child(settings_panel)

    var title := Label.new()
    title.text = "SETTINGS"
    title.position = Vector2(0, 170)
    title.size = Vector2(1920, 70)
    title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
    title.add_theme_font_size_override("font_size", 42)
    title.add_theme_color_override("font_color", Color("#edf1f5"))
    settings_panel.add_child(title)

    var sens := Label.new()
    sens.text = "TOUCH LOOK SENSITIVITY"
    sens.position = Vector2(0, 290)
    sens.size = Vector2(1920, 40)
    sens.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
    sens.add_theme_font_size_override("font_size", 20)
    sens.add_theme_color_override("font_color", Color("#8995a5"))
    settings_panel.add_child(sens)

    var values := [0.075, 0.11, 0.16]
    var names := ["LOW", "MEDIUM", "HIGH"]
    for i in range(3):
        var b := Button.new()
        b.text = names[i]
        b.position = Vector2(690 + i * 190, 350)
        b.size = Vector2(160, 60)
        b.add_theme_font_size_override("font_size", 18)
        b.add_theme_stylebox_override("normal", _button_style(Color("#141a22"), Color("#566375")))
        b.pressed.connect(func(v=values[i]):
            touch_sensitivity = v
            if player:
                player.touch_sensitivity = v
        )
        settings_panel.add_child(b)

    var close := Button.new()
    close.text = "CLOSE"
    close.position = Vector2(760, 520)
    close.size = Vector2(400, 66)
    close.add_theme_font_size_override("font_size", 22)
    close.add_theme_stylebox_override("normal", _button_style(Color("#d34b24"), Color("#ffb06b")))
    close.pressed.connect(func():
        settings_panel.queue_free()
        settings_panel = null
    )
    settings_panel.add_child(close)

func _button_style(fill: Color, border: Color) -> StyleBoxFlat:
    var s := StyleBoxFlat.new()
    s.bg_color = fill
    s.border_color = border
    s.set_border_width_all(2)
    s.corner_radius_top_left = 8
    s.corner_radius_top_right = 8
    s.corner_radius_bottom_left = 8
    s.corner_radius_bottom_right = 8
    return s

func _start_match(selected: int):
    map_id = selected
    if map_select_panel:
        map_select_panel.queue_free()
        map_select_panel = null
    menu_panel.visible = false
    match_over = false
    score = 0
    kills = 0
    player_hp = 100.0
    match_time = 0.0
    reloading = false
    fire_held = false
    ads = false
    _clear_world()
    _build_environment()
    _spawn_player()
    _spawn_enemies()
    _build_hud()
    await get_tree().process_frame
    _equip(0)

func _clear_world():
    joystick_touch_id = -1
    virtual_move = Vector2.ZERO
    for action in ["move_forward", "move_back", "move_left", "move_right", "fire"]:
        Input.action_release(action)
    for n in world.get_children():
        n.queue_free()
    for n in ui.get_children():
        if n != menu_panel:
            n.queue_free()
    player = null
    camera = null
    weapon_root = null
    hud_panel = null
    menu_preview_root = null

func _build_environment():
    var data = maps[map_id]
    var half_x: float = data.size.x * 0.5
    var half_z: float = data.size.y * 0.5
    _add_box(Vector3(0, -0.6, 0), Vector3(data.size.x, 1.0, data.size.y), data.theme.darkened(0.22), true, "floor")

    var wall_mat = data.theme.lightened(0.15)
    _add_box(Vector3(0, 3.0, -half_z), Vector3(data.size.x, 6.0, 0.8), wall_mat, true, "north_wall")
    _add_box(Vector3(0, 3.0, half_z), Vector3(data.size.x, 6.0, 0.8), wall_mat, true, "south_wall")
    _add_box(Vector3(-half_x, 3.0, 0), Vector3(0.8, 6.0, data.size.y), wall_mat, true, "west_wall")
    _add_box(Vector3(half_x, 3.0, 0), Vector3(0.8, 6.0, data.size.y), wall_mat, true, "east_wall")

    var rng := RandomNumberGenerator.new()
    rng.seed = data.seed
    for i in range(26):
        var x := rng.randf_range(-half_x + 4.0, half_x - 4.0)
        var z := rng.randf_range(-half_z + 4.0, half_z - 4.0)
        if Vector2(x, z).length() < 7.5:
            continue
        var w := rng.randf_range(1.7, 4.5)
        var d := rng.randf_range(1.7, 4.5)
        var h := rng.randf_range(1.1, 3.2)
        if i % 5 == 0:
            h += 2.5
        _add_box(Vector3(x, h * 0.5, z), Vector3(w, h, d), data.accent.darkened(0.30), true, "cover")

    for i in range(10):
        var x := rng.randf_range(-half_x + 3.0, half_x - 3.0)
        var z := rng.randf_range(-half_z + 3.0, half_z - 3.0)
        var pole := MeshInstance3D.new()
        var mesh := CylinderMesh.new()
        mesh.top_radius = 0.16
        mesh.bottom_radius = 0.22
        mesh.height = 4.0
        mesh.radial_segments = 8
        pole.mesh = mesh
        pole.position = Vector3(x, 2.0, z)
        pole.material_override = _mat(data.accent.lightened(0.15))
        world.add_child(pole)

    var sign := Label3D.new()
    sign.text = maps[map_id].name
    sign.position = Vector3(0, 5.3, -half_z + 1.2)
    sign.billboard = BaseMaterial3D.BILLBOARD_ENABLED
    sign.modulate = data.accent.lightened(0.3)
    sign.font_size = 42
    world.add_child(sign)

    var env := WorldEnvironment.new()
    var environment := Environment.new()
    environment.background_mode = Environment.BG_COLOR
    environment.background_color = Color("#0b1017")
    environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
    environment.ambient_light_color = Color("#7b8796")
    environment.ambient_light_energy = 0.56
    environment.tonemap_mode = Environment.TONE_MAPPER_FILMIC
    environment.glow_enabled = true
    environment.glow_intensity = 0.62
    environment.fog_enabled = true
    environment.fog_light_color = Color("#131922")
    environment.fog_light_energy = 0.25
    environment.fog_density = 0.008
    env.environment = environment
    world.add_child(env)

    var sun := DirectionalLight3D.new()
    sun.rotation_degrees = Vector3(-48, -32, 0)
    sun.light_energy = 1.35
    sun.shadow_enabled = true
    sun.directional_shadow_max_distance = 70.0
    world.add_child(sun)

    var fill := OmniLight3D.new()
    fill.position = Vector3(0, 6, 0)
    fill.omni_range = 25
    fill.light_energy = 1.2
    fill.light_color = data.accent.lightened(0.2)
    world.add_child(fill)

func _add_box(pos: Vector3, size: Vector3, color: Color, collision := false, node_name := "box"):
    var root: Node3D
    if collision:
        root = StaticBody3D.new()
        var cs := CollisionShape3D.new()
        var bs := BoxShape3D.new()
        bs.size = size
        cs.shape = bs
        root.add_child(cs)
    else:
        root = Node3D.new()
    root.name = node_name
    root.position = pos
    var mesh_node := MeshInstance3D.new()
    var mesh := BoxMesh.new()
    mesh.size = size
    mesh_node.mesh = mesh
    mesh_node.material_override = _mat(color)
    root.add_child(mesh_node)
    world.add_child(root)
    return root

func _mat(color: Color, metallic := 0.0, roughness := 0.82) -> StandardMaterial3D:
    var m := StandardMaterial3D.new()
    m.albedo_color = color
    m.metallic = metallic
    m.roughness = roughness
    return m

func _spawn_player():
    player = CharacterBody3D.new()
    player.name = "Player"
    player.set_script(PLAYER_SCRIPT)
    player.position = Vector3(0, 0.4, 0)
    world.add_child(player)
    player.game = self
    player.touch_sensitivity = touch_sensitivity

func _spawn_enemies():
    var spawn_points := [
        Vector3(-16, 0.6, -10), Vector3(-8,0.6,-13), Vector3(9,0.6,-12),
        Vector3(16,0.6,-7), Vector3(18,0.6,7), Vector3(10,0.6,12),
        Vector3(-9,0.6,12), Vector3(-18,0.6,7), Vector3(-15,0.6,2),
        Vector3(-5,0.6,5), Vector3(7,0.6,5), Vector3(4,0.6,-6)
    ]
    var archetypes := ["Assault","Scout","Heavy","Marksman"]
    var colors := [Color("#a9333d"), Color("#3f91c9"), Color("#7a5662"), Color("#bb8c4e")]
    var count := 12 + map_id % 3
    for i in range(count):
        var e := CharacterBody3D.new()
        e.name = "Enemy_%02d" % i
        e.set_script(ENEMY_SCRIPT)
        e.setup(self, archetypes[i % archetypes.size()], colors[i % colors.size()])
        e.position = spawn_points[i % spawn_points.size()] + Vector3(randf_range(-2,2), 0, randf_range(-2,2))
        world.add_child(e)
    wave_alive = count

func _build_hud():
    hud_panel = Control.new()
    hud_panel.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    ui.add_child(hud_panel)

    hud_title = Label.new()
    hud_title.position = Vector2(34, 26)
    hud_title.text = "SITF  //  %s" % maps[map_id].name
    hud_title.add_theme_font_size_override("font_size", 24)
    hud_title.add_theme_color_override("font_color", Color("#e7ebf0"))
    hud_panel.add_child(hud_title)

    hud_stats = Label.new()
    hud_stats.position = Vector2(36, 62)
    hud_stats.add_theme_font_size_override("font_size", 18)
    hud_stats.add_theme_color_override("font_color", Color("#9eabb9"))
    hud_panel.add_child(hud_stats)

    hud_ammo = Label.new()
    hud_ammo.position = Vector2(1550, 870)
    hud_ammo.add_theme_font_size_override("font_size", 38)
    hud_ammo.add_theme_color_override("font_color", Color("#e7ebf0"))
    hud_ammo.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
    hud_ammo.size = Vector2(320, 70)
    hud_panel.add_child(hud_ammo)

    hud_status = Label.new()
    hud_status.position = Vector2(1250, 815)
    hud_status.add_theme_font_size_override("font_size", 18)
    hud_status.add_theme_color_override("font_color", Color("#d27638"))
    hud_status.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
    hud_status.size = Vector2(620, 55)
    hud_panel.add_child(hud_status)

    crosshair = Label.new()
    crosshair.text = "+"
    crosshair.position = Vector2(947, 515)
    crosshair.add_theme_font_size_override("font_size", 42)
    crosshair.add_theme_color_override("font_color", Color("#edf2f6"))
    hud_panel.add_child(crosshair)

    damage_flash = ColorRect.new()
    damage_flash.color = Color(0.7, 0.05, 0.05, 0.0)
    damage_flash.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    damage_flash.mouse_filter = Control.MOUSE_FILTER_IGNORE
    hud_panel.add_child(damage_flash)

    _build_touch_controls()

func _build_touch_controls():
    var hint := Label.new()
    hint.text = "MOVE"
    hint.position = Vector2(58, 676)
    hint.add_theme_font_size_override("font_size", 14)
    hint.add_theme_color_override("font_color", Color(0.85,0.88,0.92,0.65))
    hud_panel.add_child(hint)

    joystick_base = Panel.new()
    joystick_base.position = Vector2(38, 700)
    joystick_base.size = Vector2(210, 210)
    joystick_base.mouse_filter = Control.MOUSE_FILTER_STOP
    joystick_base.add_theme_stylebox_override("panel", _button_style(Color(0.05,0.07,0.10,0.52), Color("#718096")))
    joystick_base.gui_input.connect(_on_joystick_input)
    hud_panel.add_child(joystick_base)

    joystick_knob = Panel.new()
    joystick_knob.position = Vector2(65, 65)
    joystick_knob.size = Vector2(80, 80)
    joystick_knob.mouse_filter = Control.MOUSE_FILTER_IGNORE
    joystick_knob.add_theme_stylebox_override("panel", _button_style(Color(0.20,0.25,0.31,0.88), Color("#d27638")))
    joystick_base.add_child(joystick_knob)

    _add_hold_button("FIRE", Vector2(1635, 785), Vector2(205, 115), "fire")
    _add_action_button("ADS", Vector2(1450, 790), Vector2(135, 62), "ads_toggle")
    _add_action_button("RELOAD", Vector2(1450, 865), Vector2(150, 58), "reload")
    _add_action_button("JUMP", Vector2(1815, 875), Vector2(80, 58), "jump")
    _add_action_button("NEXT", Vector2(1760, 790), Vector2(125, 62), "next_weapon")

    var look_label := Label.new()
    look_label.text = "DRAG TO AIM"
    look_label.position = Vector2(790, 40)
    look_label.add_theme_font_size_override("font_size", 14)
    look_label.add_theme_color_override("font_color", Color(0.85,0.88,0.92,0.38))
    hud_panel.add_child(look_label)

    look_area = Control.new()
    look_area.position = Vector2(720, 80)
    look_area.size = Vector2(1160, 650)
    look_area.mouse_filter = Control.MOUSE_FILTER_STOP
    look_area.gui_input.connect(_on_look_input)
    hud_panel.add_child(look_area)

func _add_hold_button(text_value: String, pos: Vector2, size: Vector2, action: String):
    var b := Button.new()
    b.text = text_value
    b.position = pos
    b.size = size
    b.add_theme_font_size_override("font_size", 22 if action == "fire" else 19)
    b.modulate.a = 0.82
    b.add_theme_stylebox_override("normal", _button_style(Color(0.05,0.07,0.1,0.70), Color("#718096")))
    b.add_theme_stylebox_override("pressed", _button_style(Color(0.24,0.10,0.06,0.92), Color("#ffb06b")))
    b.button_down.connect(func(): Input.action_press(action))
    b.button_up.connect(func(): Input.action_release(action))
    hud_panel.add_child(b)

func _add_action_button(text_value: String, pos: Vector2, size: Vector2, action: String):
    var b := Button.new()
    b.text = text_value
    b.position = pos
    b.size = size
    b.add_theme_font_size_override("font_size", 17)
    b.modulate.a = 0.82
    b.add_theme_stylebox_override("normal", _button_style(Color(0.05,0.07,0.1,0.70), Color("#718096")))
    b.add_theme_stylebox_override("pressed", _button_style(Color(0.24,0.10,0.06,0.92), Color("#ffb06b")))
    if action == "next_weapon":
        b.pressed.connect(func(): _equip((weapon_index + 1) % weapons.size()))
    elif action == "ads_toggle":
        b.pressed.connect(func():
            ads = not ads
            if player:
                player.set_ads(ads)
        )
    else:
        b.pressed.connect(func():
            Input.action_press(action)
            get_tree().create_timer(0.05).timeout.connect(func(): Input.action_release(action))
        )
    hud_panel.add_child(b)

func _on_joystick_input(event):
    if event is InputEventScreenTouch:
        if event.pressed:
            joystick_touch_id = event.index
            _set_virtual_move(event.position)
        elif event.index == joystick_touch_id:
            joystick_touch_id = -1
            virtual_move = Vector2.ZERO
            joystick_knob.position = Vector2(65, 65)
    elif event is InputEventScreenDrag and event.index == joystick_touch_id:
        _set_virtual_move(event.position)

func _set_virtual_move(local_pos: Vector2):
    var center := Vector2(105, 105)
    var delta := local_pos - center
    if delta.length() > 88.0:
        delta = delta.normalized() * 88.0
    virtual_move = delta / 88.0
    joystick_knob.position = center + delta - Vector2(40, 40)

func _update_virtual_move_actions():
    for action in ["move_forward", "move_back", "move_left", "move_right"]:
        Input.action_release(action)
    if virtual_move.y < -0.25:
        Input.action_press("move_forward")
    if virtual_move.y > 0.25:
        Input.action_press("move_back")
    if virtual_move.x < -0.25:
        Input.action_press("move_left")
    if virtual_move.x > 0.25:
        Input.action_press("move_right")

func _try_fire():
    if ammo <= 0:
        _start_reload()
        return
    var w = weapons[weapon_index]
    ammo -= 1
    fire_timer = w.delay
    weapon_kick = w.recoil
    muzzle_flash_left = 0.065
    _play_sound("shot")

    var origin: Vector3 = camera.global_position
    var center := get_viewport().get_visible_rect().size * 0.5
    var dir: Vector3 = camera.project_ray_normal(center)
    for pellet in range(int(w.pellets)):
        var spread := Vector3(randfn(0.0, float(w.spread)), randfn(0.0, float(w.spread)), randfn(0.0, float(w.spread)))
        var final_dir: Vector3 = (dir + spread).normalized()
        var query := PhysicsRayQueryParameters3D.create(origin, origin + final_dir * float(w.range))
        query.exclude = [player]
        var hit := get_world_3d().direct_space_state.intersect_ray(query)
        var end_point: Vector3 = origin + final_dir * float(w.range)
        if not hit.is_empty():
            end_point = hit.position
            var collider = hit.collider
            if collider and collider.is_in_group("enemies"):
                collider.take_damage(float(w.damage), hit.position)
            else:
                spawn_hit_spark(hit.position, false)
        _spawn_tracer(origin, end_point)

    if ammo == 0:
        _start_reload()

func _spawn_tracer(a: Vector3, b: Vector3):
    var root := Node3D.new()
    world.add_child(root)
    var mi := MeshInstance3D.new()
    var mesh := CylinderMesh.new()
    mesh.top_radius = 0.013
    mesh.bottom_radius = 0.025
    mesh.height = a.distance_to(b)
    mesh.radial_segments = 6
    mi.mesh = mesh
    mi.material_override = _mat(Color("#f6d59b"), 0.15, 0.55)
    root.add_child(mi)
    root.global_position = (a + b) * 0.5
    root.look_at(b, Vector3.UP)
    var tween := create_tween()
    tween.tween_property(root, "scale", Vector3(0.35,0.35,0.35), 0.055)
    tween.tween_callback(root.queue_free)

func spawn_hit_spark(pos: Vector3, enemy_hit := false):
    var amount := 7 if enemy_hit else 4
    for i in range(amount):
        var p := MeshInstance3D.new()
        var mesh := SphereMesh.new()
        mesh.radius = 0.055 if enemy_hit else 0.035
        mesh.height = 0.11 if enemy_hit else 0.07
        mesh.radial_segments = 5
        mesh.rings = 3
        p.mesh = mesh
        p.material_override = _mat(Color("#c83d48") if enemy_hit else Color("#d8b06a"))
        p.global_position = pos
        world.add_child(p)
        var target := pos + Vector3(randf_range(-0.5,0.5), randf_range(0.0,0.7), randf_range(-0.5,0.5))
        var tween := create_tween()
        tween.set_parallel()
        tween.tween_property(p, "global_position", target, 0.26)
        tween.tween_property(p, "scale", Vector3.ZERO, 0.26)
        tween.chain().tween_callback(p.queue_free)

func spawn_blood(pos: Vector3):
    for i in range(10):
        var p := MeshInstance3D.new()
        var mesh := SphereMesh.new()
        mesh.radius = randf_range(0.025, 0.075)
        mesh.height = mesh.radius * 2.0
        mesh.radial_segments = 5
        mesh.rings = 3
        p.mesh = mesh
        p.material_override = _mat(Color("#8e2028"))
        p.global_position = pos
        world.add_child(p)
        var target := pos + Vector3(randf_range(-1.0,1.0), randf_range(-0.2,1.0), randf_range(-1.0,1.0))
        var tween := create_tween()
        tween.set_parallel()
        tween.tween_property(p, "global_position", target, 0.38)
        tween.tween_property(p, "scale", Vector3.ZERO, 0.38)
        tween.chain().tween_callback(p.queue_free)

func _start_reload():
    if reloading:
        return
    var w = weapons[weapon_index]
    if ammo >= int(w.mag) or reserve <= 0:
        return
    reloading = true
    reload_left = 1.0 + float(weapon_index) * 0.12
    hud_status.text = "RELOADING..."

func _equip(i: int):
    if i < 0 or i >= weapons.size():
        return
    weapon_index = i
    reloading = false
    var w = weapons[weapon_index]
    ammo = int(w.mag)
    reserve = int(w.reserve)
    _build_weapon_model()

func _build_weapon_model():
    if not camera:
        return
    if weapon_root:
        weapon_root.queue_free()
    weapon_root = Node3D.new()
    weapon_root.position = Vector3(0.33, -0.31, -0.68)
    camera.add_child(weapon_root)

    weapon_model = Node3D.new()
    weapon_root.add_child(weapon_model)
    var w = weapons[weapon_index]
    var metal = _mat(w.color, 0.35, 0.42)
    var black = _mat(Color("#101419"), 0.25, 0.53)
    var accent = _mat(maps[map_id].accent.lightened(0.18), 0.1, 0.45)

    var body := MeshInstance3D.new()
    var body_mesh := BoxMesh.new()
    var body_size := Vector3(0.22,0.20,0.72)
    if weapon_index == 3:
        body_size = Vector3(0.25,0.24,0.78)
    elif weapon_index == 5:
        body_size = Vector3(0.20,0.22,0.88)
    body_mesh.size = body_size
    body.mesh = body_mesh
    body.material_override = metal
    body.position = Vector3(0,0,-0.25)
    weapon_model.add_child(body)

    var barrel := MeshInstance3D.new()
    var barrel_mesh := CylinderMesh.new()
    barrel_mesh.top_radius = 0.045 if weapon_index != 3 else 0.065
    barrel_mesh.bottom_radius = barrel_mesh.top_radius
    barrel_mesh.height = 0.78 if weapon_index != 3 else 0.60
    barrel_mesh.radial_segments = 8
    barrel.mesh = barrel_mesh
    barrel.rotation_degrees.x = 90
    barrel.position = Vector3(0,0,-0.80)
    barrel.material_override = black
    weapon_model.add_child(barrel)

    var sight := MeshInstance3D.new()
    var sight_mesh := BoxMesh.new()
    sight_mesh.size = Vector3(0.08,0.11,0.18)
    sight.mesh = sight_mesh
    sight.position = Vector3(0,0.18,-0.34)
    sight.material_override = accent
    weapon_model.add_child(sight)

    var grip := MeshInstance3D.new()
    var grip_mesh := BoxMesh.new()
    grip_mesh.size = Vector3(0.15,0.42,0.18)
    grip.mesh = grip_mesh
    grip.position = Vector3(0,-0.27,-0.08)
    grip.rotation_degrees.x = -8
    grip.material_override = black
    weapon_model.add_child(grip)

    var hand := MeshInstance3D.new()
    var hand_mesh := BoxMesh.new()
    hand_mesh.size = Vector3(0.22,0.22,0.34)
    hand.mesh = hand_mesh
    hand.position = Vector3(0.0,-0.21,-0.49)
    hand.material_override = _mat(Color("#b66e52"))
    weapon_model.add_child(hand)

func _update_weapon_visual(delta):
    if not weapon_root:
        return
    var target := Vector3(0.33, -0.31, -0.68)
    if ads:
        target = Vector3(0.0, -0.23, -0.48)
    target.z += weapon_kick
    weapon_root.position = weapon_root.position.lerp(target, delta * 16.0)
    weapon_root.rotation_degrees.x = lerpf(weapon_root.rotation_degrees.x, weapon_kick * 140.0, delta * 16.0)

func _update_hud():
    if hud_stats == null:
        return
    var alive_count := get_tree().get_nodes_in_group("enemies").size()
    hud_stats.text = "HP %03d     KILLS %02d     TARGETS %02d     SCORE %05d" % [int(maxf(player_hp,0)), kills, alive_count, score]
    var w = weapons[weapon_index]
    hud_ammo.text = "%02d / %03d\n%s" % [ammo, reserve, w.name]
    hud_status.text = "RELOADING" if reloading else ("ADS" if ads else "ENGAGE")
    crosshair.text = "×" if fire_held else "+"

func player_damage(amount: float):
    if match_over:
        return
    player_hp -= amount
    damage_flash.color.a = 0.22
    var tween := create_tween()
    tween.tween_property(damage_flash, "color:a", 0.0, 0.18)
    _play_sound("hurt")
    if player_hp <= 0:
        _game_over(false)

func enemy_shoot(enemy, target):
    if target == null or match_over:
        return
    var from: Vector3 = enemy.global_position + Vector3(0,1.1,0)
    var to: Vector3 = target.global_position + Vector3(0,1.45,0)
    var query := PhysicsRayQueryParameters3D.create(from, to)
    query.exclude = [enemy]
    var hit := get_world_3d().direct_space_state.intersect_ray(query)
    var endpoint: Vector3 = to if hit.is_empty() else hit.position
    var accuracy := 0.78
    if enemy.archetype == "Scout":
        accuracy = 0.56
    elif enemy.archetype == "Heavy":
        accuracy = 0.88
    elif enemy.archetype == "Marksman":
        accuracy = 0.93
    if (hit.is_empty() or hit.collider == player) and randf() < accuracy:
        player_damage(enemy.damage)
    _spawn_tracer(from, endpoint)

func enemy_has_los(enemy) -> bool:
    if player == null:
        return false
    var from: Vector3 = enemy.global_position + Vector3.UP * 1.2
    var to: Vector3 = player.global_position + Vector3.UP * 1.4
    var query := PhysicsRayQueryParameters3D.create(from, to)
    query.exclude = [enemy]
    var hit := get_world_3d().direct_space_state.intersect_ray(query)
    return hit.is_empty() or hit.collider == player

func enemy_killed(enemy):
    if match_over:
        return
    kills += 1
    score += 100 + weapon_index * 20
    wave_alive = max(0, wave_alive - 1)
    _play_sound("hit")
    if wave_alive <= 0:
        _game_over(true)

func _game_over(victory: bool):
    match_over = true
    fire_held = false
    var overlay := Panel.new()
    overlay.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    var style := StyleBoxFlat.new()
    style.bg_color = Color(0.02,0.025,0.04,0.90)
    overlay.add_theme_stylebox_override("panel", style)
    ui.add_child(overlay)

    var big := Label.new()
    big.text = "MISSION COMPLETE" if victory else "YOU WERE ELIMINATED"
    big.position = Vector2(0, 270)
    big.size = Vector2(1920, 80)
    big.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
    big.add_theme_font_size_override("font_size", 52)
    big.add_theme_color_override("font_color", maps[map_id].accent.lightened(0.45))
    overlay.add_child(big)

    var stats := Label.new()
    stats.text = "MAP  %s     KILLS  %02d     SCORE  %05d\nTIME  %05.1f" % [maps[map_id].name, kills, score, match_time]
    stats.position = Vector2(0, 380)
    stats.size = Vector2(1920, 100)
    stats.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
    stats.add_theme_font_size_override("font_size", 24)
    stats.add_theme_color_override("font_color", Color("#b6c0cc"))
    overlay.add_child(stats)

    var retry := Button.new()
    retry.text = "REDEPLOY"
    retry.position = Vector2(760, 540)
    retry.size = Vector2(400, 70)
    retry.add_theme_font_size_override("font_size", 24)
    retry.add_theme_stylebox_override("normal", _button_style(Color("#141a22"), maps[map_id].accent))
    retry.pressed.connect(func(): overlay.queue_free(); _start_match(map_id))
    overlay.add_child(retry)

    var back := Button.new()
    back.text = "MAP SELECT"
    back.position = Vector2(760, 625)
    back.size = Vector2(400, 56)
    back.add_theme_font_size_override("font_size", 18)
    back.add_theme_stylebox_override("normal", _button_style(Color("#11161d"), Color("#566375")))
    back.pressed.connect(func():
        overlay.queue_free()
        match_over = false
        _return_to_main_menu()
    )
    overlay.add_child(back)

func _return_to_main_menu():
    _clear_world()
    menu_panel.visible = true
    _build_menu_preview()

func _build_audio():
    for i in range(8):
        var p := AudioStreamPlayer.new()
        p.volume_db = -5.0
        add_child(p)
        sound_players.append(p)

func _play_sound(kind: String):
    if sound_players.is_empty():
        return
    var p: AudioStreamPlayer = sound_players[sound_cursor]
    sound_cursor = (sound_cursor + 1) % sound_players.size()
    p.stream = _make_sound(kind)
    p.play()

func _make_sound(kind: String) -> AudioStreamWAV:
    var rate := 22050
    var duration := 0.14
    var base := 130.0
    var noise_mix := 0.35
    match kind:
        "shot":
            duration = 0.12
            base = 90.0 + weapon_index * 16.0
            noise_mix = 0.78
        "reload":
            duration = 0.28
            base = 340.0
            noise_mix = 0.08
        "hit":
            duration = 0.08
            base = 660.0
            noise_mix = 0.12
        "hurt":
            duration = 0.18
            base = 95.0
            noise_mix = 0.35

    var data := PackedByteArray()
    var samples := int(rate * duration)
    for i in range(samples):
        var t := float(i) / float(rate)
        var env := exp(-t * (18.0 if kind == "shot" else 8.0))
        var tone := sin(TAU * base * t) * 0.55
        var tone2 := sin(TAU * (base * 1.51) * t) * 0.25
        var noise := randf_range(-1.0, 1.0)
        var value: float = clampf((tone + tone2 + noise * noise_mix) * env, -1.0, 1.0)
        var q := int(value * 32767.0)
        data.append(q & 255)
        data.append((q >> 8) & 255)

    var wav := AudioStreamWAV.new()
    wav.format = AudioStreamWAV.FORMAT_16_BITS
    wav.mix_rate = rate
    wav.stereo = false
    wav.data = data
    return wav

@warning_ignore_restore("inferred_declaration")
