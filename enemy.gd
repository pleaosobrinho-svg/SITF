extends CharacterBody3D

var target: Node3D
var health := 100
var speed := 2.4
var attack_range := 18.0
var fire_timer := 0.0
var body_mesh: MeshInstance3D

func _ready():
    var collider := CollisionShape3D.new()
    var capsule := CapsuleShape3D.new()
    capsule.radius = 0.38
    capsule.height = 1.8
    collider.shape = capsule
    collider.position.y = 0.9
    add_child(collider)

    body_mesh = MeshInstance3D.new()
    var mesh := CapsuleMesh.new()
    mesh.radius = 0.38
    mesh.height = 1.8
    body_mesh.mesh = mesh
    body_mesh.position.y = 0.9
    var mat := StandardMaterial3D.new()
    mat.albedo_color = Color("#c94b4b")
    body_mesh.material_override = mat
    add_child(body_mesh)

func _physics_process(delta):
    if not is_instance_valid(target):
        return

    var flat_target := Vector3(target.global_position.x, global_position.y, target.global_position.z)
    var distance := global_position.distance_to(flat_target)
    look_at(flat_target, Vector3.UP)

    if distance > attack_range:
        var dir := (flat_target-global_position).normalized()
        velocity.x = dir.x * speed
        velocity.z = dir.z * speed
        velocity.y = -0.5
        move_and_slide()
    else:
        velocity = Vector3.ZERO
        fire_timer -= delta
        if fire_timer <= 0:
            fire_timer = 1.2
            if target.has_method("take_damage"):
                target.take_damage(8)

func take_damage(amount: int):
    health -= amount
    if health <= 0:
        queue_free()
        return
    if body_mesh:
        body_mesh.scale = Vector3(1.15,1.15,1.15)
        await get_tree().create_timer(0.08).timeout
        if is_instance_valid(body_mesh):
            body_mesh.scale = Vector3.ONE
