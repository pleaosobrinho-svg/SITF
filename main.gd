extends Node3D

var player: CharacterBody3D
var enemies: Array[Node3D] = []

func _ready():
    _build_world()
    _spawn_player()
    _spawn_enemies()
    _build_ui()

func _build_world():
    var env := WorldEnvironment.new()
    var e := Environment.new()
    e.background_mode = Environment.BG_COLOR
    e.background_color = Color("#0b1018")
    e.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
    e.ambient_light_color = Color("#71809a")
    e.ambient_light_energy = 0.7
    env.environment = e
    add_child(env)

    var sun := DirectionalLight3D.new()
    sun.rotation_degrees = Vector3(-48, -25, 0)
    sun.light_energy = 1.1
    sun.shadow_enabled = true
    add_child(sun)

    _box("Ground", Vector3(0,-0.5,0), Vector3(36,1,36), Color("#252b31"))
    _box("WallNorth", Vector3(0,2,-18), Vector3(36,4,1), Color("#3a4148"))
    _box("WallSouth", Vector3(0,2,18), Vector3(36,4,1), Color("#3a4148"))
    _box("WallEast", Vector3(18,2,0), Vector3(1,4,36), Color("#3a4148"))
    _box("WallWest", Vector3(-18,2,0), Vector3(1,4,36), Color("#3a4148"))

    for p in [Vector3(-7,1,-5), Vector3(7,1,-3), Vector3(-5,1,7), Vector3(8,1,8), Vector3(0,1,-10)]:
        _box("Cover", p, Vector3(3,2,3), Color("#59616a"))

func _box(n: String, pos: Vector3, size: Vector3, color: Color):
    var body := StaticBody3D.new()
    body.name = n
    body.position = pos
    var mesh := MeshInstance3D.new()
    var box := BoxMesh.new()
    box.size = size
    mesh.mesh = box
    var mat := StandardMaterial3D.new()
    mat.albedo_color = color
    mesh.material_override = mat
    body.add_child(mesh)
    var shape := CollisionShape3D.new()
    var bshape := BoxShape3D.new()
    bshape.size = size
    shape.shape = bshape
    body.add_child(shape)
    add_child(body)

func _spawn_player():
    player = CharacterBody3D.new()
    player.name = "Player"
    player.set_script(load("res://player.gd"))
    player.position = Vector3(0,1,12)
    add_child(player)

func _spawn_enemies():
    var script = load("res://enemy.gd")
    var spots = [Vector3(-10,1,-10),Vector3(10,1,-10),Vector3(-10,1,8),Vector3(10,1,10),Vector3(0,1,-7)]
    for i in spots.size():
        var enemy := CharacterBody3D.new()
        enemy.name = "Enemy_%02d" % i
        enemy.set_script(script)
        enemy.position = spots[i]
        enemy.target = player
        add_child(enemy)
        enemies.append(enemy)

func _build_ui():
    var layer := CanvasLayer.new()
    layer.name = "HUD"
    add_child(layer)

    var title := Label.new()
    title.text = "SITF  //  SILENCE IN THE FIRE"
    title.position = Vector2(24,18)
    title.add_theme_font_size_override("font_size",22)
    layer.add_child(title)

    var info := Label.new()
    info.text = "WASD: move  •  Mouse: aim  •  Click: fire"
    info.position = Vector2(24,52)
    info.add_theme_font_size_override("font_size",16)
    layer.add_child(info)

    var cross := Label.new()
    cross.text = "+"
    cross.position = Vector2(634,346)
    cross.add_theme_font_size_override("font_size",26)
    layer.add_child(cross)

    var hp := Label.new()
    hp.name = "HP"
    hp.text = "HP  100"
    hp.position = Vector2(24,660)
    hp.add_theme_font_size_override("font_size",20)
    layer.add_child(hp)
