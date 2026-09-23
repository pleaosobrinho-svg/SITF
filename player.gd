extends CharacterBody3D

var speed := 6.0
var gravity := 18.0
var health := 100
var yaw := 0.0
var pitch := -0.08
var camera: Camera3D
var gun: MeshInstance3D
var can_shoot := true
var fire_delay := 0.13

func _ready():
    var collider := CollisionShape3D.new()
    var capsule := CapsuleShape3D.new()
    capsule.radius = 0.38
    capsule.height = 1.8
    collider.shape = capsule
    collider.position.y = 0.9
    add_child(collider)

    var body := MeshInstance3D.new()
    var capsule_mesh := CapsuleMesh.new()
    capsule_mesh.radius = 0.38
    capsule_mesh.height = 1.8
    body.mesh = capsule_mesh
    body.position.y = 0.9
    var mat := StandardMaterial3D.new()
    mat.albedo_color = Color("#4c83d6")
    body.material_override = mat
    add_child(body)

    camera = Camera3D.new()
    camera.position = Vector3(0,1.55,0)
    camera.current = true
    add_child(camera)

    gun = MeshInstance3D.new()
    var gm := BoxMesh.new()
    gm.size = Vector3(0.18,0.18,0.75)
    gun.mesh = gm
    gun.position = Vector3(0.35,-0.18,-0.55)
    var gmat := StandardMaterial3D.new()
    gmat.albedo_color = Color("#16191c")
    gun.material_override = gmat
    camera.add_child(gun)

    Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

func _unhandled_input(event):
    if event is InputEventMouseMotion:
        yaw -= event.relative.x * 0.0025
        pitch = clamp(pitch - event.relative.y * 0.0025, -1.35, 1.35)
        rotation.y = yaw
        camera.rotation.x = pitch
    if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
        shoot()

func _physics_process(delta):
    if not is_on_floor():
        velocity.y -= gravity * delta

    var input_vec := Input.get_vector("move_left","move_right","move_forward","move_back")
    var dir := (transform.basis * Vector3(input_vec.x,0,input_vec.y)).normalized()
    velocity.x = move_toward(velocity.x, dir.x * speed, 24 * delta)
    velocity.z = move_toward(velocity.z, dir.z * speed, 24 * delta)
    move_and_slide()

    if Input.is_action_pressed("shoot"):
        shoot()

    var hud = get_node_or_null("../HUD/HP")
    if hud:
        hud.text = "HP  %d" % health

func take_damage(amount: int):
    health = max(health - amount, 0)

func shoot():
    if not can_shoot or camera == null:
        return
    can_shoot = false
    var from := camera.global_position
    var to := from + -camera.global_transform.basis.z * 120.0
    var query := PhysicsRayQueryParameters3D.create(from, to)
    query.exclude = [self]
    var hit := get_world_3d().direct_space_state.intersect_ray(query)
    if hit:
        var obj = hit.collider
        if obj.has_method("take_damage"):
            obj.take_damage(34)
        _impact(hit.position)

    await get_tree().create_timer(fire_delay).timeout
    can_shoot = true

func _impact(pos: Vector3):
    var flash := OmniLight3D.new()
    flash.light_color = Color("#ffb36b")
    flash.light_energy = 3.0
    flash.omni_range = 2.0
    flash.position = pos
    get_tree().current_scene.add_child(flash)
    await get_tree().create_timer(0.04).timeout
    if is_instance_valid(flash):
        flash.queue_free()
