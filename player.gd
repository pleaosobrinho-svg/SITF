extends CharacterBody3D

var speed := 6.5
var gravity := 19.0
var health := 100
var yaw := 0.0
var pitch := -0.05
var camera: Camera3D
var weapon_root: Node3D
var can_shoot := true
var reloading := false
var fire_pressed := false
var joystick: Control
var look_sensitivity := 0.0025
var current_weapon := 0

var weapons := [
    {"name":"SMG", "damage":22, "delay":0.085, "mag":30, "reserve":120, "spread":0.012, "pellets":1},
    {"name":"RIFLE", "damage":36, "delay":0.16, "mag":30, "reserve":120, "spread":0.006, "pellets":1},
    {"name":"SHOTGUN", "damage":13, "delay":0.78, "mag":8, "reserve":48, "spread":0.055, "pellets":8}
]
var ammo := [30, 30, 8]
var reserves := [120, 120, 48]

func _ready():
    look_sensitivity = get_tree().current_scene.sensitivity
    _build_body()
    _build_camera()
    _build_weapon()
    Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

func _build_body():
    var collider := CollisionShape3D.new()
    var capsule := CapsuleShape3D.new()
    capsule.radius = 0.36
    capsule.height = 1.72
    collider.shape = capsule
    collider.position.y = 0.86
    add_child(collider)

    var torso := MeshInstance3D.new()
    var torso_mesh := BoxMesh.new()
    torso_mesh.size = Vector3(0.72, 0.95, 0.46)
    torso.mesh = torso_mesh
    torso.position.y = 0.95
    var mat := StandardMaterial3D.new()
    mat.albedo_color = Color("#456fa9")
    torso.material_override = mat
    add_child(torso)

func _build_camera():
    camera = Camera3D.new()
    camera.position = Vector3(0, 1.48, 0)
    camera.current = true
    camera.fov = 78
    add_child(camera)

func _build_weapon():
    weapon_root = Node3D.new()
    weapon_root.position = Vector3(0.34, -0.25, -0.56)
    camera.add_child(weapon_root)
    _make_weapon_mesh()

func _make_weapon_mesh():
    for c in weapon_root.get_children():
        c.queue_free()

    var body := MeshInstance3D.new()
    var body_mesh := BoxMesh.new()
    body_mesh.size = Vector3(0.18,0.18,0.55)
    body.mesh = body_mesh
    var mat := StandardMaterial3D.new()
    mat.albedo_color = Color("#14181d")
    body.material_override = mat
    weapon_root.add_child(body)

    var barrel := MeshInstance3D.new()
    var barrel_mesh := BoxMesh.new()
    barrel_mesh.size = Vector3(0.07,0.07,0.48 if current_weapon != 2 else 0.7)
    barrel.mesh = barrel_mesh
    barrel.position = Vector3(0,0,-0.48)
    barrel.material_override = mat
    weapon_root.add_child(barrel)

    var grip := MeshInstance3D.new()
    var grip_mesh := BoxMesh.new()
    grip_mesh.size = Vector3(0.11,0.27,0.14)
    grip.mesh = grip_mesh
    grip.position = Vector3(0,-0.18,0.05)
    grip.rotation_degrees.x = -12
    grip.material_override = mat
    weapon_root.add_child(grip)

func _unhandled_input(event):
    if event is InputEventMouseMotion:
        yaw -= event.relative.x * look_sensitivity
        pitch = clamp(pitch - event.relative.y * look_sensitivity, -1.25, 1.25)
        _apply_look()

    if event is InputEventKey and event.pressed:
        if event.keycode == KEY_1: switch_weapon(0)
        if event.keycode == KEY_2: switch_weapon(1)
        if event.keycode == KEY_3: switch_weapon(2)
        if event.keycode == KEY_R: reload()
        if event.keycode == KEY_ESCAPE:
            Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)

    if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
        if event.pressed:
            start_fire()
        else:
            stop_fire()

    if event is InputEventScreenDrag and event.position.x > get_viewport().get_visible_rect().size.x * 0.38:
        yaw -= event.relative.x * look_sensitivity * 1.25
        pitch = clamp(pitch - event.relative.y * look_sensitivity * 1.25, -1.25, 1.25)
        _apply_look()

func _apply_look():
    rotation.y = yaw
    camera.rotation.x = pitch

func _physics_process(delta):
    if health <= 0:
        velocity = Vector3.ZERO
        return

    var input_vec := Input.get_vector("ui_left","ui_right","ui_up","ui_down")
    if joystick:
        input_vec = joystick.value

    var dir := (transform.basis * Vector3(input_vec.x, 0, input_vec.y)).normalized()
    velocity.x = move_toward(velocity.x, dir.x * speed, 28.0 * delta)
    velocity.z = move_toward(velocity.z, dir.z * speed, 28.0 * delta)

    if not is_on_floor():
        velocity.y -= gravity * delta
    else:
        velocity.y = -0.1

    move_and_slide()

    if fire_pressed:
        shoot()

func setup_mobile_controls(j: Control):
    joystick = j

func start_fire():
    fire_pressed = true
    if Input.get_mouse_mode() != Input.MOUSE_MODE_CAPTURED:
        Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

func stop_fire():
    fire_pressed = false

func switch_weapon(index: int):
    if index < 0 or index >= weapons.size() or reloading:
        return
    current_weapon = index
    _make_weapon_mesh()

func current_weapon_name() -> String:
    return weapons[current_weapon].name

func current_ammo() -> int:
    return ammo[current_weapon]

func reserve_ammo() -> int:
    return reserves[current_weapon]

func shoot():
    if not can_shoot or reloading or health <= 0:
        return

    if ammo[current_weapon] <= 0:
        reload()
        return

    can_shoot = false
    ammo[current_weapon] -= 1
    var w: Dictionary = weapons[current_weapon]

    for i in int(w.pellets):
        var from := camera.global_position
        var forward := -camera.global_transform.basis.z
        var dir := (forward + Vector3(
            randf_range(-float(w.spread), float(w.spread)),
            randf_range(-float(w.spread), float(w.spread)),
            randf_range(-float(w.spread), float(w.spread))
        )).normalized()
        var to := from + dir * 120.0

        var query := PhysicsRayQueryParameters3D.create(from, to)
        query.exclude = [self]
        var hit := get_world_3d().direct_space_state.intersect_ray(query)

        if hit:
            var obj = hit.collider
            if obj.has_method("take_damage"):
                obj.take_damage(int(w.damage), hit.position)
            else:
                var root := get_tree().current_scene
                if root.has_method("spawn_impact"):
                    root.spawn_impact(hit.position, hit.normal)

            var root2 := get_tree().current_scene
            if root2.has_method("spawn_tracer"):
                root2.spawn_tracer(from, hit.position, current_weapon == 0)

    _play_shot_sound(current_weapon)

    var muzzle := OmniLight3D.new()
    muzzle.position = Vector3(0,0,-0.78)
    muzzle.light_color = Color("#ffd49a")
    muzzle.light_energy = 5.0
    muzzle.omni_range = 2.4
    weapon_root.add_child(muzzle)

    weapon_root.position.z = -0.48
    var recoil := create_tween()
    recoil.tween_property(weapon_root, "position:z", -0.64, 0.045)
    recoil.tween_property(weapon_root, "position:z", -0.56, 0.07)
    recoil.tween_callback(muzzle.queue_free)

    await get_tree().create_timer(float(w.delay)).timeout
    can_shoot = true

func reload():
    if reloading or ammo[current_weapon] >= int(weapons[current_weapon].mag) or reserves[current_weapon] <= 0:
        return

    reloading = true
    fire_pressed = false
    _play_reload_sound()
    await get_tree().create_timer(1.25 if current_weapon != 2 else 1.5).timeout

    var need: int = int(weapons[current_weapon].mag) - ammo[current_weapon]
    var give: int = min(need, reserves[current_weapon])
    ammo[current_weapon] += give
    reserves[current_weapon] -= give
    reloading = false

func take_damage(amount: int):
    health = max(health - amount, 0)

func _play_tone(freq: float, duration: float, volume: float):
    var stream := AudioStreamGenerator.new()
    stream.mix_rate = 22050
    stream.buffer_length = duration + 0.03

    var speaker := AudioStreamPlayer.new()
    speaker.stream = stream
    add_child(speaker)
    speaker.play()

    var playback := speaker.get_stream_playback() as AudioStreamGeneratorPlayback
    if playback:
        var frames := int(stream.mix_rate * duration)
        for i in frames:
            var t := float(i) / stream.mix_rate
            var env := exp(-16.0 * t)
            var sample := sin(TAU * freq * t) * env * volume
            playback.push_frame(Vector2(sample, sample))

    await get_tree().create_timer(duration + 0.05).timeout
    if is_instance_valid(speaker):
        speaker.queue_free()

func _play_shot_sound(kind: int):
    var freq := 520.0 if kind == 0 else (690.0 if kind == 1 else 180.0)
    var vol := 0.25 if kind == 0 else (0.3 if kind == 1 else 0.42)
    _play_tone(freq, 0.085 if kind < 2 else 0.16, vol)

func _play_reload_sound():
    _play_tone(260.0, 0.11, 0.16)
