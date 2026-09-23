extends CharacterBody3D

signal died(enemy)

enum State { CHASE, STRAFE, ATTACK, SEARCH }

var state := State.CHASE
var target: Node3D
var health := 100
var speed := 2.6
var attack_range := 22.0
var fire_cooldown := 0.0
var think_timer := 0.0
var strafe_dir := 1.0
var body_root: Node3D
var hit_flash := 0.0
var last_seen := Vector3.ZERO

func _ready():
    _build_body()
    strafe_dir = -1.0 if randf() < 0.5 else 1.0

func _build_body():
    var collider := CollisionShape3D.new()
    var box_shape := BoxShape3D.new()
    box_shape.size = Vector3(0.72, 1.8, 0.55)
    collider.shape = box_shape
    collider.position.y = 0.9
    add_child(collider)

    body_root = Node3D.new()
    add_child(body_root)

    body_root.add_child(_part(Vector3(0.75,0.95,0.5), Vector3(0,0.98,0), Color("#9e3139")))
    body_root.add_child(_part(Vector3(0.48,0.48,0.48), Vector3(0,1.72,0), Color("#c27662")))

    var left_arm := _part(Vector3(0.18,0.8,0.18), Vector3(-0.52,0.9,0), Color("#7a242b"))
    left_arm.rotation_degrees.z = -6
    body_root.add_child(left_arm)

    var right_arm := _part(Vector3(0.18,0.8,0.18), Vector3(0.52,0.9,0), Color("#7a242b"))
    right_arm.rotation_degrees.z = 6
    body_root.add_child(right_arm)

    body_root.add_child(_part(Vector3(0.22,0.72,0.22), Vector3(-0.2,0.3,0), Color("#252b31")))
    body_root.add_child(_part(Vector3(0.22,0.72,0.22), Vector3(0.2,0.3,0), Color("#252b31")))

    var eye := OmniLight3D.new()
    eye.light_color = Color("#ff5058")
    eye.light_energy = 0.2
    eye.omni_range = 0.35
    eye.position = Vector3(0,1.72,-0.24)
    body_root.add_child(eye)

func _part(size_value: Vector3, pos: Vector3, color: Color) -> MeshInstance3D:
    var m := MeshInstance3D.new()
    var mesh := BoxMesh.new()
    mesh.size = size_value
    m.mesh = mesh
    m.position = pos

    var mat := StandardMaterial3D.new()
    mat.albedo_color = color
    m.material_override = mat
    return m

func _physics_process(delta):
    if not is_instance_valid(target) or health <= 0:
        return

    think_timer -= delta
    fire_cooldown -= delta

    if think_timer <= 0:
        think_timer = 0.16 + randf_range(0.0, 0.08)
        _think()

    var flat := Vector3(target.global_position.x, global_position.y, target.global_position.z)
    var distance := global_position.distance_to(flat)
    var aim_point := target.global_position + Vector3.UP * 1.05
    look_at(flat, Vector3.UP)

    if state == State.ATTACK:
        velocity = velocity.move_toward(Vector3.ZERO, 18.0 * delta)
        _attack(distance, aim_point)
    elif state == State.STRAFE:
        var to_target := (flat - global_position).normalized()
        var side := Vector3(-to_target.z, 0, to_target.x) * strafe_dir
        velocity.x = side.x * speed
        velocity.z = side.z * speed
        move_and_slide()
        if distance < 8.0:
            strafe_dir *= -1.0
    elif state == State.SEARCH:
        var seek := (last_seen - global_position).normalized()
        velocity.x = seek.x * speed
        velocity.z = seek.z * speed
        move_and_slide()
        if global_position.distance_to(last_seen) < 1.0:
            state = State.CHASE
    else:
        _steer_toward_player(flat, delta)

    if hit_flash > 0:
        hit_flash -= delta
        body_root.scale = Vector3(1.04,1.04,1.04)
    else:
        body_root.scale = Vector3.ONE

func _think():
    var aim_point := target.global_position + Vector3.UP * 1.05
    var distance := global_position.distance_to(target.global_position)
    var sees := _has_line_of_sight(aim_point)

    if sees:
        last_seen = aim_point
        if distance <= attack_range:
            if distance < 10.0 or randf() < 0.38:
                state = State.STRAFE
            else:
                state = State.ATTACK
        else:
            state = State.CHASE
    else:
        state = State.SEARCH

func _has_line_of_sight(point: Vector3) -> bool:
    var from := global_position + Vector3.UP * 1.35
    var query := PhysicsRayQueryParameters3D.create(from, point)
    query.exclude = [self]
    var hit := get_world_3d().direct_space_state.intersect_ray(query)
    return hit.is_empty() or hit.collider == target

func _steer_toward_player(flat: Vector3, delta: float):
    var dir := (flat - global_position).normalized()
    if _blocked(dir):
        var side := Vector3(-dir.z,0,dir.x) * strafe_dir
        dir = (dir + side * 0.9).normalized()
        if randf() < 0.25:
            strafe_dir *= -1.0

    velocity.x = move_toward(velocity.x, dir.x * speed, 10.0 * delta)
    velocity.z = move_toward(velocity.z, dir.z * speed, 10.0 * delta)
    move_and_slide()

func _blocked(dir: Vector3) -> bool:
    var from := global_position + Vector3.UP * 0.75
    var to := from + dir.normalized() * 1.35
    var query := PhysicsRayQueryParameters3D.create(from, to)
    query.exclude = [self]
    return not get_world_3d().direct_space_state.intersect_ray(query).is_empty()

func _attack(distance: float, aim_point: Vector3):
    if fire_cooldown > 0 or not _has_line_of_sight(aim_point):
        return

    fire_cooldown = 0.85 + randf_range(0.0, 0.65)
    if distance > 18.0:
        return

    var chance := clamp(0.87 - distance * 0.019, 0.34, 0.82)
    if randf() < chance:
        target.take_damage(8)

    var root := get_tree().current_scene
    if root.has_method("spawn_tracer"):
        root.spawn_tracer(global_position + Vector3.UP * 1.25, aim_point, true)

func take_damage(amount: int, hit_position := Vector3.ZERO):
    health -= amount
    hit_flash = 0.09

    var root := get_tree().current_scene
    if root.has_method("spawn_blood"):
        root.spawn_blood(hit_position if hit_position != Vector3.ZERO else global_position + Vector3.UP, Vector3.UP)

    state = State.CHASE
    last_seen = target.global_position

    if health <= 0:
        died.emit(self)
        queue_free()
