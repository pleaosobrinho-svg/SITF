extends CharacterBody3D

var game
var camera: Camera3D
var look_pitch := -4.0
var look_yaw := 180.0
var look_accum := Vector2.ZERO
var mouse_sensitivity := 0.075
var touch_sensitivity := 0.11
var bob_time := 0.0
var ads := false

const WALK_SPEED := 7.5
const SPRINT_SPEED := 10.5
const GRAVITY := 22.0
const JUMP_SPEED := 7.0

func _ready():
    add_to_group("player")
    var shape := CollisionShape3D.new()
    var capsule := CapsuleShape3D.new()
    capsule.radius = 0.45
    capsule.height = 1.8
    shape.shape = capsule
    shape.position.y = 0.9
    add_child(shape)

    camera = Camera3D.new()
    camera.current = true
    camera.position = Vector3(0, 1.62, 0)
    camera.fov = 78.0
    camera.near = 0.05
    camera.far = 120.0
    add_child(camera)

    Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _unhandled_input(event):
    if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
        look_accum += event.relative * mouse_sensitivity
    if event is InputEventKey and event.pressed and event.keycode == KEY_ESCAPE:
        Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
    if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
        Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func add_touch_look(delta: Vector2):
    look_accum += delta * touch_sensitivity

func set_ads(value: bool):
    ads = value

func _physics_process(delta):
    if game == null or game.match_over:
        velocity = Vector3.ZERO
        return

    var look := look_accum
    look_accum = Vector2.ZERO
    if look.length_squared() > 0.0:
        look_yaw -= look.x
        look_pitch = clamp(look_pitch - look.y, -80.0, 80.0)
        rotation_degrees.y = look_yaw
        camera.rotation_degrees.x = look_pitch

    var input_vec := Input.get_vector("move_left", "move_right", "move_forward", "move_back")
    var local := Vector3(input_vec.x, 0, input_vec.y)
    var direction := (global_transform.basis * local).normalized()

    var speed := WALK_SPEED
    if Input.is_key_pressed(KEY_SHIFT):
        speed = SPRINT_SPEED
    if input_vec.length() > 0.1:
        speed = SPRINT_SPEED

    velocity.x = move_toward(velocity.x, direction.x * speed, 28.0 * delta)
    velocity.z = move_toward(velocity.z, direction.z * speed, 28.0 * delta)

    if is_on_floor():
        if Input.is_action_just_pressed("jump"):
            velocity.y = JUMP_SPEED
        else:
            velocity.y = -0.5
    else:
        velocity.y -= GRAVITY * delta

    move_and_slide()

    if global_position.y < -8.0:
        game.player_damage(999.0)

    bob_time += delta * (8.0 if input_vec.length() > 0.1 else 2.0)
    var bob_amp := 0.045 if input_vec.length() > 0.1 else 0.012
    var target_cam_y := 1.62 + sin(bob_time * 2.0) * bob_amp
    var target_cam_x := sin(bob_time) * bob_amp * 0.65
    camera.position.x = lerp(camera.position.x, target_cam_x, delta * 8.0)
    camera.position.y = lerp(camera.position.y, target_cam_y, delta * 8.0)
    var target_fov := 61.0 if ads else 78.0
    camera.fov = lerp(camera.fov, target_fov, delta * 10.0)
