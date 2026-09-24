extends CharacterBody3D

var game
var archetype := "Assault"
var max_health := 100.0
var health := 100.0
var move_speed := 3.4
var damage := 9.0
var fire_interval := 0.72
var attack_range := 28.0
var body_color := Color("#b7353d")
var cooldown := 0.4
var wander_target := Vector3.ZERO
var wander_timer := 0.0
var strafe_sign := 1.0
var alive := true

func setup(owner_game, type_name: String, color: Color):
    game = owner_game
    archetype = type_name
    body_color = color
    match archetype:
        "Scout":
            max_health = 65.0
            move_speed = 5.2
            damage = 7.0
            fire_interval = 0.5
        "Heavy":
            max_health = 185.0
            move_speed = 2.25
            damage = 17.0
            fire_interval = 1.0
        "Marksman":
            max_health = 85.0
            move_speed = 2.9
            damage = 28.0
            fire_interval = 1.55
            attack_range = 42.0
        _:
            max_health = 100.0
            move_speed = 3.5
            damage = 10.0
            fire_interval = 0.72
    health = max_health

func _ready():
    add_to_group("enemies")
    _build_model()
    var shape := CollisionShape3D.new()
    var capsule := CapsuleShape3D.new()
    capsule.radius = 0.48
    capsule.height = 1.8
    shape.shape = capsule
    shape.position.y = 0.9
    add_child(shape)
    wander_target = global_position + Vector3(randf_range(-8, 8), 0, randf_range(-8, 8))

func _build_model():
    var dark := StandardMaterial3D.new()
    dark.albedo_color = body_color.darkened(0.32)
    dark.roughness = 0.8
    var light := StandardMaterial3D.new()
    light.albedo_color = body_color.lightened(0.12)
    light.roughness = 0.72
    var skin := StandardMaterial3D.new()
    skin.albedo_color = Color("#b56a4f")
    skin.roughness = 0.9

    var torso := MeshInstance3D.new()
    var tm := BoxMesh.new()
    tm.size = Vector3(0.92, 0.95, 0.55)
    torso.mesh = tm
    torso.material_override = dark
    torso.position.y = 1.08
    add_child(torso)

    var head := MeshInstance3D.new()
    var hm := BoxMesh.new()
    hm.size = Vector3(0.66, 0.66, 0.66)
    head.mesh = hm
    head.material_override = skin
    head.position.y = 1.82
    add_child(head)

    var helmet := MeshInstance3D.new()
    var helm := BoxMesh.new()
    helm.size = Vector3(0.73, 0.18, 0.72)
    helmet.mesh = helm
    helmet.material_override = dark
    helmet.position.y = 2.16
    add_child(helmet)

    for side in [-1.0, 1.0]:
        var arm := MeshInstance3D.new()
        var am := BoxMesh.new()
        am.size = Vector3(0.25, 0.82, 0.25)
        arm.mesh = am
        arm.material_override = light
        arm.position = Vector3(0.62 * side, 1.10, 0)
        add_child(arm)
        var leg := MeshInstance3D.new()
        var lm := BoxMesh.new()
        lm.size = Vector3(0.31, 0.88, 0.34)
        leg.mesh = lm
        leg.material_override = dark
        leg.position = Vector3(0.24 * side, 0.36, 0)
        add_child(leg)

    var weapon := MeshInstance3D.new()
    var wm := BoxMesh.new()
    wm.size = Vector3(0.2, 0.2, 0.82)
    weapon.mesh = wm
    weapon.material_override = dark
    weapon.position = Vector3(0.0, 1.14, -0.52)
    weapon.rotation_degrees.x = 90
    add_child(weapon)

func take_damage(amount: float, hit_pos: Vector3):
    if not alive:
        return
    health -= amount
    game.spawn_hit_spark(hit_pos, true)
    if health <= 0.0:
        die()

func die():
    if not alive:
        return
    alive = false
    game.spawn_blood(global_position + Vector3.UP * 1.0)
    game.enemy_killed(self)
    var tween := create_tween()
    tween.set_parallel()
    tween.tween_property(self, "rotation_degrees.x", -78.0, 0.22)
    tween.tween_property(self, "position:y", global_position.y - 0.18, 0.32)
    await tween.finished
    queue_free()

func _physics_process(delta):
    if game == null or game.match_over or not alive:
        return

    var player = game.player
    if player == null:
        return

    cooldown -= delta
    wander_timer -= delta
    var to_player := player.global_position - global_position
    to_player.y = 0
    var dist := to_player.length()
    var has_los := game.enemy_has_los(self)

    if wander_timer <= 0.0 or wander_target.distance_to(global_position) < 1.8:
        wander_timer = randf_range(2.5, 5.0)
        wander_target = global_position + Vector3(randf_range(-10, 10), 0, randf_range(-10, 10))
        if randf() < 0.5:
            strafe_sign *= -1

    var desired := Vector3.ZERO

    if dist < attack_range and has_los:
        var face := to_player.normalized()
        global_rotation.y = lerp_angle(global_rotation.y, atan2(-face.x, -face.z), delta * 5.0)
        if archetype == "Marksman":
            desired = face * (0.18 if dist > 16.0 else -0.55)
        elif archetype == "Scout":
            desired = (face + face.cross(Vector3.UP) * strafe_sign * 0.52).normalized()
        else:
            desired = (face * (1.0 if dist > 8.0 else -0.18) + face.cross(Vector3.UP) * strafe_sign * 0.4).normalized()

        if cooldown <= 0.0 and dist < attack_range:
            cooldown = fire_interval
            game.enemy_shoot(self, player)
    elif dist < 38.0:
        var face := to_player.normalized()
        desired = face
        global_rotation.y = lerp_angle(global_rotation.y, atan2(-face.x, -face.z), delta * 3.5)
    else:
        var wander := wander_target - global_position
        wander.y = 0
        if wander.length() > 0.2:
            desired = wander.normalized()
            global_rotation.y = lerp_angle(global_rotation.y, atan2(-desired.x, -desired.z), delta * 2.0)

    velocity.x = move_toward(velocity.x, desired.x * move_speed, 12.0 * delta)
    velocity.z = move_toward(velocity.z, desired.z * move_speed, 12.0 * delta)
    if is_on_floor():
        velocity.y = -0.35
    else:
        velocity.y -= 20.0 * delta
    move_and_slide()

    if global_position.y < -7:
        queue_free()
